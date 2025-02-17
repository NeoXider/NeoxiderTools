using UnityEngine;

namespace Neo
{
    namespace Tools
    {
        public class Follow : MonoBehaviour
        {
            public enum FollowMode { ThreeD, TwoD }

            [Header("Follow Settings")]
            [SerializeField] private Transform _target;
            [SerializeField] private FollowMode _followMode = FollowMode.ThreeD;

            [Space, Header("Move")]
            [SerializeField] private bool _followPosition = true;
            [SerializeField] 
            [Range(0,1)]private float _smoothSpeed = 0.125f;
            [SerializeField] private Vector3 _offset;

            [Space, Header("Position Limit")]
            [SerializeField] private Vector2 _positionLimitX = new Vector2(0, 0);
            [SerializeField] private Vector2 _positionLimitY = new Vector2(0, 0);
            [SerializeField] private Vector2 _positionLimitZ = new Vector2(0, 0);

            // Новые переменные для ограничения поворота
            [Space, Header("Rotation Limits")]
            [SerializeField] private Vector2 _rotationLimitX = new Vector2(0, 0);
            [SerializeField] private Vector2 _rotationLimitY = new Vector2(0, 0);

            [Space, Header("Rotation Limits 2D")]
            [SerializeField] private Vector2 _rotationLimitZ = new Vector2(0, 0);
            [Space, Header("Rotation")]
            [SerializeField] private bool _followRotation = true;
            [SerializeField] private float _rotationSpeed = 5;
            [SerializeField] private Vector3 _rotationOffset3D;
            [SerializeField] private float _rotationOffset2D;

            private void LateUpdate()
            {
                if (_target == null)
                    return;

                if (_followPosition)
                {
                    FollowPosition();
                }

                if (_followRotation)
                {
                    FollowRotation();
                }
            }

            private void FollowPosition()
            {
                Vector3 desiredPosition = _target.position + _offset;

                if (_followMode == FollowMode.TwoD)
                    desiredPosition.z = _offset.z;

                if (_positionLimitX != Vector2.zero)
                    desiredPosition.x = Mathf.Clamp(desiredPosition.x, _positionLimitX.x, _positionLimitX.y);
                if (_positionLimitY != Vector2.zero)
                    desiredPosition.y = Mathf.Clamp(desiredPosition.y, _positionLimitY.x, _positionLimitY.y);
                if (_positionLimitZ != Vector2.zero)
                    desiredPosition.z = Mathf.Clamp(desiredPosition.z, _positionLimitZ.x, _positionLimitZ.y);

                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
                transform.position = smoothedPosition;
            }

            private void FollowRotation()
            {
                if (_followMode == FollowMode.ThreeD)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(_target.position - transform.position);
                    targetRotation *= Quaternion.Euler(_rotationOffset3D);

                    Quaternion targetRotationLimited;

                    if (_rotationLimitX != Vector2.zero)
                    {
                        float xAngle = Mathf.Clamp(targetRotation.eulerAngles.x, _rotationLimitX.x, _rotationLimitX.y);
                        float yAngle = Mathf.Clamp(targetRotation.eulerAngles.y, _rotationLimitY.x, _rotationLimitY.y);

                        targetRotationLimited = Quaternion.Euler(xAngle, yAngle, targetRotation.eulerAngles.z);
                    }
                    else
                    {
                        targetRotationLimited = targetRotation;
                    }

                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotationLimited, _rotationSpeed * Time.deltaTime);

                }
                else if (_followMode == FollowMode.TwoD)
                {
                    Vector3 direction = _target.position - transform.position;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    angle += _rotationOffset2D;

                    if (_rotationLimitZ != Vector2.zero)
                        angle = Mathf.Clamp(angle, _rotationLimitZ.x, _rotationLimitZ.y);

                    Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }
            }

            private void OnValidate()
            {
            }
        }
    }
}