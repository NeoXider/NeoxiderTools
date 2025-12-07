using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// Use a curve layout to position children along a bézier curve.
    /// </summary>
    [AddComponentMenu("Flexalon/Flexalon Curve Layout"), HelpURL("https://www.flexalon.com/docs/curveLayout")]
    public class FlexalonCurveLayout : LayoutBase
    {
        /// <summary> Determines how the tangent for a CurvePoint is determined. </summary>
        public enum TangentMode
        {
            /// <summary> Define the tangent by entering a value or dragging the handle in the scene window. </summary>
            Manual,

            /// <summary> Sets the tangent to match the tangent at the previous point. </summary>
            MatchPrevious,

            /// <summary> Sets the tangent to zero to create a sharp corner. </summary>
            Corner,

            /// <summary> Computes a tangent that will create a smooth curve between the previous and next points. </summary>
            Smooth,
        }

        /// <summary> A point on the curve. </summary>
        [System.Serializable]
        public struct CurvePoint
        {
            /// <summary> The position of the point. </summary>
            public Vector3 Position;

            /// <summary> Determines how the tangent for this point is determined. </summary>
            public TangentMode TangentMode;

            /// <summary> The tangent of the point. </summary>
            public Vector3 Tangent;

            /// <summary> Returns a copy of this CurvePoint with a different position. </summary>
            public CurvePoint ChangePosition(Vector3 position)
            {
                var copy = Copy();
                copy.Position = position;
                return copy;
            }

            /// <summary> Returns a copy of this CurvePoint with a different tangent. </summary>
            public CurvePoint ChangeTangent(Vector3 tangent)
            {
                var copy = Copy();
                copy.Tangent = tangent;
                return copy;
            }

            /// <summary> Returns a copy of this CurvePoint. </summary>
            public CurvePoint Copy()
            {
                return new CurvePoint {
                    Position = Position,
                    TangentMode = TangentMode,
                    Tangent = Tangent,
                };
            }
        }

        [SerializeField, HideInInspector]
        private List<CurvePoint> _points;

        /// <summary> The points that define the curve. </summary>
        public IReadOnlyList<CurvePoint> Points => _points;

        /// <summary> Determines how the children will be spaced along the curve. </summary>
        public enum SpacingOptions
        {
            /// <summary> Define the distance between each child with the "Spacing" property. </summary>
            Fixed,

            /// <summary> The first child is placed at the beginning of the curve and the last child is placed
            /// at the end of the curve. The rest of the children are placed at even distances between
            /// these points along the curve. </summary>
            Evenly,

            /// <summary> If the beginning of the curve is connected to the end of the curve, then the first
            /// child is placed at the beginning/end of the curve, and the rest of the children are placed
            /// at even distances along the curve. </summary>
            EvenlyConnected,
        }

        [SerializeField]
        private bool _lockTangents = false;
        /// <summary> Prevents the tangent handles from appearing in the editor. </summary>
        public bool LockTangents
        {
            get => _lockTangents;
            set { _lockTangents = value; }
        }

        [SerializeField]
        private bool _lockPositions = false;
        /// <summary> Prevents the position handles from appearing in the editor. </summary>
        public bool LockPositions
        {
            get => _lockPositions;
            set { _lockPositions = value; }
        }

        [SerializeField]
        private SpacingOptions _spacingType;
        /// <summary> Determines how the children will be spaced along the curve. </summary>
        public SpacingOptions SpacingType
        {
            get { return _spacingType; }
            set { _spacingType = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private float _spacing = 0.5f;
        /// <summary> Determines the fixed distance between children. </summary>
        public float Spacing
        {
            get { return _spacing; }
            set { _spacing = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private float _startAt = 0.0f;
        /// <summary> Offsets all objects along the curve. </summary>
        public float StartAt
        {
            get { return _startAt; }
            set { _startAt = value; Node.MarkDirty(); }
        }

        /// <summary> Determines how the curve is extended before the beginning and after the end. </summary>
        public enum ExtendBehavior
        {
            /// <summary> Do not extend the curve. All objects before the beginning are placed at the start, and all objects after the end are placed at the end. </summary>
            Stop,

            /// <summary> Extend the curve by continuing in the opposite direction. </summary>
            PingPong,

            /// <summary> Extend the curve in a straight line based on the tangent at the start/end of the curve. </summary>
            ExtendLine,

            /// <summary> Extend the curve by repeating the curve. </summary>
            Repeat,

            /// <summary> Extend the curve by mirroring the curve and repeating it. </summary>
            RepeatMirror
        }

        [SerializeField]
        private ExtendBehavior _beforeStart = 0.0f;
        /// <summary> Offsets all objects along the curve. </summary>
        public ExtendBehavior BeforeStart
        {
            get { return _beforeStart; }
            set { _beforeStart = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private ExtendBehavior _afterEnd = 0.0f;
        /// <summary> Offsets all objects along the curve. </summary>
        public ExtendBehavior AfterEnd
        {
            get { return _afterEnd; }
            set { _afterEnd = value; Node.MarkDirty(); }
        }

        /// <summary> Determines how children should be rotated </summary>
        public enum RotationOptions
        {
            /// <summary> Sets all child rotations to zero. </summary>
            None,

            /// <summary> Each child is rotated to the right of the forward direction of the curve. </summary>
            In,

            /// <summary> Each child is rotated to the left of the forward direction of the curve. </summary>
            Out,

            /// <summary> Each child is rotated to the right of the forward direction of the curve
            /// and rolled so that the X axis matches the curve backward direction. </summary>
            InWithRoll,

            /// <summary> Each child is rotated to the left of the forward direction of the curve
            /// and rolled so that the X axis matches the curve forward direction. </summary>
            OutWithRoll,

            /// <summary> Each child is rotated to face forward along the curve. </summary>
            Forward,

            /// <summary> Each child is rotated to face backward along the curve. </summary>
            Backward
        }

        [SerializeField]
        private RotationOptions _rotation;
        /// <summary> Determines how children should be rotated </summary>
        public RotationOptions Rotation
        {
            get { return _rotation; }
            set { _rotation = value; Node.MarkDirty(); }
        }

        /// <summary> Adds a new point to the end of the curve. </summary>
        /// <param name="point"> The point to add. </param>
        public void AddPoint(CurvePoint point)
        {
            _points.Add(point);
            MarkDirty();
        }

        /// <summary> Adds a new point to the end of the curve. </summary>
        /// <param name="position"> The position of the point. </param>
        /// <param name="tangent"> The tangent of the point. </param>
        public void AddPoint(Vector3 position, Vector3 tangent)
        {
            AddPoint(new CurvePoint{ Position = position, Tangent = tangent, TangentMode = TangentMode.Manual });
        }

        /// <summary> Inserts a new point into the curve at the specified index. </summary>
        /// <param name="index"> The index to insert the point at. </param>
        /// <param name="point"> The point to insert. </param>
        public void InsertPoint(int index, CurvePoint point)
        {
            _points.Insert(index, point);
            MarkDirty();
        }

        /// <summary> Inserts a new point into the curve at the specified index. </summary>
        /// <param name="index"> The index to insert the point at. </param>
        /// <param name="position"> The position of the point. </param>
        /// <param name="tangent"> The tangent of the point. </param>
        public void InsertPoint(int index, Vector3 position, Vector3 tangent)
        {
            InsertPoint(index, new CurvePoint{ Position = position, Tangent = tangent, TangentMode = TangentMode.Manual });
        }

        /// <summary> Replaces the point at the index with a new point. </summary>
        /// <param name="index"> The index of the point to replace. </param>
        /// <param name="point"> The new point. </param>
        public void ReplacePoint(int index, CurvePoint point)
        {
            _points.RemoveAt(index);
            InsertPoint(index, point);
        }

        /// <summary> Replaces the point at the index with a new point. </summary>
        /// <param name="index"> The index of the point to replace. </param>
        /// <param name="position"> The position of the point. </param>
        /// <param name="tangent"> The tangent of the point. </param>
        public void ReplacePoint(int index, Vector3 position, Vector3 tangent)
        {
            ReplacePoint(index, new CurvePoint{ Position = position, Tangent = tangent, TangentMode = TangentMode.Manual });
        }

        /// <summary> Removes the point at the index. </summary>
        /// <param name="index"> The index of the point to remove. </param>
        public void RemovePoint(int index)
        {
            _points.RemoveAt(index);
            MarkDirty();
        }

        /// <summary> Replaces all points of the curve. </summary>
        /// <param name="points"> The new points. </param>
        public void SetPoints(List<CurvePoint> points)
        {
            _points = points;
            MarkDirty();
        }

        [SerializeField, HideInInspector]
        private List<Vector3> _curvePositions = new List<Vector3>();

        /// <summary> Points along the curve used to position objects and can be used for visualization. </summary>
        public IReadOnlyList<Vector3> CurvePositions => _curvePositions;

        [SerializeField, HideInInspector]
        private List<Vector3> _upVectors = new List<Vector3>();

        [SerializeField, HideInInspector]
        private float _curveLength = 0;

        /// <summary> The length of the curve. </summary>
        public float CurveLength => _curveLength;

        private List<float> _curveLengths = new List<float>();
        private Bounds _curveBounds;
        private List<CurvePoint> _computedPoints = new List<CurvePoint>();

        private void UpdateCurvePositions()
        {
            if (_computedPoints.Count == _points.Count && _upVectors.Count == _points.Count)
            {
                bool curveChanged = false;
                for (int i = 0; i < _points.Count; i++)
                {
                    if (_computedPoints[i].Position != _points[i].Position || _computedPoints[i].Tangent != _points[i].Tangent)
                    {
                        curveChanged = true;
                        break;
                    }
                }

                if (!curveChanged)
                {
                    return;
                }
            }

            _curvePositions.Clear();
            _upVectors.Clear();
            _curveLengths.Clear();
            _curveLength = 0;
            _computedPoints.Clear();
            _computedPoints.AddRange(_points);

            if (_points.Count == 0)
            {
                _curveBounds = new Bounds();
                return;
            }

            _curvePositions.Add(_points[0].Position);
            _upVectors.Add(Vector3.up);
            _curveLengths.Add(0);
            _curveBounds = new Bounds(_points[0].Position, Vector3.zero);
            var prev = _points[0].Position;
            for (int i = 1; i < _points.Count; i++)
            {
                for (int j = 1; j <= 100; j++)
                {
                    var pos = ComputePositionOnBezierCurve(_points[i - 1], _points[i], (float) j / 100);
                    var len = Vector3.Distance(prev, pos);
                    _curvePositions.Add(pos);
                    var fwd = (pos - prev).normalized;
                    var right = Vector3.Cross(fwd, _upVectors[_upVectors.Count - 1]).normalized;
                    _upVectors.Add(Vector3.Cross(right, fwd));
                    _curveLength += len;
                    _curveLengths.Add(_curveLength);
                    _curveBounds.Encapsulate(pos);
                    prev = pos;
                }
            }

            var fwd0 = (_curvePositions[1] - _curvePositions[0]).normalized;
            var right0 = Vector3.Cross(fwd0, _upVectors[1]).normalized;
            _upVectors[0] = Vector3.Cross(right0, fwd0);
        }

        private (Vector3, Vector3, Vector3) GetCurvePositionAndForwardAtClampedDistance(float distance)
        {
            if (distance <= 0)
            {
                var tan = _points[0].Tangent == Vector3.zero ?
                    (_curvePositions[1] - _curvePositions[0]) :
                    _points[0].Tangent;
                return (_points[0].Position, tan.normalized, _upVectors[0]);
            }

            if (distance >= _curveLength)
            {
                var tan = _points[_points.Count - 1].Tangent == Vector3.zero ?
                    (_curvePositions[_curvePositions.Count - 1] - _curvePositions[_curvePositions.Count - 2]) :
                    _points[_points.Count - 1].Tangent;
                return (_points[_points.Count - 1].Position, tan.normalized, _upVectors[_upVectors.Count - 1]);
            }

            int s = 0;
            int e = _curvePositions.Count - 1;

            int i = 0;
            while (s != e && i < 100)
            {
                i++;
                var m = s + (e - s) / 2;
                if (_curveLengths[m] <= distance)
                {
                    s = m + 1;
                }
                else
                {
                    e = m;
                }
            }

            // We should be at the next position after distance.
            var distanceBetweenPoints = _curveLengths[e] - _curveLengths[e - 1];
            var t = distanceBetweenPoints > 0 ? (distance - _curveLengths[e - 1]) / distanceBetweenPoints : 1;
            var p = Vector3.Lerp(_curvePositions[e - 1], _curvePositions[e], t);
            var f = _curvePositions[e] - _curvePositions[e - 1];
            var up = _upVectors[e];
            return (p, f.normalized, up);
        }

        private (Vector3, Vector3, Vector3) GetPongPositionAndForwardAtDistance(float distance)
        {
            if (distance < 0)
            {
                distance = -distance;
            }

            distance = distance % (_curveLength * 2);

            if (distance > _curveLength)
            {
                distance = 2 * _curveLength - distance;
            }

            return GetCurvePositionAndForwardAtClampedDistance(distance);
        }

        private (Vector3, Vector3, Vector3) GetExtendLinePositionAndForwardAtDistance(float distance)
        {
            float reference = distance < 0 ? 0 : _curveLength;
            var (pos, fwd, up) = GetCurvePositionAndForwardAtClampedDistance(reference);
            return (pos + fwd * (distance - reference), fwd, up);
        }

        private (Vector3, Vector3, Vector3) GetRepeatPositionAndForwardAtDistance(float distance)
        {
            var startPos = _points[0].Position;
            var endPos = _points[_points.Count - 1].Position;
            var iterations = Mathf.Floor(distance / _curveLength);
            var offset = iterations * (endPos - startPos);
            distance = distance % _curveLength;

            if (distance < 0)
            {
                distance = _curveLength + distance;
            }

            var (pos, tan, up) = GetCurvePositionAndForwardAtClampedDistance(distance);
            return (pos + offset, tan, up);
        }

        private (Vector3, Vector3, Vector3) GetRepeatMirrorPositionAndForwardAtDistance(float distance)
        {
            var startPos = _points[0].Position;
            var endPos = _points[_points.Count - 1].Position;
            var iterations = Mathf.Floor(distance / _curveLength);
            float mirror = iterations % 2 == 0 ? 1 : -1;
            var (pos, tan, up) = GetPongPositionAndForwardAtDistance(distance);

            var evenIterations = Mathf.Floor(Mathf.Abs(iterations) / 2);
            var oddIterations = Mathf.Floor((Mathf.Abs(iterations) + 1) / 2);

            Vector3 offset = Vector3.zero;
            if (iterations < 0)
            {
                offset = oddIterations * 2 * startPos - evenIterations * 2 * endPos;
            }

            if (iterations > 0)
            {
                offset = oddIterations * 2 * endPos - evenIterations * 2 * startPos;
            }

            return (mirror * pos + offset, tan, up);
        }

        private (Vector3, Vector3, Vector3) GetExtendPositionAndForwardAtDistance(float distance, ExtendBehavior behavior)
        {
            switch (behavior)
            {
                case ExtendBehavior.PingPong:
                    return GetPongPositionAndForwardAtDistance(distance);
                case ExtendBehavior.ExtendLine:
                    return GetExtendLinePositionAndForwardAtDistance(distance);
                case ExtendBehavior.Repeat:
                    return GetRepeatPositionAndForwardAtDistance(distance);
                case ExtendBehavior.RepeatMirror:
                    return GetRepeatMirrorPositionAndForwardAtDistance(distance);
                default:
                    return GetCurvePositionAndForwardAtClampedDistance(distance);
            }
        }

        private (Vector3, Vector3, Vector3) GetCurvePositionAndForwardAtDistance(float distance)
        {
            // Assumes > 1 points

            bool reverse = false;

            if (distance < 0 && _beforeStart == ExtendBehavior.PingPong)
            {
                distance = -distance;
                reverse = _afterEnd == ExtendBehavior.PingPong ? Mathf.Floor(distance / _curveLength) % 2 == 0 : true;
            }
            else if (distance > _curveLength && _afterEnd == ExtendBehavior.PingPong)
            {
                reverse = _beforeStart == ExtendBehavior.PingPong ? Mathf.Floor(distance / _curveLength) % 2 == 1 : true;
                distance = _curveLength - (distance - _curveLength);
            }

            (Vector3, Vector3, Vector3) result;

            if (distance < 0)
            {
                result = GetExtendPositionAndForwardAtDistance(distance, _beforeStart);
            }
            else if (distance > _curveLength)
            {
                result = GetExtendPositionAndForwardAtDistance(distance, _afterEnd);
            }
            else
            {
                result = GetCurvePositionAndForwardAtClampedDistance(distance);
            }

            return (result.Item1, reverse ? -result.Item2 : result.Item2, result.Item3);
        }

        private void UpdateTangents()
        {
            for (int i = 0; i < _points.Count; i++)
            {
                if (_points[i].TangentMode == TangentMode.MatchPrevious && i > 0)
                {
                    _points[i] = _points[i].ChangeTangent(_points[i - 1].Tangent);
                }
                else if (_points[i].TangentMode == TangentMode.Corner)
                {
                    _points[i] = _points[i].ChangeTangent(Vector3.zero);
                }
                else if (_points[i].TangentMode == TangentMode.Smooth)
                {
                    Vector3 tangent = Vector3.zero;
                    if (i > 0)
                    {
                        if (i < _points.Count - 1)
                        {
                            var v1 = _points[i + 1].Position - _points[i].Position;
                            var v2 = _points[i].Position - _points[i - 1].Position;
                            tangent = (v1 + v2) / 4;
                        }
                        else
                        {
                            tangent = (_points[i].Position - _points[i - 1].Position) / 4;
                        }
                    }
                    else
                    {
                        if (_points.Count > 1)
                        {
                            tangent = (_points[i + 1].Position - _points[i].Position) / 4;
                        }
                    }

                    _points[i] = _points[i].ChangeTangent(tangent);
                }
            }
        }

        /// <inheritdoc />
        public override Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            FlexalonLog.Log("CurveMeasure | Size", node, size);

            UpdateTangents();
            UpdateCurvePositions();

            Vector3 center = Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                if (node.GetSizeType(i) == SizeType.Layout)
                {
                    center[i] = _curveBounds.center[i];
                    size[i] = _curveBounds.size[i];
                }
            }

            size = Math.Clamp(size, min, max);
            var childFillSizeXZ = _curveLength / node.Children.Count;
            var childFillSize = new Vector3(childFillSizeXZ, size.y, childFillSizeXZ);
            SetChildrenFillShrinkSize(node, childFillSize, size);
            return new Bounds(center, size);
        }

        /// <inheritdoc />
        public override void Arrange(FlexalonNode node, Vector3 layoutSize)
        {
            FlexalonLog.Log("CurveArrange | LayoutSize", node, layoutSize);

            if (node.Children.Count == 0 || _points == null || _points.Count < 2)
            {
                return;
            }

            var spacing = _spacing;
            if (node.Children.Count > 1)
            {
                if (_spacingType == SpacingOptions.Evenly)
                {
                    spacing = _curveLength / (node.Children.Count - 1);
                }
                else if (_spacingType == SpacingOptions.EvenlyConnected)
                {
                    spacing = _curveLength / node.Children.Count;
                }
            }

            var d = _startAt;
            for (int i = 0; i < node.Children.Count; i++)
            {
                var (position, forward, up) = GetCurvePositionAndForwardAtDistance(d);
                node.Children[i].SetPositionResult(position);
                d += spacing;

                var rotation = Quaternion.identity;
                var inDirection = Vector3.Cross(forward, up).normalized;
                up = Vector3.Cross(inDirection, forward);
                switch (_rotation)
                {
                    case RotationOptions.In:
                        rotation = Quaternion.LookRotation(inDirection);
                        break;
                    case RotationOptions.Out:
                        rotation = Quaternion.LookRotation(-inDirection);
                        break;
                    case RotationOptions.InWithRoll:
                        rotation = Quaternion.LookRotation(inDirection, up);
                        break;
                    case RotationOptions.OutWithRoll:
                        rotation = Quaternion.LookRotation(-inDirection, up);
                        break;
                    case RotationOptions.Forward:
                        rotation = Quaternion.LookRotation(forward, up);
                        break;
                    case RotationOptions.Backward:
                        rotation = Quaternion.LookRotation(-forward, up);
                        break;
                }

                node.Children[i].SetRotationResult(rotation);
            }
        }

        private static Vector3 ComputePositionOnBezierCurve(CurvePoint point1, CurvePoint point2, float t)
        {
            Vector3 p1 = point1.Position;
            Vector3 p2 = point1.Position + point1.Tangent;
            Vector3 p3 = point2.Position - point2.Tangent;
            Vector3 p4 = point2.Position;

            float a = Mathf.Pow(1 - t, 3);
            float b = 3 * Mathf.Pow(1 - t, 2) * t;
            float c = 3 * (1 - t) * Mathf.Pow(t, 2);
            float d = Mathf.Pow(t, 3);
            return p1 * a + p2 * b + p3 * c + p4 * d;
        }

        protected override void Initialize()
        {
            base.Initialize();

            var factor = 1f;
            if (transform is RectTransform || (transform.parent && transform.parent is RectTransform))
            {
                factor = 100f;
            }

            _points = new List<CurvePoint>() {
                new CurvePoint() { Position = Vector3.left * factor, TangentMode = TangentMode.Smooth, Tangent = Vector3.zero },
                new CurvePoint() { Position = Vector3.zero * factor, TangentMode = TangentMode.Smooth, Tangent = Vector3.zero },
                new CurvePoint() { Position = Vector3.right * factor, TangentMode = TangentMode.Smooth, Tangent = Vector3.zero },
            };

            _spacing = 0.5f * factor;
        }

#if UNITY_EDITOR
        public int EditorHovered = -1;

        private void OnDrawGizmosSelected()
        {
            var scale = Node.GetWorldBoxScale(true);
            Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, scale);

            for (int i = 0; i < _points.Count; i++)
            {
                float sphereSize = 0.025f;
                Gizmos.color = new Color(1, 1, 1, 0.9f);
                if (EditorHovered == i)
                {
                    Gizmos.color = Color.cyan;
                    sphereSize = 0.05f;
                }

                Gizmos.DrawSphere(_points[i].Position, sphereSize);
            }
        }
#endif
    }
}