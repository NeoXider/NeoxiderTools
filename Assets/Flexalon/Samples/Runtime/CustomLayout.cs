using System.Collections.Generic;
using UnityEngine;

namespace Flexalon.Samples
{
    // This is an example of how to implement your own Layout.
    // The layout strategy is to place the children on after
    // the other diagonally ascending.
    // See also CustomLayoutEditor.
    public class CustomLayout : LayoutBase
    {
        [SerializeField]
        private Vector3 _gap = Vector3.zero;

        private static List<FlexItem> _flexItems = new List<FlexItem>();

        public static Vector3 AggregateLayoutSizes(IReadOnlyList<FlexalonNode> nodes, Vector3 size)
        {
            var totalSize = Vector3.zero;
            foreach (var child in nodes)
            {
                // Note: this GetMeasureSize will be 0 for any child axis using SizeType.Fill.
                totalSize += child.GetMeasureSize(size);
            }

            return totalSize;
        }

        private static void ShrinkFillChildren(IReadOnlyList<FlexalonNode> nodes, Vector3 usedSize, Vector3 size, Vector3 gap)
        {
            var remainingSpace = size - usedSize;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(remainingSpace[axis]) <= 1e-6f)
                {
                    continue;
                }

                _flexItems.Clear();
                for (int i = 0; i < nodes.Count; i++)
                {
                    _flexItems.Add(Flex.CreateFlexItem(
                        nodes[i], axis, nodes[i].GetMeasureSize(axis, size[axis]), usedSize[axis], size[axis]));
                }

                Flex.GrowOrShrink(_flexItems, usedSize[axis], size[axis], gap[axis]);

                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].SetShrinkFillSize(axis, _flexItems[i].FinalSize, size[axis]);
                }
            }
        }

        // Measure update the size of this node by accounting
        // for any axes which are assigned SizeType.Layout. This method
        // should also determine the sizes of any children using SizeType.Fill
        // by calling SetFillSize.
        public override Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            // The layout size should be the sum of all child sizes.
            var aggregateSize = AggregateLayoutSizes(node.Children, size);

            // Make sure to add the gaps between children.
            aggregateSize += _gap * (node.Children.Count - 1);

            // Clamp the aggregate size between min and max. Note 'size' is already clamped.
            aggregateSize = Math.Clamp(aggregateSize, min, max);

            // Adjust the size for axes which are SizeType.Layout.
            for (int axis = 0; axis < 3; axis++)
            {
                if (node.GetSizeType(axis) == SizeType.Layout)
                {
                    size[axis] = aggregateSize[axis];
                }
            }

            // Grow or shrink the children to try to make aggregateSize match size.
            ShrinkFillChildren(node.Children, aggregateSize, size, _gap);

            return new Bounds(Vector3.zero, size);
        }

        // Arrange the children in a diagonal pattern.
        public override void Arrange(FlexalonNode node, Vector3 layoutSize)
        {
            var nextPosition = -layoutSize / 2;
            foreach (var child in node.Children)
            {
                var childSize = child.GetArrangeSize();
                child.SetPositionResult(nextPosition + childSize / 2);
                child.SetRotationResult(Quaternion.identity);
                nextPosition += childSize + _gap;
            }
        }
    }
}