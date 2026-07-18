using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public class ShopManagerTests
    {
        private GameObject _go;
        private Money _money;

        [SetUp]
        public void Setup()
        {
            SaveProvider.SetProvider(new ThrowAwaySaveProvider());
            _go = new GameObject();
#if MIRROR
            _go.AddComponent<Mirror.NetworkIdentity>();
#endif
            _money = _go.AddComponent<Money>();
            // WHY: Emulate Start
            _money.SetMoney(0);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_go);
            SaveProvider.SetProvider(new PlayerPrefsSaveProvider());
        }

        [Test]
        public void Money_Add_IncreasesCurrentAndAllMoney()
        {
            _money.SetMoney(0);
            _money.AllMoney.Value = 0;

            _money.Add(150);

            Assert.AreEqual(150f, _money.money);
            Assert.AreEqual(150f, _money.allMoney);
            Assert.AreEqual(150f, _money.LastChangeMoneyValue);
        }

        [Test]
        public void Money_Spend_DecreasesValuesIfSufficient()
        {
            _money.SetMoney(200);

            bool success = _money.Spend(50);

            Assert.IsTrue(success);
            Assert.AreEqual(150f, _money.money);
            Assert.AreEqual(-50f, _money.LastChangeMoneyValue);
        }

        [Test]
        public void Money_Spend_FailsIfInsufficient()
        {
            _money.SetMoney(20);

            bool success = _money.Spend(50);

            Assert.IsFalse(success);
            Assert.AreEqual(20f, _money.money, "Money should not be deducted");
        }

        [Test]
        public void Money_Spend_RejectsNegativeAmounts()
        {
            _money.SetMoney(20);

            bool success = _money.Spend(-10);

            Assert.IsFalse(success);
            Assert.AreEqual(20f, _money.money, "Negative spend must not increase balance.");
        }

        [Test]
        public void Money_TrySpend_ReturnsDetailedStatuses()
        {
            _money.SetMoney(100);

            MoneySpendResult confirmed = _money.TrySpend(25);
            Assert.AreEqual(MoneySpendStatus.Confirmed, confirmed.Status);
            Assert.AreEqual(100f, confirmed.BalanceBefore);
            Assert.AreEqual(75f, confirmed.BalanceAfter);
            Assert.IsTrue(_money.Spend(10), "Legacy Spend should stay true only for confirmed spends.");

            MoneySpendResult invalid = _money.TrySpend(-1);
            Assert.AreEqual(MoneySpendStatus.RejectedInvalidAmount, invalid.Status);
            Assert.AreEqual(65f, _money.money);

            MoneySpendResult insufficient = _money.TrySpend(999);
            Assert.AreEqual(MoneySpendStatus.RejectedInsufficientFunds, insufficient.Status);
            Assert.AreEqual(65f, _money.money);
        }

#if MIRROR
        [Test]
        public void Money_RateLimit_FirstCommandAtStartupIsNotDropped()
        {
            System.Reflection.MethodInfo rateLimit = typeof(Money).GetMethod(
                "RateLimit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(rateLimit, "Money.RateLimit(NetworkConnectionToClient) must exist");

            // WHY: _lastCmdTime must start at NegativeInfinity — otherwise the first command inside
            // the server's initial rate-limit window (Time.time near 0) is silently dropped.
            bool first = (bool)rateLimit.Invoke(_money, new object[] { null });
            bool second = (bool)rateLimit.Invoke(_money, new object[] { null });

            Assert.IsFalse(first, "first command must pass regardless of server uptime");
            Assert.IsTrue(second, "immediate second command from the same source is limited");
        }
#endif

        [Test]
        public void Shop_Buy_WhenSpendNeedsServerAuthority_DoesNotGrantOrFailLocally()
        {
            var shopObject = new GameObject("Shop");
            var walletObject = new GameObject("PendingWallet");
            ShopItemData item = ScriptableObject.CreateInstance<ShopItemData>();

            try
            {
                SetSerialized(item, "_id", "sword");
                SetSerialized(item, "_nameItem", "Sword");
                SetSerialized(item, "_price", 50);

                PendingAuthorityMoneySpend pendingWallet = walletObject.AddComponent<PendingAuthorityMoneySpend>();
                Shop.Shop shop = shopObject.AddComponent<Neo.Shop.Shop>();
                shop.AutoSpawnItems = false;
                shop.SetItems(new[] { item });
                shop.SetMoneySpendSource(walletObject);

                int purchased = 0;
                int failed = 0;
                shop.OnPurchasedId.AddListener(_ => purchased++);
                shop.OnPurchaseFailedId.AddListener(_ => failed++);

                shop.Buy("sword");

                Assert.AreEqual(0, pendingWallet.TrySpendCalls,
                    "Shop must not send a wallet-only spend command when item grant needs server authority.");
                Assert.IsFalse(shop.IsOwned("sword"));
                Assert.AreEqual(0, purchased);
                Assert.AreEqual(0, failed, "Pending server authority is not a local purchase failure.");
            }
            finally
            {
                Object.DestroyImmediate(item);
                Object.DestroyImmediate(walletObject);
                Object.DestroyImmediate(shopObject);
            }
        }

        [Test]
        public void Shop_BuyBundle_WhenSpendNeedsServerAuthority_DoesNotGrantBundleOrItemsLocally()
        {
            var shopObject = new GameObject("Shop");
            var walletObject = new GameObject("PendingWallet");
            ShopItemData item = ScriptableObject.CreateInstance<ShopItemData>();
            ShopBundleData bundle = ScriptableObject.CreateInstance<ShopBundleData>();

            try
            {
                SetSerialized(item, "_id", "bundle_sword");
                SetSerialized(item, "_nameItem", "Bundle Sword");
                SetSerialized(item, "_price", 5);
                SetSerialized(bundle, "_id", "starter_bundle");
                SetSerialized(bundle, "_nameBundle", "Starter Bundle");
                SetSerialized(bundle, "_bundlePrice", 100);
                SetSerialized(bundle, "_items", new[] { item });

                PendingAuthorityMoneySpend pendingWallet = walletObject.AddComponent<PendingAuthorityMoneySpend>();
                Shop.Shop shop = shopObject.AddComponent<Neo.Shop.Shop>();
                shop.AutoSpawnItems = false;
                shop.SetItems(new[] { item });
                shop.SetBundles(new[] { bundle });
                shop.SetMoneySpendSource(walletObject);

                int purchasedBundles = 0;
                int failed = 0;
                shop.OnPurchasedBundle.AddListener(_ => purchasedBundles++);
                shop.OnPurchaseFailedId.AddListener(_ => failed++);

                shop.BuyBundle("starter_bundle");

                Assert.AreEqual(0, pendingWallet.TrySpendCalls,
                    "Shop must not send a wallet-only spend command when bundle grant needs server authority.");
                Assert.IsFalse(shop.IsBundleOwned("starter_bundle"));
                Assert.IsFalse(shop.IsOwned("bundle_sword"));
                Assert.AreEqual(0, purchasedBundles);
                Assert.AreEqual(0, failed, "Pending server authority is not a local bundle purchase failure.");
            }
            finally
            {
                Object.DestroyImmediate(bundle);
                Object.DestroyImmediate(item);
                Object.DestroyImmediate(walletObject);
                Object.DestroyImmediate(shopObject);
            }
        }

        private static void SetSerialized(Object target, string propertyName, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            switch (value)
            {
                case string stringValue:
                    property.stringValue = stringValue;
                    break;
                case int intValue:
                    property.intValue = intValue;
                    break;
                case ShopItemData[] itemArray:
                    property.arraySize = itemArray.Length;
                    for (int i = 0; i < itemArray.Length; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue = itemArray[i];
                    }

                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class ThrowAwaySaveProvider : ISaveProvider
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _store = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
#pragma warning disable CS0067
            public event System.Action OnDataSaved;
            public event System.Action OnDataLoaded;
            public event System.Action<string> OnKeyChanged;
#pragma warning restore CS0067

            public int GetInt(string key, int defaultValue = 0)
            {
                return _store.TryGetValue(key, out string s) && int.TryParse(s, out int v) ? v : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _store[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _store.TryGetValue(key, out string s) && float.TryParse(s, out float v) ? v : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _store[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _store.TryGetValue(key, out string s) ? s : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _store[key] = value ?? "";
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _store.TryGetValue(key, out string s) && bool.TryParse(s, out bool v) ? v : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _store[key] = value.ToString();
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key)
            {
                return _store.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _store.Remove(key);
            }

            public void DeleteAll()
            {
                _store.Clear();
            }

            public void Save()
            {
            }

            public void Load()
            {
            }
        }

        private sealed class PendingAuthorityMoneySpend : MonoBehaviour, IMoneySpendAuthority
        {
            public int TrySpendCalls { get; private set; }

            public bool Spend(float count)
            {
                return TrySpend(count).IsConfirmed;
            }

            public MoneySpendResult TrySpend(float count)
            {
                TrySpendCalls++;
                return new MoneySpendResult(MoneySpendStatus.RequestedServerAuthority, count, 100f, 100f);
            }

            public bool CanConfirmSpendNow(float count)
            {
                return false;
            }
        }
    }
}
