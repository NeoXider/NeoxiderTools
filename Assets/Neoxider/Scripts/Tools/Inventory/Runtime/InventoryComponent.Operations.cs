using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    public sealed partial class InventoryComponent
    {
        [Button]
        /// <summary>Adds one unit of <paramref name="itemId" /> respecting stack rules and limits.</summary>
        /// <returns>Amount actually added (may be less than requested).</returns>
        public int AddItemById(int itemId)
        {
            return AddItemByIdAmount(itemId, 1);
        }

        [Button]
        /// <summary>Adds stackable or instance-based items depending on <see cref="InventoryItemData.SupportsInstanceState" />.</summary>
        /// <param name="itemId">Runtime item identifier.</param>
        /// <param name="amount">Requested amount; for instance-based items each unit becomes a separate instance.</param>
        /// <returns>Amount actually added.</returns>
        public int AddItemByIdAmount(int itemId, int amount)
        {
            EnsureRuntimeInitialized();
            if (amount <= 0 || !CanUseItemId(itemId))
            {
                if (amount > 0)
                {
                    OnCapacityRejected?.Invoke(itemId, amount);
                }

                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int added = 0;
            if (IsInstanceBasedItem(itemId))
            {
                for (int i = 0; i < amount; i++)
                {
                    int instanceAdded = _storage.AddInstance(new InventoryItemInstance(itemId));
                    if (instanceAdded <= 0)
                    {
                        break;
                    }

                    added += instanceAdded;
                }
            }
            else
            {
                added = _storage.Add(itemId, amount);
            }

            FinalizeMutation(before);

            int rejected = Math.Max(0, amount - added);
            if (rejected > 0)
            {
                OnCapacityRejected?.Invoke(itemId, rejected);
            }

            return added;
        }

        /// <summary>Adds items using <paramref name="itemData" />; uses instances when <see cref="InventoryItemData.SupportsInstanceState" /> is true.</summary>
        /// <returns>Total amount successfully added.</returns>
        public int AddItemData(InventoryItemData itemData, int amount = 1)
        {
            if (itemData == null)
            {
                return 0;
            }

            if (!itemData.SupportsInstanceState)
            {
                return AddItemByIdAmount(itemData.ItemId, amount);
            }

            int added = 0;
            for (int i = 0; i < amount; i++)
            {
                added += AddItemInstance(new InventoryItemInstance(itemData.ItemId));
            }

            return added;
        }

        /// <summary>Adds a single <see cref="InventoryItemInstance" /> with optional per-instance state payload.</summary>
        /// <returns>1 if added, 0 if rejected.</returns>
        public int AddItemInstance(InventoryItemInstance instance)
        {
            EnsureRuntimeInitialized();
            if (instance == null || !CanUseItemId(instance.ItemId))
            {
                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int added = _storage.AddInstance(instance);
            FinalizeMutation(before);
            if (added <= 0)
            {
                OnCapacityRejected?.Invoke(instance.ItemId, Math.Max(1, instance.Count));
            }

            return added;
        }

        [Button]
        /// <summary>Removes one unit of <paramref name="itemId" /> from aggregated storage or the first matching stack/slot.</summary>
        /// <returns>Amount actually removed.</returns>
        public int RemoveItemById(int itemId)
        {
            return RemoveItemByIdAmount(itemId, 1);
        }

        [Button]
        /// <summary>Removes up to <paramref name="amount" /> of <paramref name="itemId" />.</summary>
        /// <returns>Amount actually removed.</returns>
        public int RemoveItemByIdAmount(int itemId, int amount)
        {
            EnsureRuntimeInitialized();
            if (amount <= 0)
            {
                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int removed = _storage.Remove(itemId, amount);
            FinalizeMutation(before);
            return removed;
        }

        /// <summary>Removes <paramref name="amount" /> only if the inventory currently has at least that many.</summary>
        /// <returns>True if the full amount was removed.</returns>
        public bool TryConsume(int itemId, int amount)
        {
            if (amount <= 0 || !HasItemAmount(itemId, amount))
            {
                return false;
            }

            return RemoveItemByIdAmount(itemId, amount) == amount;
        }

        [Button]
        /// <summary>Removes all items and raises delta events before <see cref="OnInventoryChanged" />.</summary>
        public void ClearInventory()
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            _storage.Clear();
            FinalizeMutation(before);
        }

        [Button]
        /// <summary>Delegates to <see cref="InventoryDropper" /> for the configured selection policy.</summary>
        /// <returns>Amount dropped into the world.</returns>
        public int DropSelected(int amount = 1)
        {
            return _dropper != null ? _dropper.DropSelected(amount) : 0;
        }

        [Button]
        /// <summary>Drops <paramref name="amount" /> of <paramref name="itemId" /> via <see cref="InventoryDropper" />.</summary>
        public int DropById(int itemId, int amount = 1)
        {
            return _dropper != null ? _dropper.DropById(itemId, amount) : 0;
        }

        /// <summary>Drops using <paramref name="itemData" /> id and instance rules.</summary>
        public int DropData(InventoryItemData itemData, int amount = 1)
        {
            return _dropper != null ? _dropper.DropData(itemData, amount) : 0;
        }

        [Button]
        /// <summary>Drops from the first non-empty packed record.</summary>
        public int DropFirst(int amount = 1)
        {
            return _dropper != null ? _dropper.DropFirst(amount) : 0;
        }

        [Button]
        /// <summary>Drops from the last non-empty packed record.</summary>
        public int DropLast(int amount = 1)
        {
            return _dropper != null ? _dropper.DropLast(amount) : 0;
        }

        [Button]
        /// <summary>Editor/debug: adds one of <see cref="SelectedItemId" />.</summary>
        public int TestAdd1Selected()
        {
            return AddItemById(_selectedItemId);
        }

        [Button]
        /// <summary>Editor/debug: removes one of <see cref="SelectedItemId" />.</summary>
        public int TestRemove1Selected()
        {
            return RemoveItemById(_selectedItemId);
        }

        private Dictionary<int, int> CaptureCounts()
        {
            Dictionary<int, int> counts = new();
            if (_storage == null)
            {
                return counts;
            }

            List<InventoryItemRecord> snapshot = _storage.CreateRecordSnapshot();
            for (int i = 0; i < snapshot.Count; i++)
            {
                InventoryItemRecord record = snapshot[i];
                if (record == null || record.EffectiveCount <= 0)
                {
                    continue;
                }

                counts.TryGetValue(record.EffectiveItemId, out int current);
                counts[record.EffectiveItemId] = current + record.EffectiveCount;
            }

            return counts;
        }

        private bool FinalizeMutation(Dictionary<int, int> before)
        {
            Dictionary<int, int> after = CaptureCounts();
            bool changed = EmitDeltaEvents(before, after);
            if (changed && _autoSave)
            {
                Save();
            }

            return changed;
        }

        private bool EmitDeltaEvents(Dictionary<int, int> before, Dictionary<int, int> after)
        {
            HashSet<int> ids = new();
            foreach (KeyValuePair<int, int> pair in before)
            {
                ids.Add(pair.Key);
            }

            foreach (KeyValuePair<int, int> pair in after)
            {
                ids.Add(pair.Key);
            }

            bool changed = false;
            foreach (int itemId in ids)
            {
                before.TryGetValue(itemId, out int oldCount);
                after.TryGetValue(itemId, out int newCount);
                if (oldCount == newCount)
                {
                    continue;
                }

                changed = true;
                if (newCount > oldCount)
                {
                    OnItemAdded?.Invoke(itemId, newCount - oldCount);
                }
                else
                {
                    OnItemRemoved?.Invoke(itemId, oldCount - newCount);
                }

                OnItemCountChanged?.Invoke(itemId, newCount);
                if (oldCount > 0 && newCount <= 0)
                {
                    OnItemBecameZero?.Invoke(itemId);
                }
            }

            if (changed)
            {
                OnInventoryChanged?.Invoke();
            }

            return changed;
        }
    }
}
