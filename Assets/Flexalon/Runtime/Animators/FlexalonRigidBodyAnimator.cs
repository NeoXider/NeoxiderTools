#if UNITY_PHYSICS

using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// If you add a Rigid Body or Rigid Body 2D component a gameObject which is managed by Flexalon, then
    /// the physics system will fight with Flexalon over the object's position and rotation.
    /// Adding a Rigid Body animator will resolve this by applying forces to the the rigid body component
    /// instead of changing the transform directly.
    /// </summary>
    [AddComponentMenu("Flexalon/Flexalon Rigid Body Animator"), HelpURL("https://www.flexalon.com/docs/animators")]
    public class FlexalonRigidBodyAnimator : MonoBehaviour, TransformUpdater
    {
        private FlexalonNode _node;
        private Rigidbody _rigidBody;
        private Rigidbody2D _rigidBody2D;

        [SerializeField]
        private float _positionForce = 5.0f;
        /// <summary> How much force should be applied each frame to move the object to the layout position. </summary>
        public float PositionForce
        {
            get => _positionForce;
            set { _positionForce = value; }
        }

        [SerializeField]
        private float _rotationForce = 5.0f;
        /// <summary> How much force should be applied each frame to rotation the object to the layout rotation. </summary>
        public float RotationForce
        {
            get => _rotationForce;
            set { _rotationForce = value; }
        }

        [SerializeField]
        private float _scaleInterpolationSpeed = 5.0f;
        /// <summary> Amount the object's scale should be interpolated towards the layout size at each frame.
        /// This value is multiplied by Time.deltaTime. </summary>
        public float ScaleInterpolationSpeed
        {
            get => _scaleInterpolationSpeed;
            set { _scaleInterpolationSpeed = value; }
        }

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _fromScale;
        private RectTransform _rectTransform;

        void OnEnable()
        {
            _node = Flexalon.GetOrCreateNode(gameObject);
            _node.SetTransformUpdater(this);
            _rigidBody = GetComponent<Rigidbody>();
            _rigidBody2D = GetComponent<Rigidbody2D>();
            _targetPosition = transform.localPosition;
            _targetRotation = transform.localRotation;
            _rectTransform = (transform is RectTransform) ? (RectTransform)transform : null;
        }

        void OnDisable()
        {
            _node.SetTransformUpdater(null);
            _node = null;
        }

        /// <inheritdoc />
        public void PreUpdate(FlexalonNode node)
        {
            _fromScale = transform.lossyScale;
        }

        /// <inheritdoc />
        public bool UpdatePosition(FlexalonNode node, Vector3 position)
        {
            if (_rigidBody || _rigidBody2D)
            {
                _targetPosition = position;
                return false;
            }
            else
            {
                transform.localPosition = position;
                return true;
            }
        }

        /// <inheritdoc />
        public bool UpdateRotation(FlexalonNode node, Quaternion rotation)
        {
            if (_rigidBody || _rigidBody2D)
            {
                _targetRotation = rotation;
                return false;
            }
            else
            {
                transform.localRotation = rotation;
                return true;
            }
        }

        /// <inheritdoc />
        public bool UpdateScale(FlexalonNode node, Vector3 scale)
        {
            var worldScale = transform.parent == null ? scale : Math.Mul(scale, transform.parent.lossyScale);
            if (Vector3.Distance(_fromScale, worldScale) < 0.001f)
            {
                transform.localScale = scale;
                return true;
            }
            else
            {
                var newWorldScale = Vector3.Lerp(_fromScale, worldScale, _scaleInterpolationSpeed * Time.smoothDeltaTime);
                transform.localScale = transform.parent == null ? newWorldScale : Math.Div(newWorldScale, transform.parent.lossyScale);
                return false;
            }
        }

        /// <inheritdoc />
        public bool UpdateRectSize(FlexalonNode node, Vector2 size)
        {
            bool done = Vector2.Distance(_rectTransform.sizeDelta, size) < 0.001f;
            var newSize = done ? size : Vector2.Lerp(_rectTransform.sizeDelta, size, _scaleInterpolationSpeed * Time.smoothDeltaTime);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);
            return done;
        }

        void FixedUpdate()
        {
            if (!_rigidBody && !_rigidBody2D)
            {
                return;
            }

            bool hasLayout = _node.Parent != null || (_node.Constraint != null && _node.Constraint.Target != null);
            if (!hasLayout)
            {
                return;
            }

            var worldPos = transform.parent ? transform.parent.localToWorldMatrix.MultiplyPoint(_targetPosition) : _targetPosition;
            var force = (worldPos - transform.position) * _positionForce;
            var rot = Quaternion.Slerp(transform.localRotation, _targetRotation, _rotationForce * Time.deltaTime);
            var rotWorldSpace = (transform.parent?.rotation ?? Quaternion.identity) * rot;

            if (_rigidBody)
            {
                _rigidBody.AddForce(force, ForceMode.Force);
                _rigidBody.MoveRotation(rotWorldSpace);
            }

            if (_rigidBody2D)
            {
                _rigidBody2D?.AddForce(force, ForceMode2D.Force);
                _rigidBody2D.MoveRotation(rotWorldSpace);
            }
        }
    }
}

#endif