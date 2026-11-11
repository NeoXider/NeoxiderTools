namespace Neo.Tools
{
    /// <summary>
    ///     Cross-container slot operations for two <see cref="InventoryComponent" /> instances in <see cref="InventoryStorageMode.SlotGrid" /> mode.
    /// </summary>
    public static class InventoryTransferService
    {
        /// <summary>
        ///     Transfers the captured physical slot only while its exact item is still present.
        ///     Packed selections are rejected; capture a physical slot for grid transfers.
        /// </summary>
        public static int TransferSelection(InventoryComponent source, InventorySelection selection,
            InventoryComponent target, int targetSlotIndex, int amount = 0)
        {
            if (source == null || selection == null || !selection.IsPhysicalIndex ||
                !source.IsSelectionCurrent(selection))
            {
                return 0;
            }

            return Transfer(source, selection.Index, target, targetSlotIndex, amount);
        }

        /// <summary>
        ///     Moves, merges, or swaps between physical slots on different inventories (or moves within the same grid).
        /// </summary>
        /// <param name="sourceInventory">Container owning <paramref name="sourceSlotIndex" />.</param>
        /// <param name="sourceSlotIndex">Physical slot index on source.</param>
        /// <param name="targetInventory">Container owning <paramref name="targetSlotIndex" />.</param>
        /// <param name="targetSlotIndex">Physical slot index on target.</param>
        /// <param name="amount">0 = default (full move / merge as rules allow); positive = partial stack move when applicable.</param>
        /// <returns>Amount actually moved; 0 if inputs invalid or nothing changed.</returns>
        public static int Transfer(InventoryComponent sourceInventory, int sourceSlotIndex,
            InventoryComponent targetInventory, int targetSlotIndex, int amount = 0)
        {
            if (sourceInventory == null || targetInventory == null)
            {
                return 0;
            }

            if (!sourceInventory.IsSlotInventory || !targetInventory.IsSlotInventory)
            {
                return 0;
            }

            if (ReferenceEquals(sourceInventory, targetInventory))
            {
                return sourceInventory.MoveSlot(sourceSlotIndex, targetSlotIndex, amount);
            }

            InventorySlotState source = sourceInventory.GetSlot(sourceSlotIndex);
            InventorySlotState target = targetInventory.GetSlot(targetSlotIndex);
            if (source == null || source.IsEmpty)
            {
                return 0;
            }

            int moved = InventorySlotTransferRules.TryMoveOrSwap(ref source, ref target,
                targetInventory.GetMaxStack(source.EffectiveItemId), amount);
            if (moved <= 0)
            {
                return 0;
            }

            return sourceInventory.TryApplySlotTransfer(sourceSlotIndex, source, targetInventory, targetSlotIndex,
                target) ? moved : 0;
        }
    }
}
