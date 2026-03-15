using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Core.Level
{
    /// <summary>
    ///     NoCode actions for level/XP: AddXp, SetLevel. Target: LevelComponent or any ILevelProvider.
    /// </summary>
    [AddComponentMenu("Neoxider/Core/Level NoCode Action")]
    public sealed class LevelNoCodeAction : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private LevelComponent _levelProvider;

        [Header("Action")]
        [SerializeField] private LevelNoCodeActionType _actionType = LevelNoCodeActionType.AddXp;
        [SerializeField] [Min(0)] private int _xpAmount = 25;
        [SerializeField] [Min(1)] private int _level = 1;

        [Header("Events")]
        [SerializeField] private UnityEvent _onSuccess = new();
        [SerializeField] private UnityEventInt _onLevelUp = new();

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

            switch (_actionType)
            {
                case LevelNoCodeActionType.AddXp:
                    provider.AddXp(_xpAmount);
                    _onSuccess?.Invoke();
                    _onLevelUp?.Invoke(provider.Level);
                    break;
                case LevelNoCodeActionType.SetLevel:
                    provider.SetLevel(_level);
                    _onSuccess?.Invoke();
                    break;
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
