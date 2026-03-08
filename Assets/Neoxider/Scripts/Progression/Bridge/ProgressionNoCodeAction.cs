using Neo.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Progression
{
    /// <summary>
    /// Universal Inspector-driven bridge for progression actions.
    /// </summary>
    [NeoDoc("Progression/ProgressionNoCodeAction.md")]
    [CreateFromMenu("Neoxider/Progression/Progression NoCode Action")]
    [AddComponentMenu("Neoxider/Progression/" + nameof(ProgressionNoCodeAction))]
    public sealed class ProgressionNoCodeAction : MonoBehaviour
    {
        public enum ActionType
        {
            AddXp,
            GrantPerkPoints,
            UnlockNode,
            BuyPerk,
            ResetProgression,
            SaveProfile,
            LoadProfile
        }

        [Header("Action")] [SerializeField] private ActionType _actionType = ActionType.AddXp;
        [SerializeField] private ProgressionManager _manager;
        [SerializeField] [Min(0)] private int _xpAmount = 25;
        [SerializeField] [Min(0)] private int _perkPointsAmount = 1;
        [SerializeField] private string _nodeId = string.Empty;
        [SerializeField] private string _perkId = string.Empty;

        [Header("Events")] [SerializeField] private UnityEvent _onSuccess = new();
        [SerializeField] private ProgressionStringEvent _onFailed = new();
        [SerializeField] private ProgressionStringEvent _onResultMessage = new();

        /// <summary>
        /// Executes the configured action.
        /// </summary>
        public void Execute()
        {
            if (!TryGetManager(out ProgressionManager manager))
            {
                return;
            }

            switch (_actionType)
            {
                case ActionType.AddXp:
                    manager.AddXp(_xpAmount);
                    EmitSuccess($"Added XP: {_xpAmount}");
                    break;
                case ActionType.GrantPerkPoints:
                    manager.AddPerkPoints(_perkPointsAmount);
                    EmitSuccess($"Granted perk points: {_perkPointsAmount}");
                    break;
                case ActionType.UnlockNode:
                    if (manager.TryUnlockNode(_nodeId, out string unlockError))
                    {
                        EmitSuccess($"Unlocked node: {_nodeId}");
                    }
                    else
                    {
                        EmitFailed(unlockError);
                    }

                    break;
                case ActionType.BuyPerk:
                    if (manager.TryBuyPerk(_perkId, out string perkError))
                    {
                        EmitSuccess($"Purchased perk: {_perkId}");
                    }
                    else
                    {
                        EmitFailed(perkError);
                    }

                    break;
                case ActionType.ResetProgression:
                    manager.ResetProgression();
                    EmitSuccess("Progression reset.");
                    break;
                case ActionType.SaveProfile:
                    manager.SaveProfile();
                    EmitSuccess("Progression profile saved.");
                    break;
                case ActionType.LoadProfile:
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
