using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// Use a shape layout to position children in a shape formation with a specified number of sides.
    /// The first child is placed in the center, and subsequent children are placed in concentric layers
    /// around the center, with each layer forming the desired shape.
    /// </summary>
    [AddComponentMenu("Flexalon/Flexalon Shape Layout"), HelpURL("https://www.flexalon.com/docs/shapeLayout")]
    public class FlexalonShapeLayout : LayoutBase
    {
        [SerializeField, Min(3)]
        private int _sides = 6;
        /// <summary> Determines how many sides the shape should have. </summary>
        public int Sides
        {
            get => _sides;
            set { _sides = Mathf.Max(value, 3); MarkDirty(); }
        }

        [SerializeField]
        private float _shapeRotationDegrees = 0;
        /// <summary> Rotates the shape around the specified Plane without rotating the children. </summary>
        public float ShapeRotationDegrees
        {
            get => _shapeRotationDegrees;
            set { _shapeRotationDegrees = value; MarkDirty(); }
        }

        [SerializeField]
        private float _spacing = 1f;
        /// <summary> Determines the space between each layer of the shape. </summary>
        public float Spacing
        {
            get => _spacing;
            set { _spacing = value; MarkDirty(); }
        }

        [SerializeField]
        private Plane _plane = Plane.XZ;
        /// <summary> Determines on which plane to create the shape. </summary>
        public Plane Plane
        {
            get => _plane;
            set { _plane = value; MarkDirty(); }
        }

        [SerializeField]
        private Align _planeAlign = Align.Center;
        /// <summary> Determines how each child aligns within the size of the parent
        /// along the axis perpendicular to the Plane </summary>
        public Align PlaneAlign
        {
            get => _planeAlign;
            set { _planeAlign = value; MarkDirty(); }
        }

        private Vector3 _shapeSize;

        /// <inheritdoc />
        public override Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            var sides = Mathf.Max(3, _sides);
            // Derived from Capacity = 1 + (sides) + (2 * sides) + ... + (layers * sides)
            var layers = Mathf.Ceil((Mathf.Sqrt(1 + 8 * (node.Children.Count - 1) / sides) - 1) / 2);
            layers = node.Children.Count > 0 ? Mathf.Max(1, layers) : 0;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var (axis1, axis2) = Math.GetPlaneAxesInt(_plane);
            var axis3 = Math.GetThirdAxis(axis1, axis2);

            var incAngle = (2 * Mathf.PI) / sides;
            var rot = _shapeRotationDegrees * Mathf.PI / 180.0f;
            for (int i = 0; i < sides + 1; i++)
            {
                float angle = i * incAngle + rot;
                var vec = Vector3.zero;
                vec[axis1] = Mathf.Cos(angle);
                vec[axis2] = Mathf.Sin(angle);
                bounds.Encapsulate(vec * _spacing * layers);
            }

            _shapeSize = Vector3.Max(bounds.size, Vector3.one * 0.0001f);

            Vector3 center = Vector3.zero;

            if (node.GetSizeType(axis1) == SizeType.Layout)
            {
                center[axis1] = bounds.center[axis1];
                size[axis1] = bounds.size[axis1];
            }

            if (node.GetSizeType(axis2) == SizeType.Layout)
            {
                center[axis2] = bounds.center[axis2];
                size[axis2] = bounds.size[axis2];
            }

            if (node.GetSizeType(axis3) == SizeType.Layout)
            {
                var maxSize = 0f;
                foreach (var child in node.Children)
                {
                    maxSize = Mathf.Max(maxSize, child.GetMeasureSize(axis3, size[axis3]));
                }

                size[axis3] = maxSize;
            }

            size = Math.Clamp(size, min, max);
            var ratio = Math.Div(size, _shapeSize);
            var fillSize = new Vector3();
            fillSize[axis1] = _spacing * ratio[axis1];
            fillSize[axis2] = _spacing * ratio[axis2];
            fillSize[axis3] = size[axis3];
            FlexalonLog.Log("ShapeMeasure | Size", node, size);
            FlexalonLog.Log("ShapeMeasure | FillSize", node, fillSize);
            FlexalonLog.Log("ShapeMeasure | Spacing", node, _spacing);
            FlexalonLog.Log("ShapeMeasure | Ratio", node, ratio);
            SetChildrenFillShrinkSize(node, fillSize, size);
            return new Bounds(center, size);
        }

        /// <inheritdoc />
        public override void Arrange(FlexalonNode node, Vector3 layoutSize)
        {
            var (axis1, axis2) = Math.GetPlaneAxesInt(_plane);
            var axis3 = Math.GetThirdAxis(axis1, axis2);
            var planeVector = new Vector3();
            planeVector[axis3] = 1;

            // Place first child in the center
            node.Children[0].SetPositionResult(planeVector * Math.Align(node.Children[0].GetArrangeSize(), layoutSize, axis3, _planeAlign));
            node.Children[0].SetRotationResult(Quaternion.identity);

            // Place the rest of the children in hex patterns around the center.
            var sides = Mathf.Max(3, _sides);

            // First, compute some directions to each point of a shape.
            var incAngle = (2 * Mathf.PI) / sides;
            var rot = _shapeRotationDegrees * Mathf.PI / 180.0f;
            List<Vector3> directions = new List<Vector3>(sides + 1);
            for (int i = 0; i < sides + 1; i++)
            {
                float angle = i * incAngle + rot;
                var direction = new Vector3();
                direction[axis1] = Mathf.Cos(angle);
                direction[axis2] = Mathf.Sin(angle);
                directions.Add(direction);
            }

            var ratio = Math.Div(layoutSize, _shapeSize);
            ratio.y = 1;

            // Iterate around the shape placing children.
            // 'side' represents the side of the shape on which we're placing children.
            // 'layer' is how big of a shape we're making. Each time we run out of space
            // we move to the next layer and start a new shape.
            // 'placed' is the number of objects we've already placed.
            int side = 0;
            int layer = 1;
            int placed = 1;
            while (placed < node.Children.Count)
            {
                var p0 = directions[side] * _spacing * layer;
                var p1 = directions[side + 1] * _spacing * layer;

                PositionChild(node.Children[placed], layoutSize, p0, axis3, ratio);
                placed++;

                // Place children between the two points on the shape.
                for (int i = 1; placed < node.Children.Count && i < layer; i++)
                {
                    var p = Vector3.Lerp(p0, p1, ((float) i) / layer);
                    PositionChild(node.Children[placed], layoutSize, p, axis3, ratio);
                    placed++;
                }

                side++;
                if (side == sides)
                {
                    side = 0;
                    layer++;
                }
            }
        }

        private void PositionChild(FlexalonNode child, Vector3 layoutSize, Vector3 shapePosition, int axis3, Vector3 scale)
        {
            var position = Math.Mul(shapePosition, scale);
            position[axis3] = Math.Align(child.GetArrangeSize(), layoutSize, axis3, _planeAlign);
            child.SetPositionResult(position);
            child.SetRotationResult(Quaternion.identity);
        }
    }
}