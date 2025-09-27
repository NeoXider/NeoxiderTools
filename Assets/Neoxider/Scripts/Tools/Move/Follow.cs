using UnityEngine;

namespace Neo.Tools
{
    public class Follow : MonoBehaviour
    {
        public enum FollowMode
        {
            ThreeD,
            TwoD
        }

        [Header("Follow Settings")] [SerializeField]
        private Transform _target;

        [SerializeField] private FollowMode _followMode = FollowMode.ThreeD;

        [Header("Movement Settings")] [SerializeField]
        private bool _followPosition = true;

        [SerializeField] private bool _useSmooth = true;
        [SerializeField] [Range(0f, 5f)] private float _smoothSpeed = 1.5f;
        [SerializeField] private Vector3 _offset;

        [Header("Position Limitations")] [SerializeField]
        private Vector2 _positionLimitX = Vector2.zero;

        [SerializeField] private Vector2 _positionLimitY = Vector2.zero;
        [SerializeField] private Vector2 _positionLimitZ = Vector2.zero;

        [Header("Rotation Limits 3D")] [SerializeField]
        private Vector2 _rotationLimitX = Vector2.zero;

        [SerializeField] private Vector2 _rotationLimitY = Vector2.zero;

        [Header("Rotation Limits 2D")] [SerializeField]
        private Vector2 _rotationLimitZ = Vector2.zero;

        [Header("Rotation Settings")] [SerializeField]
        private bool _followRotation = true;

        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private Vector3 _rotationOffset3D;
        [SerializeField] private float _rotationOffset2D;

        private void LateUpdate()
        {
            if (_target == null)
                return;

            if (_followPosition)
                FollowPosition();

            if (_followRotation)
                FollowRotation();
        }

        private void OnValidate()
        {
        }

        /// <summary>
        ///     Плавное следование за позицией таргета с учетом смещения и ограничений.
        /// </summary>
        private void FollowPosition()
        {
            var desiredPosition = _target.position + _offset;

            if (_positionLimitX != Vector2.zero)
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, _positionLimitX.x, _positionLimitX.y);
            if (_positionLimitY != Vector2.zero)
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, _positionLimitY.x, _positionLimitY.y);
            if (_positionLimitZ != Vector2.zero)
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, _positionLimitZ.x, _positionLimitZ.y);

            if (_followMode == FollowMode.TwoD)
                desiredPosition.z = _offset.z;

            if (_useSmooth)
                desiredPosition =
                    Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.smoothDeltaTime);

            transform.position = desiredPosition;
        }

        /// <summary>
        ///     Плавное вращение объекта по направлению на таргет.
        ///     Для 3D используется LookRotation, для 2D — вычисление угла через Atan2 с компенсацией поворота.
        /// </summary>
        private void FollowRotation()
        {
            if (_followMode == FollowMode.ThreeD)
            {
                var direction = _target.position - transform.position;
                if (direction == Vector3.zero)
                    return;

                var targetRotation = Quaternion.LookRotation(direction);
                targetRotation *= Quaternion.Euler(_rotationOffset3D);

                var euler = targetRotation.eulerAngles;
                if (_rotationLimitX != Vector2.zero)
                    euler.x = Mathf.Clamp(euler.x, _rotationLimitX.x, _rotationLimitX.y);
                if (_rotationLimitY != Vector2.zero)
                    euler.y = Mathf.Clamp(euler.y, _rotationLimitY.x, _rotationLimitY.y);
                targetRotation = Quaternion.Euler(euler);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    _rotationSpeed * Time.smoothDeltaTime);
            }
            else if (_followMode == FollowMode.TwoD)
            {
                var direction = _target.position - transform.position;
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                angle += _rotationOffset2D;

                if (_rotationLimitZ != Vector2.zero)
                    angle = Mathf.Clamp(angle, _rotationLimitZ.x, _rotationLimitZ.y);

                var targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    _rotationSpeed * Time.smoothDeltaTime);
            }
        }
    }
}