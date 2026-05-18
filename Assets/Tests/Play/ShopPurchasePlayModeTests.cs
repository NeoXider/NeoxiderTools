using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using MoneyBehaviour = Neo.Shop.Money;
using ShopBehaviour = Neo.Shop.Shop;
using ShopItemBehaviour = Neo.Shop.ShopItem;
using ShopItemDataAsset = Neo.Shop.ShopItemData;
using ShopBundleDataAsset = Neo.Shop.ShopBundleData;
using ShopCategoryButtonBehaviour = Neo.Shop.ShopCategoryButton;
using ShopListViewBehaviour = Neo.Shop.ShopListView;
using ShopPurchaseFlowEnum = Neo.Shop.ShopPurchaseFlow;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Shop purchase flow requires Play Mode so <see cref="ShopBehaviour"/> receives Awake/Start
    ///     and IMoneySpend wiring. Covers v8.5 string-id API plus the obsolete int proxy.
    /// </summary>
    public sealed class ShopPurchasePlayModeTests
    {
        private readonly List<GameObject> _spawnedRoots = new();
        private readonly List<ScriptableObject> _spawnedAssets = new();

        [TearDown]
        public void TearDown()
        {
            SaveProvider.SetProvider(new PlayerPrefsSaveProvider());

            foreach (GameObject go in _spawnedRoots)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _spawnedRoots.Clear();

            foreach (ScriptableObject so in _spawnedAssets)
            {
                if (so != null)
                {
                    Object.DestroyImmediate(so);
                }
            }

            _spawnedAssets.Clear();
        }

        private GameObject Track(GameObject go)
        {
            _spawnedRoots.Add(go);
            return go;
        }

        private T Track<T>(T so) where T : ScriptableObject
        {
            _spawnedAssets.Add(so);
            return so;
        }

        private ShopItemDataAsset MakeItem(string id, int price, bool singlePurchase = true)
        {
            ShopItemDataAsset data = Track(ScriptableObject.CreateInstance<ShopItemDataAsset>());
            SetPrivateField(data, "_id", id);
            SetPrivateField(data, "_nameItem", id);
            SetPrivateField(data, "_price", price);
            SetPrivateField(data, "_isSinglePurchase", singlePurchase);
            return data;
        }

        private ShopBundleDataAsset MakeBundle(string id, int price, params ShopItemDataAsset[] items)
        {
            ShopBundleDataAsset bundle = Track(ScriptableObject.CreateInstance<ShopBundleDataAsset>());
            SetPrivateField(bundle, "_id", id);
            SetPrivateField(bundle, "_nameBundle", id);
            SetPrivateField(bundle, "_bundlePrice", price);
            SetPrivateField(bundle, "_isSinglePurchase", true);
            SetPrivateField(bundle, "_items", items);
            return bundle;
        }

        private (GameObject root, ShopBehaviour shop, ShopItemBehaviour[] items) BuildShop(
            ShopItemDataAsset[] datas,
            ShopBundleDataAsset[] bundles = null,
            ShopPurchaseFlowEnum flow = ShopPurchaseFlowEnum.BuyAndEquip,
            GameObject moneySource = null)
        {
            GameObject root = Track(new GameObject("ShopRoot"));
            root.SetActive(false);

            ShopBehaviour shop = root.AddComponent<ShopBehaviour>();
            ShopItemBehaviour[] shopItems = new ShopItemBehaviour[datas.Length];
            for (int i = 0; i < datas.Length; i++)
            {
                GameObject child = new($"Item_{i}");
                child.transform.SetParent(root.transform);
                shopItems[i] = child.AddComponent<ShopItemBehaviour>();
            }

            SetPrivateField(shop, "_shopItemDatas", datas);
            SetPrivateField(shop, "_shopItems", shopItems);
            SetPrivateField(shop, "_purchaseFlow", flow);
            if (bundles != null)
            {
                SetPrivateField(shop, "_bundles", bundles);
            }

            if (moneySource != null)
            {
                shop.moneySpendSource = moneySource;
            }

            return (root, shop, shopItems);
        }

        private sealed class FakeMoney : MonoBehaviour, IMoneySpend
        {
            public bool Allow = true;
            public float TotalSpent;

            public bool Spend(float count)
            {
                if (!Allow)
                {
                    return false;
                }

                TotalSpent += count;
                return true;
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        // ---------------- Tests -----------------------------------------------

        [UnityTest]
        public IEnumerator Buy_FreeItem_SelectsWithoutSpend_LegacyIntApi()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset free = MakeItem("free_a", 0);
            GameObject walletGo = Track(new GameObject("Wallet"));
            FakeMoney wallet = walletGo.AddComponent<FakeMoney>();

            var built = BuildShop(new[] { free }, moneySource: walletGo);
            built.root.SetActive(true);

            yield return null;

#pragma warning disable CS0618
            built.shop.Buy(0);
            Assert.That(built.shop.Id, Is.EqualTo(0));
            Assert.That(built.shop.PreviewId, Is.EqualTo(0));
#pragma warning restore CS0618
            Assert.That(wallet.TotalSpent, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator Buy_PaidItem_AddsToOwnedAndSpends()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset item = MakeItem("sword", 30);
            GameObject walletGo = Track(new GameObject("Wallet"));
            FakeMoney wallet = walletGo.AddComponent<FakeMoney>();

            var built = BuildShop(new[] { item }, moneySource: walletGo);
            built.root.SetActive(true);

            yield return null;

            built.shop.Buy("sword");

            Assert.That(built.shop.IsOwned("sword"), Is.True);
            Assert.That(built.shop.EquippedId, Is.EqualTo("sword"));
            Assert.That(wallet.TotalSpent, Is.EqualTo(30f));
        }

        [UnityTest]
        public IEnumerator Buy_SinglePurchase_DoesNotRebuy()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset item = MakeItem("hat", 10, singlePurchase: true);
            GameObject walletGo = Track(new GameObject("Wallet"));
            FakeMoney wallet = walletGo.AddComponent<FakeMoney>();

            var built = BuildShop(new[] { item }, moneySource: walletGo);
            built.root.SetActive(true);

            yield return null;

            built.shop.Buy("hat");
            built.shop.Buy("hat");

            Assert.That(wallet.TotalSpent, Is.EqualTo(10f));
            Assert.That(built.shop.IsOwned("hat"), Is.True);
        }

        [UnityTest]
        public IEnumerator Buy_RuntimePriceOverride_RespectedThenOwned()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset item = MakeItem("ring", 100);
            GameObject walletGo = Track(new GameObject("Wallet"));
            FakeMoney wallet = walletGo.AddComponent<FakeMoney>();

            var built = BuildShop(new[] { item }, moneySource: walletGo);
            built.root.SetActive(true);

            yield return null;

            built.shop.SetRuntimePrice("ring", 20f);
            Assert.That(built.shop.GetPrice("ring"), Is.EqualTo(20f));

            built.shop.Buy("ring");

            Assert.That(wallet.TotalSpent, Is.EqualTo(20f));
            Assert.That(built.shop.IsOwned("ring"), Is.True);
        }

        [UnityTest]
        public IEnumerator BuyBundle_GrantsAllItemsAndBundleOwned()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset a = MakeItem("a", 0);
            ShopItemDataAsset b = MakeItem("b", 0);
            ShopBundleDataAsset bundle = MakeBundle("starter", 50, a, b);

            GameObject walletGo = Track(new GameObject("Wallet"));
            FakeMoney wallet = walletGo.AddComponent<FakeMoney>();

            var built = BuildShop(new[] { a, b }, new[] { bundle }, moneySource: walletGo);
            built.root.SetActive(true);

            yield return null;

            built.shop.BuyBundle("starter");

            Assert.That(wallet.TotalSpent, Is.EqualTo(50f));
            Assert.That(built.shop.IsOwned("a"), Is.True);
            Assert.That(built.shop.IsOwned("b"), Is.True);
            Assert.That(built.shop.IsBundleOwned("starter"), Is.True);
        }

        [UnityTest]
        public IEnumerator Buy_PerItemCurrencyOverrideSaveKey_SpendsFromMatchingMoney()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset gemmedItem = MakeItem("gem_skin", 5);
            SetPrivateField(gemmedItem, "_currencyOverrideSaveKey", "Gems");

            GameObject defaultGo = Track(new GameObject("DefaultWallet"));
            FakeMoney defaultWallet = defaultGo.AddComponent<FakeMoney>();

            GameObject gemsGo = Track(new GameObject("GemsWallet"));
            MoneyBehaviour gems = gemsGo.AddComponent<MoneyBehaviour>();
            SetPrivateField(gems, "_moneySave", "Gems");
            SetPrivateField(gems, "_persistMoney", false);
            gems.SetMoney(25);

            var built = BuildShop(new[] { gemmedItem }, moneySource: defaultGo);
            built.root.SetActive(true);
            yield return null;

            built.shop.Buy("gem_skin");

            Assert.That(defaultWallet.TotalSpent, Is.EqualTo(0f));
            Assert.That(gems.money, Is.EqualTo(20f));
            Assert.That(built.shop.IsOwned("gem_skin"), Is.True);
        }

        [UnityTest]
        public IEnumerator Reorder_ShopItemData_DoesNotBreakOwned()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset a = MakeItem("a", 0);
            ShopItemDataAsset b = MakeItem("b", 0);
            GameObject walletGo = Track(new GameObject("Wallet"));
            walletGo.AddComponent<FakeMoney>();

            var built = BuildShop(new[] { a, b }, moneySource: walletGo);
            built.root.SetActive(true);
            yield return null;

            built.shop.Buy("b");
            Assert.That(built.shop.IsOwned("b"), Is.True);

            // Reorder: b becomes index 0, a becomes index 1.
            SetPrivateField(built.shop, "_shopItemDatas", new[] { b, a });

            Assert.That(built.shop.IsOwned("b"), Is.True);
            Assert.That(built.shop.IsOwned("a"), Is.False);
        }

        [UnityTest]
        public IEnumerator PurchaseFlow_Browse_IgnoresBuy()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset item = MakeItem("locked", 10);
            GameObject walletGo = Track(new GameObject("Wallet"));
            FakeMoney wallet = walletGo.AddComponent<FakeMoney>();

            var built = BuildShop(new[] { item }, flow: ShopPurchaseFlowEnum.Browse, moneySource: walletGo);
            built.root.SetActive(true);

            yield return null;

            built.shop.Buy("locked");

            Assert.That(wallet.TotalSpent, Is.EqualTo(0f));
            Assert.That(built.shop.IsOwned("locked"), Is.False);
        }

        [UnityTest]
        public IEnumerator PurchaseFlow_EquipOnly_SkipsSpending()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset item = MakeItem("crown", 9999);
            GameObject walletGo = Track(new GameObject("Wallet"));
            FakeMoney wallet = walletGo.AddComponent<FakeMoney>();

            var built = BuildShop(new[] { item }, flow: ShopPurchaseFlowEnum.EquipOnly, moneySource: walletGo);
            built.root.SetActive(true);

            yield return null;

            built.shop.Buy("crown");

            Assert.That(wallet.TotalSpent, Is.EqualTo(0f));
            Assert.That(built.shop.EquippedId, Is.EqualTo("crown"));
            Assert.That(built.shop.IsOwned("crown"), Is.False);
        }

        // ---------------- Inventory integration -------------------------------

        [UnityTest]
        public IEnumerator Buy_WithBridgeMapping_GrantsToInventory()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            InventoryItemData invItem = Track(ScriptableObject.CreateInstance<InventoryItemData>());
            SetPrivateField(invItem, "_itemId", 42);
            SetPrivateField(invItem, "_displayName", "Potion");
            SetPrivateField(invItem, "_maxStack", -1);

            ShopItemDataAsset shopItem = MakeItem("potion_buy", 5);

            GameObject walletGo = Track(new GameObject("Wallet"));
            walletGo.AddComponent<FakeMoney>();

            GameObject invGo = Track(new GameObject("Inventory"));
            InventoryComponent inventory = invGo.AddComponent<InventoryComponent>();
            SetPrivateField(inventory, "_autoLoad", false);
            SetPrivateField(inventory, "_autoSave", false);
            SetPrivateField(inventory, "_saveKey", "ShopPlayTests_Inventory");

            var built = BuildShop(new[] { shopItem }, moneySource: walletGo);

            ShopInventoryGrantBridge bridge = built.root.AddComponent<ShopInventoryGrantBridge>();
            SetPrivateField(bridge, "_shop", built.shop);
            SetPrivateField(bridge, "_inventory", inventory);
            SetPrivateField(bridge, "_useInventorySingleton", false);
            SetPrivateField(bridge, "_mappings",
                new List<ShopInventoryGrantBridge.GrantMapping>
                {
                    new()
                    {
                        ShopItemId = "potion_buy",
                        InventoryItem = invItem,
                        Amount = 3
                    }
                });

            built.root.SetActive(true);
            yield return null;

            int grantedCount = -1;
            bridge.OnGranted.AddListener((data, amount) => grantedCount = amount);

            built.shop.Buy("potion_buy");

            Assert.That(built.shop.IsOwned("potion_buy"), Is.True);
            Assert.That(inventory.GetCount(42), Is.EqualTo(3));
            Assert.That(grantedCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator BuyBundle_WithBridgeMappings_GrantsInventoryForEachItem()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            InventoryItemData invA = Track(ScriptableObject.CreateInstance<InventoryItemData>());
            SetPrivateField(invA, "_itemId", 1);
            SetPrivateField(invA, "_maxStack", -1);

            InventoryItemData invB = Track(ScriptableObject.CreateInstance<InventoryItemData>());
            SetPrivateField(invB, "_itemId", 2);
            SetPrivateField(invB, "_maxStack", -1);

            ShopItemDataAsset itemA = MakeItem("invA", 0);
            ShopItemDataAsset itemB = MakeItem("invB", 0);
            ShopBundleDataAsset bundle = MakeBundle("starter_pack", 0, itemA, itemB);

            GameObject walletGo = Track(new GameObject("Wallet"));
            walletGo.AddComponent<FakeMoney>();

            GameObject invGo = Track(new GameObject("Inventory"));
            InventoryComponent inventory = invGo.AddComponent<InventoryComponent>();
            SetPrivateField(inventory, "_autoLoad", false);
            SetPrivateField(inventory, "_autoSave", false);
            SetPrivateField(inventory, "_saveKey", "ShopPlayTests_InventoryBundle");

            var built = BuildShop(new[] { itemA, itemB }, new[] { bundle }, moneySource: walletGo);

            ShopInventoryGrantBridge bridge = built.root.AddComponent<ShopInventoryGrantBridge>();
            SetPrivateField(bridge, "_shop", built.shop);
            SetPrivateField(bridge, "_inventory", inventory);
            SetPrivateField(bridge, "_useInventorySingleton", false);
            SetPrivateField(bridge, "_mappings",
                new List<ShopInventoryGrantBridge.GrantMapping>
                {
                    new() { ShopItemId = "invA", InventoryItem = invA, Amount = 2 },
                    new() { ShopItemId = "invB", InventoryItem = invB, Amount = 5 }
                });

            built.root.SetActive(true);
            yield return null;

            built.shop.BuyBundle("starter_pack");

            Assert.That(inventory.GetCount(1), Is.EqualTo(2));
            Assert.That(inventory.GetCount(2), Is.EqualTo(5));
            Assert.That(built.shop.IsBundleOwned("starter_pack"), Is.True);
        }

        [UnityTest]
        public IEnumerator ShopListView_CategoryFilter_SpawnsViewsAndButtonsBuyVisibleItems()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset sword = MakeItem("sword", 10);
            ShopItemDataAsset axe = MakeItem("axe", 20);
            ShopItemDataAsset hat = MakeItem("hat", 30);
            SetPrivateField(sword, "_category", "weapons");
            SetPrivateField(axe, "_category", "weapons");
            SetPrivateField(hat, "_category", "skins");

            GameObject walletGo = Track(new GameObject("Wallet"));
            walletGo.AddComponent<FakeMoney>();
            var built = BuildShop(new[] { sword, axe, hat }, moneySource: walletGo);
            SetPrivateField(built.shop, "_autoSpawnItems", false);

            GameObject viewRoot = Track(new GameObject("ShopListViewRoot"));
            GameObject listRoot = new("ItemsRoot");
            listRoot.transform.SetParent(viewRoot.transform);

            GameObject prefabGo = new("ShopItemPrefab");
            prefabGo.transform.SetParent(viewRoot.transform);
            ShopItemBehaviour prefab = prefabGo.AddComponent<ShopItemBehaviour>();
            prefab.buttonBuy = prefabGo.AddComponent<Button>();

            ShopListViewBehaviour view = viewRoot.AddComponent<ShopListViewBehaviour>();
            SetPrivateField(view, "_shop", built.shop);
            SetPrivateField(view, "_itemPrefab", prefab);
            SetPrivateField(view, "_itemsRoot", listRoot.transform);

            built.root.SetActive(true);
            viewRoot.SetActive(true);
            yield return null;

            view.ShowCategory("weapons");

            Assert.That(view.VisibleItems.Count, Is.EqualTo(2));
            Assert.That(view.VisibleItems[0].Id, Is.EqualTo("sword"));
            Assert.That(view.VisibleItems[1].Id, Is.EqualTo("axe"));

            ShopItemBehaviour[] spawnedViews = listRoot.GetComponentsInChildren<ShopItemBehaviour>(true);
            Assert.That(spawnedViews.Length, Is.GreaterThanOrEqualTo(2));

            spawnedViews[0].buttonBuy.onClick.Invoke();

            Assert.That(built.shop.IsOwned("sword"), Is.True);
            Assert.That(built.shop.EquippedId, Is.EqualTo("sword"));
        }

        [UnityTest]
        public IEnumerator ShopCategoryButton_Apply_SwitchesTargetViewCategory()
        {
            SaveProvider.SetProvider(new MemorySaveProvider());

            ShopItemDataAsset sword = MakeItem("sword", 10);
            ShopItemDataAsset hat = MakeItem("hat", 30);
            SetPrivateField(sword, "_category", "weapons");
            SetPrivateField(hat, "_category", "skins");

            GameObject walletGo = Track(new GameObject("Wallet"));
            walletGo.AddComponent<FakeMoney>();
            var built = BuildShop(new[] { sword, hat }, moneySource: walletGo);

            GameObject viewRoot = Track(new GameObject("ShopListViewRoot"));
            GameObject listRoot = new("ItemsRoot");
            listRoot.transform.SetParent(viewRoot.transform);

            GameObject prefabGo = new("ShopItemPrefab");
            prefabGo.transform.SetParent(viewRoot.transform);
            ShopItemBehaviour prefab = prefabGo.AddComponent<ShopItemBehaviour>();
            prefab.buttonBuy = prefabGo.AddComponent<Button>();

            ShopListViewBehaviour view = viewRoot.AddComponent<ShopListViewBehaviour>();
            SetPrivateField(view, "_shop", built.shop);
            SetPrivateField(view, "_itemPrefab", prefab);
            SetPrivateField(view, "_itemsRoot", listRoot.transform);

            GameObject tabGo = Track(new GameObject("WeaponsTab"));
            ShopCategoryButtonBehaviour tab = tabGo.AddComponent<ShopCategoryButtonBehaviour>();
            tab.TargetView = view;
            tab.Category = "weapons";

            built.root.SetActive(true);
            viewRoot.SetActive(true);
            tabGo.SetActive(true);
            yield return null;

            tab.Apply();

            Assert.That(view.Category, Is.EqualTo("weapons"));
            Assert.That(view.VisibleItems.Count, Is.EqualTo(1));
            Assert.That(view.VisibleItems[0].Id, Is.EqualTo("sword"));
        }

        // ---------------- Memory save provider for isolation -------------------

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, string> _store = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event System.Action OnDataSaved;
            public event System.Action OnDataLoaded;
            public event System.Action<string> OnKeyChanged;

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
                _store[key] = value;
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

            public bool HasKey(string key) => _store.ContainsKey(key);
            public void DeleteKey(string key) => _store.Remove(key);
            public void DeleteAll() => _store.Clear();
            public void Save() => OnDataSaved?.Invoke();
            public void Load() => OnDataLoaded?.Invoke();
        }
    }
}
