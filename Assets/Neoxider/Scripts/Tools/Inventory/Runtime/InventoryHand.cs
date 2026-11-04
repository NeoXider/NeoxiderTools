using Neo.Extensions;
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

        [SerializeField] [Tooltip("Optional dropper used by DropEquipped() to spawn world items with physics/pickup.")]
        private InventoryDropper _dropper;

        [Header("Visual")] [SerializeField] [Tooltip("Prefab used when item data has no WorldDropPrefab.")]
        private GameObject _fallbackHandPrefab;

        [SerializeField]
        [Tooltip(
            "Fixed = multiply by Hand Scale Fixed; Relative = multiply by (1 + Hand Scale Offset) after HandView scale.")]
        private HandScaleMode _scaleInHandMode = HandScaleMode.Relative;

        [SerializeField] [Tooltip("Hand scale multiplier when Scale In Hand Mode = Fixed.")]
        private float _handScaleFixed = 1f;

        [SerializeField] [Tooltip("Hand scale delta when Scale In Hand Mode = Relative: effective = 1 + offset.")]
        private float _handScaleOffset;

        [SerializeField] [Tooltip("When true, disable all Collider/Collider2D on the equipped instance (default: on).")]
        private bool _disableCollidersInHand = true;

        [Header("Selector Sync")]
        [SerializeField]
        [Tooltip("When inventory changes, refresh Selector.Count and clamp the current index.")]
        private bool _syncSelectorOnInventoryChanged = true;

        [SerializeField]
        [Tooltip(
            "Allow slot index -1 (empty hand) when items exist; enable Allow Empty Effective Index on Selector if used.")]
        private bool _allowEmptySlot = true;

        [SerializeField]
        [Tooltip(
            "When enabled and inventory is slot-based, Slot Index means a physical slot index including empty slots.")]
        private bool _usePhysicalSlotIndices = true;

        [Header("Drop (when Dropper assigned)")]
        [SerializeField]
        [Tooltip(
            "When Dropper is set, drop key removes the equipped item via Dropper (Dropper key input is disabled while linked).")]
        private bool _allowDropInput = true;

        [SerializeField] [Tooltip("Key to drop the equipped item.")]
        private KeyCode _dropKey = KeyCode.G;

        [Header("Use")] [SerializeField] [Tooltip("Call UseEquippedItem() when Use Key is pressed.")]
        private bool _allowUseInput = true;

        [SerializeField] [Tooltip("Key to use the equipped item.")]
        private KeyCode _useKey = KeyCode.E;

        /// <summary>Raised when the equipped slot or item changes; argument is item id or -1 if empty.</summary>
        [Header("Events")]
        public UnityEvent<int> OnEquippedChanged = new();

        /// <summary>Legacy type-id use event. Prefer OnUseSelectionRequested when instance identity matters.</summary>
        public UnityEvent<int> OnUseItemRequested = new();

        /// <summary>Exact item captured before callbacks. Use InventoryUseAction or TryConsumeSelection for consumption.</summary>
        public UnityEvent<InventorySelection> OnUseSelectionRequested = new();

        private bool _isSyncingSelector;
        private bool _savedDropperAllowInput = true;

        private GameObject _spawnedInstance;
        private GameObject _spawnedPrefab;
        private InventoryItemRecord _spawnedRecord;
        private int _spawnedSlotIndex;
        private int _visualRefreshVersion;
        private Selector _writtenSelector;
        private int _writtenSelectorCount;
        private bool _isUsing;

        /// <summary>Bound inventory reference.</summary>
        public InventoryComponent Inventory => _inventory;

        /// <summary>Current slot index (packed or physical depending on settings).</summary>
        public int SlotIndex { get; private set; }

        /// <summary>Item id in hand, or -1 if none (NeoCondition-friendly).</summary>
        /// <remarks>
        ///     WHY: NeoCondition polls this getter; it reads the live slot instead of cloning the record (and its
        ///     instance payload) on every read, with the same result.
        /// </remarks>
        public int EquippedItemId => ResolveItemIdForSlot(ResolveInventory(), SlotIndex);

        /// <summary>Database entry for <see cref="EquippedItemId" />, if any.</summary>
        public InventoryItemData EquippedItemData
        {
            get
            {
                // WHY: Unity null check; "?." would treat a destroyed inventory as alive.
                InventoryComponent inventory = ResolveInventory();
                return inventory != null ? inventory.GetItemData(EquippedItemId) : null;
            }
        }

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

            _writtenSelector = null;

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

            DestroySpawned();
        }

        private void OnDestroy()
        {
            DestroySpawned();
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

        /// <summary>Captures the current slot and unique instance without removing it.</summary>
        public bool TryGetEquippedSelection(out InventorySelection selection)
        {
            InventoryComponent inventory = ResolveInventory();
            selection = null;
            return inventory != null &&
                   inventory.TryCaptureSelection(SlotIndex, out selection, _usePhysicalSlotIndices);
        }

        /// <summary>
        ///     Publishes the legacy event, then the captured selection if it still exists.
        ///     Activates only the original visual if callbacks did not replace, consume or unequip it.
        /// </summary>
        public void UseEquippedItem()
        {
            InventoryComponent inv = ResolveInventory();
            if (_isUsing || inv == null || !TryGetEquippedSelection(out InventorySelection selection))
            {
                return;
            }

            GameObject originalVisual = _spawnedInstance;
            int originalSlot = SlotIndex;
            int originalVisualVersion = _visualRefreshVersion;
            _isUsing = true;
            try
            {
                OnUseItemRequested?.Invoke(selection.ItemId);
                if (inv.IsSelectionCurrent(selection))
                {
                    OnUseSelectionRequested?.Invoke(selection);
                }

                // WHY: The version (not only the object) detects a refresh: an unchanged visual survives a consume
                // of one unit from its stack, which must still suppress activation like a replaced visual.
                if (originalVisual != null && originalVisual == _spawnedInstance && originalSlot == SlotIndex &&
                    originalVisualVersion == _visualRefreshVersion && inv == ResolveInventory() &&
                    inv.IsSelectionCurrent(selection) && originalVisual.TryGetComponent(out PickableItem pickable))
                {
                    pickable.Activate();
                }
            }
            finally
            {
                _isUsing = false;
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
                    // WHY: Selector.Count and Set re-apply the whole selection (and replicate it when networked)
                    // even for unchanged values; the hand re-syncs on every inventory change, so only real
                    // changes are written. The count is compared with what the hand last wrote to this selector,
                    // not only with the Count getter (which reports Items.Length while no explicit count is set),
                    // so the first sync after enable still switches the selector to an explicit count as before.
                    int selectorCount = count > 0 ? count : _allowEmptySlot ? 1 : 0;
                    if (!ReferenceEquals(_writtenSelector, _selector) || _writtenSelectorCount != selectorCount ||
                        _selector.Count != selectorCount)
                    {
                        _selector.Count = selectorCount;
                        _writtenSelector = _selector;
                        _writtenSelectorCount = selectorCount;
                    }

                    if (count > 0)
                    {
                        SetSelectorIndex(SlotIndex);
                    }
                    else if (_allowEmptySlot && _selector.Count == 1)
                    {
                        SetSelectorIndex(0);
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
            _visualRefreshVersion++;
            Transform anchor = _handAnchor != null ? _handAnchor : transform;
            InventoryComponent inv = ResolveInventory();
            InventoryItemRecord record = inv != null ? ResolveEquippedRecord(inv) : null;
            if (record != null && record.EffectiveItemId < 0)
            {
                record = null;
            }

            GameObject prefab = null;
            if (record != null)
            {
                InventoryItemData data = inv.GetItemData(record.EffectiveItemId);
                prefab = data != null && data.WorldDropPrefab != null ? data.WorldDropPrefab : _fallbackHandPrefab;
            }

            // WHY: Every inventory change re-syncs the hand; rebuilding an unchanged visual cost an
            // Instantiate/Destroy pair per pickup and reset any runtime state on the visual.
            if (CanKeepSpawnedVisual(record, prefab, anchor))
            {
                return;
            }

            DestroySpawned();
            if (prefab == null)
            {
                return;
            }

            _spawnedInstance = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            _spawnedPrefab = prefab;
            _spawnedRecord = record;
            _spawnedSlotIndex = SlotIndex;

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

        private void SetSelectorIndex(int index)
        {
            if (_selector.Value != index)
            {
                _selector.Set(index);
            }
        }

        private bool CanKeepSpawnedVisual(InventoryItemRecord record, GameObject prefab, Transform anchor)
        {
            if (_spawnedInstance == null || _spawnedRecord == null || record == null || prefab == null ||
                prefab != _spawnedPrefab || _spawnedSlotIndex != SlotIndex ||
                _spawnedInstance.transform.parent != anchor ||
                record.EffectiveItemId != _spawnedRecord.EffectiveItemId ||
                record.IsInstance != _spawnedRecord.IsInstance)
            {
                return false;
            }

            return !record.IsInstance || InventoryComponent.SameInstanceState(record.Instance, _spawnedRecord.Instance);
        }

        private int GetSelectableSlotCount(InventoryComponent inv)
        {
            if (inv == null)
            {
                return 0;
            }

            return _usePhysicalSlotIndices && inv.IsSlotInventory ? inv.SlotCapacity : inv.GetNonEmptySlotCount();
        }

        /// <summary>Item id of <paramref name="slotIndex" />, read from the live slot without cloning its record.</summary>
        private int ResolveItemIdForSlot(InventoryComponent inv, int slotIndex)
        {
            if (inv == null || slotIndex < 0)
            {
                return -1;
            }

            if (_usePhysicalSlotIndices && inv.IsSlotInventory)
            {
                InventorySlotState slot = inv.PeekSlotForView(slotIndex);
                return slot != null && !slot.IsEmpty ? slot.EffectiveItemId : -1;
            }

            return inv.GetItemIdAtSlotIndex(slotIndex);
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
                _spawnedInstance.SafeDestroy();
            }

            _spawnedInstance = null;
            _spawnedPrefab = null;
            _spawnedRecord = null;
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
