using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Fixed-size slot grid storage: each index maps to one <see cref="InventorySlotState" />.
    /// </summary>
    public interface ISlottedInventory
    {
        /// <summary>Number of physical slots.</summary>
        int SlotCapacity { get; }

        /// <summary>State at <paramref name="slotIndex" /> (may be empty).</summary>
        InventorySlotState GetSlot(int slotIndex);

        /// <summary>Writes slot state without validation beyond backend rules.</summary>
        void SetSlot(int slotIndex, InventorySlotState state);

        /// <summary>Swaps two slots.</summary>
        bool SwapSlots(int sourceIndex, int targetIndex);

        /// <summary>Merges or moves stack from source to target; <paramref name="amount" /> 0 = default move amount.</summary>
        /// <returns>Units moved.</returns>
        int MoveSlot(int sourceIndex, int targetIndex, int amount = 0);

        /// <summary>Removes from slot into <paramref name="record" />.</summary>
        bool TryTakeSlot(int slotIndex, int amount, out InventoryItemRecord record);

        /// <summary>Inserts or merges into slot.</summary>
        /// <returns>Amount inserted.</returns>
        int TryInsertIntoSlot(int slotIndex, InventoryItemRecord record, int amount = 0);

        /// <summary>Full slot array snapshot for save/load.</summary>
        List<InventorySlotState> CreateSlotSnapshot();
    }
}
