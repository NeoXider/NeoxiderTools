using System;

namespace Neo.Tools
{
    /// <summary>
    ///     Shared stack math for aggregated and slotted backends.
    /// </summary>
    public static class InventoryStackRules
    {
        /// <summary>True if the record is a normal id/count stack (not instance-based).</summary>
        public static bool IsSimpleStack(InventoryItemRecord record)
        {
            return record != null && !record.IsInstance && record.Count > 0 && record.ItemId >= 0;
        }

        public static bool CanMergeSimple(InventorySlotState target, InventoryItemRecord record)
        {
            if (target == null || record == null || record.IsInstance || target.IsInstance || target.IsEmpty)
            {
                return false;
            }

            return target.ItemId == record.ItemId && target.Count > 0;
        }

        /// <summary>Maps non-positive max stack to effectively unlimited.</summary>
        public static int NormalizeMaxStack(int maxStack)
        {
            return maxStack > 0 ? maxStack : int.MaxValue;
        }

        /// <summary>How many units can be added given current stack, cap, and optional global total cap.</summary>
        public static int GetAddableAmount(int current, int requested, int maxStack, int totalCount, int maxTotalItems)
        {
            if (requested <= 0)
            {
                return 0;
            }

            int addable = requested;
            int normalizedMaxStack = NormalizeMaxStack(maxStack);
            int stackRoom = normalizedMaxStack - Math.Max(0, current);
            if (stackRoom <= 0)
            {
                return 0;
            }

            addable = Math.Min(addable, stackRoom);
            if (maxTotalItems > 0)
            {
                int totalRoom = maxTotalItems - totalCount;
                if (totalRoom <= 0)
                {
                    return 0;
                }

                addable = Math.Min(addable, totalRoom);
            }

            return addable;
        }
    }
}
