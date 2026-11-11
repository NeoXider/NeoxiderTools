namespace Neo.Tools
{
    public sealed partial class InventoryComponent
    {
        /// <summary>Captures an exact item; aggregated inventories always use packed indices.</summary>
        public bool TryCaptureSelection(int index, out InventorySelection selection, bool physicalIndex = true)
        {
            EnsureRuntimeInitialized();
            return InventorySelection.TryCapture(_storage, index, physicalIndex && IsSlotInventory, out selection);
        }

        /// <summary>Whether this inventory still contains the captured item at its original index.</summary>
        public bool IsSelectionCurrent(InventorySelection selection)
        {
            return TryGetSelectionRecord(selection, out InventoryItemRecord unused);
        }

        /// <summary>Reads current count/payload for an exact selection without exposing mutable storage.</summary>
        public bool TryGetSelectionRecord(InventorySelection selection, out InventoryItemRecord record)
        {
            EnsureRuntimeInitialized();
            record = null;
            return selection != null && selection.TryGetCurrent(_storage, out record);
        }

        /// <summary>
        ///     Removes exactly amount from the captured slot/instance, preserving UID and payload.
        ///     Stale selections, foreign inventories and insufficient quantities change nothing.
        /// </summary>
        public bool TryTakeSelection(InventorySelection selection, int amount, out InventoryItemRecord record)
        {
            record = null;
            MutationReceipt receipt = CommitSelectionTake(selection, amount);
            if (receipt == null)
            {
                return false;
            }

            record = receipt.Record;
            receipt.Publish();
            receipt.ThrowIfFaulted();
            return true;
        }

        /// <summary>Consumes the selected instance/stack rather than the first item with the same type id.</summary>
        public bool TryConsumeSelection(InventorySelection selection, int amount = 1)
        {
            return TryTakeSelection(selection, amount, out InventoryItemRecord unused);
        }

        internal MutationReceipt CommitSelectionTake(InventorySelection selection, int amount)
        {
            EnsureRuntimeInitialized();
            if (selection == null || amount <= 0 ||
                !selection.TryGetCurrent(_storage, out InventoryItemRecord current) ||
                current.EffectiveCount < amount)
            {
                return null;
            }

            return CommitRecordTake(selection.Index, amount, selection.IsPhysicalIndex);
        }
    }
}
