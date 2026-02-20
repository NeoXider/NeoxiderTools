using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Rigidbody2D-based side-scroller controller with run, jump and optional camera follow.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [NeoDoc("Tools/Move/PlayerController2DPhysics.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/PlayerController2DPhysics")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PlayerController2DPhysics))]
    public class PlayerController2DPhysics : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Rigidbody2D _rigidbody;

        [SerializeField] private Transform _groundCheck;
        [SerializeField] private Camera _followCamera;

        [Header("Movement")] [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;
        [SerializeField] private float _acceleration = 55f;
        [SerializeField] private float _deceleration = 70f;
        [SerializeField] private bool _canRun = true;
        [SerializeField] private bool _flipByVelocityX = true;

        [Header("Jump")] [SerializeField] private bool _canJump = true;
        [SerializeField] private float _jumpImpulse = 10f;
        [SerializeField] private float _coyoteTime = 0.1f;
        [SerializeField] private float _jumpBufferTime = 0.1f;

        [Header("Grounding")] [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _groundCheckRadius = 0.2f;

        [Header("Camera Follow")] [SerializeField]
        private bool _followCameraEnabled = true;

        [SerializeField] private Vector3 _cameraOffset = new(0f, 1.5f, -10f);
        [SerializeField] private float _cameraFollowSpeed = 10f;

        [Header("Input")] [SerializeField] private InputBackend _inputBackend = InputBackend.AutoPreferNew;
        [SerializeField] private string _horizontalAxis = "Horizontal";
        [SerializeField] private string _jumpButton = "Jump";
        [SerializeField] private KeyCode _runKey = KeyCode.LeftShift;

        [Header("Events")]
        [SerializeField] private UnityEvent _onJumped = new();
        [SerializeField] private UnityEvent _onLanded = new();
        [SerializeField] private UnityEvent _onMoveStart = new();
        [SerializeField] private UnityEvent _onMoveStop = new();
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpPressedThisFrame;

        private float _moveInputX;
        private bool _movementEnabled = true;
        private bool _wasMoving;
        private bool _newInputUnavailableWarningShown;
        private bool _wasGrounded;

        /// <summary>
        ///     Gets whether the character is currently grounded.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        ///     Gets whether sprint input is active.
        /// </summary>
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();
            if (_followCamera == null)
                _followCamera = Camera.main;
            if (_groundCheck == null)
                Debug.LogWarning("[PlayerController2DPhysics] Ground Check transform is not set. Ground detection will use transform position.", this);
        }

        private void Update()
        {
            CaptureInput();
            UpdateGroundState();
            UpdateJumpTimers();
            TryConsumeJump();
            bool movingNow = _movementEnabled && Mathf.Abs(_moveInputX) > 0.01f;
            if (movingNow != _wasMoving)
            {
                _wasMoving = movingNow;
                if (movingNow)
                    _onMoveStart?.Invoke();
                else
                    _onMoveStop?.Invoke();
            }
        }

        private void FixedUpdate()
        {
            HandleMovement(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            UpdateCameraFollow(Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 checkPos = _groundCheck != null
                ? _groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.5f;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(checkPos, _groundCheckRadius);
        }

        /// <summary>
        ///     Enables or disables movement processing.
        /// </summary>
        /// <param name="enabled">True to process input and movement; otherwise false.</param>
        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled)
            {
                _moveInputX = 0f;
                IsRunning = false;
            }
        }

        /// <summary>
        ///     Enables or disables camera follow behavior.
        /// </summary>
        /// <param name="enabled">True to follow the player with camera; otherwise false.</param>
        public void SetCameraFollowEnabled(bool enabled)
        {
            _followCameraEnabled = enabled;
        }

        /// <summary>
        ///     Teleports the character and optionally resets current velocity.
        /// </summary>
        /// <param name="worldPosition">Destination world position.</param>
        /// <param name="resetVelocity">When true velocity is reset.</param>
        public void Teleport(Vector3 worldPosition, bool resetVelocity = true)
        {
            transform.position = worldPosition;
            if (resetVelocity)
            {
                _rigidbody.velocity = Vector2.zero;
            }
        }

        private void CaptureInput()
        {
            if (_movementEnabled)
            {
                _moveInputX = ReadMoveXInput();
                IsRunning = _canRun && ReadRunHeld() && Mathf.Abs(_moveInputX) > 0.01f;
                _jumpPressedThisFrame = _canJump && ReadJumpPressed();
            }
            else
            {
                _moveInputX = 0f;
                IsRunning = false;
                _jumpPressedThisFrame = false;
            }
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
                    "[PlayerController2DPhysics] New Input System is not available. Falling back to Legacy Input Manager.");
                _newInputUnavailableWarningShown = true;
            }

            return false;
        }

        private float ReadMoveXInput()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadMove().x;
            }

            return Input.GetAxisRaw(_horizontalAxis);
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

        private void UpdateGroundState()
        {
            _wasGrounded = IsGrounded;
            Vector2 checkPos = _groundCheck != null
                ? _groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.5f;
            IsGrounded = Physics2D.OverlapCircle(checkPos, _groundCheckRadius, _groundMask);
            if (!_wasGrounded && IsGrounded)
            {
                _onLanded?.Invoke();
            }
        }

        private void UpdateJumpTimers()
        {
            if (IsGrounded)
            {
                _coyoteTimer = _coyoteTime;
            }
            else
            {
                _coyoteTimer -= Time.deltaTime;
            }

            if (_jumpPressedThisFrame)
            {
                _jumpBufferTimer = _jumpBufferTime;
            }
            else
            {
                _jumpBufferTimer -= Time.deltaTime;
            }
        }

        private void TryConsumeJump()
        {
            if (!_canJump)
            {
                return;
            }

            if (_coyoteTimer <= 0f || _jumpBufferTimer <= 0f)
            {
                return;
            }

            Vector2 velocity = _rigidbody.velocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            _rigidbody.velocity = velocity;
            _rigidbody.AddForce(Vector2.up * _jumpImpulse, ForceMode2D.Impulse);
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _onJumped?.Invoke();
        }

        private void HandleMovement(float deltaTime)
        {
            float maxSpeed = IsRunning ? _runSpeed : _walkSpeed;
            float targetSpeedX = _moveInputX * maxSpeed;

            float currentSpeedX = _rigidbody.velocity.x;
            float acceleration = Mathf.Abs(_moveInputX) > 0.01f ? _acceleration : _deceleration;
            float nextSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeedX, acceleration * deltaTime);

            _rigidbody.velocity = new Vector2(nextSpeedX, _rigidbody.velocity.y);

            if (_flipByVelocityX && Mathf.Abs(nextSpeedX) > 0.01f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(nextSpeedX);
                transform.localScale = scale;
            }
        }

        private void UpdateCameraFollow(float deltaTime)
        {
            if (!_followCameraEnabled || _followCamera == null)
            {
                return;
            }

            Vector3 target = transform.position + _cameraOffset;
            _followCamera.transform.position = Vector3.Lerp(_followCamera.transform.position, target,
                1f - Mathf.Exp(-_cameraFollowSpeed * deltaTime));
        }

        private enum InputBackend
        {
            AutoPreferNew,
            NewInputSystem,
            LegacyInputManager
        }
    }
}