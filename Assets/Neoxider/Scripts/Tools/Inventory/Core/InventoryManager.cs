using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Чистый C# менеджер инвентаря: хранение количества предметов и операции Add/Remove/Has/GetCount.
    /// </summary>
    [Serializable]
    public sealed class InventoryManager
    {
        private readonly Dictionary<int, int> _counts = new();
        private readonly Dictionary<int, int> _maxStackByItemId = new();
        private int _totalCount;

        /// <summary>
        ///     Максимум разных itemId в инвентаре. 0 = без лимита.
        /// </summary>
        public int MaxUniqueItems { get; set; }

        /// <summary>
        ///     Максимум общего количества всех предметов. 0 = без лимита.
        /// </summary>
        public int MaxTotalItems { get; set; }

        /// <summary>Суммарное количество всех предметов.</summary>
        public int TotalCount => _totalCount;

        /// <summary>Количество уникальных itemId в инвентаре.</summary>
        public int UniqueCount => _counts.Count;

        /// <summary>
        ///     Возвращает true, если предмет есть в количестве не меньше amount.
        /// </summary>
        public bool Has(int itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                return true;
            }

            return _counts.TryGetValue(itemId, out int current) && current >= amount;
        }

        /// <summary>Возвращает текущее количество предмета.</summary>
        public int GetCount(int itemId)
        {
            return _counts.TryGetValue(itemId, out int current) ? current : 0;
        }

        /// <summary>
        ///     Добавляет предмет и возвращает реально добавленное количество (может быть меньше из-за лимитов).
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

            int next = current + addable;
            _counts[itemId] = next;
            _totalCount += addable;
            return addable;
        }

        /// <summary>
        ///     Удаляет предмет и возвращает реально удаленное количество.
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
            }
            else
            {
                _counts[itemId] = next;
            }

            _totalCount -= removed;
            if (_totalCount < 0)
            {
                _totalCount = 0;
            }

            return removed;
        }

        /// <summary>Полностью очищает инвентарь.</summary>
        public void Clear()
        {
            _counts.Clear();
            _totalCount = 0;
        }

        /// <summary>Снимок инвентаря в виде списка записей.</summary>
        public List<InventoryEntry> CreateSnapshot()
        {
            List<InventoryEntry> entries = new(_counts.Count);
            foreach (KeyValuePair<int, int> pair in _counts)
            {
                entries.Add(new InventoryEntry(pair.Key, pair.Value));
            }

            return entries;
        }

        /// <summary>
        ///     Заполняет инвентарь из списка сохраненных записей.
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
                    continue;
                }
            }
        }

        /// <summary>
        ///     Устанавливает лимит стека для конкретного предмета. 0 или меньше = без лимита для itemId.
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

        /// <summary>Удаляет все индивидуальные лимиты стеков.</summary>
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
                int totalRoom = MaxTotalItems - _totalCount;
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
