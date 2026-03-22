using System;
using System.Collections.Generic;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     How <see cref="InventoryComponent.Load" /> combines SaveProvider data with initial inventory content.
    /// </summary>
    public enum InventoryLoadMode
    {
        /// <summary>Use save when it has content; otherwise apply initial state.</summary>
        UseSaveIfExists = 0,

        /// <summary>Apply initial state first, then merge save entries/instances/slots on top.</summary>
        MergeSaveWithInitial = 1,

        /// <summary>Ignore SaveProvider and apply only initial state.</summary>
        InitialOnlyIgnoreSave = 2
    }

    /// <summary>
    ///     Scene inventory facade: add/remove items, optional fixed slot grid, SaveProvider persistence, UnityEvents, and optional drop via <see cref="InventoryDropper" />.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="InventoryStorageMode.Aggregated" /> for stacks grouped by item id, or <see cref="InventoryStorageMode.SlotGrid" /> for physical slots (hotbar, chests).
    /// </remarks>
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

        [Header("Storage")] [SerializeField]
        private InventoryStorageMode _storageMode = InventoryStorageMode.Aggregated;

        [SerializeField] [Min(0)] [Tooltip("Slot count used when Storage Mode = Slot Grid.")]
        private int _slotCount = 10;

        [Header("Limits")] [SerializeField] [Min(0)] [Tooltip("0 = unlimited unique item ids.")]
        private int _maxUniqueItems;

        [SerializeField] [Min(0)] [Tooltip("0 = unlimited total items in inventory.")]
        private int _maxTotalItems;

        [Header("Save")] [SerializeField] [Tooltip("Enable auto loading in Awake.")]
        private bool _autoLoad = true;

        [SerializeField]
        [Tooltip(
            "When the inventory changes, persist JSON to SaveProvider under Save Key. Call SaveProvider.Save() when you want to flush to disk (e.g. on quit or pause).")]
        private bool _autoSave = true;

        [SerializeField] [Tooltip("SaveProvider key for this inventory instance.")]
        private string _saveKey = "Inventory_Default";

        [SerializeField] [Tooltip("Invoke OnInventoryChanged after Load(), so UI can refresh immediately.")]
        private bool _invokeEventsOnLoad = true;

        [SerializeField] [Tooltip("Load strategy for combining SaveProvider data and initial state.")]
        private InventoryLoadMode _loadMode = InventoryLoadMode.UseSaveIfExists;

        [SerializeField] [Tooltip("If enabled, initial state is applied when resulting inventory is empty.")]
        private bool _applyInitialIfResultEmpty = true;

        [Header("Condition Helper")]
        [SerializeField]
        [Tooltip("Selected id used by SelectedItemCount property for NeoCondition-friendly checks.")]
        private int _selectedItemId;

        [Header("Drop")] [SerializeField] [Tooltip("Optional drop module connected to this inventory.")]
        private InventoryDropper _dropper;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Read-only mirror of contents in Play mode; refreshed when the inventory changes.")]
        private List<InventoryEntry> _debugEntries = new();

        [Header("Events")]
        /// <summary>Raised after any mutation that changes stored items (add, remove, slot move, clear).</summary>
        public UnityEvent OnInventoryChanged = new();

        /// <summary>Raised when items are added successfully. Arguments: item id, amount added.</summary>
        public UnityEvent<int, int> OnItemAdded = new();

        /// <summary>Raised when items are removed successfully. Arguments: item id, amount removed.</summary>
        public UnityEvent<int, int> OnItemRemoved = new();

        /// <summary>Raised when the count for an item id changes. Arguments: item id, new total count.</summary>
        public UnityEvent<int, int> OnItemCountChanged = new();

        /// <summary>Raised when an item id reaches zero count after a change.</summary>
        public UnityEvent<int> OnItemBecameZero = new();

        /// <summary>Raised when part or all of an add/remove request is rejected (limits, invalid id, full slots).</summary>
        public UnityEvent<int, int> OnCapacityRejected = new();

        /// <summary>Raised at the start of <see cref="Load" /> before storage is repopulated.</summary>
        public UnityEvent OnBeforeLoad = new();

        /// <summary>Raised when <see cref="Load" /> has finished applying save/initial data.</summary>
        public UnityEvent OnLoaded = new();

        /// <summary>Raised after <see cref="Save" /> writes JSON to SaveProvider.</summary>
        public UnityEvent OnSaved = new();

        private readonly InventoryConstraints _constraints = new();
        private IInventoryStorage _storage;
        private bool _runtimeInitialized;

        /// <summary>Optional item database used for max stack and <see cref="GetItemData" />.</summary>
        public InventoryDatabase Database => _database;

        /// <summary>SaveProvider key used by <see cref="Save" /> and <see cref="Load" />.</summary>
        public string SaveKey => _saveKey;

        /// <summary>Configured storage layout (aggregated stacks vs fixed slot grid).</summary>
        public InventoryStorageMode StorageMode => _storageMode;

        /// <summary>Sum of all item counts in this container.</summary>
        public int TotalItemCount => _storage != null ? _storage.TotalCount : 0;

        /// <summary>Number of distinct item ids with count greater than zero.</summary>
        public int UniqueItemCount => _storage != null ? _storage.UniqueCount : 0;

        /// <summary>True when <see cref="TotalItemCount" /> is zero.</summary>
        public bool IsEmpty => TotalItemCount <= 0;

        /// <summary>True when this component uses <see cref="InventoryStorageMode.SlotGrid" />.</summary>
        public bool IsSlotInventory => _storageMode == InventoryStorageMode.SlotGrid;

        /// <summary>Number of physical slots in slot-grid mode; zero in aggregated mode.</summary>
        public int SlotCapacity => _storage is ISlottedInventory slotted ? slotted.SlotCapacity : _storageMode == InventoryStorageMode.SlotGrid ? _slotCount : 0;

        /// <summary>Item id used by <see cref="SelectedItemCount" /> for NeoCondition-style checks.</summary>
        public int SelectedItemId
        {
            get => _selectedItemId;
            set => _selectedItemId = value;
        }

        /// <summary>Current count of <see cref="SelectedItemId" />.</summary>
        public int SelectedItemCount => GetCount(_selectedItemId);

        /// <summary>Active load strategy for combining save and initial state.</summary>
        public InventoryLoadMode LoadMode => _loadMode;

        /// <summary>True when this instance registers as the singleton <see cref="Singleton{T}.I" /> on Awake.</summary>
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

            if (_slotCount < 0)
            {
                _slotCount = 0;
            }
        }

        /// <summary>Returns the singleton instance if set; otherwise finds any <see cref="InventoryComponent" /> in the scene.</summary>
        public static InventoryComponent FindDefault()
        {
            return I != null ? I : FindObjectOfType<InventoryComponent>();
        }

        [Button]
        /// <summary>Adds one unit of <paramref name="itemId" /> respecting stack rules and limits.</summary>
        /// <returns>Amount actually added (may be less than requested).</returns>
        public int AddItemById(int itemId)
        {
            return AddItemByIdAmount(itemId, 1);
        }

        [Button]
        /// <summary>Adds stackable or instance-based items depending on <see cref="InventoryItemData.SupportsInstanceState" />.</summary>
        /// <param name="itemId">Runtime item identifier.</param>
        /// <param name="amount">Requested amount; for instance-based items each unit becomes a separate instance.</param>
        /// <returns>Amount actually added.</returns>
        public int AddItemByIdAmount(int itemId, int amount)
        {
            EnsureRuntimeInitialized();
            if (amount <= 0 || !CanUseItemId(itemId))
            {
                if (amount > 0)
                {
                    OnCapacityRejected?.Invoke(itemId, amount);
                }

                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int added = 0;
            if (IsInstanceBasedItem(itemId))
            {
                for (int i = 0; i < amount; i++)
                {
                    int instanceAdded = _storage.AddInstance(new InventoryItemInstance(itemId));
                    if (instanceAdded <= 0)
                    {
                        break;
                    }

                    added += instanceAdded;
                }
            }
            else
            {
                added = _storage.Add(itemId, amount);
            }

            FinalizeMutation(before);

            int rejected = Math.Max(0, amount - added);
            if (rejected > 0)
            {
                OnCapacityRejected?.Invoke(itemId, rejected);
            }

            return added;
        }

        /// <summary>Adds items using <paramref name="itemData" />; uses instances when <see cref="InventoryItemData.SupportsInstanceState" /> is true.</summary>
        /// <returns>Total amount successfully added.</returns>
        public int AddItemData(InventoryItemData itemData, int amount = 1)
        {
            if (itemData == null)
            {
                return 0;
            }

            if (!itemData.SupportsInstanceState)
            {
                return AddItemByIdAmount(itemData.ItemId, amount);
            }

            int added = 0;
            for (int i = 0; i < amount; i++)
            {
                added += AddItemInstance(new InventoryItemInstance(itemData.ItemId));
            }

            return added;
        }

        /// <summary>Adds a single <see cref="InventoryItemInstance" /> with optional per-instance state payload.</summary>
        /// <returns>1 if added, 0 if rejected.</returns>
        public int AddItemInstance(InventoryItemInstance instance)
        {
            EnsureRuntimeInitialized();
            if (instance == null || !CanUseItemId(instance.ItemId))
            {
                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int added = _storage.AddInstance(instance);
            FinalizeMutation(before);
            if (added <= 0)
            {
                OnCapacityRejected?.Invoke(instance.ItemId, Math.Max(1, instance.Count));
            }

            return added;
        }

        [Button]
        /// <summary>Removes one unit of <paramref name="itemId" /> from aggregated storage or the first matching stack/slot.</summary>
        /// <returns>Amount actually removed.</returns>
        public int RemoveItemById(int itemId)
        {
            return RemoveItemByIdAmount(itemId, 1);
        }

        [Button]
        /// <summary>Removes up to <paramref name="amount" /> of <paramref name="itemId" />.</summary>
        /// <returns>Amount actually removed.</returns>
        public int RemoveItemByIdAmount(int itemId, int amount)
        {
            EnsureRuntimeInitialized();
            if (amount <= 0)
            {
                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int removed = _storage.Remove(itemId, amount);
            FinalizeMutation(before);
            return removed;
        }

        /// <summary>Removes <paramref name="amount" /> only if the inventory currently has at least that many.</summary>
        /// <returns>True if the full amount was removed.</returns>
        public bool TryConsume(int itemId, int amount)
        {
            if (amount <= 0 || !HasItemAmount(itemId, amount))
            {
                return false;
            }

            return RemoveItemByIdAmount(itemId, amount) == amount;
        }

        /// <summary>True if at least one of <paramref name="itemId" /> is stored.</summary>
        public bool HasItem(int itemId)
        {
            EnsureRuntimeInitialized();
            return _storage.Has(itemId);
        }

        /// <summary>True if stored count for <paramref name="itemId" /> is at least <paramref name="amount" />.</summary>
        public bool HasItemAmount(int itemId, int amount)
        {
            EnsureRuntimeInitialized();
            return _storage.Has(itemId, amount);
        }

        /// <summary>Total stored count for <paramref name="itemId" /> (aggregated across stacks/slots).</summary>
        public int GetCount(int itemId)
        {
            EnsureRuntimeInitialized();
            return _storage.GetCount(itemId);
        }

        /// <summary>Effective max stack for <paramref name="itemId" /> from constraints (0 means unlimited).</summary>
        public int GetMaxStack(int itemId)
        {
            EnsureRuntimeInitialized();
            return _constraints.GetMaxStack(itemId);
        }

        /// <summary>True when database entry for <paramref name="itemId" /> has <see cref="InventoryItemData.SupportsInstanceState" />.</summary>
        public bool SupportsInstanceState(int itemId)
        {
            InventoryItemData data = GetItemData(itemId);
            return data != null && data.SupportsInstanceState;
        }

        /// <summary>Packed list of records in storage order (slot index order in grid mode).</summary>
        public List<InventoryItemRecord> GetSnapshotRecords()
        {
            EnsureRuntimeInitialized();
            return _storage.CreateRecordSnapshot();
        }

        /// <summary>All instance-based items currently stored (clone per entry).</summary>
        public List<InventoryItemInstance> GetSnapshotInstances()
        {
            EnsureRuntimeInitialized();
            return _storage.CreateInstanceSnapshot();
        }

        /// <summary>Legacy-friendly list of id/count pairs derived from <see cref="GetSnapshotRecords" />.</summary>
        public List<InventoryEntry> GetSnapshotEntries()
        {
            List<InventoryItemRecord> records = GetSnapshotRecords();
            List<InventoryEntry> entries = new(records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                InventoryItemRecord record = records[i];
                if (record != null && record.EffectiveCount > 0)
                {
                    entries.Add(record.ToEntry());
                }
            }

            return entries;
        }

        /// <summary>Number of non-empty records in the packed snapshot (not always equal to physical slot count).</summary>
        public int GetNonEmptySlotCount()
        {
            return GetSnapshotRecords().Count;
        }

        /// <summary>Gets the record at packed index (iteration order of <see cref="GetSnapshotRecords" />).</summary>
        public bool TryGetRecordAtPackedIndex(int packedIndex, out InventoryItemRecord record)
        {
            EnsureRuntimeInitialized();
            return _storage.TryGetRecordAtPackedIndex(packedIndex, out record);
        }

        /// <summary>Item id at packed index, or -1 if out of range or empty.</summary>
        public int GetItemIdAtSlotIndex(int slotIndex)
        {
            return TryGetRecordAtPackedIndex(slotIndex, out InventoryItemRecord record) ? record.EffectiveItemId : -1;
        }

        /// <summary>Record at packed index, or null if invalid/empty.</summary>
        public InventoryItemRecord GetRecordAtSlotIndex(int slotIndex)
        {
            return TryGetRecordAtPackedIndex(slotIndex, out InventoryItemRecord record) ? record : null;
        }

        /// <summary>Clone of instance payload at packed index if the record is instance-based; otherwise null.</summary>
        public InventoryItemInstance GetInstanceAtSlotIndex(int slotIndex)
        {
            return TryGetRecordAtPackedIndex(slotIndex, out InventoryItemRecord record) && record.IsInstance
                ? record.Instance.Clone()
                : null;
        }

        /// <summary>Removes up to <paramref name="amount" /> from the packed index and returns the taken record.</summary>
        public bool TryTakeRecordAtPackedIndex(int packedIndex, int amount, out InventoryItemRecord record)
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            bool result = _storage.TryTakeRecordAtPackedIndex(packedIndex, amount, out record);
            FinalizeMutation(before);
            return result;
        }

        /// <summary>Physical slot state in slot-grid mode; empty slot in aggregated mode.</summary>
        public InventorySlotState GetSlot(int slotIndex)
        {
            EnsureRuntimeInitialized();
            return _storage is ISlottedInventory slotted ? slotted.GetSlot(slotIndex) : InventorySlotState.Empty();
        }

        /// <summary>Clone of instance in physical slot <paramref name="slotIndex" /> (slot grid only).</summary>
        public InventoryItemInstance GetInstanceAtPhysicalSlot(int slotIndex)
        {
            InventorySlotState slot = GetSlot(slotIndex);
            return slot.IsInstance && slot.Instance != null ? slot.Instance.Clone() : null;
        }

        /// <summary>Record representing the physical slot contents, or null if empty (slot grid only).</summary>
        public InventoryItemRecord GetPhysicalSlotRecord(int slotIndex)
        {
            InventorySlotState slot = GetSlot(slotIndex);
            return slot != null && !slot.IsEmpty ? slot.ToRecord() : null;
        }

        /// <summary>Replaces a physical slot if constraints allow (slot grid only).</summary>
        /// <returns>True if the slot was updated.</returns>
        public bool TrySetSlot(int slotIndex, InventorySlotState state)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted || slotIndex < 0 || slotIndex >= slotted.SlotCapacity)
            {
                return false;
            }

            if (!CanApplySlotState(slotIndex, state))
            {
                return false;
            }

            Dictionary<int, int> before = CaptureCounts();
            slotted.SetSlot(slotIndex, state);
            FinalizeMutation(before);
            return true;
        }

        /// <summary>Swaps two physical slots (slot grid only).</summary>
        public bool SwapSlots(int sourceIndex, int targetIndex)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted)
            {
                return false;
            }

            Dictionary<int, int> before = CaptureCounts();
            bool result = slotted.SwapSlots(sourceIndex, targetIndex);
            FinalizeMutation(before);
            return result;
        }

        /// <summary>Moves or merges stack from source slot into target (slot grid). <paramref name="amount" /> 0 = move all compatible amount.</summary>
        /// <returns>Amount moved.</returns>
        public int MoveSlot(int sourceIndex, int targetIndex, int amount = 0)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted)
            {
                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int moved = slotted.MoveSlot(sourceIndex, targetIndex, amount);
            FinalizeMutation(before);
            return moved;
        }

        /// <summary>Removes from physical slot and returns a record (slot grid only).</summary>
        public bool TryTakeSlot(int slotIndex, int amount, out InventoryItemRecord record)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted)
            {
                record = null;
                return false;
            }

            Dictionary<int, int> before = CaptureCounts();
            bool result = slotted.TryTakeSlot(slotIndex, amount, out record);
            FinalizeMutation(before);
            return result;
        }

        /// <summary>Finds the first packed record matching <paramref name="itemId" /> and takes up to <paramref name="amount" />.</summary>
        public bool TryTakeFirstRecordByItemId(int itemId, int amount, out InventoryItemRecord record)
        {
            record = null;
            List<InventoryItemRecord> records = GetSnapshotRecords();
            for (int i = 0; i < records.Count; i++)
            {
                InventoryItemRecord current = records[i];
                if (current != null && current.EffectiveItemId == itemId)
                {
                    return TryTakeRecordAtPackedIndex(i, amount, out record);
                }
            }

            return false;
        }

        /// <summary>Inserts or merges <paramref name="record" /> into physical slot (slot grid only).</summary>
        /// <returns>Amount inserted.</returns>
        public int TryInsertIntoSlot(int slotIndex, InventoryItemRecord record, int amount = 0)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted)
            {
                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int inserted = slotted.TryInsertIntoSlot(slotIndex, record, amount);
            FinalizeMutation(before);
            return inserted;
        }

        /// <summary>First item id with positive count in packed order, or -1 if empty.</summary>
        public int GetFirstItemId()
        {
            List<InventoryItemRecord> records = GetSnapshotRecords();
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].EffectiveCount > 0)
                {
                    return records[i].EffectiveItemId;
                }
            }

            return -1;
        }

        /// <summary>Last item id with positive count in packed order, or -1 if empty.</summary>
        public int GetLastItemId()
        {
            List<InventoryItemRecord> records = GetSnapshotRecords();
            for (int i = records.Count - 1; i >= 0; i--)
            {
                if (records[i].EffectiveCount > 0)
                {
                    return records[i].EffectiveItemId;
                }
            }

            return -1;
        }

        /// <summary>Looks up <see cref="InventoryItemData" /> from <see cref="Database" />.</summary>
        public InventoryItemData GetItemData(int itemId)
        {
            return _database != null ? _database.GetItemData(itemId) : null;
        }

        [Button]
        /// <summary>Delegates to <see cref="InventoryDropper" /> for the configured selection policy.</summary>
        /// <returns>Amount dropped into the world.</returns>
        public int DropSelected(int amount = 1)
        {
            return _dropper != null ? _dropper.DropSelected(amount) : 0;
        }

        [Button]
        /// <summary>Drops <paramref name="amount" /> of <paramref name="itemId" /> via <see cref="InventoryDropper" />.</summary>
        public int DropById(int itemId, int amount = 1)
        {
            return _dropper != null ? _dropper.DropById(itemId, amount) : 0;
        }

        /// <summary>Drops using <paramref name="itemData" /> id and instance rules.</summary>
        public int DropData(InventoryItemData itemData, int amount = 1)
        {
            return _dropper != null ? _dropper.DropData(itemData, amount) : 0;
        }

        [Button]
        /// <summary>Drops from the first non-empty packed record.</summary>
        public int DropFirst(int amount = 1)
        {
            return _dropper != null ? _dropper.DropFirst(amount) : 0;
        }

        [Button]
        /// <summary>Drops from the last non-empty packed record.</summary>
        public int DropLast(int amount = 1)
        {
            return _dropper != null ? _dropper.DropLast(amount) : 0;
        }

        [Button]
        /// <summary>Editor/debug: adds one of <see cref="SelectedItemId" />.</summary>
        public int TestAdd1Selected()
        {
            return AddItemById(_selectedItemId);
        }

        [Button]
        /// <summary>Editor/debug: removes one of <see cref="SelectedItemId" />.</summary>
        public int TestRemove1Selected()
        {
            return RemoveItemById(_selectedItemId);
        }

        [Button]
        /// <summary>Removes all items and raises delta events before <see cref="OnInventoryChanged" />.</summary>
        public void ClearInventory()
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            _storage.Clear();
            FinalizeMutation(before);
        }

        [Button]
        /// <summary>Serializes current storage into SaveProvider JSON under <see cref="SaveKey" />.</summary>
        public void Save()
        {
            EnsureRuntimeInitialized();
            if (string.IsNullOrWhiteSpace(_saveKey))
            {
                Debug.LogWarning("[InventoryComponent] Save key is empty", this);
                return;
            }

            InventorySaveData data = new()
            {
                Version = 1,
                StorageMode = (int)_storageMode,
                SlotCapacity = SlotCapacity
            };

            List<InventoryItemRecord> records = _storage.CreateRecordSnapshot();
            for (int i = 0; i < records.Count; i++)
            {
                InventoryItemRecord record = records[i];
                if (record == null || record.EffectiveCount <= 0)
                {
                    continue;
                }

                if (record.IsInstance)
                {
                    data.Instances.Add(record.Instance.Clone());
                }
                else
                {
                    data.Entries.Add(record.ToEntry());
                }
            }

            if (_storage is ISlottedInventory slotted)
            {
                data.Slots = slotted.CreateSlotSnapshot();
            }

            string json = JsonUtility.ToJson(data);
            SaveProvider.SetString(_saveKey, json);
            OnSaved?.Invoke();
        }

        [Button]
        /// <summary>Clears storage and repopulates from save and/or initial state per <see cref="LoadMode" />.</summary>
        public void Load()
        {
            _runtimeInitialized = true;
            EnsureStorage();
            OnBeforeLoad?.Invoke();
            ApplyConstraints();
            _storage.Clear();

            InventorySaveData saveData = TryReadSaveData();
            switch (_loadMode)
            {
                case InventoryLoadMode.InitialOnlyIgnoreSave:
                    ApplyInitialState();
                    break;
                case InventoryLoadMode.MergeSaveWithInitial:
                    ApplyInitialState();
                    ImportSaveData(saveData);
                    break;
                default:
                    if (saveData != null && HasAnySaveContent(saveData))
                    {
                        ImportSaveData(saveData);
                    }
                    else
                    {
                        ApplyInitialState();
                    }

                    break;
            }

            if (_applyInitialIfResultEmpty && TotalItemCount <= 0)
            {
                ApplyInitialState();
            }

            OnLoaded?.Invoke();
            if (_invokeEventsOnLoad)
            {
                OnInventoryChanged?.Invoke();
            }
        }

        private void RefreshDebugContent()
        {
            if (!Application.isPlaying || _storage == null)
            {
                return;
            }

            _debugEntries = GetSnapshotEntries();
        }

        private void EnsureRuntimeInitialized()
        {
            if (_runtimeInitialized)
            {
                return;
            }

            _runtimeInitialized = true;
            EnsureStorage();
            ApplyConstraints();

            if (_autoLoad)
            {
                Load();
            }
            else
            {
                _storage.Clear();
                ApplyInitialState();
                if (_invokeEventsOnLoad)
                {
                    OnInventoryChanged?.Invoke();
                }
            }
        }

        private void EnsureStorage()
        {
            if (_storage != null)
            {
                return;
            }

            _storage = _storageMode == InventoryStorageMode.SlotGrid
                ? new SlotGridInventory(_slotCount)
                : new AggregatedInventory();
        }

        private bool CanUseItemId(int itemId)
        {
            if (!_restrictToDatabase || _database == null)
            {
                return true;
            }

            return _database.ContainsId(itemId);
        }

        private bool IsInstanceBasedItem(int itemId)
        {
            InventoryItemData itemData = GetItemData(itemId);
            return itemData != null && itemData.SupportsInstanceState;
        }

        private void ApplyConstraints()
        {
            _constraints.MaxUniqueItems = _maxUniqueItems;
            _constraints.MaxTotalItems = _maxTotalItems;
            _constraints.ClearItemMaxStacks();

            if (_database != null && _database.Items != null)
            {
                for (int i = 0; i < _database.Items.Count; i++)
                {
                    InventoryItemData item = _database.Items[i];
                    if (item != null)
                    {
                        _constraints.SetItemMaxStack(item.ItemId, item.MaxStack);
                    }
                }
            }

            EnsureStorage();
            _storage.SetConstraints(_constraints);
        }

        private Dictionary<int, int> CaptureCounts()
        {
            Dictionary<int, int> counts = new();
            if (_storage == null)
            {
                return counts;
            }

            List<InventoryItemRecord> snapshot = _storage.CreateRecordSnapshot();
            for (int i = 0; i < snapshot.Count; i++)
            {
                InventoryItemRecord record = snapshot[i];
                if (record == null || record.EffectiveCount <= 0)
                {
                    continue;
                }

                counts.TryGetValue(record.EffectiveItemId, out int current);
                counts[record.EffectiveItemId] = current + record.EffectiveCount;
            }

            return counts;
        }

        private bool FinalizeMutation(Dictionary<int, int> before)
        {
            Dictionary<int, int> after = CaptureCounts();
            bool changed = EmitDeltaEvents(before, after);
            if (changed && _autoSave)
            {
                Save();
            }

            return changed;
        }

        private bool EmitDeltaEvents(Dictionary<int, int> before, Dictionary<int, int> after)
        {
            HashSet<int> ids = new();
            foreach (KeyValuePair<int, int> pair in before)
            {
                ids.Add(pair.Key);
            }

            foreach (KeyValuePair<int, int> pair in after)
            {
                ids.Add(pair.Key);
            }

            bool changed = false;
            foreach (int itemId in ids)
            {
                before.TryGetValue(itemId, out int oldCount);
                after.TryGetValue(itemId, out int newCount);
                if (oldCount == newCount)
                {
                    continue;
                }

                changed = true;
                if (newCount > oldCount)
                {
                    OnItemAdded?.Invoke(itemId, newCount - oldCount);
                }
                else
                {
                    OnItemRemoved?.Invoke(itemId, oldCount - newCount);
                }

                OnItemCountChanged?.Invoke(itemId, newCount);
                if (oldCount > 0 && newCount <= 0)
                {
                    OnItemBecameZero?.Invoke(itemId);
                }
            }

            if (changed)
            {
                OnInventoryChanged?.Invoke();
            }

            return changed;
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

                if (IsInstanceBasedItem(entry.ItemId))
                {
                    for (int i = 0; i < entry.Count; i++)
                    {
                        _storage.AddInstance(new InventoryItemInstance(entry.ItemId));
                    }
                }
                else
                {
                    _storage.Add(entry.ItemId, entry.Count);
                }
            }
        }

        private void ImportInstances(IEnumerable<InventoryItemInstance> instances)
        {
            if (instances == null)
            {
                return;
            }

            foreach (InventoryItemInstance instance in instances)
            {
                if (instance != null && instance.Count > 0 && CanUseItemId(instance.ItemId))
                {
                    _storage.AddInstance(instance);
                }
            }
        }

        private void ImportSlots(IEnumerable<InventorySlotState> slots)
        {
            if (_storage is not ISlottedInventory slotted || slots == null)
            {
                return;
            }

            int index = 0;
            foreach (InventorySlotState slot in slots)
            {
                if (index >= slotted.SlotCapacity)
                {
                    break;
                }

                if (slot == null || slot.IsEmpty || !CanUseItemId(slot.EffectiveItemId))
                {
                    index++;
                    continue;
                }

                slotted.SetSlot(index, slot);
                index++;
            }
        }

        private void ImportSlotRecords(IEnumerable<InventorySlotState> slots)
        {
            if (slots == null)
            {
                return;
            }

            foreach (InventorySlotState slot in slots)
            {
                if (slot == null || slot.IsEmpty || !CanUseItemId(slot.EffectiveItemId))
                {
                    continue;
                }

                if (slot.IsInstance && slot.Instance != null)
                {
                    _storage.AddInstance(slot.Instance);
                }
                else
                {
                    _storage.Add(slot.ItemId, slot.Count);
                }
            }
        }

        private void ApplyInitialState()
        {
            ImportEntries(_initialStateData != null ? _initialStateData.Entries : null);
            ImportEntries(_initialEntries);
        }

        private bool CanApplySlotState(int slotIndex, InventorySlotState state)
        {
            if (_storage is not ISlottedInventory slotted)
            {
                return false;
            }

            InventorySlotState next = state != null ? state.Clone() : InventorySlotState.Empty();
            if (!next.IsEmpty)
            {
                if (!CanUseItemId(next.EffectiveItemId))
                {
                    return false;
                }

                int maxStack = GetMaxStack(next.EffectiveItemId);
                if (!next.IsInstance && maxStack > 0 && next.Count > maxStack)
                {
                    return false;
                }
            }

            List<InventorySlotState> slots = slotted.CreateSlotSnapshot();
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                return false;
            }

            slots[slotIndex] = next;

            int total = 0;
            HashSet<int> unique = new();
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlotState slot = slots[i];
                if (slot == null || slot.IsEmpty)
                {
                    continue;
                }

                total += slot.EffectiveCount;
                unique.Add(slot.EffectiveItemId);
            }

            if (_maxTotalItems > 0 && total > _maxTotalItems)
            {
                return false;
            }

            if (_maxUniqueItems > 0 && unique.Count > _maxUniqueItems)
            {
                return false;
            }

            return true;
        }

        private void ImportSaveData(InventorySaveData data)
        {
            if (data == null)
            {
                return;
            }

            bool directSlotRestore = _storageMode == InventoryStorageMode.SlotGrid &&
                                     data.Version > 0 &&
                                     data.StorageMode == (int)InventoryStorageMode.SlotGrid &&
                                     data.Slots != null &&
                                     data.Slots.Count > 0;
            if (directSlotRestore)
            {
                ImportSlots(data.Slots);
                return;
            }

            if (data.StorageMode == (int)InventoryStorageMode.SlotGrid && data.Slots != null && data.Slots.Count > 0)
            {
                ImportSlotRecords(data.Slots);
                return;
            }

            ImportEntries(data.Entries);
            ImportInstances(data.Instances);
        }

        private bool HasAnySaveContent(InventorySaveData data)
        {
            return data != null &&
                   ((data.Entries != null && data.Entries.Count > 0) ||
                    (data.Instances != null && data.Instances.Count > 0) ||
                    (data.Slots != null && data.Slots.Count > 0));
        }

        private InventorySaveData TryReadSaveData()
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

            return JsonUtility.FromJson<InventorySaveData>(json);
        }
    }
}
