using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Shop
{
    public class Shop : MonoBehaviour
    {
        [SerializeField]
        private ShopItemData[] _shopItemDatas;

        [SerializeField]
        private ShopItem[] _shopItems;

        [SerializeField]
        private bool _useSetItem = true;

        [SerializeField]
        private bool _autoSubscribe = true;

        [SerializeField]
        private string _keySave = "Shop";

        [Space]
        [SerializeField, RequireInterface(typeof(IMoneySpend))]
        private GameObject _IMoneySpend;

        [Space]
        public UnityEvent<int> OnSelect;
        public UnityEvent<int> OnPurchased;
        public UnityEvent<int> OnPurchaseFailed;
        public UnityEvent OnLoad;

        [Space]
        [Header("Debug")]
        [SerializeField]
        private int[] _prices;

        private IMoneySpend _money;

        private bool load = false;

        private void Awake()
        {
            Subscriber(true);
        }

        void Start()
        {
            Load();
            _money = _IMoneySpend.GetComponent<IMoneySpend>();

            if (_useSetItem)
            {
                Select(0);
            }
        }

        private void Subscriber(bool subscribe)
        {
            for (int i = 0; i < _shopItems.Length; i++)
            {
                int id = i;

                if (subscribe)
                {
                    if (_autoSubscribe)
                        _shopItems[i].buttonBuySet.onClick.AddListener(() => Buy(id));
                }
                else
                {
                    if (_autoSubscribe)
                        _shopItems[i].buttonBuySet.onClick.RemoveListener(() => Buy(id));
                }
            }
        }

        private void OnDestroy()
        {
            Subscriber(false);
        }

        private void Load()
        {
            _prices = new int[_shopItemDatas.Length];

            for (int i = 0; i < _prices.Length; i++)
            {
                _prices[i] = PlayerPrefs.GetInt(_keySave + i, _shopItemDatas[i].price);
            }

            load = true;

            Visual();

            OnLoad?.Invoke();
        }

        private void Save()
        {
            for (int i = 0; i < _prices.Length; i++)
            {
                PlayerPrefs.SetInt(_keySave + i, _prices[i]);
            }
        }

        public void Buy(int id)
        {
            if (_prices[id] == 0 && _shopItemDatas[id].isSinglePurchase)
            {
                Visual();

                Select(id);
            }
            else if (_money.Spend(_prices[id]))
            {
                if (_shopItemDatas[id].isSinglePurchase)
                    _prices[id] = 0;

                Save();

                OnPurchased?.Invoke(id);

                Visual();
            }
            else
            {
                OnPurchaseFailed?.Invoke(id);
            }
        }

        private void Select(int id)
        {
            OnSelect?.Invoke(id);

            if (_useSetItem)
            {
                for (int i = 0; i < _shopItems.Length; i++)
                {
                    _shopItems[i].Select(i == id);
                }
            }
        }

        private void OnEnable()
        {
            if (load)
                Visual();
        }

        public void Visual()
        {
            for (int i = 0; i < _shopItems.Length; i++)
            {
                _shopItems[i].Visual(_shopItemDatas[i], _prices[i]);
            }
        }
    }
}
