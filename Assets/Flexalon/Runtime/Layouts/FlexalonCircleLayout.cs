using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Flexalon
{
    /// <summary> Use a circle layout to position children along a circle or spiral. </summary>
    [AddComponentMenu("Flexalon/Flexalon Circle Layout"), HelpURL("https://www.flexalon.com/docs/circleLayout")]
    public class FlexalonCircleLayout : LayoutBase
    {
        [SerializeField]
        private Plane _plane = Plane.XZ;
        /// <summary> Determines on which plane to create the circle. </summary>
        public Plane Plane
        {
            get => _plane;
            set { _plane = value; MarkDirty(); }
        }

        [SerializeField]
        private float _radius = 1;
        /// <summary> Initial radius of the circle. </summary>
        public float Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                _initialRadius = InitialRadiusOptions.Fixed;
                Node.MarkDirty();
            }
        }

        [SerializeField, Obsolete("Use Initial Radius instead")]
        private bool _useWidth = false;

        /// <summary> Determines the initial radius of the circle. </summary>
        public enum InitialRadiusOptions
        {
            /// <summary> The initial radius is a fixed value. </summary>
            Fixed,

            /// <summary> The initial radius is half the size of the layout on the first plane axis. </summary>
            HalfAxis1,

            /// <summary> The initial radius is half the size of the layout on the other plane axis. </summary>
            HalfAxis2,

            /// <summary> The initial radius is half the size of the layout on the smaller plane axis.</summary>
            HalfMinAxis,

            /// <summary> The initial radius is half the size of the layout on the larger plane axis.</summary>
            HalfMaxAxis,
        }

        [SerializeField]
        private InitialRadiusOptions _initialRadius;
        /// <summary> Determines the initial radius of the circle. </summary>
        public InitialRadiusOptions InitialRadius
        {
            get { return _initialRadius; }
            set { _initialRadius = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private bool _spiral = false;
        /// <summary> If checked, positions each object at increasing heights to form a spiral. </summary>
        public bool Spiral
        {
            get { return _spiral; }
            set { _spiral = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private float _spiralSpacing = 0;
        /// <summary> Vertical spacing between objects in the spiral. </summary>
        public float SpiralSpacing
        {
            get { return _spiralSpacing; }
            set { _spiralSpacing = value; Node.MarkDirty(); }
        }

        /// <summary> Determines how the space between children is distributed. </summary>
        public enum SpacingOptions
        {
            /// <summary> The Spacing Degrees property determines the space between children. </summary>
            Fixed,

            /// <summary> The space around the circle is distributed between children. </summary>
            Evenly
        }

        [SerializeField]
        private SpacingOptions _spacingType;
        /// <summary> Determines how the space between children is distributed. </summary>
        public SpacingOptions SpacingType
        {
            get { return _spacingType; }
            set { _spacingType = value; Node.MarkDirty(); }
        }

        [SerializeField]
        /// <summary> Radial space between children when SpacingType is Fixed. </summary>
        private float _spacingDegrees = 30.0f;
        public float SpacingDegrees
        {
            get { return _spacingDegrees; }
            set { _spacingDegrees = value; Node.MarkDirty(); }
        }

        /// <summary> Determines if and how the radius changes. </summary>
        public enum RadiusOptions
        {
            /// <summary> The radius does not change. </summary>
            Constant,

            /// <summary> The radius is incremented for each child by the Radius Step property.
            /// This can be used to create an inward or outward spiral. </summary>
            Step,

            /// <summary> If set to Wrap, the radius is incremented each time around the circle.
            /// This can be used to create concentric circles of objects. </summary>
            Wrap
        }

        [SerializeField]
        private RadiusOptions _radiusType = RadiusOptions.Constant;
        /// <summary> Determines if and how the radius changes. </summary>
        public RadiusOptions RadiusType
        {
            get { return _radiusType; }
            set { _radiusType = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private float _radiusStep = 0.1f;
        /// <summary> Determines how much the radius should change at each interval. </summary>
        public float RadiusStep
        {
            get { return _radiusStep; }
            set { _radiusStep = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private float _startAtDegrees = 0.0f;
        /// <summary> By default, the first child will be placed at (radius, 0, 0).
        /// Start At Degrees value will add an offset all children around the circle. </summary>
        public float StartAtDegrees
        {
            get { return _startAtDegrees; }
            set { _startAtDegrees = value; Node.MarkDirty(); }
        }

        /// <summary> Determines how children should be rotated. </summary>
        public enum RotateOptions
        {
            /// <summary> Child rotation is set to zero. </summary>
            None,

            /// <summary> Children face out of the circle. </summary>
            Out,

            /// <summary> Children face into the circle. </summary>
            In,

            /// <summary> Children face forward along the circle. </summary>
            Forward,

            /// <summary> Children face backward along the circle. </summary>
            Backward
        }

        [SerializeField]
        private RotateOptions _rotate = RotateOptions.None;
        /// <summary> Determines how children should be rotated. </summary>
        public RotateOptions Rotate
        {
            get { return _rotate; }
            set { _rotate = value; Node.MarkDirty(); }
        }

        [SerializeField, FormerlySerializedAs("_verticalAlign")]
        private Align _planeAlign = Align.Center;
        /// <summary> Aligns the layout with the size set by the Flexalon Object Component.
        /// For a circle, this will align each individual object in the layout. For a spiral,
        /// this will align the entire spiral. </summary>
        public Align PlaneAlign
        {
            get { return _planeAlign; }
            set { _planeAlign = value; Node.MarkDirty(); }
        }

        private float GetSpacing(FlexalonNode node)
        {
            var spacing = _spacingDegrees * Mathf.PI / 180;
            if (_spacingType == SpacingOptions.Evenly)
            {
                if (node.Children.Count < 2)
                {
                    spacing = 0;
                }
                else
                {
                    spacing = 2 * Mathf.PI / node.Children.Count;
                }
            }
            return spacing;
        }

        private float GetRadius(Vector3 layoutSize, int axis1, int axis2)
        {
            switch (_initialRadius)
            {
                case InitialRadiusOptions.Fixed:
                    return _radius;
                case InitialRadiusOptions.HalfAxis1:
                    return layoutSize[axis1] / 2;
                case InitialRadiusOptions.HalfAxis2:
                    return layoutSize[axis2] / 2;
                case InitialRadiusOptions.HalfMinAxis:
                    return Mathf.Min(layoutSize[axis1], layoutSize[axis2]) / 2;
                case InitialRadiusOptions.HalfMaxAxis:
                    return Mathf.Max(layoutSize[axis1], layoutSize[axis2]) / 2;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float MeasureHeight(FlexalonNode node, int heightAxis, Vector3 size)
        {
            if (!_spiral)
            {
                var maxHeight = 0f;
                foreach (var child in node.Children)
                {
                    maxHeight = Mathf.Max(maxHeight, child.GetMeasureSize(heightAxis, size[heightAxis]));
                }

                return maxHeight;
            }

            if (node.Children.Count == 0)
            {
                return 0;
            }

            var totalHeight = _spiralSpacing * (node.Children.Count - 1);
            foreach (var child in node.Children)
            {
                totalHeight += child.GetMeasureSize(heightAxis, size[heightAxis]);
            }

            return totalHeight;
        }

        private float MeasureDiameter(FlexalonNode node, Vector3 size, int circleAxis1, float spacing)
        {
            var diameter = _radius * 2;
            if (_initialRadius != InitialRadiusOptions.Fixed)
            {
                float maxChildSize = 0;
                foreach (var child in node.Children)
                {
                    var childSize = child.GetMeasureSize(size);
                    maxChildSize = Mathf.Max(maxChildSize, childSize[circleAxis1]);
                }

                diameter = maxChildSize / Mathf.Tan(spacing / 2);
            }

            if (_radiusType == RadiusOptions.Step)
            {
                var radius = diameter / 2;
                var stepRadius = Mathf.Abs(radius + _radiusStep * (node.Children.Count - 1));
                if (stepRadius > Mathf.Abs(radius))
                {
                    diameter = stepRadius * 2;
                }
            }
            else if (_radiusType == RadiusOptions.Wrap)
            {
                var startAt = _startAtDegrees * Mathf.PI / 180;
                var lastAngle = (node.Children.Count - 1) * spacing + startAt;
                float timesAroundCircle = Mathf.Floor(lastAngle / (Mathf.PI * 2));
                var radius = diameter / 2;
                var wrapRadius = Mathf.Abs(radius + timesAroundCircle * _radiusStep);
                if (wrapRadius > Mathf.Abs(radius))
                {
                    diameter = wrapRadius * 2;
                }
            }

            return diameter;
        }

        public void ShrinkFillChildrenToDiameter(FlexalonNode node, float spacing, int circleAxis1, int circleAxis2, Vector3 size)
        {
            var childSize1 = (node.Children.Count <= 2 && _spacingType == SpacingOptions.Evenly) ? 1 :
                size[circleAxis1] * Mathf.Tan(spacing / 2);

            var childSize2 = (node.Children.Count <= 2 && _spacingType == SpacingOptions.Evenly) ? 1 :
                size[circleAxis2] * Mathf.Tan(spacing / 2);

            foreach (var child in node.Children)
            {
                child.SetShrinkFillSize(circleAxis1, childSize1, size[circleAxis1]);
                child.SetShrinkFillSize(circleAxis2, childSize2, size[circleAxis2]);
            }
        }

        public void ShrinkFillChildrenToCircleHeight(FlexalonNode node, float height, int heightAxis)
        {
            foreach (var child in node.Children)
            {
                child.SetShrinkFillSize(heightAxis, height, height);
            }
        }

        private static List<FlexItem> _flexItems = new List<FlexItem>();

        public void ShrinkFillChildrenToSpiralHeight(FlexalonNode node, float height, int heightAxis, float size)
        {
            var remainingSpace = size - height;
            if (Mathf.Abs(remainingSpace) <= 1e-6f)
            {
                return;
            }

            _flexItems.Clear();
            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                _flexItems.Add(Flex.CreateFlexItem(
                    child, heightAxis, child.GetMeasureSize(heightAxis, size), height, size));
            }

            Flex.GrowOrShrink(_flexItems, height, size, _spiralSpacing);

            for (int i = 0; i < node.Children.Count; i++)
            {
                node.Children[i].SetShrinkFillSize(heightAxis, _flexItems[i].FinalSize, size, true);
            }
        }

        /// <inheritdoc />
        public override Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            FlexalonLog.Log("CircleMeasure", node, size, min, max);

            var spacing = GetSpacing(node);
            var (circleAxis1, circleAxis2) = Math.GetPlaneAxesInt(_plane);
            var heightAxis = (int)Math.GetThirdAxis(circleAxis1, circleAxis2);

            var diameter = MeasureDiameter(node, size, circleAxis1, spacing);
            FlexalonLog.Log("CircleMeasure | diameter 1", node, diameter);

            if (node.GetSizeType(circleAxis1) == SizeType.Layout)
            {
                size[circleAxis1] = Mathf.Clamp(diameter, min[circleAxis1], max[circleAxis1]);
            }

            if (node.GetSizeType(circleAxis2) == SizeType.Layout)
            {
                size[circleAxis2] = Mathf.Clamp(diameter, min[circleAxis2], max[circleAxis2]);
            }

            // Recompute diameter based on InitialRadiusType
            diameter = GetRadius(size, circleAxis1, circleAxis2) * 2;
            FlexalonLog.Log("CircleMeasure | diameter 2", node, diameter);

            // Recompute size based on this updated diameter.
            if (node.GetSizeType(circleAxis1) == SizeType.Layout)
            {
                size[circleAxis1] = Mathf.Clamp(diameter, min[circleAxis1], max[circleAxis1]);
            }

            if (node.GetSizeType(circleAxis2) == SizeType.Layout)
            {
                size[circleAxis2] = Mathf.Clamp(diameter, min[circleAxis2], max[circleAxis2]);
            }

            var height = MeasureHeight(node, heightAxis, size);
            height = Mathf.Clamp(height, min[heightAxis], max[heightAxis]);
            FlexalonLog.Log("CircleMeasure | height", node, height);

            if (node.GetSizeType(heightAxis) == SizeType.Layout)
            {
                size[heightAxis] = height;
            }

            ShrinkFillChildrenToDiameter(node, spacing, circleAxis1, circleAxis2, size);

            if (_spiral)
            {
                ShrinkFillChildrenToSpiralHeight(node, height, heightAxis, size[heightAxis]);
            }
            else
            {
                ShrinkFillChildrenToCircleHeight(node, size[heightAxis], heightAxis);
            }

            return new Bounds(Vector3.zero, size);
        }

        /// <inheritdoc />
        public override void Arrange(FlexalonNode node, Vector3 layoutSize)
        {
            FlexalonLog.Log("CircleArrange | LayoutSize", node, layoutSize);

            var (circleAxis1, circleAxis2) = Math.GetPlaneAxesInt(_plane);
            var heightAxis = (int)Math.GetThirdAxis(circleAxis1, circleAxis2);
            var startAt = _startAtDegrees * Mathf.PI / 180;
            var spacing = GetSpacing(node);
            var startRadius = GetRadius(layoutSize, circleAxis1, circleAxis2);

            var spiralHeight = _spiralSpacing * (node.Children.Count - 1);
            float minSpiralHeight = 0;
            foreach (var child in node.Children)
            {
                float childHeight = child.GetArrangeSize()[heightAxis];
                spiralHeight += childHeight;
                minSpiralHeight = Mathf.Max(minSpiralHeight, childHeight);
            }

            spiralHeight = Mathf.Max(spiralHeight, minSpiralHeight);

            float spiralStart = Math.Align(spiralHeight, layoutSize[heightAxis], _planeAlign) - spiralHeight / 2;
            float spiralPos = spiralStart;
            var rotationAxis = _plane == Plane.XZ ? Vector3.up : _plane == Plane.XY ? -Vector3.forward : Vector3.right;

            int forwardAxis;
            switch (_rotate)
            {
                case RotateOptions.None:
                    forwardAxis = circleAxis1;
                    break;
                case RotateOptions.Forward:
                    forwardAxis = circleAxis2;
                    break;
                case RotateOptions.Backward:
                    forwardAxis = circleAxis2;
                    break;
                case RotateOptions.In:
                    forwardAxis = circleAxis1;
                    break;
                case RotateOptions.Out:
                    forwardAxis = circleAxis1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                var angle = i * spacing + startAt;
                var child = node.Children[i];
                var childSize = child.GetArrangeSize();

                float radius = startRadius;
                if (_radiusType == RadiusOptions.Step)
                {
                    radius += i * _radiusStep;
                }
                else if (_radiusType == RadiusOptions.Wrap)
                {
                    float timesAroundCircle = Mathf.Floor(angle / (Mathf.PI * 2));
                    radius += timesAroundCircle * _radiusStep;
                }

                var pos = Vector3.zero;
                pos[circleAxis1] = radius * Mathf.Cos(angle);
                pos[circleAxis2] = radius * Mathf.Sin(angle);

                if (_spiral)
                {
                    pos[heightAxis] = spiralPos + childSize[heightAxis] * 0.5f;
                    spiralPos += childSize[heightAxis] + _spiralSpacing;
                    spiralPos = Mathf.Max(spiralStart, spiralPos);
                }
                else
                {
                    pos[heightAxis] = Math.Align(childSize, layoutSize, heightAxis, _planeAlign);
                }

                child.SetPositionResult(pos);

                float rotation = -angle;
                switch (_rotate)
                {
                    case RotateOptions.None:
                        rotation = 0;
                        break;
                    case RotateOptions.Forward:
                        break;
                    case RotateOptions.Backward:
                        rotation += Mathf.PI;
                        break;
                    case RotateOptions.In:
                        rotation -= Mathf.PI * 0.5f;
                        break;
                    case RotateOptions.Out:
                        rotation += Mathf.PI * 0.5f;
                        break;
                }

                var q = Quaternion.AngleAxis(rotation * 180.0f / Mathf.PI, rotationAxis);
                child.SetRotationResult(q);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (Node != null)
            {
                var (circleAxis1, circleAxis2) = Math.GetPlaneAxesInt(_plane);

                // Draw a semitransparent circle at the transforms position
                Gizmos.color = new Color(1, 1, 0, 0.5f);
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                int segments = 30;
                var scale = Node.GetWorldBoxScale(true);

                var radius = GetRadius(Node.Result.AdapterBounds.size, circleAxis1, circleAxis2);

                for (int i = 0; i < segments; i++)
                {
                    var a1 = Mathf.PI * 2 * (i / (float)segments);
                    var a2 = Mathf.PI * 2 * ((i + 1) / (float)segments);
                    var p1 = Vector3.zero;
                    var p2 = Vector3.zero;
                    p1[circleAxis1] = radius * Mathf.Cos(a1);
                    p1[circleAxis2] = radius * Mathf.Sin(a1);
                    p2[circleAxis1] = radius * Mathf.Cos(a2);
                    p2[circleAxis2] = radius * Mathf.Sin(a2);
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (transform is RectTransform || (transform.parent && transform.parent is RectTransform))
            {
                _plane = Plane.XY;
                _radius = 100;
            }
        }

        protected override void Upgrade(int fromVersion)
        {
            if (fromVersion < 4)
            {
                #pragma warning disable 618
                if (_useWidth)
                {
                    _initialRadius = InitialRadiusOptions.HalfAxis1;
                }
                #pragma warning restore 618

                if (_rotate == RotateOptions.In)
                {
                    _rotate = RotateOptions.Out;
                }
                else if (_rotate == RotateOptions.Out)
                {
                    _rotate = RotateOptions.In;
                }
            }
        }
    }
}