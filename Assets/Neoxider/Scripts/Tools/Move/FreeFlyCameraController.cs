using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Scene-view style free-flight controller for cameras and debug objects.
    ///     By default look and movement are active only while the right mouse button is held.
    /// </summary>
    [NeoDoc("Tools/Move/FreeFlyCameraController.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/FreeFlyCameraController")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(FreeFlyCameraController))]
    public class FreeFlyCameraController : MonoBehaviour
    {
        public enum InputBackend
        {
            LegacyInputManager,
            NewInputSystem,
            AutoPreferNew
        }

        public enum MovementSpace
        {
            Local,
            World
        }

        [Header("Mode")] [SerializeField] private bool _controllerEnabled = true;

        [Tooltip("Scene View style: look is active only while the configured mouse button is held.")] [SerializeField]
        private bool _requireLookButton = true;

        [Tooltip("0 = left, 1 = right, 2 = middle. Default is right mouse button.")] [SerializeField]
        private int _lookMouseButton = 1;

        [Tooltip("When enabled, keyboard movement is ignored until look mode is active.")] [SerializeField]
        private bool _moveOnlyWhileLooking = true;

        [Tooltip("Lock and hide cursor while look mode is active. The previous cursor state is restored on exit.")]
        [SerializeField]
        private bool _lockCursorWhileLooking = true;

        [Header("Input")] [SerializeField] private InputBackend _inputBackend = InputBackend.AutoPreferNew;
        [SerializeField] private KeyCode _forwardKey = KeyCode.W;
        [SerializeField] private KeyCode _backKey = KeyCode.S;
        [SerializeField] private KeyCode _leftKey = KeyCode.A;
        [SerializeField] private KeyCode _rightKey = KeyCode.D;
        [SerializeField] private KeyCode _upKey = KeyCode.E;
        [SerializeField] private KeyCode _downKey = KeyCode.Q;
        [SerializeField] private KeyCode _fastKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode _slowKey = KeyCode.LeftAlt;
        [SerializeField] private string _mouseXAxis = "Mouse X";
        [SerializeField] private string _mouseYAxis = "Mouse Y";
        [SerializeField] private float _newLookDeltaScale = 0.02f;

        [Tooltip("When enabled, logs one-time warnings if the selected input backend must fall back.")] [SerializeField]
        private bool _logInputFallbackWarnings;

        [Header("Movement")] [SerializeField] private MovementSpace _movementSpace = MovementSpace.Local;
        [SerializeField] private float _baseSpeed = 8f;
        [SerializeField] private float _fastMultiplier = 4f;
        [SerializeField] private float _slowMultiplier = 0.25f;
        [SerializeField] private bool _normalizeDiagonalMovement = true;
        [SerializeField] private bool _useUnscaledTime = true;

        [Tooltip("Mouse wheel changes Base Speed while this component is enabled.")] [SerializeField]
        private bool _allowMouseWheelSpeed = true;

        [SerializeField] private float _mouseWheelSpeedStep = 2f;
        [SerializeField] private float _minBaseSpeed = 0.05f;
        [SerializeField] private float _maxBaseSpeed = 100f;

        [Header("Look")] [SerializeField] private float _lookSensitivity = 2f;
        [SerializeField] private bool _invertY;
        [SerializeField] private float _minPitch = -89f;
        [SerializeField] private float _maxPitch = 89f;

        [Header("Events")] [SerializeField] private UnityEvent _onLookStart = new();
        [SerializeField] private UnityEvent _onLookStop = new();
        [SerializeField] private UnityEvent _onFlyStart = new();
        [SerializeField] private UnityEvent _onFlyStop = new();

        private bool _legacyInputUnavailableWarningShown;
        private bool _newInputUnavailableWarningShown;
        private bool _isLooking;
        private bool _isFlying;
        private bool _hasCursorSnapshot;
        private CursorLockMode _previousLockState;
        private bool _previousCursorVisible;
        private float _yaw;
        private float _pitch;
        private Vector3? _externalMoveInput;
        private Vector2? _externalLookInput;

        public bool ControllerEnabled => _controllerEnabled;
        public bool RequireLookButton => _requireLookButton;
        public bool MoveOnlyWhileLooking => _moveOnlyWhileLooking;
        public bool IsLooking => _isLooking;
        public bool IsFlying => _isFlying;
        public float BaseSpeed => _baseSpeed;

        private void Awake()
        {
            CacheAnglesFromTransform();
            ClampSettings();
        }

        private void OnDisable()
        {
            SetLooking(false);
            SetFlying(false);
        }

        private void OnValidate()
        {
            ClampSettings();
        }

        private void Update()
        {
            Tick(_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>
        ///     Advances movement/look using configured input. Exposed for deterministic tests and manual driving.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_controllerEnabled || deltaTime <= 0f)
            {
                SetLooking(false);
                SetFlying(false);
                return;
            }

            bool lookActive = !_requireLookButton || ReadMouseButton(_lookMouseButton);
            SetLooking(lookActive);
            ApplyMouseWheelSpeed();

            Vector2 lookInput = lookActive ? _externalLookInput ?? ReadLookInput() : Vector2.zero;
            if (lookInput.sqrMagnitude > 0.000001f)
            {
                ApplyLookDelta(lookInput);
            }

            bool movementAllowed = !_moveOnlyWhileLooking || lookActive;
            Vector3 moveInput = movementAllowed ? _externalMoveInput ?? ReadMoveInput() : Vector3.zero;
            Vector3 worldDelta = BuildWorldMoveDelta(moveInput, ReadSpeedMultiplier(), deltaTime);
            SetFlying(worldDelta.sqrMagnitude > 0.000001f);

            if (worldDelta.sqrMagnitude > 0.000001f)
            {
                transform.position += worldDelta;
            }
        }

        public void SetControllerEnabled(bool enabled)
        {
            _controllerEnabled = enabled;
            if (!enabled)
            {
                SetLooking(false);
                SetFlying(false);
            }
        }

        public void SetRequireLookButton(bool requireLookButton)
        {
            _requireLookButton = requireLookButton;
            if (requireLookButton && !ReadMouseButton(_lookMouseButton))
            {
                SetLooking(false);
            }
        }

        public void SetMoveOnlyWhileLooking(bool moveOnlyWhileLooking)
        {
            _moveOnlyWhileLooking = moveOnlyWhileLooking;
        }

        public void SetBaseSpeed(float speed)
        {
            _baseSpeed = Mathf.Clamp(speed, _minBaseSpeed, _maxBaseSpeed);
        }

        public void SetExternalMoveInput(Vector3? input)
        {
            _externalMoveInput = input;
        }

        public void SetExternalLookInput(Vector2? input)
        {
            _externalLookInput = input;
        }

        public void ClearExternalInput()
        {
            _externalMoveInput = null;
            _externalLookInput = null;
        }

        public void SetRotationAngles(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        public void Warp(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            CacheAnglesFromTransform();
        }

        public Vector3 BuildWorldMoveDelta(Vector3 localInput, float speedMultiplier, float deltaTime)
        {
            Vector3 input = _normalizeDiagonalMovement && localInput.sqrMagnitude > 1f
                ? localInput.normalized
                : localInput;
            Vector3 direction = _movementSpace == MovementSpace.Local ? transform.TransformDirection(input) : input;
            return direction * (_baseSpeed * Mathf.Max(0f, speedMultiplier) * Mathf.Max(0f, deltaTime));
        }

        private void CacheAnglesFromTransform()
        {
            Vector3 euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = NormalizeAngle(euler.x);
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        private void ApplyLookDelta(Vector2 lookInput)
        {
            float pitchSign = _invertY ? 1f : -1f;
            _yaw += lookInput.x * _lookSensitivity;
            _pitch += lookInput.y * _lookSensitivity * pitchSign;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private Vector3 ReadMoveInput()
        {
            float x = ReadKeyAxis(_rightKey, _leftKey);
            float y = ReadKeyAxis(_upKey, _downKey);
            float z = ReadKeyAxis(_forwardKey, _backKey);
            return new Vector3(x, y, z);
        }

        private static float ReadKeyAxis(KeyCode positive, KeyCode negative)
        {
            float value = 0f;
            if (positive != KeyCode.None && KeyInputCompat.GetKey(positive))
            {
                value += 1f;
            }

            if (negative != KeyCode.None && KeyInputCompat.GetKey(negative))
            {
                value -= 1f;
            }

            return value;
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

        private float ReadSpeedMultiplier()
        {
            if (_fastKey != KeyCode.None && KeyInputCompat.GetKey(_fastKey))
            {
                return _fastMultiplier;
            }

            if (_slowKey != KeyCode.None && KeyInputCompat.GetKey(_slowKey))
            {
                return _slowMultiplier;
            }

            return 1f;
        }

        private bool ReadMouseButton(int button)
        {
            return MouseInputCompat.TryGetButton(Mathf.Max(0, button), out bool pressed) && pressed;
        }

        private void ApplyMouseWheelSpeed()
        {
            if (!_allowMouseWheelSpeed)
            {
                return;
            }

            float scroll = ReadMouseScroll();
            if (Mathf.Abs(scroll) <= 0.0001f)
            {
                return;
            }

            SetBaseSpeed(_baseSpeed + scroll * _mouseWheelSpeedStep);
        }

        private float ReadMouseScroll()
        {
            try
            {
                return Input.mouseScrollDelta.y;
            }
            catch (InvalidOperationException)
            {
                return 0f;
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

            if (!IsLegacyInputAvailable())
            {
                return true;
            }

            if ((_inputBackend == InputBackend.NewInputSystem || _inputBackend == InputBackend.AutoPreferNew) &&
                _logInputFallbackWarnings &&
                !_newInputUnavailableWarningShown)
            {
                NeoDiagnostics.LogWarning(
                    "[FreeFlyCameraController] New Input System is not available. Falling back to Legacy Input Manager.",
                    this);
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

        private void WarnLegacyInputUnavailable()
        {
            if (!_logInputFallbackWarnings || _legacyInputUnavailableWarningShown)
            {
                return;
            }

            NeoDiagnostics.LogWarning(
                "[FreeFlyCameraController] Legacy Input Manager is not available. Falling back to New Input System when possible.",
                this);
            _legacyInputUnavailableWarningShown = true;
        }

        private void SetLooking(bool looking)
        {
            if (_isLooking == looking)
            {
                return;
            }

            _isLooking = looking;
            if (looking)
            {
                CaptureCursorSnapshot();
                if (_lockCursorWhileLooking)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                _onLookStart?.Invoke();
            }
            else
            {
                RestoreCursorSnapshot();
                _onLookStop?.Invoke();
            }
        }

        private void SetFlying(bool flying)
        {
            if (_isFlying == flying)
            {
                return;
            }

            _isFlying = flying;
            if (flying)
            {
                _onFlyStart?.Invoke();
            }
            else
            {
                _onFlyStop?.Invoke();
            }
        }

        private void CaptureCursorSnapshot()
        {
            if (!_lockCursorWhileLooking || _hasCursorSnapshot)
            {
                return;
            }

            _previousLockState = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _hasCursorSnapshot = true;
        }

        private void RestoreCursorSnapshot()
        {
            if (!_lockCursorWhileLooking || !_hasCursorSnapshot)
            {
                return;
            }

            Cursor.lockState = _previousLockState;
            Cursor.visible = _previousCursorVisible;
            _hasCursorSnapshot = false;
        }

        private void ClampSettings()
        {
            _lookMouseButton = Mathf.Max(0, _lookMouseButton);
            _baseSpeed = Mathf.Max(0f, _baseSpeed);
            _fastMultiplier = Mathf.Max(0f, _fastMultiplier);
            _slowMultiplier = Mathf.Max(0f, _slowMultiplier);
            _newLookDeltaScale = Mathf.Max(0f, _newLookDeltaScale);
            _lookSensitivity = Mathf.Max(0f, _lookSensitivity);
            _minBaseSpeed = Mathf.Max(0f, _minBaseSpeed);
            _maxBaseSpeed = Mathf.Max(_minBaseSpeed, _maxBaseSpeed);
            if (_minPitch > _maxPitch)
            {
                (_minPitch, _maxPitch) = (_maxPitch, _minPitch);
            }
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }
    }
}
