using System;
using System.Collections.Generic;
using Neo.Shop;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Optional bridge between <see cref="Neo.Shop.Shop"/> and <see cref="InventoryComponent"/>:
    ///     listens to <c>OnPurchasedId</c> / <c>OnPurchasedBundle</c> and grants the configured
    ///     <see cref="InventoryItemData"/> mappings to the resolved inventory.
    ///
    ///     Lives in <c>Neo.Tools.Inventory</c> on purpose — Shop must NOT depend on
    ///     <c>Neo.Tools.Inventory</c> (that would create an asmdef cycle through
    ///     <c>Neo.Tools.View → Neo.Tools.Components → Neo.Shop</c>). Inverting the direction here keeps
    ///     <see cref="Shop"/> currency-only and lets users opt into the inventory link by simply dropping
    ///     this component on the same GameObject (or any object referencing the Shop).
    /// </summary>
    [AddComponentMenu("Neoxider/Tools/Inventory/" + nameof(ShopInventoryGrantBridge))]
    public sealed class ShopInventoryGrantBridge : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("Source Shop. When null, the first Shop found in parents/scene is used at Awake.")]
        [SerializeField]
        private Shop.Shop _shop;

        [Tooltip("Target inventory. When null, falls back to InventoryComponent.Instance singleton if Use Inventory Singleton is on.")]
        [SerializeField]
        private InventoryComponent _inventory;

        [Tooltip("If true and Inventory is null, fall back to InventoryComponent.Instance singleton.")]
        [SerializeField]
        private bool _useInventorySingleton = true;

        [Header("Mappings (shop item id → inventory item)")]
        [Tooltip(
            "Per-item grants. When a ShopItemData with matching Id is purchased (directly or through a bundle), this inventory item is added.")]
        [SerializeField]
        private List<GrantMapping> _mappings = new();

        [Header("Events")]
        public ShopInventoryGrantEvent OnGranted = new();

        private bool _subscribed;

        /// <summary>Currently resolved Shop (explicit field, then scene/parent lookup).</summary>
        public Shop.Shop Shop => _shop;

        /// <summary>Currently resolved Inventory (explicit field, then singleton fallback).</summary>
        public InventoryComponent Inventory => ResolveInventory();

        /// <summary>Mutable list of mappings — exposed for runtime additions (e.g. from DLC unlocks).</summary>
        public List<GrantMapping> Mappings => _mappings;

        private void Awake()
        {
            if (_shop == null)
            {
                _shop = GetComponentInParent<Shop.Shop>();
            }

            Subscribe(true);
        }

        private void OnDestroy()
        {
            Subscribe(false);
        }

        private void Subscribe(bool subscribe)
        {
            if (_shop == null)
            {
                return;
            }

            if (subscribe && !_subscribed)
            {
                _shop.OnPurchasedId?.AddListener(HandlePurchasedId);
                _subscribed = true;
            }
            else if (!subscribe && _subscribed)
            {
                _shop.OnPurchasedId?.RemoveListener(HandlePurchasedId);
                _subscribed = false;
            }
        }

        private void HandlePurchasedId(string itemId)
        {
            GrantForShopItemId(itemId);
        }

        /// <summary>
        ///     Grants the inventory mapping for <paramref name="shopItemId"/> if one is configured.
        ///     NoCode-friendly: can be wired to any UnityEvent that produces a string id.
        /// </summary>
        public void GrantForShopItemId(string shopItemId)
        {
            if (string.IsNullOrEmpty(shopItemId))
            {
                return;
            }

            for (int i = 0; i < _mappings.Count; i++)
            {
                GrantMapping mapping = _mappings[i];
                if (mapping.ShopItemId == shopItemId && mapping.InventoryItem != null)
                {
                    GrantDirect(mapping.InventoryItem, mapping.Amount);
                }
            }
        }

        /// <summary>
        ///     Direct grant — for code paths that already hold an <see cref="InventoryItemData"/>.
        ///     Returns the amount actually added by the inventory (0 when no inventory is resolved or
        ///     when the inventory rejected the grant via limits).
        /// </summary>
        public int GrantDirect(InventoryItemData itemData, int amount)
        {
            if (itemData == null || amount <= 0)
            {
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return 0;
            }

            int added = inv.AddItemData(itemData, amount);
            if (added > 0)
            {
                OnGranted?.Invoke(itemData, added);
            }

            return added;
        }

        /// <summary>Programmatically swaps the inventory used for grants.</summary>
        public void SetInventory(InventoryComponent inventory)
        {
            _inventory = inventory;
            if (inventory != null)
            {
                _useInventorySingleton = false;
            }
        }

        /// <summary>Programmatically swaps the source Shop (re-subscribes events).</summary>
        public void SetShop(Shop.Shop shop)
        {
            if (_shop == shop)
            {
                return;
            }

            Subscribe(false);
            _shop = shop;
            Subscribe(true);
        }

        private InventoryComponent ResolveInventory()
        {
            if (_inventory != null)
            {
                return _inventory;
            }

            if (_useInventorySingleton && InventoryComponent.HasInstance)
            {
                return InventoryComponent.Instance;
            }

            return null;
        }

        /// <summary>
        ///     Inspector-configurable mapping: when the Shop fires <c>OnPurchasedId(shopItemId)</c>,
        ///     the bridge grants <see cref="Amount"/> of <see cref="InventoryItem"/> to the resolved
        ///     inventory.
        /// </summary>
        [Serializable]
        public struct GrantMapping
        {
            [Tooltip("Shop item id (from ShopItemData.Id). Match is case-sensitive.")]
            public string ShopItemId;

            [Tooltip("Inventory item to add.")]
            public InventoryItemData InventoryItem;

            [Tooltip("Amount granted per purchase (minimum 1 at runtime).")]
            public int Amount;
        }
    }
}
