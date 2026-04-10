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
    public sealed partial class InventoryComponent : Singleton<InventoryComponent>
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

#pragma warning disable 0414
        [SerializeField] [Tooltip("If enabled, initial state is applied when resulting inventory is empty.")]
        private bool _applyInitialIfResultEmpty = true;
#pragma warning restore 0414

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

        private bool _isLoading;

        /// <summary>Sum of all item counts in this container.</summary>
        public int TotalItemCount => _storage != null ? _storage.TotalCount : 0;

        /// <summary>Number of distinct item ids with count greater than zero.</summary>
        public int UniqueItemCount => _storage != null ? _storage.UniqueCount : 0;

        /// <summary>True when <see cref="TotalItemCount" /> is zero.</summary>
        public bool IsEmpty => TotalItemCount <= 0;

        /// <summary>True when this component uses <see cref="InventoryStorageMode.SlotGrid" />.</summary>
        public bool IsSlotInventory => _storageMode == InventoryStorageMode.SlotGrid;

        /// <summary>Number of physical slots in slot-grid mode; zero in aggregated mode.</summary>
        public int SlotCapacity => _storage is ISlottedInventory slotted ? slotted.SlotCapacity :
            _storageMode == InventoryStorageMode.SlotGrid ? _slotCount : 0;

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
            return I != null ? I : FindFirstObjectByType<InventoryComponent>();
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
                if (!_isLoading)
                {
                    Load();
                }
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
    }
}
