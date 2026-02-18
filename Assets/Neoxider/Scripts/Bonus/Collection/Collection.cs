using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using Neo.Save;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

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

        [Space] [Tooltip("Invoked when collection composition changes: (unlockedCount, totalCount).")]
        public UnityEvent<int, int> OnCompletionChanged;

        [Tooltip("Invoked when collection composition changes: ratio 0–1.")]
        public UnityEvent<float> OnCompletionPercentageChanged;

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

        /// <summary>True, если этот экземпляр зарегистрирован как Collection.I (глобальный синглтон). При false — работа по ссылке для нескольких коллекций.</summary>
        public bool IsSingleton => SetInstanceOnAwakeEnabled;

        /// <summary>Доля завершения коллекции (0–1). При пустой коллекции — 0.</summary>
        public float GetCompletionPercentage()
        {
            int total = ItemCount;
            return total == 0 ? 0f : (float)UnlockedCount / total;
        }

        /// <summary>Строка вида "1/5" для отображения в UI.</summary>
        public string GetCompletionCountText()
        {
            return $"{UnlockedCount}/{ItemCount}";
        }

        private void InvokeCompletionEvents()
        {
            int unlocked = UnlockedCount;
            int total = ItemCount;
            OnCompletionChanged?.Invoke(unlocked, total);
            OnCompletionPercentageChanged?.Invoke(GetCompletionPercentage());
        }

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
                InvokeCompletionEvents();
                return;
            }

            if (_enabledItems == null || _enabledItems.Length != _itemCollectionDatas.Length)
            {
                _enabledItems = new bool[_itemCollectionDatas.Length];
            }

            for (int i = 0; i < _itemCollectionDatas.Length; i++)
            {
                _enabledItems[i] = SaveProvider.GetInt($"{_saveKeyPrefix}Item_{i}") == 1;
            }

            OnLoadItems?.Invoke();
            InvokeCompletionEvents();
        }

        [Button]
        public void Save()
        {
            if (_enabledItems == null || _itemCollectionDatas == null)
            {
                return;
            }

            for (int i = 0; i < _enabledItems.Length && i < _itemCollectionDatas.Length; i++)
            {
                SaveProvider.SetInt($"{_saveKeyPrefix}Item_{i}", _enabledItems[i] ? 1 : 0);
            }

            SaveProvider.Save();
        }

        [Button]
        public ItemCollectionData GetPrize()
        {
            if (_itemCollectionDatas == null || _itemCollectionDatas.Length == 0 ||
                _enabledItems == null || _enabledItems.Length != _itemCollectionDatas.Length)
            {
                return null;
            }

            var lockedIndices = new List<int>();
            for (int i = 0; i < _enabledItems.Length; i++)
            {
                if (!_enabledItems[i])
                {
                    lockedIndices.Add(i);
                }
            }

            if (lockedIndices.Count == 0)
            {
                return null;
            }

            int prizeId = _randomPrize ? lockedIndices.GetRandomElement() : lockedIndices[0];

            AddItem(prizeId);
            OnGetItem?.Invoke(prizeId);

            return _itemCollectionDatas[prizeId];
        }

        public bool HasItem(int id)
        {
            if (_enabledItems == null || id < 0 || id >= _enabledItems.Length)
            {
                return false;
            }

            return _enabledItems[id];
        }

        public void AddItem(int id)
        {
            TryAddItem(id);
        }

        /// <summary>Добавляет предмет по индексу. Возвращает true, если предмет был добавлен (раньше не был разблокирован).</summary>
        public bool TryAddItem(int id)
        {
            if (_enabledItems == null || id < 0 || id >= _enabledItems.Length)
            {
                return false;
            }

            if (_enabledItems[id])
            {
                return false;
            }

            _enabledItems[id] = true;
            SaveProvider.SetInt($"{_saveKeyPrefix}Item_{id}", 1);
            SaveProvider.Save();

            OnItemAdded?.Invoke(id);
            InvokeCompletionEvents();
            return true;
        }

        /// <summary>Добавляет предмет по данным (ищет индекс в ItemCollectionDatas).</summary>
        public void AddItem(ItemCollectionData data)
        {
            if (data == null || _itemCollectionDatas == null)
            {
                return;
            }

            int index = Array.IndexOf(_itemCollectionDatas, data);
            if (index >= 0)
            {
                AddItem(index);
            }
        }

        public void RemoveItem(int id)
        {
            if (_enabledItems == null || id < 0 || id >= _enabledItems.Length)
            {
                return;
            }

            if (!_enabledItems[id])
            {
                return;
            }

            _enabledItems[id] = false;
            SaveProvider.SetInt($"{_saveKeyPrefix}Item_{id}", 0);
            SaveProvider.Save();

            OnItemRemoved?.Invoke(id);
            InvokeCompletionEvents();
        }

        public void SetItemEnabled(int id, bool enabled)
        {
            if (enabled)
            {
                AddItem(id);
            }
            else
            {
                RemoveItem(id);
            }
        }

        [Button]
        public void ClearCollection()
        {
            if (_enabledItems == null || _itemCollectionDatas == null)
            {
                return;
            }

            int count = Mathf.Min(_enabledItems.Length, _itemCollectionDatas.Length);
            for (int i = 0; i < count; i++)
            {
                if (_enabledItems[i])
                {
                    _enabledItems[i] = false;
                    SaveProvider.SetInt($"{_saveKeyPrefix}Item_{i}", 0);
                    OnItemRemoved?.Invoke(i);
                }
            }

            SaveProvider.Save();
            InvokeCompletionEvents();
        }

        [Button]
        public void UnlockAllItems()
        {
            if (_itemCollectionDatas == null || _itemCollectionDatas.Length == 0)
            {
                return;
            }

            if (_enabledItems == null || _enabledItems.Length != _itemCollectionDatas.Length)
            {
                _enabledItems = new bool[_itemCollectionDatas.Length];
            }

            for (int i = 0; i < _itemCollectionDatas.Length; i++)
            {
                if (!_enabledItems[i])
                {
                    AddItem(i);
                }
            }

            InvokeCompletionEvents();
        }

        public ItemCollectionData GetItemData(int id)
        {
            if (_itemCollectionDatas == null || id < 0 || id >= _itemCollectionDatas.Length)
            {
                return null;
            }

            return _itemCollectionDatas[id];
        }

        public int[] GetUnlockedIds()
        {
            if (_enabledItems == null || _itemCollectionDatas == null)
            {
                return Array.Empty<int>();
            }

            var list = new List<int>();
            int count = Mathf.Min(_enabledItems.Length, _itemCollectionDatas.Length);
            for (int i = 0; i < count; i++)
            {
                if (_enabledItems[i])
                {
                    list.Add(i);
                }
            }

            return list.ToArray();
        }

        public int[] GetLockedIds()
        {
            if (_enabledItems == null || _itemCollectionDatas == null)
            {
                return Array.Empty<int>();
            }

            var list = new List<int>();
            int count = Mathf.Min(_enabledItems.Length, _itemCollectionDatas.Length);
            for (int i = 0; i < count; i++)
            {
                if (!_enabledItems[i])
                {
                    list.Add(i);
                }
            }

            return list.ToArray();
        }

        public int[] GetIdsByCategory(int category)
        {
            if (_itemCollectionDatas == null)
            {
                return Array.Empty<int>();
            }

            var list = new List<int>();
            for (int i = 0; i < _itemCollectionDatas.Length; i++)
            {
                if (_itemCollectionDatas[i] != null && _itemCollectionDatas[i].Category == category)
                {
                    list.Add(i);
                }
            }

            return list.ToArray();
        }

        public int[] GetIdsByRarity(ItemRarity rarity)
        {
            if (_itemCollectionDatas == null)
            {
                return Array.Empty<int>();
            }

            var list = new List<int>();
            for (int i = 0; i < _itemCollectionDatas.Length; i++)
            {
                if (_itemCollectionDatas[i] != null && _itemCollectionDatas[i].Rarity == rarity)
                {
                    list.Add(i);
                }
            }

            return list.ToArray();
        }

        public int[] GetIdsByType(int itemType)
        {
            if (_itemCollectionDatas == null)
            {
                return Array.Empty<int>();
            }

            var list = new List<int>();
            for (int i = 0; i < _itemCollectionDatas.Length; i++)
            {
                if (_itemCollectionDatas[i] != null && _itemCollectionDatas[i].ItemType == itemType)
                {
                    list.Add(i);
                }
            }

            return list.ToArray();
        }

        public int GetUnlockedCountByCategory(int category)
        {
            if (_enabledItems == null || _itemCollectionDatas == null)
            {
                return 0;
            }

            int n = 0;
            int count = Mathf.Min(_enabledItems.Length, _itemCollectionDatas.Length);
            for (int i = 0; i < count; i++)
            {
                if (_enabledItems[i] && _itemCollectionDatas[i] != null && _itemCollectionDatas[i].Category == category)
                {
                    n++;
                }
            }

            return n;
        }

        public int GetUnlockedCountByRarity(ItemRarity rarity)
        {
            if (_enabledItems == null || _itemCollectionDatas == null)
            {
                return 0;
            }

            int n = 0;
            int count = Mathf.Min(_enabledItems.Length, _itemCollectionDatas.Length);
            for (int i = 0; i < count; i++)
            {
                if (_enabledItems[i] && _itemCollectionDatas[i] != null && _itemCollectionDatas[i].Rarity == rarity)
                {
                    n++;
                }
            }

            return n;
        }

        public int GetUnlockedCountByType(int itemType)
        {
            if (_enabledItems == null || _itemCollectionDatas == null)
            {
                return 0;
            }

            int n = 0;
            int count = Mathf.Min(_enabledItems.Length, _itemCollectionDatas.Length);
            for (int i = 0; i < count; i++)
            {
                if (_enabledItems[i] && _itemCollectionDatas[i] != null && _itemCollectionDatas[i].ItemType == itemType)
                {
                    n++;
                }
            }

            return n;
        }
    }
}