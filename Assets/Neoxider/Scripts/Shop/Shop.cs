using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Shop
{
    /// <summary>
    ///     Shop controller. Holds a list of <see cref="ShopItemData"/> (and optional <see cref="ShopBundleData"/>),
    ///     handles purchase / equip flow via <see cref="ShopPurchaseFlow"/>, and persists owned items +
    ///     equipped id + runtime price overrides as a single JSON blob (<see cref="ShopProfileData"/>).
    ///
    ///     Item identity is the stable <see cref="ShopItemData.Id"/> string. Old int-indexed API
    ///     (<see cref="Id"/>, <see cref="Buy(int)"/>, <c>OnPurchased&lt;int&gt;</c>) is preserved as
    ///     <see cref="ObsoleteAttribute"/> proxies that resolve through the item array; events fire in both
    ///     int and string forms so existing scene wiring keeps working.
    ///
    ///     Inventory integration is handled by a separate bridge component
    ///     (<c>Neo.Tools.ShopInventoryGrantBridge</c>) — drop it onto the same GameObject as Shop or
    ///     InventoryComponent and configure mappings (shopItemId → InventoryItemData + amount). The
    ///     bridge subscribes to <see cref="OnPurchasedId"/> / <see cref="OnPurchasedBundle"/>. Kept
    ///     out of <c>Neo.Shop</c> deliberately to avoid pulling <c>Neo.Tools.Inventory</c> into the
    ///     Shop assembly closure.
    /// </summary>
    [NeoDoc("Shop/Shop.md")]
    [CreateFromMenu("Neoxider/Shop/Shop")]
    [AddComponentMenu("Neoxider/" + "Shop/" + nameof(Shop))]
    public class Shop : MonoBehaviour
    {
        [Header("Flow")]
        [Tooltip("High-level purchase/equip mode. See ShopPurchaseFlow for semantics.")]
        [SerializeField]
        private ShopPurchaseFlow _purchaseFlow = ShopPurchaseFlow.BuyAndEquip;

        [Header("Items / Bundles")]
        [SerializeField]
        private ShopItemData[] _shopItemDatas;

        [Tooltip("Optional list of bundles. Each bundle grants its items on purchase.")] [SerializeField]
        private ShopBundleData[] _bundles;

        [SerializeField] private ShopItem _shopItemPreview;

        [SerializeField] private ShopItem[] _shopItems;

        [Header("Spawn Shop Items")]
        [Tooltip("Parent transform for spawned ShopItems. Falls back to this GameObject when null.")]
        [SerializeField]
        private Transform _container;

        [Tooltip("Optional prefab. Shop spawns missing ShopItems until count matches _shopItemDatas.Length.")]
        [SerializeField]
        private ShopItem _prefab;

        [Tooltip("When enabled, Shop fills its own _shopItems list from Prefab. Disable when using external views such as ShopListView.")]
        [SerializeField]
        private bool _autoSpawnItems = true;

        [Header("Save")]
        [Tooltip("Single SaveProvider key. ShopProfileData JSON is stored here. Wipe-friendly: deleting the key resets the whole shop.")]
        [SerializeField]
        private string _keySave = "Shop";

        [Header("Currency")]
        [Tooltip("Default IMoneySpend source. When null, Money.I is used. Per-item / per-bundle Currency Override Save Key takes precedence.")]
        [FormerlySerializedAs("IMoneySpend")]
        [SerializeField]
        public GameObject moneySpendSource;

        [Header("Advanced")]
        [Tooltip("Auto-subscribe ShopItem.buttonBuy.onClick to Buy(itemIndex).")]
        [SerializeField]
        private bool _autoSubscribe = true;

        [Tooltip("On failed purchase, switch the preview to the item the player tried to buy.")] [SerializeField]
        private bool _changePreviewOnPurchaseFailed;

        [Tooltip("Propagate selection to ShopItem.Select(bool) on every list entry. Set false if your UI does its own highlighting.")]
        [FormerlySerializedAs("_useSetItem")]
        [SerializeField]
        private bool _propagateSelectionVisual = true;

        [Tooltip(
            "When true (and PurchaseFlow is BuyAndEquip / EquipOnly), equips the saved item at load; if none is saved, selects the first catalog item. Has no effect for BuyOnly / Browse flows.")]
        [SerializeField]
        private bool _activateSavedEquipped = true;

        [Header("Legacy (deprecated)")]
        [Tooltip("Устарело — цены теперь берутся из ShopItemData.price + runtime overrides из ShopProfileData. Поле сохранено для совместимости со старыми сценами и игнорируется в рантайме.")]
        [SerializeField]
        private int[] _prices;

#pragma warning disable 0414
        [Tooltip("Устарело — теперь весь сейв магазина живёт в едином ключе Save Key (JSON ShopProfileData). Поле игнорируется в рантайме.")]
        [SerializeField]
        private string _keySaveEquipped = "ShopEquipped";
#pragma warning restore 0414

        [Space] [Header("Events (int — legacy)")]
        public UnityEvent<int> OnSelect = new();
        public UnityEvent<int> OnPurchased = new();
        public UnityEvent<int> OnPurchaseFailed = new();
        public UnityEvent OnLoad = new();

        [Header("Events (string)")]
        public ShopStringEvent OnSelectId = new();
        public ShopStringEvent OnPurchasedId = new();
        public ShopStringEvent OnPurchaseFailedId = new();
        public ShopBundleEvent OnPurchasedBundle = new();
        public UnityEvent OnShopChanged = new();

        // --- runtime state ---
        private UnityEventDelegateCache _buyDelegates;
        private IMoneySpend _defaultMoney;
        private ShopProfileData _profile = new();
        private bool _started;

        /// <summary>
        ///     Legacy: prices array. Kept for backwards-compatible serialization. Runtime ignores it —
        ///     prices come from <see cref="ShopItemData.price"/> + <see cref="ShopProfileData.PriceOverrides"/>.
        /// </summary>
        [Obsolete("Use GetPrice(string) / ShopItemData.price. _prices is ignored in runtime since v8.5.")]
        public int[] Prices => _prices;

        /// <summary>Read-only view of the item-data list.</summary>
        public ShopItemData[] ShopItemDatas => _shopItemDatas;

        /// <summary>Read-only view of the bundle list. Never null in runtime (returns empty array when not configured).</summary>
        public ShopBundleData[] Bundles => _bundles ?? Array.Empty<ShopBundleData>();

        /// <summary>Whether Shop creates missing item views by itself.</summary>
        public bool AutoSpawnItems
        {
            get => _autoSpawnItems;
            set => _autoSpawnItems = value;
        }

        /// <summary>Currently equipped item ID. Empty when nothing is equipped.</summary>
        public string EquippedId => _profile.EquippedId;

        /// <summary>Item ID currently shown in the preview slot. Empty when no preview.</summary>
        public string PreviewIdString { get; private set; } = "";

        /// <summary>Currently selected item index in <see cref="ShopItemDatas"/>. -1 when nothing matches.</summary>
        [Obsolete("Use EquippedId. Integer-indexed API will be removed in v9.")]
        public int Id
        {
            get => IndexOfItemDataById(_profile.EquippedId);
            set => Select(ItemIdByIndex(value));
        }

        /// <summary>Preview slot index. -1 when no preview.</summary>
        [Obsolete("Use PreviewIdString. Integer-indexed API will be removed in v9.")]
        public int PreviewId => IndexOfItemDataById(PreviewIdString);

        private void Awake()
        {
            EnsureMissingItemIds();
            LoadProfile();
            SpawnItems();
            Subscribe(true);
            PreviewIdString = string.IsNullOrEmpty(_profile.EquippedId)
                ? FirstItemId()
                : _profile.EquippedId;
            VisualPreview();
        }

        private void Start()
        {
            _defaultMoney = ResolveDefaultCurrency();

            VisualAll();

            TryActivateEquippedOnLoad();

            _started = true;
            OnLoad?.Invoke();
        }

        private void OnEnable()
        {
            if (!_started)
            {
                return;
            }

            VisualAll();
            if (_propagateSelectionVisual && !string.IsNullOrEmpty(_profile.EquippedId))
            {
                PropagateSelectionVisual(_profile.EquippedId);
            }
        }

        private void OnDestroy()
        {
            Subscribe(false);
        }

        private void OnValidate()
        {
            _shopItems ??= GetComponentsInChildren<ShopItem>(true);
        }

        private void TryActivateEquippedOnLoad()
        {
            if (!_activateSavedEquipped)
            {
                return;
            }

            ShopPurchaseFlow flow = _purchaseFlow;
            if (flow != ShopPurchaseFlow.BuyAndEquip && flow != ShopPurchaseFlow.EquipOnly)
            {
                return;
            }

            string itemId = _profile.EquippedId;
            if (string.IsNullOrEmpty(itemId) || ResolveItemDataById(itemId) == null)
            {
                itemId = FirstItemId();
            }

            if (!string.IsNullOrEmpty(itemId))
            {
                Select(itemId);
            }
        }

        private void EnsureMissingItemIds()
        {
            EnsureMissingItemIds(_shopItemDatas);
        }

        /// <summary>
        ///     Fills empty <see cref="ShopItemData.Id"/> from display name, asset name, or index.
        ///     Runs in <see cref="Awake"/> before <see cref="LoadProfile"/> so saves match real keys.
        /// </summary>
        private static void EnsureMissingItemIds(ShopItemData[] items)
        {
            if (items == null || items.Length == 0)
            {
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                ShopItemData data = items[i];
                if (data == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(data.Id))
                {
                    continue;
                }

                string basePart = SanitizeIdToken(data.nameItem);
                if (string.IsNullOrEmpty(basePart))
                {
                    basePart = SanitizeIdToken(data.name);
                }

                if (string.IsNullOrEmpty(basePart))
                {
                    basePart = "shop_item";
                }

                data.AssignIdIfEmpty($"{basePart}_{i}");
            }
        }

        private static string SanitizeIdToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "";
            }

            return raw.Trim().Replace(" ", "_");
        }

        // ---------- Public API: string -------------------------------------------------

        /// <summary>
        ///     Initiates the purchase / equip flow for the given item id. Behaviour depends on
        ///     <see cref="ShopPurchaseFlow"/>. Owned items don't spend money again.
        /// </summary>
        public void Buy(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            ShopPurchaseFlow flow = _purchaseFlow;
            if (flow == ShopPurchaseFlow.Browse)
            {
                return;
            }

            ShopItemData data = ResolveItemDataById(itemId);
            if (data == null)
            {
                return;
            }

            if (flow == ShopPurchaseFlow.EquipOnly)
            {
                Select(itemId);
                ShowPreview(itemId);
                return;
            }

            float price = GetPrice(itemId);
            bool isOwned = _profile.IsItemOwned(itemId);
            bool isFree = price <= 0f;

            if (isOwned || isFree)
            {
                if (isFree && !isOwned)
                {
                    if (data.isSinglePurchase)
                    {
                        _profile.TryAddOwnedItem(itemId);
                    }

                    SaveProfile();
                    VisualAll();
                    InvokePurchasedEvents(itemId);
                }

                if (flow == ShopPurchaseFlow.BuyAndEquip)
                {
                    Select(itemId);
                }

                ShowPreview(itemId);
                return;
            }

            IMoneySpend money = ResolveCurrency(data.CurrencyOverrideSaveKey);
            if (money != null && money.Spend(price))
            {
                if (data.isSinglePurchase)
                {
                    _profile.TryAddOwnedItem(itemId);
                }

                SaveProfile();
                VisualAll();

                InvokePurchasedEvents(itemId);

                if (flow == ShopPurchaseFlow.BuyAndEquip)
                {
                    Select(itemId);
                }

                ShowPreview(itemId);
                return;
            }

            int failedIndex = IndexOfItemDataById(itemId);
            if (failedIndex >= 0)
            {
                OnPurchaseFailed?.Invoke(failedIndex);
            }

            OnPurchaseFailedId?.Invoke(itemId);

            if (_changePreviewOnPurchaseFailed)
            {
                ShowPreview(itemId);
            }
        }

        /// <summary>Purchases the bundle and grants all its items (and per-item inventory hooks) on success.</summary>
        public void BuyBundle(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId))
            {
                return;
            }

            ShopPurchaseFlow flow = _purchaseFlow;
            if (flow == ShopPurchaseFlow.Browse || flow == ShopPurchaseFlow.EquipOnly)
            {
                return;
            }

            ShopBundleData bundle = ResolveBundleById(bundleId);
            if (bundle == null)
            {
                return;
            }

            if (bundle.isSinglePurchase && _profile.IsBundleOwned(bundleId))
            {
                return;
            }

            float price = Mathf.Max(0, bundle.price);
            IMoneySpend money = ResolveCurrency(bundle.CurrencyOverrideSaveKey);

            bool needsSpend = price > 0f;
            if (needsSpend && (money == null || !money.Spend(price)))
            {
                OnPurchaseFailedId?.Invoke(bundleId);
                return;
            }

            if (bundle.Items != null)
            {
                for (int i = 0; i < bundle.Items.Count; i++)
                {
                    ShopItemData item = bundle.Items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    if (item.isSinglePurchase)
                    {
                        _profile.TryAddOwnedItem(item.Id);
                    }

                    OnPurchasedId?.Invoke(item.Id);
                }
            }

            if (bundle.isSinglePurchase)
            {
                _profile.TryAddOwnedBundle(bundleId);
            }

            SaveProfile();
            VisualAll();
            OnPurchasedBundle?.Invoke(bundle);
        }

        /// <summary>Selects the item by id. Fires OnSelectId (and int proxy when index is resolvable).</summary>
        public void Select(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                _profile.EquippedId = "";
                SaveProfile();
                OnSelectId?.Invoke("");
                return;
            }

            _profile.EquippedId = itemId;
            SaveProfile();

            int index = IndexOfItemDataById(itemId);
            if (index >= 0)
            {
                OnSelect?.Invoke(index);
            }

            OnSelectId?.Invoke(itemId);

            if (_propagateSelectionVisual)
            {
                PropagateSelectionVisual(itemId);
            }
        }

        /// <summary>Sets the preview slot to the given item id; updates the visual immediately.</summary>
        public void ShowPreview(string itemId)
        {
            PreviewIdString = itemId ?? "";
            VisualPreview();
        }

        /// <summary>True when <paramref name="itemId"/> is in the owned set.</summary>
        public bool IsOwned(string itemId)
        {
            return _profile.IsItemOwned(itemId);
        }

        /// <summary>True when <paramref name="bundleId"/> is in the owned-bundles set.</summary>
        public bool IsBundleOwned(string bundleId)
        {
            return _profile.IsBundleOwned(bundleId);
        }

        /// <summary>
        ///     Runtime price for <paramref name="itemId"/>. Returns 0 when item is unknown; applies
        ///     runtime override from <see cref="ShopProfileData.PriceOverrides"/> when present.
        /// </summary>
        public float GetPrice(string itemId)
        {
            ShopItemData data = ResolveItemDataById(itemId);
            if (data == null)
            {
                return 0f;
            }

            return _profile.GetPriceOrDefault(itemId, data.price);
        }

        /// <summary>Persists a runtime price override (e.g. discount) for the given item.</summary>
        public void SetRuntimePrice(string itemId, float price)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            _profile.SetPriceOverride(itemId, price);
            SaveProfile();
            VisualAll();
        }

        /// <summary>Removes a runtime price override; subsequent GetPrice calls return ShopItemData.price.</summary>
        public void ClearRuntimePrice(string itemId)
        {
            if (_profile.ClearPriceOverride(itemId))
            {
                SaveProfile();
                VisualAll();
            }
        }

        /// <summary>Replaces the runtime catalog and refreshes all connected views.</summary>
        public void SetItems(ShopItemData[] items, bool resetPreviewToFirst = true)
        {
            _shopItemDatas = items ?? Array.Empty<ShopItemData>();
            EnsureMissingItemIds(_shopItemDatas);
            if (resetPreviewToFirst || ResolveItemDataById(PreviewIdString) == null)
            {
                PreviewIdString = FirstItemId();
            }

            if (!string.IsNullOrEmpty(_profile.EquippedId) && ResolveItemDataById(_profile.EquippedId) == null)
            {
                _profile.EquippedId = "";
                SaveProfile();
            }

            VisualAll();
            VisualPreview();
        }

        /// <summary>Replaces the runtime bundle catalog.</summary>
        public void SetBundles(ShopBundleData[] bundles)
        {
            _bundles = bundles ?? Array.Empty<ShopBundleData>();
            OnShopChanged?.Invoke();
        }

        /// <summary>Assigns the default currency source at runtime.</summary>
        public void SetMoneySpendSource(GameObject source)
        {
            moneySpendSource = source;
            _defaultMoney = ResolveDefaultCurrency();
        }

        /// <summary>NoCode-friendly toggle for Shop-owned item spawning.</summary>
        public void SetAutoSpawnItems(bool autoSpawnItems)
        {
            _autoSpawnItems = autoSpawnItems;
        }

        /// <summary>Filtered list of item-data assets by <see cref="ShopItemData.Category"/>.</summary>
        public IReadOnlyList<ShopItemData> GetItemsInCategory(string category)
        {
            if (_shopItemDatas == null || _shopItemDatas.Length == 0)
            {
                return Array.Empty<ShopItemData>();
            }

            return _shopItemDatas
                .Where(d => d != null && string.Equals(d.Category ?? "", category ?? "", StringComparison.Ordinal))
                .ToArray();
        }

        /// <summary>Distinct category labels from configured items, in first-seen order.</summary>
        public IReadOnlyList<string> GetCategories(bool includeEmpty = false)
        {
            if (_shopItemDatas == null || _shopItemDatas.Length == 0)
            {
                return Array.Empty<string>();
            }

            List<string> categories = new();
            HashSet<string> seen = new(StringComparer.Ordinal);
            for (int i = 0; i < _shopItemDatas.Length; i++)
            {
                ShopItemData data = _shopItemDatas[i];
                if (data == null)
                {
                    continue;
                }

                string category = data.Category ?? "";
                if (!includeEmpty && string.IsNullOrEmpty(category))
                {
                    continue;
                }

                if (seen.Add(category))
                {
                    categories.Add(category);
                }
            }

            return categories;
        }

        /// <summary>Refreshes Shop-owned item visuals and preview, then notifies external views.</summary>
        public void RefreshVisuals()
        {
            VisualAll();
            VisualPreview();
        }

        // ---------- Public API: legacy int proxy --------------------------------------

        /// <summary>Legacy alias for <see cref="Buy(string)"/>.</summary>
        [Obsolete("Use Buy(string itemId). Will be removed in v9.")]
        public void Buy()
        {
            string id = string.IsNullOrEmpty(PreviewIdString) ? _profile.EquippedId : PreviewIdString;
            if (!string.IsNullOrEmpty(id))
            {
                Buy(id);
            }
        }

        /// <summary>Legacy alias for <see cref="Buy(string)"/> via array index.</summary>
        [Obsolete("Use Buy(string itemId). Will be removed in v9.")]
        public void Buy(int id)
        {
            string resolved = ItemIdByIndex(id);
            if (!string.IsNullOrEmpty(resolved))
            {
                Buy(resolved);
            }
        }

        /// <summary>Legacy alias for <see cref="ShowPreview(string)"/> via array index.</summary>
        [Obsolete("Use ShowPreview(string itemId). Will be removed in v9.")]
        public void ShowPreview(int id)
        {
            ShowPreview(ItemIdByIndex(id));
        }

        // ---------- Internals ----------------------------------------------------------

        private void SpawnItems()
        {
            if (_shopItemDatas == null)
            {
                return;
            }

            if (!_autoSpawnItems)
            {
                return;
            }

            List<ShopItem> list = _shopItems != null ? _shopItems.ToList() : new List<ShopItem>();

            if (_prefab != null)
            {
                Transform parent = _container != null ? _container : transform;
                for (int i = list.Count; i < _shopItemDatas.Length; i++)
                {
                    ShopItem inst = Instantiate(_prefab, parent);
                    inst.gameObject.SetActive(true);
                    list.Add(inst);
                }

                if (_prefab.gameObject.scene.IsValid())
                {
                    _prefab.gameObject.SetActive(false);
                }
            }

            _shopItems = list.ToArray();
        }

        private void Subscribe(bool subscribe)
        {
            if (_shopItems == null || _shopItems.Length == 0 || !_autoSubscribe)
            {
                return;
            }

            if (subscribe)
            {
                _buyDelegates ??= new UnityEventDelegateCache();
                _buyDelegates.Clear();
                for (int i = 0; i < _shopItems.Length; i++)
                {
                    if (_shopItems[i] == null || _shopItems[i].buttonBuy == null)
                    {
                        continue;
                    }

                    int index = i;
                    _buyDelegates.SubscribeAt(i, _shopItems[i].buttonBuy.onClick, () => BuyByIndexInternal(index));
                }
            }
            else if (_buyDelegates != null)
            {
                for (int i = 0; i < _shopItems.Length && i < _buyDelegates.Count; i++)
                {
                    if (_shopItems[i] != null && _shopItems[i].buttonBuy != null)
                    {
                        _buyDelegates.UnsubscribeAt(i, _shopItems[i].buttonBuy.onClick);
                    }
                }
            }
        }

        private void BuyByIndexInternal(int index)
        {
            string id = ItemIdByIndex(index);
            if (!string.IsNullOrEmpty(id))
            {
                Buy(id);
            }
        }

        private void InvokePurchasedEvents(string itemId)
        {
            int index = IndexOfItemDataById(itemId);
            if (index >= 0)
            {
                OnPurchased?.Invoke(index);
            }

            OnPurchasedId?.Invoke(itemId);
        }

        private void LoadProfile()
        {
            string json = SaveProvider.GetString(_keySave, "");
            if (string.IsNullOrEmpty(json))
            {
                _profile = new ShopProfileData();
                return;
            }

            try
            {
                _profile = JsonUtility.FromJson<ShopProfileData>(json) ?? new ShopProfileData();
            }
            catch
            {
                _profile = new ShopProfileData();
            }

            _profile.Sanitize();
        }

        private void SaveProfile()
        {
            _profile.Sanitize();
            string json = JsonUtility.ToJson(_profile);
            SaveProvider.SetString(_keySave, json);
        }

        private void VisualAll()
        {
            if (_shopItems == null || _shopItemDatas == null)
            {
                OnShopChanged?.Invoke();
                return;
            }

            for (int i = 0; i < _shopItems.Length; i++)
            {
                if (_shopItems[i] == null)
                {
                    continue;
                }

                ShopItemData data = i < _shopItemDatas.Length ? _shopItemDatas[i] : null;
                float price = data != null ? GetPrice(data.Id) : 0f;
                bool owned = data != null && _profile.IsItemOwned(data.Id);
                _shopItems[i].Visual(data, owned ? 0 : Mathf.RoundToInt(price), i);
            }

            OnShopChanged?.Invoke();
        }

        private void VisualPreview()
        {
            if (_shopItemPreview == null)
            {
                return;
            }

            ShopItemData data = ResolveItemDataById(PreviewIdString);
            if (data == null)
            {
                int idx = IndexOfItemDataById(_profile.EquippedId);
                _shopItemPreview.Visual((ShopItemData)null, 0, idx);
                return;
            }

            bool owned = _profile.IsItemOwned(data.Id);
            float price = GetPrice(data.Id);
            _shopItemPreview.Visual(data, owned ? 0 : Mathf.RoundToInt(price), IndexOfItemDataById(data.Id));
        }

        private void PropagateSelectionVisual(string itemId)
        {
            if (_shopItems == null || _shopItemDatas == null)
            {
                return;
            }

            for (int i = 0; i < _shopItems.Length; i++)
            {
                if (_shopItems[i] == null)
                {
                    continue;
                }

                ShopItemData data = i < _shopItemDatas.Length ? _shopItemDatas[i] : null;
                bool active = data != null && data.Id == itemId;
                _shopItems[i].Select(active);
            }
        }

        private IMoneySpend ResolveDefaultCurrency()
        {
            if (moneySpendSource != null)
            {
                IMoneySpend src = moneySpendSource.GetComponent<IMoneySpend>();
                if (src != null)
                {
                    return src;
                }
            }

            return Money.I;
        }

        private IMoneySpend ResolveCurrency(string overrideSaveKey)
        {
            if (!string.IsNullOrEmpty(overrideSaveKey) && Money.TryFindBySaveKey(overrideSaveKey, out Money keyedMoney))
            {
                return keyedMoney;
            }

            return _defaultMoney ?? Money.I;
        }

        private ShopItemData ResolveItemDataById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _shopItemDatas == null)
            {
                return null;
            }

            for (int i = 0; i < _shopItemDatas.Length; i++)
            {
                ShopItemData data = _shopItemDatas[i];
                if (data != null && data.Id == itemId)
                {
                    return data;
                }
            }

            return null;
        }

        private ShopBundleData ResolveBundleById(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId) || _bundles == null)
            {
                return null;
            }

            for (int i = 0; i < _bundles.Length; i++)
            {
                ShopBundleData bundle = _bundles[i];
                if (bundle != null && bundle.Id == bundleId)
                {
                    return bundle;
                }
            }

            return null;
        }

        private int IndexOfItemDataById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _shopItemDatas == null)
            {
                return -1;
            }

            for (int i = 0; i < _shopItemDatas.Length; i++)
            {
                if (_shopItemDatas[i] != null && _shopItemDatas[i].Id == itemId)
                {
                    return i;
                }
            }

            return -1;
        }

        private string ItemIdByIndex(int index)
        {
            if (_shopItemDatas == null || index < 0 || index >= _shopItemDatas.Length)
            {
                return "";
            }

            ShopItemData data = _shopItemDatas[index];
            return data != null ? data.Id : "";
        }

        private string FirstItemId()
        {
            if (_shopItemDatas == null)
            {
                return "";
            }

            for (int i = 0; i < _shopItemDatas.Length; i++)
            {
                if (_shopItemDatas[i] != null && !string.IsNullOrEmpty(_shopItemDatas[i].Id))
                {
                    return _shopItemDatas[i].Id;
                }
            }

            return "";
        }
    }
}
