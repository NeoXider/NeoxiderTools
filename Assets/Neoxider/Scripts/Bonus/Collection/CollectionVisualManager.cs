using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Bonus
{
    public class CollectionVisualManager : MonoBehaviour
    {
        public static CollectionVisualManager Instance;
        [SerializeField, GetComponents(true)]private ItemCollection[] _items;

        [SerializeField] private bool enableSetItem = true;
        public UnityEvent<int> OnSetItem;

        private void Awake()
        {
            Instance = this;

            Subscriber(true);
        }

        private void Subscriber(bool subscribe)
        {
            for (int i = 0; i < _items.Length; i++)
            {
                int id = i;

                if (subscribe)
                {
                    _items[i].button.onClick.AddListener(() => SetItem(id));
                }
                else
                {
                    _items[i].button.onClick.RemoveListener(() => SetItem(id));
                }
            }
        }

        private void OnDestroy() 
        {
            Subscriber(false);    
        }

        private void OnEnable()
        {
            Visual();
        }

        public void Visual()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                UpdateItemVisibility(i);
            }
        }

        public void UpdateItemVisibility(int id)
        {
            if (_items.Length > id && _items[id] != null)
            {
                _items[id].SetData(Collection.Instance.itemCollectionDatas[id]);
                _items[id].SetEnabled(Collection.Instance.enabledItems[id]);
            }
        }

        public void SetItem(int id)
        {
            if (!enableSetItem || (enableSetItem && Collection.Instance.enabledItems[id]))
            {
                OnSetItem?.Invoke(id);
            }
        }

        public void SetItem(ItemCollection itemCollection)
        {
            int id = Array.IndexOf(_items, itemCollection);
            SetItem(id);
        }
    }
}