using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// The Flexalon Constraint component positions its gameObject relative to a target,
    /// which can be any other gameObject. The constrained object or the target can also
    /// have layout components, which provides a powerful way to combine layouts together.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Flexalon/Flexalon Constraint"), HelpURL("https://www.flexalon.com/docs/constraints")]
    public class FlexalonConstraint : FlexalonComponent, Constraint
    {
        [SerializeField]
        private GameObject _target;
        /// <summary>
        /// Which gameObject to constrain to.
        /// The position depends on the Align and Pivot options (see below).
        /// The rotation is set to match the target's rotation.
        /// The available space is set to match the target's size. Set the width, height, and
        /// depth properties on the Flexalon Object Component to Parent to match the target's size.
        /// </summary>
        public GameObject Target
        {
            get { return _target; }
            set { _target = value; MarkDirty(); }
        }

        [SerializeField]
        private Align _horizontalAlign = Align.Center;
        /// <summary> Determines how the horizontal axis aligns to the target's box. </summary>
        public Align HorizontalAlign
        {
            get { return _horizontalAlign; }
            set { _horizontalAlign = value; MarkDirty(); }
        }

        [SerializeField]
        private Align _verticalAlign = Align.Center;
        /// <summary> Determines how the vertical axis aligns to the target's box. </summary>
        public Align VerticalAlign
        {
            get { return _verticalAlign; }
            set { _verticalAlign = value; MarkDirty(); }
        }

        [SerializeField]
        private Align _depthAlign = Align.Center;
        /// <summary> Determines how the depth axis aligns to the target's box. </summary>
        public Align DepthAlign
        {
            get { return _depthAlign; }
            set { _depthAlign = value; MarkDirty(); }
        }

        [SerializeField]
        private Align _horizontalPivot = Align.Center;
        /// <summary> Determines how the horizontal axis aligns to this object's box. </summary>
        public Align HorizontalPivot
        {
            get { return _horizontalPivot; }
            set { _horizontalPivot = value; MarkDirty(); }
        }

        [SerializeField]
        private Align _verticalPivot = Align.Center;
        /// <summary> Determines how the vertical axis aligns to this object's box. </summary>
        public Align VerticalPivot
        {
            get { return _verticalPivot; }
            set { _verticalPivot = value; MarkDirty(); }
        }

        [SerializeField]
        private Align _depthPivot = Align.Center;
        /// <summary> Determines how the depth axis aligns to this object's box. </summary>
        public Align DepthPivot
        {
            get { return _depthPivot; }
            set { _depthPivot = value; MarkDirty(); }
        }

        private Transform _lastParent;
        private Vector3 _lastTargetPosition;
        private Quaternion _lastTargetRotation;
        private Vector3 _lastTargetScale;

        /// <inheritdoc />
        protected override void ResetProperties()
        {
            UpdateTarget(null);
        }

        /// <inheritdoc />
        public override void DoUpdate()
        {
            if (_target)
            {
                if (_lastTargetPosition != _target.transform.position ||
                    _lastTargetRotation != _target.transform.rotation ||
                    _lastTargetScale != _target.transform.lossyScale ||
                    _lastParent != transform.parent)
                {
                    MarkDirty();
                }

                // Detect scale/rotation changes in edit mode, even if we don't have a FlexalonObject.
                if (!Application.isPlaying && !Node.Dirty && !Node.HasFlexalonObject &&
                    (Node.Result.TransformPosition != transform.localPosition ||
                     Node.Result.TransformRotation != transform.localRotation ||
                     Node.Result.TransformScale != transform.localScale))
                {
                    MarkDirty();
                }
            }
        }

        /// <inheritdoc />
        protected override void UpdateProperties()
        {
            UpdateTarget(_target);
        }

        private void UpdateTarget(GameObject target)
        {
            if (target)
            {
                var targetNode = Flexalon.GetOrCreateNode(target);
                Node.SetConstraint(this, targetNode);

                if (!targetNode.HasResult)
                {
                    targetNode.MarkDirty();
                }

                _lastTargetPosition = target.transform.position;
                _lastTargetRotation = target.transform.rotation;
                _lastTargetScale = target.transform.lossyScale;
                _lastParent = transform.parent;
            }
            else
            {
                Node.SetConstraint(null, null);
            }
        }

        /// <summary> Applies the constraint. </summary>s
        public void Constrain(FlexalonNode node)
        {
            if (_target)
            {
                var targetNode = Flexalon.GetOrCreateNode(_target);
                var targetSize = targetNode.Result.AdapterBounds.size;
                var bounds = node.Result.RotatedAndScaledBounds;
                bounds.center += node.Margin.Center;
                bounds.size += node.Margin.Size;

                var targetScale = targetNode.GetWorldBoxScale(true);
                targetSize.Scale(targetScale);

                targetSize += targetNode.Margin.Size;

                var alignPosition = Math.Align(targetSize, _horizontalAlign, _verticalAlign, _depthAlign);

                FlexalonLog.Log("Constrain:AlignPosition [Initial]", node, alignPosition);

                alignPosition += targetNode.Result.AdapterBounds.center;
                alignPosition += targetNode.Margin.Center;
                FlexalonLog.Log("Constrain:AlignPosition [Centered]", node, alignPosition);

                var pivotPosition = Math.Align(bounds.size, _horizontalPivot, _verticalPivot, _depthPivot);
                var scale = Node.GetWorldBoxScale(false);
                pivotPosition.Scale(scale);

                FlexalonLog.Log("Constrain:PivotPosition", node, pivotPosition);

                var worldRotation = _target.transform.rotation;
                var localRotation = Quaternion.Inverse(transform.parent?.rotation ?? Quaternion.identity) * worldRotation;
                var position = alignPosition - pivotPosition - bounds.center + node.Offset;

                var worldPosition = worldRotation * position + _target.transform.position;
                FlexalonLog.Log("Constrain:WorldPosition", node, worldPosition);

                var localPosition = transform.parent?.worldToLocalMatrix.MultiplyPoint(worldPosition) ?? worldPosition;
                FlexalonLog.Log("Constrain:LocalPosition", node, localPosition);

                node.SetPositionResult(localPosition);
                node.SetRotationResult(localRotation);
            }
        }
    }
}