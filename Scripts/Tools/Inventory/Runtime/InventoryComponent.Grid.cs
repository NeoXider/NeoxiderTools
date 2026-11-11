using System.Collections.Generic;
using System;
using System.Runtime.ExceptionServices;
using UnityEngine.Pool;

namespace Neo.Tools
{
    public sealed partial class InventoryComponent
    {
        /// <summary>Physical slot state in slot-grid mode; empty slot in aggregated mode.</summary>
        public InventorySlotState GetSlot(int slotIndex)
        {
            EnsureRuntimeInitialized();
            return _storage is ISlottedInventory slotted ? slotted.GetSlot(slotIndex) : InventorySlotState.Empty();
        }

        /// <summary>Clone of instance in physical slot <paramref name="slotIndex" /> (slot grid only).</summary>
        public InventoryItemInstance GetInstanceAtPhysicalSlot(int slotIndex)
        {
            InventorySlotState slot = GetSlot(slotIndex);
            return slot.IsInstance && slot.Instance != null ? slot.Instance.Clone() : null;
        }

        /// <summary>Record representing the physical slot contents, or null if empty (slot grid only).</summary>
        public InventoryItemRecord GetPhysicalSlotRecord(int slotIndex)
        {
            InventorySlotState slot = GetSlot(slotIndex);
            return slot != null && !slot.IsEmpty ? slot.ToRecord() : null;
        }

        /// <summary>Replaces a physical slot if constraints allow (slot grid only).</summary>
        /// <returns>True if the slot was updated.</returns>
        public bool TrySetSlot(int slotIndex, InventorySlotState state)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted || slotIndex < 0 || slotIndex >= slotted.SlotCapacity)
            {
                return false;
            }

            if (!CanApplySlotState(slotIndex, state))
            {
                return false;
            }

            Dictionary<int, int> before = CaptureCounts();
            slotted.SetSlot(slotIndex, state);
            FinalizeMutation(before, true);
            return true;
        }

        /// <summary>Swaps two physical slots (slot grid only).</summary>
        public bool SwapSlots(int sourceIndex, int targetIndex)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted)
            {
                return false;
            }

            Dictionary<int, int> before = CaptureCounts();
            bool result = slotted.SwapSlots(sourceIndex, targetIndex);
            FinalizeMutation(before, result);
            return result;
        }

        /// <summary>Moves or merges stack from source slot into target (slot grid). <paramref name="amount" /> 0 = move all compatible amount.</summary>
        /// <returns>Amount moved.</returns>
        public int MoveSlot(int sourceIndex, int targetIndex, int amount = 0)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted)
            {
                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int moved = slotted.MoveSlot(sourceIndex, targetIndex, amount);
            FinalizeMutation(before, moved > 0);
            return moved;
        }

        /// <summary>Removes from physical slot and returns a record (slot grid only).</summary>
        public bool TryTakeSlot(int slotIndex, int amount, out InventoryItemRecord record)
        {
            MutationReceipt receipt = CommitRecordTake(slotIndex, amount, true);
            record = receipt.Record;
            receipt.Publish();
            receipt.ThrowIfFaulted();
            return record != null;
        }

        /// <summary>Inserts or merges <paramref name="record" /> into physical slot (slot grid only).</summary>
        /// <returns>Amount inserted.</returns>
        public int TryInsertIntoSlot(int slotIndex, InventoryItemRecord record, int amount = 0)
        {
            EnsureRuntimeInitialized();
            if (_storage is not ISlottedInventory slotted || record == null ||
                !CanUseItemId(record.EffectiveItemId))
            {
                return 0;
            }

            Dictionary<int, int> before = CaptureCounts();
            int inserted = slotted.TryInsertIntoSlot(slotIndex, record, amount);
            FinalizeMutation(before);
            return inserted;
        }

        internal bool TryApplySlotTransfer(int sourceIndex, InventorySlotState source,
            InventoryComponent targetInventory, int targetIndex, InventorySlotState target)
        {
            if (ReferenceEquals(this, targetInventory) || _storage is not ISlottedInventory sourceSlots ||
                targetInventory._storage is not ISlottedInventory targetSlots ||
                !CanApplySlotState(sourceIndex, source) || !targetInventory.CanApplySlotState(targetIndex, target))
            {
                return false;
            }

            Dictionary<int, int> sourceBefore = CaptureCounts();
            Dictionary<int, int> targetBefore = targetInventory.CaptureCounts();
            sourceSlots.SetSlot(sourceIndex, source);
            targetSlots.SetSlot(targetIndex, target);
            Dictionary<int, int> sourceAfter = CaptureCounts();
            Dictionary<int, int> targetAfter = targetInventory.CaptureCounts();
            ExceptionDispatchInfo firstFault = null;
            try
            {
                FinalizeMutation(sourceBefore, sourceAfter, true);
            }
            catch (Exception exception)
            {
                firstFault = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                try
                {
                    targetInventory.FinalizeMutation(targetBefore, targetAfter, true);
                }
                catch (Exception exception)
                {
                    if (firstFault == null)
                    {
                        firstFault = ExceptionDispatchInfo.Capture(exception);
                    }
                }
            }

            firstFault?.Throw();
            return true;
        }

        private bool CanApplySlotState(int slotIndex, InventorySlotState state)
        {
            if (_storage is not ISlottedInventory slotted)
            {
                return false;
            }

            // WHY: The candidate is only read below, so it needs no defensive deep copy.
            InventorySlotState next = state ?? InventorySlotState.Empty();
            if (!next.IsEmpty)
            {
                if (next.EffectiveItemId < 0 || !CanUseItemId(next.EffectiveItemId))
                {
                    return false;
                }

                int maxStack = GetMaxStack(next.EffectiveItemId);
                if (!next.IsInstance && maxStack > 0 && next.Count > maxStack)
                {
                    return false;
                }
            }

            int capacity = slotted.SlotCapacity;
            if (slotIndex < 0 || slotIndex >= capacity)
            {
                return false;
            }

            // WHY: The built-in grid is read in place; cloning every slot per validation made each slot import
            // during Load O(slots) clones, i.e. O(slots^2) for a full grid. Custom backends keep the snapshot path.
            SlotGridInventory grid = slotted as SlotGridInventory;
            List<InventorySlotState> snapshot = grid == null ? slotted.CreateSlotSnapshot() : null;
            long total = 0;
            HashSet<int> unique = HashSetPool<int>.Get();
            try
            {
                for (int i = 0; i < capacity; i++)
                {
                    InventorySlotState slot = i == slotIndex ? next
                        : grid != null ? grid.PeekSlot(i)
                        : i < snapshot.Count ? snapshot[i] : null;
                    if (slot == null || slot.IsEmpty)
                    {
                        continue;
                    }

                    total += slot.EffectiveCount;
                    unique.Add(slot.EffectiveItemId);
                }

                if (total > int.MaxValue || (_maxTotalItems > 0 && total > _maxTotalItems))
                {
                    return false;
                }

                return _maxUniqueItems <= 0 || unique.Count <= _maxUniqueItems;
            }
            finally
            {
                HashSetPool<int>.Release(unique);
            }
        }

        /// <summary>Live slot for one-shot UI binding inside this assembly; never null, must not be mutated.</summary>
        internal InventorySlotState PeekSlotForView(int slotIndex)
        {
            EnsureRuntimeInitialized();
            if (_storage is SlotGridInventory grid)
            {
                return grid.PeekSlot(slotIndex) ?? InventorySlotState.Empty();
            }

            return GetSlot(slotIndex);
        }
    }
}
