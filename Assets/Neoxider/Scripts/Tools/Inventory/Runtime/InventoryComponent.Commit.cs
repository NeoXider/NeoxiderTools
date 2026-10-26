using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Neo.Tools
{
    public sealed partial class InventoryComponent
    {
        // Internal ownership handoff for world items. Storage commits before any user notification;
        // the caller settles its exact accepted/extracted quantity before publishing this receipt.
        internal sealed class MutationReceipt
        {
            private readonly InventoryComponent _inventory;
            private readonly Dictionary<int, int> _before;
            private readonly Dictionary<int, int> _after;
            private readonly int _rejectedItemId;
            private readonly int _rejectedCount;
            private bool _published;

            internal MutationReceipt(InventoryComponent inventory, Dictionary<int, int> before, int acceptedCount,
                InventoryItemRecord record = null, int physicalSlotIndex = -1, int rejectedItemId = -1,
                int rejectedCount = 0)
            {
                _inventory = inventory;
                _before = before;
                _after = inventory.CaptureCounts();
                AcceptedCount = acceptedCount;
                Record = record;
                PhysicalSlotIndex = physicalSlotIndex;
                _rejectedItemId = rejectedItemId;
                _rejectedCount = rejectedCount;
            }

            internal int AcceptedCount { get; }
            internal InventoryItemRecord Record { get; }
            internal int PhysicalSlotIndex { get; }
            internal ExceptionDispatchInfo Fault { get; private set; }

            internal void CaptureFault(Exception exception)
            {
                if (Fault == null)
                {
                    Fault = ExceptionDispatchInfo.Capture(exception);
                }
            }

            internal void Publish()
            {
                if (_published)
                {
                    return;
                }

                _published = true;
                try
                {
                    _inventory.FinalizeMutation(_before, _after, false);
                }
                catch (Exception exception)
                {
                    CaptureFault(exception);
                }

                if (_rejectedCount > 0)
                {
                    try
                    {
                        _inventory.OnCapacityRejected?.Invoke(_rejectedItemId, _rejectedCount);
                    }
                    catch (Exception exception)
                    {
                        CaptureFault(exception);
                    }
                }
            }

            internal void ThrowIfFaulted()
            {
                Fault?.Throw();
            }
        }

        internal MutationReceipt CommitItemAdd(int itemId, int amount)
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            int added = 0;
            if (amount > 0 && CanUseItemId(itemId))
            {
                if (IsInstanceBasedItem(itemId))
                {
                    for (int i = 0; i < amount; i++)
                    {
                        int accepted = _storage.AddInstance(new InventoryItemInstance(itemId));
                        if (accepted <= 0)
                        {
                            break;
                        }

                        added += accepted;
                    }
                }
                else
                {
                    added = _storage.Add(itemId, amount);
                }
            }

            return new MutationReceipt(this, before, added, rejectedItemId: itemId,
                rejectedCount: Math.Max(0, amount - added));
        }

        internal MutationReceipt CommitInstanceAdd(InventoryItemInstance instance)
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            if (instance == null || !CanUseItemId(instance.ItemId))
            {
                return new MutationReceipt(this, before, 0);
            }

            InventoryItemInstance normalized = instance.Clone();
            int added = _storage.AddInstance(normalized);
            return new MutationReceipt(this, before, added,
                added > 0 ? new InventoryItemRecord(normalized) : null,
                rejectedItemId: normalized.ItemId, rejectedCount: added > 0 ? 0 : normalized.Count);
        }

        internal MutationReceipt CommitRecordTake(int index, int amount, bool physicalIndex)
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            InventoryItemRecord record = null;
            int physicalSlot = physicalIndex ? index : ResolvePhysicalSlotIndex(index);
            if (physicalIndex)
            {
                if (_storage is ISlottedInventory slotted)
                {
                    slotted.TryTakeSlot(index, amount, out record);
                }
            }
            else
            {
                _storage.TryTakeRecordAtPackedIndex(index, amount, out record);
            }

            return new MutationReceipt(this, before, record != null ? record.EffectiveCount : 0, record,
                physicalSlot);
        }

        internal int FindPackedIndex(int itemId)
        {
            List<InventoryItemRecord> records = GetSnapshotRecords();
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].EffectiveItemId == itemId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int ResolvePhysicalSlotIndex(int packedIndex)
        {
            if (packedIndex < 0 || _storage is not ISlottedInventory slotted)
            {
                return -1;
            }

            int current = 0;
            for (int i = 0; i < slotted.SlotCapacity; i++)
            {
                if (slotted.GetSlot(i).IsEmpty)
                {
                    continue;
                }

                if (current++ == packedIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        internal MutationReceipt CommitRecordReturn(int physicalSlotIndex, InventoryItemRecord record)
        {
            EnsureRuntimeInitialized();
            Dictionary<int, int> before = CaptureCounts();
            int restored = 0;
            if (record != null)
            {
                if (_storage is ISlottedInventory slotted)
                {
                    InventorySlotState slot = slotted.GetSlot(physicalSlotIndex);
                    if (slot.IsInstance && record.IsInstance && SameInstanceState(slot.Instance, record.Instance))
                    {
                        int count = InventoryStackRules.GetAddableAmount(slot.EffectiveCount, record.EffectiveCount,
                            0, _storage.TotalCount, _constraints.MaxTotalItems);
                        if (count > 0)
                        {
                            slot.Instance.Count += count;
                            slot.Count = slot.Instance.Count;
                            if (CanApplySlotState(physicalSlotIndex, slot))
                            {
                                slotted.SetSlot(physicalSlotIndex, slot);
                                restored = count;
                            }
                        }
                    }
                    else
                    {
                        restored = slotted.TryInsertIntoSlot(physicalSlotIndex, record);
                    }
                }

                int remaining = record.EffectiveCount - restored;
                if (remaining > 0)
                {
                    if (record.IsInstance)
                    {
                        InventoryItemInstance instance = record.Instance.Clone();
                        instance.Count = remaining;
                        restored += _storage.AddInstance(instance);
                    }
                    else
                    {
                        restored += _storage.Add(record.ItemId, remaining);
                    }
                }
            }

            return new MutationReceipt(this, before, restored);
        }

        private static bool SameInstanceState(InventoryItemInstance first, InventoryItemInstance second)
        {
            if (first.InstanceId != second.InstanceId || first.ItemId != second.ItemId ||
                first.StateVersion != second.StateVersion || first.ComponentStates.Count != second.ComponentStates.Count)
            {
                return false;
            }

            for (int i = 0; i < first.ComponentStates.Count; i++)
            {
                if (first.ComponentStates[i].Key != second.ComponentStates[i].Key ||
                    first.ComponentStates[i].Json != second.ComponentStates[i].Json)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
