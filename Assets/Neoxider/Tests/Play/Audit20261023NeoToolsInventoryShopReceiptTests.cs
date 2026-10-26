using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
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
    public sealed class Audit20261023NeoToolsInventoryShopReceiptTests
    {
        private readonly List<Object> _created = new();
        private GameObject _root;
        private ISaveProvider _previousProvider;
        private bool _previousErrors;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            SaveProvider.SetProvider(new ShopPurchasePlayModeTests.MemorySaveProvider());
            _previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            NeoDiagnostics.Configure(errors: true);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_root != null)
                {
                    Object.DestroyImmediate(_root);
                }

                for (int i = _created.Count - 1; i >= 0; i--)
                {
                    if (_created[i] != null)
                    {
                        Object.DestroyImmediate(_created[i]);
                    }
                }
            }
            finally
            {
                _created.Clear();
                _root = null;
                NeoDiagnostics.Configure(errors: _previousErrors);
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        [UnityTest]
        public IEnumerator ThrowingBridgeOnGranted_ItemRemainsPaidOwnedAndDelivered()
        {
            yield return PurchaseWithThrowingGrantObserver(false);
        }

        [UnityTest]
        public IEnumerator ThrowingBridgeOnGranted_BundleStillDeliversLaterChild()
        {
            yield return PurchaseWithThrowingGrantObserver(true);
        }

        private IEnumerator PurchaseWithThrowingGrantObserver(bool purchaseBundle)
        {
            GameObject root = new("InventoryBridgeReceipt");
            _root = root;
            _created.Add(root);
            root.SetActive(false);
            Money wallet = root.AddComponent<Money>();
            SetField(wallet, "_persistMoney", false);
            wallet.MaxMoney = 100f;
            wallet.AddOverflow(200f);
            InventoryComponent inventory = root.AddComponent<InventoryComponent>();
            SetField(inventory, "_setInstanceOnAwake", false);
            SetField(inventory, "_autoLoad", false);
            SetField(inventory, "_autoSave", false);
            ShopBehaviour shop = root.AddComponent<ShopBehaviour>();
            shop.AutoSpawnItems = false;
            SetField(shop, "_activateSavedEquipped", false);
            shop.SetMoneySpendSource(wallet.gameObject);
            ShopItemData first = NewAsset<ShopItemData>();
            first.AssignIdIfEmpty("inventory-receipt-first");
            SetField(first, "_price", 50);
            ShopItemData second = NewAsset<ShopItemData>();
            second.AssignIdIfEmpty("inventory-receipt-second");
            SetField(second, "_price", 50);
            shop.SetItems(new[] { first, second });
            ShopBundleData bundle = NewAsset<ShopBundleData>();
            SetField(bundle, "_id", "inventory-receipt-bundle");
            SetField(bundle, "_bundlePrice", 50);
            SetField(bundle, "_items", new[] { first, second });
            shop.SetBundles(new[] { bundle });
            InventoryItemData firstGrant = NewAsset<InventoryItemData>();
            SetField(firstGrant, "_itemId", 7301);
            InventoryItemData secondGrant = NewAsset<InventoryItemData>();
            SetField(secondGrant, "_itemId", 7302);
            ShopInventoryGrantBridge bridge = root.AddComponent<ShopInventoryGrantBridge>();
            bridge.SetShop(shop);
            bridge.SetInventory(inventory);
            bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
                { ShopItemId = first.Id, InventoryItem = firstGrant, Amount = 3 });
            bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
                { ShopItemId = second.Id, InventoryItem = secondGrant, Amount = 2 });
            root.SetActive(true);
            yield return null;

            int granted = 0;
            int failures = 0;
            int bundles = 0;
            List<ShopNotificationFault> faults = new();
            bridge.OnGranted.AddListener((item, count) =>
            {
                granted += count;
                if (item == firstGrant)
                {
                    throw new InvalidOperationException("grant observer failed");
                }
            });
            shop.OnPurchaseFailedId.AddListener(_ => failures++);
            shop.OnPurchasedBundle.AddListener(_ => bundles++);
            shop.OnNotificationFault.AddListener(faults.Add);
            string purchaseId = purchaseBundle ? bundle.Id : first.Id;
            LogAssert.Expect(LogType.Error,
                new Regex($@"\[Shop\] Observer 'OnPurchasedId' failed after purchase '{purchaseId}'"));

            if (purchaseBundle)
            {
                shop.BuyBundle(bundle);
            }
            else
            {
                shop.Buy(first);
            }

            Assert.That(wallet.money, Is.EqualTo(150f));
            Assert.That(wallet.allMoney, Is.EqualTo(200f));
            Assert.That(shop.IsOwned(first), Is.True);
            Assert.That(shop.IsOwned(second), Is.EqualTo(purchaseBundle));
            Assert.That(inventory.GetCount(7301), Is.EqualTo(3));
            Assert.That(inventory.GetCount(7302), Is.EqualTo(purchaseBundle ? 2 : 0));
            Assert.That(granted, Is.EqualTo(purchaseBundle ? 5 : 3));
            Assert.That(failures, Is.Zero);
            Assert.That(bundles, Is.EqualTo(purchaseBundle ? 1 : 0));
            ShopProfileData saved = JsonUtility.FromJson<ShopProfileData>(SaveProvider.GetString("Shop"));
            Assert.That(saved.IsItemOwned(first.Id), Is.True);
            Assert.That(saved.IsItemOwned(second.Id), Is.EqualTo(purchaseBundle));
            Assert.That(saved.IsBundleOwned(bundle.Id), Is.EqualTo(purchaseBundle));
            Assert.That(faults.Count, Is.EqualTo(1));
            Assert.That(faults[0].PurchaseId, Is.EqualTo(purchaseId));
            Assert.That(faults[0].ItemId, Is.EqualTo(first.Id));
            Assert.That(faults[0].Message, Is.EqualTo("grant observer failed"));
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
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
    }
}
