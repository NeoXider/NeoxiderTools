using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Bonus
{
    public class ItemCollectionInfo : MonoBehaviour
    {
        [SerializeField] private Collection _collection;
        [SerializeField] private TMP_Text _textName;
        [SerializeField] private TMP_Text _textDescription;
        [TextArea(1, 4)] [SerializeField] private string _textDefaultValue;
        [SerializeField] private Image _imageItem;
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
                _textName.text = itemCollectionData.itemName ?? string.Empty;
            }

            if (_textDescription != null)
            {
                if (!string.IsNullOrEmpty(itemCollectionData.description))
                    _textDescription.text = itemCollectionData.description;
                else
                    _textDescription.text = _textDefaultValue ?? string.Empty;
            }

            if (_imageItem != null)
            {
                if (itemCollectionData.sprite != null)
                {
                    _imageItem.sprite = itemCollectionData.sprite;
                    if (_setNativeSize)
                        _imageItem.SetNativeSize();
                }
            }
        }

        public void SetItemId(int id)
        {
            var collection = CollectionInstance;
            if (collection != null)
            {
                var itemData = collection.GetItemData(id);
                if (itemData != null)
                    SetData(itemData);
            }
        }
    }
}