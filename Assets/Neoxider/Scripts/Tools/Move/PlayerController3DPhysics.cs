using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Rigidbody-based 3D player controller with mouse-look, movement, sprint and jump.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [NeoDoc("Tools/Move/PlayerController3DPhysics.md")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PlayerController3DPhysics))]
    public class PlayerController3DPhysics : MonoBehaviour
    {
        private enum MovementReferenceMode
        {
            CharacterRotation,
            CameraYaw,
            WorldAxes
        }

        private enum LookYawMode
        {
            RotateCharacter,
            RotateCameraPivot,
            RotateBoth
        }

        private enum InputBackend
        {
            AutoPreferNew,
            NewInputSystem,
            LegacyInputManager
        }

        [Header("References")] [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _groundCheck;

        [Header("Movement")] [SerializeField]
        private MovementReferenceMode _movementReference = MovementReferenceMode.CharacterRotation;

        [SerializeField] private float _movementYawOffset;
        [SerializeField] private float _walkSpeed = 4.5f;
        [SerializeField] private float _runSpeed = 7.5f;
        [SerializeField] private float _groundAcceleration = 45f;
        [SerializeField] private float _airAcceleration = 18f;
        [SerializeField] private float _jumpImpulse = 6.5f;
        [SerializeField] private float _extraGravityMultiplier = 1.6f;
        [SerializeField] private bool _canRun = true;
        [SerializeField] private bool _canJump = true;

        [Header("Grounding")] [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _groundCheckRadius = 0.25f;
        [SerializeField] private float _groundProbeOffset = 0.05f;
        [SerializeField] private float _coyoteTime = 0.1f;
        [SerializeField] private float _jumpBufferTime = 0.1f;

        [Header("Look")] [SerializeField] private bool _canLook = true;
        [SerializeField] private LookYawMode _lookYawMode = LookYawMode.RotateCharacter;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _minPitch = -80f;
        [SerializeField] private float _maxPitch = 80f;

        [Header("Input")] [SerializeField] private InputBackend _inputBackend = InputBackend.AutoPreferNew;
        [SerializeField] private string _horizontalAxis = "Horizontal";
        [SerializeField] private string _verticalAxis = "Vertical";
        [SerializeField] private string _mouseXAxis = "Mouse X";
        [SerializeField] private string _mouseYAxis = "Mouse Y";
        [SerializeField] private string _jumpButton = "Jump";
        [SerializeField] private KeyCode _runKey = KeyCode.LeftShift;
        [SerializeField] private float _newLookDeltaScale = 0.02f;

        [Header("Cursor")] [SerializeField] private bool _lockCursorOnStart = true;

        [Header("Events")] [SerializeField] private UnityEvent _onJumped = new();
        [SerializeField] private UnityEvent _onLanded = new();

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _pitch;
        private float _yaw;
        private float _jumpBufferTimer;
        private float _coyoteTimer;
        private bool _movementEnabled = true;
        private bool _lookEnabled = true;
        private bool _isGrounded;
        private bool _wasGrounded;
        private readonly Collider[] _groundHits = new Collider[16];
        private bool _newInputUnavailableWarningShown;

        /// <summary>
        ///     Gets whether the character is currently grounded.
        /// </summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>
        ///     Gets whether sprint input is active.
        /// </summary>
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_cameraPivot == null && Camera.main != null)
            {
                _cameraPivot = Camera.main.transform;
            }

            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.freezeRotation = true;
            _pitch = _cameraPivot != null ? NormalizePitch(_cameraPivot.localEulerAngles.x) : 0f;
            _yaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            if (_lockCursorOnStart)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            CaptureInput();
            HandleLookInput();
        }

        private void FixedUpdate()
        {
            UpdateGroundState();
            UpdateJumpTimers(Time.fixedDeltaTime);
            ApplyLookRotation();
            HandleMovement(Time.fixedDeltaTime);
            HandleJump();
            ApplyExtraGravity();
        }

        /// <summary>
        ///     Enables or disables movement processing.
        /// </summary>
        /// <param name="enabled">True to process movement; otherwise false.</param>
        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled)
            {
                _moveInput = Vector2.zero;
                IsRunning = false;
            }
        }

        /// <summary>
        ///     Enables or disables look processing.
        /// </summary>
        /// <param name="enabled">True to process look input; otherwise false.</param>
        public void SetLookEnabled(bool enabled)
        {
            _lookEnabled = enabled;
            if (!enabled)
            {
                _lookInput = Vector2.zero;
            }
        }

        /// <summary>
        ///     Locks or unlocks the cursor and updates visibility accordingly.
        /// </summary>
        /// <param name="locked">True to lock and hide cursor; false to unlock and show.</param>
        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>
        ///     Teleports the character while preserving current rotation.
        /// </summary>
        /// <param name="worldPosition">Destination world position.</param>
        public void Teleport(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void CaptureInput()
        {
            if (_movementEnabled)
            {
                _moveInput = ReadMoveInput();
                _moveInput = Vector2.ClampMagnitude(_moveInput, 1f);
                IsRunning = _canRun && ReadRunHeld() && _moveInput.sqrMagnitude > 0.001f;
            }
            else
            {
                _moveInput = Vector2.zero;
                IsRunning = false;
            }

            if (_lookEnabled)
            {
                _lookInput = ReadLookInput();
            }
            else
            {
                _lookInput = Vector2.zero;
            }

            if (_canJump && _movementEnabled && ReadJumpPressed())
            {
                _jumpBufferTimer = _jumpBufferTime;
            }
        }

        private void UpdateGroundState()
        {
            _wasGrounded = _isGrounded;
            Vector3 probe = (_groundCheck != null ? _groundCheck.position : transform.position) +
                            Vector3.down * _groundProbeOffset;
            int hitCount = Physics.OverlapSphereNonAlloc(probe, _groundCheckRadius, _groundHits, _groundMask,
                QueryTriggerInteraction.Ignore);
            bool grounded = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _groundHits[i];
                if (hit == null)
                {
                    continue;
                }

                if (!hit.transform.IsChildOf(transform))
                {
                    grounded = true;
                    break;
                }
            }

            _isGrounded = grounded;

            if (_isGrounded)
            {
                _coyoteTimer = _coyoteTime;
            }

            if (!_wasGrounded && _isGrounded)
            {
                _onLanded?.Invoke();
            }
        }

        private void UpdateJumpTimers(float deltaTime)
        {
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - deltaTime);
            if (!_isGrounded)
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - deltaTime);
            }
        }

        private void HandleMovement(float deltaTime)
        {
            Quaternion basisRotation = GetMovementBasisRotation();
            Vector3 forward = basisRotation * Vector3.forward;
            Vector3 right = basisRotation * Vector3.right;

            Vector3 desiredDirection = (right * _moveInput.x + forward * _moveInput.y).normalized;
            float speed = IsRunning ? _runSpeed : _walkSpeed;
            Vector3 desiredVelocity = desiredDirection * speed;

            Vector3 currentHorizontalVelocity = new(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
            float acceleration = _isGrounded ? _groundAcceleration : _airAcceleration;
            Vector3 nextHorizontalVelocity =
                Vector3.MoveTowards(currentHorizontalVelocity, desiredVelocity, acceleration * deltaTime);

            _rigidbody.velocity =
                new Vector3(nextHorizontalVelocity.x, _rigidbody.velocity.y, nextHorizontalVelocity.z);
        }

        private void HandleJump()
        {
            if (!_canJump || _jumpBufferTimer <= 0f || _coyoteTimer <= 0f)
            {
                return;
            }

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            Vector3 velocity = _rigidbody.velocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            _rigidbody.velocity = velocity;
            _rigidbody.AddForce(Vector3.up * _jumpImpulse, ForceMode.Impulse);
            _isGrounded = false;
            _onJumped?.Invoke();
        }

        private void ApplyExtraGravity()
        {
            if (_isGrounded || _extraGravityMultiplier <= 1f)
            {
                return;
            }

            Vector3 extraGravity = Physics.gravity * (_extraGravityMultiplier - 1f);
            _rigidbody.AddForce(extraGravity, ForceMode.Acceleration);
        }

        private void HandleLookInput()
        {
            if (!_canLook || !_lookEnabled)
            {
                return;
            }

            float yawDelta = _lookInput.x * _mouseSensitivity;
            float pitchDelta = _lookInput.y * _mouseSensitivity;
            _yaw += yawDelta;
            _pitch -= pitchDelta;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        private void ApplyLookRotation()
        {
            if (!_canLook || !_lookEnabled)
            {
                return;
            }

            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            bool characterRotated = false;
            switch (_lookYawMode)
            {
                case LookYawMode.RotateCharacter:
                    _rigidbody.MoveRotation(yawRotation);
                    characterRotated = true;
                    break;
                case LookYawMode.RotateCameraPivot:
                    break;
                case LookYawMode.RotateBoth:
                    _rigidbody.MoveRotation(yawRotation);
                    characterRotated = true;
                    break;
            }

            if (_cameraPivot != null)
            {
                if (_lookYawMode == LookYawMode.RotateCameraPivot ||
                    (_lookYawMode == LookYawMode.RotateBoth && !characterRotated))
                {
                    _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                }
                else
                {
                    _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
                }
            }
        }

        private Quaternion GetMovementBasisRotation()
        {
            float baseYaw = 0f;
            switch (_movementReference)
            {
                case MovementReferenceMode.CharacterRotation:
                    baseYaw = transform.eulerAngles.y;
                    break;
                case MovementReferenceMode.CameraYaw:
                    baseYaw = _cameraPivot != null ? _cameraPivot.eulerAngles.y : transform.eulerAngles.y;
                    break;
                case MovementReferenceMode.WorldAxes:
                    baseYaw = 0f;
                    break;
            }

            return Quaternion.Euler(0f, baseYaw + _movementYawOffset, 0f);
        }

        private bool ShouldUseNewInput()
        {
            if (_inputBackend == InputBackend.LegacyInputManager)
            {
                return false;
            }

            if (OptionalInputSystemBridge.IsAvailable)
            {
                return true;
            }

            if ((_inputBackend == InputBackend.NewInputSystem || _inputBackend == InputBackend.AutoPreferNew) &&
                !_newInputUnavailableWarningShown)
            {
                Debug.LogWarning(
                    "[PlayerController3DPhysics] New Input System is not available. Falling back to Legacy Input Manager.");
                _newInputUnavailableWarningShown = true;
            }

            return false;
        }

        private Vector2 ReadMoveInput()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadMove();
            }

            return new Vector2(Input.GetAxisRaw(_horizontalAxis), Input.GetAxisRaw(_verticalAxis));
        }

        private Vector2 ReadLookInput()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadLookDelta(_newLookDeltaScale);
            }

            return new Vector2(Input.GetAxis(_mouseXAxis), Input.GetAxis(_mouseYAxis));
        }

        private bool ReadJumpPressed()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadJumpPressed();
            }

            return Input.GetButtonDown(_jumpButton);
        }

        private bool ReadRunHeld()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadRunHeld();
            }

            return Input.GetKey(_runKey);
        }

        private static float NormalizePitch(float angle)
        {
            float normalized = angle % 360f;
            if (normalized > 180f)
            {
                normalized -= 360f;
            }

            return normalized;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 probe = (_groundCheck != null ? _groundCheck.position : transform.position) +
                            Vector3.down * _groundProbeOffset;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(probe, _groundCheckRadius);
        }
    }
}
