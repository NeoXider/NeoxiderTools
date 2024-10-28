using Neoxider.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neoxider
{
    namespace Shop
    {
        [AddComponentMenu("Neoxider/" + "Shop/" + nameof(ShopItem))]
        public class ShopItem : MonoBehaviour
        {
            [SerializeField]
            private ShopItemData _shopItemData;

            [SerializeField]
            private bool _purchasedUsed = false;

            [SerializeField]
            private bool _startUpdateVisual = true;

            [SerializeField]
            private Image _imageItem;

            [SerializeField]
            private int _id;

            [Space, SerializeField]
            private bool _saveUseId = false;

            [Space, SerializeField, RequireInterface(typeof(IMoneySpend))]
            private GameObject _IMoneySpend;

            [Space, Header("Events")]
            public UnityEvent<int> OnChangeId;
            public UnityEvent<int> OnChangePrice;

            [Space, Header("Money")]
            public UnityEvent OnPurchased;
            public UnityEvent OnPurchaseFailed;

            [Space, Header("Activ Item")]
            public UnityEvent<int> OnSelect;
            public UnityEvent<int> OnUse;

            private IMoneySpend _money;
            private int _useId;

            private void Awake()
            {
                _money = _IMoneySpend.GetComponent<IMoneySpend>();
            }

            void Start()
            {
                _shopItemData.Load();

                if (_startUpdateVisual)
                    UpdateVisual();
            }

            public void AddId()
            {
                ChangeId(_id + 1);
            }

            public void RemoveId()
            {
                ChangeId(_id - 1);
            }

            public void ChangeId(int id)
            {
                _id = Mathf.Clamp(id, 0, _shopItemData.curentPrice.Length - 1);

                UpdateVisual();

                if (_saveUseId)
                {
                    if (_useId == _id && _shopItemData.isSinglePurchase)
                    {
                        Used();
                    }
                }
            }

            public void SetItem()
            {
                int price = _shopItemData.curentPrice[_id];

                if (price > 0)
                {
                    Buy(price);
                }
                else // is selected
                {
                    Used();
                }
            }

            private void Buy(int price)
            {
                if (_money.Spend(price))
                {
                    _shopItemData.Save(_id);
                    UpdateVisual();

                    if (_purchasedUsed)
                        Used();
                    else
                        Select();

                    OnPurchased?.Invoke();
                }
                else
                {
                    OnPurchaseFailed?.Invoke();
                }
            }

            private void Select()
            {
                OnSelect?.Invoke(_id);
            }

            private void Used()
            {
                _useId = _id;
                OnUse?.Invoke(_id);
            }

            public void UpdateVisual()
            {
                if (_imageItem != null)
                    _imageItem.sprite = _shopItemData.spritesShop[_id];

                OnChangePrice?.Invoke(_shopItemData.curentPrice[_id]);
                OnChangeId?.Invoke(_id);
            }

            private void OnValidate()
            {
                if (_shopItemData != null)
                    _id = Mathf.Clamp(_id, 0, _shopItemData.price.Length - 1);

                if (_imageItem != null)
                    _imageItem.sprite = _shopItemData.spritesShop[_id];
            }


        }
    }
}
