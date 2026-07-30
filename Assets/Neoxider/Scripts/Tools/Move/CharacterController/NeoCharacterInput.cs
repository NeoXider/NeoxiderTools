using System;
using CMF;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Movement input source for the Character Movement Fundamentals controllers. Replaces CMF's
    ///     <c>CharacterKeyboardInput</c> with the Neoxider input stack: New Input System or legacy Input Manager
    ///     (auto-detected), plus injection hooks for on-screen joysticks and network/AI drivers.
    /// </summary>
    /// <remarks>
    ///     Put this on the same GameObject as <c>AdvancedWalkerController</c> (or any other CMF
    ///     <c>Controller</c>) — CMF looks up a <see cref="CharacterInput" /> on its own GameObject in Awake.
    ///     <para>
    ///         <see cref="IsJumpKeyPressed" /> returns the <em>held</em> state, not a one-frame edge: CMF derives
    ///         press/release edges itself and uses the release to cut variable-height jumps short.
    ///     </para>
    /// </remarks>
    [NeoDoc("Tools/Move/CharacterController/NeoCharacterInput.md")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(NeoCharacterInput))]
    public class NeoCharacterInput : CharacterInput
    {
        [Header("Input")] [SerializeField] private NeoInputBackend _inputBackend = NeoInputBackend.AutoPreferNew;
        [SerializeField] private string _horizontalAxis = "Horizontal";
        [SerializeField] private string _verticalAxis = "Vertical";
        [SerializeField] private string _jumpButton = "Jump";
        [SerializeField] private KeyCode _runKey = KeyCode.LeftShift;

        [Header("Gating")]
        [Tooltip("Process movement input. Toggle via SetMovementEnabled(bool) — e.g. from a pause menu or cutscene.")]
        [SerializeField]
        private bool _movementEnabled = true;

        [Tooltip("Process jump input. Toggle via SetJumpEnabled(bool).")] [SerializeField]
        private bool _jumpEnabled = true;

        [Tooltip("Allow sprint. Read by NeoCharacterSprint; has no effect on its own.")] [SerializeField]
        private bool _canRun = true;

        [Header("Diagnostics")] [SerializeField]
        private bool _logInputFallbackWarnings;

        private bool _externalJumpHeld;

        // WHY: external overrides for on-screen joysticks / touch controls / AI. Null means "use the real device".
        private Vector2? _externalMoveInput;
        private bool _externalRunHeld;
        private bool _legacyInputUnavailableWarningShown;
        private bool _newInputUnavailableWarningShown;

        /// <summary>
        ///     Gets whether movement input is processed. Change via <see cref="SetMovementEnabled" />.
        /// </summary>
        public bool MovementEnabled => _movementEnabled;

        /// <summary>
        ///     Gets whether jump input is processed. Change via <see cref="SetJumpEnabled" />.
        /// </summary>
        public bool JumpEnabled => _jumpEnabled;

        /// <summary>
        ///     Gets whether sprint is currently held. Consumed by <see cref="NeoCharacterSprint" />.
        /// </summary>
        public bool IsRunHeld
        {
            get
            {
                if (!_canRun || !_movementEnabled)
                {
                    return false;
                }

                return _externalMoveInput.HasValue ? _externalRunHeld : ReadRunHeld();
            }
        }

        /// <inheritdoc />
        public override float GetHorizontalMovementInput()
        {
            return ReadMove().x;
        }

        /// <inheritdoc />
        public override float GetVerticalMovementInput()
        {
            return ReadMove().y;
        }

        /// <inheritdoc />
        public override bool IsJumpKeyPressed()
        {
            if (!_jumpEnabled)
            {
                return false;
            }

            return _externalJumpHeld || ReadJumpHeld();
        }

        /// <summary>
        ///     Enables or disables movement input processing. Callable from a UnityEvent (dynamic bool).
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
        }

        /// <summary>
        ///     Enables or disables jump input processing. Callable from a UnityEvent (dynamic bool).
        /// </summary>
        public void SetJumpEnabled(bool enabled)
        {
            _jumpEnabled = enabled;
        }

        /// <summary>
        ///     Injects movement input from an external source (on-screen joystick, touch pad, AI). While set, the
        ///     built-in device reading is bypassed. Pass null to revert to built-in input.
        /// </summary>
        /// <param name="input">Movement vector (x = strafe, y = forward). Clamped to magnitude 1.</param>
        public void SetMoveInput(Vector2? input)
        {
            _externalMoveInput = input.HasValue ? Vector2.ClampMagnitude(input.Value, 1f) : (Vector2?)null;
        }

        /// <summary>
        ///     Injects the jump button state from an external source (on-screen button). Hold it for the duration of
        ///     the jump — CMF uses the release edge to cut the jump short, so a one-frame pulse always jumps at
        ///     minimum height.
        /// </summary>
        /// <param name="held">True while the jump button is held.</param>
        public void SetJumpInput(bool held)
        {
            _externalJumpHeld = held;
        }

        /// <summary>
        ///     Injects sprint state from an external source. Only read while <see cref="SetMoveInput" /> is active.
        /// </summary>
        /// <param name="held">True while sprinting.</param>
        public void SetRunInput(bool held)
        {
            _externalRunHeld = held;
        }

        private Vector2 ReadMove()
        {
            if (!_movementEnabled)
            {
                return Vector2.zero;
            }

            if (_externalMoveInput.HasValue)
            {
                return _externalMoveInput.Value;
            }

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

        private bool ReadJumpHeld()
        {
            if (ShouldUseNewInput())
            {
                return OptionalInputSystemBridge.ReadJumpHeld();
            }

            try
            {
                return Input.GetButton(_jumpButton);
            }
            catch (InvalidOperationException)
            {
                WarnLegacyInputUnavailable();
                return OptionalInputSystemBridge.ReadJumpHeld();
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
                    "[NeoCharacterInput] New Input System is not available. Falling back to Legacy Input Manager.",
                    this);
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
                "[NeoCharacterInput] Legacy Input Manager is unavailable in current Player Settings. Falling back to New Input System where possible.",
                this);
        }
    }
}
