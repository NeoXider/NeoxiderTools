using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Neo;
using Neo.Extensions;
using Neo.Tools;

namespace Neo.Bonus
{
    public class Collection : Singleton<Collection>
    {
        [SerializeField] private string _saveKeyPrefix = "Collection_";
        [SerializeField] private bool _randomPrize = true;
        [SerializeField] private ItemCollectionData[] _itemCollectionDatas;
        [SerializeField] private bool[] _enabledItems;

        [Space] public UnityEvent<int> OnGetItem;
        public UnityEvent OnLoadItems;
        public UnityEvent<int> OnItemAdded;
        public UnityEvent<int> OnItemRemoved;

        public string SaveKeyPrefix
        {
            get => _saveKeyPrefix;
            set => _saveKeyPrefix = value;
        }

        public bool RandomPrize
        {
            get => _randomPrize;
            set => _randomPrize = value;
        }

        public ItemCollectionData[] ItemCollectionDatas
        {
            get => _itemCollectionDatas;
            set => _itemCollectionDatas = value;
        }

        public bool[] EnabledItems
        {
            get => _enabledItems;
            private set => _enabledItems = value;
        }

        public int ItemCount => _itemCollectionDatas != null ? _itemCollectionDatas.Length : 0;
        public int UnlockedCount => _enabledItems != null ? _enabledItems.Count(x => x) : 0;
        public int LockedCount => ItemCount - UnlockedCount;

        protected override void Init()
        {
            base.Init();
            Load();
        }

        [Button]
        public void Load()
        {
            if (_itemCollectionDatas == null || _itemCollectionDatas.Length == 0)
            {
                _enabledItems = new bool[0];
                OnLoadItems?.Invoke();
                return;
            }

            if (_enabledItems == null || _enabledItems.Length != _itemCollectionDatas.Length)
            {
                _enabledItems = new bool[_itemCollectionDatas.Length];
            }

            for (var i = 0; i < _itemCollectionDatas.Length; i++)
            {
                _enabledItems[i] = PlayerPrefs.GetInt($"{_saveKeyPrefix}Item_{i}", 0) == 1;
            }

            OnLoadItems?.Invoke();
        }

        [Button]
        public void Save()
        {
            if (_enabledItems == null || _itemCollectionDatas == null)
                return;

            for (var i = 0; i < _enabledItems.Length && i < _itemCollectionDatas.Length; i++)
            {
                PlayerPrefs.SetInt($"{_saveKeyPrefix}Item_{i}", _enabledItems[i] ? 1 : 0);
            }

            PlayerPrefs.Save();
        }

        [Button]
        public ItemCollectionData GetPrize()
        {
            if (_itemCollectionDatas == null || _itemCollectionDatas.Length == 0)
                return null;

            var uniqs = _itemCollectionDatas.Where(x => !_enabledItems[Array.IndexOf(_itemCollectionDatas, x)]).ToArray();

            if (uniqs.Length == 0) return null;

            var prizeId = Array.IndexOf(_itemCollectionDatas, _randomPrize ? uniqs.GetRandomElement() : uniqs.First());

            AddItem(prizeId);
            OnGetItem?.Invoke(prizeId);

            return _itemCollectionDatas[prizeId];
        }

        public bool HasItem(int id)
        {
            if (_enabledItems == null || id < 0 || id >= _enabledItems.Length)
                return false;

            return _enabledItems[id];
        }

        public void AddItem(int id)
        {
            if (_enabledItems == null || id < 0 || id >= _enabledItems.Length)
                return;

            if (_enabledItems[id])
                return;

            _enabledItems[id] = true;
            PlayerPrefs.SetInt($"{_saveKeyPrefix}Item_{id}", 1);
            PlayerPrefs.Save();

            OnItemAdded?.Invoke(id);
        }

        public void RemoveItem(int id)
        {
            if (_enabledItems == null || id < 0 || id >= _enabledItems.Length)
                return;

            if (!_enabledItems[id])
                return;

            _enabledItems[id] = false;
            PlayerPrefs.SetInt($"{_saveKeyPrefix}Item_{id}", 0);
            PlayerPrefs.Save();

            OnItemRemoved?.Invoke(id);
        }

        public void SetItemEnabled(int id, bool enabled)
        {
            if (enabled)
                AddItem(id);
            else
                RemoveItem(id);
        }

        [Button]
        public void ClearCollection()
        {
            if (_enabledItems == null)
                return;

            for (var i = 0; i < _enabledItems.Length; i++)
            {
                if (_enabledItems[i])
                {
                    _enabledItems[i] = false;
                    PlayerPrefs.SetInt($"{_saveKeyPrefix}Item_{i}", 0);
                    OnItemRemoved?.Invoke(i);
                }
            }

            PlayerPrefs.Save();
        }

        [Button]
        public void UnlockAllItems()
        {
            if (_itemCollectionDatas == null || _itemCollectionDatas.Length == 0)
                return;

            for (var i = 0; i < _itemCollectionDatas.Length; i++)
            {
                if (!_enabledItems[i])
                    AddItem(i);
            }
        }

        public ItemCollectionData GetItemData(int id)
        {
            if (_itemCollectionDatas == null || id < 0 || id >= _itemCollectionDatas.Length)
                return null;

            return _itemCollectionDatas[id];
        }
    }
}