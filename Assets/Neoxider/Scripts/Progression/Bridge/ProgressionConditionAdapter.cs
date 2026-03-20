using Neo.Condition;
using UnityEngine;

namespace Neo.Progression
{
    /// <summary>
    ///     Condition adapter that exposes progression checks to other no-code systems.
    /// </summary>
    [NeoDoc("Progression/ProgressionConditionAdapter.md")]
    [CreateFromMenu("Neoxider/Progression/Progression Condition Adapter")]
    [AddComponentMenu("Neoxider/Progression/" + nameof(ProgressionConditionAdapter))]
    public sealed class ProgressionConditionAdapter : MonoBehaviour, IConditionEvaluator
    {
        [SerializeField] private ProgressionManager _manager;

        [SerializeField]
        private ProgressionConditionEvaluationMode _mode = ProgressionConditionEvaluationMode.LevelAtLeast;

        [SerializeField] private string _nodeId = string.Empty;
        [SerializeField] private string _perkId = string.Empty;
        [SerializeField] [Min(0)] private int _threshold = 1;
        [SerializeField] private bool _invert;

        /// <summary>
        ///     Gets the last evaluated result.
        /// </summary>
        public bool LastResult { get; private set; }

        /// <summary>
        ///     Evaluates the configured progression condition.
        /// </summary>
        public bool Evaluate(GameObject context)
        {
            ProgressionManager manager = ResolveManager(context);
            if (manager == null)
            {
                LastResult = false;
                return _invert ? !LastResult : LastResult;
            }

            bool result = _mode switch
            {
                ProgressionConditionEvaluationMode.HasUnlockedNode => manager.HasUnlockedNode(_nodeId),
                ProgressionConditionEvaluationMode.HasPurchasedPerk => manager.HasPurchasedPerk(_perkId),
                ProgressionConditionEvaluationMode.LevelAtLeast => manager.CurrentLevel >= _threshold,
                ProgressionConditionEvaluationMode.XpAtLeast => manager.TotalXp >= _threshold,
                ProgressionConditionEvaluationMode.PerkPointsAtLeast => manager.AvailablePerkPoints >= _threshold,
                _ => false
            };

            LastResult = _invert ? !result : result;
            return LastResult;
        }

        /// <summary>
        ///     Evaluates the configured progression condition using this component as the context.
        /// </summary>
        public bool EvaluateCurrent()
        {
            return Evaluate(gameObject);
        }

        private ProgressionManager ResolveManager(GameObject context)
        {
            if (_manager != null)
            {
                return _manager;
            }

            if (context != null && context.TryGetComponent(out ProgressionManager localManager))
            {
                return localManager;
            }

            return ProgressionManager.Instance;
        }
    }
}
