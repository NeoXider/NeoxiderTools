using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     UI grid bound to one <see cref="InventoryComponent" /> in slot-grid mode; optional click-to-select transfer via <see cref="InventoryTransferService" />.
    /// </summary>
    [NeoDoc("Tools/Inventory/InventorySlotGridView.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventorySlotGridView")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventorySlotGridView))]
    public sealed class InventorySlotGridView : MonoBehaviour
    {
        [Header("Source")] [SerializeField] [Tooltip("Inventory to display; must use Storage Mode = Slot Grid.")]
        private InventoryComponent _inventory;

        [SerializeField] [Tooltip("If Inventory is null, use InventoryComponent.FindDefault() on enable.")]
        private bool _autoFindInventory = true;

        [SerializeField] [Tooltip("Refresh when the bound inventory finishes Load().")]
        private bool _refreshOnLoaded = true;

        [SerializeField] [Tooltip("Schedule Refresh() on the next frame after enable (layout-safe).")]
        private bool _refreshNextFrameOnEnable = true;

        [Header("Slots")] [SerializeField] [Tooltip("Prefab instantiated per slot when Manual Slots is empty.")]
        private InventorySlotView _slotPrefab;

        [SerializeField] [Tooltip("Parent for spawned slot views; defaults to this transform.")]
        private Transform _slotsRoot;

        [SerializeField] [Tooltip("Pre-placed slot views; when non-empty, prefab spawning is skipped.")]
        private List<InventorySlotView> _manualSlots = new();

        [Header("Transfer")]
        [SerializeField]
        [Tooltip("First click selects a slot; second click on another grid runs InventoryTransferService.Transfer.")]
        private bool _enableClickTransfer = true;

        private static InventorySlotGridView _selectedGrid;
        private static int _selectedSlot = -1;

        private readonly List<InventorySlotView> _spawnedSlots = new();

        /// <summary>Currently bound inventory.</summary>
        public InventoryComponent Inventory => _inventory;

        private void OnEnable()
        {
            BindIfNeeded();
            Subscribe();
            Refresh();
            if (_refreshNextFrameOnEnable)
            {
                StartCoroutine(RefreshNextFrame());
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (ReferenceEquals(_selectedGrid, this))
            {
                _selectedGrid = null;
                _selectedSlot = -1;
            }
        }

        /// <summary>Rebinds the grid to another inventory and refreshes.</summary>
        public void SetInventory(InventoryComponent inventory)
        {
            if (_inventory == inventory)
            {
                return;
            }

            Unsubscribe();
            _inventory = inventory;
            Subscribe();
            Refresh();
        }

        /// <summary>Rebuilds slot visuals from the bound inventory.</summary>
        public void Refresh()
        {
            if (_inventory == null || !_inventory.IsSlotInventory)
            {
                ClearAll();
                return;
            }

            EnsureSlotViews(_inventory.SlotCapacity);
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                InventorySlotView slotView = _spawnedSlots[i];
                if (slotView == null)
                {
                    continue;
                }

                slotView.gameObject.SetActive(i < _inventory.SlotCapacity);
                if (i >= _inventory.SlotCapacity)
                {
                    continue;
                }

                InventorySlotState slot = _inventory.GetSlot(i);
                InventoryItemData data = !slot.IsEmpty ? _inventory.GetItemData(slot.EffectiveItemId) : null;
                bool selected = ReferenceEquals(_selectedGrid, this) && _selectedSlot == i;
                slotView.Bind(this, i, data, slot, selected);
            }
        }

        /// <summary>Selection / transfer handler invoked from <see cref="InventorySlotView" />.</summary>
        public void HandleSlotClick(int slotIndex)
        {
            if (!_enableClickTransfer || _inventory == null || !_inventory.IsSlotInventory)
            {
                return;
            }

            if (!ReferenceEquals(_selectedGrid, null) &&
                ReferenceEquals(_selectedGrid, this) &&
                _selectedSlot == slotIndex)
            {
                _selectedGrid = null;
                _selectedSlot = -1;
                Refresh();
                return;
            }

            if (_selectedGrid == null)
            {
                _selectedGrid = this;
                _selectedSlot = slotIndex;
                Refresh();
                return;
            }

            InventoryTransferService.Transfer(_selectedGrid.Inventory, _selectedSlot, _inventory, slotIndex);

            InventorySlotGridView sourceGrid = _selectedGrid;
            _selectedGrid = null;
            _selectedSlot = -1;
            sourceGrid?.Refresh();
            Refresh();
        }

        private void Subscribe()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.OnInventoryChanged.AddListener(Refresh);
            if (_refreshOnLoaded)
            {
                _inventory.OnLoaded.AddListener(Refresh);
            }
        }

        private void Unsubscribe()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.OnInventoryChanged.RemoveListener(Refresh);
            if (_refreshOnLoaded)
            {
                _inventory.OnLoaded.RemoveListener(Refresh);
            }
        }

        private void BindIfNeeded()
        {
            if (_inventory == null && _autoFindInventory)
            {
                _inventory = InventoryComponent.FindDefault();
            }

            if (_slotsRoot == null)
            {
                _slotsRoot = transform;
            }
        }

        private void EnsureSlotViews(int count)
        {
            if (_manualSlots != null && _manualSlots.Count > 0)
            {
                _spawnedSlots.Clear();
                for (int i = 0; i < _manualSlots.Count; i++)
                {
                    if (_manualSlots[i] != null)
                    {
                        _spawnedSlots.Add(_manualSlots[i]);
                    }
                }

                return;
            }

            if (_slotPrefab == null || _slotsRoot == null)
            {
                return;
            }

            while (_spawnedSlots.Count < count)
            {
                InventorySlotView created = Instantiate(_slotPrefab, _slotsRoot);
                _spawnedSlots.Add(created);
            }
        }

        private void ClearAll()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i] != null)
                {
                    _spawnedSlots[i].gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            Refresh();
        }
    }
}
