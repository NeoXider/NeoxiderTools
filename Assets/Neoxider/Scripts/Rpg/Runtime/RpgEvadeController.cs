using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    /// RPG-native evade ability with cooldown and optional invulnerability window.
    /// </summary>
    [NeoDoc("Rpg/RpgEvadeController.md")]
    [CreateFromMenu("Neoxider/RPG/RpgEvadeController")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgEvadeController))]
    public sealed class RpgEvadeController : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private RpgCombatant _combatant;
        [SerializeField] private RpgStatsManager _profileManager;

        [Header("Built-in Input")]
        [SerializeField] private bool _enableBuiltInInput = true;
        [SerializeField] private RpgButtonBinding _evadeBinding = RpgButtonBinding.CreateEvadeDefault();

        [Header("Timings")]
        [SerializeField] [Min(0.01f)] private float _evadeDuration = 0.35f;
        [SerializeField] [Min(0.01f)] private float _cooldown = 1.5f;
        [SerializeField] private bool _grantInvulnerability = true;

        [Header("Reactive State")]
        public ReactivePropertyBool IsEvadingState = new(false);
        public ReactivePropertyFloat RemainingCooldownState = new(0f);

        [Header("Events")]
        [SerializeField] private UnityEvent _onEvadeStarted = new();
        [SerializeField] private UnityEvent _onEvadeFinished = new();
        [SerializeField] private UnityEvent _onCooldownReady = new();

        private float _cooldownReadyAt;
        private float _evadeEndAt;
        private bool _cooldownReadyInvoked = true;
        private bool _grantedInvulnerability;

        /// <summary>
        /// Gets whether evade is active right now.
        /// </summary>
        public bool IsEvading => Time.time < _evadeEndAt;

        /// <summary>
        /// Gets whether the ability can be started right now.
        /// </summary>
        public bool CanEvade => !IsEvading && GetRemainingCooldown() <= 0f && ResolveReceiver()?.CanPerformActions != false;

        /// <summary>
        /// Gets or sets whether the built-in input listener is enabled.
        /// </summary>
        public bool EnableBuiltInInput
        {
            get => _enableBuiltInInput;
            set => _enableBuiltInInput = value;
        }

        private void Update()
        {
            if (_enableBuiltInInput && _evadeBinding != null && _evadeBinding.IsPressedThisFrame())
            {
                TryStartEvade();
            }

            RemainingCooldownState.SetValueWithoutNotify(GetRemainingCooldown());
            IsEvadingState.SetValueWithoutNotify(IsEvading);
            RemainingCooldownState.ForceNotify();
            IsEvadingState.ForceNotify();

            if (_grantInvulnerability && _grantedInvulnerability && !IsEvading)
            {
                ResolveReceiver()?.RemoveInvulnerabilityLock();
                _grantedInvulnerability = false;
            }

            if (!_cooldownReadyInvoked && GetRemainingCooldown() <= 0f)
            {
                _cooldownReadyInvoked = true;
                _onCooldownReady?.Invoke();
            }
        }

        /// <summary>
        /// Starts evade if the controller is ready.
        /// </summary>
        public bool TryStartEvade()
        {
            if (!CanEvade)
            {
                return false;
            }

            _evadeEndAt = Time.time + _evadeDuration;
            _cooldownReadyAt = Time.time + _cooldown;
            _cooldownReadyInvoked = false;
            _onEvadeStarted?.Invoke();

            IRpgCombatReceiver receiver = ResolveReceiver();
            if (_grantInvulnerability && receiver != null)
            {
                receiver.AddInvulnerabilityLock();
                _grantedInvulnerability = true;
            }

            CancelInvoke(nameof(FinishEvade));
            Invoke(nameof(FinishEvade), _evadeDuration);
            return true;
        }

        /// <summary>
        /// Returns the remaining cooldown in seconds.
        /// </summary>
        public float GetRemainingCooldown()
        {
            return Mathf.Max(0f, _cooldownReadyAt - Time.time);
        }

        /// <summary>
        /// Clears the active cooldown immediately.
        /// </summary>
        public void ResetCooldown()
        {
            _cooldownReadyAt = 0f;
            RemainingCooldownState.Value = 0f;
            _cooldownReadyInvoked = true;
        }

        private void FinishEvade()
        {
            if (_grantInvulnerability)
            {
                ResolveReceiver()?.RemoveInvulnerabilityLock();
                _grantedInvulnerability = false;
            }

            _onEvadeFinished?.Invoke();
        }

        private IRpgCombatReceiver ResolveReceiver()
        {
            if (_combatant != null)
            {
                return _combatant;
            }

            if (_profileManager != null)
            {
                return _profileManager;
            }

            return GetComponent<RpgCombatant>() as IRpgCombatReceiver ?? GetComponent<RpgStatsManager>();
        }
    }
}
