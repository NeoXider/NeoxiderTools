using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Save;
using Neo.Shop;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using ShopBehaviour = Neo.Shop.Shop;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoShopCommitPlayModeTests
    {
        private readonly List<GameObject> _roots = new();
        private readonly List<ScriptableObject> _assets = new();
        private ISaveProvider _previousProvider;
        private bool _previousErrors;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            SaveProvider.SetProvider(new MemoryProvider());
            _previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            NeoDiagnostics.Configure(errors: true);
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _roots.Count - 1; i >= 0; i--)
                {
                    if (_roots[i] != null)
                    {
                        Object.DestroyImmediate(_roots[i]);
                    }
                }

                for (int i = _assets.Count - 1; i >= 0; i--)
                {
                    if (_assets[i] != null)
                    {
                        Object.DestroyImmediate(_assets[i]);
                    }
                }
            }
            finally
            {
                _roots.Clear();
                _assets.Clear();
                Time.timeScale = _previousTimeScale;
                NeoDiagnostics.Configure(errors: _previousErrors);
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        [UnityTest]
        public IEnumerator ItemGrantThenObserverFailure_KeepsWalletProfileAndActualInventoryCommitted()
        {
            yield return RunGrantObserverCase(false);
        }

        [UnityTest]
        public IEnumerator BundleGrantThenObserverFailure_ContinuesLaterChildrenAndKeepsConservation()
        {
            yield return RunGrantObserverCase(true);
        }

        [UnityTest]
        public IEnumerator PriceOneAtHugeBalance_RejectsBeforeProfileSaveAndActualInventoryGrant()
        {
            GameObject root = NewRoot("ShopZeroEffectiveDebit");
            root.SetActive(false);
            Money money = root.AddComponent<Money>();
            money.MaxMoney = 100f;
            money.AddOverflow(100000000f);
            InventoryComponent inventory = root.AddComponent<InventoryComponent>();
            SetField(inventory, "_autoLoad", false);
            SetField(inventory, "_autoSave", false);
            ShopBehaviour shop = root.AddComponent<ShopBehaviour>();
            shop.AutoSpawnItems = false;
            SetField(shop, "_activateSavedEquipped", false);
            shop.SetMoneySpendSource(money.gameObject);
            ShopItemData item = MakeItem("price-one", 1);
            shop.SetItems(new[] { item });
            InventoryItemData grant = MakeInventoryItem(4203);
            ShopInventoryGrantBridge bridge = root.AddComponent<ShopInventoryGrantBridge>();
            bridge.SetShop(shop);
            bridge.SetInventory(inventory);
            bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
                { ShopItemId = item.Id, InventoryItem = grant, Amount = 3 });
            root.SetActive(true);
            yield return null;

            int grants = 0;
            int purchases = 0;
            int failures = 0;
            int walletEvents = 0;
            int saveWrites = 0;
            bridge.OnGranted.AddListener((_, amount) => grants += amount);
            shop.OnPurchasedId.AddListener(_ => purchases++);
            shop.OnPurchaseFailedId.AddListener(_ => failures++);
            money.CurrentMoney.AddListener(_ => walletEvents++);
            money.AllMoney.AddListener(_ => walletEvents++);
            money.LastChangeMoney.AddListener(_ => walletEvents++);
            money.LevelMoney.AddListener(_ => walletEvents++);
            string profileBefore = SaveProvider.GetString("Shop");
            ISaveProvider provider = SaveProvider.CurrentProvider;
            Action<string> observeSave = _ => saveWrites++;
            provider.OnKeyChanged += observeSave;
            try
            {
                Assert.That(shop.CanAfford(item), Is.False);
                shop.Buy(item);

                Assert.That(money.money, Is.EqualTo(100000000f));
                Assert.That(money.allMoney, Is.EqualTo(100000000f));
                Assert.That(money.LastChangeMoneyValue, Is.EqualTo(100000000f));
                Assert.That(SaveProvider.GetFloat(money.SaveKey), Is.EqualTo(100000000f));
                Assert.That(SaveProvider.GetFloat(money.SaveKey + nameof(Money.AllMoney)), Is.EqualTo(100000000f));
                Assert.That(shop.IsOwned(item), Is.False);
                Assert.That(shop.EquippedId, Is.Empty);
                Assert.That(inventory.GetCount(grant.ItemId), Is.Zero);
                Assert.That(grants, Is.Zero);
                Assert.That(purchases, Is.Zero);
                Assert.That(failures, Is.EqualTo(1));
                Assert.That(walletEvents, Is.Zero);
                Assert.That(saveWrites, Is.Zero);
                Assert.That(SaveProvider.GetString("Shop"), Is.EqualTo(profileBefore));
                Assert.That(shop.LastNotificationFault, Is.Null);
            }
            finally
            {
                provider.OnKeyChanged -= observeSave;
            }
        }

        private IEnumerator RunGrantObserverCase(bool bundlePurchase)
        {
            GameObject root = NewRoot("ShopCommit");
            root.SetActive(false);
            Money money = root.AddComponent<Money>();
            SetField(money, "_persistMoney", false);
            money.MaxMoney = 100f;
            money.AddOverflow(200f);
            InventoryComponent inventory = root.AddComponent<InventoryComponent>();
            SetField(inventory, "_autoLoad", false);
            SetField(inventory, "_autoSave", false);
            ShopBehaviour shop = root.AddComponent<ShopBehaviour>();
            shop.AutoSpawnItems = false;
            SetField(shop, "_activateSavedEquipped", false);
            shop.SetMoneySpendSource(money.gameObject);
            ShopItemData first = MakeItem("first", 50);
            ShopItemData second = MakeItem("second", 50);
            shop.SetItems(new[] { first, second });
            ShopBundleData bundle = MakeBundle(first, second);
            shop.SetBundles(new[] { bundle });
            InventoryItemData firstGrant = MakeInventoryItem(4201);
            InventoryItemData secondGrant = MakeInventoryItem(4202);
            ShopInventoryGrantBridge bridge = root.AddComponent<ShopInventoryGrantBridge>();
            bridge.SetShop(shop);
            bridge.SetInventory(inventory);
            bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
                { ShopItemId = first.Id, InventoryItem = firstGrant, Amount = 3 });
            bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
                { ShopItemId = second.Id, InventoryItem = secondGrant, Amount = 2 });
            root.SetActive(true);
            yield return null;

            int failures = 0;
            int bundleNotifications = 0;
            int acceptedGrants = 0;
            List<ShopNotificationFault> faults = new();
            bridge.OnGranted.AddListener((item, amount) => acceptedGrants += amount);
            shop.OnPurchaseFailedId.AddListener(_ => failures++);
            shop.OnPurchasedBundle.AddListener(_ => bundleNotifications++);
            shop.OnNotificationFault.AddListener(faults.Add);
            // Register after OnEnable so the real bridge delivers its committed grant before this observer faults.
            shop.OnPurchasedId.AddListener(id =>
            {
                if (id == first.Id)
                {
                    throw new InvalidOperationException("post-delivery observer failed");
                }
            });
            string purchaseId = bundlePurchase ? bundle.Id : first.Id;
            LogAssert.Expect(LogType.Error,
                new Regex($@"\[Shop\] Observer 'OnPurchasedId' failed after purchase '{purchaseId}'"));

            if (bundlePurchase)
            {
                shop.BuyBundle(bundle);
            }
            else
            {
                shop.Buy(first);
            }

            Assert.That(money.money, Is.EqualTo(150f));
            Assert.That(money.allMoney, Is.EqualTo(200f));
            Assert.That(shop.IsOwned(first), Is.True);
            Assert.That(inventory.GetCount(firstGrant.ItemId), Is.EqualTo(3));
            Assert.That(inventory.GetCount(secondGrant.ItemId), Is.EqualTo(bundlePurchase ? 2 : 0));
            Assert.That(acceptedGrants, Is.EqualTo(bundlePurchase ? 5 : 3));
            Assert.That(bundleNotifications, Is.EqualTo(bundlePurchase ? 1 : 0));
            Assert.That(failures, Is.Zero);
            ShopProfileData saved = JsonUtility.FromJson<ShopProfileData>(SaveProvider.GetString("Shop"));
            Assert.That(saved.IsItemOwned(first.Id), Is.True);
            Assert.That(saved.IsItemOwned(second.Id), Is.EqualTo(bundlePurchase));
            Assert.That(saved.IsBundleOwned(bundle.Id), Is.EqualTo(bundlePurchase));
            Assert.That(faults.Count, Is.EqualTo(1));
            Assert.That(faults[0].PurchaseId, Is.EqualTo(purchaseId));
            Assert.That(faults[0].ItemId, Is.EqualTo(first.Id));
            Assert.That(faults[0].Message, Is.EqualTo("post-delivery observer failed"));
        }

        [UnityTest]
        public IEnumerator TextMoney_DisablePausesBaseTween_AndEnableUsesCurrentBalance()
        {
            GameObject walletRoot = NewRoot("Wallet");
            Money money = walletRoot.AddComponent<Money>();
            SetField(money, "_persistMoney", false);
            money.Add(200f);
            GameObject textRoot = NewRoot("MoneyText");
            textRoot.SetActive(false);
            TMP_Text label = textRoot.AddComponent<TextMeshProUGUI>();
            TextMoney display = textRoot.AddComponent<TextMoney>();
            SetField(display, "text", label);
            SetField(display, "_timeAnim", 30f);
            display.SetMoneySource(money);
            textRoot.SetActive(true);
            yield return null;
            yield return new WaitForSecondsRealtime(0.1f);

            float pausedNumber = display.currentNum;
            string pausedText = label.text;
            Assert.That(pausedNumber, Is.GreaterThan(0f).And.LessThan(200f));
            display.enabled = false;
            money.Add(50f);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(display.currentNum, Is.EqualTo(pausedNumber));
            Assert.That(label.text, Is.EqualTo(pausedText));
            Assert.That(display.amount, Is.EqualTo(200f));

            SetField(display, "_timeAnim", 0f);
            display.enabled = true;
            yield return null;
            yield return null;
            Assert.That(display.amount, Is.EqualTo(250f));
            Assert.That(display.currentNum, Is.EqualTo(250f).Within(0.001f));
            Assert.That(label.text, Is.EqualTo("250"));
        }

#if MIRROR
        [UnityTest]
        public IEnumerator MoneyLimiter_PeerWindowsAdvanceWhileGameTimeIsPaused()
        {
            Money money = NewRoot("MoneyLimiter").AddComponent<Money>();
            SetField(money, "_persistMoney", false);
            MethodInfo rateLimit = typeof(Money).GetMethod("RateLimit", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rateLimit, Is.Not.Null);
            NetworkConnectionToClient first = new(3201);
            NetworkConnectionToClient second = new(3202);
            Time.timeScale = 0f;
            Assert.That((bool)rateLimit.Invoke(money, new object[] { first }), Is.False);
            Assert.That((bool)rateLimit.Invoke(money, new object[] { first }), Is.True);
            Assert.That((bool)rateLimit.Invoke(money, new object[] { second }), Is.False);
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That((bool)rateLimit.Invoke(money, new object[] { first }), Is.False);
            Assert.That(money.money, Is.Zero);
            // This directly observes private limiter cadence, not actual remote Command admission.
        }
#endif

        private GameObject NewRoot(string name)
        {
            GameObject root = new(name);
            _roots.Add(root);
            return root;
        }

        private ShopItemData MakeItem(string id, int price)
        {
            ShopItemData item = ScriptableObject.CreateInstance<ShopItemData>();
            _assets.Add(item);
            item.AssignIdIfEmpty(id);
            SetField(item, "_price", price);
            return item;
        }

        private InventoryItemData MakeInventoryItem(int id)
        {
            InventoryItemData item = ScriptableObject.CreateInstance<InventoryItemData>();
            _assets.Add(item);
            SetField(item, "_itemId", id);
            SetField(item, "_maxStack", -1);
            return item;
        }

        private ShopBundleData MakeBundle(params ShopItemData[] items)
        {
            ShopBundleData bundle = ScriptableObject.CreateInstance<ShopBundleData>();
            _assets.Add(bundle);
            SetField(bundle, "_id", "starter");
            SetField(bundle, "_bundlePrice", 50);
            SetField(bundle, "_items", items);
            return bundle;
        }

        private static void SetField(object target, string name, object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Missing setup field {name}");
        }

        private sealed class MemoryProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new();
            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0) =>
                _values.TryGetValue(key, out object value) && value is int number ? number : defaultValue;
            public float GetFloat(string key, float defaultValue = 0f) =>
                _values.TryGetValue(key, out object value) && value is float number ? number : defaultValue;
            public string GetString(string key, string defaultValue = "") =>
                _values.TryGetValue(key, out object value) && value is string text ? text : defaultValue;
            public bool GetBool(string key, bool defaultValue = false) =>
                _values.TryGetValue(key, out object value) && value is bool flag ? flag : defaultValue;

            public void SetInt(string key, int value) => Set(key, value);
            public void SetFloat(string key, float value) => Set(key, value);
            public void SetString(string key, string value) => Set(key, value);
            public void SetBool(string key, bool value) => Set(key, value);
            public bool HasKey(string key) => _values.ContainsKey(key);
            public void DeleteKey(string key) => _values.Remove(key);
            public void DeleteAll() => _values.Clear();
            public void Save() => OnDataSaved?.Invoke();
            public void Load() => OnDataLoaded?.Invoke();

            private void Set(string key, object value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
