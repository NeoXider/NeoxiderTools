using CMF;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Adds sprint to CMF's <see cref="AdvancedWalkerController" />, which ships with a single movement speed.
    ///     Reads <see cref="NeoCharacterInput.IsRunHeld" /> and swaps the controller's speed while it is held.
    /// </summary>
    /// <remarks>
    ///     Walk speed is captured from the controller in Awake, so the value authored on
    ///     <c>AdvancedWalkerController.movementSpeed</c> stays the single source of truth for walking.
    /// </remarks>
    [NeoDoc("Tools/Move/CharacterController/NeoCharacterSprint.md")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(NeoCharacterSprint))]
    [RequireComponent(typeof(AdvancedWalkerController))]
    public class NeoCharacterSprint : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private AdvancedWalkerController _controller;

        [SerializeField] private NeoCharacterInput _input;

        [Header("Speed")]
        [Tooltip("Multiplies the controller's authored movement speed while sprint is held.")]
        [Min(1f)]
        [SerializeField]
        private float _sprintSpeedMultiplier = 1.7f;

        [Tooltip("Speed change per second. 0 switches instantly.")] [Min(0f)] [SerializeField]
        private float _speedLerpRate;

        [Tooltip("Only sprint while the character is grounded.")] [SerializeField]
        private bool _requireGrounded = true;

        [Header("Events")] [SerializeField] private UnityEvent _onSprintStart = new();
        [SerializeField] private UnityEvent _onSprintStop = new();

        private float _walkSpeed;
        private bool _wasSprinting;

        /// <summary>
        ///     Gets whether sprint is currently applied.
        /// </summary>
        public bool IsSprinting { get; private set; }

        /// <summary>
        ///     Gets the walk speed captured from the controller in Awake.
        /// </summary>
        public float WalkSpeed => _walkSpeed;

        /// <summary>
        ///     Gets the speed applied while sprinting.
        /// </summary>
        public float SprintSpeed => _walkSpeed * _sprintSpeedMultiplier;

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<AdvancedWalkerController>();
            }

            if (_input == null)
            {
                _input = GetComponent<NeoCharacterInput>();
            }

            _walkSpeed = _controller.movementSpeed;
        }

        private void Update()
        {
            bool sprinting = ShouldSprint();
            if (sprinting != _wasSprinting)
            {
                _wasSprinting = sprinting;
                IsSprinting = sprinting;
                if (sprinting)
                {
                    _onSprintStart?.Invoke();
                }
                else
                {
                    _onSprintStop?.Invoke();
                }
            }

            float target = sprinting ? SprintSpeed : _walkSpeed;
            _controller.movementSpeed = _speedLerpRate <= 0f
                ? target
                : Mathf.MoveTowards(_controller.movementSpeed, target, _speedLerpRate * Time.deltaTime);
        }

        /// <summary>
        ///     Re-reads the walk speed from the controller. Call this after changing
        ///     <c>AdvancedWalkerController.movementSpeed</c> at runtime so sprint scales from the new value.
        /// </summary>
        public void RefreshWalkSpeed()
        {
            _walkSpeed = _controller.movementSpeed;
        }

        /// <summary>
        ///     Overrides the walk speed sprint scales from, and applies it immediately when not sprinting.
        /// </summary>
        public void SetWalkSpeed(float walkSpeed)
        {
            _walkSpeed = Mathf.Max(0f, walkSpeed);
        }

        private bool ShouldSprint()
        {
            if (_input == null || !_input.IsRunHeld)
            {
                return false;
            }

            return !_requireGrounded || _controller.IsGrounded();
        }
    }
}
