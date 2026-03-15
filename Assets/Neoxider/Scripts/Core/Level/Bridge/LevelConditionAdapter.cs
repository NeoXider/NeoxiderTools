using Neo.Condition;
using UnityEngine;

namespace Neo.Core.Level
{
    /// <summary>
    ///     Condition adapter for level/XP checks. Use with NeoCondition, StateMachine, etc.
    /// </summary>
    [AddComponentMenu("Neoxider/Core/Level Condition Adapter")]
    public sealed class LevelConditionAdapter : MonoBehaviour, IConditionEvaluator
    {
        [SerializeField] private LevelComponent _levelProvider;
        [SerializeField] private LevelConditionEvaluationMode _mode = LevelConditionEvaluationMode.LevelAtLeast;
        [SerializeField] [Min(0)] private int _threshold = 1;
        [SerializeField] private bool _invert;

        public bool LastResult { get; private set; }

        /// <inheritdoc />
        public bool Evaluate(GameObject context)
        {
            ILevelProvider provider = ResolveProvider(context);
            if (provider == null)
            {
                LastResult = false;
                return _invert;
            }

            bool result = _mode switch
            {
                LevelConditionEvaluationMode.LevelAtLeast => provider.Level >= _threshold,
                LevelConditionEvaluationMode.XpAtLeast => provider.TotalXp >= _threshold,
                LevelConditionEvaluationMode.XpToNextLevelAtMost => provider.XpToNextLevel <= _threshold,
                _ => false
            };

            LastResult = _invert ? !result : result;
            return LastResult;
        }

        public bool EvaluateCurrent()
        {
            return Evaluate(gameObject);
        }

        private ILevelProvider ResolveProvider(GameObject context)
        {
            if (_levelProvider != null)
            {
                return _levelProvider;
            }

            if (context != null && context.TryGetComponent(out LevelComponent comp))
            {
                return comp;
            }

            return null;
        }
    }
}
