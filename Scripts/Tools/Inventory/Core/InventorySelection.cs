using System;

namespace Neo.Tools
{
    /// <summary>
    ///     Captures one stack or unique instance in one storage. A selection never follows a
    ///     different item into the captured slot. Use on the same thread as the storage.
    /// </summary>
    public sealed class InventorySelection
    {
        private readonly IInventoryStorage _owner;
        private readonly InventoryItemRecord _record;

        private InventorySelection(IInventoryStorage owner, int index, bool physicalIndex,
            InventoryItemRecord record)
        {
            _owner = owner;
            Index = index;
            IsPhysicalIndex = physicalIndex;
            _record = record.Clone();
        }

        /// <summary>Captured physical slot or packed record index.</summary>
        public int Index { get; }

        /// <summary>Whether <see cref="Index" /> includes empty physical slots.</summary>
        public bool IsPhysicalIndex { get; }

        /// <summary>Item type, shared by all instances of this item.</summary>
        public int ItemId => _record.EffectiveItemId;

        /// <summary>Unique instance id, or null for an ordinary stack.</summary>
        public string InstanceId => _record.Instance?.InstanceId;

        /// <summary>A detached snapshot; changing it cannot modify the storage or selection.</summary>
        public InventoryItemRecord Record => _record.Clone();

        /// <summary>Captures a nonempty record. Physical indices require an ISlottedInventory backend.</summary>
        public static bool TryCapture(IInventoryStorage storage, int index, bool physicalIndex,
            out InventorySelection selection)
        {
            selection = null;
            if (!TryRead(storage, index, physicalIndex, out InventoryItemRecord record))
            {
                return false;
            }

            selection = new InventorySelection(storage, index, physicalIndex, record);
            return true;
        }

        /// <summary>
        ///     Validates storage ownership and item identity. Count and payload may change while
        ///     the same instance remains in place. Plain stacks have slot/type identity only.
        /// </summary>
        public bool IsCurrent(IInventoryStorage storage)
        {
            return TryGetCurrent(storage, out InventoryItemRecord unused);
        }

        /// <summary>Reads the current count and state only if the captured identity is still present.</summary>
        public bool TryGetCurrent(IInventoryStorage storage, out InventoryItemRecord record)
        {
            record = null;
            if (!ReferenceEquals(storage, _owner) ||
                !TryRead(storage, Index, IsPhysicalIndex, out InventoryItemRecord current) ||
                current.EffectiveItemId != ItemId || current.IsInstance != _record.IsInstance ||
                (current.IsInstance && !string.Equals(current.Instance.InstanceId, InstanceId,
                    StringComparison.Ordinal)))
            {
                return false;
            }

            record = current;
            return true;
        }

        /// <summary>
        ///     Takes exactly the requested quantity from a plain C# backend, or changes nothing.
        ///     For scene inventories use InventoryComponent.TryTakeSelection to publish events/save.
        /// </summary>
        public bool TryTake(IInventoryStorage storage, int amount, out InventoryItemRecord record)
        {
            record = null;
            if (amount <= 0 || !TryGetCurrent(storage, out InventoryItemRecord current) ||
                current.EffectiveCount < amount)
            {
                return false;
            }

            return IsPhysicalIndex
                ? ((ISlottedInventory)storage).TryTakeSlot(Index, amount, out record)
                : storage.TryTakeRecordAtPackedIndex(Index, amount, out record);
        }

        private static bool TryRead(IInventoryStorage storage, int index, bool physicalIndex,
            out InventoryItemRecord record)
        {
            record = null;
            if (storage == null || index < 0)
            {
                return false;
            }

            if (physicalIndex)
            {
                if (storage is not ISlottedInventory slotted || index >= slotted.SlotCapacity)
                {
                    return false;
                }

                InventorySlotState slot = slotted.GetSlot(index);
                if (slot == null || slot.IsEmpty)
                {
                    return false;
                }

                record = slot.ToRecord();
            }
            else if (!storage.TryGetRecordAtPackedIndex(index, out record))
            {
                return false;
            }

            if (record == null || record.EffectiveItemId < 0 || record.EffectiveCount <= 0)
            {
                record = null;
                return false;
            }

            record = record.Clone();
            return true;
        }
    }
}
