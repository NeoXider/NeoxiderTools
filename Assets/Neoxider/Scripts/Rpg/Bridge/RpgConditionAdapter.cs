using Neo.Condition;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// Condition adapter that exposes RPG checks to other no-code systems.
    /// </summary>
    [NeoDoc("Rpg/RpgConditionAdapter.md")]
    [CreateFromMenu("Neoxider/RPG/Rpg Condition Adapter")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgConditionAdapter))]
    public sealed class RpgConditionAdapter : MonoBehaviour, IConditionEvaluator
    {
        public enum EvaluationMode
        {
            HpAtLeast,
            HpPercentAtLeast,
            LevelAtLeast,
            IsDead,
            HasBuff,
            HasStatus,
            CanPerformActions,
            IsInvulnerable,
            CanEvade,
            AttackReady
        }

        [SerializeField] private RpgStatsManager _manager;
        [SerializeField] private RpgCombatant _combatant;
        [SerializeField] private RpgEvadeController _evadeController;
        [SerializeField] private RpgAttackController _attackController;
        [SerializeField] private EvaluationMode _mode = EvaluationMode.HpAtLeast;
        [SerializeField] [Min(0f)] private float _threshold = 50f;
        [SerializeField] [Min(1)] private int _levelThreshold = 1;
        [SerializeField] private string _buffId = string.Empty;
        [SerializeField] private string _statusId = string.Empty;
        [SerializeField] private string _attackId = string.Empty;
        [SerializeField] private bool _invert;

        /// <summary>
        /// Gets the last evaluated result.
        /// </summary>
        public bool LastResult { get; private set; }

        /// <summary>
        /// Evaluates the configured RPG condition.
        /// </summary>
        public bool Evaluate(GameObject context)
        {
            IRpgCombatReceiver receiver = ResolveReceiver(context);
            if (receiver == null)
            {
                LastResult = false;
                return _invert ? !LastResult : LastResult;
            }

            bool result = _mode switch
            {
                EvaluationMode.HpAtLeast => receiver.CurrentHp >= _threshold,
                EvaluationMode.HpPercentAtLeast => receiver.MaxHp > 0f && (receiver.CurrentHp / receiver.MaxHp) >= (_threshold / 100f),
                EvaluationMode.LevelAtLeast => receiver.Level >= _levelThreshold,
                EvaluationMode.IsDead => receiver.IsDead,
                EvaluationMode.HasBuff => HasBuff(receiver, _buffId),
                EvaluationMode.HasStatus => HasStatus(receiver, _statusId),
                EvaluationMode.CanPerformActions => receiver.CanPerformActions,
                EvaluationMode.IsInvulnerable => receiver.IsInvulnerable,
                EvaluationMode.CanEvade => _evadeController != null && _evadeController.CanEvade,
                EvaluationMode.AttackReady => _attackController != null && _attackController.CanUseAttack(_attackId, out _),
                _ => false
            };

            LastResult = _invert ? !result : result;
            return LastResult;
        }

        /// <summary>
        /// Evaluates the configured RPG condition using this component as the context.
        /// </summary>
        public bool EvaluateCurrent()
        {
            return Evaluate(gameObject);
        }

        private IRpgCombatReceiver ResolveReceiver(GameObject context)
        {
            if (_combatant != null)
            {
                return _combatant;
            }

            if (_manager != null)
            {
                return _manager;
            }

            if (context != null && context.TryGetComponent(out RpgCombatant localCombatant))
            {
                return localCombatant;
            }

            if (context != null && context.TryGetComponent(out RpgStatsManager localManager))
            {
                return localManager;
            }

            return RpgStatsManager.Instance;
        }

        private static bool HasBuff(IRpgCombatReceiver receiver, string buffId)
        {
            if (receiver is RpgCombatant combatant)
            {
                return combatant.HasBuff(buffId);
            }

            if (receiver is RpgStatsManager manager)
            {
                return manager.HasBuff(buffId);
            }

            return false;
        }

        private static bool HasStatus(IRpgCombatReceiver receiver, string statusId)
        {
            if (receiver is RpgCombatant combatant)
            {
                return combatant.HasStatus(statusId);
            }

            if (receiver is RpgStatsManager manager)
            {
                return manager.HasStatus(statusId);
            }

            return false;
        }
    }
}
