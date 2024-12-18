using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neoxider.Bonus
{
    public class ItemCollectionInfo : MonoBehaviour
    {
        [SerializeField] private Collection _colllection;
        [SerializeField] private TMP_Text _textName;
        [SerializeField] private TMP_Text _textDescription;
        [TextArea(1,4)]
        [SerializeField] private string _textDefaultValue;
        [SerializeField] private Image _imageItem;
        [SerializeField] private bool _setNativeSize = true;

        public void SetData(ItemCollectionData itemCollectionData)
        {
            if (_textName != null)
                _textName.text = itemCollectionData.itemName;

            if (_textDescription != null)
            {
                if (itemCollectionData.description != string.Empty)
                {
                    _textDescription.text = itemCollectionData.description;
                }
                else
                {
                    _textDescription.text = _textDefaultValue;
                }
            }

            if (_imageItem != null)
            {
                _imageItem.sprite = itemCollectionData.sprite;
                if (_setNativeSize)
                    _imageItem.SetNativeSize();
            }
        }

        public void SetItemId(int id)
        {
            if (_colllection != null)
                SetData(_colllection.itemCollectionDatas[id]);
        }
    }
}
