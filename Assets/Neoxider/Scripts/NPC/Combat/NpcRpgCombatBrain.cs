using Neo;
using Neo.Rpg;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.NPC.Combat
{
    /// <summary>
    /// Modular NPC combat brain that combines navigation, target selection, and RPG attack presets.
    /// </summary>
    [DisallowMultipleComponent]
    [NeoDoc("NPC/Combat/NpcRpgCombatBrain.md")]
    [CreateFromMenu("Neoxider/NPC/NpcRpgCombatBrain")]
    [AddComponentMenu("Neoxider/NPC/Combat/" + nameof(NpcRpgCombatBrain))]
    public sealed class NpcRpgCombatBrain : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private bool _isActive = true;
        [SerializeField] private NpcCombatPreset _preset;
        [SerializeField] private NpcNavigation _navigation;
        [SerializeField] private RpgTargetSelector _targetSelector;
        [SerializeField] private RpgAttackController _attackController;
        [SerializeField] private RpgCombatant _combatant;
        [SerializeField] private RpgStatsManager _profileSource;
        [SerializeField] private Transform _lookOrigin;

        [Header("Behaviour")]
        [SerializeField] private bool _autoAcquireTarget = true;
        [SerializeField] private bool _disableAttackControllerInput = true;
        [SerializeField] private bool _clearTargetOnDisable = true;
        [SerializeField] [Min(0.05f)] private float _decisionInterval = 0.15f;

        [Header("Events")]
        [SerializeField] private RpgGameObjectEvent _onTargetAcquired = new();
        [SerializeField] private RpgGameObjectEvent _onTargetLost = new();
        [SerializeField] private UnityEvent _onChaseStarted = new();
        [SerializeField] private UnityEvent _onHoldingPosition = new();
        [SerializeField] private UnityEvent _onAttackTriggered = new();
        [SerializeField] private RpgStringEvent _onAttackFailed = new();
        [SerializeField] private RpgStringEvent _onDecisionChanged = new();

        [Header("Debug")]
        [SerializeField] private bool _debugMode;
        [SerializeField] [TextArea(2, 6)] private string _lastDecision;

        private float _nextDecisionTime;
        private bool _navigationModeCaptured;
        private NpcNavigation.NavigationMode _capturedNavigationMode;
        private NpcCombatDecisionCore.Decision? _previousDecision;

        /// <summary>
        /// Gets the current combat target.
        /// </summary>
        public GameObject CurrentTarget => _targetSelector != null ? _targetSelector.CurrentTarget : null;

        /// <summary>
        /// Gets whether the brain currently has a target.
        /// </summary>
        public bool HasTarget => CurrentTarget != null;

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
        /// Resolves missing references from the current GameObject.
        /// </summary>
        [Button]
        public void AutoResolveReferences()
        {
            _navigation ??= GetComponent<NpcNavigation>();
            _targetSelector ??= GetComponent<RpgTargetSelector>();
            _attackController ??= GetComponent<RpgAttackController>();
            _combatant ??= GetComponent<RpgCombatant>();
            _profileSource ??= GetComponent<RpgStatsManager>();
            _lookOrigin ??= transform;
        }

        /// <summary>
        /// Forces an immediate brain evaluation.
        /// </summary>
        [Button]
        public void EvaluateNow()
        {
            EvaluateBrain();
        }

        /// <summary>
        /// Selects a target using the attached selector.
        /// </summary>
        [Button]
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
        /// Clears the combat target and restores navigation if needed.
        /// </summary>
        [Button]
        public void ClearCombatTarget()
        {
            GameObject previousTarget = CurrentTarget;
            _targetSelector?.ClearTarget();
            RestoreNavigationModeIfNeeded();

            if (previousTarget != null)
            {
                _onTargetLost?.Invoke(previousTarget);
            }
        }

        /// <summary>
        /// Tries to execute the configured attack preset against the current target.
        /// </summary>
        [Button]
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

            switch (decision)
            {
                case NpcCombatDecisionCore.Decision.AcquireTarget:
                    if (_autoAcquireTarget)
                    {
                        AcquireTarget();
                    }

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

            GameObject target = CurrentTarget;
            if (_preset.AttackPreset.RequireTarget && target == null)
            {
                EmitAttackFailure($"Preset '{_preset.AttackPreset.Id}' requires a target.");
                return false;
            }

            if (_preset.StopMovementInsideAttackRange)
            {
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
            _navigation.SetMode(NpcNavigation.NavigationMode.FollowTarget);
            _navigation.SetFollowTarget(target.transform);
            _navigation.SetRunning(_preset.RunWhileChasing);
            _navigation.Resume();
            _onChaseStarted?.Invoke();
        }

        private void HoldPosition()
        {
            if (_preset != null && _preset.StopMovementInsideAttackRange)
            {
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
            if (_combatant != null)
            {
                return _combatant;
            }

            if (_profileSource != null)
            {
                return _profileSource;
            }

            return GetComponent<IRpgCombatReceiver>();
        }

        private void CaptureNavigationModeIfNeeded()
        {
            if (_navigation == null || _navigationModeCaptured || _preset == null || !_preset.AutoRestoreNavigationMode)
            {
                return;
            }

            _capturedNavigationMode = _navigation.Mode;
            _navigationModeCaptured = true;
        }

        private void RestoreNavigationModeIfNeeded()
        {
            if (_navigation == null || !_navigationModeCaptured || _preset == null || !_preset.AutoRestoreNavigationMode)
            {
                return;
            }

            _navigation.SetMode(_capturedNavigationMode);
            _navigation.Resume();
            _navigationModeCaptured = false;
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
                Debug.Log($"[NpcRpgCombatBrain] {line}", this);
            }
        }

        private void EmitAttackFailure(string failReason)
        {
            string message = string.IsNullOrWhiteSpace(failReason) ? "NPC attack failed." : failReason;
            _onAttackFailed?.Invoke(message);

            if (_debugMode)
            {
                Debug.LogWarning($"[NpcRpgCombatBrain] {message}", this);
            }
        }
    }
}
