using UnityEngine;
using UnityEngine.Events;

namespace Neo.Progression
{
    /// <summary>
    ///     Universal Inspector-driven bridge for progression actions.
    /// </summary>
    [NeoDoc("Progression/ProgressionNoCodeAction.md")]
    [CreateFromMenu("Neoxider/Progression/Progression NoCode Action")]
    [AddComponentMenu("Neoxider/Progression/" + nameof(ProgressionNoCodeAction))]
    public sealed class ProgressionNoCodeAction : MonoBehaviour
    {
        [Header("Action")] [SerializeField]
        private ProgressionNoCodeActionType _actionType = ProgressionNoCodeActionType.AddXp;

        [SerializeField] private ProgressionManager _manager;
        [SerializeField] [Min(0)] private int _xpAmount = 25;
        [SerializeField] [Min(0)] private int _perkPointsAmount = 1;
        [SerializeField] private string _nodeId = string.Empty;
        [SerializeField] private string _perkId = string.Empty;

        [Header("Events")] [SerializeField] private UnityEvent _onSuccess = new();
        [SerializeField] private ProgressionStringEvent _onFailed = new();
        [SerializeField] private ProgressionStringEvent _onResultMessage = new();

        /// <summary>
        ///     Executes the configured action.
        /// </summary>
        public void Execute()
        {
            if (!TryGetManager(out ProgressionManager manager))
            {
                return;
            }

            switch (_actionType)
            {
                case ProgressionNoCodeActionType.AddXp:
                    manager.AddXp(_xpAmount);
                    EmitSuccess($"Added XP: {_xpAmount}");
                    break;
                case ProgressionNoCodeActionType.GrantPerkPoints:
                    manager.AddPerkPoints(_perkPointsAmount);
                    EmitSuccess($"Granted perk points: {_perkPointsAmount}");
                    break;
                case ProgressionNoCodeActionType.UnlockNode:
                    if (manager.TryUnlockNode(_nodeId, out string unlockError))
                    {
                        EmitSuccess($"Unlocked node: {_nodeId}");
                    }
                    else
                    {
                        EmitFailed(unlockError);
                    }

                    break;
                case ProgressionNoCodeActionType.BuyPerk:
                    if (manager.TryBuyPerk(_perkId, out string perkError))
                    {
                        EmitSuccess($"Purchased perk: {_perkId}");
                    }
                    else
                    {
                        EmitFailed(perkError);
                    }

                    break;
                case ProgressionNoCodeActionType.ResetProgression:
                    manager.ResetProgression();
                    EmitSuccess("Progression reset.");
                    break;
                case ProgressionNoCodeActionType.SaveProfile:
                    manager.SaveProfile();
                    EmitSuccess("Progression profile saved.");
                    break;
                case ProgressionNoCodeActionType.LoadProfile:
                    manager.LoadProfile();
                    EmitSuccess("Progression profile loaded.");
                    break;
            }
        }

        private bool TryGetManager(out ProgressionManager manager)
        {
            manager = _manager != null ? _manager : ProgressionManager.Instance;
            if (manager != null)
            {
                return true;
            }

            EmitFailed("ProgressionManager not found.");
            return false;
        }

        private void EmitSuccess(string message)
        {
            _onSuccess?.Invoke();
            _onResultMessage?.Invoke(message);
        }

        private void EmitFailed(string message)
        {
            string resolvedMessage = string.IsNullOrWhiteSpace(message) ? "Progression action failed." : message;
            _onFailed?.Invoke(resolvedMessage);
            _onResultMessage?.Invoke(resolvedMessage);
        }
    }
}
