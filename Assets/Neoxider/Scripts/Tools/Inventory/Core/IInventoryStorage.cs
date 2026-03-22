using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Plain C# inventory storage backend (aggregated or slotted) without Unity lifecycle.
    /// </summary>
    public interface IInventoryStorage
    {
        /// <summary>Sum of all stored item counts.</summary>
        int TotalCount { get; }

        /// <summary>Number of distinct item ids with count greater than zero.</summary>
        int UniqueCount { get; }

        /// <summary>Applies stack and capacity limits used by add/remove operations.</summary>
        void SetConstraints(InventoryConstraints constraints);

        /// <summary>Adds stackable count for <paramref name="itemId" />.</summary>
        /// <returns>Amount actually added.</returns>
        int Add(int itemId, int amount = 1);

        /// <summary>Adds a non-merged instance entry.</summary>
        /// <returns>1 if added, 0 if rejected.</returns>
        int AddInstance(InventoryItemInstance instance);

        /// <summary>Removes up to <paramref name="amount" /> of <paramref name="itemId" />.</summary>
        /// <returns>Amount removed.</returns>
        int Remove(int itemId, int amount = 1);

        /// <summary>True if stored count is at least <paramref name="amount" />.</summary>
        bool Has(int itemId, int amount = 1);

        /// <summary>Total count for <paramref name="itemId" />.</summary>
        int GetCount(int itemId);

        /// <summary>Ordered list of records (slot order for grid backends).</summary>
        List<InventoryItemRecord> CreateRecordSnapshot();

        /// <summary>Clone of each stored instance-based item.</summary>
        List<InventoryItemInstance> CreateInstanceSnapshot();

        /// <summary>Reads record at packed iteration index without removing.</summary>
        bool TryGetRecordAtPackedIndex(int packedIndex, out InventoryItemRecord record);

        /// <summary>Removes up to <paramref name="amount" /> from the packed index.</summary>
        bool TryTakeRecordAtPackedIndex(int packedIndex, int amount, out InventoryItemRecord record);

        /// <summary>Removes the first matching instance-based item for <paramref name="itemId" />.</summary>
        bool TryRemoveFirstInstance(int itemId, out InventoryItemInstance instance);

        /// <summary>Clears all stored items.</summary>
        void Clear();
    }
}
