using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// Use a align layout to align all children to the parent on the specified axes.
    /// For example, use a align layout to place all children along a floor, wall, or edge.
    ///
    /// Once aligned, you can adjust the position, rotation, or size of each child by
    /// editing the Offset, Rotation, Size, and Scale properties on that child's Flexalon Object Component.
    /// </summary>
    [AddComponentMenu("Flexalon/Flexalon Align Layout"), HelpURL("https://www.flexalon.com/docs/alignLayout")]
    public class FlexalonAlignLayout : LayoutBase
    {
        [SerializeField]
        private Align _horizontalAlign = Align.Center;
        /// <summary> The horizontal alignment in the size of the layout. </summary>
        public Align HorizontalAlign
        {
            get { return _horizontalAlign; }
            set { _horizontalAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _verticalAlign = Align.Center;
        /// <summary> The vertical alignment in the size of the layout. </summary>
        public Align VerticalAlign
        {
            get { return _verticalAlign; }
            set { _verticalAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _depthAlign = Align.Center;
        /// <summary> The depth alignment in the size of the layout. </summary>
        public Align DepthAlign
        {
            get { return _depthAlign; }
            set { _depthAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _horizontalPivot = Align.Center;
        /// <summary> The horizontal pivot in the size of each child. </summary>
        public Align HorizontalPivot
        {
            get { return _horizontalPivot; }
            set { _horizontalPivot = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _verticalPivot = Align.Center;
        /// <summary> The vertical pivot in the size of each child. </summary>
        public Align VerticalPivot
        {
            get { return _verticalPivot; }
            set { _verticalPivot = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _depthPivot = Align.Center;
        /// <summary> The depth pivot in the size of each child. </summary>
        public Align DepthPivot
        {
            get { return _depthPivot; }
            set { _depthPivot = value; Node.MarkDirty(); }
        }

        /// <inheritdoc />
        public override Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            FlexalonLog.Log("AlignMeasure | Size", node, size);

            Vector3 maxSize = Vector3.zero;
            foreach (var child in node.Children)
            {
                maxSize = Vector3.Max(maxSize, child.GetMeasureSize(size));
            }

            for (int i = 0; i < 3; i++)
            {
                if (node.GetSizeType(i) == SizeType.Layout)
                {
                    size[i] = maxSize[i];
                }
            }

            size = Math.Clamp(size, min, max);
            SetChildrenFillShrinkSize(node, size, size);
            return new Bounds(Vector3.zero, size);
        }

        /// <inheritdoc />
        public override void Arrange(FlexalonNode node, Vector3 size)
        {
            FlexalonLog.Log("AlignArrange | Size", node, size);

            foreach (var child in node.Children)
            {
                child.SetRotationResult(Quaternion.identity);
                child.SetPositionResult(Math.Align(child.GetArrangeSize(), size,
                    _horizontalAlign, _verticalAlign, _depthAlign,
                    _horizontalPivot, _verticalPivot, _depthPivot));
            }
        }
    }
}