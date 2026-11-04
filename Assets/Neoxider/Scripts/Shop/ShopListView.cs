using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Shop
{
    public enum ShopListButtonAction
    {
        Buy = 0,
        Preview = 1,
        Select = 2
    }

    /// <summary>
    ///     Optional dynamic storefront view for <see cref="Shop"/>.
    ///     It owns item view creation, category filtering, and button wiring while Shop keeps
    ///     purchase, save, currency, and ownership rules.
    /// </summary>
    [NeoDoc("Shop/ShopListView.md")]
    [CreateFromMenu("Neoxider/Shop/ShopListView")]
    [AddComponentMenu("Neoxider/Shop/" + nameof(ShopListView))]
    public sealed class ShopListView : MonoBehaviour
    {
        [Header("Source")] [SerializeField] private Shop _shop;

        [Tooltip("Empty category shows all items when Show All When Category Empty is enabled.")] [SerializeField]
        private string _category = "";

        [SerializeField] private bool _showAllWhenCategoryEmpty = true;
        [SerializeField] private bool _includeOwned = true;
        [SerializeField] private bool _includeUnowned = true;

        [Tooltip("Useful for consumable-free cosmetic stores where already owned one-time items should disappear.")]
        [SerializeField]
        private bool _hideOwnedSinglePurchaseItems;

        [Header("Views")] [SerializeField] private ShopItem _itemPrefab;
        [SerializeField] private Transform _itemsRoot;

        [Tooltip("Optional pre-authored views. Missing views are spawned from Item Prefab.")] [SerializeField]
        private List<ShopItem> _views = new();

        [Tooltip("Disable the scene prefab object after cloning it.")] [SerializeField]
        private bool _hideScenePrefab = true;

        [Header("Buttons")] [SerializeField] private bool _autoBindButtons = true;
        [SerializeField] private ShopListButtonAction _buttonAction = ShopListButtonAction.Buy;

        [Header("Refresh")] [SerializeField] private bool _bindShopIfNull = true;
        [SerializeField] private bool _refreshOnEnable = true;
        [SerializeField] private bool _refreshOnShopEvents = true;

        [Header("Events")] public ShopStringEvent OnCategoryChanged = new();
        public UnityEvent OnRefreshed = new();

        private readonly List<ShopItemData> _visibleItems = new();
        private readonly List<UnityAction> _buttonActions = new();
        private readonly List<UnityEvent> _boundButtonEvents = new();
        private bool _subscribedToShop;
        private Shop _refreshedShop;
        private int _refreshedShopVersion;

        public Shop Shop => _shop;
        public string Category => _category;
        public IReadOnlyList<ShopItemData> VisibleItems => _visibleItems;

        /// <summary>Item views managed by this list (pre-authored plus spawned), in bind order.</summary>
        public IReadOnlyList<ShopItem> Views => _views;

        /// <summary>Current click behaviour bound to item buttons.</summary>
        public ShopListButtonAction ButtonAction => _buttonAction;

        private void OnEnable()
        {
            BindIfNeeded();
            SubscribeShop(true);

            if (_refreshOnEnable)
            {
                Refresh();
            }
            else
            {
                SubscribeButtons(true);
            }
        }

        private void OnDisable()
        {
            SubscribeButtons(false);
            SubscribeShop(false);
        }

        private void OnValidate()
        {
            if (_itemsRoot == null)
            {
                _itemsRoot = transform;
            }
        }

        public void SetShop(Shop shop)
        {
            if (_shop == shop)
            {
                return;
            }

            SubscribeShop(false);
            _shop = shop;
            SubscribeShop(true);
            Refresh();
        }

        public void ShowAll()
        {
            SetCategory("");
        }

        public void ShowCategory(string category)
        {
            SetCategory(category);
        }

        public void SetCategory(string category)
        {
            string next = category ?? "";
            if (string.Equals(_category, next, StringComparison.Ordinal))
            {
                Refresh();
                return;
            }

            _category = next;
            OnCategoryChanged?.Invoke(_category);
            Refresh();
        }

        public void SetIncludeOwned(bool includeOwned)
        {
            _includeOwned = includeOwned;
            Refresh();
        }

        public void SetIncludeUnowned(bool includeUnowned)
        {
            _includeUnowned = includeUnowned;
            Refresh();
        }

        public void SetButtonAction(ShopListButtonAction action)
        {
            if (_buttonAction == action)
            {
                return;
            }

            SubscribeButtons(false);
            _buttonAction = action;
            SubscribeButtons(true);
        }

        public void SetItemPrefab(ShopItem itemPrefab)
        {
            _itemPrefab = itemPrefab;
            Refresh();
        }

        public void SetItemsRoot(Transform itemsRoot)
        {
            _itemsRoot = itemsRoot != null ? itemsRoot : transform;
            Refresh();
        }

        public void SetShowAllWhenCategoryEmpty(bool showAll)
        {
            _showAllWhenCategoryEmpty = showAll;
            Refresh();
        }

        public void SetHideOwnedSinglePurchaseItems(bool hideOwned)
        {
            _hideOwnedSinglePurchaseItems = hideOwned;
            Refresh();
        }

        public void Refresh()
        {
            BindIfNeeded();
            SubscribeShop(true);
            _visibleItems.Clear();

            if (_shop == null)
            {
                _refreshedShop = null;
                SetActiveViewCount(0);
                SubscribeButtons(true);
                OnRefreshed?.Invoke();
                return;
            }

            _refreshedShop = _shop;
            _refreshedShopVersion = _shop.StateVersion;

            ShopItemData[] allItems = _shop.ShopItemDatas;
            if (allItems != null)
            {
                for (int i = 0; i < allItems.Length; i++)
                {
                    ShopItemData data = allItems[i];
                    if (ShouldShow(data))
                    {
                        _visibleItems.Add(data);
                    }
                }
            }

            SetActiveViewCount(_visibleItems.Count);
            for (int i = 0; i < _visibleItems.Count; i++)
            {
                BindView(i, _visibleItems[i]);
            }

            SubscribeButtons(true);
            OnRefreshed?.Invoke();
        }

        private void BindIfNeeded()
        {
            if (_shop == null && _bindShopIfNull)
            {
                _shop = ShopResolver.Resolve<Shop>(this);
            }

            if (_itemsRoot == null)
            {
                _itemsRoot = transform;
            }
        }

        private void SubscribeShop(bool subscribe)
        {
            if (_shop == null)
            {
                _subscribedToShop = false;
                return;
            }

            if ((subscribe && !isActiveAndEnabled) ||
                (!_refreshOnShopEvents && (subscribe || !_subscribedToShop)))
            {
                return;
            }

            if (subscribe && !_subscribedToShop)
            {
                _shop.OnShopChanged.AddListener(Refresh);
                _shop.OnLoad.AddListener(Refresh);
                _shop.OnSelectId.AddListener(HandleShopIdEvent);
                _shop.OnPurchasedId.AddListener(HandleShopIdEvent);
                _subscribedToShop = true;
            }
            else if (!subscribe && _subscribedToShop)
            {
                _shop.OnShopChanged.RemoveListener(Refresh);
                _shop.OnLoad.RemoveListener(Refresh);
                _shop.OnSelectId.RemoveListener(HandleShopIdEvent);
                _shop.OnPurchasedId.RemoveListener(HandleShopIdEvent);
                _subscribedToShop = false;
            }
        }

        private void HandleShopIdEvent(string _)
        {
            // WHY: one purchase raises OnShopChanged, then OnPurchasedId per item and OnSelectId; the first
            // refresh already shows the committed state, so the follow-up notifications skip the rebuild.
            if (_shop != null && ReferenceEquals(_refreshedShop, _shop) &&
                _refreshedShopVersion == _shop.StateVersion)
            {
                return;
            }

            Refresh();
        }

        private bool ShouldShow(ShopItemData data)
        {
            if (data == null)
            {
                return false;
            }

            if (!MatchesCategory(data))
            {
                return false;
            }

            bool owned = _shop != null && _shop.IsOwned(data.Id);
            if (owned && !_includeOwned)
            {
                return false;
            }

            if (!owned && !_includeUnowned)
            {
                return false;
            }

            return !_hideOwnedSinglePurchaseItems || !owned || !data.isSinglePurchase;
        }

        private bool MatchesCategory(ShopItemData data)
        {
            if (_showAllWhenCategoryEmpty && string.IsNullOrEmpty(_category))
            {
                return true;
            }

            return string.Equals(data.Category ?? "", _category ?? "", StringComparison.Ordinal);
        }

        private void SetActiveViewCount(int count)
        {
            EnsureViewCount(count);
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i] != null)
                {
                    bool active = i < count;
                    if (!active)
                    {
                        _views[i].Clear();
                    }

                    _views[i].gameObject.SetActive(active);
                }
            }
        }

        private void EnsureViewCount(int count)
        {
            if (_itemPrefab == null || _itemsRoot == null)
            {
                return;
            }

            while (_views.Count < count)
            {
                ShopItem created = Instantiate(_itemPrefab, _itemsRoot);
                created.gameObject.SetActive(true);
                _views.Add(created);
            }

            if (_hideScenePrefab && _itemPrefab.gameObject.scene.IsValid())
            {
                _itemPrefab.gameObject.SetActive(false);
            }
        }

        private void BindView(int index, ShopItemData data)
        {
            if (index < 0 || index >= _views.Count || _views[index] == null || data == null)
            {
                return;
            }

            bool owned = _shop.IsOwned(data.Id);
            float price = _shop.PriceOf(data);
            _views[index].Visual(data, owned ? 0 : Mathf.RoundToInt(price));
            _views[index].Select(_shop.EquippedId == data.Id);
        }

        private void SubscribeButtons(bool subscribe)
        {
            if (!subscribe || !_autoBindButtons || !isActiveAndEnabled)
            {
                for (int i = 0; i < _boundButtonEvents.Count; i++)
                {
                    BindButton(i, null);
                }

                return;
            }

            int bound = Mathf.Min(_visibleItems.Count, _views.Count);
            for (int i = 0; i < bound; i++)
            {
                ShopItem view = _views[i];
                BindButton(i, view != null && view.buttonBuy != null ? view.buttonBuy.onClick : null);
            }

            for (int i = bound; i < _boundButtonEvents.Count; i++)
            {
                BindButton(i, null);
            }
        }

        // WHY: views keep their slot index across refreshes, so each slot reuses one cached action and only
        // touches listeners when its button changes. Rebinding every refresh allocated a closure per row.
        private void BindButton(int slot, UnityEvent target)
        {
            while (_buttonActions.Count <= slot)
            {
                int captured = _buttonActions.Count;
                _buttonActions.Add(() => ExecuteButtonAction(captured));
                _boundButtonEvents.Add(null);
            }

            UnityEvent current = _boundButtonEvents[slot];
            if (ReferenceEquals(current, target))
            {
                return;
            }

            UnityAction action = _buttonActions[slot];
            current?.RemoveListener(action);
            target?.AddListener(action);
            _boundButtonEvents[slot] = target;
        }

        private void ExecuteButtonAction(int visibleIndex)
        {
            if (_shop == null || visibleIndex < 0 || visibleIndex >= _visibleItems.Count)
            {
                return;
            }

            string itemId = _visibleItems[visibleIndex] != null ? _visibleItems[visibleIndex].Id : "";
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            switch (_buttonAction)
            {
                case ShopListButtonAction.Preview:
                    _shop.ShowPreview(itemId);
                    return;
                case ShopListButtonAction.Select:
                    _shop.Select(itemId);
                    return;
                default:
                    _shop.Buy(itemId);
                    return;
            }
        }
    }
}
