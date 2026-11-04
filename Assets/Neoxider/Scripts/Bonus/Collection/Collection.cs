using System;
using System.Collections.Generic;
using Neo.Extensions;
using Neo.Save;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Bonus
{
    [CreateFromMenu("Neoxider/Bonus/Collection")]
    [AddComponentMenu("Neoxider/Bonus/" + nameof(Collection))]
    [NeoDoc("Bonus/Collection/README.md")]
    public class Collection : Singleton<Collection>
    {
        [SerializeField] private string _saveKeyPrefix = "Collection_";
        [SerializeField] private bool _randomPrize = true;
        [SerializeField] private ItemCollectionData[] _itemCollectionDatas;
        [SerializeField] private bool[] _enabledItems;

        [Space] public UnityEvent<int> OnGetItem = new();
        public UnityEvent OnLoadItems = new();
        public UnityEvent<int> OnItemAdded = new();
        public UnityEvent<int> OnItemRemoved = new();

        [Space] [Tooltip("Invoked when collection composition changes: (unlockedCount, totalCount).")]
        public UnityEvent<int, int> OnCompletionChanged = new();

        [Tooltip("Invoked when collection composition changes: ratio 0–1.")]
        public UnityEvent<float> OnCompletionPercentageChanged = new();

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
        public int UnlockedCount => _enabledItems != null ? EnsureSet().UnlockedCount : 0;
        public int LockedCount => ItemCount - UnlockedCount;

        /// <summary>
        ///     Read-only view of the plain C# state behind this component (same storage as <see cref="EnabledItems" />).
        ///     Subscribe to its C# events from code; they are raised after this component has persisted the change and
        ///     invoked the matching UnityEvent. Change state through this component (<see cref="TryAddItem(int)" />,
        ///     <see cref="RemoveItem" />, ...) so it is saved and announced. For independent sets that need no scene
        ///     object, create a <see cref="CollectionSet" /> directly.
        /// </summary>
        public IReadOnlyCollectionSet Set => EnsureSet();

        /// <summary>
        ///     Optional random source for <see cref="GetPrize()" /> returning an integer in <c>[0, maxExclusive)</c>.
        ///     Null uses <c>UnityEngine.Random</c>. Inject one for deterministic tests or server-authored prizes. A value
        ///     outside the range throws <see cref="InvalidOperationException" /> before anything is unlocked.
        /// </summary>
        public Func<int, int> RandomIndexSource { get; set; }

        private CollectionSet _set;

        /// <summary>
        ///     True when this instance is registered as Collection.I (global singleton). When false, use by reference for
        ///     multiple collections; such an instance still loads its saved unlocks in <c>Awake</c>.
        /// </summary>
        public bool IsSingleton => SetInstanceOnAwakeEnabled;

        /// <summary>Collection completion ratio (0–1). Returns 0 when the collection is empty.</summary>
        public float GetCompletionPercentage()
        {
            int total = ItemCount;
            return total == 0 ? 0f : (float)UnlockedCount / total;
        }

        /// <summary>String like "1/5" for UI display.</summary>
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
            EnsureSet().RaiseCompletionChanged();
        }

        private CollectionSet EnsureSet()
        {
            if (_set == null)
            {
                // WHY: no save prefix; the component persists every change itself and orders its UnityEvents.
                _set = new CollectionSet(_enabledItems, null, IsCatalogSlotAssigned);
            }
            else if (!_set.UsesStorage(_enabledItems))
            {
                // WHY: Load/UnlockAllItems may replace the serialized array; keep the same core so subscribers survive.
                _set.AdoptStorage(_enabledItems);
            }

            return _set;
        }

        private bool IsCatalogSlotAssigned(int index)
        {
            return _itemCollectionDatas != null && index < _itemCollectionDatas.Length &&
                   _itemCollectionDatas[index] != null;
        }

        protected override void Awake()
        {
            base.Awake();
            // WHY: Singleton.Awake initializes only the instance it registers as Collection.I. A collection kept out
            // of the global slot (Set Instance On Awake off) would otherwise never load its saved unlocks.
            if (!SetInstanceOnAwakeEnabled)
            {
                Init();
            }
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
                _enabledItems[i] = SaveProvider.GetInt(CollectionSet.GetItemSaveKey(_saveKeyPrefix, i)) == 1;
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
                SaveProvider.SetInt(CollectionSet.GetItemSaveKey(_saveKeyPrefix, i), _enabledItems[i] ? 1 : 0);
            }
        }

        [Button]
        public ItemCollectionData GetPrize()
        {
            return GetPrize(null);
        }

        /// <summary>Grants a prize; <paramref name="onCommitted" /> settles the caller's cost after the unlock, before notifications.</summary>
        internal ItemCollectionData GetPrize(Action onCommitted)
        {
            if (_itemCollectionDatas == null || _itemCollectionDatas.Length == 0 ||
                _enabledItems == null || _enabledItems.Length != _itemCollectionDatas.Length)
            {
                return null;
            }

            int prizeId = EnsureSet().PickLockedIndex(_randomPrize, RandomIndexSource);
            if (prizeId < 0)
            {
                return null;
            }

            ItemCollectionData prize = _itemCollectionDatas[prizeId];

            if (!TryAddItem(prizeId, onCommitted))
            {
                return null;
            }

            OnGetItem?.Invoke(prizeId);

            return prize;
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

        /// <summary>Adds an item by index. Returns true if it was newly unlocked.</summary>
        public bool TryAddItem(int id)
        {
            return TryAddItem(id, null);
        }

        private bool TryAddItem(int id, Action onCommitted)
        {
            if (_enabledItems == null || _itemCollectionDatas == null || id < 0 ||
                id >= _enabledItems.Length || id >= _itemCollectionDatas.Length || _itemCollectionDatas[id] == null)
            {
                return false;
            }

            if (!EnsureSet().SetUnlockedSilently(id, true))
            {
                return false;
            }

            try
            {
                SaveProvider.SetInt(CollectionSet.GetItemSaveKey(_saveKeyPrefix, id), 1);
            }
            finally
            {
                // WHY: a persistence observer may throw after accepting the unlock.
                // The accepted prize must still consume its associated cost.
                onCommitted?.Invoke();
            }

            OnItemAdded?.Invoke(id);
            _set.RaiseItemUnlocked(id);
            InvokeCompletionEvents();
            return true;
        }

        /// <summary>Adds an item by data (looks up index in ItemCollectionDatas).</summary>
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

            if (!EnsureSet().SetUnlockedSilently(id, false))
            {
                return;
            }

            SaveProvider.SetInt(CollectionSet.GetItemSaveKey(_saveKeyPrefix, id), 0);

            OnItemRemoved?.Invoke(id);
            _set.RaiseItemLocked(id);
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

            CollectionSet set = EnsureSet();
            int count = Mathf.Min(_enabledItems.Length, _itemCollectionDatas.Length);
            for (int i = 0; i < count; i++)
            {
                if (set.SetUnlockedSilently(i, false))
                {
                    SaveProvider.SetInt(CollectionSet.GetItemSaveKey(_saveKeyPrefix, i), 0);
                    OnItemRemoved?.Invoke(i);
                    set.RaiseItemLocked(i);
                }
            }

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

            List<int> list = new();
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

            List<int> list = new();
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

            List<int> list = new();
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

            List<int> list = new();
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

            List<int> list = new();
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
