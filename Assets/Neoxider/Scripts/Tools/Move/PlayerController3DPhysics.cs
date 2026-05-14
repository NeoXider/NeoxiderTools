using System;
using Neo.Settings;
using UnityEngine;
using UnityEngine.Events;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Rigidbody-based 3D player controller with mouse-look, movement, sprint and jump. Cursor lock / Escape can be
    ///     fully disabled via <see cref="CursorControlEnabled"/> when another system (e.g. <see cref="CursorLockController"/>)
    ///     owns the pointer.
    /// </summary>
    /// <remarks>
    ///     With Mirror, this type is a <see cref="Mirror.NetworkBehaviour"/> and expects a networked player prefab.
    ///     Add <see cref="Mirror.NetworkRigidbodyUnreliable"/> on the same GameObject yourself when you need replication;
    ///     typical settings: <c>syncDirection = ClientToServer</c>, <c>Coordinate Space = World</c> if needed.
    ///     Leave <c>NetworkRigidbodyUnreliable.useFixedUpdate</c> disabled on stock Mirror: when it is on, the rigidbody
    ///     component’s <c>FixedUpdate</c> shadows the transform’s pending snapshot apply, so remote proxies may not move.
    ///     <see cref="Awake"/> assigns <c>NetworkRigidbodyUnreliable.target</c> to this character's <see cref="Rigidbody"/> transform
    ///     so a wrong child target in the Inspector cannot break replication.
    ///     Uses <see cref="DefaultExecutionOrderAttribute"/> so this <c>Awake</c> runs before <c>NetworkRigidbodyUnreliable.Awake</c>,
    ///     which must see the correct target when caching the Rigidbody.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody))]
#if MIRROR
    [RequireComponent(typeof(NetworkIdentity))]
#endif
    [NeoDoc("Tools/Move/PlayerController3DPhysics.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/PlayerController3DPhysics",
        "Prefabs/Tools/First Person Controller.prefab")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(PlayerController3DPhysics))]
    public class PlayerController3DPhysics : 
#if MIRROR
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
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

        [Tooltip("Allow jump. Can be changed at runtime via SetJumpEnabled(bool).")] [SerializeField]
        private bool _canJump = true;

        [Header("Grounding")] [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _groundCheckRadius = 0.25f;
        [SerializeField] private float _groundProbeOffset = 0.05f;
        [SerializeField] private float _coyoteTime = 0.1f;
        [SerializeField] private float _jumpBufferTime = 0.1f;

        [Header("Look")] [SerializeField] private bool _canLook = true;
        [SerializeField] private LookYawMode _lookYawMode = LookYawMode.RotateCharacter;

        [Tooltip("When enabled, look speed uses GameSettings.MouseSensitivity (and live updates).")] [SerializeField]
        private bool _useGameSettingsMouseSensitivity = true;

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

        [Header("Cursor")]
        [Tooltip(
            "When off, this component never changes Cursor.lockState / Cursor.visible (no lock on Start, no Escape toggle, SetLookEnabled will not auto-lock, SetCursorLocked is a no-op). Use when CursorLockController or your UI owns the cursor. Default: on.")]
        [SerializeField]
        private bool _enableCursorControl = true;

        [Tooltip("Lock and hide cursor in Start() when Enable Cursor Control is on and no external CursorLockController.")]
        [SerializeField]
        private bool _lockCursorOnStart = true;

        [Tooltip(
            "When cursor is visible (unlocked), do not rotate camera. Enabled by default so that UI/menu doesn't cause look.")]
        [SerializeField]
        private bool _pauseLookWhenCursorVisible = true;

        [Tooltip(
            "Whether look (camera rotation) is enabled. Can be changed by SetLookEnabled() or automatically when game is paused (if Disable Look On Pause is on).")]
        [SerializeField]
        private bool _lookEnabled = true;

        [Tooltip("When enabled, look is set to false on EM.OnPause and true on EM.OnResume.")] [SerializeField]
        private bool _disableLookOnPause = true;

        [Tooltip(
            "When enabled, Escape toggles cursor lock and look: ESC with locked cursor = unlock and disable look; ESC with visible cursor = lock and enable look. Disable if you use CursorLockController for ESC to avoid double toggle.")]
        [SerializeField]
        private bool _toggleCursorOnEscape = true;

        [Tooltip(
            "Optional external cursor controller (for menu/pause pages or a shared UI root). If assigned and active, this controller becomes the authoritative cursor source instead of a same-object CursorLockController.")]
        [SerializeField]
        private CursorLockController _externalCursorLockController;

        [Header("Events")] [SerializeField] private UnityEvent _onJumped = new();
        [SerializeField] private UnityEvent _onLanded = new();
        [SerializeField] private UnityEvent _onMoveStart = new();
        [SerializeField] private UnityEvent _onMoveStop = new();
        private readonly Collider[] _groundHits = new Collider[16];
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _legacyInputUnavailableWarningShown;
        private Vector2 _lookInput;

        private Vector2 _moveInput;

        [Tooltip("Process walk/run and jump buffer input. Toggle via SetMovementEnabled(bool).")] [SerializeField]
        private bool _movementEnabled = true;

        private bool _newInputUnavailableWarningShown;
        private bool _kinematicMovementWarningShown;
        private float _pitch;
        private bool _wasGrounded;
        private bool _wasMoving;
        private float _yaw;

        // External input overrides (for on-screen joystick / touch controls)
        private Vector2? _externalMoveInput;
        private Vector2? _externalLookInput;
        private bool _externalJumpPressed;
        private bool _externalRunHeld;

        private bool HasInputAuthority
        {
            get
            {
#if MIRROR
                return isLocalPlayer;
#else
                return true;
#endif
            }
        }

        /// <summary>
        ///     Gets whether the character is currently grounded.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        ///     Gets whether sprint input is active.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        ///     Gets whether horizontal movement and sprint input are processed. Change via <see cref="SetMovementEnabled" />.
        /// </summary>
        public bool MovementEnabled => _movementEnabled;

        /// <summary>
        ///     Gets whether jump input and jump execution are allowed. Change via <see cref="SetJumpEnabled" />.
        /// </summary>
        public bool JumpEnabled => _canJump;

        /// <summary>
        ///     Gets whether look (camera rotation) is enabled. Change via SetLookEnabled(bool) or automatically by pause when
        ///     Disable Look On Pause is on.
        /// </summary>
        public bool LookEnabled => _lookEnabled;

        /// <summary>
        ///     When false, this controller does not modify <see cref="Cursor"/> (no automatic lock on Start, no Escape
        ///     toggle, <see cref="SetCursorLocked"/> and look auto-lock are skipped). Default is true.
        /// </summary>
        public bool CursorControlEnabled
        {
            get => _enableCursorControl;
            set => _enableCursorControl = value;
        }

        private bool IsLookActive => _lookEnabled && (!_pauseLookWhenCursorVisible || !Cursor.visible);

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

            if (_groundCheck == null)
            {
                Debug.LogWarning(
                    "[PlayerController3DPhysics] Ground Check transform is not set. Ground detection will use transform position.",
                    this);
            }

            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.freezeRotation = true;
            _pitch = _cameraPivot != null ? NormalizePitch(_cameraPivot.localEulerAngles.x) : 0f;
            _yaw = transform.eulerAngles.y;
            if (_externalCursorLockController == null)
            {
                _externalCursorLockController = GetComponent<CursorLockController>();
            }

#if MIRROR
            var netRb = GetComponent<NetworkRigidbodyUnreliable>();
            if (netRb != null && netRb.target != _rigidbody.transform)
            {
                netRb.target = _rigidbody.transform;
            }
#endif
        }

        private void Start()
        {
            EnsureLocalRigidbodyDynamic();

            if (HasInputAuthority && _enableCursorControl && _lockCursorOnStart && !HasExternalCursorControl())
            {
                SetCursorLocked(true);
            }
        }

#if MIRROR
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            EnsureLocalRigidbodyDynamic();
        }
#endif

        private void Update()
        {
            if (HasInputAuthority && _enableCursorControl && _toggleCursorOnEscape &&
                !HasExternalCursorControl() && ReadEscapePressed())
            {
                if (Cursor.visible)
                {
                    SetCursorLocked(true);
                    SetLookEnabled(true);
                }
                else
                {
                    SetCursorLocked(false);
                    SetLookEnabled(false);
                }
            }

            CaptureInput();
            if (HasInputAuthority)
            {
                HandleLookInput();
            }
            bool movingNow = _movementEnabled && _moveInput.sqrMagnitude > 0.001f;
            if (movingNow != _wasMoving)
            {
                _wasMoving = movingNow;
                if (movingNow)
                {
                    _onMoveStart?.Invoke();
                }
                else
                {
                    _onMoveStop?.Invoke();
                }
            }
        }

        private void FixedUpdate()
        {
            UpdateGroundState();
            if (!HasInputAuthority) return;

            UpdateJumpTimers(Time.fixedDeltaTime);
            ApplyLookRotation();
            HandleMovement(Time.fixedDeltaTime);
            HandleJump();
            ApplyExtraGravity();
        }

        private void OnEnable()
        {
            if (_useGameSettingsMouseSensitivity)
            {
                GameSettings.OnSettingsChanged += OnGameSettingsChanged;
            }

            if (_disableLookOnPause && EM.TryGetInstance(out EM eventManager))
            {
                eventManager.OnPause.AddListener(OnPauseLook);
                eventManager.OnResume.AddListener(OnResumeLook);
            }
        }

        private void OnDisable()
        {
            if (_useGameSettingsMouseSensitivity)
            {
                GameSettings.OnSettingsChanged -= OnGameSettingsChanged;
            }

            if (_disableLookOnPause && EM.TryGetInstance(out EM eventManager))
            {
                eventManager.OnPause.RemoveListener(OnPauseLook);
                eventManager.OnResume.RemoveListener(OnResumeLook);
            }
        }

        private void OnGameSettingsChanged()
        {
            if (_useGameSettingsMouseSensitivity)
            {
                _mouseSensitivity = GameSettings.MouseSensitivity;
            }
        }

        private float EffectiveMouseSensitivity =>
            _useGameSettingsMouseSensitivity ? GameSettings.MouseSensitivity : _mouseSensitivity;

        private void OnDrawGizmosSelected()
        {
            Vector3 probe = (_groundCheck != null ? _groundCheck.position : transform.position) +
                            Vector3.down * _groundProbeOffset;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(probe, _groundCheckRadius);
        }

        private void OnPauseLook()
        {
            SetLookEnabled(false);
        }

        private void OnResumeLook()
        {
            SetLookEnabled(true);
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
                _jumpBufferTimer = 0f;
            }
        }

        /// <summary>
        ///     Enables or disables jump (input buffering and applying impulse). Clears jump buffer when disabling.
        /// </summary>
        public void SetJumpEnabled(bool enabled)
        {
            _canJump = enabled;
            if (!enabled)
            {
                _jumpBufferTimer = 0f;
            }
        }

        /// <summary>
        ///     Enables or disables look processing. When enabling and Pause Look When Cursor Visible is on, also locks the
        ///     cursor if <see cref="CursorControlEnabled"/> is true. Callable from UnityEvent (dynamic bool).
        /// </summary>
        /// <param name="enabled">True to process look input; otherwise false.</param>
        public void SetLookEnabled(bool enabled)
        {
            _lookEnabled = enabled;
            if (_enableCursorControl && enabled && _pauseLookWhenCursorVisible && !HasExternalCursorControl())
            {
                SetCursorLocked(true);
            }

            if (!enabled)
            {
                _lookInput = Vector2.zero;
            }
        }

        /// <summary>
        ///     Locks or unlocks the cursor and updates visibility accordingly. No-op when <see cref="CursorControlEnabled" />
        ///     is false.
        /// </summary>
        /// <param name="locked">True to lock and hide cursor; false to unlock and show.</param>
        public void SetCursorLocked(bool locked)
        {
            if (!_enableCursorControl)
            {
                return;
            }

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private bool HasExternalCursorControl()
        {
            return _externalCursorLockController != null &&
                   _externalCursorLockController.enabled &&
                   _externalCursorLockController.ControllerEnabled;
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

        /// <summary>
        ///     Inject movement input from an external source (on-screen joystick, touch pad, etc.).
        ///     When set, the built-in keyboard/gamepad movement reading is bypassed.
        ///     Pass null to revert to built-in input.
        /// </summary>
        /// <param name="input">Movement vector (x = strafe, y = forward). Clamped to magnitude 1.</param>
        public void SetMoveInput(Vector2? input)
        {
            _externalMoveInput = input.HasValue ? Vector2.ClampMagnitude(input.Value, 1f) : (Vector2?)null;
        }

        /// <summary>
        ///     Inject look input from an external source (on-screen look pad, gyroscope, etc.).
        ///     When set, the built-in mouse/stick look reading is bypassed.
        ///     Pass null to revert to built-in input.
        /// </summary>
        /// <param name="input">Look delta (x = yaw, y = pitch).</param>
        public void SetLookInput(Vector2? input)
        {
            _externalLookInput = input;
        }

        /// <summary>
        ///     Inject a one-shot jump command from an external source (on-screen button).
        ///     Resets automatically after being consumed.
        /// </summary>
        public void SetJumpInput()
        {
            _externalJumpPressed = true;
        }

        /// <summary>
        ///     Inject run (sprint) state from an external source (on-screen toggle/button).
        /// </summary>
        /// <param name="held">True while running.</param>
        public void SetRunInput(bool held)
        {
            _externalRunHeld = held;
        }

        private void CaptureInput()
        {
            if (!HasInputAuthority)
            {
                _moveInput = Vector2.zero;
                IsRunning = false;
                _jumpBufferTimer = 0f;
                _lookInput = Vector2.zero;
                return;
            }

            if (_movementEnabled)
            {
                _moveInput = _externalMoveInput ?? ReadMoveInput();
                _moveInput = Vector2.ClampMagnitude(_moveInput, 1f);
                bool runHeld = _externalMoveInput.HasValue ? _externalRunHeld : ReadRunHeld();
                IsRunning = _canRun && runHeld && _moveInput.sqrMagnitude > 0.001f;
            }
            else
            {
                _moveInput = Vector2.zero;
                IsRunning = false;
            }

            if (IsLookActive)
            {
                _lookInput = _externalLookInput ?? ReadLookInput();
            }
            else
            {
                _lookInput = Vector2.zero;
            }

            bool jumpPressed = _externalJumpPressed || ReadJumpPressed();
            _externalJumpPressed = false;
            if (_canJump && _movementEnabled && jumpPressed)
            {
                _jumpBufferTimer = _jumpBufferTime;
            }
        }

        private void UpdateGroundState()
        {
            _wasGrounded = IsGrounded;
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

            IsGrounded = grounded;

            if (IsGrounded)
            {
                _coyoteTimer = _coyoteTime;
            }

            if (!_wasGrounded && IsGrounded)
            {
                _onLanded?.Invoke();
            }
        }

        private void UpdateJumpTimers(float deltaTime)
        {
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - deltaTime);
            if (!IsGrounded)
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - deltaTime);
            }
        }

        private void HandleMovement(float deltaTime)
        {
            if (_rigidbody.isKinematic)
            {
                WarnKinematicMovementOnce();
                return;
            }

            Quaternion basisRotation = GetMovementBasisRotation();
            Vector3 forward = basisRotation * Vector3.forward;
            Vector3 right = basisRotation * Vector3.right;

            Vector3 desiredDirection = (right * _moveInput.x + forward * _moveInput.y).normalized;
            float speed = IsRunning ? _runSpeed : _walkSpeed;
            Vector3 desiredVelocity = desiredDirection * speed;

            Vector3 currentHorizontalVelocity = new(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
            float acceleration = IsGrounded ? _groundAcceleration : _airAcceleration;
            var nextHorizontalVelocity =
                Vector3.MoveTowards(currentHorizontalVelocity, desiredVelocity, acceleration * deltaTime);

            _rigidbody.velocity =
                new Vector3(nextHorizontalVelocity.x, _rigidbody.velocity.y, nextHorizontalVelocity.z);
        }

        private void HandleJump()
        {
            if (_rigidbody.isKinematic)
            {
                WarnKinematicMovementOnce();
                return;
            }

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
            IsGrounded = false;
            _onJumped?.Invoke();
        }

        private void EnsureLocalRigidbodyDynamic()
        {
            if (!HasInputAuthority || _rigidbody == null || !_rigidbody.isKinematic)
            {
                return;
            }

            _rigidbody.isKinematic = false;
        }

        private void WarnKinematicMovementOnce()
        {
            if (_kinematicMovementWarningShown)
            {
                return;
            }

            _kinematicMovementWarningShown = true;
            Debug.LogWarning(
                "[PlayerController3DPhysics] Rigidbody is kinematic, so velocity-based movement is skipped. Disable Is Kinematic for the local player.",
                this);
        }

        private void ApplyExtraGravity()
        {
            if (_rigidbody.isKinematic)
            {
                return;
            }

            if (IsGrounded || _extraGravityMultiplier <= 1f)
            {
                return;
            }

            Vector3 extraGravity = Physics.gravity * (_extraGravityMultiplier - 1f);
            _rigidbody.AddForce(extraGravity, ForceMode.Acceleration);
        }

        private void HandleLookInput()
        {
            if (!_canLook || !IsLookActive)
            {
                return;
            }

            float sens = EffectiveMouseSensitivity;
            float yawDelta = _lookInput.x * sens;
            float pitchDelta = _lookInput.y * sens;
            _yaw += yawDelta;
            _pitch -= pitchDelta;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        private void ApplyLookRotation()
        {
            if (!_canLook || !IsLookActive)
            {
                return;
            }

            var yawRotation = Quaternion.Euler(0f, _yaw, 0f);
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

            if (!IsLegacyInputAvailable())
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

        private static bool IsLegacyInputAvailable()
        {
            try
            {
                Vector3 _ = Input.mousePosition;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private bool ReadEscapePressed()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadKeyDown(KeyCode.Escape);
            }

            try
            {
                return Input.GetKeyDown(KeyCode.Escape);
            }
            catch (InvalidOperationException)
            {
                WarnLegacyInputUnavailable();
                return OptionalInputSystemBridge.ReadKeyDown(KeyCode.Escape);
            }
        }

        private Vector2 ReadMoveInput()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadMove();
            }

            try
            {
                return new Vector2(Input.GetAxisRaw(_horizontalAxis), Input.GetAxisRaw(_verticalAxis));
            }
            catch (InvalidOperationException)
            {
                WarnLegacyInputUnavailable();
                return OptionalInputSystemBridge.IsAvailable ? OptionalInputSystemBridge.ReadMove() : Vector2.zero;
            }
        }

        private Vector2 ReadLookInput()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadLookDelta(_newLookDeltaScale);
            }

            try
            {
                return new Vector2(Input.GetAxis(_mouseXAxis), Input.GetAxis(_mouseYAxis));
            }
            catch (InvalidOperationException)
            {
                WarnLegacyInputUnavailable();
                return OptionalInputSystemBridge.IsAvailable
                    ? OptionalInputSystemBridge.ReadLookDelta(_newLookDeltaScale)
                    : Vector2.zero;
            }
        }

        private bool ReadJumpPressed()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadJumpPressed();
            }

            try
            {
                return Input.GetButtonDown(_jumpButton);
            }
            catch (InvalidOperationException)
            {
                WarnLegacyInputUnavailable();
                return OptionalInputSystemBridge.ReadJumpPressed();
            }
        }

        private bool ReadRunHeld()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadRunHeld();
            }

            try
            {
                return Input.GetKey(_runKey);
            }
            catch (InvalidOperationException)
            {
                WarnLegacyInputUnavailable();
                return OptionalInputSystemBridge.ReadRunHeld() || OptionalInputSystemBridge.ReadKeyHeld(_runKey);
            }
        }

        private void WarnLegacyInputUnavailable()
        {
            if (_legacyInputUnavailableWarningShown)
            {
                return;
            }

            Debug.LogWarning(
                "[PlayerController3DPhysics] Legacy Input Manager is unavailable in current Player Settings. Falling back to New Input System where possible.",
                this);
            _legacyInputUnavailableWarningShown = true;
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
    }
}
