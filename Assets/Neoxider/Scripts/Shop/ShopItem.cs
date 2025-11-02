using System;
using Neo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Neo.Shop
{
    [AddComponentMenu("Neoxider/" + "Shop/" + nameof(ShopItem))]
    public class ShopItem : MonoBehaviour
    {
        [SerializeField] private int _id;

        [Space] [Header("View")] [SerializeField]
        private TMP_Text _textName;

        [SerializeField] private TMP_Text _textDescription;

        [SerializeField] private TMP_Text _textPrice;

        [SerializeField] private Image _imageItem;

        [SerializeField] private Image _imageIco;

        [Header("SpriteRenderers (optional)")]
        [SerializeField] private SpriteRenderer _spriteRendererItem;
        [SerializeField] private SpriteRenderer _spriteRendererIcon;

        [SerializeField] private ButtonPrice buttonPrice;

        public Button buttonBuy;

        [Space] public UnityEvent OnSelectItem;
        public UnityEvent OnDeselectItem;


        public void Visual(ShopItemData shopItemData, int price, int id)
        {
            if (buttonPrice != null)
            {
                buttonPrice.SetPrice(price);
            }

            if (_textPrice != null)
                _textPrice.text = price.ToString();

            if (shopItemData != null)
            {
                if (_textName != null)
                    _textName.text = shopItemData.nameItem;

                if (_textDescription != null)
                    _textDescription.text = shopItemData.description;

                if (_imageItem != null)
                    _imageItem.sprite = shopItemData.sprite;

                if (_spriteRendererItem != null)
                    _spriteRendererItem.sprite = shopItemData.sprite;

                if (_imageIco != null)
                    _imageIco.sprite = shopItemData.icon;

                if (_spriteRendererIcon != null)
                    _spriteRendererIcon.sprite = shopItemData.icon;
            }
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
        }

        private void OnValidate()
        {
            buttonBuy ??= GetComponentInChildren<Button>(true);
        }
    }
}