using System;
using System.Collections.Generic;
using Neo.Extensions;
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
        [Header("Source")]
        [SerializeField] private Shop _shop;

        [Tooltip("Empty category shows all items when Show All When Category Empty is enabled.")]
        [SerializeField]
        private string _category = "";

        [SerializeField] private bool _showAllWhenCategoryEmpty = true;
        [SerializeField] private bool _includeOwned = true;
        [SerializeField] private bool _includeUnowned = true;

        [Tooltip("Useful for consumable-free cosmetic stores where already owned one-time items should disappear.")]
        [SerializeField]
        private bool _hideOwnedSinglePurchaseItems;

        [Header("Views")]
        [SerializeField] private ShopItem _itemPrefab;
        [SerializeField] private Transform _itemsRoot;

        [Tooltip("Optional pre-authored views. Missing views are spawned from Item Prefab.")]
        [SerializeField]
        private List<ShopItem> _views = new();

        [Tooltip("Disable the scene prefab object after cloning it.")]
        [SerializeField]
        private bool _hideScenePrefab = true;

        [Header("Buttons")]
        [SerializeField] private bool _autoBindButtons = true;
        [SerializeField] private ShopListButtonAction _buttonAction = ShopListButtonAction.Buy;

        [Header("Refresh")]
        [SerializeField] private bool _bindShopIfNull = true;
        [SerializeField] private bool _refreshOnEnable = true;
        [SerializeField] private bool _refreshOnShopEvents = true;

        [Header("Events")]
        public ShopStringEvent OnCategoryChanged = new();
        public UnityEvent OnRefreshed = new();

        private readonly List<ShopItemData> _visibleItems = new();
        private UnityEventDelegateCache _buttonDelegates;
        private bool _subscribedToShop;

        public Shop Shop => _shop;
        public string Category => _category;
        public IReadOnlyList<ShopItemData> VisibleItems => _visibleItems;

        private void OnEnable()
        {
            BindIfNeeded();
            SubscribeShop(true);

            if (_refreshOnEnable)
            {
                Refresh();
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
            SubscribeButtons(false);
            _visibleItems.Clear();

            if (_shop == null)
            {
                SetActiveViewCount(0);
                OnRefreshed?.Invoke();
                return;
            }

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
                _shop = GetComponentInParent<Shop>();
                if (_shop == null)
                {
                    _shop = FindFirstObjectByType<Shop>();
                }
            }

            if (_itemsRoot == null)
            {
                _itemsRoot = transform;
            }
        }

        private void SubscribeShop(bool subscribe)
        {
            if (_shop == null || (!_refreshOnShopEvents && (subscribe || !_subscribedToShop)))
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
            float price = _shop.GetPrice(data.Id);
            _views[index].Visual(data, owned ? 0 : Mathf.RoundToInt(price));
            _views[index].Select(_shop.EquippedId == data.Id);
        }

        private void SubscribeButtons(bool subscribe)
        {
            if (!_autoBindButtons)
            {
                return;
            }

            _buttonDelegates ??= new UnityEventDelegateCache();

            if (subscribe)
            {
                _buttonDelegates.Clear();
                for (int i = 0; i < _visibleItems.Count && i < _views.Count; i++)
                {
                    if (_views[i] == null || _views[i].buttonBuy == null)
                    {
                        continue;
                    }

                    int index = i;
                    _buttonDelegates.SubscribeAt(i, _views[i].buttonBuy.onClick, () => ExecuteButtonAction(index));
                }

                return;
            }

            for (int i = 0; i < _views.Count && i < _buttonDelegates.Count; i++)
            {
                if (_views[i] != null && _views[i].buttonBuy != null)
                {
                    _buttonDelegates.UnsubscribeAt(i, _views[i].buttonBuy.onClick);
                }
            }

            _buttonDelegates.Clear();
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
