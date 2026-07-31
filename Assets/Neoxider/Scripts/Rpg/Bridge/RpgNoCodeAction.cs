using Neo.Rpg.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Inspector-driven bridge that converts an action selector + parameters into a method call
    ///     on a target <see cref="RpgCharacter"/>. Replaces the legacy singleton flow.
    /// </summary>
    [NeoDoc("Rpg/RpgNoCodeAction.md")]
    [CreateFromMenu("Neoxider/RPG/Rpg NoCode Action")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgNoCodeAction))]
    public sealed class RpgNoCodeAction : MonoBehaviour
    {
        [Header("Action")] [SerializeField] private RpgNoCodeActionType _actionType = RpgNoCodeActionType.Heal;

        [Tooltip("Target character. When empty, searches up the hierarchy on Execute().")] [SerializeField]
        private RpgCharacter _character;

        [SerializeField] private float _amount = 25f;
        [SerializeField] [Min(1)] private int _level = 1;
        [SerializeField] private int _intAmount = 1;
        [SerializeField] private string _buffId = string.Empty;
        [SerializeField] private string _statusId = string.Empty;
        [SerializeField] private RpgStatId _resource = new(RpgStatPreset.Mana);
        [SerializeField] private RpgStatId _stat = new(RpgStatPreset.Strength);
        [SerializeField] [HideInInspector] private string _resourceId = string.Empty;
        [SerializeField] [Min(0)] private int _inlineBuffIndex;
        [SerializeField] private bool _boolValue = true;
        [SerializeField] private string _attackId = string.Empty;
        [SerializeField] private string _presetId = string.Empty;
        [SerializeField] private RpgAttackController _attackController;
        [SerializeField] private RpgEvadeController _evadeController;

        [Header("Events")] [SerializeField] private UnityEvent _onSuccess = new();
        [SerializeField] private RpgStringEvent _onFailed = new();
        [SerializeField] private RpgStringEvent _onResultMessage = new();

        /// <summary>Raised after any action succeeds. Mirrors the other NoCode bridges for code subscription.</summary>
        public UnityEvent OnSuccess => _onSuccess;

        /// <summary>Raised with the failure reason when an action is rejected (e.g. no target, dead character).</summary>
        public RpgStringEvent OnFailed => _onFailed;

        /// <summary>Raised with a human-readable result message for every Execute (success or failure).</summary>
        public RpgStringEvent OnResultMessage => _onResultMessage;

        [Button]
        public void Execute()
        {
            RpgCharacter character = null;
            if (RequiresCharacter() && !TryGetCharacter(out character))
            {
                return;
            }

            switch (_actionType)
            {
                case RpgNoCodeActionType.TakeDamage:
                    character.Damage(_amount);
                    EmitSuccess($"Damage: {_amount}");
                    break;
                case RpgNoCodeActionType.Heal:
                    character.Heal(_amount);
                    EmitSuccess($"Healed: {_amount}");
                    break;
                case RpgNoCodeActionType.SetMaxHp:
                    character.SetMaxResource(Core.Resources.RpgResourceId.Hp, _amount);
                    EmitSuccess($"Set max HP: {_amount}");
                    break;
                case RpgNoCodeActionType.SetMaxResource:
                    character.SetMaxResource(ResourceId, _amount);
                    EmitSuccess($"Set max {ResourceId}: {_amount}");
                    break;
                case RpgNoCodeActionType.AddMaxResource:
                    character.AddMaxResource(ResourceId, _amount);
                    EmitSuccess($"Add max {ResourceId}: {_amount}");
                    break;
                case RpgNoCodeActionType.SpendResource:
                    if (character.Spend(ResourceId, _amount))
                    {
                        EmitSuccess($"Spent {ResourceId}: {_amount}");
                    }
                    else
                    {
                        EmitFailed($"Cannot spend {ResourceId}: {_amount}");
                    }

                    break;
                case RpgNoCodeActionType.RefillResource:
                    character.Refill(ResourceId, _amount);
                    EmitSuccess($"Refilled {ResourceId}: {_amount}");
                    break;
                case RpgNoCodeActionType.RestoreResource:
                    character.RestoreResource(ResourceId);
                    EmitSuccess($"Restored {ResourceId}");
                    break;
                case RpgNoCodeActionType.RestoreAllResources:
                    character.Restore();
                    EmitSuccess("Restored all resources.");
                    break;
                case RpgNoCodeActionType.AddStatBase:
                    character.AddStatBase(StatId, _amount);
                    EmitSuccess($"Add stat {StatId}: {_amount}");
                    break;
                case RpgNoCodeActionType.SetStatBase:
                    character.SetStatBase(StatId, _amount);
                    EmitSuccess($"Set stat {StatId}: {_amount}");
                    break;
                case RpgNoCodeActionType.SetLevel:
                    character.SetLevel(_level);
                    EmitSuccess($"Set level: {_level}");
                    break;
                case RpgNoCodeActionType.AddLevel:
                    character.AddLevel(_intAmount);
                    EmitSuccess($"Add level: {_intAmount}");
                    break;
                case RpgNoCodeActionType.AddXp:
                    character.AddXp(_amount);
                    EmitSuccess($"Add XP: {_amount}");
                    break;
                case RpgNoCodeActionType.AddUpgradePoints:
                    character.AddUpgradePoints(_intAmount);
                    EmitSuccess($"Add upgrade points: {_intAmount}");
                    break;
                case RpgNoCodeActionType.UpgradeStat:
                    if (character.UpgradeStat(StatId))
                    {
                        EmitSuccess($"Upgraded stat: {StatId}");
                    }
                    else
                    {
                        EmitFailed($"Cannot upgrade stat: {StatId}");
                    }

                    break;
                case RpgNoCodeActionType.ApplyBuff:
                    if (character.ApplyBuffById(_buffId))
                    {
                        EmitSuccess($"Applied buff: {_buffId}");
                    }
                    else
                    {
                        EmitFailed($"Buff not found: {_buffId}");
                    }

                    break;
                case RpgNoCodeActionType.ApplyInlineBuff:
                    if (character.ApplyInlineBuff(_inlineBuffIndex))
                    {
                        EmitSuccess($"Applied inline buff: {_inlineBuffIndex}");
                    }
                    else
                    {
                        EmitFailed($"Inline buff not found: {_inlineBuffIndex}");
                    }

                    break;
                case RpgNoCodeActionType.ApplyStatus:
                    if (character.ApplyStatusById(_statusId))
                    {
                        EmitSuccess($"Applied status: {_statusId}");
                    }
                    else
                    {
                        EmitFailed($"Status not found: {_statusId}");
                    }

                    break;
                case RpgNoCodeActionType.RemoveBuff:
                    if (character.RemoveBuff(_buffId))
                    {
                        EmitSuccess($"Removed buff: {_buffId}");
                    }
                    else
                    {
                        EmitFailed($"Buff not active: {_buffId}");
                    }

                    break;
                case RpgNoCodeActionType.RemoveStatus:
                    if (character.RemoveStatus(_statusId))
                    {
                        EmitSuccess($"Removed status: {_statusId}");
                    }
                    else
                    {
                        EmitFailed($"Status not active: {_statusId}");
                    }

                    break;
                case RpgNoCodeActionType.UseAttackById:
                    string atkErr = null;
                    if (_attackController != null && _attackController.TryUseAttack(_attackId, out atkErr))
                    {
                        EmitSuccess($"Used attack: {_attackId}");
                    }
                    else
                    {
                        EmitFailed(string.IsNullOrWhiteSpace(atkErr) ? "Attack controller missing." : atkErr);
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
                    string presetErr = null;
                    if (_attackController != null && _attackController.TryUsePreset(_presetId, out presetErr))
                    {
                        EmitSuccess($"Used preset: {_presetId}");
                    }
                    else
                    {
                        EmitFailed(string.IsNullOrWhiteSpace(presetErr) ? "Preset failed." : presetErr);
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
                    character.ResetProfile();
                    EmitSuccess("Profile reset.");
                    break;
                case RpgNoCodeActionType.SaveProfile:
                    character.SaveProfile();
                    EmitSuccess("Profile saved.");
                    break;
                case RpgNoCodeActionType.LoadProfile:
                    character.LoadProfile();
                    EmitSuccess("Profile loaded.");
                    break;
                case RpgNoCodeActionType.ClearAllBuffs:
                    character.ClearAllBuffs();
                    EmitSuccess("Cleared all buffs.");
                    break;
                case RpgNoCodeActionType.ClearAllStatuses:
                    character.ClearAllStatuses();
                    EmitSuccess("Cleared all statuses.");
                    break;
                case RpgNoCodeActionType.LockInvulnerable:
                    character.LockInvulnerable();
                    EmitSuccess("Invulnerability locked.");
                    break;
                case RpgNoCodeActionType.UnlockInvulnerable:
                    character.UnlockInvulnerable();
                    EmitSuccess("Invulnerability unlocked.");
                    break;
                case RpgNoCodeActionType.SetInvulnerable:
                    character.SetInvulnerable(_boolValue);
                    EmitSuccess($"Invulnerable: {_boolValue}");
                    break;
            }
        }

        private string ResourceId =>
            !string.IsNullOrWhiteSpace(_resourceId) ? _resourceId.Trim() : _resource.Value;

        private string StatId => _stat.Value;

        private bool TryGetCharacter(out RpgCharacter character)
        {
            character = _character != null ? _character : GetComponentInParent<RpgCharacter>();
            if (character != null)
            {
                return true;
            }

            EmitFailed("RpgCharacter not found.");
            return false;
        }

        private bool RequiresCharacter()
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
            string resolved = string.IsNullOrWhiteSpace(message) ? "RPG action failed." : message;
            _onFailed?.Invoke(resolved);
            _onResultMessage?.Invoke(resolved);
        }
    }
}
