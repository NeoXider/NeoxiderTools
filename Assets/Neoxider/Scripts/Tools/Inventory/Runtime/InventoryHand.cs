using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     How the equipped item scale is combined with <see cref="HandView" /> base scale.
    /// </summary>
    public enum HandScaleMode
    {
        /// <summary>Multiply by <see cref="InventoryHand" /> hand scale fixed value.</summary>
        Fixed,

        /// <summary>Multiply by (1 + hand scale offset) on top of item HandView scale.</summary>
        Relative
    }

    /// <summary>
    ///     Shows one selected inventory item at <see cref="Transform" /> Hand Anchor; supports packed or physical slot indices and optional <see cref="Selector" /> / <see cref="InventoryDropper" />.
    /// </summary>
    [NeoDoc("Tools/Inventory/InventoryHand.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryHand")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryHand))]
    public sealed class InventoryHand : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField]
        [Tooltip("Target inventory; if null and Auto Find is on, uses InventoryComponent.FindDefault().")]
        private InventoryComponent _inventory;

        [SerializeField] [Tooltip("Transform where the equipped item prefab is parented (e.g. hand bone).")]
        private Transform _handAnchor;

        [SerializeField]
        [Tooltip("Optional Selector: Count tracks selectable slots; Next/Previous change the equipped slot.")]
        private Selector _selector;

        [SerializeField] [Tooltip("If Inventory is null, resolve via FindDefault() on enable.")]
        private bool _autoFindInventory = true;

        [SerializeField]
        [Tooltip("Optional dropper used by DropEquipped() to spawn world items with physics/pickup.")]
        private InventoryDropper _dropper;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Prefab used when item data has no WorldDropPrefab.")]
        private GameObject _fallbackHandPrefab;

        [SerializeField]
        [Tooltip("Fixed = multiply by Hand Scale Fixed; Relative = multiply by (1 + Hand Scale Offset) after HandView scale.")]
        private HandScaleMode _scaleInHandMode = HandScaleMode.Relative;

        [SerializeField]
        [Tooltip("Hand scale multiplier when Scale In Hand Mode = Fixed.")]
        private float _handScaleFixed = 1f;

        [SerializeField]
        [Tooltip("Hand scale delta when Scale In Hand Mode = Relative: effective = 1 + offset.")]
        private float _handScaleOffset;

        [SerializeField]
        [Tooltip("When true, disable all Collider/Collider2D on the equipped instance (default: on).")]
        private bool _disableCollidersInHand = true;

        [Header("Selector Sync")]
        [SerializeField]
        [Tooltip("When inventory changes, refresh Selector.Count and clamp the current index.")]
        private bool _syncSelectorOnInventoryChanged = true;

        [SerializeField]
        [Tooltip("Allow slot index -1 (empty hand) when items exist; enable Allow Empty Effective Index on Selector if used.")]
        private bool _allowEmptySlot = true;

        [SerializeField]
        [Tooltip("When enabled and inventory is slot-based, Slot Index means a physical slot index including empty slots.")]
        private bool _usePhysicalSlotIndices = true;

        [Header("Drop (when Dropper assigned)")]
        [SerializeField]
        [Tooltip("When Dropper is set, drop key removes the equipped item via Dropper (Dropper key input is disabled while linked).")]
        private bool _allowDropInput = true;

        [SerializeField] [Tooltip("Key to drop the equipped item.")]
        private KeyCode _dropKey = KeyCode.G;

        [Header("Use")] [SerializeField] [Tooltip("Call UseEquippedItem() when Use Key is pressed.")]
        private bool _allowUseInput = true;

        [SerializeField] [Tooltip("Key to use the equipped item.")]
        private KeyCode _useKey = KeyCode.E;

        [Header("Events")]
        /// <summary>Raised when the equipped slot or item changes; argument is item id or -1 if empty.</summary>
        public UnityEvent<int> OnEquippedChanged = new();

        /// <summary>Raised from UseEquippedItem(); subscribe for gameplay effects; consume with inventory.TryConsume(itemId, 1) if needed.</summary>
        public UnityEvent<int> OnUseItemRequested = new();

        private bool _isSyncingSelector;
        private bool _savedDropperAllowInput = true;

        private GameObject _spawnedInstance;

        /// <summary>Bound inventory reference.</summary>
        public InventoryComponent Inventory => _inventory;

        /// <summary>Current slot index (packed or physical depending on settings).</summary>
        public int SlotIndex { get; private set; }

        /// <summary>Item id in hand, or -1 if none (NeoCondition-friendly).</summary>
        public int EquippedItemId => ResolveEquippedRecord()?.EffectiveItemId ?? -1;

        /// <summary>Database entry for <see cref="EquippedItemId" />, if any.</summary>
        public InventoryItemData EquippedItemData => ResolveInventory()?.GetItemData(EquippedItemId);

        private void Update()
        {
            if (_allowUseInput && KeyInputCompat.GetKeyDown(_useKey))
            {
                UseEquippedItem();
                return;
            }

            if (_dropper == null || !_allowDropInput || !KeyInputCompat.GetKeyDown(_dropKey))
            {
                return;
            }

            DropEquipped();
        }

        private void OnEnable()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv != null)
            {
                inv.OnInventoryChanged.AddListener(OnInventoryChanged);
            }

            if (_selector != null)
            {
                _selector.OnSelectionChanged.AddListener(OnSelectorSelectionChanged);
            }

            if (_dropper != null)
            {
                _savedDropperAllowInput = _dropper.AllowDropInput;
                _dropper.AllowDropInput = false;
            }

            RefreshSlotFromInventory();
        }

        private void OnDisable()
        {
            if (_dropper != null)
            {
                _dropper.AllowDropInput = _savedDropperAllowInput;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv != null)
            {
                inv.OnInventoryChanged.RemoveListener(OnInventoryChanged);
            }

            if (_selector != null)
            {
                _selector.OnSelectionChanged.RemoveListener(OnSelectorSelectionChanged);
            }
        }

        private void OnInventoryChanged()
        {
            if (!_syncSelectorOnInventoryChanged)
            {
                return;
            }

            RefreshSlotFromInventory();
        }

        private void OnSelectorSelectionChanged(int index)
        {
            if (_isSyncingSelector)
            {
                return;
            }

            SetSlotIndex(index);
        }

        /// <summary>Moves to the next selectable slot (wraps).</summary>
        public void SelectNext()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int count = GetSelectableSlotCount(inv);
            if (count <= 0)
            {
                return;
            }

            SlotIndex = (SlotIndex + 1) % count;
            ApplySlotAndSync();
        }

        /// <summary>Moves to the previous selectable slot (wraps).</summary>
        public void SelectPrevious()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int count = GetSelectableSlotCount(inv);
            if (count <= 0)
            {
                return;
            }

            SlotIndex = SlotIndex <= 0 ? count - 1 : SlotIndex - 1;
            ApplySlotAndSync();
        }

        /// <summary>Invokes <see cref="OnUseItemRequested" /> then <see cref="PickableItem.Activate" /> on the spawned instance if present.</summary>
        public void UseEquippedItem()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int itemId = EquippedItemId;
            if (itemId < 0)
            {
                return;
            }

            OnUseItemRequested?.Invoke(itemId);

            if (_spawnedInstance != null)
            {
                PickableItem pickable = _spawnedInstance.GetComponent<PickableItem>();
                pickable?.Activate();
            }
        }

        /// <summary>Drops via <see cref="InventoryDropper" /> using physical slot or packed index per settings.</summary>
        /// <returns>Amount dropped, or 0 if nothing equipped or no dropper.</returns>
        public int DropEquipped(int amount = 1)
        {
            if (_dropper == null || amount <= 0)
            {
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return 0;
            }

            int itemId = EquippedItemId;
            if (itemId < 0)
            {
                return 0;
            }

            return _usePhysicalSlotIndices && inv.IsSlotInventory
                ? _dropper.DropSlot(SlotIndex, amount)
                : _dropper.DropPackedIndex(SlotIndex, amount);
        }

        /// <summary>Sets current slot; index -1 clears hand when Allow Empty Slot is enabled.</summary>
        public void SetSlotIndex(int index)
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int count = GetSelectableSlotCount(inv);
            if (count <= 0)
            {
                SlotIndex = 0;
                SyncSelectorAndRefreshHand();
                return;
            }

            if (index == -1 && _allowEmptySlot)
            {
                SlotIndex = -1;
                inv.SelectedItemId = -1;
                SyncSelectorAndRefreshHand();
                OnEquippedChanged?.Invoke(-1);
                return;
            }

            SlotIndex = Mathf.Clamp(index, 0, count - 1);
            ApplySlotAndSync();
        }

        /// <summary>Re-syncs slot index, Selector, and hand visual from inventory contents.</summary>
        public void RefreshSlotFromInventory()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                DestroySpawned();
                SyncSelectorAndRefreshHand();
                return;
            }

            int count = GetSelectableSlotCount(inv);
            if (count <= 0)
            {
                SlotIndex = 0;
                inv.SelectedItemId = -1;
                DestroySpawned();
                SyncSelectorAndRefreshHand();
                return;
            }

            if (SlotIndex >= count)
            {
                SlotIndex = count - 1;
            }

            int itemId = ResolveItemIdForSlot(inv, SlotIndex);
            inv.SelectedItemId = itemId;
            SyncSelectorAndRefreshHand();
        }

        private void ApplySlotAndSync()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int count = GetSelectableSlotCount(inv);
            if (count <= 0)
            {
                inv.SelectedItemId = -1;
                SyncSelectorAndRefreshHand();
                return;
            }

            if (SlotIndex < 0)
            {
                inv.SelectedItemId = -1;
                SyncSelectorAndRefreshHand();
                OnEquippedChanged?.Invoke(-1);
                return;
            }

            int itemId = ResolveItemIdForSlot(inv, SlotIndex);
            inv.SelectedItemId = itemId;
            SyncSelectorAndRefreshHand();
            OnEquippedChanged?.Invoke(itemId);
        }

        private void SyncSelectorAndRefreshHand()
        {
            if (_isSyncingSelector)
            {
                return;
            }

            _isSyncingSelector = true;
            try
            {
                InventoryComponent inv = ResolveInventory();
                int count = inv != null ? GetSelectableSlotCount(inv) : 0;

                if (_selector != null)
                {
                    _selector.Count = count > 0 ? count : _allowEmptySlot ? 1 : 0;
                    if (count > 0)
                    {
                        _selector.Set(SlotIndex);
                    }
                    else if (_allowEmptySlot && _selector.Count == 1)
                    {
                        _selector.Set(0);
                    }
                }

                RefreshHandVisual();
            }
            finally
            {
                _isSyncingSelector = false;
            }
        }

        private void RefreshHandVisual()
        {
            DestroySpawned();

            Transform anchor = _handAnchor != null ? _handAnchor : transform;
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            InventoryItemRecord record = ResolveEquippedRecord(inv);
            if (record == null || record.EffectiveItemId < 0)
            {
                return;
            }

            int itemId = record.EffectiveItemId;
            InventoryItemData data = inv.GetItemData(itemId);
            GameObject prefab = data != null && data.WorldDropPrefab != null
                ? data.WorldDropPrefab
                : _fallbackHandPrefab;
            if (prefab == null)
            {
                return;
            }

            _spawnedInstance = Instantiate(prefab, anchor.position, anchor.rotation, anchor);

            HandView handView = _spawnedInstance.GetComponentInChildren<HandView>(true);
            float baseScale = handView != null ? handView.ScaleInHand : 1f;
            if (handView != null)
            {
                _spawnedInstance.transform.localPosition = handView.PositionOffset;
                _spawnedInstance.transform.localRotation = Quaternion.Euler(handView.RotationOffset);
            }
            else
            {
                _spawnedInstance.transform.localPosition = Vector3.zero;
                _spawnedInstance.transform.localRotation = Quaternion.identity;
            }

            float handScale = _scaleInHandMode == HandScaleMode.Relative ? 1f + _handScaleOffset : _handScaleFixed;
            _spawnedInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, baseScale * handScale);

            SetPhysicsOnInstance(_spawnedInstance, false);
            if (_disableCollidersInHand)
            {
                SetCollidersOnInstance(_spawnedInstance, false);
            }

            if (record.IsInstance && record.Instance != null)
            {
                InventoryItemStateUtility.RestoreInstance(_spawnedInstance, record.Instance);
            }
        }

        private int GetSelectableSlotCount(InventoryComponent inv)
        {
            if (inv == null)
            {
                return 0;
            }

            return _usePhysicalSlotIndices && inv.IsSlotInventory ? inv.SlotCapacity : inv.GetNonEmptySlotCount();
        }

        private int ResolveItemIdForSlot(InventoryComponent inv, int slotIndex)
        {
            InventoryItemRecord record = ResolveEquippedRecord(inv, slotIndex);
            return record != null ? record.EffectiveItemId : -1;
        }

        private InventoryItemRecord ResolveEquippedRecord()
        {
            return ResolveEquippedRecord(ResolveInventory(), SlotIndex);
        }

        private InventoryItemRecord ResolveEquippedRecord(InventoryComponent inv)
        {
            return ResolveEquippedRecord(inv, SlotIndex);
        }

        private InventoryItemRecord ResolveEquippedRecord(InventoryComponent inv, int slotIndex)
        {
            if (inv == null || slotIndex < 0)
            {
                return null;
            }

            return _usePhysicalSlotIndices && inv.IsSlotInventory
                ? inv.GetPhysicalSlotRecord(slotIndex)
                : inv.GetRecordAtSlotIndex(slotIndex);
        }

        private static void SetPhysicsOnInstance(GameObject instance, bool enabled)
        {
            if (instance == null)
            {
                return;
            }

            Rigidbody[] rbs = instance.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rbs.Length; i++)
            {
                rbs[i].isKinematic = !enabled;
            }

            Rigidbody2D[] rb2ds = instance.GetComponentsInChildren<Rigidbody2D>(true);
            for (int i = 0; i < rb2ds.Length; i++)
            {
                rb2ds[i].simulated = enabled;
            }
        }

        private static void SetCollidersOnInstance(GameObject instance, bool enabled)
        {
            if (instance == null)
            {
                return;
            }

            Collider[] cols = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i].enabled = enabled;
            }

            Collider2D[] cols2D = instance.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols2D.Length; i++)
            {
                cols2D[i].enabled = enabled;
            }
        }

        private void DestroySpawned()
        {
            if (_spawnedInstance != null)
            {
                Destroy(_spawnedInstance);
                _spawnedInstance = null;
            }
        }

        private InventoryComponent ResolveInventory()
        {
            if (_inventory != null)
            {
                return _inventory;
            }

            if (!_autoFindInventory)
            {
                return null;
            }

            _inventory = InventoryComponent.FindDefault();
            return _inventory;
        }
    }
}
