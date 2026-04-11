using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Universal Inspector-driven bridge for RPG actions.
    /// </summary>
    [NeoDoc("Rpg/RpgNoCodeAction.md")]
    [CreateFromMenu("Neoxider/RPG/Rpg NoCode Action")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgNoCodeAction))]
    public sealed class RpgNoCodeAction : MonoBehaviour
    {
        [Header("Action")] [SerializeField] private RpgNoCodeActionType _actionType = RpgNoCodeActionType.Heal;
        [SerializeField] private RpgStatsManager _manager;
        [SerializeField] [Min(0f)] private float _amount = 25f;
        [SerializeField] [Min(1)] private int _level = 1;
        [SerializeField] private string _buffId = string.Empty;
        [SerializeField] private string _statusId = string.Empty;
        [SerializeField] private string _attackId = string.Empty;
        [SerializeField] private string _presetId = string.Empty;
        [SerializeField] private RpgAttackController _attackController;
        [SerializeField] private RpgEvadeController _evadeController;

        [Header("Events")] [SerializeField] private UnityEvent _onSuccess = new();
        [SerializeField] private RpgStringEvent _onFailed = new();
        [SerializeField] private RpgStringEvent _onResultMessage = new();

        /// <summary>
        ///     Executes the configured action.
        /// </summary>
        [Button]
        public void Execute()
        {
            RpgStatsManager manager = null;
            if (RequiresManager() && !TryGetManager(out manager))
            {
                return;
            }

            switch (_actionType)
            {
                case RpgNoCodeActionType.TakeDamage:
                    manager.TakeDamage(new RpgDamageInfo(_amount, null, null));
                    EmitSuccess($"Took damage: {_amount}");
                    break;
                case RpgNoCodeActionType.Heal:
                    manager.Heal(_amount);
                    EmitSuccess($"Healed: {_amount}");
                    break;
                case RpgNoCodeActionType.SetMaxHp:
                    manager.SetMaxHp(_amount);
                    EmitSuccess($"Set max HP: {_amount}");
                    break;
                case RpgNoCodeActionType.SetLevel:
                    manager.SetLevel(_level);
                    EmitSuccess($"Set level: {_level}");
                    break;
                case RpgNoCodeActionType.ApplyBuff:
                    if (manager.TryApplyBuff(_buffId, out string buffError))
                    {
                        EmitSuccess($"Applied buff: {_buffId}");
                    }
                    else
                    {
                        EmitFailed(buffError);
                    }

                    break;
                case RpgNoCodeActionType.ApplyStatus:
                    if (manager.TryApplyStatus(_statusId, out string statusError))
                    {
                        EmitSuccess($"Applied status: {_statusId}");
                    }
                    else
                    {
                        EmitFailed(statusError);
                    }

                    break;
                case RpgNoCodeActionType.RemoveBuff:
                    manager.RemoveBuff(_buffId);
                    EmitSuccess($"Removed buff: {_buffId}");
                    break;
                case RpgNoCodeActionType.RemoveStatus:
                    manager.RemoveStatus(_statusId);
                    EmitSuccess($"Removed status: {_statusId}");
                    break;
                case RpgNoCodeActionType.UseAttackById:
                    string attackError = null;
                    if (_attackController != null && _attackController.TryUseAttack(_attackId, out attackError))
                    {
                        EmitSuccess($"Used attack: {_attackId}");
                    }
                    else
                    {
                        EmitFailed(string.IsNullOrWhiteSpace(attackError)
                            ? "Attack controller not assigned."
                            : attackError);
                    }

                    break;
                case RpgNoCodeActionType.UsePrimaryAttack:
                    if (_attackController != null && _attackController.UsePrimaryAttack())
                    {
                        EmitSuccess("Used primary attack.");
                    }
                    else
                    {
                        EmitFailed("Primary attack failed.");
                    }

                    break;
                case RpgNoCodeActionType.UsePresetById:
                    string presetError = null;
                    if (_attackController != null && _attackController.TryUsePreset(_presetId, out presetError))
                    {
                        EmitSuccess($"Used preset: {_presetId}");
                    }
                    else
                    {
                        EmitFailed(string.IsNullOrWhiteSpace(presetError) ? "Preset attack failed." : presetError);
                    }

                    break;
                case RpgNoCodeActionType.UsePrimaryPreset:
                    if (_attackController != null && _attackController.UsePrimaryPreset())
                    {
                        EmitSuccess("Used primary preset.");
                    }
                    else
                    {
                        EmitFailed("Primary preset failed.");
                    }

                    break;
                case RpgNoCodeActionType.StartEvade:
                    if (_evadeController != null && _evadeController.TryStartEvade())
                    {
                        EmitSuccess("Evade started.");
                    }
                    else
                    {
                        EmitFailed("Evade failed.");
                    }

                    break;
                case RpgNoCodeActionType.ResetProfile:
                    manager.ResetProfile();
                    EmitSuccess("RPG profile reset.");
                    break;
                case RpgNoCodeActionType.SaveProfile:
                    manager.SaveProfile();
                    EmitSuccess("RPG profile saved.");
                    break;
                case RpgNoCodeActionType.LoadProfile:
                    manager.LoadProfile();
                    EmitSuccess("RPG profile loaded.");
                    break;
            }
        }

        private bool TryGetManager(out RpgStatsManager manager)
        {
            manager = _manager != null ? _manager : RpgStatsManager.Instance;
            if (manager != null)
            {
                return true;
            }

            EmitFailed("RpgStatsManager not found.");
            return false;
        }

        private bool RequiresManager()
        {
            return _actionType != RpgNoCodeActionType.UseAttackById
                   && _actionType != RpgNoCodeActionType.UsePrimaryAttack
                   && _actionType != RpgNoCodeActionType.UsePresetById
                   && _actionType != RpgNoCodeActionType.UsePrimaryPreset
                   && _actionType != RpgNoCodeActionType.StartEvade;
        }

        private void EmitSuccess(string message)
        {
            _onSuccess?.Invoke();
            _onResultMessage?.Invoke(message);
        }

        private void EmitFailed(string message)
        {
            string resolvedMessage = string.IsNullOrWhiteSpace(message) ? "RPG action failed." : message;
            _onFailed?.Invoke(resolvedMessage);
            _onResultMessage?.Invoke(resolvedMessage);
        }
    }
}
