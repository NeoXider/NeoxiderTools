using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Core.Level
{
    /// <summary>
    ///     NoCode actions for level/XP: AddXp, SetLevel. Targets a LevelComponent through ILevelProvider.
    /// </summary>
    [CreateFromMenu("Neoxider/Core/Level NoCode Action")]
    [AddComponentMenu("Neoxider/Core/Level NoCode Action")]
    [NeoDoc("Core/Level/Bridge/LevelNoCodeAction.md")]
    public sealed class LevelNoCodeAction : MonoBehaviour
    {
        [Header("Target")] [SerializeField] private LevelComponent _levelProvider;

        [Header("Action")] [SerializeField] private LevelNoCodeActionType _actionType = LevelNoCodeActionType.AddXp;

        [SerializeField] [Min(0)] private int _xpAmount = 25;
        [SerializeField] [Min(1)] private int _level = 1;

        [Header("Events")] [SerializeField] private UnityEvent _onSuccess = new();

        [SerializeField] private UnityEventInt _onLevelUp = new();

        /// <summary>Gets or sets the explicit level provider (falls back to a sibling LevelComponent).</summary>
        public LevelComponent LevelProvider
        {
            get => _levelProvider;
            set => _levelProvider = value;
        }

        /// <summary>Gets or sets the configured action type.</summary>
        public LevelNoCodeActionType ActionType
        {
            get => _actionType;
            set => _actionType = value;
        }

        /// <summary>Gets or sets the XP amount used by the AddXp action (UnityEvent-callable via SetXpAmount).</summary>
        public int XpAmount
        {
            get => _xpAmount;
            set => _xpAmount = value < 0 ? 0 : value;
        }

        /// <summary>Gets or sets the target level used by the SetLevel action.</summary>
        public int TargetLevel
        {
            get => _level;
            set => _level = value < 1 ? 1 : value;
        }

        /// <summary>Gets the UnityEvent raised after a successful action.</summary>
        public UnityEvent OnSuccess => _onSuccess;

        /// <summary>Gets the level-change event, using the result captured before OnSuccess listeners run.</summary>
        public UnityEventInt OnLevelUp => _onLevelUp;

        /// <summary>Sets the XP amount (1-arg overload for UnityEvent wiring).</summary>
        public void SetXpAmount(int amount)
        {
            XpAmount = amount;
        }

        /// <summary>Sets the target level (1-arg overload for UnityEvent wiring).</summary>
        public void SetTargetLevel(int level)
        {
            TargetLevel = level;
        }

        /// <summary>
        ///     Executes the configured action on the resolved level provider.
        /// </summary>
        public void Execute()
        {
            ILevelProvider provider = ResolveProvider();
            if (provider == null)
            {
                return;
            }

            int previousLevel = provider.Level;
            switch (_actionType)
            {
                case LevelNoCodeActionType.AddXp:
                    provider.AddXp(_xpAmount);
                    break;
                case LevelNoCodeActionType.SetLevel:
                    provider.SetLevel(_level);
                    break;
                default:
                    return;
            }

            // WHY: success listeners can mutate the provider; report the completed action's result.
            int resultingLevel = provider.Level;
            _onSuccess?.Invoke();
            if (resultingLevel != previousLevel)
            {
                _onLevelUp?.Invoke(resultingLevel);
            }
        }

        private ILevelProvider ResolveProvider()
        {
            if (_levelProvider != null)
            {
                return _levelProvider;
            }

            if (gameObject.TryGetComponent(out LevelComponent comp))
            {
                return comp;
            }

            return null;
        }
    }
}
