using Neoxider.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neoxider.Shop
{

    [AddComponentMenu("Neoxider/" + "Shop/" + nameof(ShopItem))]
    public class ShopItem : MonoBehaviour
    {

        [SerializeField]
        private int _id;

        [Space]
        [Header("View")]

        [SerializeField]
        private TMP_Text _textName;

        [SerializeField]
        private TMP_Text _textDescription;

        [SerializeField]
        private TMP_Text _textPrice;

        [SerializeField]
        private Image _imageItem;

        [SerializeField]
        private Image _imageIco;

        [SerializeField]
        private ButtonPrice buttonPrice;

        public Button buttonBuySet;

        [Space]
        public UnityEvent OnSelectItem;
        public UnityEvent OnDeselectItem;


        public void Visual(ShopItemData shopItemData, int price)
        {
            if (buttonPrice != null)
                buttonPrice.SetPrice(price);

            if (_textName != null)
                _textName.text = shopItemData.nameItem;

            if (_textDescription != null)
                _textDescription.text = shopItemData.description;

            if (_textPrice != null)
                _textPrice.text = price.ToString();

            if (_imageItem != null)
                _imageItem.sprite = shopItemData.sprite;

            if (_imageIco != null)
                _imageIco.sprite = shopItemData.icon;
        }

        public void Select(bool active)
        {
            if (active)
            {
                if (buttonPrice != null)
                    buttonPrice.TrySetVisualId(2);

                OnSelectItem?.Invoke();
            }
            else
            {
                if (buttonPrice != null)
                    buttonPrice.TrySetVisualId(1);

                OnDeselectItem?.Invoke();
            }
<<<<<<< HEAD
=======

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
>>>>>>> cd70dbfa19a493705dc2252ca7235db4219e024a
        }
    }
}
