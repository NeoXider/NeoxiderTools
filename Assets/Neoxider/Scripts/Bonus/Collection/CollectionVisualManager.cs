using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Neo;
using Neo.Tools;

namespace Neo.Bonus
{
    public class CollectionVisualManager : Singleton<CollectionVisualManager>
    {
        [SerializeField] [GetComponents(true)] private ItemCollection[] _items;

        [SerializeField] private bool _enableSetItem = true;
        public UnityEvent<int> OnSetItem;

        public ItemCollection[] Items => _items;
        public int ItemsCount => _items != null ? _items.Length : 0;

        public bool EnableSetItem
        {
            get => _enableSetItem;
            set => _enableSetItem = value;
        }

        protected override void Init()
        {
            base.Init();
            
            if (_items == null || _items.Length == 0)
            {
                Debug.LogWarning("[CollectionVisualManager] Items array is empty!");
                return;
            }

            Subscriber(true);
        }

        private void Start()
        {
            if (Collection.IsInitialized)
            {
                Collection.I.OnItemAdded.AddListener(OnCollectionItemChanged);
                Collection.I.OnItemRemoved.AddListener(OnCollectionItemChanged);
                Collection.I.OnLoadItems.AddListener(OnCollectionLoaded);
            }
            else
            {
                StartCoroutine(WaitForCollectionAndSubscribe());
            }

            if (Collection.IsInitialized && _items != null && _items.Length > 0)
            {
                Visual();
            }
        }

        private IEnumerator WaitForCollectionAndSubscribe()
        {
            while (!Collection.IsInitialized)
            {
                yield return null;
            }

            Collection.I.OnItemAdded.AddListener(OnCollectionItemChanged);
            Collection.I.OnItemRemoved.AddListener(OnCollectionItemChanged);
            Collection.I.OnLoadItems.AddListener(OnCollectionLoaded);

            if (_items != null && _items.Length > 0)
            {
                Visual();
            }
        }

        private void OnCollectionLoaded()
        {
            if (_items != null && _items.Length > 0)
            {
                Visual();
            }
        }

        private void OnCollectionItemChanged(int id)
        {
            if (_items != null && id >= 0 && id < _items.Length)
            {
                UpdateItemVisibility(id);
            }
        }

        private void OnDestroy()
        {
            Subscriber(false);
            
            if (Collection.IsInitialized)
            {
                Collection.I.OnItemAdded.RemoveListener(OnCollectionItemChanged);
                Collection.I.OnItemRemoved.RemoveListener(OnCollectionItemChanged);
                Collection.I.OnLoadItems.RemoveListener(OnCollectionLoaded);
            }
        }

        private void Subscriber(bool subscribe)
        {
            if (_items == null)
                return;

            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null || _items[i].button == null)
                    continue;

                var id = i;

                if (subscribe)
                    _items[i].button.onClick.AddListener(() => SetItem(id));
                else
                    _items[i].button.onClick.RemoveListener(() => SetItem(id));
            }
        }

        [Button]
        public void Visual()
        {
            if (_items == null || _items.Length == 0)
                return;

            if (!Collection.IsInitialized)
            {
                Debug.LogWarning("[CollectionVisualManager] Collection is not initialized yet!");
                return;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                if (i < Collection.I.ItemCount)
                {
                    UpdateItemVisibility(i);
                }
            }
        }

        public void UpdateItemVisibility(int id)
        {
            if (_items == null || id < 0 || id >= _items.Length)
                return;

            if (!Collection.IsInitialized)
                return;

            if (_items[id] == null)
                return;

            var itemData = Collection.I.GetItemData(id);
            if (itemData != null)
            {
                _items[id].SetData(itemData);
            }

            _items[id].SetEnabled(Collection.I.HasItem(id));
        }

        public void SetItem(int id)
        {
            if (!_enableSetItem || (_enableSetItem && Collection.I.HasItem(id))) OnSetItem?.Invoke(id);
        }

        public ItemCollection GetItem(int id)
        {
            if (_items == null || id < 0 || id >= _items.Length)
                return null;

            return _items[id];
        }

        [Button]
        public void RefreshAllItems()
        {
            Visual();
        }

        public void RefreshItem(int id)
        {
            UpdateItemVisibility(id);
        }

        public void SetItem(ItemCollection itemCollection)
        {
            var id = Array.IndexOf(_items, itemCollection);
            SetItem(id);
        }
    }
}