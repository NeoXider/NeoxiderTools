using Neo.Condition;
using Neo.Reactive;
using Neo.Rpg.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Bridge that exposes <see cref="RpgCharacter"/> state as a NeoCondition predicate.
    /// </summary>
    [NeoDoc("Rpg/RpgConditionAdapter.md")]
    [CreateFromMenu("Neoxider/RPG/Rpg Condition Adapter")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgConditionAdapter))]
    public sealed class RpgConditionAdapter : MonoBehaviour, IConditionEvaluator
    {
        [Tooltip("Character to query. If empty, searches the context GameObject's hierarchy.")] [SerializeField]
        private RpgCharacter _character;

        [SerializeField] private RpgEvadeController _evadeController;
        [SerializeField] private RpgAttackController _attackController;
        [SerializeField] private RpgConditionEvaluationMode _mode = RpgConditionEvaluationMode.HpAtLeast;
        [SerializeField] [Min(0f)] private float _threshold = 50f;
        [SerializeField] [Min(1)] private int _levelThreshold = 1;
        [SerializeField] private RpgStatId _resource = new(RpgStatPreset.Hp);
        [SerializeField] private RpgStatId _stat = new(RpgStatPreset.Strength);
        [SerializeField] [HideInInspector] private string _resourceId = string.Empty;
        [SerializeField] private string _buffId = string.Empty;
        [SerializeField] private string _statusId = string.Empty;
        [SerializeField] private string _attackId = string.Empty;
        [SerializeField] private bool _invert;

        [Header("Events")] [SerializeField] private UnityEventBool _onEvaluated = new();
        [SerializeField] private UnityEvent _onTrue = new();
        [SerializeField] private UnityEvent _onFalse = new();

        public bool LastResult { get; private set; }

        public bool Evaluate(GameObject context)
        {
            RpgCharacter character = ResolveCharacter(context);
            if (character == null)
            {
                LastResult = _invert;
                return LastResult;
            }

            bool result = _mode switch
            {
                RpgConditionEvaluationMode.HpAtLeast => character.HpValue >= _threshold,
                RpgConditionEvaluationMode.HpPercentAtLeast => character.HpPercentValue >= _threshold / 100f,
                RpgConditionEvaluationMode.LevelAtLeast => character.LevelValue >= _levelThreshold,
                RpgConditionEvaluationMode.IsDead => character.IsDead,
                RpgConditionEvaluationMode.HasBuff => character.HasBuff(_buffId),
                RpgConditionEvaluationMode.HasStatus => character.HasStatus(_statusId),
                RpgConditionEvaluationMode.CanPerformActions => character.CanPerformActions,
                RpgConditionEvaluationMode.IsInvulnerable => character.IsInvulnerable,
                RpgConditionEvaluationMode.CanEvade => _evadeController != null && _evadeController.CanEvade,
                RpgConditionEvaluationMode.AttackReady => _attackController != null &&
                                                          _attackController.CanUseAttack(_attackId, out _),
                RpgConditionEvaluationMode.ResourceAtLeast => character.GetResource(ResourceId) >= _threshold,
                RpgConditionEvaluationMode.ResourceBelow => character.GetResource(ResourceId) < _threshold,
                RpgConditionEvaluationMode.ResourcePercentAtLeast =>
                    character.GetResourcePercent(ResourceId) >= _threshold / 100f,
                RpgConditionEvaluationMode.ResourcePercentBelow =>
                    character.GetResourcePercent(ResourceId) < _threshold / 100f,
                RpgConditionEvaluationMode.StatAtLeast => character.GetStat(StatId) >= _threshold,
                RpgConditionEvaluationMode.StatBelow => character.GetStat(StatId) < _threshold,
                RpgConditionEvaluationMode.UpgradePointsAtLeast => character.UpgradePointsValue >= _levelThreshold,
                RpgConditionEvaluationMode.UpgradeLevelAtLeast => character.GetUpgradeLevel(StatId) >= _levelThreshold,
                RpgConditionEvaluationMode.XpAtLeast => character.XpValue >= _threshold,
                _ => false
            };

            LastResult = _invert ? !result : result;
            _onEvaluated?.Invoke(LastResult);
            if (LastResult)
            {
                _onTrue?.Invoke();
            }
            else
            {
                _onFalse?.Invoke();
            }

            return LastResult;
        }

        [Button]
        public bool EvaluateCurrent()
        {
            return Evaluate(gameObject);
        }

        private RpgCharacter ResolveCharacter(GameObject context)
        {
            if (_character != null)
            {
                return _character;
            }

            if (context == null)
            {
                return null;
            }

            return context.TryGetComponent(out RpgCharacter c) ? c : context.GetComponentInParent<RpgCharacter>();
        }

        private string ResourceId =>
            !string.IsNullOrWhiteSpace(_resourceId) ? _resourceId.Trim() : _resource.Value;

        private string StatId => _stat.Value;
    }
}
