using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Bonus
{
    [NeoDoc("Bonus/Collection/ItemCollectionInfo.md")]
    [CreateFromMenu("Neoxider/Bonus/ItemCollectionInfo")]
    [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(ItemCollectionInfo))]
    public class ItemCollectionInfo : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Collection _collection;

        [SerializeField] private TMP_Text _textName;
        [SerializeField] private TMP_Text _textDescription;
        [SerializeField] private Image _imageItem;

        [Header("Settings")] [TextArea(1, 4)] [SerializeField]
        private string _textDefaultValue;

        [SerializeField] private bool _setNativeSize;

        private Collection CollectionInstance => _collection != null ? _collection : Collection.I;

        public void SetData(ItemCollectionData itemCollectionData)
        {
            if (itemCollectionData == null)
            {
                Debug.LogWarning("[ItemCollectionInfo] SetData: itemCollectionData is null!");
                return;
            }

            if (_textName != null)
            {
                _textName.text = itemCollectionData.ItemName ?? string.Empty;
            }

            if (_textDescription != null)
            {
                if (!string.IsNullOrEmpty(itemCollectionData.Description))
                {
                    _textDescription.text = itemCollectionData.Description;
                }
                else
                {
                    _textDescription.text = _textDefaultValue ?? string.Empty;
                }
            }

            if (_imageItem != null)
            {
                if (itemCollectionData.Sprite != null)
                {
                    _imageItem.sprite = itemCollectionData.Sprite;
                    if (_setNativeSize)
                    {
                        _imageItem.SetNativeSize();
                    }
                }
            }
        }

        public void SetItemId(int id)
        {
            Collection collection = CollectionInstance;
            if (collection != null)
            {
                ItemCollectionData itemData = collection.GetItemData(id);
                if (itemData != null)
                {
                    SetData(itemData);
                }
            }
        }
    }
}