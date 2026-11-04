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
        [Tooltip("Explicit manager. Empty: a manager on the context GameObject, then the singleton.")] [SerializeField]
        private ProgressionManager _manager;

        [Tooltip("Which progression fact is checked.")] [SerializeField]
        private ProgressionConditionEvaluationMode _mode = ProgressionConditionEvaluationMode.LevelAtLeast;

        [Tooltip("Unlock node id for HasUnlockedNode.")] [SerializeField]
        private string _nodeId = string.Empty;

        [Tooltip("Perk id for HasPurchasedPerk.")] [SerializeField]
        private string _perkId = string.Empty;

        [Tooltip("Minimum value for LevelAtLeast, XpAtLeast and PerkPointsAtLeast.")] [SerializeField] [Min(0)]
        private int _threshold = 1;

        [Tooltip("Negate the result.")] [SerializeField]
        private bool _invert;

        /// <summary>Gets or sets the explicit manager (falls back to the context GameObject, then the singleton).</summary>
        public ProgressionManager Manager
        {
            get => _manager;
            set => _manager = value;
        }

        /// <summary>Gets or sets the evaluated progression fact.</summary>
        public ProgressionConditionEvaluationMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>Gets or sets the node id used by HasUnlockedNode.</summary>
        public string NodeId
        {
            get => _nodeId;
            set => _nodeId = value ?? string.Empty;
        }

        /// <summary>Gets or sets the perk id used by HasPurchasedPerk.</summary>
        public string PerkId
        {
            get => _perkId;
            set => _perkId = value ?? string.Empty;
        }

        /// <summary>Gets or sets the minimum value used by the AtLeast modes (clamped to zero).</summary>
        public int Threshold
        {
            get => _threshold;
            set => _threshold = value < 0 ? 0 : value;
        }

        /// <summary>Gets or sets whether the result is negated.</summary>
        public bool Invert
        {
            get => _invert;
            set => _invert = value;
        }

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
                LastResult = _invert;
                return LastResult;
            }

            manager.EnsureInitialized();
            bool result = _mode switch
            {
                ProgressionConditionEvaluationMode.HasUnlockedNode => manager.HasUnlockedNode(_nodeId),
                ProgressionConditionEvaluationMode.HasPurchasedPerk => manager.HasPurchasedPerk(_perkId),
                ProgressionConditionEvaluationMode.LevelAtLeast => manager.CurrentLevel >= _threshold,
                ProgressionConditionEvaluationMode.XpAtLeast => manager.TotalXp >= _threshold,
                ProgressionConditionEvaluationMode.PerkPointsAtLeast => manager.AvailablePerkPoints >= _threshold,
                ProgressionConditionEvaluationMode.HasPremium => manager.HasPremium,
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
