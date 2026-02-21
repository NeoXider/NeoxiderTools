using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Shop
{
    [NeoDoc("Shop.md")]
    [CreateFromMenu("Neoxider/Shop/Shop")]
    [AddComponentMenu("Neoxider/" + "Shop/" + nameof(Shop))]
    public class Shop : MonoBehaviour
    {
        [Header("Price if null shopItemDatas")] [SerializeField]
        private int[] _prices;

        [SerializeField] private ShopItemData[] _shopItemDatas;

        [SerializeField] private ShopItem _shopItemPreview;

        [SerializeField] private ShopItem[] _shopItems;

        [Space] [Header("Settings")] [SerializeField]
        private bool _useSetItem = true;

        [SerializeField] private bool _autoSubscribe = true;

        [Tooltip("If true, saved ShopEquipped item is auto-activated on load")] [SerializeField]
        private bool _activateSavedEquipped = true;

        [SerializeField] private string _keySave = "Shop";
        [SerializeField] private string _keySaveEquipped = "ShopEquipped";

        [Tooltip(
            "Если true, при неудачной покупке (недостаточно денег) превью переключится на товар покупки. Если false — превью не изменяется.")]
        [SerializeField]
        private bool _changePreviewOnPurchaseFailed;

        [Space] [Header("Spawn Shop Items")] [SerializeField]
        private Transform _container;

        [SerializeField] private ShopItem _prefab;

        [Space] [Tooltip("GameObject with IMoneySpend (e.g. Money). If null, Money.I is used.")]
        [FormerlySerializedAs("IMoneySpend")] [SerializeField] public GameObject moneySpendSource;

        [Space] public UnityEvent<int> OnSelect;
        public UnityEvent<int> OnPurchased;
        public UnityEvent<int> OnPurchaseFailed;
        public UnityEvent OnLoad;
        private int _id;

        private IMoneySpend _money;
        private int _savedEquippedId;

        private bool load;

        private List<UnityEngine.Events.UnityAction> _buyDelegates;

        public int[] Prices => _prices;
        public ShopItemData[] ShopItemDatas => _shopItemDatas;

        public int PreviewId { get; private set; }

        public int Id
        {
            get => _id;
            set => Select(value);
        }

        private void Awake()
        {
            Load();
            LoadEquipped();
            Spawn();
            Subscriber(true);
            // Инициализируем превью сохранённым индексом (если допустим)
            ShowPreview(PreviewId);
        }

        private void Start()
        {
            if (moneySpendSource != null)
            {
                _money = moneySpendSource.GetComponent<IMoneySpend>() ?? Money.I;
            }
            else
            {
                _money = Money.I;
            }

            Visual();

            // Активируем сохраненный элемент если включена опция
            if (_activateSavedEquipped && _useSetItem)
            {
                int equippedId = Mathf.Clamp(_savedEquippedId, 0, _prices.Length - 1);
                Select(equippedId);
            }

            load = true;
            OnLoad?.Invoke();
        }

        private void OnEnable()
        {
            if (load)
            {
                Visual();
                // Восстанавливаем состояние выбора после обновления визуала
                if (_useSetItem)
                {
                    Select(_id);
                }
            }
        }

        private void OnDestroy()
        {
            Subscriber(false);
        }

        public void OnValidate()
        {
            _shopItems ??= GetComponentsInChildren<ShopItem>(true);

            if (NotNullDatas())
            {
                _prices = _shopItemDatas.Select(p => p.price).ToArray();
            }
        }

        private void Spawn()
        {
            if (_prefab == null)
            {
                return;
            }

            List<ShopItem> shopItemsList = _shopItems?.ToList() ?? new List<ShopItem>();
            Transform parent = _container != null ? _container : transform;

            for (int i = shopItemsList.Count; i < _prices.Length; i++)
            {
                ShopItem newShopItem = Instantiate(_prefab, parent);
                newShopItem.gameObject.SetActive(true);
                shopItemsList.Add(newShopItem);
            }

            _shopItems = shopItemsList.ToArray();

            if (_prefab.gameObject.scene.IsValid())
            {
                _prefab.gameObject.SetActive(false);
            }
        }

        private void Subscriber(bool subscribe)
        {
            if (_shopItems == null || _shopItems.Length == 0)
                return;

            if (subscribe)
            {
                _buyDelegates ??= new List<UnityEngine.Events.UnityAction>();
                _buyDelegates.Clear();
                for (int i = 0; i < _shopItems.Length; i++)
                {
                    int id = i;
                    UnityEngine.Events.UnityAction action = () => Buy(id);
                    _buyDelegates.Add(action);
                    if (_autoSubscribe && _shopItems[i].buttonBuy != null)
                        _shopItems[i].buttonBuy.onClick.AddListener(action);
                }
            }
            else
            {
                if (_buyDelegates != null && _autoSubscribe)
                {
                    for (int i = 0; i < _shopItems.Length && i < _buyDelegates.Count; i++)
                    {
                        if (_shopItems[i].buttonBuy != null)
                            _shopItems[i].buttonBuy.onClick.RemoveListener(_buyDelegates[i]);
                    }
                }
            }
        }

        private void Load()
        {
            if (NotNullDatas())
            {
                _prices ??= _shopItemDatas.Select(p => p.price).ToArray();
            }
            else if (_prices == null)
            {
                _prices = Array.Empty<int>();
                return;
            }

            if (_prices.Length == 0)
                return;

            int[] prices = new int[_prices.Length];
            for (int i = 0; i < _prices.Length; i++)
            {
                int defaultValue = NotNullDatas() && i < _shopItemDatas.Length ? _shopItemDatas[i].price : _prices[i];
                prices[i] = SaveProvider.GetInt(_keySave + i, defaultValue);
            }

            _prices = prices;
        }

        private void Save()
        {
            if (_prices == null)
                return;
            for (int i = 0; i < _prices.Length; i++)
            {
                SaveProvider.SetInt(_keySave + i, _prices[i]);
            }

            SaveProvider.Save();
        }

        private void LoadEquipped()
        {
            _savedEquippedId = SaveProvider.GetInt(_keySaveEquipped);
            _savedEquippedId = Mathf.Clamp(_savedEquippedId, 0,
                _prices != null && _prices.Length > 0 ? _prices.Length - 1 : 0);
            PreviewId = _savedEquippedId;
        }

        public void ShowPreview(int id)
        {
            PreviewId = id;
            VisualPreview();
        }

        private void VisualPreview()
        {
            if (_prices == null || PreviewId < 0 || PreviewId >= _prices.Length)
                return;
            ShopItemData data = _shopItemDatas != null && PreviewId < _shopItemDatas.Length ? _shopItemDatas[PreviewId] : null;
            _shopItemPreview?.Visual(data, _prices[PreviewId], PreviewId);
        }

        public void Buy()
        {
            Buy(PreviewId);
        }

        public void Buy(int id)
        {
            if (_prices == null || id < 0 || id >= _prices.Length)
                return;
            if (NotNullDatas() && id >= _shopItemDatas.Length)
                return;

            if (_prices[id] == 0)
            {
                Visual();
                Select(id);
                ShowPreview(id);
            }
            else if (_money != null && _money.Spend(_prices[id]))
            {
                if (NotNullDatas() && _shopItemDatas[id].isSinglePurchase)
                {
                    _prices[id] = 0;
                }

                Save();
                Visual();
                Select(id);
                OnPurchased?.Invoke(id);
                ShowPreview(id);
            }
            else
            {
                OnPurchaseFailed?.Invoke(id);
                if (_changePreviewOnPurchaseFailed)
                {
                    ShowPreview(id);
                }
            }
        }

        private void Select(int id)
        {
            _id = id;
            SaveProvider.SetInt(_keySaveEquipped, _id);
            OnSelect?.Invoke(id);

            if (_useSetItem)
            {
                for (int i = 0; i < _shopItems.Length; i++)
                {
                    _shopItems[i].Select(i == id);
                }
            }
        }

        public void Visual()
        {
            if (_shopItems == null || _prices == null)
                return;
            for (int i = 0; i < _shopItems.Length; i++)
            {
                ShopItemData data = _shopItemDatas != null && i < _shopItemDatas.Length ? _shopItemDatas[i] : null;
                int price = i < _prices.Length ? _prices[i] : 0;
                _shopItems[i].Visual(data, price, i);
            }
        }

        private bool NotNullDatas()
        {
            return _shopItemDatas != null && _shopItemDatas.Length > 0;
        }
    }
}