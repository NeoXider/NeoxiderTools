using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Shop;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoShopBridgesPurchaseTests
    {
        private const string ProfileKey = "Audit20261023NeoShopBridges.Purchases";
        private const string FeedbackFailure = "bridge feedback failed";
        private readonly List<Object> _objects = new();
        private ISaveProvider _previousProvider;
        private bool _previousErrors;
        private ProfileSaveProvider _provider;
        private InventoryComponent _inventory;
        private InventoryItemData _firstReward;
        private InventoryItemData _secondReward;
        private ShopItemData _firstItem;
        private ShopItemData _secondItem;
        private ShopBundleData _bundle;
        private ShopBehaviour _shop;
        private Money _money;
        private ShopInventoryGrantBridge _bridge;
        private readonly List<ShopNotificationFault> _faults = new();
        private int _purchaseFailures;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            _provider = new ProfileSaveProvider();
            SaveProvider.SetProvider(_provider);
            NeoDiagnostics.Configure(errors: true);
            _purchaseFailures = 0;
            _faults.Clear();

            _firstReward = MakeReward(42);
            _secondReward = MakeReward(43);
            _inventory = NewObject("Bridge purchase inventory").AddComponent<InventoryComponent>();
            JsonUtility.FromJsonOverwrite(
                "{\"_setInstanceOnAwake\":false,\"_autoLoad\":false,\"_autoSave\":false}", _inventory);

            _money = NewObject("Bridge purchase wallet").AddComponent<Money>();
            JsonUtility.FromJsonOverwrite("{\"_persistMoney\":false}", _money);
            _money.MaxMoney = 100f;
            _money.AddOverflow(100f);

            _firstItem = MakeItem("potion", 20);
            _secondItem = MakeItem("shield", 40);
            _bundle = ScriptableObject.CreateInstance<ShopBundleData>();
            _objects.Add(_bundle);
            JsonUtility.FromJsonOverwrite("{\"_id\":\"starter\",\"_bundlePrice\":30}", _bundle);

            _shop = NewObject("Bridge purchase shop").AddComponent<ShopBehaviour>();
            JsonUtility.FromJsonOverwrite("{\"_keySave\":\"" + ProfileKey + "\"}", _shop);
            _shop.AutoSpawnItems = false;
            _shop.SetMoneySpendSource(_money.gameObject);
            _shop.SetItems(new[] { _firstItem, _secondItem });
            _shop.OnNotificationFault.AddListener(_faults.Add);
            _shop.OnPurchaseFailedId.AddListener((string itemId) => _purchaseFailures++);

            _bridge = NewObject("Bridge purchase adapter").AddComponent<ShopInventoryGrantBridge>();
            _bridge.SetShop(_shop);
            _bridge.SetInventory(_inventory);
            _bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
                { ShopItemId = _firstItem.Id, InventoryItem = _firstReward, Amount = 3 });
            _bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
                { ShopItemId = _secondItem.Id, InventoryItem = _secondReward, Amount = 2 });
            _bridge.gameObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }
            }
            finally
            {
                _objects.Clear();
                _faults.Clear();
                NeoDiagnostics.Configure(errors: _previousErrors);
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        [Test]
        public void PaidPurchase_OnGrantedThrows_KeepsCommittedWalletProfileAndInventory()
        {
            bool committedBeforeFeedback = false;
            _bridge.OnGranted.AddListener((InventoryItemData item, int amount) =>
            {
                committedBeforeFeedback = _shop.IsOwned(_firstItem) && ReadProfile().IsItemOwned(_firstItem.Id);
                throw new InvalidOperationException(FeedbackFailure);
            });
            ExpectFeedbackFault(_firstItem.Id);

            _shop.Buy(_firstItem);

            Assert.That(committedBeforeFeedback, Is.True);
            AssertCommitted(false);
            AssertFeedbackFault(_firstItem.Id);
        }

        [Test]
        public void BundlePurchase_OnGrantedThrows_StillDeliversLaterChildAndBundleNotification()
        {
            ConfigureBundle();
            int acceptedAmount = 0;
            int bundleNotifications = 0;
            _shop.OnPurchasedBundle.AddListener((ShopBundleData bundle) => bundleNotifications++);
            _bridge.OnGranted.AddListener((InventoryItemData item, int amount) =>
            {
                acceptedAmount += amount;
                if (item == _firstReward)
                {
                    throw new InvalidOperationException(FeedbackFailure);
                }
            });
            ExpectFeedbackFault(_bundle.Id);

            _shop.BuyBundle(_bundle);

            AssertCommitted(true);
            Assert.That(acceptedAmount, Is.EqualTo(5));
            Assert.That(bundleNotifications, Is.EqualTo(1));
            AssertFeedbackFault(_bundle.Id);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProfileSaveFails_RefundsBeforeInventoryDelivery_AndRetryGrantsOnce(bool bundlePurchase)
        {
            ConfigureBundle();
            float balanceAtFailedSave = -1f;
            int grantNotifications = 0;
            _provider.FailNextProfileWrite = true;
            _provider.BeforeRejectedProfileWrite = () => balanceAtFailedSave = _money.money;
            _bridge.OnGranted.AddListener((InventoryItemData item, int amount) => grantNotifications++);
            string purchaseId = bundlePurchase ? _bundle.Id : _firstItem.Id;
            LogAssert.Expect(LogType.Error,
                "[Shop] Purchase of '" + purchaseId + "' failed while committing profile: profile write failed. Rolling back.");

            Buy(bundlePurchase);

            Assert.That(balanceAtFailedSave, Is.EqualTo(bundlePurchase ? 70f : 80f));
            Assert.That(_provider.RejectedProfileWrites, Is.EqualTo(1));
            Assert.That(_money.money, Is.EqualTo(100f));
            Assert.That(_money.allMoney, Is.EqualTo(100f));
            Assert.That(_shop.IsOwned(_firstItem), Is.False);
            Assert.That(_shop.IsOwned(_secondItem), Is.False);
            Assert.That(_shop.IsBundleOwned(_bundle), Is.False);
            Assert.That(_inventory.GetCount(_firstReward.ItemId), Is.Zero);
            Assert.That(_inventory.GetCount(_secondReward.ItemId), Is.Zero);
            Assert.That(grantNotifications, Is.Zero);
            Assert.That(_purchaseFailures, Is.EqualTo(1));
            Assert.That(_faults, Is.Empty);
            Assert.That(_shop.LastNotificationFault, Is.Null);
            ShopProfileData rolledBack = ReadProfile();
            Assert.That(rolledBack.IsItemOwned(_firstItem.Id), Is.False);
            Assert.That(rolledBack.IsItemOwned(_secondItem.Id), Is.False);
            Assert.That(rolledBack.IsBundleOwned(_bundle.Id), Is.False);

            Buy(bundlePurchase);

            Assert.That(_money.money, Is.EqualTo(bundlePurchase ? 70f : 80f));
            Assert.That(_shop.IsOwned(_firstItem), Is.True);
            Assert.That(_inventory.GetCount(_firstReward.ItemId), Is.EqualTo(3));
            Assert.That(_inventory.GetCount(_secondReward.ItemId), Is.EqualTo(bundlePurchase ? 2 : 0));
            Assert.That(grantNotifications, Is.EqualTo(bundlePurchase ? 2 : 1));
            Assert.That(_purchaseFailures, Is.EqualTo(1));
            Assert.That(ReadProfile().IsItemOwned(_firstItem.Id), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CapacityRejectsPartOrAll_PurchaseRemainsCommittedWithActualGrant(bool alreadyFull)
        {
            JsonUtility.FromJsonOverwrite("{\"_maxTotalItems\":2}", _inventory);
            if (alreadyFull)
            {
                Assert.That(_inventory.AddItemData(_firstReward, 2), Is.EqualTo(2));
            }

            int acceptedAmount = 0;
            int grantNotifications = 0;
            _bridge.OnGranted.AddListener((InventoryItemData item, int amount) =>
            {
                acceptedAmount += amount;
                grantNotifications++;
            });

            _shop.Buy(_firstItem);

            Assert.That(_money.money, Is.EqualTo(80f));
            Assert.That(_shop.IsOwned(_firstItem), Is.True);
            Assert.That(ReadProfile().IsItemOwned(_firstItem.Id), Is.True);
            Assert.That(_inventory.GetCount(_firstReward.ItemId), Is.EqualTo(2));
            Assert.That(acceptedAmount, Is.EqualTo(alreadyFull ? 0 : 2));
            Assert.That(grantNotifications, Is.EqualTo(alreadyFull ? 0 : 1));
            Assert.That(_purchaseFailures, Is.Zero);
            Assert.That(_faults, Is.Empty);
        }

        [Test]
        public void OnGranted_NestedItemAndBundlePurchases_AreRejectedBeforeAnotherDebit()
        {
            ConfigureBundle();
            _bridge.OnGranted.AddListener((InventoryItemData item, int amount) =>
            {
                if (item == _firstReward)
                {
                    _shop.Buy(_secondItem);
                    _shop.BuyBundle(_bundle);
                }
            });

            _shop.Buy(_firstItem);

            AssertCommitted(false);
            Assert.That(_shop.IsBundleOwned(_bundle), Is.False);
            Assert.That(_faults, Is.Empty);
        }

        private void AssertCommitted(bool bundlePurchase)
        {
            Assert.That(_money.money, Is.EqualTo(bundlePurchase ? 70f : 80f));
            Assert.That(_money.allMoney, Is.EqualTo(100f));
            Assert.That(_shop.IsOwned(_firstItem), Is.True);
            Assert.That(_shop.IsOwned(_secondItem), Is.EqualTo(bundlePurchase));
            Assert.That(_inventory.GetCount(_firstReward.ItemId), Is.EqualTo(3));
            Assert.That(_inventory.GetCount(_secondReward.ItemId), Is.EqualTo(bundlePurchase ? 2 : 0));
            Assert.That(_purchaseFailures, Is.Zero);
            ShopProfileData saved = ReadProfile();
            Assert.That(saved.IsItemOwned(_firstItem.Id), Is.True);
            Assert.That(saved.IsItemOwned(_secondItem.Id), Is.EqualTo(bundlePurchase));
            Assert.That(saved.IsBundleOwned(_bundle.Id), Is.EqualTo(bundlePurchase));
        }

        private void AssertFeedbackFault(string purchaseId)
        {
            Assert.That(_faults.Count, Is.EqualTo(1));
            ShopNotificationFault fault = _faults[0];
            Assert.That(fault.PurchaseId, Is.EqualTo(purchaseId));
            Assert.That(fault.ItemId, Is.EqualTo(_firstItem.Id));
            Assert.That(fault.Observer, Is.EqualTo(nameof(ShopBehaviour.OnPurchasedId)));
            Assert.That(fault.Message, Is.EqualTo(FeedbackFailure));
            Assert.That(_shop.LastNotificationFault, Is.SameAs(fault));
        }

        private void ExpectFeedbackFault(string purchaseId)
        {
            LogAssert.Expect(LogType.Error, "[Shop] Observer 'OnPurchasedId' failed after purchase '" + purchaseId +
                "': " + FeedbackFailure + ". Purchase state is unchanged.");
        }

        private void Buy(bool bundlePurchase)
        {
            if (bundlePurchase)
            {
                _shop.BuyBundle(_bundle);
            }
            else
            {
                _shop.Buy(_firstItem);
            }
        }

        private ShopProfileData ReadProfile()
        {
            return JsonUtility.FromJson<ShopProfileData>(_provider.GetString(ProfileKey));
        }

        private void ConfigureBundle()
        {
            FieldInfo itemsField = typeof(ShopBundleData).GetField("_items",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(itemsField, Is.Not.Null);
            itemsField.SetValue(_bundle, new[] { _firstItem, _secondItem });
            _shop.SetBundles(new[] { _bundle });
        }

        private GameObject NewObject(string name)
        {
            GameObject gameObject = new(name);
            gameObject.SetActive(false);
            _objects.Add(gameObject);
            return gameObject;
        }

        private ShopItemData MakeItem(string id, int price)
        {
            ShopItemData item = ScriptableObject.CreateInstance<ShopItemData>();
            _objects.Add(item);
            item.AssignIdIfEmpty(id);
            JsonUtility.FromJsonOverwrite("{\"_price\":" + price + "}", item);
            return item;
        }

        private InventoryItemData MakeReward(int id)
        {
            InventoryItemData item = ScriptableObject.CreateInstance<InventoryItemData>();
            _objects.Add(item);
            JsonUtility.FromJsonOverwrite("{\"_itemId\":" + id + "}", item);
            return item;
        }

        private sealed class ProfileSaveProvider : ISaveProvider
        {
            private readonly ShopPurchasePlayModeTests.MemorySaveProvider _inner = new();
            public bool FailNextProfileWrite;
            public int RejectedProfileWrites;
            public Action BeforeRejectedProfileWrite;
            public SaveProviderType ProviderType => _inner.ProviderType;
            public event Action OnDataSaved
            {
                add => _inner.OnDataSaved += value;
                remove => _inner.OnDataSaved -= value;
            }
            public event Action OnDataLoaded
            {
                add => _inner.OnDataLoaded += value;
                remove => _inner.OnDataLoaded -= value;
            }
            public event Action<string> OnKeyChanged
            {
                add => _inner.OnKeyChanged += value;
                remove => _inner.OnKeyChanged -= value;
            }
            public int GetInt(string key, int defaultValue = 0) => _inner.GetInt(key, defaultValue);
            public float GetFloat(string key, float defaultValue = 0f) => _inner.GetFloat(key, defaultValue);
            public string GetString(string key, string defaultValue = "") => _inner.GetString(key, defaultValue);
            public bool GetBool(string key, bool defaultValue = false) => _inner.GetBool(key, defaultValue);
            public void SetInt(string key, int value) => _inner.SetInt(key, value);
            public void SetFloat(string key, float value) => _inner.SetFloat(key, value);
            public void SetBool(string key, bool value) => _inner.SetBool(key, value);
            public bool HasKey(string key) => _inner.HasKey(key);
            public void DeleteKey(string key) => _inner.DeleteKey(key);
            public void DeleteAll() => _inner.DeleteAll();
            public void Save() => _inner.Save();
            public void Load() => _inner.Load();

            public void SetString(string key, string value)
            {
                if (key == ProfileKey && FailNextProfileWrite)
                {
                    FailNextProfileWrite = false;
                    RejectedProfileWrites++;
                    BeforeRejectedProfileWrite?.Invoke();
                    throw new InvalidOperationException("profile write failed");
                }

                _inner.SetString(key, value);
            }
        }
    }
}
