using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Bonus
{
    [NeoDoc("Bonus/Collection/ItemCollection.md")]
    [AddComponentMenu("Neo/" + "Bonus/" + nameof(ItemCollection))]
    public class ItemCollection : MonoBehaviour
    {
        [Header("References")] [GetComponent] [SerializeField] private Button _button;
        [SerializeField] private Image _itemImage;
        [SerializeField] private Collection _collection;
        [SerializeField] private ItemCollectionInfo _itemInfo;

        public Button Button => _button;
        private int _itemId = -1;

        private Collection CollectionInstance => _collection != null ? _collection : Collection.I;
        public int ItemId => _itemId;

        public UnityEvent<bool> OnChangeEnabled;
        public UnityEvent OnActive;
        public UnityEvent OnDeactivated;
        private ItemCollectionData _currentData;
        private bool _isInitialized;

        public Image ItemImage => _itemImage;
        public ItemCollectionInfo ItemInfo => _itemInfo;
        public bool IsEnabled { get; private set; }

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
                {
                    OnActive?.Invoke();
                }
                else
                {
                    OnDeactivated?.Invoke();
                }
            }
        }

        public void SetSprite(Sprite sprite)
        {
            if (_itemImage != null)
            {
                _itemImage.sprite = sprite;
            }
        }

        public void SetData(ItemCollectionData itemCollectionData)
        {
            if (itemCollectionData == null)
            {
                return;
            }

            _currentData = itemCollectionData;
            SetSprite(itemCollectionData.Sprite);

            if (_itemInfo != null)
            {
                _itemInfo.SetData(itemCollectionData);
            }
        }

        public ItemCollectionData GetCurrentData()
        {
            return _currentData;
        }

        public void SetItemId(int id)
        {
            _itemId = id;
        }

        /// <summary>Добавляет текущий предмет в коллекцию (по ItemId). Удобно вызывать из UnityEvent (кнопка).</summary>
        public void Unlock()
        {
            if (_itemId < 0)
            {
                return;
            }

            Collection col = CollectionInstance;
            if (col != null && col.ItemCount > _itemId)
            {
                col.AddItem(_itemId);
            }
        }
    }
}