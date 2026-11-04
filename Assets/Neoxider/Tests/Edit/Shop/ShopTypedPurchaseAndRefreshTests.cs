using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Save;
using Neo.Shop;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the typed purchase API (<see cref="ShopBehaviour.TryBuy(string)"/>), list-view refresh
    ///     coalescing and allocation, runtime catalog growth for Shop-owned views, and event-driven
    ///     <see cref="TextMoney"/> wallet binding.
    /// </summary>
    public sealed class ShopTypedPurchaseAndRefreshTests
    {
        private readonly List<Object> _objects = new();
        private readonly List<MonoBehaviour> _enabled = new();
        private ISaveProvider _previousProvider;
        private Audit20261023NeoShopMemoryProvider _provider;
        private bool _previousErrors;
        private Money _money;
        private ShopBehaviour _shop;
        private ShopItemData _sword;
        private ShopItemData _hat;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _provider = new Audit20261023NeoShopMemoryProvider();
            SaveProvider.SetProvider(_provider);
            _previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            NeoDiagnostics.Configure(errors: true);
            _money = CreateMoney("TypedCoins");
            _shop = NewObject("Shop").AddComponent<ShopBehaviour>();
            _shop.AutoSpawnItems = false;
            _shop.SetMoneySpendSource(_money.gameObject);
            _sword = CreateItem("sword", 50);
            _hat = CreateItem("hat", 20);
            _shop.SetItems(new[] { _sword, _hat });
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _enabled.Count - 1; i >= 0; i--)
            {
                if (_enabled[i] != null)
                {
                    InvokeLifecycle(_enabled[i], "OnDisable");
                }
            }

            _enabled.Clear();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            SaveProvider.SetProvider(_previousProvider);
            NeoDiagnostics.Configure(errors: _previousErrors);
        }

        [Test]
        public void TryBuy_ReportsPurchasedThenAlreadyOwned()
        {
            _money.Add(100f);

            Assert.That(_shop.TryBuy(_sword), Is.EqualTo(ShopPurchaseStatus.Purchased));
            Assert.That(_shop.TryBuy(_sword), Is.EqualTo(ShopPurchaseStatus.AlreadyOwned));

            Assert.That(_money.money, Is.EqualTo(50f), "An owned item is not charged again.");
            Assert.That(_shop.IsOwned(_sword), Is.True);
            Assert.That(_shop.EquippedId, Is.EqualTo(_sword.Id));
        }

        [Test]
        public void TryBuy_WithoutBalance_ReportsInsufficientFundsAndRaisesFailure()
        {
            string failed = null;
            _shop.OnPurchaseFailedId.AddListener(id => failed = id);

            Assert.That(_shop.TryBuy(_sword.Id), Is.EqualTo(ShopPurchaseStatus.InsufficientFunds));

            Assert.That(failed, Is.EqualTo(_sword.Id));
            Assert.That(_shop.IsOwned(_sword), Is.False);
        }

        [Test]
        public void TryBuy_ProfileWriteFailure_ReportsCommitFailedAndRefunds()
        {
            _money.Add(100f);
            _provider.StringWritesToFail = 1;
            LogAssert.Expect(LogType.Error, new Regex(@"\[Shop\] Purchase of 'sword' failed while committing profile"));

            Assert.That(_shop.TryBuy(_sword), Is.EqualTo(ShopPurchaseStatus.CommitFailed));

            Assert.That(_money.money, Is.EqualTo(100f));
            Assert.That(_shop.IsOwned(_sword), Is.False);
        }

        [Test]
        public void TryBuy_UnknownItemOrBrowseFlow_ReportsUnavailable()
        {
            _money.Add(100f);
            Assert.That(_shop.TryBuy("missing"), Is.EqualTo(ShopPurchaseStatus.Unavailable));
            Assert.That(_shop.TryBuy((ShopItemData)null), Is.EqualTo(ShopPurchaseStatus.Unavailable));

            SetPrivate(_shop, "_purchaseFlow", ShopPurchaseFlow.Browse);

            Assert.That(_shop.TryBuy(_sword), Is.EqualTo(ShopPurchaseStatus.Unavailable));
            Assert.That(_money.money, Is.EqualTo(100f));
        }

        [Test]
        public void TryBuy_EquipOnlyFlow_ReportsSelectedWithoutSpending()
        {
            _money.Add(100f);
            SetPrivate(_shop, "_purchaseFlow", ShopPurchaseFlow.EquipOnly);

            Assert.That(_shop.TryBuy(_hat), Is.EqualTo(ShopPurchaseStatus.Selected));

            Assert.That(_shop.EquippedId, Is.EqualTo(_hat.Id));
            Assert.That(_money.money, Is.EqualTo(100f));
        }

        [Test]
        public void TryBuy_FromPurchaseCallback_ReportsBusy()
        {
            _money.Add(100f);
            ShopPurchaseStatus nested = ShopPurchaseStatus.Purchased;
            _shop.OnPurchasedId.AddListener(id =>
            {
                if (id == _sword.Id)
                {
                    nested = _shop.TryBuy(_hat);
                }
            });

            Assert.That(_shop.TryBuy(_sword), Is.EqualTo(ShopPurchaseStatus.Purchased));

            Assert.That(nested, Is.EqualTo(ShopPurchaseStatus.Busy));
            Assert.That(_shop.IsOwned(_hat), Is.False);
            Assert.That(_shop.TryBuy(_hat), Is.EqualTo(ShopPurchaseStatus.Purchased), "The guard is released.");
        }

        [Test]
        public void TryBuyBundle_ReportsPurchasedThenAlreadyOwned()
        {
            _money.Add(100f);
            ShopBundleData bundle = CreateBundle("starter", 30, _sword, _hat);
            _shop.SetBundles(new[] { bundle });
            int failures = 0;
            _shop.OnPurchaseFailedId.AddListener(_ => failures++);

            Assert.That(_shop.TryBuyBundle(bundle), Is.EqualTo(ShopPurchaseStatus.Purchased));
            Assert.That(_shop.TryBuyBundle(bundle), Is.EqualTo(ShopPurchaseStatus.AlreadyOwned),
                "An owned single-purchase bundle is told apart from an unknown id.");
            Assert.That(_shop.TryBuyBundle("missing"), Is.EqualTo(ShopPurchaseStatus.Unavailable));

            Assert.That(failures, Is.Zero, "Owned and unknown bundles are no-ops, not failed purchases.");

            Assert.That(_money.money, Is.EqualTo(70f));
            Assert.That(_shop.IsOwned(_sword), Is.True);
            Assert.That(_shop.IsOwned(_hat), Is.True);
        }

        [Test]
        public void TryBuyBundle_WithoutBalance_ReportsInsufficientFunds()
        {
            ShopBundleData bundle = CreateBundle("starter", 30, _sword);
            _shop.SetBundles(new[] { bundle });

            Assert.That(_shop.TryBuyBundle(bundle.Id), Is.EqualTo(ShopPurchaseStatus.InsufficientFunds));
            Assert.That(_shop.IsBundleOwned(bundle), Is.False);
        }

        [Test]
        public void ListView_SinglePurchase_RebuildsOnce()
        {
            _money.Add(100f);
            ShopListView list = CreateList(CreateRow("Row A"), CreateRow("Row B"));
            int refreshes = 0;
            list.OnRefreshed.AddListener(() => refreshes++);

            _shop.Buy(_sword);

            Assert.That(refreshes, Is.EqualTo(1), "OnPurchasedId/OnSelectId after OnShopChanged carry no new state.");
            Assert.That(list.Views[0].BoundItemData, Is.SameAs(_sword));
        }

        [Test]
        public void ListView_BundlePurchase_RebuildsOnce()
        {
            _money.Add(100f);
            ShopBundleData bundle = CreateBundle("starter", 30, _sword, _hat);
            _shop.SetBundles(new[] { bundle });
            ShopListView list = CreateList(CreateRow("Row A"), CreateRow("Row B"));
            int refreshes = 0;
            list.OnRefreshed.AddListener(() => refreshes++);

            _shop.BuyBundle(bundle);

            Assert.That(refreshes, Is.EqualTo(1), "Per-item bundle notifications must not rebuild the list again.");
        }

        [Test]
        public void ListView_SelectionChange_StillRefreshes()
        {
            ShopListView list = CreateList(CreateRow("Row A"), CreateRow("Row B"));
            int refreshes = 0;
            list.OnRefreshed.AddListener(() => refreshes++);

            _shop.Select(_hat);

            Assert.That(refreshes, Is.EqualTo(1));
        }

        [Test]
        public void ListView_RepeatedRefreshes_KeepOneButtonListenerPerRow()
        {
            ShopItem row = CreateRow("Row A");
            ShopListView list = CreateList(row);
            list.SetButtonAction(ShopListButtonAction.Select);
            for (int i = 0; i < 5; i++)
            {
                list.Refresh();
            }

            int selections = 0;
            _shop.OnSelectId.AddListener(_ => selections++);

            row.buttonBuy.onClick.Invoke();

            Assert.That(selections, Is.EqualTo(1));
            Assert.That(_shop.EquippedId, Is.EqualTo(_sword.Id));
        }

        [Test]
        public void ListView_WarmRefresh_DoesNotAllocate()
        {
            ShopListView list = CreateList(CreateRow("Row A"), CreateRow("Row B"));
            for (int i = 0; i < 4; i++)
            {
                list.Refresh();
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 100; i++)
            {
                list.Refresh();
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.Zero, "Rebinding unchanged rows must reuse the cached button actions.");
        }

        [Test]
        public void SetItems_LargerCatalog_SpawnsAndBindsShopOwnedViews()
        {
            _money.Add(100f);
            ShopBehaviour shop = NewObject("Spawning shop").AddComponent<ShopBehaviour>();
            SetPrivate(shop, "_keySave", "SpawningShop");
            shop.SetMoneySpendSource(_money.gameObject);
            ShopItem prefab = CreateRow("Template row");
            SetPrivate(shop, "_prefab", prefab);
            SetPrivate(shop, "_shopItems", new ShopItem[0]);
            SetPrivate(shop, "_shopItemDatas", new[] { _sword });
            InvokeLifecycle(shop, "Awake");
            Assert.That(GetShopViews(shop).Length, Is.EqualTo(1));

            shop.SetItems(new[] { _sword, _hat });

            ShopItem[] views = GetShopViews(shop);
            Assert.That(views.Length, Is.EqualTo(2), "A grown catalog gets a view for every entry.");
            for (int i = 0; i < views.Length; i++)
            {
                _objects.Add(views[i].gameObject);
            }

            Assert.That(views[1].BoundItemData, Is.SameAs(_hat));
            int selections = 0;
            shop.OnSelectId.AddListener(_ => selections++);

            views[1].buttonBuy.onClick.Invoke();

            Assert.That(shop.IsOwned(_hat), Is.True);
            Assert.That(selections, Is.EqualTo(1), "Re-subscribing must not stack a second Buy listener.");
            Assert.That(_money.money, Is.EqualTo(80f));
        }

        [Test]
        public void Unsubscribe_AfterButtonReassignment_DetachesTheOriginallySubscribedButton()
        {
            _money.Add(100f);
            ShopBehaviour shop = NewObject("Rewired shop").AddComponent<ShopBehaviour>();
            SetPrivate(shop, "_keySave", "RewiredShop");
            shop.SetMoneySpendSource(_money.gameObject);
            ShopItem row = CreateRow("Rewired row");
            SetPrivate(shop, "_shopItems", new[] { row });
            SetPrivate(shop, "_shopItemDatas", new[] { _sword });
            InvokeLifecycle(shop, "Awake");
            Button original = row.buttonBuy;
            row.buttonBuy = NewObject("Replacement button").AddComponent<Button>();

            InvokeLifecycle(shop, "OnDestroy");
            original.onClick.Invoke();

            Assert.That(shop.IsOwned(_sword), Is.False, "The button subscribed in Awake must lose its Buy listener.");
            Assert.That(_money.money, Is.EqualTo(100f));
        }

        [Test]
        public void GetItemsInCategory_ListOverload_FillsWithoutAllocatingAnArray()
        {
            SetPrivate(_hat, "_category", "Head");
            List<ShopItemData> results = new() { _sword };

            int count = _shop.GetItemsInCategory("Head", results);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(results, Is.EqualTo(new[] { _hat }));
            Assert.That(_shop.GetItemsInCategory("Head"), Is.EqualTo(new[] { _hat }));
            Assert.That(_shop.GetItemsInCategory("none"), Is.Empty);
        }

        [Test]
        public void IdLookups_CatalogEditedInPlace_ResolveTheLiveEntry()
        {
            Assert.That(_shop.GetPrice(_sword.Id), Is.EqualTo(50f));
            Assert.That(_shop.GetPrice(_hat.Id), Is.EqualTo(20f));
            ShopItemData boots = CreateItem("boots", 7);

            _shop.ShopItemDatas[0] = boots;

            Assert.That(_shop.GetPrice(_sword.Id), Is.Zero, "An entry replaced in the live array is no longer resolved.");
            Assert.That(_shop.CanAfford(_sword.Id), Is.False);
            Assert.That(_shop.TryBuy(_sword.Id), Is.EqualTo(ShopPurchaseStatus.Unavailable));
            Assert.That(_shop.GetPrice(boots.Id), Is.EqualTo(7f), "A new entry is found without a SetItems call.");
            Assert.That(_shop.GetPrice(_hat.Id), Is.EqualTo(20f));

            ShopItemData[] reordered = { _hat, boots };
            _shop.SetItems(reordered);
            Assert.That(_shop.GetPrice(_hat.Id), Is.EqualTo(20f));
            Assert.That(_shop.GetPrice(boots.Id), Is.EqualTo(7f));
            Assert.That(_shop.GetPrice("missing"), Is.Zero);
        }

        [Test]
        public void IdLookups_DuplicateIds_ResolveTheFirstEntryLikeTheScan()
        {
            ShopItemData cheap = CreateItem("twin", 5);
            ShopItemData expensive = CreateItem("twin", 500);
            _shop.SetItems(new[] { cheap, expensive });

            Assert.That(_shop.GetPrice("twin"), Is.EqualTo(5f));
            Assert.That(_shop.GetPrice("twin"), Is.EqualTo(5f), "The cached index keeps the first match.");
        }

        [Test]
        public void IdLookups_InPlaceDuplicateOfAnIndexedId_FirstEntryWinsOnceTheSameArrayIsReapplied()
        {
            Assert.That(_shop.GetPrice(_hat.Id), Is.EqualTo(20f), "Indexes the hat in the second slot.");
            ShopItemData hatTwin = CreateItem(_hat.Id, 3);

            _shop.ShopItemDatas[0] = hatTwin;
            _shop.SetItems(_shop.ShopItemDatas, false);

            Assert.That(_shop.GetPrice(_hat.Id), Is.EqualTo(3f),
                "Re-applying the edited array re-indexes it, so the first duplicate wins like the linear scan.");
        }

        [Test]
        public void IdLookups_WarmCatalog_DoNotAllocate()
        {
            _money.Add(30f);
            string swordId = _sword.Id;
            string hatId = _hat.Id;
            _shop.GetPrice(swordId);
            _shop.CanAfford(hatId);
            _shop.ResolveCurrencyMoney(swordId);

            GcAlloc.AssertNone(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    _shop.GetPrice(swordId);
                    _shop.CanAfford(hatId);
                    _shop.IsOwned(swordId);
                    _shop.ResolveCurrencyMoney(swordId);
                }
            }, "Resolving catalog ids for view refreshes must not allocate.");
        }

        [Test]
        public void TextMoney_WithoutWallet_BindsWhenTheWalletRegisters()
        {
            // WHY: the fixture wallet would be the Money.I fallback; this case needs a scene without wallets.
            Object.DestroyImmediate(_money.gameObject);
            TextMoney text = NewObject("Late balance").AddComponent<TextMoney>();
            text.SetMoneySaveKey("LateCoins");
            Enable(text);
            Assume.That(text.BoundMoney, Is.Null, "A stray scene wallet would satisfy the lookup immediately.");

            Money late = CreateMoney("LateCoins");
            late.Add(42f);
            Enable(late);

            Assert.That(text.BoundMoney, Is.SameAs(late));
            Assert.That(text.amount, Is.EqualTo(42f));
            late.Add(8f);
            Assert.That(text.amount, Is.EqualTo(50f));

            InvokeLifecycle(text, "OnDisable");
            late.Add(1f);
            Assert.That(text.amount, Is.EqualTo(50f), "Disabling unsubscribes from the bound property.");
        }

        private ShopListView CreateList(params ShopItem[] rows)
        {
            ShopListView list = NewObject("List").AddComponent<ShopListView>();
            SetPrivate(list, "_bindShopIfNull", false);
            SetPrivate(list, "_views", new List<ShopItem>(rows));
            list.SetShop(_shop);
            Enable(list);
            return list;
        }

        private ShopItem CreateRow(string name)
        {
            GameObject created = NewObject(name);
            ShopItem row = created.AddComponent<ShopItem>();
            row.buttonBuy = created.AddComponent<Button>();
            return row;
        }

        private Money CreateMoney(string key)
        {
            Money wallet = NewObject(key).AddComponent<Money>();
            SetPrivate(wallet, "_persistMoney", false);
            SetPrivate(wallet, "_moneySave", key);
            return wallet;
        }

        private ShopItemData CreateItem(string id, int price)
        {
            ShopItemData item = ScriptableObject.CreateInstance<ShopItemData>();
            _objects.Add(item);
            item.AssignIdIfEmpty(id);
            SetPrivate(item, "_price", price);
            return item;
        }

        private ShopBundleData CreateBundle(string id, int price, params ShopItemData[] items)
        {
            ShopBundleData bundle = ScriptableObject.CreateInstance<ShopBundleData>();
            _objects.Add(bundle);
            SetPrivate(bundle, "_id", id);
            SetPrivate(bundle, "_bundlePrice", price);
            SetPrivate(bundle, "_items", items);
            return bundle;
        }

        private GameObject NewObject(string name)
        {
            GameObject created = new(name);
            _objects.Add(created);
            return created;
        }

        private void Enable(MonoBehaviour component)
        {
            component.enabled = true;
            InvokeLifecycle(component, "OnEnable");
            if (!_enabled.Contains(component))
            {
                _enabled.Add(component);
            }
        }

        private static ShopItem[] GetShopViews(ShopBehaviour shop)
        {
            FieldInfo field = typeof(ShopBehaviour).GetField("_shopItems", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (ShopItem[])field.GetValue(shop);
        }

        private static void SetPrivate(object target, string name, object value)
        {
            Type type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, $"Serialized setup field {name} is missing.");
            field.SetValue(target, value);
        }

        private static void InvokeLifecycle(MonoBehaviour target, string method)
        {
            Type type = target.GetType();
            MethodInfo callback = null;
            while (type != null && callback == null)
            {
                callback = type.GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                type = type.BaseType;
            }

            Assert.That(callback, Is.Not.Null, $"Lifecycle callback {method} is missing.");
            callback.Invoke(target, null);
        }
    }
}
