using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     One inventory item with optional serialized component state (<see cref="ComponentStates" />).
    /// </summary>
    [Serializable]
    public sealed class InventoryItemInstance
    {
        /// <summary>Stable id for debugging and save round-trip; generated if empty.</summary>
        public string InstanceId;

        /// <summary>Logical item type id.</summary>
        public int ItemId;

        /// <summary>Stack size for instance rows (usually 1 for unique items).</summary>
        public int Count = 1;

        /// <summary>Optional schema version for custom migration.</summary>
        public int StateVersion;

        /// <summary>Captured JSON blobs keyed by <see cref="IInventoryItemState.InventoryStateKey" />.</summary>
        public List<InventoryItemComponentState> ComponentStates = new();

        public InventoryItemInstance()
        {
        }

        /// <summary>Creates a new instance with generated <see cref="InstanceId" />.</summary>
        public InventoryItemInstance(int itemId, int count = 1)
        {
            InstanceId = Guid.NewGuid().ToString("N");
            ItemId = itemId;
            Count = Math.Max(1, count);
        }

        /// <summary>Deep copy including component state list.</summary>
        public InventoryItemInstance Clone()
        {
            InventoryItemInstance clone = new()
            {
                InstanceId = string.IsNullOrWhiteSpace(InstanceId) ? Guid.NewGuid().ToString("N") : InstanceId,
                ItemId = ItemId,
                Count = Math.Max(1, Count),
                StateVersion = StateVersion,
                ComponentStates = new List<InventoryItemComponentState>()
            };

            if (ComponentStates == null)
            {
                return clone;
            }

            for (int i = 0; i < ComponentStates.Count; i++)
            {
                InventoryItemComponentState state = ComponentStates[i];
                if (state != null)
                {
                    clone.ComponentStates.Add(state.Clone());
                }
            }

            return clone;
        }
    }
}
