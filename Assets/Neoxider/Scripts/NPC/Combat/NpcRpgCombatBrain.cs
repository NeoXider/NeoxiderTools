using Neo.Rpg;
using Neo.Rpg.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.NPC.Combat
{
    /// <summary>
    ///     Modular NPC combat brain that combines navigation, target selection, and RPG attack presets.
    /// </summary>
    [DisallowMultipleComponent]
    [NeoDoc("NPC/Combat/NpcRpgCombatBrain.md")]
    [CreateFromMenu("Neoxider/NPC/NpcRpgCombatBrain")]
    [AddComponentMenu("Neoxider/NPC/Combat/" + nameof(NpcRpgCombatBrain))]
    public sealed class NpcRpgCombatBrain : MonoBehaviour
    {
        [Header("Setup")] [SerializeField] private bool _isActive = true;

        [SerializeField] private NpcCombatPreset _preset;
        [SerializeField] private NpcNavigation _navigation;
        [SerializeField] private RpgTargetSelector _targetSelector;
        [SerializeField] private RpgAttackController _attackController;
        [SerializeField] private RpgCharacter _character;
        [SerializeField] private Transform _lookOrigin;

        [Header("Behaviour")] [SerializeField] private bool _autoAcquireTarget = true;

        [SerializeField] private bool _disableAttackControllerInput = true;
        [SerializeField] private bool _clearTargetOnDisable = true;
        [SerializeField] [Min(0.05f)] private float _decisionInterval = 0.15f;

        [Header("Events")] [SerializeField] private RpgGameObjectEvent _onTargetAcquired = new();

        [SerializeField] private RpgGameObjectEvent _onTargetLost = new();
        [SerializeField] private UnityEvent _onChaseStarted = new();
        [SerializeField] private UnityEvent _onHoldingPosition = new();
        [SerializeField] private UnityEvent _onAttackTriggered = new();
        [SerializeField] private RpgStringEvent _onAttackFailed = new();
        [SerializeField] private RpgStringEvent _onDecisionChanged = new();

        [Header("Debug")] [SerializeField] private bool _debugMode;

        [SerializeField] [TextArea(2, 6)] private string _lastDecision;
        private NpcNavigation.NavigationMode _capturedNavigationMode;
        private Transform _capturedFollowTarget;
        private bool _capturedNavigationActive;
        private bool _navigationModeCaptured;

        private float _nextDecisionTime;
        private NpcCombatDecisionCore.Decision? _previousDecision;
        private GameObject _chaseTarget;
        private Vector3 _chasePathTargetPosition;
        private IRpgCombatReceiver _actor;

        private const float ChaseRepathDistanceSqr = 0.001f;

        /// <summary>
        ///     Gets the current combat target.
        /// </summary>
        public GameObject CurrentTarget => _targetSelector != null ? _targetSelector.CurrentTarget : null;

        /// <summary>
        ///     Gets whether the brain currently has a target.
        /// </summary>
        public bool HasTarget => CurrentTarget != null;

        /// <summary>Master switch for the decision loop. Disabling it does not clear the target.</summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        /// <summary>Preset driving distances and the attack; can be swapped at runtime.</summary>
        public NpcCombatPreset Preset
        {
            get => _preset;
            set => _preset = value;
        }

        /// <summary>Last decision taken by the loop; null before the first evaluation.</summary>
        public NpcCombatDecisionCore.Decision? CurrentDecision => _previousDecision;

        /// <summary>Raised when a new target is selected.</summary>
        public RpgGameObjectEvent OnTargetAcquired => _onTargetAcquired;

        /// <summary>Raised with the previous target when it is cleared.</summary>
        public RpgGameObjectEvent OnTargetLost => _onTargetLost;

        /// <summary>Raised once when a chase starts (or the chased target changes), not on every decision tick.</summary>
        public UnityEvent OnChaseStarted => _onChaseStarted;

        /// <summary>Raised on each decision tick that holds position.</summary>
        public UnityEvent OnHoldingPosition => _onHoldingPosition;

        /// <summary>Raised after the attack controller accepted the preset attack.</summary>
        public UnityEvent OnAttackTriggered => _onAttackTriggered;

        /// <summary>Raised with the failure reason when an attack attempt is rejected.</summary>
        public RpgStringEvent OnAttackFailed => _onAttackFailed;

        /// <summary>Raised with a readable summary whenever the decision changes.</summary>
        public RpgStringEvent OnDecisionChanged => _onDecisionChanged;

        private void Awake()
        {
            AutoResolveReferences();

            if (_disableAttackControllerInput && _attackController != null)
            {
                _attackController.EnableBuiltInInput = false;
            }
        }

        private void Update()
        {
            if (!_isActive || _preset == null || _attackController == null || _targetSelector == null)
            {
                return;
            }

            if (Time.time < _nextDecisionTime)
            {
                return;
            }

            _nextDecisionTime = Time.time + _decisionInterval;
            EvaluateBrain();
        }

        private void OnDisable()
        {
            _chaseTarget = null;
            if (_clearTargetOnDisable)
            {
                ClearCombatTarget();
            }
            else
            {
                RestoreNavigationModeIfNeeded();
            }
        }

        private void OnValidate()
        {
            _decisionInterval = Mathf.Max(0.05f, _decisionInterval);
        }

        /// <summary>
        ///     Resolves missing references from the current GameObject.
        /// </summary>
        [Button]
        public void AutoResolveReferences()
        {
            if (_navigation == null)
            {
                _navigation = GetComponent<NpcNavigation>();
            }

            if (_targetSelector == null)
            {
                _targetSelector = GetComponent<RpgTargetSelector>();
            }

            if (_attackController == null)
            {
                _attackController = GetComponent<RpgAttackController>();
            }

            if (_character == null)
            {
                _character = GetComponent<RpgCharacter>();
            }

            _actor = _character != null ? _character : GetComponent<IRpgCombatReceiver>();

            if (_lookOrigin == null)
            {
                _lookOrigin = transform;
            }
        }

        /// <summary>
        ///     Forces an immediate brain evaluation.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void EvaluateNow()
        {
            EvaluateBrain();
        }

        /// <summary>
        ///     Selects a target using the attached selector.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public GameObject AcquireTarget()
        {
            GameObject previousTarget = CurrentTarget;
            GameObject selected = _targetSelector != null ? _targetSelector.SelectTarget() : null;

            if (selected != null && selected != previousTarget)
            {
                _onTargetAcquired?.Invoke(selected);
            }

            return selected;
        }

        /// <summary>
        ///     Clears the combat target and restores navigation if needed.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void ClearCombatTarget()
        {
            GameObject previousTarget = CurrentTarget;
            _chaseTarget = null;
            _targetSelector?.ClearTarget();
            RestoreNavigationModeIfNeeded();

            if (previousTarget != null)
            {
                _onTargetLost?.Invoke(previousTarget);
            }
        }

        /// <summary>
        ///     Tries to execute the configured attack preset against the current target.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public bool ForceAttack()
        {
            return TryAttackCurrentTarget();
        }

        private void EvaluateBrain()
        {
            if (_preset == null || _attackController == null || _targetSelector == null)
            {
                return;
            }

            GameObject target = CurrentTarget;
            if (target == null && _autoAcquireTarget)
            {
                target = AcquireTarget();
            }

            float distanceToTarget = target != null
                ? Vector3.Distance(transform.position, target.transform.position)
                : float.PositiveInfinity;

            bool canAct = ResolveActor()?.CanPerformActions ?? true;
            bool canAttack = CanUseCurrentPreset();
            NpcCombatDecisionCore.Decision decision = NpcCombatDecisionCore.Decide(
                target != null,
                canAct,
                canAttack,
                distanceToTarget,
                ResolvePreferredAttackDistance(),
                ResolveLoseTargetDistance());

            EmitDecision(decision, target, distanceToTarget);
            if (decision != NpcCombatDecisionCore.Decision.ChaseTarget)
            {
                _chaseTarget = null;
            }

            switch (decision)
            {
                case NpcCombatDecisionCore.Decision.AcquireTarget:
                    break;
                case NpcCombatDecisionCore.Decision.ClearTarget:
                    ClearCombatTarget();
                    break;
                case NpcCombatDecisionCore.Decision.ChaseTarget:
                    StartChasingTarget(target);
                    break;
                case NpcCombatDecisionCore.Decision.HoldPosition:
                    HoldPosition();
                    break;
                case NpcCombatDecisionCore.Decision.Attack:
                    TryAttackCurrentTarget();
                    break;
            }
        }

        private bool TryAttackCurrentTarget()
        {
            if (_preset == null || _preset.AttackPreset == null)
            {
                EmitAttackFailure("NPC combat preset or attack preset is missing.");
                return false;
            }

            if (_attackController == null)
            {
                EmitAttackFailure("NPC attack controller is missing.");
                return false;
            }

            GameObject target = CurrentTarget;
            if (_preset.AttackPreset.RequireTarget && target == null)
            {
                EmitAttackFailure($"Preset '{_preset.AttackPreset.Id}' requires a target.");
                return false;
            }

            if (_preset.StopMovementInsideAttackRange)
            {
                CaptureNavigationModeIfNeeded();
                _navigation?.Stop();
            }

            if (_preset.FaceTargetBeforeAttack)
            {
                FaceTarget(target);
            }

            bool success = _attackController.TryUsePreset(_preset.AttackPreset, target, out string failReason);
            if (success)
            {
                _onAttackTriggered?.Invoke();
                return true;
            }

            EmitAttackFailure(failReason);
            return false;
        }

        private void StartChasingTarget(GameObject target)
        {
            if (target == null || _navigation == null)
            {
                return;
            }

            CaptureNavigationModeIfNeeded();

            // WHY: this runs every decision tick while chasing. Re-applying an unchanged target resets the
            // follow core and raises onTargetChanged each tick, so only the parts that drifted are applied.
            Transform targetTransform = target.transform;
            bool chaseStarted = _chaseTarget != target;
            bool pathRequested = false;
            if (_navigation.Mode != NpcNavigation.NavigationMode.FollowTarget)
            {
                // WHY: assigning the target first lets mode entry path straight to it instead of the stale target.
                _navigation.SetFollowTarget(targetTransform);
                _navigation.SetMode(NpcNavigation.NavigationMode.FollowTarget);
                pathRequested = true;
            }
            else if (_navigation.FollowTarget != targetTransform)
            {
                _navigation.SetFollowTarget(targetTransform);
                pathRequested = true;
            }

            if (chaseStarted)
            {
                _navigation.SetRunning(_preset.RunWhileChasing);
            }

            if (!_navigation.IsActive)
            {
                _navigation.Resume();
            }

            // WHY: Resume() keeps the path from before a hold, and with Auto Update Path off NpcNavigation never
            // re-paths on its own, so the brain refreshes it on a restarted chase and whenever the target moved.
            Vector3 targetPosition = targetTransform.position;
            if (!pathRequested && (chaseStarted || (!_navigation.AutoUpdatePath &&
                    (targetPosition - _chasePathTargetPosition).sqrMagnitude > ChaseRepathDistanceSqr)))
            {
                _navigation.RefreshPath();
                pathRequested = true;
            }

            if (pathRequested)
            {
                _chasePathTargetPosition = targetPosition;
            }

            if (chaseStarted)
            {
                _chaseTarget = target;
                _onChaseStarted?.Invoke();
            }
        }

        private void HoldPosition()
        {
            if (_preset != null && _preset.StopMovementInsideAttackRange)
            {
                CaptureNavigationModeIfNeeded();
                _navigation?.Stop();
            }

            _onHoldingPosition?.Invoke();
        }

        private void FaceTarget(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Transform pivot = _lookOrigin != null ? _lookOrigin : transform;
            Vector3 direction = target.transform.position - pivot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            pivot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private bool CanUseCurrentPreset()
        {
            if (_preset == null || _preset.AttackPreset == null || _preset.AttackPreset.AttackDefinition == null ||
                _attackController == null)
            {
                return false;
            }

            return _attackController.CanUsePreset(_preset.AttackPreset, out _);
        }

        private float ResolvePreferredAttackDistance()
        {
            if (_preset != null)
            {
                return _preset.PreferredAttackDistance;
            }

            return 1f;
        }

        private float ResolveLoseTargetDistance()
        {
            if (_preset != null)
            {
                return _preset.LoseTargetDistance;
            }

            return 10f;
        }

        private IRpgCombatReceiver ResolveActor()
        {
            if (_character != null)
            {
                return _character;
            }

            // WHY: GetComponent on every decision tick adds up across crowds, so a found receiver is cached.
            // A miss is not cached: a receiver added after Awake (code-built NPCs) or a replaced one is still
            // found, otherwise a dead or stunned NPC would keep acting.
            if (_actor == null || (_actor is Object actorObject && actorObject == null))
            {
                _actor = GetComponent<IRpgCombatReceiver>();
            }

            return _actor;
        }

        private void CaptureNavigationModeIfNeeded()
        {
            if (_navigation == null || _navigationModeCaptured || _preset == null || !_preset.AutoRestoreNavigationMode)
            {
                return;
            }

            _capturedNavigationMode = _navigation.Mode;
            _capturedFollowTarget = _navigation.FollowTarget;
            _capturedNavigationActive = _navigation.IsActive;
            _navigationModeCaptured = true;
        }

        private void RestoreNavigationModeIfNeeded()
        {
            if (_navigation == null || !_navigationModeCaptured)
            {
                return;
            }

            _navigationModeCaptured = false;
            _navigation.SetFollowTarget(_capturedFollowTarget);
            _navigation.SetMode(_capturedNavigationMode);
            if (_capturedNavigationActive)
            {
                _navigation.Resume();
            }
            else
            {
                _navigation.Stop();
            }
        }

        private void EmitDecision(NpcCombatDecisionCore.Decision decision, GameObject target, float distanceToTarget)
        {
            if (_previousDecision == decision)
            {
                return;
            }

            _previousDecision = decision;
            string targetName = target != null ? target.name : "none";
            string line = $"Decision={decision}, Target={targetName}, Distance={distanceToTarget:0.##}";
            _lastDecision = line;
            _onDecisionChanged?.Invoke(line);

            if (_debugMode)
            {
                NeoDiagnostics.Log($"[NpcRpgCombatBrain] {line}", this);
            }
        }

        private void EmitAttackFailure(string failReason)
        {
            string message = string.IsNullOrWhiteSpace(failReason) ? "NPC attack failed." : failReason;
            _onAttackFailed?.Invoke(message);

            if (_debugMode)
            {
                NeoDiagnostics.LogWarning($"[NpcRpgCombatBrain] {message}", this);
            }
        }
    }
}
