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

        // WHY: bounds the re-renders requested by IShopVariantView callbacks (for example an OnOwned listener that
        // calls Equip) so a callback that keeps changing the state cannot loop forever.
        private const int MaxRenderPasses = 4;

        private readonly List<IShopVariantView> _viewBuffer = new();
        private readonly List<PushedSlot> _pushedSlots = new();
        private Shop _subscribedShop;
        private int _refreshBatchDepth;
        private bool _rendering;
        private bool _renderRequested;
        private bool _pushAllRequested;

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
                _listView.OnRefreshed.AddListener(HandleStateNotification);
            }

            UpdateShopSubscription(ShopRef);

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
                _listView.OnRefreshed.RemoveListener(HandleStateNotification);
            }

            UpdateShopSubscription(null);

            if (_equipment != null)
            {
                _equipment.OnEquipChanged.RemoveListener(HandleEquipChanged);
            }
        }

        /// <summary>
        ///     Equips the item (EquipmentManager when assigned, otherwise Shop selection). Slots whose state changed are
        ///     pushed once, after both changes and before <see cref="OnVariantEquipped" />.
        /// </summary>
        public void Equip(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            BeginRefreshBatch();
            try
            {
                if (_equipment != null)
                {
                    _equipment.EquipById(itemId);
                }

                Shop shop = ShopRef;
                if (shop != null && !string.Equals(shop.EquippedId, itemId, StringComparison.Ordinal))
                {
                    shop.Select(itemId);
                }
            }
            finally
            {
                EndRefreshBatch();
            }

            OnVariantEquipped?.Invoke(itemId);
        }

        /// <summary>
        ///     Empty/none state: clears the equipment category (when an EquipmentManager is assigned)
        ///     and the Shop selection. Wire an optional "empty slot" button here. Slots whose state changed are pushed
        ///     once, before <see cref="OnUnequipped" />.
        /// </summary>
        public void Unequip()
        {
            BeginRefreshBatch();
            try
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
            }
            finally
            {
                EndRefreshBatch();
            }

            OnUnequipped?.Invoke();
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

        /// <summary>
        ///     Pushes the current state of every visible slot to its <see cref="IShopVariantView"/> components, including
        ///     slots whose state did not change. Called while <see cref="Equip" />, <see cref="Unequip" />, a forwarded
        ///     selection or a slot callback is still applying its changes, it is deferred to one push when that change
        ///     completes. Shop, list and equipment notifications do not use this full push: they push only the slots
        ///     whose state or bound item changed.
        /// </summary>
        public void RefreshStates()
        {
            RequestRender(true);
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

            RequestRender(false);
        }

        private void HandleSelected(string itemId)
        {
            BeginRefreshBatch();
            try
            {
                if (_equipment != null && _forwardSelectionToEquipment && !string.IsNullOrEmpty(itemId) &&
                    !_equipment.IsEquipped(itemId))
                {
                    _equipment.EquipById(itemId);
                }
            }
            finally
            {
                EndRefreshBatch();
            }
        }

        private void HandleEquipChanged(string categoryId, string itemId)
        {
            RequestRender(false);
        }

        // WHY: one shop change reaches the panel several times (Shop.OnShopChanged, OnSelectId, the ShopListView
        // refresh it triggers, the forwarded equipment change), in an order set by subscription order. Pushing only
        // the slots that changed renders each change once in every order, and a render that runs before the
        // selection reaches the EquipmentManager pushes nothing instead of a stale state.
        private void HandleStateNotification()
        {
            RequestRender(false);
        }

        private void BeginRefreshBatch()
        {
            _refreshBatchDepth++;
        }

        /// <summary>Closes one batch level; closing the outermost level renders the changed slots once.</summary>
        private void EndRefreshBatch()
        {
            _refreshBatchDepth--;
            if (_refreshBatchDepth > 0)
            {
                return;
            }

            RequestRender(false);
        }

        private void RequestRender(bool pushAll)
        {
            _renderRequested = true;
            _pushAllRequested |= pushAll;
            // WHY: a render requested from an IShopVariantView callback runs after the current pass instead of
            // inside it; a nested pass would refill _viewBuffer while the outer loop still iterates it.
            if (_refreshBatchDepth > 0 || _rendering)
            {
                return;
            }

            _rendering = true;
            try
            {
                for (int pass = 0; pass < MaxRenderPasses && _renderRequested; pass++)
                {
                    bool pushAllSlots = _pushAllRequested;
                    _renderRequested = false;
                    _pushAllRequested = false;
                    PushSlotStates(pushAllSlots);
                }
            }
            finally
            {
                _rendering = false;
                _renderRequested = false;
                _pushAllRequested = false;
            }
        }

        private void PushSlotStates(bool pushAll)
        {
            if (isActiveAndEnabled)
            {
                UpdateShopSubscription(ShopRef);
            }

            if (_listView == null)
            {
                return;
            }

            IReadOnlyList<ShopItem> views = _listView.Views;
            for (int i = 0; i < views.Count; i++)
            {
                ShopItem view = views[i];
                ShopItemData data = view != null ? view.BoundItemData : null;
                if (data == null)
                {
                    continue;
                }

                ShopVariantState state = GetState(data.Id);
                if (!pushAll && WasPushed(i, view, data, state))
                {
                    continue;
                }

                view.GetComponentsInChildren(true, _viewBuffer);
                for (int v = 0; v < _viewBuffer.Count; v++)
                {
                    _viewBuffer[v]?.ApplyVariantState(state, data);
                }

                StorePushed(i, view, data, state);
            }
        }

        private bool WasPushed(int slot, ShopItem view, ShopItemData data, ShopVariantState state)
        {
            if (slot >= _pushedSlots.Count)
            {
                return false;
            }

            PushedSlot pushed = _pushedSlots[slot];
            return pushed.State == state && ReferenceEquals(pushed.View, view) && ReferenceEquals(pushed.Data, data);
        }

        private void StorePushed(int slot, ShopItem view, ShopItemData data, ShopVariantState state)
        {
            while (_pushedSlots.Count <= slot)
            {
                _pushedSlots.Add(default);
            }

            _pushedSlots[slot] = new PushedSlot(view, data, state);
        }

        private void UpdateShopSubscription(Shop shop)
        {
            if (_subscribedShop == shop)
            {
                return;
            }

            if (_subscribedShop != null)
            {
                _subscribedShop.OnPurchasedId.RemoveListener(HandlePurchased);
                _subscribedShop.OnSelectId.RemoveListener(HandleSelected);
                _subscribedShop.OnShopChanged.RemoveListener(HandleStateNotification);
            }

            _subscribedShop = shop;
            if (_subscribedShop != null)
            {
                _subscribedShop.OnPurchasedId.AddListener(HandlePurchased);
                _subscribedShop.OnSelectId.AddListener(HandleSelected);
                _subscribedShop.OnShopChanged.AddListener(HandleStateNotification);
            }
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

        /// <summary>Last state pushed to one slot, used to skip notifications that change nothing.</summary>
        private readonly struct PushedSlot
        {
            public readonly ShopItem View;
            public readonly ShopItemData Data;
            public readonly ShopVariantState State;

            public PushedSlot(ShopItem view, ShopItemData data, ShopVariantState state)
            {
                View = view;
                Data = data;
                State = state;
            }
        }
    }
}
