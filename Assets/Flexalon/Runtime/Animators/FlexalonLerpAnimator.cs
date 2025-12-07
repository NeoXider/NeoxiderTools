using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// The lerp animator constnatly performs a linear interpolation between
    /// the object's current position and its layout position. This is useful
    /// if the layout position is continuously changing.
    /// </summary>
    [AddComponentMenu("Flexalon/Flexalon Lerp Animator"), HelpURL("https://www.flexalon.com/docs/animators")]
    public class FlexalonLerpAnimator : MonoBehaviour, TransformUpdater
    {
        private FlexalonNode _node;
        private RectTransform _rectTransform;

        [SerializeField]
        private bool _animateInWorldSpace = true;
        /// <summary> Determines if the animation should be performed in world space. </summary>
        public bool AnimateInWorldSpace
        {
            get => _animateInWorldSpace;
            set { _animateInWorldSpace = value; }
        }

        [SerializeField]
        private float _interpolationSpeed = 5.0f;
        /// <summary> Amount the object should be interpolated towards the target at each frame.
        /// This value is multiplied by Time.deltaTime. </summary>
        public float InterpolationSpeed
        {
            get => _interpolationSpeed;
            set { _interpolationSpeed = value; }
        }

        [SerializeField]
        private bool _animatePosition = true;
        /// <summary> Determines if the position should be animated. </summary>
        public bool AnimatePosition
        {
            get => _animatePosition;
            set { _animatePosition = value; }
        }

        [SerializeField]
        private bool _animateRotation = true;
        /// <summary> Determines if the rotation should be animated. </summary>
        public bool AnimateRotation
        {
            get => _animateRotation;
            set { _animateRotation = value; }
        }

        [SerializeField]
        private bool _animateScale = true;
        /// <summary> Determines if the rotation should be animated. </summary>
        public bool AnimateScale
        {
            get => _animateScale;
            set { _animateScale = value; }
        }

        private Vector3 _fromPosition;
        private Quaternion _fromRotation;
        private Vector3 _fromScale;

        void OnEnable()
        {
            _node = Flexalon.GetOrCreateNode(gameObject);
            _node.SetTransformUpdater(this);
            _rectTransform = (transform is RectTransform) ? (RectTransform)transform : null;
        }

        void OnDisable()
        {
            _node?.SetTransformUpdater(null);
            _node = null;
        }

        /// <inheritdoc />
        public void PreUpdate(FlexalonNode node)
        {
            _fromPosition = transform.position;
            _fromRotation = transform.rotation;
            _fromScale = transform.lossyScale;
        }

        /// <inheritdoc />
        public bool UpdatePosition(FlexalonNode node, Vector3 position)
        {
            if (_animateInWorldSpace)
            {
                var worldPosition = transform.parent ? transform.parent.localToWorldMatrix.MultiplyPoint(position) : position;
                if (!_animatePosition || Vector3.Distance(_fromPosition, worldPosition) < 0.001f)
                {
                    transform.localPosition = position;
                    return true;
                }
                else
                {
                    transform.position = Vector3.Lerp(_fromPosition, worldPosition, _interpolationSpeed * Time.smoothDeltaTime);
                    return false;
                }
            }
            else
            {
                if (!_animatePosition || Vector3.Distance(transform.localPosition, position) < 0.001f)
                {
                    transform.localPosition = position;
                    return true;
                }
                else
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, position, _interpolationSpeed * Time.smoothDeltaTime);
                    return false;
                }
            }
        }

        /// <inheritdoc />
        public bool UpdateRotation(FlexalonNode node, Quaternion rotation)
        {
            if (_animateInWorldSpace)
            {
                var worldRotation = transform.parent ? transform.parent.rotation * rotation : rotation;
                if (!_animateRotation || Mathf.Abs(Quaternion.Angle(_fromRotation, worldRotation)) < 0.001f)
                {
                    transform.localRotation = rotation;
                    return true;
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(_fromRotation, worldRotation, _interpolationSpeed * Time.smoothDeltaTime);
                    return false;
                }
            }
            else
            {
                if (!_animateRotation || Mathf.Abs(Quaternion.Angle(transform.localRotation, rotation)) < 0.001f)
                {
                    transform.localRotation = rotation;
                    return true;
                }
                else
                {
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, rotation, _interpolationSpeed * Time.smoothDeltaTime);
                    return false;
                }
            }
        }

        /// <inheritdoc />
        public bool UpdateScale(FlexalonNode node, Vector3 scale)
        {
            if (_animateInWorldSpace)
            {
                var worldScale = transform.parent ? Math.Mul(scale, transform.parent.lossyScale) : scale;
                if (!_animateScale || Vector3.Distance(_fromScale, worldScale) < 0.001f)
                {
                    transform.localScale = scale;
                    return true;
                }
                else
                {
                    var newWorldScale = Vector3.Lerp(_fromScale, worldScale, _interpolationSpeed * Time.smoothDeltaTime);
                    transform.localScale = transform.parent ? Math.Div(newWorldScale, transform.parent.lossyScale) : newWorldScale;
                    return false;
                }
            }
            else
            {
                if (!_animateScale || Vector3.Distance(transform.localScale, scale) < 0.001f)
                {
                    transform.localScale = scale;
                    return true;
                }
                else
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, scale, _interpolationSpeed * Time.smoothDeltaTime);
                    return false;
                }
            }
        }

        /// <inheritdoc />
        public bool UpdateRectSize(FlexalonNode node, Vector2 size)
        {
            bool done = !_animateScale || Vector2.Distance(_rectTransform.sizeDelta, size) < 0.001f;
            var newSize = done ? size : Vector2.Lerp(_rectTransform.sizeDelta, size, _interpolationSpeed * Time.smoothDeltaTime);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);
            return done;
        }
    }
}