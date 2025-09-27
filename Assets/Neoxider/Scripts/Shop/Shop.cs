using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

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

        [SerializeField] private string _keySave = "Shop";

        [Space] [Header("Spawn Shop Items")] [SerializeField]
        private Transform _container;

        [SerializeField] private ShopItem _prefab;

        [Space] [SerializeField] [RequireInterface(typeof(IMoneySpend))]
        private GameObject _IMoneySpend;

        [Space] public UnityEvent<int> OnSelect;
        public UnityEvent<int> OnPurchased;
        public UnityEvent<int> OnPurchaseFailed;
        public UnityEvent OnLoad;
        private int _id;

        [Space] [Header("Debug")] private IMoneySpend _money;

        private bool load;

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
            Spawn();
            Subscriber(true);
            ShowPreview(PreviewId);
        }

        private void Start()
        {
            if (_IMoneySpend != null)
                _money = _IMoneySpend?.GetComponent<IMoneySpend>() ?? Money.I;
            else
                _money = Money.I;

            Visual();

            if (_useSetItem) Select(0);

            load = true;
            OnLoad?.Invoke();
        }

        private void OnEnable()
        {
            if (load)
                Visual();
        }

        private void OnDestroy()
        {
            Subscriber(false);
        }

        public void OnValidate()
        {
            _shopItems ??= GetComponentsInChildren<ShopItem>(true);

            if (NotNullDatas())
                _prices = _shopItemDatas.Select(p => p.price).ToArray();
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

        public void ShowPreview(int id)
        {
            PreviewId = id;
            VisualPreview();
        }

        private void VisualPreview()
        {
            var data = PreviewId < _shopItemDatas.Length ? _shopItemDatas[PreviewId] : null;
            _shopItemPreview?.Visual(data, _prices[PreviewId], PreviewId);
        }

        public void Buy()
        {
            Buy(PreviewId);
        }

        public void Buy(int id)
        {
            if (_prices[id] == 0)
            {
                Visual();

                Select(id);
            }
            else if (_money.Spend(_prices[id]))
            {
                if (_shopItemDatas[id].isSinglePurchase)
                    _prices[id] = 0;

                Save();

                Visual();

                Select(id);

                OnPurchased?.Invoke(id);
            }
            else
            {
                OnPurchaseFailed?.Invoke(id);
            }

            ShowPreview(id);
        }

        private void Select(int id)
        {
            _id = id;
            OnSelect?.Invoke(id);

            if (_useSetItem)
                for (var i = 0; i < _shopItems.Length; i++)
                    _shopItems[i].Select(i == id);
        }

        public void Visual()
        {
            for (var i = 0; i < _shopItems.Length; i++) _shopItems[i].Visual(_shopItemDatas[i], _prices[i], i);
        }

        private bool NotNullDatas()
        {
            return _shopItemDatas != null && _shopItemDatas.Length > 0;
        }
    }
}