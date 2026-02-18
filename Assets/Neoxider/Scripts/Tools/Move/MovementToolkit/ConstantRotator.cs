using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Move/MovementToolkit/ConstantRotator.md")]
    [CreateFromMenu("Neoxider/Tools/ConstantRotator")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ConstantRotator))]
    public class ConstantRotator : MonoBehaviour
    {
        public enum AxisSource
        {
            None,
            LocalForward3D,
            Up2D,
            Right2D,
            Custom
        }

        public enum RotationMode
        {
            Transform,
            Rigidbody,
            Rigidbody2D
        }

        [Header("Mode")] public RotationMode mode = RotationMode.Transform;

        [Tooltip("If true, axis is in local space; otherwise world space")]
        public bool spaceLocal = true;

        [Tooltip("Subtract time from speed")]
        public bool useDeltaTime = true;

        [Header("Axis")] public AxisSource axisSource = AxisSource.None;

        public Vector3 customAxis = Vector3.up;

        [Header("Speed (deg/sec)")] public float degreesPerSecond = 90f;

        private Rigidbody2D _rb2D;

        private Rigidbody _rb3D;

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

            if (axis.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            return axis.normalized;
        }

        private float Dt()
        {
            return useDeltaTime ? Time.deltaTime : 1f;
        }

        private float Fdt()
        {
            return useDeltaTime ? Time.fixedDeltaTime : 1f;
        }

        private void RotateTransform()
        {
            Vector3 axis = ResolveAxisWorld();
            if (axis == Vector3.zero)
            {
                return; // нет вращения
            }

            float deltaDeg = degreesPerSecond * Dt();
            // Вращение вокруг оси в мировом пространстве
            transform.Rotate(axis, deltaDeg, Space.World);
        }

        private void RotateRigidbody3D()
        {
            Vector3 axis = ResolveAxisWorld();
            if (axis == Vector3.zero)
            {
                return;
            }

            float deltaDeg = degreesPerSecond * Fdt();
            Quaternion deltaRot = Quaternion.AngleAxis(deltaDeg, axis);
            _rb3D.MoveRotation(deltaRot * _rb3D.rotation);
        }

        private void RotateRigidbody2D()
        {
            // Для 2D берём проекцию оси на Z, чтобы определить знак вращения
            Vector3 axis = ResolveAxisWorld();
            if (axis == Vector3.zero)
            {
                return;
            }

            float sign = Mathf.Sign(Vector3.Dot(axis, Vector3.forward));
            float deltaDeg = degreesPerSecond * Fdt() * (sign == 0 ? 1f : sign);
            _rb2D.MoveRotation(_rb2D.rotation + deltaDeg);
        }
    }
}
