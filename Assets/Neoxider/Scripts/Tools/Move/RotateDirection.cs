using UnityEngine;

namespace Neo.Tools
{
    public class RotateDirection : MonoBehaviour
    {
        [SerializeField, GetComponent] private MoveSimple _moveSimple;
        public Vector3 directionOffset = new Vector3(0, 0, -5f);
        public Vector3 offset = Vector3.zero;
        public Vector3 direction;

        public bool useRotation = false;
        public bool rotate2d = false;
        public Vector3 offsetRotation = Vector3.zero;
        public Transform target;

        void Update()
        {
            if (_moveSimple != null)
                direction = _moveSimple.direction;

            if (direction == Vector3.zero)
                return;

            direction = direction.normalized;

            Vector3 targetPosition = target.position + GetDirection() + offset;

            transform.position = targetPosition;

            if (useRotation)
            {
                if (rotate2d)
                    transform.LookAt2D(target);
                else
                    transform.LookAt(target);

                transform.Rotate(offsetRotation);
            }
        }

        private Vector3 GetDirection()
        {
            return new Vector3(direction.x * directionOffset.x, direction.y * directionOffset.y, direction.z * directionOffset.z);
        }

        private void OnValidate()
        {
            Update();
        }
    }
}