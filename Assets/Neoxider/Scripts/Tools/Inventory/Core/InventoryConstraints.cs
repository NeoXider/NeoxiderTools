using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Per-item max stack and global capacity limits applied by inventory backends.
    /// </summary>
    [Serializable]
    public sealed class InventoryConstraints
    {
        private readonly Dictionary<int, int> _maxStackByItemId = new();

        /// <summary>Maximum distinct item ids allowed; 0 = unlimited.</summary>
        public int MaxUniqueItems { get; set; }

        /// <summary>Maximum total item count; 0 = unlimited.</summary>
        public int MaxTotalItems { get; set; }

        /// <summary>Clears per-id max stack map.</summary>
        public void ClearItemMaxStacks()
        {
            _maxStackByItemId.Clear();
        }

        /// <summary>Sets max stack for <paramref name="itemId" />; non-positive removes the override (unlimited).</summary>
        public void SetItemMaxStack(int itemId, int maxStack)
        {
            if (maxStack <= 0)
            {
                _maxStackByItemId.Remove(itemId);
                return;
            }

            _maxStackByItemId[itemId] = maxStack;
        }

        /// <summary>Configured max stack for <paramref name="itemId" />; 0 means no per-item cap.</summary>
        public int GetMaxStack(int itemId)
        {
            return _maxStackByItemId.TryGetValue(itemId, out int maxStack) ? maxStack : 0;
        }

        /// <summary>Deep copy of limits and per-item stacks.</summary>
        public InventoryConstraints Clone()
        {
            InventoryConstraints copy = new()
            {
                MaxUniqueItems = MaxUniqueItems,
                MaxTotalItems = MaxTotalItems
            };

            foreach (KeyValuePair<int, int> pair in _maxStackByItemId)
            {
                copy._maxStackByItemId[pair.Key] = pair.Value;
            }

            return copy;
        }
    }
}
