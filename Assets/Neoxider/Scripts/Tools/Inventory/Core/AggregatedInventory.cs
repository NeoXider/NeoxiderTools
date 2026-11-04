using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Pure C# storage: merges simple stacks by item id; keeps instance-based records separate.
    /// </summary>
    [Serializable]
    public sealed class AggregatedInventory : IInventoryStorage, IInventoryRecordReader
    {
        private readonly List<InventoryItemRecord> _records = new();
        private InventoryConstraints _constraints = new();

        /// <inheritdoc />
        public int TotalCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _records.Count; i++)
                {
                    total += _records[i].EffectiveCount;
                }

                return total;
            }
        }

        /// <inheritdoc />
        public int UniqueCount
        {
            get
            {
                // WHY: A local set keeps this getter free of shared state, so concurrent readers stay safe; the Add
                // path only reads it when a unique-item cap is configured.
                HashSet<int> unique = new();
                for (int i = 0; i < _records.Count; i++)
                {
                    InventoryItemRecord record = _records[i];
                    if (record != null && record.EffectiveCount > 0)
                    {
                        unique.Add(record.EffectiveItemId);
                    }
                }

                return unique.Count;
            }
        }

        int IInventoryRecordReader.PackedRecordCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _records.Count; i++)
                {
                    if (IsPacked(_records[i]))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        void IInventoryRecordReader.AccumulateCounts(Dictionary<int, int> counts)
        {
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (!IsPacked(record))
                {
                    continue;
                }

                counts.TryGetValue(record.EffectiveItemId, out int current);
                counts[record.EffectiveItemId] = current + record.EffectiveCount;
            }
        }

        void IInventoryRecordReader.AppendEntries(List<InventoryEntry> entries)
        {
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (IsPacked(record))
                {
                    entries.Add(new InventoryEntry(record.EffectiveItemId, record.EffectiveCount));
                }
            }
        }

        int IInventoryRecordReader.GetPackedItemId(int packedIndex)
        {
            if (packedIndex < 0)
            {
                return -1;
            }

            int current = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (!IsPacked(record))
                {
                    continue;
                }

                if (current++ == packedIndex)
                {
                    return record.EffectiveItemId;
                }
            }

            return -1;
        }

        int IInventoryRecordReader.FindPackedIndex(int itemId)
        {
            int current = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (!IsPacked(record))
                {
                    continue;
                }

                if (record.EffectiveItemId == itemId)
                {
                    return current;
                }

                current++;
            }

            return -1;
        }

        /// <summary>
        ///     Appends the live (not cloned) non-empty records for read-only callers inside the assembly.
        ///     Callers must not mutate or retain them.
        /// </summary>
        internal void AppendLiveRecords(List<InventoryItemRecord> records)
        {
            for (int i = 0; i < _records.Count; i++)
            {
                if (IsPacked(_records[i]))
                {
                    records.Add(_records[i]);
                }
            }
        }

        private static bool IsPacked(InventoryItemRecord record)
        {
            return record != null && record.EffectiveCount > 0;
        }

        /// <inheritdoc />
        public void SetConstraints(InventoryConstraints constraints)
        {
            _constraints = constraints != null ? constraints.Clone() : new InventoryConstraints();
        }

        /// <inheritdoc />
        public int Add(int itemId, int amount = 1)
        {
            if (itemId < 0 || amount <= 0)
            {
                return 0;
            }

            InventoryItemRecord existing = FindSimpleRecord(itemId);
            if (existing == null && !Has(itemId) && _constraints.MaxUniqueItems > 0 &&
                UniqueCount >= _constraints.MaxUniqueItems)
            {
                return 0;
            }

            int current = existing != null ? existing.Count : 0;
            int addable = InventoryStackRules.GetAddableAmount(current, amount, _constraints.GetMaxStack(itemId),
                TotalCount,
                _constraints.MaxTotalItems);
            if (addable <= 0)
            {
                return 0;
            }

            if (existing == null)
            {
                _records.Add(new InventoryItemRecord(itemId, addable));
            }
            else
            {
                existing.Count += addable;
            }

            return addable;
        }

        /// <inheritdoc />
        public int AddInstance(InventoryItemInstance instance)
        {
            if (instance == null)
            {
                return 0;
            }

            InventoryItemInstance clone = instance.Clone();
            int count = Math.Max(1, clone.Count);
            clone.Count = count;

            if (clone.ItemId < 0)
            {
                return 0;
            }

            bool hasSameType = Has(clone.ItemId);
            if (!hasSameType && _constraints.MaxUniqueItems > 0 && UniqueCount >= _constraints.MaxUniqueItems)
            {
                return 0;
            }

            if (InventoryStackRules.GetAddableAmount(0, count, 0, TotalCount, _constraints.MaxTotalItems) < count)
            {
                return 0;
            }

            _records.Add(new InventoryItemRecord(clone));
            return count;
        }

        /// <inheritdoc />
        public int Remove(int itemId, int amount = 1)
        {
            if (itemId < 0 || amount <= 0)
            {
                return 0;
            }

            int remaining = amount;
            int removed = 0;
            for (int i = 0; i < _records.Count && remaining > 0;)
            {
                InventoryItemRecord record = _records[i];
                if (record == null || record.EffectiveItemId != itemId || record.EffectiveCount <= 0)
                {
                    i++;
                    continue;
                }

                int take = Math.Min(remaining, record.EffectiveCount);
                remaining -= take;
                removed += take;

                if (record.IsInstance)
                {
                    if (take >= record.Instance.Count)
                    {
                        _records.RemoveAt(i);
                        continue;
                    }

                    record.Instance.Count -= take;
                    record.Count = record.Instance.Count;
                }
                else
                {
                    record.Count -= take;
                    if (record.Count <= 0)
                    {
                        _records.RemoveAt(i);
                        continue;
                    }
                }

                i++;
            }

            return removed;
        }

        /// <inheritdoc />
        public bool Has(int itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                return true;
            }

            return GetCount(itemId) >= amount;
        }

        /// <inheritdoc />
        public int GetCount(int itemId)
        {
            int total = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (record != null && record.EffectiveItemId == itemId)
                {
                    total += record.EffectiveCount;
                }
            }

            return total;
        }

        /// <inheritdoc />
        public List<InventoryItemRecord> CreateRecordSnapshot()
        {
            List<InventoryItemRecord> snapshot = new(_records.Count);
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (record != null && record.EffectiveCount > 0)
                {
                    snapshot.Add(record.Clone());
                }
            }

            return snapshot;
        }

        /// <inheritdoc />
        public List<InventoryItemInstance> CreateInstanceSnapshot()
        {
            List<InventoryItemInstance> snapshot = new();
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (record != null && record.IsInstance && record.Instance != null)
                {
                    snapshot.Add(record.Instance.Clone());
                }
            }

            return snapshot;
        }

        /// <inheritdoc />
        public bool TryGetRecordAtPackedIndex(int packedIndex, out InventoryItemRecord record)
        {
            record = null;
            if (packedIndex < 0 || packedIndex >= _records.Count)
            {
                return false;
            }

            InventoryItemRecord found = _records[packedIndex];
            if (found == null || found.EffectiveCount <= 0)
            {
                return false;
            }

            record = found.Clone();
            return true;
        }

        /// <summary>
        ///     Effective item id that <see cref="TryGetRecordAtPackedIndex" /> reports for <paramref name="packedIndex" />,
        ///     or -1, without cloning the record or its instance payload.
        /// </summary>
        internal int PeekItemIdAtPackedIndex(int packedIndex)
        {
            if (packedIndex < 0 || packedIndex >= _records.Count)
            {
                return -1;
            }

            InventoryItemRecord found = _records[packedIndex];
            return found != null && found.EffectiveCount > 0 ? found.EffectiveItemId : -1;
        }

        /// <inheritdoc />
        public bool TryTakeRecordAtPackedIndex(int packedIndex, int amount, out InventoryItemRecord record)
        {
            record = null;
            if (packedIndex < 0 || packedIndex >= _records.Count)
            {
                return false;
            }

            InventoryItemRecord found = _records[packedIndex];
            if (found == null || found.EffectiveCount <= 0)
            {
                return false;
            }

            int take = amount > 0 ? Math.Min(amount, found.EffectiveCount) : found.EffectiveCount;
            if (take <= 0)
            {
                return false;
            }

            if (found.IsInstance)
            {
                InventoryItemInstance instance = found.Instance.Clone();
                instance.Count = Math.Max(1, take);
                record = new InventoryItemRecord(instance);

                if (take >= found.Instance.Count)
                {
                    _records.RemoveAt(packedIndex);
                }
                else
                {
                    found.Instance.Count -= take;
                    found.Count = found.Instance.Count;
                }

                return true;
            }

            record = new InventoryItemRecord(found.ItemId, take);
            found.Count -= take;
            if (found.Count <= 0)
            {
                _records.RemoveAt(packedIndex);
            }

            return true;
        }

        /// <inheritdoc />
        public bool TryRemoveFirstInstance(int itemId, out InventoryItemInstance instance)
        {
            instance = null;
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (record == null || !record.IsInstance || record.EffectiveItemId != itemId)
                {
                    continue;
                }

                instance = record.Instance.Clone();
                _records.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void Clear()
        {
            _records.Clear();
        }

        private InventoryItemRecord FindSimpleRecord(int itemId)
        {
            for (int i = 0; i < _records.Count; i++)
            {
                InventoryItemRecord record = _records[i];
                if (record != null && !record.IsInstance && record.ItemId == itemId)
                {
                    return record;
                }
            }

            return null;
        }
    }
}
