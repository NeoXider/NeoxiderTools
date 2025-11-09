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
        private ItemCollectionData _currentData;
        private bool _isInitialized;

        private void Awake()
        {
            IsEnabled = false;
            _isInitialized = false;
        }

        public void SetEnabled(bool active)
        {
            if (!_isInitialized || IsEnabled != active)
            {
                IsEnabled = active;
                _isInitialized = true;
                OnChangeEnabled.Invoke(active);

                if (active)
                    OnActive?.Invoke();
                else
                    OnDeactivated?.Invoke();
            }
        }

        public void SetSprite(Sprite sprite)
        {
            if (_itemImage != null)
                _itemImage.sprite = sprite;
        }

        public void SetData(ItemCollectionData itemCollectionData)
        {
            if (itemCollectionData == null)
                return;

            _currentData = itemCollectionData;
            SetSprite(itemCollectionData.sprite);
            
            if (_itemInfo != null)
            {
                _itemInfo.SetData(itemCollectionData);
            }
        }

        public ItemCollectionData GetCurrentData()
        {
            return _currentData;
        }
    }
}