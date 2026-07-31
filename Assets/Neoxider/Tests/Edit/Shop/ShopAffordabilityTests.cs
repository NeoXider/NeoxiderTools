using System.Reflection;
using Neo.Shop;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using ShopBehaviour = Neo.Shop.Shop;
using UnityEngine.UI;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the reactive affordability surface: <see cref="Shop.CanAfford(string)"/> (balance
    ///     changes, multi-currency overrides, owned/free items), the <see cref="ButtonPrice"/>
    ///     Unaffordable state, and <see cref="ShopPurchaseButtonView"/> state resolution including
    ///     balance subscription, rebinding, and the enable/disable lifecycle.
    /// </summary>
    [TestFixture]
    public class ShopAffordabilityTests
    {
        private GameObject _shopGo;
        private GameObject _moneyGo;
        private ShopBehaviour _shop;
        private Money _money;
        private ShopItemData _item;

        [SetUp]
        public void SetUp()
        {
            _moneyGo = new GameObject("Money");
            _money = _moneyGo.AddComponent<Money>();
            SetPrivate(_money, "_persistMoney", false);

            _shopGo = new GameObject("Shop");
            _shop = _shopGo.AddComponent<ShopBehaviour>();
            _shop.moneySpendSource = _moneyGo;

            _item = CreateItem("sword", 100, "");
            _shop.SetItems(new[] { _item });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_shopGo);
            Object.DestroyImmediate(_moneyGo);
            Object.DestroyImmediate(_item);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (info == null && target.GetType().BaseType != null)
            {
                info = target.GetType().BaseType.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            }

            Assert.IsNotNull(info, $"{target.GetType().Name}.{field} field expected");
            info.SetValue(target, value);
        }

        private static ShopItemData CreateItem(string id, int price, string currencyKey)
        {
            var data = ScriptableObject.CreateInstance<ShopItemData>();
            SetPrivate(data, "_id", id);
            SetPrivate(data, "_price", price);
            SetPrivate(data, "_currencyOverrideSaveKey", currencyKey);
            SetPrivate(data, "_isSinglePurchase", true);
            return data;
        }

        private ShopProfileData Profile()
        {
            FieldInfo info = typeof(ShopBehaviour).GetField("_profile", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "Shop._profile field expected");
            return (ShopProfileData)info.GetValue(_shop);
        }

        [Test]
        public void CanAfford_UnknownItem_False()
        {
            Assert.IsFalse(_shop.CanAfford("missing"));
            Assert.IsFalse(_shop.CanAfford((ShopItemData)null));
        }

        [Test]
        public void CanAfford_TracksBalanceChanges()
        {
            _money.SetMoney(50f);
            Assert.IsFalse(_shop.CanAfford("sword"));

            _money.Add(50f);
            Assert.IsTrue(_shop.CanAfford("sword"), "Exact balance is affordable.");

            _money.Spend(1f);
            Assert.IsFalse(_shop.CanAfford("sword"));
        }

        [Test]
        public void CanAfford_FreeItem_AlwaysTrue()
        {
            ShopItemData free = CreateItem("hat", 0, "");
            try
            {
                _shop.SetItems(new[] { _item, free });
                _money.SetMoney(0f);

                Assert.IsTrue(_shop.CanAfford("hat"));
            }
            finally
            {
                Object.DestroyImmediate(free);
            }
        }

        [Test]
        public void CanAfford_OwnedItem_TrueRegardlessOfBalance()
        {
            _money.SetMoney(0f);
            Profile().TryAddOwnedItem("sword");

            Assert.IsTrue(_shop.CanAfford("sword"));
        }

        [Test]
        public void CanAfford_RuntimePriceOverride_IsRespected()
        {
            _money.SetMoney(50f);
            Assert.IsFalse(_shop.CanAfford("sword"));

            _shop.SetRuntimePrice("sword", 40f);
            Assert.IsTrue(_shop.CanAfford("sword"), "Discounted price must be re-evaluated.");
            _shop.ClearRuntimePrice("sword");
        }

        [Test]
        public void CanAfford_MultiCurrency_UsesPerItemOverrideWallet()
        {
            var gemsGo = new GameObject("Gems");
            Money gems = gemsGo.AddComponent<Money>();
            ShopItemData gemItem = CreateItem("ring", 10, "Gems");
            try
            {
                SetPrivate(gems, "_persistMoney", false);
                SetPrivate(gems, "_moneySave", "Gems");
                _shop.SetItems(new[] { _item, gemItem });

                _money.SetMoney(1000f); // WHY: default wallet is rich...
                gems.SetMoney(0f); // ...but the item is priced in gems

                Assert.IsFalse(_shop.CanAfford("ring"));

                gems.SetMoney(10f);
                Assert.IsTrue(_shop.CanAfford("ring"));

                Assert.AreSame(gems, _shop.ResolveCurrencyMoney("ring"));
                Assert.AreSame(_money, _shop.ResolveCurrencyMoney("sword"));
            }
            finally
            {
                Object.DestroyImmediate(gemsGo);
                Object.DestroyImmediate(gemItem);
            }
        }

        [Test]
        public void Buy_InsufficientFunds_FiresFailedAndDoesNotOwn()
        {
            _money.SetMoney(10f);
            string failedId = null;
            _shop.OnPurchaseFailedId.AddListener(id => failedId = id);

            _shop.Buy("sword");

            Assert.AreEqual("sword", failedId);
            Assert.IsFalse(_shop.IsOwned("sword"));
            Assert.IsFalse(_shop.CanAfford("sword"));
        }

        [Test]
        public void ButtonPrice_UnaffordableState_IsKeptForPricedItems()
        {
            var go = new GameObject("ButtonPrice");
            try
            {
                ButtonPrice button = go.AddComponent<ButtonPrice>();

                button.SetVisual(100, ButtonPrice.ButtonType.Unaffordable);
                Assert.AreEqual(ButtonPrice.ButtonType.Unaffordable, button.CurrentType);

                button.SetAutoVisual(100, ButtonPrice.ButtonType.Unaffordable);
                Assert.AreEqual(ButtonPrice.ButtonType.Unaffordable, button.CurrentType,
                    "Auto-type must not coerce a priced Unaffordable state back to Buy.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ButtonPrice_UnaffordableFreeItem_DegradesToSelect()
        {
            var go = new GameObject("ButtonPrice");
            try
            {
                ButtonPrice button = go.AddComponent<ButtonPrice>();

                button.SetAutoVisual(0, ButtonPrice.ButtonType.Unaffordable);
                Assert.AreEqual(ButtonPrice.ButtonType.Select, button.CurrentType,
                    "Free items are always affordable.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void InvokeLifecycle(ShopPurchaseButtonView view, string method)
        {
            MethodInfo info = typeof(ShopPurchaseButtonView).GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"ShopPurchaseButtonView.{method} expected");
            info.Invoke(view, null);
        }

        private ShopPurchaseButtonView CreateView(out ShopItem itemView, out Button button)
        {
            var itemGo = new GameObject("ShopItemSlot");
            itemView = itemGo.AddComponent<ShopItem>();
            button = itemGo.AddComponent<Button>();
            itemView.buttonBuy = button;

            ShopPurchaseButtonView view = itemGo.AddComponent<ShopPurchaseButtonView>();
            SetPrivate(view, "_shop", _shop);
            SetPrivate(view, "_item", itemView);
            SetPrivate(view, "_button", button);
            return view;
        }

        [Test]
        public void View_UnaffordableItem_DisablesButton_AndBalanceChangeReenables()
        {
            ShopPurchaseButtonView view = CreateView(out ShopItem itemView, out Button button);
            try
            {
                _money.SetMoney(10f);
                itemView.Visual(_item, 100);

                InvokeLifecycle(view, "OnEnable");

                Assert.AreEqual(ButtonPrice.ButtonType.Unaffordable, view.CurrentState);
                Assert.IsFalse(button.interactable);

                _money.SetMoney(500f); // WHY: balance subscription must refresh the state immediately

                Assert.AreEqual(ButtonPrice.ButtonType.Buy, view.CurrentState);
                Assert.IsTrue(button.interactable);
            }
            finally
            {
                InvokeLifecycle(view, "OnDisable");
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void View_OwnedAndEquippedStates_WinOverAffordability()
        {
            ShopPurchaseButtonView view = CreateView(out ShopItem itemView, out Button button);
            try
            {
                _money.SetMoney(0f);
                itemView.Visual(_item, 100);
                Profile().TryAddOwnedItem("sword");

                InvokeLifecycle(view, "OnEnable");
                Assert.AreEqual(ButtonPrice.ButtonType.Select, view.CurrentState);
                Assert.IsTrue(button.interactable);

                Profile().EquippedId = "sword";
                view.Refresh();
                Assert.AreEqual(ButtonPrice.ButtonType.Selected, view.CurrentState);
            }
            finally
            {
                InvokeLifecycle(view, "OnDisable");
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void View_Rebinding_ResubscribesToItemCurrency()
        {
            var gemsGo = new GameObject("Gems");
            Money gems = gemsGo.AddComponent<Money>();
            ShopItemData gemItem = CreateItem("ring", 10, "Gems");
            ShopPurchaseButtonView view = CreateView(out ShopItem itemView, out Button button);
            try
            {
                SetPrivate(gems, "_persistMoney", false);
                SetPrivate(gems, "_moneySave", "Gems");
                _shop.SetItems(new[] { _item, gemItem });

                _money.SetMoney(1000f);
                gems.SetMoney(0f);

                itemView.Visual(_item, 100);
                InvokeLifecycle(view, "OnEnable");
                Assert.AreEqual(ButtonPrice.ButtonType.Buy, view.CurrentState);

                itemView.Visual(gemItem, 10); // WHY: slot rebinds to the gem-priced item
                _shop.RefreshVisuals(); // WHY: shop refresh notifies the view

                Assert.AreEqual(ButtonPrice.ButtonType.Unaffordable, view.CurrentState);

                gems.SetMoney(50f); // WHY: the NEW wallet's balance must be the subscribed one
                Assert.AreEqual(ButtonPrice.ButtonType.Buy, view.CurrentState);
            }
            finally
            {
                InvokeLifecycle(view, "OnDisable");
                Object.DestroyImmediate(view.gameObject);
                Object.DestroyImmediate(gemsGo);
                Object.DestroyImmediate(gemItem);
            }
        }

        [Test]
        public void View_Disable_StopsReactingToBalance()
        {
            ShopPurchaseButtonView view = CreateView(out ShopItem itemView, out Button button);
            try
            {
                _money.SetMoney(10f);
                itemView.Visual(_item, 100);

                InvokeLifecycle(view, "OnEnable");
                Assert.AreEqual(ButtonPrice.ButtonType.Unaffordable, view.CurrentState);

                InvokeLifecycle(view, "OnDisable");
                _money.SetMoney(500f);

                Assert.AreEqual(ButtonPrice.ButtonType.Unaffordable, view.CurrentState,
                    "Disabled views must unsubscribe from balance changes.");
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }
    }
}
