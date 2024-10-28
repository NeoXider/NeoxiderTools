using UnityEngine;

namespace Neoxider
{
    public class Follow : MonoBehaviour
    {
        public enum FollowMode { ThreeD, TwoD }

        [Header("Follow Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private FollowMode _followMode = FollowMode.ThreeD;

        [Space, Header("Move")]
        [SerializeField] private bool _followPosition = true;
        [SerializeField] private float _smoothSpeed = 0.125f;
        [SerializeField] private Vector3 _offset;


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
                desiredPosition.z = transform.position.z;

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
            transform.position = smoothedPosition;
        }

        private void FollowRotation()
        {
            if (_followMode == FollowMode.ThreeD)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_target.position - transform.position);
                targetRotation *= Quaternion.Euler(_rotationOffset3D);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
            else if (_followMode == FollowMode.TwoD)
            {
                Vector3 direction = _target.position - transform.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                angle += _rotationOffset2D;
                Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }
        private void OnValidate()
        {
        }
    }
}
