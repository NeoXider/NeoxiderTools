using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     JSON-serializable snapshot of one <see cref="InventoryComponent" /> (entries, instances, and optional slot grid).
    /// </summary>
    [Serializable]
    public sealed class InventorySaveData
    {
        /// <summary>Format version for migration in <see cref="InventoryComponent" />.</summary>
        public int Version = 1;

        /// <summary>Stored <see cref="InventoryStorageMode" /> as int.</summary>
        public int StorageMode;

        /// <summary>Slot count when grid data is present.</summary>
        public int SlotCapacity;

        /// <summary>Aggregated stack entries (non-instance items).</summary>
        public List<InventoryEntry> Entries = new();

        /// <summary>Instance-based items with per-item state payload.</summary>
        public List<InventoryItemInstance> Instances = new();

        /// <summary>Per-slot state when using slot grid persistence.</summary>
        public List<InventorySlotState> Slots = new();
    }
}
