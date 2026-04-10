using System.Collections.Generic;

namespace Neo.Tools
{
    public sealed partial class InventoryComponent
    {
        /// <summary>True if at least one of <paramref name="itemId" /> is stored.</summary>
        public bool HasItem(int itemId)
        {
            EnsureRuntimeInitialized();
            return _storage.Has(itemId);
        }

        /// <summary>True if stored count for <paramref name="itemId" /> is at least <paramref name="amount" />.</summary>
        public bool HasItemAmount(int itemId, int amount)
        {
            EnsureRuntimeInitialized();
            return _storage.Has(itemId, amount);
        }

        /// <summary>Total stored count for <paramref name="itemId" /> (aggregated across stacks/slots).</summary>
        public int GetCount(int itemId)
        {
            EnsureRuntimeInitialized();
            return _storage.GetCount(itemId);
        }

        /// <summary>Effective max stack for <paramref name="itemId" /> from constraints (0 means unlimited).</summary>
        public int GetMaxStack(int itemId)
        {
            EnsureRuntimeInitialized();
            return _constraints.GetMaxStack(itemId);
        }

        /// <summary>True when database entry for <paramref name="itemId" /> has <see cref="InventoryItemData.SupportsInstanceState" />.</summary>
        public bool SupportsInstanceState(int itemId)
        {
            InventoryItemData data = GetItemData(itemId);
            return data != null && data.SupportsInstanceState;
        }

        /// <summary>Packed list of records in storage order (slot index order in grid mode).</summary>
        public List<InventoryItemRecord> GetSnapshotRecords()
        {
            EnsureRuntimeInitialized();
            return _storage.CreateRecordSnapshot();
        }

        /// <summary>All instance-based items currently stored (clone per entry).</summary>
        public List<InventoryItemInstance> GetSnapshotInstances()
        {
            EnsureRuntimeInitialized();
            return _storage.CreateInstanceSnapshot();
        }

        /// <summary>Legacy-friendly list of id/count pairs derived from <see cref="GetSnapshotRecords" />.</summary>
        public List<InventoryEntry> GetSnapshotEntries()
        {
            List<InventoryItemRecord> records = GetSnapshotRecords();
            List<InventoryEntry> entries = new(records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                InventoryItemRecord record = records[i];
                if (record != null && record.EffectiveCount > 0)
                {
                    entries.Add(record.ToEntry());
                }
            }

            return entries;
        }

        /// <summary>Number of non-empty records in the packed snapshot (not always equal to physical slot count).</summary>
        public int GetNonEmptySlotCount()
        {
            return GetSnapshotRecords().Count;
        }

        /// <summary>Gets the record at packed index (iteration order of <see cref="GetSnapshotRecords" />).</summary>
        public bool TryGetRecordAtPackedIndex(int packedIndex, out InventoryItemRecord record)
        {
            EnsureRuntimeInitialized();
            return _storage.TryGetRecordAtPackedIndex(packedIndex, out record);
        }

        /// <summary>Item id at packed index, or -1 if out of range or empty.</summary>
        public int GetItemIdAtSlotIndex(int slotIndex)
        {
            return TryGetRecordAtPackedIndex(slotIndex, out InventoryItemRecord record) ? record.EffectiveItemId : -1;
        }

        /// <summary>Record at packed index, or null if invalid/empty.</summary>
        public InventoryItemRecord GetRecordAtSlotIndex(int slotIndex)
        {
            return TryGetRecordAtPackedIndex(slotIndex, out InventoryItemRecord record) ? record : null;
        }

        /// <summary>Clone of instance payload at packed index if the record is instance-based; otherwise null.</summary>
        public InventoryItemInstance GetInstanceAtSlotIndex(int slotIndex)
        {
            return TryGetRecordAtPackedIndex(slotIndex, out InventoryItemRecord record) && record.IsInstance
                ? record.Instance.Clone()
                : null;
        }

        /// <summary>Removes up to <paramref name="amount" /> from the packed index and returns the taken record.</summary>
        public bool TryTakeRecordAtPackedIndex(int packedIndex, int amount, out InventoryItemRecord record)
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            bool result = _storage.TryTakeRecordAtPackedIndex(packedIndex, amount, out record);
            FinalizeMutation(before);
            return result;
        }

        /// <summary>Finds the first packed record matching <paramref name="itemId" /> and takes up to <paramref name="amount" />.</summary>
        public bool TryTakeFirstRecordByItemId(int itemId, int amount, out InventoryItemRecord record)
        {
            record = null;
            List<InventoryItemRecord> records = GetSnapshotRecords();
            for (int i = 0; i < records.Count; i++)
            {
                InventoryItemRecord current = records[i];
                if (current != null && current.EffectiveItemId == itemId)
                {
                    return TryTakeRecordAtPackedIndex(i, amount, out record);
                }
            }

            return false;
        }

        /// <summary>First item id with positive count in packed order, or -1 if empty.</summary>
        public int GetFirstItemId()
        {
            List<InventoryItemRecord> records = GetSnapshotRecords();
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].EffectiveCount > 0)
                {
                    return records[i].EffectiveItemId;
                }
            }

            return -1;
        }

        /// <summary>Last item id with positive count in packed order, or -1 if empty.</summary>
        public int GetLastItemId()
        {
            List<InventoryItemRecord> records = GetSnapshotRecords();
            for (int i = records.Count - 1; i >= 0; i--)
            {
                if (records[i].EffectiveCount > 0)
                {
                    return records[i].EffectiveItemId;
                }
            }

            return -1;
        }

        /// <summary>Looks up <see cref="InventoryItemData" /> from <see cref="Database" />.</summary>
        public InventoryItemData GetItemData(int itemId)
        {
            return _database != null ? _database.GetItemData(itemId) : null;
        }
    }
}
