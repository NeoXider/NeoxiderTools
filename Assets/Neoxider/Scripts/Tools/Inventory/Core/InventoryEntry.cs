using System;

namespace Neo.Tools
{
    /// <summary>
    ///     Serializable item id and stack count for initial state and legacy snapshots.
    /// </summary>
    [Serializable]
    public sealed class InventoryEntry
    {
        /// <summary>Item type id.</summary>
        public int ItemId;

        /// <summary>Stack size.</summary>
        public int Count;

        public InventoryEntry()
        {
        }

        /// <summary>Creates an entry with non-negative <paramref name="count" />.</summary>
        public InventoryEntry(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}
