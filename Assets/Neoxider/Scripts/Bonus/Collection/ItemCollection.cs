using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Bonus
{
    public class ItemCollection : MonoBehaviour
    {
        [GetComponent] public Button button;
        [SerializeField] private Image _itemImage;
        [SerializeField] private ItemCollectionInfo _itemInfo;

        public UnityEvent<bool> OnChangeEnabled;
        public UnityEvent OnActive;
        public UnityEvent OnDeactivated;

        public Image ItemImage => _itemImage;
        public ItemCollectionInfo ItemInfo => _itemInfo;
        public bool IsEnabled { get; private set; }

        public void SetEnabled(bool active)
        {
            if (IsEnabled == active)
                return;

            IsEnabled = active;
            OnChangeEnabled.Invoke(active);

            if (active)
                OnActive?.Invoke();
            else
                OnDeactivated?.Invoke();
        }

        public void SetSprite(Sprite sprite)
        {
            if (_itemImage != null)
                _itemImage.sprite = sprite;
        }

        public void SetData(ItemCollectionData itemCollectionData)
        {
            if (itemCollectionData != null)
            {
                SetSprite(itemCollectionData.sprite);
                
                if (_itemInfo != null)
                    _itemInfo.SetData(itemCollectionData);
            }
        }

        public ItemCollectionData GetCurrentData()
        {
            // Можно расширить, если нужно хранить текущие данные
            return null;
        }
    }
}