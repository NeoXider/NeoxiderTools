using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Pure C# inventory: item counts and Add/Remove/Has/GetCount.
    /// </summary>
    [Serializable]
    public sealed class InventoryManager
    {
        private readonly Dictionary<int, int> _counts = new();
        private readonly Dictionary<int, int> _maxStackByItemId = new();
        private readonly List<int> _order = new();

        /// <summary>
        ///     Max distinct itemIds. 0 = unlimited.
        /// </summary>
        public int MaxUniqueItems { get; set; }

        /// <summary>
        ///     Max total item count across all stacks. 0 = unlimited.
        /// </summary>
        public int MaxTotalItems { get; set; }

        /// <summary>Total stacked count of all items.</summary>
        public int TotalCount { get; private set; }

        /// <summary>Number of unique itemIds.</summary>
        public int UniqueCount => _counts.Count;

        /// <summary>
        ///     True if at least <paramref name="amount" /> of itemId is present.
        /// </summary>
        public bool Has(int itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                return true;
            }

            return _counts.TryGetValue(itemId, out int current) && current >= amount;
        }

        /// <summary>Current count for itemId.</summary>
        public int GetCount(int itemId)
        {
            return _counts.TryGetValue(itemId, out int current) ? current : 0;
        }

        /// <summary>
        ///     Adds items; returns amount actually added (may be less due to limits).
        /// </summary>
        public int Add(int itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int current = GetCount(itemId);
            int addable = CalculateAddableAmount(itemId, amount, current);
            if (addable <= 0)
            {
                return 0;
            }

            if (current <= 0)
            {
                _order.Add(itemId);
            }

            int next = current + addable;
            _counts[itemId] = next;
            TotalCount += addable;
            return addable;
        }

        /// <summary>
        ///     Removes items; returns amount actually removed.
        /// </summary>
        public int Remove(int itemId, int amount = 1)
        {
            if (amount <= 0 || !_counts.TryGetValue(itemId, out int current) || current <= 0)
            {
                return 0;
            }

            int removed = Math.Min(amount, current);
            int next = current - removed;

            if (next <= 0)
            {
                _counts.Remove(itemId);
                _order.Remove(itemId);
            }
            else
            {
                _counts[itemId] = next;
            }

            TotalCount -= removed;
            if (TotalCount < 0)
            {
                TotalCount = 0;
            }

            return removed;
        }

        /// <summary>Clears the inventory.</summary>
        public void Clear()
        {
            _counts.Clear();
            _order.Clear();
            TotalCount = 0;
        }

        /// <summary>Snapshot as entry list (order = add order).</summary>
        public List<InventoryEntry> CreateSnapshot()
        {
            List<InventoryEntry> entries = new(_order.Count);
            for (int i = 0; i < _order.Count; i++)
            {
                int id = _order[i];
                if (_counts.TryGetValue(id, out int count) && count > 0)
                {
                    entries.Add(new InventoryEntry(id, count));
                }
            }

            return entries;
        }

        /// <summary>
        ///     Replaces inventory from saved entries.
        /// </summary>
        public void ReplaceFrom(IEnumerable<InventoryEntry> entries)
        {
            Clear();
            if (entries == null)
            {
                return;
            }

            foreach (InventoryEntry entry in entries)
            {
                if (entry == null || entry.Count <= 0)
                {
                    continue;
                }

                int added = Add(entry.ItemId, entry.Count);
                if (added <= 0)
                {
                }
            }
        }

        /// <summary>
        ///     Sets per-item stack cap. 0 or less removes limit for that itemId.
        /// </summary>
        public void SetItemMaxStack(int itemId, int maxStack)
        {
            if (maxStack <= 0)
            {
                _maxStackByItemId.Remove(itemId);
                return;
            }

            _maxStackByItemId[itemId] = maxStack;
        }

        /// <summary>Clears all per-item stack limits.</summary>
        public void ClearItemMaxStacks()
        {
            _maxStackByItemId.Clear();
        }

        private int CalculateAddableAmount(int itemId, int requested, int current)
        {
            int addable = requested;

            if (current <= 0 && MaxUniqueItems > 0 && _counts.Count >= MaxUniqueItems)
            {
                return 0;
            }

            int maxStack = GetMaxStack(itemId);
            if (maxStack > 0)
            {
                int stackRoom = maxStack - current;
                if (stackRoom <= 0)
                {
                    return 0;
                }

                addable = Math.Min(addable, stackRoom);
            }

            if (MaxTotalItems > 0)
            {
                int totalRoom = MaxTotalItems - TotalCount;
                if (totalRoom <= 0)
                {
                    return 0;
                }

                addable = Math.Min(addable, totalRoom);
            }

            return addable;
        }

        private int GetMaxStack(int itemId)
        {
            return _maxStackByItemId.TryGetValue(itemId, out int maxStack) ? maxStack : 0;
        }
    }
}
