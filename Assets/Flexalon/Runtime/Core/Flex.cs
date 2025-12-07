using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    public class FlexItem
    {
        public float MinSize;
        public float MaxSize;
        public float StartSize;
        public float ShrinkFactor;
        public float GrowFactor;
        public float FinalSize;
    }

    public static class Flex
    {
        public static void GrowOrShrink(List<FlexItem> items, float usedSpace, float totalSpace, float gap)
        {
            if (totalSpace > usedSpace)
            {
                Grow(items, totalSpace, gap);
            }
            else
            {
                Shrink(items, totalSpace, gap);
            }
        }

        public static void Grow(List<FlexItem> items, float space, float gap)
        {
            space -= gap * (items.Count - 1);

            var totalFactor = 0f;
            foreach (var item in items)
            {
                if (item.GrowFactor == 0)
                {
                    item.FinalSize = Mathf.Clamp(item.StartSize, item.MinSize, item.MaxSize);
                }
                else if (item.StartSize >= item.MaxSize)
                {
                    item.FinalSize = item.MaxSize;
                }
                else
                {
                    item.FinalSize = item.StartSize;
                    totalFactor += item.GrowFactor;
                }

                space = Mathf.Max(0, space - item.FinalSize);
            }

            var canGrow = totalFactor > 0;
            var remaining = space;
            while (canGrow)
            {
                canGrow = false;
                foreach (var item in items)
                {
                    if (item.FinalSize < item.MaxSize && item.GrowFactor > 0)
                    {
                        if (totalFactor >= 1)
                        {
                            item.FinalSize = item.StartSize + remaining * item.GrowFactor * (1 / totalFactor);
                        }
                        else
                        {
                            item.FinalSize = item.StartSize + space * item.GrowFactor;
                        }

                        if (item.FinalSize > item.MaxSize)
                        {
                            item.FinalSize = item.MaxSize;
                            totalFactor -= item.GrowFactor;
                            remaining -= item.MaxSize;
                            canGrow = totalFactor > 0;
                        }
                        else if (item.FinalSize < item.MinSize)
                        {
                            item.FinalSize = item.MinSize;
                            totalFactor -= item.GrowFactor;
                            remaining -= item.MinSize;
                            canGrow = totalFactor > 0;
                            item.GrowFactor = 0;
                        }
                    }
                }
            }
        }

        public static void Shrink(List<FlexItem> items, float space, float gap)
        {
            var totalFactor = 0f;
            space -= gap * (items.Count - 1);

            foreach (var item in items)
            {
                item.FinalSize = Mathf.Clamp(item.StartSize, item.MinSize, item.MaxSize);
                if (item.FinalSize > item.MinSize && item.ShrinkFactor > 0)
                {
                    totalFactor += item.ShrinkFactor;
                }
                else
                {
                    space = Mathf.Max(0, space - item.FinalSize);
                }
            }

            bool canShrink = totalFactor > 0;
            while (canShrink)
            {
                canShrink = false;
                foreach (var item in items)
                {
                    if (item.FinalSize > item.MinSize && item.ShrinkFactor > 0)
                    {
                        item.FinalSize = space * item.ShrinkFactor * (1 / totalFactor);
                        if (item.FinalSize < item.MinSize)
                        {
                            item.FinalSize = item.MinSize;
                            space = Mathf.Max(0, space - item.MinSize);
                            totalFactor -= item.ShrinkFactor;
                            canShrink = totalFactor > 0;
                        }
                    }
                }
            }
        }

        public static FlexItem CreateFlexItem(FlexalonNode node, int axis, float childSize, float usedSize, float layoutSize)
        {
            var growFactor = node.GetSizeType(axis) == SizeType.Fill ? node.SizeOfParent[axis] : 0;

            return new FlexItem()
            {
                MinSize = node.GetMinSize(axis, layoutSize),
                MaxSize = node.GetMaxSize(axis, layoutSize),
                StartSize = growFactor == 0 ? childSize : 0, // Don't support flex-basis + grow
                ShrinkFactor = node.CanShrink(axis) ? childSize / usedSize : 0,
                GrowFactor = growFactor
            };
        }
    }
}