using System;

namespace Neo.Tools
{
    /// <summary>
    ///     Layout strategy for <see cref="InventoryComponent" /> backing storage.
    /// </summary>
    [Serializable]
    public enum InventoryStorageMode
    {
        /// <summary>Stacks grouped by item id (legacy list of records).</summary>
        Aggregated = 0,

        /// <summary>Fixed slot count; each slot holds its own stack or instance.</summary>
        SlotGrid = 1
    }
}
