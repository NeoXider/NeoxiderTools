using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Moves an object based on keyboard / joystick axes.
    ///     Supports different movement planes (XY, XZ, YZ) for both 2D and 3D games.
    ///     If a Rigidbody2D is present, the script moves in <c>FixedUpdate</c>
    ///     via <c>MovePosition</c>; otherwise it translates the Transform in
    ///     <c>Update</c>. Fires UnityEvents when motion begins or ends and
    ///     implements <see cref="IMover" /> for external control.
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(KeyboardMover))]
    public class KeyboardMover : MonoBehaviour, IMover
    {
        #region === public configuration ===

        public enum AxisMode
        {
            AxisNormalized,
            AxisRaw
        }

        public enum MovementPlane
        {
            XY,
            XZ,
            YZ
        }

        [Header("Movement Plane")] [Tooltip("The plane on which movement occurs.")] [SerializeField]
        private MovementPlane movementPlane = MovementPlane.XY;

        [Header("Mode")] [Tooltip("How the axis vector is treated.")] [SerializeField]
        private AxisMode axisMode = AxisMode.AxisRaw;

        [Header("Speed")] [Tooltip("Units per second.")] [SerializeField]
        private float speed = 5f;

        public UnityEvent OnMoveStart;

        public UnityEvent OnMoveStop;

        #endregion

        //--------------------------------------------------------------------

        #region === IMover ===

        /// <inheritdoc />
        public bool IsMoving { get; private set; }

        /// <inheritdoc />
        public void MoveDelta(Vector2 delta)
        {
            transform.Translate(delta, Space.World);
        }

        /// <inheritdoc />
        public void MoveToPoint(Vector2 worldPoint)
        {
            transform.position = worldPoint;
        }

        #endregion

        //--------------------------------------------------------------------

        #region === private fields ===

        private Rigidbody2D _rb;
        private Vector3 _cachedDelta;
        private bool _wasMovingLast;

        #endregion

        //--------------------------------------------------------------------

        #region === unity callbacks ===

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            _cachedDelta = ComputeDelta(Time.deltaTime);

            // If kinematic (no Rigidbody2D) – move right away
            if (!_rb)
            {
                ApplyDelta(_cachedDelta);
            }
        }

        private void FixedUpdate()
        {
            if (!_rb)
            {
                return;
            }

            float k = Time.fixedDeltaTime / Time.deltaTime;
            ApplyDelta(_cachedDelta * k); // preserves speed in physics step
        }

        #endregion

        //--------------------------------------------------------------------

        #region === movement logic ===

        private Vector3 ComputeDelta(float dt)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 dir = new(h, v);

            if (axisMode == AxisMode.AxisNormalized && dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
            }

            Vector2 scaledDir = dir * speed * dt;
            return MapToPlane(scaledDir);
        }

        private Vector3 MapToPlane(Vector2 input)
        {
            return movementPlane switch
            {
                MovementPlane.XY => new Vector3(input.x, input.y, 0f),
                MovementPlane.XZ => new Vector3(input.x, 0f, input.y),
                MovementPlane.YZ => new Vector3(0f, input.x, input.y),
                _ => new Vector3(input.x, input.y, 0f)
            };
        }

        private void ApplyDelta(Vector3 delta)
        {
            bool movingNow = delta.sqrMagnitude > 0.0001f;

            if (movingNow && !_wasMovingLast)
            {
                OnMoveStart?.Invoke();
            }

            if (!movingNow && _wasMovingLast)
            {
                OnMoveStop?.Invoke();
            }

            IsMoving = movingNow;
            _wasMovingLast = movingNow;

            if (!movingNow)
            {
                return;
            }

            if (_rb)
            {
                _rb.MovePosition(_rb.position + (Vector2)delta);
            }
            else
            {
                transform.Translate(delta, Space.World);
            }
        }

        #endregion
    }
}