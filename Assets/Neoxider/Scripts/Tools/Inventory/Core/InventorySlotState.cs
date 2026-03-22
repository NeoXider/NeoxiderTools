using System;

namespace Neo.Tools
{
    /// <summary>
    ///     Serializable content of one physical slot in <see cref="InventoryStorageMode.SlotGrid" />.
    /// </summary>
    [Serializable]
    public sealed class InventorySlotState
    {
        /// <summary>Stack item id when <see cref="Instance" /> is null; -1 when empty.</summary>
        public int ItemId = -1;

        /// <summary>Stack count for simple items.</summary>
        public int Count;

        /// <summary>When set, slot holds a unique item with payload instead of a plain stack.</summary>
        public InventoryItemInstance Instance;

        /// <summary>True when the slot has no items.</summary>
        public bool IsEmpty => Instance == null && (ItemId < 0 || Count <= 0);

        /// <summary>True when <see cref="Instance" /> drives the slot.</summary>
        public bool IsInstance => Instance != null;

        /// <summary>Resolved item id.</summary>
        public int EffectiveItemId => Instance != null ? Instance.ItemId : ItemId;

        /// <summary>Resolved item count.</summary>
        public int EffectiveCount => Instance != null ? Math.Max(1, Instance.Count) : Count;

        /// <summary>Creates an empty slot.</summary>
        public static InventorySlotState Empty()
        {
            return new InventorySlotState();
        }

        /// <summary>Deep copy.</summary>
        public InventorySlotState Clone()
        {
            return new InventorySlotState
            {
                ItemId = ItemId,
                Count = Count,
                Instance = Instance != null ? Instance.Clone() : null
            };
        }

        /// <summary>Converts to a record for transfer APIs.</summary>
        public InventoryItemRecord ToRecord()
        {
            return IsInstance ? new InventoryItemRecord(Instance) : new InventoryItemRecord(ItemId, Count);
        }

        /// <summary>Clears the slot to empty.</summary>
        public void Clear()
        {
            ItemId = -1;
            Count = 0;
            Instance = null;
        }
    }
}
