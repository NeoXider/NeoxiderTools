using UnityEngine;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/Move/MovementToolkit/ConstantMover")]
    public class ConstantMover : MonoBehaviour
    {
        public enum MovementMode { Transform, Rigidbody, Rigidbody2D }
        public enum DirectionSource { LocalForward3D, Up2D, Right2D, Custom }

        [Header("Mode")]
        public MovementMode mode = MovementMode.Transform;
        [Tooltip("Если true — направление/ось берутся в локальном пространстве, иначе — в мировом")] public bool spaceLocal = true;
        [Tooltip("Вычитать время из скорости")] public bool useDeltaTime = true;

        [Header("Direction")]
        public DirectionSource directionSource = DirectionSource.LocalForward3D;
        public Vector3 customDirection = Vector3.forward;

        [Header("Speed (units/sec)")]
        public float speed = 1f;

        [Header("Axis Locks (world space)")]
        public bool lockX;
        public bool lockY;
        public bool lockZ;

        private Rigidbody _rb3D;
        private Rigidbody2D _rb2D;

        private void Awake()
        {
            _rb3D = GetComponent<Rigidbody>();
            _rb2D = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (mode == MovementMode.Transform)
            {
                MoveTransform();
            }
        }

        private void FixedUpdate()
        {
            if (mode == MovementMode.Rigidbody)
            {
                if (_rb3D == null)
                {
                    MoveTransform();
                    return;
                }
                MoveRigidbody3D();
            }
            else if (mode == MovementMode.Rigidbody2D)
            {
                if (_rb2D == null)
                {
                    MoveTransform();
                    return;
                }
                MoveRigidbody2D();
            }
        }

        private Vector3 ResolveDirectionWorld()
        {
            Vector3 dir;
            switch (directionSource)
            {
                case DirectionSource.Up2D:
                    dir = spaceLocal ? transform.up : Vector3.up;
                    break;
                case DirectionSource.Right2D:
                    dir = spaceLocal ? transform.right : Vector3.right;
                    break;
                case DirectionSource.Custom:
                    dir = spaceLocal ? transform.TransformDirection(customDirection) : customDirection;
                    break;
                case DirectionSource.LocalForward3D:
                default:
                    dir = spaceLocal ? transform.forward : Vector3.forward;
                    break;
            }
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
            return dir.normalized;
        }

        private float Dt() => useDeltaTime ? Time.deltaTime : 1f;
        private float Fdt() => useDeltaTime ? Time.fixedDeltaTime : 1f;

        private void ApplyAxisLocks(ref Vector3 delta)
        {
            if (lockX) delta.x = 0f;
            if (lockY) delta.y = 0f;
            if (lockZ) delta.z = 0f;
        }

        private void MoveTransform()
        {
            Vector3 dirWorld = ResolveDirectionWorld();
            Vector3 delta = dirWorld * speed * Dt();
            ApplyAxisLocks(ref delta);
            transform.position += delta;
        }

        private void MoveRigidbody3D()
        {
            Vector3 dirWorld = ResolveDirectionWorld();
            Vector3 delta = dirWorld * speed * Fdt();
            ApplyAxisLocks(ref delta);
            _rb3D.MovePosition(_rb3D.position + delta);
        }

        private void MoveRigidbody2D()
        {
            Vector3 dirWorld = ResolveDirectionWorld();
            Vector3 delta = dirWorld * speed * Fdt();
            ApplyAxisLocks(ref delta);
            _rb2D.MovePosition(_rb2D.position + (Vector2)delta);
        }
    }
}


