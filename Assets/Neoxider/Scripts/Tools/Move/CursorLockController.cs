using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Controls cursor visibility and lock state. Supports optional apply on Enable/Disable
    ///     and runtime toggle key. For New Input System, call SetCursorLocked or ToggleCursorState from your action's
    ///     callback.
    /// </summary>
    [NeoDoc("Tools/Move/CursorLockController.md")]
    [CreateFromMenu("Neoxider/Tools/Movement/CursorLockController")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(CursorLockController))]
    public class CursorLockController : MonoBehaviour
    {
        public enum ControlMode
        {
            AutomaticAndManual,
            AutomaticOnly,
            ManualOnly
        }

        public enum CursorAccessKeyMode
        {
            HoldToShowCursor,
            ToggleShowCursor
        }

        public enum CursorStateMode
        {
            LockAndHide,
            OnlyHide,
            OnlyLock
        }

        private static readonly List<CursorLockController> ActiveControllers = new();

        [Header("Mode")]
        [Tooltip("LockAndHide = lock + hide; OnlyHide = visibility only; OnlyLock = lock only.")]
        [SerializeField]
        private CursorStateMode _mode = CursorStateMode.LockAndHide;

        [Tooltip(
            "Automatic = Start/OnEnable/OnDisable/hotkey. Manual = direct method calls. By default both are allowed.")]
        [SerializeField]
        private ControlMode _controlMode = ControlMode.AutomaticAndManual;

        [Header("Controller")]
        [Tooltip(
            "Master switch. When disabled, this component does not react to toggle input or lifecycle state changes.")]
        [SerializeField]
        private bool _controllerEnabled = true;

        [Header("Start State")] [SerializeField]
        private bool _lockOnStart = true;

        [SerializeField] [Tooltip("Apply start state only when Controller Enabled is true.")]
        private bool _applyStartOnlyWhenControllerEnabled = true;

        [Header("Lifecycle (optional)")]
        [Tooltip("Change Controller Enabled when this GameObject becomes enabled.")]
        [SerializeField]
        private bool _setControllerEnabledOnEnable;

        [SerializeField] private bool _controllerEnabledOnEnable = true;

        [Tooltip("Apply cursor state when this component is enabled (e.g. when returning to gameplay).")]
        [SerializeField]
        private bool _applyOnEnable;

        [SerializeField] private bool _lockOnEnable = true;

        [Tooltip("Change Controller Enabled when this GameObject becomes disabled.")] [SerializeField]
        private bool _setControllerEnabledOnDisable;

        [SerializeField] private bool _controllerEnabledOnDisable;

        [Tooltip("Apply cursor state when this component is disabled (e.g. when opening menu/pause).")] [SerializeField]
        private bool _applyOnDisable;

        [SerializeField] private bool _lockOnDisable;

        [Header("Toggle")] [SerializeField] private bool _allowToggle = true;

        [SerializeField] private KeyCode _toggleKey = KeyCode.Escape;

        [Header("Cursor Access Key")]
        [Tooltip(
            "Optional shortcut for temporary cursor access. Disabled by default until Allow Cursor Access Key is enabled.")]
        [SerializeField]
        private bool _allowCursorAccessKey;

        [SerializeField] private KeyCode _cursorAccessKey = KeyCode.Z;
        [SerializeField] private CursorAccessKeyMode _cursorAccessKeyMode = CursorAccessKeyMode.HoldToShowCursor;

        [Header("Events")] [SerializeField] private UnityEvent _onCursorLocked = new();

        [SerializeField] private UnityEvent _onCursorUnlocked = new();
        private bool _cursorAccessActive;
        private bool _cursorAccessHadOwnership;
        private bool _cursorAccessPreviousLocked;
        private bool _requestedLocked;

        /// <summary>
        ///     Gets whether cursor is currently locked.
        /// </summary>
        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        public bool ControllerEnabled => _controllerEnabled;
        public bool HasCursorOwnership { get; private set; }

        public ControlMode Mode => _controlMode;

        private bool SupportsAutomatic => _controlMode != ControlMode.ManualOnly;
        private bool SupportsManual => _controlMode != ControlMode.AutomaticOnly;

        private void Start()
        {
            if (!SupportsAutomatic)
            {
                return;
            }

            if (_applyStartOnlyWhenControllerEnabled && !_controllerEnabled)
            {
                return;
            }

            AcquireCursorControl(_lockOnStart);
        }

        private void Update()
        {
            if (!SupportsAutomatic || !_controllerEnabled)
            {
                return;
            }

            if (_allowToggle && KeyInputCompat.GetKeyDown(_toggleKey))
            {
                ToggleCursorState();
            }

            HandleCursorAccessKey();
        }

        private void OnEnable()
        {
            if (_setControllerEnabledOnEnable)
            {
                _controllerEnabled = _controllerEnabledOnEnable;
            }

            if (SupportsAutomatic && _controllerEnabled && _applyOnEnable)
            {
                AcquireCursorControl(_lockOnEnable);
            }
        }

        private void OnDisable()
        {
            bool applyDisableState = SupportsAutomatic && _controllerEnabled && _applyOnDisable;
            bool lockOnDisable = _lockOnDisable;

            ReleaseControl();

            if (applyDisableState && GetTopController() == null)
            {
                ApplyCursorState(lockOnDisable);
            }

            if (_setControllerEnabledOnDisable)
            {
                _controllerEnabled = _controllerEnabledOnDisable;
            }
        }

        /// <summary>
        ///     Applies cursor state according to Mode: lock and/or visibility.
        /// </summary>
        /// <param name="locked">True = apply "locked" state (lock and/or hide by mode), false = apply "unlocked" state.</param>
        public void SetCursorLocked(bool locked)
        {
            if (!SupportsManual)
            {
                return;
            }

            AcquireCursorControl(locked);
        }

        private void ApplyCursorState(bool locked)
        {
            switch (_mode)
            {
                case CursorStateMode.LockAndHide:
                    Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
                    Cursor.visible = !locked;
                    break;
                case CursorStateMode.OnlyHide:
                    Cursor.visible = !locked;
                    break;
                case CursorStateMode.OnlyLock:
                    Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
                    break;
            }

            if (locked)
            {
                _onCursorLocked?.Invoke();
            }
            else
            {
                _onCursorUnlocked?.Invoke();
            }
        }

        /// <summary>
        ///     Toggles cursor lock state.
        /// </summary>
        public void ToggleCursorState()
        {
            if (!(SupportsManual || SupportsAutomatic) || !_controllerEnabled)
            {
                return;
            }

            bool currentLocked = HasCursorOwnership ? _requestedLocked : IsLocked;
            AcquireCursorControl(!currentLocked);
        }

        /// <summary>
        ///     Shows and unlocks cursor.
        /// </summary>
        public void ShowCursor()
        {
            SetCursorLocked(false);
        }

        /// <summary>
        ///     Hides and locks cursor.
        /// </summary>
        public void HideCursor()
        {
            SetCursorLocked(true);
        }

        /// <summary>
        ///     Releases this controller's ownership. If another controller was active before, its requested state is restored.
        /// </summary>
        public void ReleaseControl()
        {
            if (!HasCursorOwnership)
            {
                return;
            }

            bool wasTopController = ReferenceEquals(GetTopController(), this);
            ActiveControllers.Remove(this);
            HasCursorOwnership = false;

            if (wasTopController)
            {
                CursorLockController top = GetTopController();
                if (top != null)
                {
                    top.ApplyCursorState(top._requestedLocked);
                }
            }
        }

        /// <summary>
        ///     Enables or disables this controller. When disabled, toggle input and lifecycle cursor application are ignored.
        /// </summary>
        public void SetControllerEnabled(bool enabled)
        {
            _controllerEnabled = enabled;
            if (!enabled)
            {
                _cursorAccessActive = false;
                ReleaseControl();
            }
        }

        public void EnableController()
        {
            SetControllerEnabled(true);
        }

        public void DisableController()
        {
            SetControllerEnabled(false);
        }

        private void AcquireCursorControl(bool locked)
        {
            _requestedLocked = locked;
            ActiveControllers.Remove(this);
            ActiveControllers.Add(this);
            HasCursorOwnership = true;
            ApplyCursorState(locked);
        }

        private void HandleCursorAccessKey()
        {
            if (!_allowCursorAccessKey)
            {
                return;
            }

            switch (_cursorAccessKeyMode)
            {
                case CursorAccessKeyMode.HoldToShowCursor:
                    if (KeyInputCompat.GetKeyDown(_cursorAccessKey))
                    {
                        StartCursorAccess();
                    }
                    else if (KeyInputCompat.GetKeyUp(_cursorAccessKey))
                    {
                        EndCursorAccess();
                    }

                    break;

                case CursorAccessKeyMode.ToggleShowCursor:
                    if (KeyInputCompat.GetKeyDown(_cursorAccessKey))
                    {
                        if (_cursorAccessActive)
                        {
                            EndCursorAccess();
                        }
                        else
                        {
                            StartCursorAccess();
                        }
                    }

                    break;
            }
        }

        private void StartCursorAccess()
        {
            if (_cursorAccessActive)
            {
                return;
            }

            _cursorAccessActive = true;
            _cursorAccessHadOwnership = HasCursorOwnership;
            _cursorAccessPreviousLocked = HasCursorOwnership ? _requestedLocked : IsLocked;
            AcquireCursorControl(false);
        }

        private void EndCursorAccess()
        {
            if (!_cursorAccessActive)
            {
                return;
            }

            _cursorAccessActive = false;

            if (_cursorAccessHadOwnership)
            {
                AcquireCursorControl(_cursorAccessPreviousLocked);
            }
            else
            {
                ReleaseControl();
            }
        }

        private static CursorLockController GetTopController()
        {
            return ActiveControllers.Count > 0 ? ActiveControllers[ActiveControllers.Count - 1] : null;
        }
    }
}
