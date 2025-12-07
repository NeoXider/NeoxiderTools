using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// The curve animator applies a curve the the position, rotation, and scale
    /// of the object. The curve is restarted each time the layout position changes.
    /// This is ideal for scenarios in which the layout position does not change often.
    /// </summary>
    [AddComponentMenu("Flexalon/Flexalon Curve Animator"), HelpURL("https://www.flexalon.com/docs/animators")]
    public class FlexalonCurveAnimator : MonoBehaviour, TransformUpdater
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
        private AnimationCurve _curve = AnimationCurve.Linear(0, 0, 1, 1);
        /// <summary> The curve to apply. Should begin at 0 and end at 1. </summary>
        public AnimationCurve Curve
        {
            get => _curve;
            set { _curve = value; }
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
        /// <summary> Determines if the scale should be animated. </summary>
        public bool AnimateScale
        {
            get => _animateScale;
            set { _animateScale = value; }
        }

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _startScale;
        private Vector2 _startRectSize;

        private Vector3 _endPosition;
        private Quaternion _endRotation;
        private Vector3 _endScale;
        private Vector2 _endRectSize;

        private float _positionTime;
        private float _rotationTime;
        private float _scaleTime;
        private float _rectSizeTime;

        private Vector3 _fromPosition;
        private Quaternion _fromRotation;
        private Vector3 _fromScale;
        private Vector2 _fromRectSize;

        void OnEnable()
        {
            _startPosition = _endPosition = new Vector3(float.NaN, float.NaN, float.NaN);
            _startRotation = _endRotation = new Quaternion(float.NaN, float.NaN, float.NaN, float.NaN);
            _startScale = _endScale = new Vector3(float.NaN, float.NaN, float.NaN);
            _positionTime = _rotationTime = _scaleTime = 0;
            _rectTransform = (transform is RectTransform) ? (RectTransform)transform : null;

            _node = Flexalon.GetOrCreateNode(gameObject);
            _node.SetTransformUpdater(this);
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
            _fromRectSize = _rectTransform?.rect.size ?? Vector2.zero;
        }

        /// <inheritdoc />
        public bool UpdatePosition(FlexalonNode node, Vector3 position)
        {
            var newEndPosition = position;
            var newStartPosition = transform.localPosition;

            if (_animateInWorldSpace)
            {
                newEndPosition = transform.parent ? transform.parent.localToWorldMatrix.MultiplyPoint(position) : position;;
                newStartPosition = _fromPosition;
            }

            if (newEndPosition != _endPosition)
            {
                _startPosition = newStartPosition;
                _endPosition = newEndPosition;
                _positionTime = 0;
            }

            _positionTime += Time.smoothDeltaTime;

            if (!_animatePosition || _positionTime > _curve.keys[_curve.keys.Length - 1].time)
            {
                transform.localPosition = position;
                _endPosition = new Vector3(float.NaN, float.NaN, float.NaN);
                return true;
            }
            else
            {
                var newPosition = Vector3.Lerp(_startPosition, _endPosition, _curve.Evaluate(_positionTime));
                if (_animateInWorldSpace)
                {
                    transform.position = newPosition;
                }
                else
                {
                    transform.localPosition = newPosition;
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool UpdateRotation(FlexalonNode node, Quaternion rotation)
        {
            var newEndRotation = rotation;
            var newStartRotation = transform.localRotation;

            if (_animateInWorldSpace)
            {
                newEndRotation = transform.parent ? transform.parent.rotation * rotation : rotation;;
                newStartRotation = _fromRotation;
            }

            if (newEndRotation != _endRotation)
            {
                _startRotation = newStartRotation;
                _endRotation = newEndRotation;
                _rotationTime = 0;
            }

            _rotationTime += Time.smoothDeltaTime;

            if (!_animateRotation || _rotationTime > _curve.keys[_curve.keys.Length - 1].time)
            {
                transform.localRotation = rotation;
                _endRotation = new Quaternion(float.NaN, float.NaN, float.NaN, float.NaN);
                return true;
            }
            else
            {
                var newRotation = Quaternion.Slerp(_startRotation, _endRotation, _curve.Evaluate(_rotationTime));
                if (_animateInWorldSpace)
                {
                    transform.rotation = newRotation;
                }
                else
                {
                    transform.localRotation = newRotation;
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool UpdateScale(FlexalonNode node, Vector3 scale)
        {
            var newEndScale = scale;
            var newStartScale = transform.localScale;

            if (_animateInWorldSpace)
            {
                newEndScale = transform.parent ? Math.Mul(scale, transform.parent.lossyScale) : scale;
                newStartScale = _fromScale;
            }

            if (newEndScale != _endScale)
            {
                _startScale = newStartScale;
                _endScale = newEndScale;
                _scaleTime = 0;
            }

            _scaleTime += Time.smoothDeltaTime;

            if (!_animateScale || _scaleTime > _curve.keys[_curve.keys.Length - 1].time)
            {
                transform.localScale = scale;
                _endScale = new Vector3(float.NaN, float.NaN, float.NaN);
                return true;
            }
            else
            {
                var newScale = Vector3.Lerp(_startScale, _endScale, _curve.Evaluate(_scaleTime));

                if (_animateInWorldSpace)
                {
                    transform.localScale = transform.parent ? Math.Div(newScale, transform.parent.lossyScale) : newScale;
                }
                else
                {
                    transform.localScale = newScale;
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool UpdateRectSize(FlexalonNode node, Vector2 size)
        {
            if (size != _endRectSize)
            {
                _startRectSize = _fromRectSize;
                _endRectSize = size;
                _rectSizeTime = 0;
            }

            _rectSizeTime += Time.smoothDeltaTime;
            bool done = !_animateScale || _rectSizeTime > _curve.keys[_curve.keys.Length - 1].time;
            var newSize = done ? size : Vector2.Lerp(_startRectSize, _endRectSize, _curve.Evaluate(_rectSizeTime));
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);

            if (done)
            {
                _endRectSize = new Vector2(float.NaN, float.NaN);
            }

            return done;
        }
    }
}