using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Shop
{
    /// <summary>Ownership/equipment state of one variant slot, as rendered by <see cref="IShopVariantView"/>.</summary>
    public enum ShopVariantState
    {
        Unowned = 0,
        Owned = 1,
        Equipped = 2
    }

    /// <summary>
    ///     Small view contract for variant slots: implement it on any component under a
    ///     <see cref="ShopItem"/> view to receive state pushes from <see cref="ShopVariantsPanel"/>.
    ///     Projects supply their own sprites, badges, preview dimming, lock/check icons, captions, and
    ///     layout — the panel only reports the state. <see cref="ShopVariantStateView"/> is the
    ///     ready-made GameObject-toggling implementation.
    /// </summary>
    public interface IShopVariantView
    {
        void ApplyVariantState(ShopVariantState state, ShopItemData data);
    }

    /// <summary>
    ///     Furniture/equipment variants panel over the existing Shop views: pairs a
    ///     <see cref="ShopListView"/> (item views, category filtering, button wiring) with an optional
    ///     <see cref="EquipmentManager"/> and keeps every visible slot rendered as unowned / owned /
    ///     equipped through <see cref="IShopVariantView"/>. Buying an unowned variant equips it after a
    ///     successful purchase (optional), and the panel refreshes automatically after ownership,
    ///     selection, equipment, and list changes. An optional empty/unequip control calls
    ///     <see cref="Unequip"/>.
    ///     Item ids are shared between <see cref="ShopItemData.Id"/> and
    ///     <see cref="EquipItemDefinition"/> ids when an EquipmentManager is used.
    /// </summary>
    [NeoDoc("Shop/ShopVariantsPanel.md")]
    [CreateFromMenu("Neoxider/Shop/ShopVariantsPanel")]
    [AddComponentMenu("Neoxider/Shop/" + nameof(ShopVariantsPanel))]
    public sealed class ShopVariantsPanel : MonoBehaviour
    {
        [Tooltip("List view that owns the variant slots; auto-resolved from this object or parents.")]
        [SerializeField]
        private ShopListView _listView;

        [Tooltip("Optional equipment manager; when empty, equipping falls back to Shop.Select.")]
        [SerializeField]
        private EquipmentManager _equipment;

        [Header("Behaviour")]
        [Tooltip("Equip an item right after its successful purchase.")]
        [SerializeField]
        private bool _equipAfterPurchase = true;

        [Tooltip("Forward Shop selection (Select/BuyAndEquip flows) into the EquipmentManager.")]
        [SerializeField]
        private bool _forwardSelectionToEquipment = true;

        [Tooltip("Category cleared by Unequip when an EquipmentManager is assigned.")] [SerializeField]
        private string _unequipCategoryId = "";

        [Space] public UnityEvent<string> OnVariantEquipped = new();
        public UnityEvent OnUnequipped = new();

        private readonly List<IShopVariantView> _viewBuffer = new();

        /// <summary>List view the panel renders (may be null before resolution).</summary>
        public ShopListView ListView => _listView;

        private Shop ShopRef => _listView != null ? _listView.Shop : null;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_listView != null)
            {
                _listView.OnRefreshed.AddListener(RefreshStates);
            }

            Shop shop = ShopRef;
            if (shop != null)
            {
                shop.OnPurchasedId.AddListener(HandlePurchased);
                shop.OnSelectId.AddListener(HandleSelected);
                shop.OnShopChanged.AddListener(RefreshStates);
            }

            if (_equipment != null)
            {
                _equipment.OnEquipChanged.AddListener(HandleEquipChanged);
            }

            RefreshStates();
        }

        private void OnDisable()
        {
            if (_listView != null)
            {
                _listView.OnRefreshed.RemoveListener(RefreshStates);
            }

            Shop shop = ShopRef;
            if (shop != null)
            {
                shop.OnPurchasedId.RemoveListener(HandlePurchased);
                shop.OnSelectId.RemoveListener(HandleSelected);
                shop.OnShopChanged.RemoveListener(RefreshStates);
            }

            if (_equipment != null)
            {
                _equipment.OnEquipChanged.RemoveListener(HandleEquipChanged);
            }
        }

        /// <summary>Equips the item (EquipmentManager when assigned, otherwise Shop selection).</summary>
        public void Equip(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            if (_equipment != null)
            {
                _equipment.EquipById(itemId);
            }

            Shop shop = ShopRef;
            if (shop != null && !string.Equals(shop.EquippedId, itemId, StringComparison.Ordinal))
            {
                shop.Select(itemId);
            }

            OnVariantEquipped?.Invoke(itemId);
            RefreshStates();
        }

        /// <summary>
        ///     Empty/none state: clears the equipment category (when an EquipmentManager is assigned)
        ///     and the Shop selection. Wire an optional "empty slot" button here.
        /// </summary>
        public void Unequip()
        {
            if (_equipment != null)
            {
                _equipment.Unequip(_unequipCategoryId);
            }

            Shop shop = ShopRef;
            if (shop != null && !string.IsNullOrEmpty(shop.EquippedId))
            {
                shop.Select("");
            }

            OnUnequipped?.Invoke();
            RefreshStates();
        }

        /// <summary>Current state of one item id, using the same rules the slot rendering uses.</summary>
        public ShopVariantState GetState(string itemId)
        {
            Shop shop = ShopRef;
            if (shop == null || string.IsNullOrEmpty(itemId))
            {
                return ShopVariantState.Unowned;
            }

            if (IsEquipped(shop, itemId))
            {
                return ShopVariantState.Equipped;
            }

            return shop.IsOwned(itemId) || shop.GetPrice(itemId) <= 0f
                ? ShopVariantState.Owned
                : ShopVariantState.Unowned;
        }

        /// <summary>Pushes the current state of every visible slot to its <see cref="IShopVariantView"/> components.</summary>
        public void RefreshStates()
        {
            if (_listView == null)
            {
                return;
            }

            IReadOnlyList<ShopItem> views = _listView.Views;
            for (int i = 0; i < views.Count; i++)
            {
                ShopItem view = views[i];
                if (view == null || view.BoundItemData == null)
                {
                    continue;
                }

                ShopVariantState state = GetState(view.BoundItemData.Id);

                view.GetComponentsInChildren(true, _viewBuffer);
                for (int v = 0; v < _viewBuffer.Count; v++)
                {
                    _viewBuffer[v]?.ApplyVariantState(state, view.BoundItemData);
                }
            }
        }

        private bool IsEquipped(Shop shop, string itemId)
        {
            if (_equipment != null)
            {
                return _equipment.IsEquipped(itemId);
            }

            return string.Equals(shop.EquippedId, itemId, StringComparison.Ordinal);
        }

        private void HandlePurchased(string itemId)
        {
            if (_equipAfterPurchase)
            {
                Equip(itemId);
                return;
            }

            RefreshStates();
        }

        private void HandleSelected(string itemId)
        {
            if (_equipment != null && _forwardSelectionToEquipment && !string.IsNullOrEmpty(itemId) &&
                !_equipment.IsEquipped(itemId))
            {
                _equipment.EquipById(itemId);
            }

            RefreshStates();
        }

        private void HandleEquipChanged(string categoryId, string itemId)
        {
            RefreshStates();
        }

        private void ResolveReferences()
        {
            if (_listView == null)
            {
                _listView = GetComponentInParent<ShopListView>();
                if (_listView == null)
                {
                    _listView = GetComponentInChildren<ShopListView>(true);
                }
            }

            if (_equipment == null)
            {
                _equipment = GetComponentInParent<EquipmentManager>();
            }
        }
    }
}
