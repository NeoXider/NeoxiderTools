using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Bonus
{
    [NeoDoc("Bonus/Collection/ItemCollection.md")]
    [CreateFromMenu("Neoxider/Bonus/ItemCollection")]
    [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(ItemCollection))]
    public class ItemCollection : MonoBehaviour
    {
        [Header("References")] [GetComponent] [SerializeField]
        private Button _button;

        [SerializeField] private Image _itemImage;
        [SerializeField] private Collection _collection;
        [SerializeField] private ItemCollectionInfo _itemInfo;

        public UnityEvent<bool> OnChangeEnabled = new();
        public UnityEvent OnActive = new();
        public UnityEvent OnDeactivated = new();
        private ItemCollectionData _currentData;
        private Collection _displayedCollection;
        private bool _isInitialized;

        public Button Button => _button;

        /// <summary>
        ///     Collection <see cref="Unlock" /> writes to: the assigned Collection field, otherwise the collection the
        ///     owning <see cref="CollectionVisualManager" /> displays this view for, otherwise the global
        ///     <c>Collection.I</c>.
        /// </summary>
        private Collection CollectionInstance
        {
            get
            {
                if (_collection != null)
                {
                    return _collection;
                }

                return _displayedCollection != null ? _displayedCollection : Collection.I;
            }
        }
        public int ItemId { get; private set; } = -1;

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
                OnChangeEnabled?.Invoke(active);

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
            ItemId = id;
        }

        /// <summary>Set by the owning manager so a view on a non-global panel unlocks into that panel's collection.</summary>
        internal void SetDisplayedCollection(Collection collection)
        {
            _displayedCollection = collection;
        }

        /// <summary>
        ///     Adds the current item by ItemId to the assigned Collection, otherwise to the collection the owning
        ///     manager displays, otherwise to <c>Collection.I</c>. Handy from UnityEvent (e.g. button).
        /// </summary>
        public void Unlock()
        {
            if (ItemId < 0)
            {
                return;
            }

            Collection col = CollectionInstance;
            if (col != null && col.ItemCount > ItemId)
            {
                col.AddItem(ItemId);
            }
        }
    }
}
