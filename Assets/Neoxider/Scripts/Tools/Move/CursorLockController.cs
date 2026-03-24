using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
        public enum ConfigurationPreset
        {
            Custom,
            Gameplay_Default,
            UI_Page_ShowCursorWhileActive,
            UI_MenuScene_Standalone
        }

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

        /// <summary>
        ///     When to snapshot <see cref="Cursor.lockState"/> / <see cref="Cursor.visible"/> for restore (similar to <see cref="PausePage"/> cursor logic).
        /// </summary>
        public enum LifecycleSnapshotMode
        {
            /// <summary>Lock On Enable/Disable flags only; no snapshot (legacy behaviour).</summary>
            None,

            /// <summary>
            ///     Save cursor before applying OnEnable; on OnDisable (if controller stack is empty), see After Lifecycle Disable.
            /// </summary>
            SaveOnEnable,

            /// <summary>
            ///     At start of OnDisable (when apply is enabled), save cursor; on next OnEnable, see After Lifecycle Enable.
            /// </summary>
            SaveOnDisable
        }

        /// <summary>Behaviour after lifecycle OnDisable when <see cref="SaveOnEnable"/> was used and the controller stack is empty.</summary>
        public enum AfterLifecycleDisableCursorBehavior
        {
            /// <summary>Apply configured disable state via <c>ApplyCursorState(_lockOnDisable)</c>.</summary>
            ApplyConfigured,

            /// <summary>Restore lock/visibility from before this OnEnable cycle.</summary>
            RestorePrevious,

            /// <summary>Always locked and hidden (same idea as <see cref="PausePage.AfterPauseCursorBehavior.ForceLockedHidden"/>).</summary>
            ForceLockedHidden
        }

        /// <summary>Behaviour on OnEnable after <see cref="SaveOnDisable"/>.</summary>
        public enum AfterLifecycleEnableCursorBehavior
        {
            /// <summary>Take ownership with <c>AcquireCursorControl(_lockOnEnable)</c>.</summary>
            ApplyConfigured,

            /// <summary>Restore snapshot taken on the last OnDisable.</summary>
            RestorePrevious
        }

        private static readonly List<CursorLockController> ActiveControllers = new();
        private static bool _sceneHookRegistered;

        [Header("Preset")]
        [Tooltip(
            "Quick setup. Non-Custom presets overwrite the fields below in OnValidate when changed. Pick Custom for full manual control.")]
        [SerializeField]
        private ConfigurationPreset _preset = ConfigurationPreset.Custom;

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

        [Header("Lifecycle snapshot (optional)")]
        [Tooltip(
            "None: no snapshot. SaveOnEnable: save before OnEnable apply; on exit see After Lifecycle Disable. SaveOnDisable: save at start of OnDisable; on next OnEnable see After Lifecycle Enable (reverse of SaveOnEnable).")]
        [SerializeField]
        private LifecycleSnapshotMode _lifecycleSnapshotMode;

        [Tooltip("Used with SaveOnEnable: what to do when the controller stack is empty after OnDisable.")]
        [SerializeField]
        private AfterLifecycleDisableCursorBehavior _afterLifecycleDisable =
            AfterLifecycleDisableCursorBehavior.ApplyConfigured;

        [Tooltip(
            "Used with SaveOnDisable: RestorePrevious restores the OnDisable snapshot; otherwise behaves like a normal OnEnable apply.")]
        [SerializeField]
        private AfterLifecycleEnableCursorBehavior _afterLifecycleEnable =
            AfterLifecycleEnableCursorBehavior.ApplyConfigured;

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

        private bool _hasLifecycleSnapshotFromEnable;
        private CursorLockMode _lifecycleSnapshotEnterLockState;
        private bool _lifecycleSnapshotEnterVisible;

        private bool _hasLifecycleSnapshotFromDisable;
        private CursorLockMode _lifecycleSnapshotExitLockState;
        private bool _lifecycleSnapshotExitVisible;

#if UNITY_EDITOR
        private ConfigurationPreset _presetAppliedInEditor = ConfigurationPreset.Custom;
#endif

        /// <summary>
        ///     Gets whether cursor is currently locked.
        /// </summary>
        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        public bool ControllerEnabled => _controllerEnabled;
        public bool HasCursorOwnership { get; private set; }

        public ControlMode Mode => _controlMode;

        public ConfigurationPreset Preset => _preset;

        public LifecycleSnapshotMode SnapshotMode => _lifecycleSnapshotMode;

        private bool SupportsAutomatic => _controlMode != ControlMode.ManualOnly;
        private bool SupportsManual => _controlMode != ControlMode.AutomaticOnly;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedSanitizeStack;
            _sceneHookRegistered = false;
            ActiveControllers.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneLoadedHook()
        {
            if (_sceneHookRegistered)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoadedSanitizeStack;
            SceneManager.sceneLoaded += OnSceneLoadedSanitizeStack;
            _sceneHookRegistered = true;
        }

        private static void OnSceneLoadedSanitizeStack(Scene scene, LoadSceneMode mode)
        {
            SanitizeActiveControllersList();
            CursorLockController top = GetTopController();
            if (top != null)
            {
                top.ApplyCursorState(top._requestedLocked);
            }
        }

        private static void SanitizeActiveControllersList()
        {
            for (int i = ActiveControllers.Count - 1; i >= 0; i--)
            {
                CursorLockController c = ActiveControllers[i];
                if (c == null)
                {
                    ActiveControllers.RemoveAt(i);
                }
            }
        }

        private static CursorLockController GetTopController()
        {
            SanitizeActiveControllersList();
            int count = ActiveControllers.Count;
            return count > 0 ? ActiveControllers[count - 1] : null;
        }

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

            if (!SupportsAutomatic || !_controllerEnabled || !_applyOnEnable)
            {
                return;
            }

            if (_lifecycleSnapshotMode == LifecycleSnapshotMode.SaveOnDisable && _hasLifecycleSnapshotFromDisable)
            {
                if (_afterLifecycleEnable == AfterLifecycleEnableCursorBehavior.RestorePrevious)
                {
                    ApplyRawCursorState(_lifecycleSnapshotExitLockState, _lifecycleSnapshotExitVisible);
                    _hasLifecycleSnapshotFromDisable = false;
                    return;
                }

                _hasLifecycleSnapshotFromDisable = false;
            }

            if (_lifecycleSnapshotMode == LifecycleSnapshotMode.SaveOnEnable)
            {
                _lifecycleSnapshotEnterLockState = Cursor.lockState;
                _lifecycleSnapshotEnterVisible = Cursor.visible;
                _hasLifecycleSnapshotFromEnable = true;
            }

            AcquireCursorControl(_lockOnEnable);
        }

        private void OnDisable()
        {
            bool applyDisableState = SupportsAutomatic && _controllerEnabled && _applyOnDisable;
            bool lockOnDisable = _lockOnDisable;

            bool hadEnterSnapshot = _lifecycleSnapshotMode == LifecycleSnapshotMode.SaveOnEnable &&
                                    _hasLifecycleSnapshotFromEnable;

            if (_lifecycleSnapshotMode == LifecycleSnapshotMode.SaveOnDisable && applyDisableState)
            {
                _lifecycleSnapshotExitLockState = Cursor.lockState;
                _lifecycleSnapshotExitVisible = Cursor.visible;
                _hasLifecycleSnapshotFromDisable = true;
            }

            ReleaseControlInternal(reapplyBelowIfWasTop: true);

            if (applyDisableState && GetTopController() == null)
            {
                if (hadEnterSnapshot)
                {
                    switch (_afterLifecycleDisable)
                    {
                        case AfterLifecycleDisableCursorBehavior.RestorePrevious:
                            ApplyRawCursorState(_lifecycleSnapshotEnterLockState, _lifecycleSnapshotEnterVisible);
                            break;
                        case AfterLifecycleDisableCursorBehavior.ForceLockedHidden:
                            Cursor.lockState = CursorLockMode.Locked;
                            Cursor.visible = false;
                            break;
                        default:
                            ApplyCursorState(lockOnDisable);
                            break;
                    }
                }
                else
                {
                    ApplyCursorState(lockOnDisable);
                }
            }

            if (_lifecycleSnapshotMode == LifecycleSnapshotMode.SaveOnEnable)
            {
                _hasLifecycleSnapshotFromEnable = false;
            }

            if (_setControllerEnabledOnDisable)
            {
                _controllerEnabled = _controllerEnabledOnDisable;
            }
        }

        private static void ApplyRawCursorState(CursorLockMode lockState, bool visible)
        {
            Cursor.lockState = lockState;
            Cursor.visible = visible;
        }

        private void OnDestroy()
        {
            _cursorAccessActive = false;
            _hasLifecycleSnapshotFromEnable = false;
            _hasLifecycleSnapshotFromDisable = false;
            SanitizeActiveControllersList();
            bool wasTop = HasCursorOwnership && ActiveControllers.Count > 0 &&
                          ReferenceEquals(ActiveControllers[ActiveControllers.Count - 1], this);

            ActiveControllers.Remove(this);
            HasCursorOwnership = false;

            SanitizeActiveControllersList();

            if (wasTop)
            {
                CursorLockController top = GetTopController();
                if (top != null)
                {
                    top.ApplyCursorState(top._requestedLocked);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_preset != ConfigurationPreset.Custom && _preset != _presetAppliedInEditor)
            {
                ApplyPreset(_preset);
                _presetAppliedInEditor = _preset;
            }

            if (_preset == ConfigurationPreset.Custom)
            {
                _presetAppliedInEditor = ConfigurationPreset.Custom;
            }
        }

        private void ApplyPreset(ConfigurationPreset preset)
        {
            switch (preset)
            {
                case ConfigurationPreset.Gameplay_Default:
                    _mode = CursorStateMode.LockAndHide;
                    _controlMode = ControlMode.AutomaticAndManual;
                    _controllerEnabled = true;
                    _lockOnStart = true;
                    _applyStartOnlyWhenControllerEnabled = true;
                    _setControllerEnabledOnEnable = false;
                    _applyOnEnable = false;
                    _lockOnEnable = true;
                    _setControllerEnabledOnDisable = false;
                    _controllerEnabledOnDisable = false;
                    _applyOnDisable = false;
                    _lockOnDisable = true;
                    _lifecycleSnapshotMode = LifecycleSnapshotMode.None;
                    _afterLifecycleDisable = AfterLifecycleDisableCursorBehavior.ApplyConfigured;
                    _afterLifecycleEnable = AfterLifecycleEnableCursorBehavior.ApplyConfigured;
                    _allowToggle = true;
                    _toggleKey = KeyCode.Escape;
                    _allowCursorAccessKey = false;
                    break;

                case ConfigurationPreset.UI_Page_ShowCursorWhileActive:
                    _mode = CursorStateMode.LockAndHide;
                    _controlMode = ControlMode.AutomaticAndManual;
                    _controllerEnabled = true;
                    _lockOnStart = false;
                    _applyStartOnlyWhenControllerEnabled = true;
                    _setControllerEnabledOnEnable = false;
                    _applyOnEnable = true;
                    _lockOnEnable = false;
                    _setControllerEnabledOnDisable = false;
                    _controllerEnabledOnDisable = false;
                    _applyOnDisable = true;
                    _lockOnDisable = true;
                    _lifecycleSnapshotMode = LifecycleSnapshotMode.SaveOnEnable;
                    _afterLifecycleDisable = AfterLifecycleDisableCursorBehavior.RestorePrevious;
                    _afterLifecycleEnable = AfterLifecycleEnableCursorBehavior.ApplyConfigured;
                    _allowToggle = false;
                    _allowCursorAccessKey = false;
                    break;

                case ConfigurationPreset.UI_MenuScene_Standalone:
                    _mode = CursorStateMode.LockAndHide;
                    _controlMode = ControlMode.AutomaticAndManual;
                    _controllerEnabled = true;
                    _lockOnStart = false;
                    _applyStartOnlyWhenControllerEnabled = true;
                    _setControllerEnabledOnEnable = false;
                    _applyOnEnable = true;
                    _lockOnEnable = false;
                    _setControllerEnabledOnDisable = false;
                    _controllerEnabledOnDisable = false;
                    _applyOnDisable = false;
                    _lockOnDisable = true;
                    _lifecycleSnapshotMode = LifecycleSnapshotMode.None;
                    _afterLifecycleDisable = AfterLifecycleDisableCursorBehavior.ApplyConfigured;
                    _afterLifecycleEnable = AfterLifecycleEnableCursorBehavior.ApplyConfigured;
                    _allowToggle = false;
                    _allowCursorAccessKey = false;
                    break;
            }
        }
#endif

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
            ReleaseControlInternal(reapplyBelowIfWasTop: true);
        }

        private void ReleaseControlInternal(bool reapplyBelowIfWasTop)
        {
            if (!HasCursorOwnership)
            {
                return;
            }

            SanitizeActiveControllersList();
            bool wasTop = ActiveControllers.Count > 0 &&
                          ReferenceEquals(ActiveControllers[ActiveControllers.Count - 1], this);
            ActiveControllers.Remove(this);
            HasCursorOwnership = false;

            if (reapplyBelowIfWasTop && wasTop)
            {
                SanitizeActiveControllersList();
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
                ReleaseControlInternal(reapplyBelowIfWasTop: true);
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
                ReleaseControlInternal(reapplyBelowIfWasTop: true);
            }
        }
    }
}
