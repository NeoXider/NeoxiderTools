using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace Neo.Tools
{
    public sealed partial class InventoryComponent
    {
        /// <summary>Adds one unit of <paramref name="itemId" /> respecting stack rules and limits.</summary>
        /// <returns>Amount actually added (may be less than requested).</returns>
        [Button(PlayModeOnly = true)]
        public int AddItemById(int itemId)
        {
            return AddItemByIdAmount(itemId, 1);
        }

        /// <summary>Adds stackable or instance-based items depending on <see cref="InventoryItemData.SupportsInstanceState" />.</summary>
        /// <param name="itemId">Runtime item identifier.</param>
        /// <param name="amount">Requested amount; for instance-based items each unit becomes a separate instance.</param>
        /// <returns>Amount actually added.</returns>
        [Button(PlayModeOnly = true)]
        public int AddItemByIdAmount(int itemId, int amount)
        {
            MutationReceipt receipt = CommitItemAdd(itemId, amount);
            receipt.Publish();
            receipt.ThrowIfFaulted();
            return receipt.AcceptedCount;
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
                int instanceAdded = AddItemInstance(new InventoryItemInstance(itemData.ItemId));
                if (instanceAdded <= 0)
                {
                    break;
                }

                added += instanceAdded;
            }

            return added;
        }

        /// <summary>Adds a single <see cref="InventoryItemInstance" /> with optional per-instance state payload.</summary>
        /// <returns>The instance count if added, 0 if rejected.</returns>
        public int AddItemInstance(InventoryItemInstance instance)
        {
            MutationReceipt receipt = CommitInstanceAdd(instance);
            receipt.Publish();
            receipt.ThrowIfFaulted();
            return receipt.AcceptedCount;
        }

        /// <summary>Removes one unit of <paramref name="itemId" /> from aggregated storage or the first matching stack/slot.</summary>
        /// <returns>Amount actually removed.</returns>
        [Button(PlayModeOnly = true)]
        public int RemoveItemById(int itemId)
        {
            return RemoveItemByIdAmount(itemId, 1);
        }

        /// <summary>Removes up to <paramref name="amount" /> of <paramref name="itemId" />.</summary>
        /// <returns>Amount actually removed.</returns>
        [Button(PlayModeOnly = true)]
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

        /// <summary>Removes all items and raises delta events before <see cref="OnInventoryChanged" />.</summary>
        [Button(PlayModeOnly = true)]
        public void ClearInventory()
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            _storage.Clear();
            FinalizeMutation(before);
        }

        /// <summary>Delegates to <see cref="InventoryDropper" /> for the configured selection policy.</summary>
        /// <returns>Amount dropped into the world.</returns>
        [Button(PlayModeOnly = true)]
        public int DropSelected(int amount = 1)
        {
            return _dropper != null ? _dropper.DropSelected(amount) : 0;
        }

        /// <summary>Drops <paramref name="amount" /> of <paramref name="itemId" /> via <see cref="InventoryDropper" />.</summary>
        [Button(PlayModeOnly = true)]
        public int DropById(int itemId, int amount = 1)
        {
            return _dropper != null ? _dropper.DropById(itemId, amount) : 0;
        }

        /// <summary>Drops using <paramref name="itemData" /> id and instance rules.</summary>
        public int DropData(InventoryItemData itemData, int amount = 1)
        {
            return _dropper != null ? _dropper.DropData(itemData, amount) : 0;
        }

        /// <summary>Drops from the first non-empty packed record.</summary>
        [Button(PlayModeOnly = true)]
        public int DropFirst(int amount = 1)
        {
            return _dropper != null ? _dropper.DropFirst(amount) : 0;
        }

        /// <summary>Drops from the last non-empty packed record.</summary>
        [Button(PlayModeOnly = true)]
        public int DropLast(int amount = 1)
        {
            return _dropper != null ? _dropper.DropLast(amount) : 0;
        }

        /// <summary>Editor/debug: adds one of <see cref="SelectedItemId" />.</summary>
        [Button(PlayModeOnly = true)]
        public int TestAdd1Selected()
        {
            return AddItemById(_selectedItemId);
        }

        /// <summary>Editor/debug: removes one of <see cref="SelectedItemId" />.</summary>
        [Button(PlayModeOnly = true)]
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

            if (_storage is IInventoryRecordReader reader)
            {
                reader.AccumulateCounts(counts);
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

        private bool FinalizeMutation(Dictionary<int, int> before, bool contentChanged = false)
        {
            return FinalizeMutation(before, CaptureCounts(), contentChanged);
        }

        private bool FinalizeMutation(Dictionary<int, int> before, Dictionary<int, int> after, bool contentChanged)
        {
            bool changed = contentChanged || before.Count != after.Count;
            if (!changed)
            {
                foreach (KeyValuePair<int, int> pair in before)
                {
                    if (!after.TryGetValue(pair.Key, out int count) || count != pair.Value)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            bool shouldSave = changed && _autoSave;
            if (shouldSave && _autoSaveDeferDepth > 0)
            {
                // WHY: the outermost DeferAutoSave scope writes once for the whole burst.
                _autoSavePending = true;
                shouldSave = false;
            }

            ExceptionDispatchInfo firstFault = null;
            try
            {
                EmitDeltaEvents(before, after, contentChanged);
            }
            catch (Exception exception)
            {
                firstFault = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                if (shouldSave)
                {
                    try
                    {
                        Save();
                    }
                    catch (Exception exception)
                    {
                        if (firstFault == null)
                        {
                            firstFault = ExceptionDispatchInfo.Capture(exception);
                        }
                    }
                }
            }

            firstFault?.Throw();
            return changed;
        }

        // WHY: Walking "before" and then the after-only ids keeps the former merged-id event order without
        // allocating a set per mutation; both maps belong to this mutation, so a listener that mutates the
        // inventory again builds its own maps and cannot invalidate the enumeration.
        private bool EmitDeltaEvents(Dictionary<int, int> before, Dictionary<int, int> after, bool contentChanged)
        {
            bool changed = contentChanged;
            foreach (KeyValuePair<int, int> pair in before)
            {
                after.TryGetValue(pair.Key, out int newCount);
                changed |= EmitItemDelta(pair.Key, pair.Value, newCount);
            }

            foreach (KeyValuePair<int, int> pair in after)
            {
                if (!before.ContainsKey(pair.Key))
                {
                    changed |= EmitItemDelta(pair.Key, 0, pair.Value);
                }
            }

            if (changed)
            {
                OnInventoryChanged?.Invoke();
            }

            return changed;
        }

        private bool EmitItemDelta(int itemId, int oldCount, int newCount)
        {
            if (oldCount == newCount)
            {
                return false;
            }

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

            return true;
        }
    }
}
