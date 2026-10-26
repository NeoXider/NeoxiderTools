using System.Collections.Generic;
using Neo.Shop;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Tests.Play
{
    [TestFixture]
    public sealed class Audit20261023NeoShopBridgesTests
    {
        private readonly List<Object> _objects = new();
        private InventoryComponent _inventory;
        private InventoryItemData _item;

        [SetUp]
        public void SetUp()
        {
            _item = ScriptableObject.CreateInstance<InventoryItemData>();
            _objects.Add(_item);
            JsonUtility.FromJsonOverwrite("{\"_itemId\":42}", _item);

            _inventory = CreateObject("Audit bridge inventory").AddComponent<InventoryComponent>();
            JsonUtility.FromJsonOverwrite(
                "{\"_setInstanceOnAwake\":false,\"_autoLoad\":false,\"_autoSave\":false}", _inventory);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void SetShop_AfterSourceDestroyed_GrantsFromReplacementAndDetachesRetainedEvent()
        {
            ShopBehaviour original = CreateShop();
            ShopInventoryGrantBridge bridge = CreateBridge(original);
            ShopStringEvent retainedEvent = original.OnPurchasedId;
            retainedEvent.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(3));

            Object.DestroyImmediate(original.gameObject);
            ShopBehaviour replacement = CreateShop();
            bridge.SetShop(replacement);
            replacement.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(6), "The replacement must be subscribed.");

            retainedEvent.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(6), "The previous event must be detached.");
        }

        [Test]
        public void SetShop_NullAfterSourceDestroyed_DetachesRetainedEvent()
        {
            ShopBehaviour original = CreateShop();
            ShopInventoryGrantBridge bridge = CreateBridge(original);
            ShopStringEvent retainedEvent = original.OnPurchasedId;
            Object.DestroyImmediate(original.gameObject);

            bridge.SetShop(null);
            retainedEvent.Invoke("potion");

            Assert.That(_inventory.GetCount(42), Is.Zero);
            Assert.That(ReferenceEquals(bridge.Shop, null), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisabledBridge_IgnoresPurchasesAndRebindsOnceWhenEnabled(bool deactivateObject)
        {
            ShopBehaviour original = CreateShop();
            ShopInventoryGrantBridge bridge = CreateBridge(original);
            original.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(3));

            SetBridgeActive(bridge, false, deactivateObject);
            original.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(3), "Disabled bridges must not grant purchases.");

            ShopBehaviour replacement = CreateShop();
            bridge.SetShop(replacement);
            replacement.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(3), "SetShop must respect the disabled state.");

            SetBridgeActive(bridge, true, deactivateObject);
            SetBridgeActive(bridge, false, deactivateObject);
            SetBridgeActive(bridge, true, deactivateObject);
            original.OnPurchasedId.Invoke("potion");
            replacement.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(6), "Only one listener on the new source may grant.");
        }

        [Test]
        public void DisabledDuringPurchaseNotification_DoesNotGrantTheInFlightEvent()
        {
            ShopBehaviour shop = CreateShop();
            ShopInventoryGrantBridge bridge = null;
            shop.OnPurchasedId.AddListener((string itemId) => bridge.enabled = false);
            bridge = CreateBridge(shop);

            shop.OnPurchasedId.Invoke("potion");

            Assert.That(bridge.enabled, Is.False);
            Assert.That(_inventory.GetCount(42), Is.Zero);
        }

        [Test]
        public void InitiallyDisabledBridge_DoesNotSubscribeUntilEnabled()
        {
            ShopBehaviour shop = CreateShop();
            ShopInventoryGrantBridge bridge = CreateBridge(shop, false);
            shop.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.Zero);

            bridge.enabled = true;
            shop.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(3));
        }

        [Test]
        public void DisablingAfterShopEventReplaced_DetachesTheOriginalEvent()
        {
            ShopBehaviour shop = CreateShop();
            ShopInventoryGrantBridge bridge = CreateBridge(shop);
            ShopStringEvent retainedEvent = shop.OnPurchasedId;
            shop.OnPurchasedId = new ShopStringEvent();
            bridge.enabled = false;

            retainedEvent.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.Zero);

            bridge.enabled = true;
            shop.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(3));
        }

        [Test]
        public void Awake_WithoutParentShop_UsesSceneFallbackAndGrants()
        {
            ShopBehaviour shop = CreateShop();
            shop.gameObject.SetActive(true);
            ShopInventoryGrantBridge bridge = CreateBridge(null);

            Assert.That(bridge.Shop, Is.SameAs(shop));
            shop.OnPurchasedId.Invoke("potion");
            Assert.That(_inventory.GetCount(42), Is.EqualTo(3));
        }

        [Test]
        public void GrantDirect_OnDisabledBridge_ReportsActualCapacityAndIgnoresInvalidRequests()
        {
            JsonUtility.FromJsonOverwrite("{\"_maxTotalItems\":2}", _inventory);
            ShopInventoryGrantBridge bridge = CreateBridge(CreateShop());
            bridge.enabled = false;
            int notifications = 0;
            int notifiedAmount = 0;
            InventoryItemData notifiedItem = null;
            bridge.OnGranted.AddListener((InventoryItemData item, int amount) =>
            {
                notifications++;
                notifiedAmount = amount;
                notifiedItem = item;
            });

            bridge.GrantForShopItemId(null);
            bridge.GrantForShopItemId("");
            bridge.GrantForShopItemId("POTION");
            Assert.That(_inventory.GetCount(42), Is.Zero);
            Assert.That(notifications, Is.Zero);
            Assert.That(bridge.GrantDirect(_item, 5), Is.EqualTo(2));
            Assert.That(_inventory.GetCount(42), Is.EqualTo(2));
            Assert.That(notifiedItem, Is.SameAs(_item));
            Assert.That(notifiedAmount, Is.EqualTo(2));
            Assert.That(bridge.GrantDirect(_item, 1), Is.Zero);
            Assert.That(bridge.GrantDirect(_item, 0), Is.Zero);
            Assert.That(bridge.GrantDirect(_item, -1), Is.Zero);
            Assert.That(bridge.GrantDirect(null, 1), Is.Zero);
            Assert.That(_inventory.GetCount(42), Is.EqualTo(2));
            Assert.That(notifications, Is.EqualTo(1));
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            gameObject.SetActive(false);
            _objects.Add(gameObject);
            return gameObject;
        }

        private ShopBehaviour CreateShop()
        {
            ShopBehaviour shop = CreateObject("Audit bridge shop").AddComponent<ShopBehaviour>();
            shop.AutoSpawnItems = false;
            return shop;
        }

        private ShopInventoryGrantBridge CreateBridge(ShopBehaviour shop, bool enabled = true)
        {
            ShopInventoryGrantBridge bridge = CreateObject("Audit bridge").AddComponent<ShopInventoryGrantBridge>();
            bridge.enabled = enabled;
            bridge.SetInventory(_inventory);
            bridge.SetShop(shop);
            bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
            {
                ShopItemId = "potion",
                InventoryItem = _item,
                Amount = 3
            });
            bridge.gameObject.SetActive(true);
            return bridge;
        }

        private static void SetBridgeActive(ShopInventoryGrantBridge bridge, bool active, bool deactivateObject)
        {
            if (deactivateObject)
            {
                bridge.gameObject.SetActive(active);
            }
            else
            {
                bridge.enabled = active;
            }
        }
    }
}
