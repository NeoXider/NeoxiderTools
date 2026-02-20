using System;

namespace Neo.Tools
{
    /// <summary>
    ///     Пара itemId + count для хранения состояния инвентаря.
    /// </summary>
    [Serializable]
    public sealed class InventoryEntry
    {
        public int ItemId;
        public int Count;

        public InventoryEntry()
        {
        }

        public InventoryEntry(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}
