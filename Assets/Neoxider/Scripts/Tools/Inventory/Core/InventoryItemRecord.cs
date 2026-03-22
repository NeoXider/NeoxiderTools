using System;

namespace Neo.Tools
{
    /// <summary>
    ///     Serializable stack or instance row used in snapshots and transfer operations.
    /// </summary>
    [Serializable]
    public sealed class InventoryItemRecord
    {
        /// <summary>Item id when <see cref="Instance" /> is null.</summary>
        public int ItemId;

        /// <summary>Stack count when not instance-based.</summary>
        public int Count;

        /// <summary>Optional per-item state; when set, <see cref="ItemId" />/<see cref="Count" /> mirror the instance.</summary>
        public InventoryItemInstance Instance;

        /// <summary>True when <see cref="Instance" /> is assigned.</summary>
        public bool IsInstance => Instance != null;

        /// <summary>Resolved item id for UI and rules.</summary>
        public int EffectiveItemId => Instance != null ? Instance.ItemId : ItemId;

        /// <summary>Resolved count (instances always at least 1).</summary>
        public int EffectiveCount => Instance != null ? Math.Max(1, Instance.Count) : Count;

        public InventoryItemRecord()
        {
        }

        /// <summary>Creates a simple stack record.</summary>
        public InventoryItemRecord(int itemId, int count)
        {
            ItemId = itemId;
            Count = Math.Max(0, count);
        }

        /// <summary>Wraps a clone of <paramref name="instance" />.</summary>
        public InventoryItemRecord(InventoryItemInstance instance)
        {
            Instance = instance != null ? instance.Clone() : null;
            if (Instance != null)
            {
                ItemId = Instance.ItemId;
                Count = Math.Max(1, Instance.Count);
            }
        }

        /// <summary>Deep copy of this record.</summary>
        public InventoryItemRecord Clone()
        {
            return IsInstance ? new InventoryItemRecord(Instance) : new InventoryItemRecord(ItemId, Count);
        }

        /// <summary>Legacy id/count pair for UI lists (loses instance detail).</summary>
        public InventoryEntry ToEntry()
        {
            return new InventoryEntry(EffectiveItemId, EffectiveCount);
        }
    }
}
