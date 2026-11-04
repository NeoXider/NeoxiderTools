using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Clone-free reads implemented by the built-in storages for the paths the facade runs on every mutation.
    /// </summary>
    // WHY: The public snapshot APIs deep-clone every instance payload. Count deltas, the debug mirror and UI lists
    // only need ids and counts, so cloning the whole inventory twice per add/remove was the dominant mutation cost.
    // Custom IInventoryStorage implementations keep working through the snapshot fallback.
    internal interface IInventoryRecordReader
    {
        /// <summary>Number of non-empty records in packed order.</summary>
        int PackedRecordCount { get; }

        /// <summary>Adds each non-empty record's effective count to <paramref name="counts" />, in packed order.</summary>
        void AccumulateCounts(Dictionary<int, int> counts);

        /// <summary>Appends one id/count entry per non-empty record, in packed order.</summary>
        void AppendEntries(List<InventoryEntry> entries);

        /// <summary>Effective item id of the non-empty record at <paramref name="packedIndex" />, or -1.</summary>
        int GetPackedItemId(int packedIndex);

        /// <summary>Packed index of the first non-empty record with <paramref name="itemId" />, or -1.</summary>
        int FindPackedIndex(int itemId);
    }
}
