using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Tools
{
    [NeoDoc("Tools/Inventory/InventoryDropper.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryDropper")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryDropper))]
    public sealed class InventoryDropper : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField]
        [Tooltip("Inventory source. If null and Auto Find enabled, InventoryComponent.FindDefault() is used.")]
        private InventoryComponent _inventory;

        [SerializeField] [Tooltip("Optional drop position source. If empty, this transform is used.")]
        private Transform _dropPoint;

        [SerializeField] [Tooltip("Resolve InventoryComponent.FindDefault() if Inventory is not assigned.")]
        private bool _autoFindInventory = true;

        [Header("Input")] [SerializeField] [Tooltip("Allow dropping via keyboard input.")]
        private bool _allowDropInput = true;

        [SerializeField] [Tooltip("Master switch for dropping logic.")]
        private bool _canDrop = true;

        [SerializeField] [Tooltip("Key used for default drop action.")]
        private KeyCode _dropKey = KeyCode.G;

        [SerializeField] [Tooltip("Use inventory.SelectedItemId when pressing drop key.")]
        private bool _dropSelectedOnKey = true;

        [SerializeField]
        [Tooltip(
            "Fallback item id when Drop Selected On Key is disabled. Use -1 to drop the last item (by snapshot order).")]
        private int _dropItemIdOnKey;

        [SerializeField]
        [Tooltip(
            "When current item (selected or configured id) has count 0, drop the next available item from inventory.")]
        private bool _dropNextWhenEmpty = true;

        [SerializeField] [Min(1)] [Tooltip("Amount dropped by keyboard action.")]
        private int _dropAmountOnKey = 1;

        [Header("Spawn")] [SerializeField] [Tooltip("Fallback prefab when item has no WorldDropPrefab.")]
        private GameObject _fallbackDropPrefab;

        [SerializeField] [Tooltip("Random offset radius around drop point.")] [Min(0f)]
        private float _randomRadius;

        [SerializeField] [Tooltip("Forward force direction in local space.")]
        private Vector3 _throwDirection = Vector3.forward;

        [SerializeField] [Tooltip("Impulse force applied to spawned rigidbody.")] [Min(0f)]
        private float _throwImpulse = 2f;

        [Header("Physics")] [SerializeField] [Tooltip("Ensure Rigidbody on spawned object.")]
        private bool _addRigidbody3D = true;

        [SerializeField] [Tooltip("Ensure Rigidbody2D on spawned object.")]
        private bool _addRigidbody2D;

        [SerializeField] [Tooltip("Ensure collider exists on spawned object.")]
        private bool _addColliderIfMissing = true;

        [SerializeField] [Tooltip("Use trigger collider for auto-pick flow.")]
        private bool _useTriggerCollider = true;

        [SerializeField] [Min(0.01f)] private float _defaultColliderRadius = 0.35f;

        [Header("Pickup")]
        [SerializeField]
        [Tooltip("Ensure PickableItem component and configure it for dropped item.")]
        private bool _configurePickableItem = true;

        [SerializeField] [Tooltip("Set target inventory on dropped PickableItem.")]
        private bool _setTargetInventoryForPickable;

        [Header("Events")] public UnityEvent<int, int, GameObject> OnItemDropped = new();
        public UnityEvent<int, int> OnDropFailed = new();

        [Serializable]
        private sealed class PendingReturn
        {
            public InventoryComponent Inventory;
            public InventoryItemRecord Record;
            public int PhysicalSlotIndex;
        }

        private enum DropSource
        {
            ItemId,
            Packed,
            Physical
        }

        [SerializeField] [HideInInspector] private List<PendingReturn> _pendingReturns = new();
        private bool _isDropping;

        public InventoryComponent Inventory => _inventory;

        /// <summary>Quantity still owned by this dropper after a conflicted, capacity-limited return.</summary>
        public long PendingReturnCount
        {
            get
            {
                long count = 0;
                for (int i = 0; i < _pendingReturns.Count; i++)
                {
                    count += _pendingReturns[i].Record.EffectiveCount;
                }

                return count;
            }
        }

        /// <summary>Copies of records awaiting recovery; these copies do not transfer ownership.</summary>
        public List<InventoryItemRecord> GetPendingReturnRecords()
        {
            List<InventoryItemRecord> records = new(_pendingReturns.Count);
            for (int i = 0; i < _pendingReturns.Count; i++)
            {
                records.Add(_pendingReturns[i].Record.Clone());
            }

            return records;
        }

        /// <summary>Retries returns to the originating inventory/slot; returns the exact recovered quantity.</summary>
        [Button(PlayModeOnly = true)]
        public long RetryPendingReturns()
        {
            if (_isDropping)
            {
                return 0;
            }

            _isDropping = true;
            long restored = 0;
            ExceptionDispatchInfo firstFault = null;
            try
            {
                for (int i = _pendingReturns.Count - 1; i >= 0; i--)
                {
                    InventoryComponent.MutationReceipt receipt = TryReturn(_pendingReturns[i]);
                    if (receipt == null)
                    {
                        continue;
                    }

                    restored += receipt.AcceptedCount;
                    receipt.Publish();
                    if (firstFault == null)
                    {
                        firstFault = receipt.Fault;
                    }
                }
            }
            finally
            {
                _isDropping = false;
            }

            firstFault?.Throw();
            return restored;
        }

        /// <summary>
        ///     Drop hotkey input. When an InventoryHand is linked to a Dropper, it can set this to false so that only the
        ///     hand handles the hotkey.
        /// </summary>
        public bool AllowDropInput
        {
            get => _allowDropInput;
            set => _allowDropInput = value;
        }

        public bool CanDrop
        {
            get => _canDrop;
            set => _canDrop = value;
        }

        private void Update()
        {
            if (!_allowDropInput || !_canDrop || !KeyInputCompat.GetKeyDown(_dropKey))
            {
                return;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(-1, _dropAmountOnKey);
                return;
            }

            int itemId = ResolveItemIdForKeyDrop(inv);
            if (itemId < 0)
            {
                OnDropFailed?.Invoke(-1, _dropAmountOnKey);
                return;
            }

            DropById(itemId, _dropAmountOnKey);
        }

        /// <summary>
        ///     Resolves which item id to drop on key press: selected, configured id, or -1 = last; with fallback to next available
        ///     when empty.
        /// </summary>
        private int ResolveItemIdForKeyDrop(InventoryComponent inv)
        {
            int candidate = _dropSelectedOnKey
                ? inv.SelectedItemId
                : _dropItemIdOnKey == -1
                    ? inv.GetLastItemId()
                    : _dropItemIdOnKey;

            if (_dropNextWhenEmpty && (candidate < 0 || inv.GetCount(candidate) <= 0))
            {
                candidate = inv.GetFirstItemId();
            }

            return candidate;
        }

        public int DropSelected(int amount = 1)
        {
            if (!_canDrop)
            {
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            int itemId = ResolveItemIdForKeyDrop(inv);
            if (itemId < 0)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            return DropById(itemId, amount);
        }

        public int DropById(int itemId, int amount = 1)
        {
            return DropAt(itemId, amount, DropSource.ItemId);
        }

        public int DropData(InventoryItemData itemData, int amount = 1)
        {
            if (!_canDrop)
            {
                return 0;
            }

            if (itemData == null)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            return DropById(itemData.ItemId, amount);
        }

        public int DropByIdOne(int itemId)
        {
            return DropById(itemId);
        }

        public int DropPackedIndex(int packedIndex, int amount = 1)
        {
            return DropAt(packedIndex, amount, DropSource.Packed);
        }

        public int DropSlot(int slotIndex, int amount = 0)
        {
            return DropAt(slotIndex, amount, DropSource.Physical);
        }

        /// <summary>
        ///     Drops the first item (by snapshot order) from inventory. Returns amount actually dropped, or 0 if empty or CanDrop
        ///     is false.
        /// </summary>
        public int DropFirst(int amount = 1)
        {
            if (!_canDrop)
            {
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            if (inv.GetNonEmptySlotCount() <= 0)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            return DropPackedIndex(0, amount);
        }

        /// <summary>
        ///     Drops the last item (by snapshot order) from inventory. Returns amount actually dropped, or 0 if empty or CanDrop
        ///     is false.
        /// </summary>
        public int DropLast(int amount = 1)
        {
            if (!_canDrop)
            {
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            int count = inv.GetNonEmptySlotCount();
            if (count <= 0)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            return DropPackedIndex(count - 1, amount);
        }

        public int DropConfiguredById()
        {
            if (!_canDrop)
            {
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(_dropItemIdOnKey, _dropAmountOnKey);
                return 0;
            }

            int itemId = _dropItemIdOnKey == -1 ? inv.GetLastItemId() : _dropItemIdOnKey;
            if (_dropNextWhenEmpty && (itemId < 0 || inv.GetCount(itemId) <= 0))
            {
                itemId = inv.GetFirstItemId();
            }

            if (itemId < 0)
            {
                OnDropFailed?.Invoke(_dropItemIdOnKey, _dropAmountOnKey);
                return 0;
            }

            return DropById(itemId, _dropAmountOnKey);
        }

        public void SetDropEnabled(bool enabled)
        {
            _canDrop = enabled;
        }

        public void SetDropItemId(int itemId)
        {
            _dropItemIdOnKey = itemId;
        }

        private GameObject ResolveDropPrefab(InventoryItemData itemData)
        {
            return itemData != null && itemData.WorldDropPrefab != null
                ? itemData.WorldDropPrefab
                : _fallbackDropPrefab;
        }

        private int DropAt(int index, int amount, DropSource source)
        {
            if (!_canDrop || _isDropping)
            {
                return 0;
            }

            _isDropping = true;
            try
            {
                int failedItemId = source == DropSource.ItemId ? index : -1;
                InventoryComponent inventory = ResolveInventory();
                if (_pendingReturns.Count > 0 || inventory == null || amount < 0 ||
                    (source != DropSource.Physical && amount == 0))
                {
                    OnDropFailed?.Invoke(failedItemId, amount);
                    return 0;
                }

                int recordIndex = source == DropSource.ItemId ? inventory.FindPackedIndex(index) : index;
                InventoryItemRecord record = source == DropSource.Physical
                    ? inventory.GetPhysicalSlotRecord(recordIndex)
                    : inventory.GetRecordAtSlotIndex(recordIndex);
                if (record == null)
                {
                    OnDropFailed?.Invoke(failedItemId, amount);
                    return 0;
                }

                InventoryItemData itemData = inventory.GetItemData(record.EffectiveItemId);
                GameObject prefab = ResolveDropPrefab(itemData);
                if (prefab == null)
                {
                    OnDropFailed?.Invoke(record.EffectiveItemId, amount);
                    return 0;
                }

                InventoryComponent.MutationReceipt receipt = inventory.CommitRecordTake(recordIndex, amount,
                    source == DropSource.Physical);
                if (receipt.Record == null)
                {
                    OnDropFailed?.Invoke(record.EffectiveItemId, amount);
                    return 0;
                }

                return DropRecord(inventory, itemData, prefab, receipt,
                    amount > 0 ? amount : receipt.AcceptedCount);
            }
            finally
            {
                _isDropping = false;
            }
        }

        private int DropRecord(InventoryComponent inventory, InventoryItemData itemData, GameObject prefab,
            InventoryComponent.MutationReceipt receipt, int failedAmount)
        {
            InventoryItemRecord record = receipt.Record;
            receipt.Publish();
            GameObject dropped = null;
            if (receipt.Fault == null)
            {
                try
                {
                    dropped = SpawnDroppedItem(prefab, inventory, itemData, record);
                }
                catch (Exception ex)
                {
                    NeoDiagnostics.LogError(
                        $"[InventoryDropper] Failed to spawn dropped item {record.EffectiveItemId}: {ex.Message}", this);
                }
            }

            if (dropped == null)
            {
                PendingReturn pending = new()
                {
                    Inventory = inventory,
                    Record = record.Clone(),
                    PhysicalSlotIndex = receipt.PhysicalSlotIndex
                };
                _pendingReturns.Add(pending);
                InventoryComponent.MutationReceipt returned = TryReturn(pending);
                if (returned != null)
                {
                    returned.Publish();
                    if (returned.Fault != null)
                    {
                        receipt.CaptureFault(returned.Fault.SourceException);
                    }
                }

                try
                {
                    OnDropFailed?.Invoke(record.EffectiveItemId, failedAmount);
                }
                catch (Exception exception)
                {
                    receipt.CaptureFault(exception);
                }

                receipt.ThrowIfFaulted();
                return 0;
            }

            try
            {
                OnItemDropped?.Invoke(record.EffectiveItemId, record.EffectiveCount, dropped);
            }
            catch (Exception exception)
            {
                receipt.CaptureFault(exception);
            }

            receipt.ThrowIfFaulted();
            return record.EffectiveCount;
        }

        private InventoryComponent.MutationReceipt TryReturn(PendingReturn pending)
        {
            if (pending.Inventory == null)
            {
                return null;
            }

            InventoryComponent.MutationReceipt receipt = pending.Inventory.CommitRecordReturn(
                pending.PhysicalSlotIndex, pending.Record);
            int remaining = pending.Record.EffectiveCount - receipt.AcceptedCount;
            if (remaining <= 0)
            {
                _pendingReturns.Remove(pending);
            }
            else
            {
                pending.Record.Count = remaining;
                if (pending.Record.IsInstance)
                {
                    pending.Record.Instance.Count = remaining;
                }
            }

            return receipt;
        }

        private GameObject SpawnDroppedItem(GameObject prefab, InventoryComponent inventory, InventoryItemData itemData,
            InventoryItemRecord record)
        {
            Vector3 spawnPosition = GetSpawnPosition();
            GameObject dropped = Instantiate(prefab, spawnPosition, Quaternion.identity);
            if (dropped == null)
            {
                return null;
            }

            try
            {
                EnsureColliders(dropped);
                ApplyPhysicsImpulse(dropped);

                if (_configurePickableItem)
                {
                    ConfigurePickable(dropped, inventory, itemData, record);
                }

                return dropped;
            }
            catch
            {
                dropped.SafeDestroy();
                throw;
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

        private Vector3 GetSpawnPosition()
        {
            Transform source = _dropPoint != null ? _dropPoint : transform;
            Vector3 basePosition = source.position;
            if (_randomRadius <= 0f)
            {
                return basePosition;
            }

            Vector2 offset = Random.insideUnitCircle * _randomRadius;
            return basePosition + new Vector3(offset.x, 0f, offset.y);
        }

        private void EnsureColliders(GameObject dropped)
        {
            if (!_addColliderIfMissing)
            {
                return;
            }

            if (_addRigidbody2D)
            {
                Collider2D col2D = dropped.GetComponent<Collider2D>();
                if (col2D == null)
                {
                    CircleCollider2D created = dropped.AddComponent<CircleCollider2D>();
                    created.radius = _defaultColliderRadius;
                    created.isTrigger = _useTriggerCollider;
                }
            }
            else
            {
                Collider col = dropped.GetComponent<Collider>();
                if (col == null)
                {
                    SphereCollider created = dropped.AddComponent<SphereCollider>();
                    created.radius = _defaultColliderRadius;
                    created.isTrigger = _useTriggerCollider;
                }
            }
        }

        private void ApplyPhysicsImpulse(GameObject dropped)
        {
            Vector3 direction = transform.TransformDirection(_throwDirection.normalized);

            if (_addRigidbody2D)
            {
                Rigidbody2D rb2D = dropped.GetComponent<Rigidbody2D>();
                if (rb2D == null)
                {
                    rb2D = dropped.AddComponent<Rigidbody2D>();
                }

                if (_throwImpulse > 0f)
                {
                    Vector2 force = new(direction.x, direction.z);
                    if (force.sqrMagnitude > 0f)
                    {
                        rb2D.AddForce(force * _throwImpulse, ForceMode2D.Impulse);
                    }
                }
            }

            if (_addRigidbody3D)
            {
                Rigidbody rb = dropped.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = dropped.AddComponent<Rigidbody>();
                }

                if (_throwImpulse > 0f && direction.sqrMagnitude > 0f)
                {
                    rb.AddForce(direction * _throwImpulse, ForceMode.Impulse);
                }
            }
        }

        private void ConfigurePickable(GameObject dropped, InventoryComponent inventory, InventoryItemData itemData,
            InventoryItemRecord record)
        {
            PickableItem pickable = dropped.GetComponent<PickableItem>();
            if (pickable == null)
            {
                pickable = dropped.AddComponent<PickableItem>();
            }

            if (record.IsInstance && record.Instance != null)
            {
                pickable.ConfigureInstance(itemData, record.Instance,
                    _setTargetInventoryForPickable ? inventory : pickable.TargetInventory);
            }
            else
            {
                pickable.Configure(itemData, record.EffectiveItemId, record.EffectiveCount,
                    _setTargetInventoryForPickable ? inventory : pickable.TargetInventory);
            }
        }
    }
}
