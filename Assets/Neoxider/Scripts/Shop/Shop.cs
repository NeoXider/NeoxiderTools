using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Neo.Shop
{
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

        [Tooltip("Если true, при загрузке автоматически активируется сохраненный ShopEquipped элемент")]
        [SerializeField] private bool _activateSavedEquipped = true;

        [SerializeField] private string _keySave = "Shop";
        [SerializeField] private string _keySaveEquipped = "ShopEquipped";

        [Tooltip("Если true, при неудачной покупке (недостаточно денег) превью переключится на товар покупки. Если false — превью не изменяется.")]
        [SerializeField] private bool _changePreviewOnPurchaseFailed = false;

        [Space] [Header("Spawn Shop Items")] [SerializeField]
        private Transform _container;

        [SerializeField] private ShopItem _prefab;

        [Space] [SerializeField]
        public GameObject IMoneySpend;

        public int[] Prices => _prices;
        public ShopItemData[] ShopItemDatas => _shopItemDatas;

        [Space] public UnityEvent<int> OnSelect;
        public UnityEvent<int> OnPurchased;
        public UnityEvent<int> OnPurchaseFailed;
        public UnityEvent OnLoad;
        
        private IMoneySpend _money;

        private bool load = false;
        private int _previewId;
        private int _id;
        private int _savedEquippedId;

        public int PreviewId => _previewId;

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
            ShowPreview(_previewId);
        }

        private void Spawn()
        {
            if (_prefab == null) return;

            var shopItemsList = _shopItems?.ToList() ?? new List<ShopItem>();
            var parent = _container != null ? _container : transform;

            for (var i = shopItemsList.Count; i < _prices.Length; i++)
            {
                var newShopItem = Instantiate(_prefab, parent);
                newShopItem.gameObject.SetActive(true);
                shopItemsList.Add(newShopItem);
            }

            _shopItems = shopItemsList.ToArray();

            if (_prefab.gameObject.scene.IsValid()) _prefab.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (IMoneySpend != null)
                _money = IMoneySpend?.GetComponent<IMoneySpend>() ?? Money.I;
            else
                _money = Money.I;
            
            Visual();
            
            // Активируем сохраненный элемент если включена опция
            if (_activateSavedEquipped && _useSetItem)
            {
                var equippedId = Mathf.Clamp(_savedEquippedId, 0, _prices.Length - 1);
                Select(equippedId);
            }

            load = true;
            OnLoad?.Invoke();
        }

        private void Subscriber(bool subscribe)
        {
            for (var i = 0; i < _shopItems.Length; i++)
            {
                var id = i;

                if (subscribe)
                {
                    if (_autoSubscribe)
                        _shopItems[i].buttonBuy.onClick.AddListener(() => Buy(id));
                }
                else
                {
                    if (_autoSubscribe)
                        _shopItems[i].buttonBuy.onClick.RemoveListener(() => Buy(id));
                }
            }
        }

        private void OnDestroy()
        {
            Subscriber(false);
        }

        private void Load()
        {
            var prices = new int[NotNullDatas() ? _shopItemDatas.Length : _prices.Length];

            for (var i = 0; i < _prices.Length; i++)
                prices[i] = PlayerPrefs.GetInt(_keySave + i, NotNullDatas() ? _shopItemDatas[i].price : _prices[i]);

            _prices = prices;
        }

        private void Save()
        {
            for (var i = 0; i < _prices.Length; i++) PlayerPrefs.SetInt(_keySave + i, _prices[i]);
        }

        private void LoadEquipped()
        {
            _savedEquippedId = PlayerPrefs.GetInt(_keySaveEquipped, 0);
            _savedEquippedId = Mathf.Clamp(_savedEquippedId, 0, (_prices != null && _prices.Length > 0) ? _prices.Length - 1 : 0);
            _previewId = _savedEquippedId;
        }

        public void ShowPreview(int id)
        {
            _previewId = id;
            VisualPreview();
        }

        private void VisualPreview()
        {
            var data = _previewId < _shopItemDatas.Length ? _shopItemDatas[_previewId] : null;
            _shopItemPreview?.Visual(data, _prices[_previewId],_previewId);
        }

        public void Buy()
        {
            Buy(_previewId);
        }

        public void Buy(int id)
        {
            if (_prices[id] == 0)
            {
                Visual();

                Select(id);

                // Бесплатный/уже купленный товар — всегда обновляем превью
                ShowPreview(id);
            }
            else if (_money.Spend(_prices[id]))
            {
                if (_shopItemDatas[id].isSinglePurchase)
                    _prices[id] = 0;

                Save();

                Visual();

                Select(id);

                OnPurchased?.Invoke(id);

                // Покупка успешна — обновляем превью
                ShowPreview(id);
            }
            else
            {
                OnPurchaseFailed?.Invoke(id);
                // При неудаче — меняем превью только если включён флаг
                if (_changePreviewOnPurchaseFailed)
                {
                    ShowPreview(id);
                }
            }
        }

        private void Select(int id)
        {
            _id = id;
            PlayerPrefs.SetInt(_keySaveEquipped, _id);
            OnSelect?.Invoke(id);

            if (_useSetItem)
                for (var i = 0; i < _shopItems.Length; i++)
                    _shopItems[i].Select(i == id);
        }

        private void OnEnable()
        {
            if (load)
            {
                Visual();
                // Восстанавливаем состояние выбора после обновления визуала
                if (_useSetItem)
                    Select(_id);
            }
        }

        public void Visual()
        {
            for (var i = 0; i < _shopItems.Length; i++) _shopItems[i].Visual(_shopItemDatas[i], _prices[i],i);
        }

        public void OnValidate() 
        {
            _shopItems ??= GetComponentsInChildren<ShopItem>(true);

            if (NotNullDatas())
                _prices = _shopItemDatas.Select(p => p.price).ToArray();
        }

        private bool NotNullDatas()
        {
            return _shopItemDatas != null && _shopItemDatas.Length > 0;
        }
    }
}