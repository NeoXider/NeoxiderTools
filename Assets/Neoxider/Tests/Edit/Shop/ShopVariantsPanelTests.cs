using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the furniture/equipment variants panel: unowned/owned/equipped state rendering
    ///     through <see cref="IShopVariantView"/>, buy-then-equip after a successful purchase,
    ///     refresh on ownership/equipment changes, the EquipmentManager bridge, and Unequip.
    /// </summary>
    [TestFixture]
    public class ShopVariantsPanelTests
    {
        private const string TestSaveKey = "Test_ShopVariantsPanel";

        private GameObject _root;
        private ShopBehaviour _shop;
        private Money _money;
        private ShopListView _listView;
        private ShopVariantsPanel _panel;
        private ShopItem _slotA;
        private ShopItem _slotB;
        private TestVariantView _viewA;
        private TestVariantView _viewB;
        private ShopItemData _itemA;
        private ShopItemData _itemB;

        /// <summary>Records the states the panel pushes.</summary>
        private sealed class TestVariantView : MonoBehaviour, IShopVariantView
        {
            public readonly List<ShopVariantState> States = new();
            public ShopVariantState Current => States.Count > 0 ? States[^1] : ShopVariantState.Unowned;

            public void ApplyVariantState(ShopVariantState state, ShopItemData data)
            {
                States.Add(state);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ShopVariantsPanelTests");

            _money = _root.AddComponent<Money>();
            SetPrivate(_money, "_persistMoney", false);

            _shop = _root.AddComponent<ShopBehaviour>();
            _shop.moneySpendSource = _root;
            SetPrivate(_shop, "_keySave", TestSaveKey);
            SetPrivate(_shop, "_purchaseFlow", ShopPurchaseFlow.BuyOnly); // panel must do the equipping

            _itemA = CreateItem("chair", 100);
            _itemB = CreateItem("table", 100);
            _shop.SetItems(new[] { _itemA, _itemB });

            _listView = _root.AddComponent<ShopListView>();
            SetPrivate(_listView, "_shop", _shop);

            _slotA = CreateSlot("SlotA", out _viewA);
            _slotB = CreateSlot("SlotB", out _viewB);
            SetPrivate(_listView, "_views", new List<ShopItem> { _slotA, _slotB });
            _slotA.Visual(_itemA, 100);
            _slotB.Visual(_itemB, 100);

            _panel = _root.AddComponent<ShopVariantsPanel>();
            SetPrivate(_panel, "_listView", _listView);
        }

        [TearDown]
        public void TearDown()
        {
            InvokeLifecycle(_panel, "OnDisable");
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_itemA);
            Object.DestroyImmediate(_itemB);
            SaveProvider.DeleteKey(TestSaveKey);
        }

        private ShopItem CreateSlot(string name, out TestVariantView view)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform);
            ShopItem slot = go.AddComponent<ShopItem>();
            view = go.AddComponent<TestVariantView>();
            return slot;
        }

        private static ShopItemData CreateItem(string id, int price)
        {
            var data = ScriptableObject.CreateInstance<ShopItemData>();
            SetPrivate(data, "_id", id);
            SetPrivate(data, "_price", price);
            SetPrivate(data, "_isSinglePurchase", true);
            return data;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{field} field expected");
            info.SetValue(target, value);
        }

        private static void InvokeLifecycle(Component component, string method)
        {
            MethodInfo info = component.GetType().GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"{component.GetType().Name}.{method} expected");
            info.Invoke(component, null);
        }

        private ShopProfileData Profile()
        {
            FieldInfo info = typeof(ShopBehaviour).GetField("_profile", BindingFlags.Instance | BindingFlags.NonPublic);
            return (ShopProfileData)info.GetValue(_shop);
        }

        [Test]
        public void RefreshStates_RendersUnownedOwnedEquipped()
        {
            Profile().TryAddOwnedItem("table");
            Profile().EquippedId = "table";

            _panel.RefreshStates();

            Assert.AreEqual(ShopVariantState.Unowned, _viewA.Current);
            Assert.AreEqual(ShopVariantState.Equipped, _viewB.Current);

            Profile().EquippedId = "";
            _panel.RefreshStates();
            Assert.AreEqual(ShopVariantState.Owned, _viewB.Current, "Owned but not equipped.");
        }

        [Test]
        public void Purchase_EquipsAfterSuccessfulBuy()
        {
            _money.SetMoney(500f);
            InvokeLifecycle(_panel, "OnEnable");

            _shop.Buy("chair");

            Assert.IsTrue(_shop.IsOwned("chair"));
            Assert.AreEqual("chair", _shop.EquippedId,
                "BuyOnly flow does not equip — the panel must equip after the purchase.");
            Assert.AreEqual(ShopVariantState.Equipped, _viewA.Current);
        }

        [Test]
        public void FailedPurchase_DoesNotEquip()
        {
            _money.SetMoney(1f);
            InvokeLifecycle(_panel, "OnEnable");

            _shop.Buy("chair");

            Assert.IsFalse(_shop.IsOwned("chair"));
            Assert.AreEqual("", _shop.EquippedId);
            Assert.AreEqual(ShopVariantState.Unowned, _viewA.Current);
        }

        [Test]
        public void Unequip_ClearsSelection_AndRendersOwned()
        {
            Profile().TryAddOwnedItem("chair");
            Profile().EquippedId = "chair";
            InvokeLifecycle(_panel, "OnEnable");
            Assert.AreEqual(ShopVariantState.Equipped, _viewA.Current);

            bool unequippedRaised = false;
            _panel.OnUnequipped.AddListener(() => unequippedRaised = true);

            _panel.Unequip();

            Assert.IsTrue(unequippedRaised);
            Assert.AreEqual("", _shop.EquippedId);
            Assert.AreEqual(ShopVariantState.Owned, _viewA.Current);
        }

        [Test]
        public void EquipmentManagerBridge_EquipsAndReportsState()
        {
            var equipmentGo = new GameObject("Equipment");
            equipmentGo.transform.SetParent(_root.transform);
            EquipmentManager equipment = equipmentGo.AddComponent<EquipmentManager>();
            SetPrivate(equipment, "_persist", false);

            var definition = ScriptableObject.CreateInstance<EquipItemDefinition>();
            try
            {
                SetPrivate(definition, "_id", "chair");
                SetPrivate(definition, "_categoryId", "furniture");
                SetPrivate(equipment, "_items", new[] { definition });
                SetPrivate(_panel, "_equipment", equipment);
                SetPrivate(_panel, "_unequipCategoryId", "furniture");

                Profile().TryAddOwnedItem("chair");
                InvokeLifecycle(_panel, "OnEnable");

                _panel.Equip("chair");
                Assert.IsTrue(equipment.IsEquipped("chair"));
                Assert.AreEqual(ShopVariantState.Equipped, _viewA.Current);

                _panel.Unequip();
                Assert.IsFalse(equipment.IsEquipped("chair"));
                Assert.AreEqual(ShopVariantState.Owned, _viewA.Current);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
