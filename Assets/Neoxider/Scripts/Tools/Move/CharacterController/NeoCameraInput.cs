using System;
using CMF;
using Neo.Settings;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Look input source for the Character Movement Fundamentals camera controllers. Replaces CMF's
    ///     <c>CameraMouseInput</c> / <c>CameraJoystickInput</c> with the Neoxider input stack: New Input System or
    ///     legacy Input Manager (auto-detected), <see cref="GameSettings.MouseSensitivity" />, cursor-aware gating,
    ///     pause handling and injection hooks for on-screen look pads.
    /// </summary>
    /// <remarks>
    ///     Put this on the same GameObject as CMF's <c>CameraController</c> (or <c>ThirdPersonCameraController</c>) —
    ///     CMF looks up a <see cref="CameraInput" /> on its own GameObject in Awake.
    ///     <para>
    ///         This component never writes <see cref="Cursor" /> state. Cursor ownership stays with
    ///         <see cref="CursorLockController" /> (or whatever owns the pointer in your game); this component only
    ///         <em>reads</em> the resulting state to decide whether look should be processed.
    ///     </para>
    /// </remarks>
    [NeoDoc("Tools/Move/CharacterController/NeoCameraInput.md")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(NeoCameraInput))]
    public class NeoCameraInput : CameraInput
    {
        [Header("Input")] [SerializeField] private NeoInputBackend _inputBackend = NeoInputBackend.AutoPreferNew;
        [SerializeField] private string _mouseXAxis = "Mouse X";
        [SerializeField] private string _mouseYAxis = "Mouse Y";

        [Tooltip("Scales the raw pointer delta before sensitivity is applied. Matches CMF's Mouse Input Multiplier.")]
        [SerializeField]
        private float _mouseInputMultiplier = 0.01f;

        [Tooltip("Gamepad right-stick look speed, relative to the camera controller's Camera Speed.")] [SerializeField]
        private float _stickInputMultiplier = 1f;

        [SerializeField] private bool _invertHorizontal;
        [SerializeField] private bool _invertVertical;

        [Header("Sensitivity")]
        [Tooltip("When enabled, look speed uses GameSettings.MouseSensitivity and follows it live.")]
        [SerializeField]
        private bool _useGameSettingsMouseSensitivity = true;

        [SerializeField] private float _mouseSensitivity = 1f;

        [Header("Gating")]
        [Tooltip("Whether look is processed at all. Change via SetLookEnabled(bool).")]
        [SerializeField]
        private bool _lookEnabled = true;

        [Tooltip("When the cursor is visible (unlocked), do not rotate the camera. On by default so UI/menus do not drag the view.")]
        [SerializeField]
        private bool _pauseLookWhenCursorVisible = true;

        [Tooltip("When enabled, look is disabled on EM.OnPause and re-enabled on EM.OnResume.")] [SerializeField]
        private bool _disableLookOnPause = true;

        [Header("Diagnostics")] [SerializeField]
        private bool _logInputFallbackWarnings;

        // WHY: Input System Mouse.delta is raw pixels, while the legacy "Mouse X"/"Mouse Y" axes are pixels scaled
        // by the Input Manager's default 0.1 mouse sensitivity. Without this factor the New Input System backend
        // looks ~10x faster than the legacy one for the same physical mouse move.
        private const float PointerDeltaToLegacyAxis = 0.1f;

        // WHY: external override for on-screen look pads / gyro. Null means "use the real device".
        private Vector2? _externalLookInput;
        private bool _legacyInputUnavailableWarningShown;
        private bool _newInputUnavailableWarningShown;

        /// <summary>
        ///     Gets whether look input is processed. Change via <see cref="SetLookEnabled" />.
        /// </summary>
        public bool LookEnabled => _lookEnabled;

        /// <summary>
        ///     Gets whether look is currently active, taking the cursor gate into account.
        /// </summary>
        public bool IsLookActive => _lookEnabled && (!_pauseLookWhenCursorVisible || !Cursor.visible);

        private float EffectiveSensitivity =>
            _useGameSettingsMouseSensitivity ? GameSettings.MouseSensitivity : _mouseSensitivity;

        private void OnEnable()
        {
            if (_disableLookOnPause && EM.TryGetInstance(out EM eventManager))
            {
                eventManager.OnPause.AddListener(OnPauseLook);
                eventManager.OnResume.AddListener(OnResumeLook);
            }
        }

        private void OnDisable()
        {
            if (_disableLookOnPause && EM.TryGetInstance(out EM eventManager))
            {
                eventManager.OnPause.RemoveListener(OnPauseLook);
                eventManager.OnResume.RemoveListener(OnResumeLook);
            }
        }

        /// <inheritdoc />
        public override float GetHorizontalCameraInput()
        {
            float value = ReadLookRate().x;
            return _invertHorizontal ? -value : value;
        }

        /// <inheritdoc />
        public override float GetVerticalCameraInput()
        {
            // WHY: CMF clamps the vertical angle as "up is negative", so raw pointer Y is inverted here to match
            // the same look direction as its own CameraMouseInput.
            float value = -ReadLookRate().y;
            return _invertVertical ? -value : value;
        }

        /// <summary>
        ///     Enables or disables look processing. Callable from a UnityEvent (dynamic bool).
        /// </summary>
        public void SetLookEnabled(bool enabled)
        {
            _lookEnabled = enabled;
        }

        /// <summary>
        ///     Injects look input from an external source (on-screen look pad, gyroscope). While set, the built-in
        ///     device reading is bypassed. Pass null to revert to built-in input.
        /// </summary>
        /// <param name="input">Look rate (x = yaw, y = pitch), in the same units as a gamepad stick.</param>
        public void SetLookInput(Vector2? input)
        {
            _externalLookInput = input;
        }

        private void OnPauseLook()
        {
            SetLookEnabled(false);
        }

        private void OnResumeLook()
        {
            SetLookEnabled(true);
        }

        private Vector2 ReadLookRate()
        {
            if (!IsLookActive)
            {
                return Vector2.zero;
            }

            float sensitivity = EffectiveSensitivity;

            if (_externalLookInput.HasValue)
            {
                return _externalLookInput.Value * (_stickInputMultiplier * sensitivity);
            }

            if (ShouldUseNewInput())
            {
                Vector2 pointerDelta = OptionalInputSystemBridge.ReadPointerDelta() *
                                       (PointerDeltaToLegacyAxis * _mouseInputMultiplier);
                Vector2 stick = OptionalInputSystemBridge.ReadLookStick() * _stickInputMultiplier;

                var pointerRate = new Vector2(
                    NeoLookRate.FromFrameDelta(pointerDelta.x, Time.deltaTime, Time.timeScale),
                    NeoLookRate.FromFrameDelta(pointerDelta.y, Time.deltaTime, Time.timeScale));

                return (pointerRate + stick) * sensitivity;
            }

            try
            {
                var raw = new Vector2(Input.GetAxisRaw(_mouseXAxis), Input.GetAxisRaw(_mouseYAxis)) *
                          _mouseInputMultiplier;
                var rate = new Vector2(
                    NeoLookRate.FromFrameDelta(raw.x, Time.deltaTime, Time.timeScale),
                    NeoLookRate.FromFrameDelta(raw.y, Time.deltaTime, Time.timeScale));
                return rate * sensitivity;
            }
            catch (InvalidOperationException)
            {
                WarnLegacyInputUnavailable();
                return Vector2.zero;
            }
        }

        private bool ShouldUseNewInput()
        {
            bool newAvailable = OptionalInputSystemBridge.IsAvailable;
            bool legacyAvailable = NeoInputAvailability.IsLegacyInputAvailable();
            bool useNew = NeoInputBackendResolver.ShouldUseNewInput(_inputBackend, newAvailable, legacyAvailable);

            if (!useNew && !newAvailable && _inputBackend == NeoInputBackend.NewInputSystem &&
                _logInputFallbackWarnings && !_newInputUnavailableWarningShown)
            {
                _newInputUnavailableWarningShown = true;
                NeoDiagnostics.LogWarning(
                    "[NeoCameraInput] New Input System is not available. Falling back to Legacy Input Manager.", this);
            }

            return useNew;
        }

        private void WarnLegacyInputUnavailable()
        {
            if (_legacyInputUnavailableWarningShown || !_logInputFallbackWarnings)
            {
                return;
            }

            _legacyInputUnavailableWarningShown = true;
            NeoDiagnostics.LogWarning(
                "[NeoCameraInput] Legacy Input Manager is unavailable in current Player Settings. Look input is disabled — switch Input Backend to Auto or install the Input System package.",
                this);
        }
    }
}
