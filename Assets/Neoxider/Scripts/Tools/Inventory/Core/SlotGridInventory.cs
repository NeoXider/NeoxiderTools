using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     Pure C# fixed slot array implementing both <see cref="IInventoryStorage" /> and <see cref="ISlottedInventory" />.
    /// </summary>
    [Serializable]
    public sealed class SlotGridInventory : IInventoryStorage, ISlottedInventory
    {
        private InventoryConstraints _constraints = new();
        private InventorySlotState[] _slots;

        /// <summary>Allocates <paramref name="slotCapacity" /> empty slots.</summary>
        public SlotGridInventory(int slotCapacity)
        {
            Resize(slotCapacity);
        }

        /// <inheritdoc />
        public int SlotCapacity => _slots != null ? _slots.Length : 0;

        /// <inheritdoc />
        public int TotalCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < SlotCapacity; i++)
                {
                    total += _slots[i].EffectiveCount;
                }

                return total;
            }
        }

        /// <inheritdoc />
        public int UniqueCount
        {
            get
            {
                HashSet<int> unique = new();
                for (int i = 0; i < SlotCapacity; i++)
                {
                    InventorySlotState slot = _slots[i];
                    if (!slot.IsEmpty)
                    {
                        unique.Add(slot.EffectiveItemId);
                    }
                }

                return unique.Count;
            }
        }

        /// <summary>Changes slot count; copies overlapping indices from the previous array.</summary>
        public void Resize(int slotCapacity)
        {
            int capacity = Math.Max(0, slotCapacity);
            InventorySlotState[] next = new InventorySlotState[capacity];
            for (int i = 0; i < capacity; i++)
            {
                next[i] = InventorySlotState.Empty();
            }

            if (_slots != null)
            {
                int copyCount = Math.Min(_slots.Length, next.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    next[i] = _slots[i].Clone();
                }
            }

            _slots = next;
        }

        /// <inheritdoc />
        public void SetConstraints(InventoryConstraints constraints)
        {
            _constraints = constraints != null ? constraints.Clone() : new InventoryConstraints();
        }

        /// <inheritdoc />
        public int Add(int itemId, int amount = 1)
        {
            if (itemId < 0 || amount <= 0 || SlotCapacity <= 0)
            {
                return 0;
            }

            int added = 0;
            int maxStack = _constraints.GetMaxStack(itemId);
            bool hasSameType = Has(itemId);
            if (!hasSameType && _constraints.MaxUniqueItems > 0 && UniqueCount >= _constraints.MaxUniqueItems)
            {
                return 0;
            }

            for (int i = 0; i < SlotCapacity && added < amount; i++)
            {
                InventorySlotState slot = _slots[i];
                if (slot.IsEmpty || slot.IsInstance || slot.ItemId != itemId)
                {
                    continue;
                }

                int addable = InventoryStackRules.GetAddableAmount(slot.Count, amount - added, maxStack, TotalCount, _constraints.MaxTotalItems);
                if (addable <= 0)
                {
                    continue;
                }

                slot.Count += addable;
                added += addable;
            }

            for (int i = 0; i < SlotCapacity && added < amount; i++)
            {
                InventorySlotState slot = _slots[i];
                if (!slot.IsEmpty)
                {
                    continue;
                }

                int addable = InventoryStackRules.GetAddableAmount(0, amount - added, maxStack, TotalCount, _constraints.MaxTotalItems);
                if (addable <= 0)
                {
                    continue;
                }

                _slots[i] = new InventorySlotState
                {
                    ItemId = itemId,
                    Count = addable
                };
                added += addable;
            }

            return added;
        }

        /// <inheritdoc />
        public int AddInstance(InventoryItemInstance instance)
        {
            if (instance == null || SlotCapacity <= 0)
            {
                return 0;
            }

            InventoryItemInstance clone = instance.Clone();
            bool hasSameType = Has(clone.ItemId);
            if (!hasSameType && _constraints.MaxUniqueItems > 0 && UniqueCount >= _constraints.MaxUniqueItems)
            {
                return 0;
            }

            if (_constraints.MaxTotalItems > 0 && TotalCount + clone.Count > _constraints.MaxTotalItems)
            {
                return 0;
            }

            for (int i = 0; i < SlotCapacity; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    continue;
                }

                _slots[i] = new InventorySlotState
                {
                    ItemId = clone.ItemId,
                    Count = Math.Max(1, clone.Count),
                    Instance = clone
                };
                return clone.Count;
            }

            return 0;
        }

        /// <inheritdoc />
        public int Remove(int itemId, int amount = 1)
        {
            if (itemId < 0 || amount <= 0)
            {
                return 0;
            }

            int removed = 0;
            for (int i = 0; i < SlotCapacity && removed < amount; i++)
            {
                InventorySlotState slot = _slots[i];
                if (slot.IsEmpty || slot.EffectiveItemId != itemId)
                {
                    continue;
                }

                int take = Math.Min(amount - removed, slot.EffectiveCount);
                removed += take;

                if (slot.IsInstance)
                {
                    if (take >= slot.Instance.Count)
                    {
                        _slots[i] = InventorySlotState.Empty();
                    }
                    else
                    {
                        slot.Instance.Count -= take;
                        slot.Count = slot.Instance.Count;
                    }
                }
                else
                {
                    slot.Count -= take;
                    if (slot.Count <= 0)
                    {
                        _slots[i] = InventorySlotState.Empty();
                    }
                }
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
            for (int i = 0; i < SlotCapacity; i++)
            {
                InventorySlotState slot = _slots[i];
                if (!slot.IsEmpty && slot.EffectiveItemId == itemId)
                {
                    total += slot.EffectiveCount;
                }
            }

            return total;
        }

        /// <inheritdoc />
        public List<InventoryItemRecord> CreateRecordSnapshot()
        {
            List<InventoryItemRecord> records = new();
            for (int i = 0; i < SlotCapacity; i++)
            {
                InventorySlotState slot = _slots[i];
                if (!slot.IsEmpty)
                {
                    records.Add(slot.ToRecord());
                }
            }

            return records;
        }

        /// <inheritdoc />
        public List<InventoryItemInstance> CreateInstanceSnapshot()
        {
            List<InventoryItemInstance> instances = new();
            for (int i = 0; i < SlotCapacity; i++)
            {
                InventorySlotState slot = _slots[i];
                if (slot.IsInstance && slot.Instance != null)
                {
                    instances.Add(slot.Instance.Clone());
                }
            }

            return instances;
        }

        /// <inheritdoc />
        public bool TryGetRecordAtPackedIndex(int packedIndex, out InventoryItemRecord record)
        {
            record = null;
            if (packedIndex < 0)
            {
                return false;
            }

            int current = 0;
            for (int i = 0; i < SlotCapacity; i++)
            {
                InventorySlotState slot = _slots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                if (current == packedIndex)
                {
                    record = slot.ToRecord();
                    return true;
                }

                current++;
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryTakeRecordAtPackedIndex(int packedIndex, int amount, out InventoryItemRecord record)
        {
            record = null;
            if (packedIndex < 0)
            {
                return false;
            }

            int current = 0;
            for (int i = 0; i < SlotCapacity; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    continue;
                }

                if (current == packedIndex)
                {
                    return TryTakeSlot(i, amount, out record);
                }

                current++;
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryRemoveFirstInstance(int itemId, out InventoryItemInstance instance)
        {
            instance = null;
            for (int i = 0; i < SlotCapacity; i++)
            {
                InventorySlotState slot = _slots[i];
                if (!slot.IsInstance || slot.EffectiveItemId != itemId)
                {
                    continue;
                }

                instance = slot.Instance.Clone();
                _slots[i] = InventorySlotState.Empty();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public InventorySlotState GetSlot(int slotIndex)
        {
            return IsValidSlotIndex(slotIndex) ? _slots[slotIndex].Clone() : InventorySlotState.Empty();
        }

        /// <inheritdoc />
        public void SetSlot(int slotIndex, InventorySlotState state)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }

            _slots[slotIndex] = state != null ? state.Clone() : InventorySlotState.Empty();
        }

        /// <inheritdoc />
        public bool SwapSlots(int sourceIndex, int targetIndex)
        {
            if (!IsValidSlotIndex(sourceIndex) || !IsValidSlotIndex(targetIndex) || sourceIndex == targetIndex)
            {
                return false;
            }

            InventorySlotState temp = _slots[sourceIndex];
            _slots[sourceIndex] = _slots[targetIndex];
            _slots[targetIndex] = temp;
            return true;
        }

        /// <inheritdoc />
        public int MoveSlot(int sourceIndex, int targetIndex, int amount = 0)
        {
            if (!IsValidSlotIndex(sourceIndex) || !IsValidSlotIndex(targetIndex) || sourceIndex == targetIndex)
            {
                return 0;
            }

            InventorySlotState source = _slots[sourceIndex].Clone();
            InventorySlotState target = _slots[targetIndex].Clone();
            int moved = InventorySlotTransferRules.TryMoveOrSwap(ref source, ref target,
                _constraints.GetMaxStack(source.EffectiveItemId), amount);
            if (moved <= 0)
            {
                return 0;
            }

            _slots[sourceIndex] = source ?? InventorySlotState.Empty();
            _slots[targetIndex] = target ?? InventorySlotState.Empty();
            return moved;
        }

        /// <inheritdoc />
        public bool TryTakeSlot(int slotIndex, int amount, out InventoryItemRecord record)
        {
            record = null;
            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            InventorySlotState source = _slots[slotIndex].Clone();
            record = InventorySlotTransferRules.Extract(ref source, amount);
            if (record == null)
            {
                return false;
            }

            _slots[slotIndex] = source ?? InventorySlotState.Empty();
            return true;
        }

        /// <inheritdoc />
        public int TryInsertIntoSlot(int slotIndex, InventoryItemRecord record, int amount = 0)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return 0;
            }

            InventorySlotState slot = _slots[slotIndex].Clone();
            int inserted = InventorySlotTransferRules.TryInsert(ref slot, record,
                _constraints.GetMaxStack(record != null ? record.EffectiveItemId : -1), amount);
            if (inserted <= 0)
            {
                return 0;
            }

            _slots[slotIndex] = slot ?? InventorySlotState.Empty();
            return inserted;
        }

        /// <inheritdoc />
        public List<InventorySlotState> CreateSlotSnapshot()
        {
            List<InventorySlotState> snapshot = new(SlotCapacity);
            for (int i = 0; i < SlotCapacity; i++)
            {
                snapshot.Add(_slots[i].Clone());
            }

            return snapshot;
        }

        /// <inheritdoc />
        public void Clear()
        {
            for (int i = 0; i < SlotCapacity; i++)
            {
                _slots[i] = InventorySlotState.Empty();
            }
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SlotCapacity;
        }
    }
}
