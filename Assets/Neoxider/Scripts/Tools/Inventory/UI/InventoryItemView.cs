using TMPro;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Tools
{
    [NeoDoc("Tools/Inventory/InventoryView.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryItemView")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryItemView))]
    public sealed class InventoryItemView : MonoBehaviour
    {
        [Header("Data")] [SerializeField] [Tooltip("Default item id used in manual InventoryView mode.")]
        private int _itemId = -1;

        [Header("Optional UI")] [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private GameObject _root;
        [SerializeField] private string _countFormat = "{0}";

        public int BoundItemId => _itemId;

        public void Bind(InventoryItemData itemData, int itemId, int count)
        {
            _itemId = itemId;

            if (_root != null)
            {
                _root.SetActive(true);
            }

            if (_iconImage != null)
            {
                Sprite icon = itemData != null ? itemData.Icon : null;
                if (icon == null && itemData != null && itemData.WorldDropPrefab != null)
                {
                    icon = itemData.WorldDropPrefab.GetPreviewSprite();
                }

                _iconImage.enabled = icon != null;
                _iconImage.sprite = icon;
            }

            if (_nameText != null)
            {
                _nameText.text = itemData != null ? itemData.DisplayName : itemId.ToString();
            }

            if (_countText != null)
            {
                _countText.text = string.Format(_countFormat, count);
            }
        }

        public void Clear()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void SetItemId(int itemId)
        {
            _itemId = itemId;
        }
    }
}
