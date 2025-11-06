using UnityEngine;

namespace Neo.Tools 
{
    [AddComponentMenu("Neoxider/Move/MovementToolkit/ConstantRotator")]
    public class ConstantRotator : MonoBehaviour
    {
        public enum RotationMode { Transform, Rigidbody, Rigidbody2D }
        public enum AxisSource { None, LocalForward3D, Up2D, Right2D, Custom }

        [Header("Mode")]
        public RotationMode mode = RotationMode.Transform;
        [Tooltip("Если true — ось берётся в локальном пространстве, иначе — в мировом")] public bool spaceLocal = true;
        [Tooltip("Вычитать время из скорости")] public bool useDeltaTime = true;

        [Header("Axis")]
        public AxisSource axisSource = AxisSource.None;
        public Vector3 customAxis = Vector3.up;

        [Header("Speed (deg/sec)")]
        public float degreesPerSecond = 90f;

        private Rigidbody _rb3D;
        private Rigidbody2D _rb2D;

        private void Awake()
        {
            _rb3D = GetComponent<Rigidbody>();
            _rb2D = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (mode == RotationMode.Transform)
            {
                RotateTransform();
            }
        }

        private void FixedUpdate()
        {
            if (mode == RotationMode.Rigidbody)
            {
                if (_rb3D == null)
                {
                    RotateTransform();
                    return;
                }
                RotateRigidbody3D();
            }
            else if (mode == RotationMode.Rigidbody2D)
            {
                if (_rb2D == null)
                {
                    RotateTransform();
                    return;
                }
                RotateRigidbody2D();
            }
        }

        private Vector3 ResolveAxisWorld()
        {
            if (axisSource == AxisSource.None)
            {
                return Vector3.zero;
            }

            Vector3 axis;
            switch (axisSource)
            {
                case AxisSource.Up2D:
                    axis = spaceLocal ? transform.up : Vector3.up;
                    break;
                case AxisSource.Right2D:
                    axis = spaceLocal ? transform.right : Vector3.right;
                    break;
                case AxisSource.Custom:
                    axis = spaceLocal ? transform.TransformDirection(customAxis) : customAxis;
                    break;
                case AxisSource.LocalForward3D:
                default:
                    axis = spaceLocal ? transform.forward : Vector3.forward;
                    break;
            }
            if (axis.sqrMagnitude < 1e-6f) return Vector3.zero;
            return axis.normalized;
        }

        private float Dt() => useDeltaTime ? Time.deltaTime : 1f;
        private float Fdt() => useDeltaTime ? Time.fixedDeltaTime : 1f;

        private void RotateTransform()
        {
            var axis = ResolveAxisWorld();
            if (axis == Vector3.zero) return; // нет вращения
            float deltaDeg = degreesPerSecond * Dt();
            // Вращение вокруг оси в мировом пространстве
            transform.Rotate(axis, deltaDeg, Space.World);
        }

        private void RotateRigidbody3D()
        {
            var axis = ResolveAxisWorld();
            if (axis == Vector3.zero) return;
            float deltaDeg = degreesPerSecond * Fdt();
            var deltaRot = Quaternion.AngleAxis(deltaDeg, axis);
            _rb3D.MoveRotation(deltaRot * _rb3D.rotation);
        }

        private void RotateRigidbody2D()
        {
            // Для 2D берём проекцию оси на Z, чтобы определить знак вращения
            var axis = ResolveAxisWorld();
            if (axis == Vector3.zero) return;
            float sign = Mathf.Sign(Vector3.Dot(axis, Vector3.forward));
            float deltaDeg = degreesPerSecond * Fdt() * (sign == 0 ? 1f : sign);
            _rb2D.MoveRotation(_rb2D.rotation + deltaDeg);
        }
    }
}


