using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using ShopBehaviour = Neo.Shop.Shop;
using TestVariantView = global::Neo.Editor.Tests.Audit20261023FixtureShopVariantsPanelTestVariantView;

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
        private Audit20261023NeoEditorTestsSaveScope _saveScope;
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

        [SetUp]
        public void SetUp()
        {
            _saveScope = new Audit20261023NeoEditorTestsSaveScope();
            _root = new GameObject("ShopVariantsPanelTests");

            _money = _root.AddComponent<Money>();
            SetPrivate(_money, "_persistMoney", false);

            _shop = _root.AddComponent<ShopBehaviour>();
            _shop.moneySpendSource = _root;
            SetPrivate(_shop, "_keySave", TestSaveKey);
            // WHY: BuyOnly leaves the equipping to the panel.
            SetPrivate(_shop, "_purchaseFlow", ShopPurchaseFlow.BuyOnly);

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
            try
            {
                if (_panel != null) InvokeLifecycle(_panel, "OnDisable");
                if (_listView != null) InvokeLifecycle(_listView, "OnDisable");
                Object.DestroyImmediate(_root);
                Object.DestroyImmediate(_itemA);
                Object.DestroyImmediate(_itemB);
            }
            finally
            {
                _saveScope?.Dispose();
                _saveScope = null;
            }
        }

        private ShopItem CreateSlot(string name, out TestVariantView view)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_root.transform);
            ShopItem slot = go.AddComponent<ShopItem>();
            view = go.AddComponent<TestVariantView>();
            return slot;
        }

        private static ShopItemData CreateItem(string id, int price)
        {
            ShopItemData data = ScriptableObject.CreateInstance<ShopItemData>();
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

        /// <summary>
        ///     Subscribes the list view to the shop as a scene does (Refresh On Shop Events on). The order decides whose
        ///     shop listeners run first, so tests cover both.
        /// </summary>
        private void EnableListViewAndPanel(bool listViewFirst)
        {
            if (listViewFirst)
            {
                InvokeLifecycle(_listView, "OnEnable");
                InvokeLifecycle(_panel, "OnEnable");
            }
            else
            {
                InvokeLifecycle(_panel, "OnEnable");
                InvokeLifecycle(_listView, "OnEnable");
            }
        }

        private static List<ShopVariantState> PushedSince(TestVariantView view, int before)
        {
            return view.States.GetRange(before, view.States.Count - before);
        }

        private EquipmentManager CreateEquipment(params EquipItemDefinition[] definitions)
        {
            GameObject equipmentGo = new GameObject("Equipment");
            equipmentGo.transform.SetParent(_root.transform);
            EquipmentManager equipment = equipmentGo.AddComponent<EquipmentManager>();
            SetPrivate(equipment, "_persist", false);
            SetPrivate(equipment, "_items", definitions);
            return equipment;
        }

        private static EquipItemDefinition CreateDefinition(string id, string categoryId)
        {
            EquipItemDefinition definition = ScriptableObject.CreateInstance<EquipItemDefinition>();
            SetPrivate(definition, "_id", id);
            SetPrivate(definition, "_categoryId", categoryId);
            return definition;
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
        public void Purchase_ThatEquips_RendersEachSlotOncePerChange()
        {
            _money.SetMoney(500f);
            InvokeLifecycle(_panel, "OnEnable");
            int before = _viewA.States.Count;

            _shop.Buy("chair");

            List<ShopVariantState> pushed = _viewA.States.GetRange(before, _viewA.States.Count - before);
            Assert.That(pushed, Is.EqualTo(new[] { ShopVariantState.Owned, ShopVariantState.Equipped }),
                "The purchase and the equip that follows render the slot once each, not once per notification.");
        }

        [Test]
        public void EquipAndUnequip_ThroughEquipmentManager_RenderEachSlotOnce()
        {
            GameObject equipmentGo = new GameObject("Equipment");
            equipmentGo.transform.SetParent(_root.transform);
            EquipmentManager equipment = equipmentGo.AddComponent<EquipmentManager>();
            SetPrivate(equipment, "_persist", false);

            EquipItemDefinition definition = ScriptableObject.CreateInstance<EquipItemDefinition>();
            try
            {
                SetPrivate(definition, "_id", "chair");
                SetPrivate(definition, "_categoryId", "furniture");
                SetPrivate(equipment, "_items", new[] { definition });
                SetPrivate(_panel, "_equipment", equipment);
                SetPrivate(_panel, "_unequipCategoryId", "furniture");
                Profile().TryAddOwnedItem("chair");
                InvokeLifecycle(_panel, "OnEnable");
                ShopVariantState? stateWhenRaised = null;
                _panel.OnVariantEquipped.AddListener(_ => stateWhenRaised = _viewA.Current);

                int beforeEquip = _viewA.States.Count;
                _panel.Equip("chair");

                Assert.That(_viewA.States.Count - beforeEquip, Is.EqualTo(1),
                    "Equipment, selection and shop notifications of one equip render the slot once.");
                Assert.That(_viewA.Current, Is.EqualTo(ShopVariantState.Equipped));
                Assert.That(stateWhenRaised, Is.EqualTo(ShopVariantState.Equipped),
                    "OnVariantEquipped is raised after the slots show the new state.");

                int beforeUnequip = _viewA.States.Count;
                _panel.Unequip();

                Assert.That(_viewA.States.Count - beforeUnequip, Is.EqualTo(1));
                Assert.That(_viewA.Current, Is.EqualTo(ShopVariantState.Owned));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
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
            GameObject equipmentGo = new GameObject("Equipment");
            equipmentGo.transform.SetParent(_root.transform);
            EquipmentManager equipment = equipmentGo.AddComponent<EquipmentManager>();
            SetPrivate(equipment, "_persist", false);

            EquipItemDefinition definition = ScriptableObject.CreateInstance<EquipItemDefinition>();
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

        [TestCase(true)]
        [TestCase(false)]
        public void ShopSelect_WithSubscribedListView_PushesEachChangedSlotOnce(bool listViewFirst)
        {
            Profile().TryAddOwnedItem("chair");
            Profile().TryAddOwnedItem("table");
            Profile().EquippedId = "chair";
            EnableListViewAndPanel(listViewFirst);
            Assert.That(_viewA.Current, Is.EqualTo(ShopVariantState.Equipped));
            int beforeA = _viewA.States.Count;
            int beforeB = _viewB.States.Count;

            _shop.Select("table");

            Assert.That(PushedSince(_viewA, beforeA), Is.EqualTo(new[] { ShopVariantState.Owned }));
            Assert.That(PushedSince(_viewB, beforeB), Is.EqualTo(new[] { ShopVariantState.Equipped }),
                "The list refresh, OnSelectId and the batch end describe one change and push it once.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ShopSelect_ForwardedToEquipment_PushesOnlyTheFinalStateOnce(bool listViewFirst)
        {
            EquipItemDefinition chair = CreateDefinition("chair", "furniture");
            EquipItemDefinition table = CreateDefinition("table", "furniture");
            try
            {
                EquipmentManager equipment = CreateEquipment(chair, table);
                SetPrivate(_panel, "_equipment", equipment);
                Profile().TryAddOwnedItem("chair");
                Profile().TryAddOwnedItem("table");
                Profile().EquippedId = "chair";
                equipment.EquipById("chair");
                EnableListViewAndPanel(listViewFirst);
                int beforeA = _viewA.States.Count;
                int beforeB = _viewB.States.Count;

                _shop.Select("table");

                Assert.That(equipment.IsEquipped("table"), Is.True, "The selection is forwarded.");
                Assert.That(PushedSince(_viewA, beforeA), Is.EqualTo(new[] { ShopVariantState.Owned }));
                Assert.That(PushedSince(_viewB, beforeB), Is.EqualTo(new[] { ShopVariantState.Equipped }),
                    "A list refresh that runs before the selection reaches the EquipmentManager must not push the " +
                    "stale state.");
            }
            finally
            {
                Object.DestroyImmediate(chair);
                Object.DestroyImmediate(table);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PurchaseThatEquips_WithSubscribedListView_PushesOwnedThenEquipped(bool listViewFirst)
        {
            _money.SetMoney(500f);
            EnableListViewAndPanel(listViewFirst);
            int beforeA = _viewA.States.Count;
            int beforeB = _viewB.States.Count;
            int equippedEvents = 0;
            _panel.OnVariantEquipped.AddListener(_ => equippedEvents++);

            _shop.Buy("chair");

            Assert.That(PushedSince(_viewA, beforeA),
                Is.EqualTo(new[] { ShopVariantState.Owned, ShopVariantState.Equipped }),
                "The purchase and the equip are pushed once each, whatever order the listeners run in.");
            Assert.That(PushedSince(_viewB, beforeB), Is.Empty, "A slot whose state did not change is not pushed.");
            Assert.That(equippedEvents, Is.EqualTo(1));
        }

        [Test]
        public void PurchaseThatEquips_ThroughEquipment_WithSubscribedListView_PushesOwnedThenEquipped()
        {
            EquipItemDefinition chair = CreateDefinition("chair", "furniture");
            try
            {
                EquipmentManager equipment = CreateEquipment(chair);
                SetPrivate(_panel, "_equipment", equipment);
                _money.SetMoney(500f);
                EnableListViewAndPanel(true);
                int beforeA = _viewA.States.Count;

                _shop.Buy("chair");

                Assert.That(equipment.IsEquipped("chair"), Is.True);
                Assert.That(PushedSince(_viewA, beforeA),
                    Is.EqualTo(new[] { ShopVariantState.Owned, ShopVariantState.Equipped }));
            }
            finally
            {
                Object.DestroyImmediate(chair);
            }
        }

        [Test]
        public void RefreshStates_RepushesUnchangedSlots()
        {
            InvokeLifecycle(_panel, "OnEnable");
            int beforeA = _viewA.States.Count;

            _panel.RefreshStates();

            Assert.That(PushedSince(_viewA, beforeA), Is.EqualTo(new[] { ShopVariantState.Unowned }),
                "The public call keeps its full-push contract.");
        }

        [Test]
        public void SlotCallbackThatEquips_DoesNotLeaveAStaleStateOnAnyView()
        {
            SetPrivate(_panel, "_equipAfterPurchase", false);
            ShopVariantStateView stateView = _slotA.gameObject.AddComponent<ShopVariantStateView>();
            stateView.OnOwned.AddListener(data => _panel.Equip(data.Id));
            _money.SetMoney(500f);
            InvokeLifecycle(_panel, "OnEnable");
            int beforeA = _viewA.States.Count;

            _shop.Buy("chair");

            Assert.That(_shop.EquippedId, Is.EqualTo("chair"));
            Assert.That(PushedSince(_viewA, beforeA),
                Is.EqualTo(new[] { ShopVariantState.Owned, ShopVariantState.Equipped }));
            Assert.That(stateView.CurrentState, Is.EqualTo(ShopVariantState.Equipped),
                "The render requested from inside a slot callback runs as a follow-up pass once the current one ends.");
        }
    }
}
