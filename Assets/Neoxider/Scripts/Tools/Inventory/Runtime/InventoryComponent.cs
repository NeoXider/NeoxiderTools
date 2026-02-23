using System;
using System.Collections.Generic;
using Neo;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public enum InventoryLoadMode
    {
        UseSaveIfExists = 0,
        MergeSaveWithInitial = 1,
        InitialOnlyIgnoreSave = 2
    }

    [NeoDoc("Tools/Inventory/InventoryComponent.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryComponent")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryComponent))]
    public sealed class InventoryComponent : Singleton<InventoryComponent>
    {
        [Header("Data")] [SerializeField] [Tooltip("Optional items database for max stack setup and id lookup.")]
        private InventoryDatabase _database;

        [SerializeField] [Tooltip("If enabled, adding unknown ids (not present in database) is blocked.")]
        private bool _restrictToDatabase;

        [SerializeField] [Tooltip("Optional initial entries used when save key has no data.")]
        private List<InventoryEntry> _initialEntries = new();

        [SerializeField] [Tooltip("Optional ScriptableObject with initial inventory content.")]
        private InventoryInitialStateData _initialStateData;

        [Header("Limits")] [SerializeField] [Min(0)] [Tooltip("0 = unlimited unique item ids.")]
        private int _maxUniqueItems;

        [SerializeField] [Min(0)] [Tooltip("0 = unlimited total items in inventory.")]
        private int _maxTotalItems;

        [Header("Save")] [SerializeField] [Tooltip("Enable auto loading in Awake.")]
        private bool _autoLoad = true;

        [SerializeField] [Tooltip("При изменении инвентаря автоматически записывать состояние в SaveProvider по Save Key. Запись на диск — через SaveProvider.Save() при выходе/паузе по необходимости.")]
        private bool _autoSave = true;

        [SerializeField] [Tooltip("SaveProvider key for this inventory instance.")]
        private string _saveKey = "Inventory_Default";

        [SerializeField]
        [Tooltip("Invoke OnInventoryChanged after Load(), so UI can refresh immediately.")]
        private bool _invokeEventsOnLoad = true;

        [SerializeField] [Tooltip("Load strategy for combining SaveProvider data and initial state.")]
        private InventoryLoadMode _loadMode = InventoryLoadMode.UseSaveIfExists;

        [SerializeField] [Tooltip("If enabled, initial state is applied when resulting inventory is empty.")]
        private bool _applyInitialIfResultEmpty = true;

        [Header("Condition Helper")] [SerializeField]
        [Tooltip("Selected id used by SelectedItemCount property for NeoCondition-friendly checks.")]
        private int _selectedItemId;

        [Header("Drop")] [SerializeField] [Tooltip("Optional drop module connected to this inventory.")]
        private InventoryDropper _dropper;

        [Header("Debug")]
        [SerializeField] [Tooltip("Текущее содержимое инвентаря (только для просмотра в Play mode, обновляется при изменениях).")]
        private List<InventoryEntry> _debugEntries = new();

        [Header("Events")] public UnityEvent OnInventoryChanged = new();
        public UnityEvent<int, int> OnItemAdded = new();
        public UnityEvent<int, int> OnItemRemoved = new();
        public UnityEvent<int, int> OnItemCountChanged = new();
        public UnityEvent<int> OnItemBecameZero = new();
        public UnityEvent<int, int> OnCapacityRejected = new();
        public UnityEvent OnBeforeLoad = new();
        public UnityEvent OnLoaded = new();
        public UnityEvent OnSaved = new();

        private readonly InventoryManager _manager = new();
        private bool _runtimeInitialized;

        public InventoryDatabase Database => _database;
        public string SaveKey => _saveKey;
        public int TotalItemCount => _manager.TotalCount;
        public int UniqueItemCount => _manager.UniqueCount;
        public bool IsEmpty => _manager.TotalCount <= 0;

        public int SelectedItemId
        {
            get => _selectedItemId;
            set => _selectedItemId = value;
        }

        public int SelectedItemCount => GetCount(_selectedItemId);
        public InventoryLoadMode LoadMode => _loadMode;

        /// <summary>
        ///     True, если этот экземпляр зарегистрирован как InventoryComponent.I (глобальный singleton).
        ///     При false — компонент работает как обычный экземпляр и поддерживает несколько инвентарей в сцене.
        /// </summary>
        public bool IsSingleton => SetInstanceOnAwakeEnabled;

        protected override void Awake()
        {
            base.Awake();
            EnsureRuntimeInitialized();
        }

        protected override void Init()
        {
            base.Init();
            EnsureRuntimeInitialized();
        }

        private void OnValidate()
        {
            if (_maxUniqueItems < 0)
            {
                _maxUniqueItems = 0;
            }

            if (_maxTotalItems < 0)
            {
                _maxTotalItems = 0;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            OnInventoryChanged.AddListener(RefreshDebugContent);
            OnLoaded.AddListener(RefreshDebugContent);
            RefreshDebugContent();
        }

        private void OnDisable()
        {
            OnInventoryChanged.RemoveListener(RefreshDebugContent);
            OnLoaded.RemoveListener(RefreshDebugContent);
        }

        private void RefreshDebugContent()
        {
            if (!Application.isPlaying || _manager == null)
            {
                return;
            }

            List<InventoryEntry> entries = _manager.CreateSnapshot();
            _debugEntries.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                InventoryEntry e = entries[i];
                if (e.Count > 0)
                {
                    _debugEntries.Add(new InventoryEntry(e.ItemId, e.Count));
                }
            }
        }

        public static InventoryComponent FindDefault()
        {
            return I != null ? I : FindObjectOfType<InventoryComponent>();
        }

        [Button("Add 1", 100)]
        public int AddItemById(int itemId)
        {
            return AddItemByIdAmount(itemId, 1);
        }

        [Button("Add N", 100)]
        public int AddItemByIdAmount(int itemId, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            if (!CanUseItemId(itemId))
            {
                OnCapacityRejected?.Invoke(itemId, amount);
                return 0;
            }

            int added = _manager.Add(itemId, amount);
            int rejected = Math.Max(0, amount - added);

            if (added > 0)
            {
                int newCount = _manager.GetCount(itemId);
                OnItemAdded?.Invoke(itemId, added);
                OnItemCountChanged?.Invoke(itemId, newCount);
                OnInventoryChanged?.Invoke();

                if (_autoSave)
                {
                    Save();
                }
            }

            if (rejected > 0)
            {
                OnCapacityRejected?.Invoke(itemId, rejected);
            }

            return added;
        }

        public int AddItemData(InventoryItemData itemData, int amount = 1)
        {
            if (itemData == null)
            {
                return 0;
            }

            return AddItemByIdAmount(itemData.ItemId, amount);
        }

        [Button("Remove 1", 100)]
        public int RemoveItemById(int itemId)
        {
            return RemoveItemByIdAmount(itemId, 1);
        }

        [Button("Remove N", 100)]
        public int RemoveItemByIdAmount(int itemId, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int removed = _manager.Remove(itemId, amount);
            if (removed <= 0)
            {
                return 0;
            }

            int newCount = _manager.GetCount(itemId);
            OnItemRemoved?.Invoke(itemId, removed);
            OnItemCountChanged?.Invoke(itemId, newCount);
            if (newCount <= 0)
            {
                OnItemBecameZero?.Invoke(itemId);
            }

            OnInventoryChanged?.Invoke();

            if (_autoSave)
            {
                Save();
            }

            return removed;
        }

        /// <summary>
        ///     Удаляет amount предметов только если хватает. Возвращает true, если удалено ровно amount.
        ///     Удобно для трат: if (inventory.TryConsume(itemId, cost)) { ... }
        /// </summary>
        public bool TryConsume(int itemId, int amount)
        {
            if (amount <= 0 || !HasItemAmount(itemId, amount))
            {
                return false;
            }

            RemoveItemByIdAmount(itemId, amount);
            return true;
        }

        public bool HasItem(int itemId)
        {
            return _manager.Has(itemId, 1);
        }

        public bool HasItemAmount(int itemId, int amount)
        {
            return _manager.Has(itemId, amount);
        }

        public int GetCount(int itemId)
        {
            return _manager.GetCount(itemId);
        }

        public List<InventoryEntry> GetSnapshotEntries()
        {
            return _manager.CreateSnapshot();
        }

        /// <summary>
        ///     Количество слотов с ненулевым количеством (порядок = порядок добавления).
        /// </summary>
        public int GetNonEmptySlotCount()
        {
            return GetSnapshotEntries().Count;
        }

        /// <summary>
        ///     ItemId в слоте по индексу (0..GetNonEmptySlotCount()-1). -1 если индекс вне диапазона.
        /// </summary>
        public int GetItemIdAtSlotIndex(int slotIndex)
        {
            List<InventoryEntry> entries = GetSnapshotEntries();
            if (slotIndex < 0 || slotIndex >= entries.Count)
            {
                return -1;
            }

            return entries[slotIndex].ItemId;
        }

        /// <summary>
        ///     Returns the first item id that has count &gt; 0 (by snapshot order), or a value &lt; 0 if inventory is empty.
        /// </summary>
        public int GetFirstItemId()
        {
            List<InventoryEntry> entries = GetSnapshotEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Count > 0)
                {
                    return entries[i].ItemId;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Returns the last item id in snapshot that has count &gt; 0, or a value &lt; 0 if inventory is empty.
        /// </summary>
        public int GetLastItemId()
        {
            List<InventoryEntry> entries = GetSnapshotEntries();
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Count > 0)
                {
                    return entries[i].ItemId;
                }
            }

            return -1;
        }

        public InventoryItemData GetItemData(int itemId)
        {
            return _database != null ? _database.GetItemData(itemId) : null;
        }

        [Button("Drop Selected", 110)]
        public int DropSelected(int amount = 1)
        {
            return _dropper != null ? _dropper.DropSelected(amount) : 0;
        }

        [Button("Drop By Id", 110)]
        public int DropById(int itemId, int amount = 1)
        {
            return _dropper != null ? _dropper.DropById(itemId, amount) : 0;
        }

        public int DropData(InventoryItemData itemData, int amount = 1)
        {
            return _dropper != null ? _dropper.DropData(itemData, amount) : 0;
        }

        /// <summary>Drops the first item (by snapshot order). Returns amount dropped, or 0 if no dropper or empty.</summary>
        [Button("Drop First", 100)]
        public int DropFirst(int amount = 1)
        {
            return _dropper != null ? _dropper.DropFirst(amount) : 0;
        }

        /// <summary>Drops the last item (by snapshot order). Returns amount dropped, or 0 if no dropper or empty.</summary>
        [Button("Drop Last", 100)]
        public int DropLast(int amount = 1)
        {
            return _dropper != null ? _dropper.DropLast(amount) : 0;
        }

        [Button("Add 1 (Selected Id)", 140)]
        public int TestAdd1Selected()
        {
            return AddItemById(_selectedItemId);
        }

        [Button("Remove 1 (Selected Id)", 140)]
        public int TestRemove1Selected()
        {
            return RemoveItemById(_selectedItemId);
        }

        [Button("Clear", 80)]
        public void ClearInventory()
        {
            if (_manager.TotalCount <= 0)
            {
                return;
            }

            List<InventoryEntry> snapshot = _manager.CreateSnapshot();
            for (int i = 0; i < snapshot.Count; i++)
            {
                InventoryEntry e = snapshot[i];
                if (e.Count <= 0)
                {
                    continue;
                }

                OnItemRemoved?.Invoke(e.ItemId, e.Count);
                OnItemCountChanged?.Invoke(e.ItemId, 0);
                OnItemBecameZero?.Invoke(e.ItemId);
            }

            _manager.Clear();
            OnInventoryChanged?.Invoke();

            if (_autoSave)
            {
                Save();
            }
        }

        [Button("Save", 80)]
        public void Save()
        {
            if (string.IsNullOrWhiteSpace(_saveKey))
            {
                Debug.LogWarning("[InventoryComponent] Save key is empty", this);
                return;
            }

            InventorySaveData data = new()
            {
                Entries = _manager.CreateSnapshot()
            };

            string json = JsonUtility.ToJson(data);
            SaveProvider.SetString(_saveKey, json);
            OnSaved?.Invoke();
        }

        [Button("Load", 80)]
        public void Load()
        {
            OnBeforeLoad?.Invoke();
            ApplyConstraints();
            _manager.Clear();

            List<InventoryEntry> savedEntries = TryReadSavedEntries();
            switch (_loadMode)
            {
                case InventoryLoadMode.InitialOnlyIgnoreSave:
                    ApplyInitialState();
                    break;
                case InventoryLoadMode.MergeSaveWithInitial:
                    ApplyInitialState();
                    ImportEntries(savedEntries);
                    break;
                default:
                    if (savedEntries != null && savedEntries.Count > 0)
                    {
                        ImportEntries(savedEntries);
                    }
                    else
                    {
                        ApplyInitialState();
                    }

                    break;
            }

            if (_applyInitialIfResultEmpty && _manager.TotalCount <= 0)
            {
                ApplyInitialState();
            }

            OnLoaded?.Invoke();
            if (_invokeEventsOnLoad)
            {
                OnInventoryChanged?.Invoke();
            }
        }

        private void ResetToInitialEntries()
        {
            _manager.Clear();
            ApplyConstraints();
            ApplyInitialState();
        }

        private bool CanUseItemId(int itemId)
        {
            if (!_restrictToDatabase || _database == null)
            {
                return true;
            }

            return _database.ContainsId(itemId);
        }

        private void ApplyConstraints()
        {
            _manager.MaxUniqueItems = _maxUniqueItems;
            _manager.MaxTotalItems = _maxTotalItems;
            _manager.ClearItemMaxStacks();

            if (_database == null || _database.Items == null)
            {
                return;
            }

            for (int i = 0; i < _database.Items.Count; i++)
            {
                InventoryItemData item = _database.Items[i];
                if (item == null)
                {
                    continue;
                }

                _manager.SetItemMaxStack(item.ItemId, item.MaxStack);
            }
        }

        private void EnsureRuntimeInitialized()
        {
            if (_runtimeInitialized)
            {
                return;
            }

            _runtimeInitialized = true;
            ApplyConstraints();

            if (_autoLoad)
            {
                Load();
            }
            else
            {
                ResetToInitialEntries();
                if (_invokeEventsOnLoad)
                {
                    OnInventoryChanged?.Invoke();
                }
            }
        }

        private void ImportEntries(IEnumerable<InventoryEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            foreach (InventoryEntry entry in entries)
            {
                if (entry == null || entry.Count <= 0 || !CanUseItemId(entry.ItemId))
                {
                    continue;
                }

                _manager.Add(entry.ItemId, entry.Count);
            }
        }

        private void ApplyInitialState()
        {
            ImportEntries(_initialStateData != null ? _initialStateData.Entries : null);
            ImportEntries(_initialEntries);
        }

        private List<InventoryEntry> TryReadSavedEntries()
        {
            if (string.IsNullOrWhiteSpace(_saveKey) || !SaveProvider.HasKey(_saveKey))
            {
                return null;
            }

            string json = SaveProvider.GetString(_saveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
            return data?.Entries;
        }
    }
}
