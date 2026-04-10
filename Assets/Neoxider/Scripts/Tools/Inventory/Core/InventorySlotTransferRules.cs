using System;

namespace Neo.Tools
{
    /// <summary>
    ///     Low-level slot merge, move, and swap helpers used by slotted storage and <see cref="InventoryTransferService" />.
    /// </summary>
    public static class InventorySlotTransferRules
    {
        /// <summary>
        ///     Merges into target, moves to empty target, or full-swaps incompatible stacks.
        /// </summary>
        /// <returns>Units moved or swapped.</returns>
        public static int TryMoveOrSwap(ref InventorySlotState source, ref InventorySlotState target, int maxStack,
            int amount = 0)
        {
            if (source == null || source.IsEmpty)
            {
                return 0;
            }

            if (target == null)
            {
                target = InventorySlotState.Empty();
            }

            if (!target.IsEmpty && InventoryStackRules.CanMergeSimple(target, source.ToRecord()))
            {
                int desired = amount > 0 ? amount : source.Count;
                int addable = InventoryStackRules.GetAddableAmount(target.Count, desired, maxStack, target.Count, 0);
                if (addable <= 0)
                {
                    return 0;
                }

                target.Count += addable;
                source.Count -= addable;
                if (source.Count <= 0)
                {
                    source = InventorySlotState.Empty();
                }

                return addable;
            }

            int available = source.EffectiveCount;
            int requested = amount > 0 ? Math.Min(amount, available) : available;
            if (requested <= 0)
            {
                return 0;
            }

            if (target.IsEmpty)
            {
                InventoryItemRecord extracted = Extract(ref source, requested);
                if (extracted == null)
                {
                    return 0;
                }

                target = CreateSlotFromRecord(extracted);
                return extracted.EffectiveCount;
            }

            if (amount > 0 && requested < available)
            {
                return 0;
            }

            InventorySlotState temp = source.Clone();
            source = target.Clone();
            target = temp;
            return requested;
        }

        public static InventoryItemRecord Extract(ref InventorySlotState source, int amount)
        {
            if (source == null || source.IsEmpty)
            {
                return null;
            }

            int takeAmount = amount > 0 ? Math.Min(amount, source.EffectiveCount) : source.EffectiveCount;
            if (takeAmount <= 0)
            {
                return null;
            }

            if (source.IsInstance)
            {
                InventoryItemInstance instance = source.Instance.Clone();
                instance.Count = Math.Max(1, takeAmount);

                if (takeAmount >= source.Instance.Count)
                {
                    source = InventorySlotState.Empty();
                }
                else
                {
                    source.Instance.Count -= takeAmount;
                }

                return new InventoryItemRecord(instance);
            }

            InventoryItemRecord record = new(source.ItemId, takeAmount);
            source.Count -= takeAmount;
            if (source.Count <= 0)
            {
                source = InventorySlotState.Empty();
            }

            return record;
        }

        /// <summary>Inserts or merges <paramref name="record" /> into <paramref name="target" />.</summary>
        /// <returns>Amount inserted.</returns>
        public static int TryInsert(ref InventorySlotState target, InventoryItemRecord record, int maxStack,
            int amount = 0)
        {
            if (record == null)
            {
                return 0;
            }

            int desired = amount > 0 ? Math.Min(amount, record.EffectiveCount) : record.EffectiveCount;
            if (desired <= 0)
            {
                return 0;
            }

            if (target == null)
            {
                target = InventorySlotState.Empty();
            }

            if (record.IsInstance)
            {
                if (!target.IsEmpty)
                {
                    return 0;
                }

                InventoryItemInstance instance = record.Instance.Clone();
                instance.Count = Math.Max(1, desired);
                target = new InventorySlotState
                {
                    ItemId = instance.ItemId,
                    Count = instance.Count,
                    Instance = instance
                };
                return instance.Count;
            }

            if (target.IsEmpty)
            {
                target = new InventorySlotState
                {
                    ItemId = record.ItemId,
                    Count = desired
                };
                return desired;
            }

            if (!InventoryStackRules.CanMergeSimple(target, record))
            {
                return 0;
            }

            int addable = InventoryStackRules.GetAddableAmount(target.Count, desired, maxStack, target.Count, 0);
            if (addable <= 0)
            {
                return 0;
            }

            target.Count += addable;
            return addable;
        }

        /// <summary>Builds slot state from a stack or instance record.</summary>
        public static InventorySlotState CreateSlotFromRecord(InventoryItemRecord record)
        {
            if (record == null)
            {
                return InventorySlotState.Empty();
            }

            if (record.IsInstance)
            {
                InventoryItemInstance instance = record.Instance.Clone();
                return new InventorySlotState
                {
                    ItemId = instance.ItemId,
                    Count = Math.Max(1, instance.Count),
                    Instance = instance
                };
            }

            return new InventorySlotState
            {
                ItemId = record.ItemId,
                Count = Math.Max(0, record.Count)
            };
        }
    }
}
