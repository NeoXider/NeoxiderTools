using Neo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Shop
{
    [NeoDoc("Shop/ShopItem.md")]
    [CreateFromMenu("Neoxider/Shop/ShopItem")]
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

        [Header("SpriteRenderers (optional)")] [SerializeField]
        private SpriteRenderer _spriteRendererItem;

        [SerializeField] private SpriteRenderer _spriteRendererIcon;

        [SerializeField] private ButtonPrice buttonPrice;

        public Button buttonBuy;

        [Space] public UnityEvent OnSelectItem;
        public UnityEvent OnDeselectItem;

        [System.Obsolete("Use BoundItemId / BoundItemData / BoundBundleData. Integer view ids will be removed in v9.")]
        public int LegacyId => _id;

        public string BoundItemId { get; private set; } = "";
        public ShopItemData BoundItemData { get; private set; }
        public ShopBundleData BoundBundleData { get; private set; }

        private void OnValidate()
        {
            buttonBuy ??= GetComponentInChildren<Button>(true);
        }


        public void Visual(ShopItemData shopItemData, int price)
        {
            VisualInternal(shopItemData, null, price, -1);
        }

        [System.Obsolete("Use Visual(ShopItemData, int). Integer view ids will be removed in v9.")]
        public void Visual(ShopItemData shopItemData, int price, int id)
        {
            VisualInternal(shopItemData, null, price, id);
        }

        /// <summary>
        ///     Visualises a <see cref="ShopBundleData"/> in this slot — uses bundle name, description,
        ///     sprite, and icon. Useful for bundle UI tabs / dialogs alongside the regular item list.
        /// </summary>
        public void Visual(ShopBundleData bundleData, int price)
        {
            VisualInternal(null, bundleData, price, -1);
        }

        [System.Obsolete("Use Visual(ShopBundleData, int). Integer view ids will be removed in v9.")]
        public void Visual(ShopBundleData bundleData, int price, int id)
        {
            VisualInternal(null, bundleData, price, id);
        }

        private void VisualInternal(ShopItemData shopItemData, ShopBundleData bundleData, int price, int legacyId)
        {
            _id = legacyId;
            BoundItemData = shopItemData;
            BoundBundleData = bundleData;
            BoundItemId = shopItemData != null ? shopItemData.Id : bundleData != null ? bundleData.Id : "";
            ApplyPrice(price);

            if (shopItemData != null)
            {
                ApplyDisplay(shopItemData.nameItem, shopItemData.description, shopItemData.sprite, shopItemData.icon);
            }
            else if (bundleData != null)
            {
                ApplyDisplay(bundleData.nameBundle, bundleData.description, bundleData.sprite, bundleData.icon);
            }
            else
            {
                ApplyDisplay("", "", null, null);
            }
        }

        public void Clear()
        {
            _id = -1;
            BoundItemData = null;
            BoundBundleData = null;
            BoundItemId = "";
            ApplyDisplay("", "", null, null);
            ApplyPrice(0);
            Select(false);
        }

        private void ApplyPrice(int price)
        {
            if (buttonPrice != null)
            {
                buttonPrice.SetPrice(price);
            }

            if (_textPrice != null)
            {
                _textPrice.text = price.ToString();
            }
        }

        private void ApplyDisplay(string name, string description, Sprite sprite, Sprite icon)
        {
            if (_textName != null)
            {
                _textName.text = name;
            }

            if (_textDescription != null)
            {
                _textDescription.text = description;
            }

            if (_imageItem != null)
            {
                _imageItem.sprite = sprite;
            }

            if (_spriteRendererItem != null)
            {
                _spriteRendererItem.sprite = sprite;
            }

            if (_imageIco != null)
            {
                _imageIco.sprite = icon;
            }

            if (_spriteRendererIcon != null)
            {
                _spriteRendererIcon.sprite = icon;
            }
        }

        public void Select(bool active)
        {
            if (active)
            {
                if (buttonPrice != null)
                {
                    buttonPrice.TrySetVisualId(2);
                }

                OnSelectItem?.Invoke();
            }
            else
            {
                if (buttonPrice != null)
                {
                    buttonPrice.TrySetVisualId(1);
                }

                OnDeselectItem?.Invoke();
            }
        }
    }
}
