using UnityEngine;
using UnityEngine.Events;

namespace Neo.Abilities
{
    /// <summary>
    ///     Universal Inspector-driven bridge for ability actions: cast (first/by id/at unit/at self),
    ///     grant/revoke, ability and unit levels, apply/remove modifiers, damage and heal — all invoked
    ///     from a UnityEvent via the parameterless <see cref="Execute" />. Targets resolve from the
    ///     serialized references, falling back to components on this GameObject and its parents.
    /// </summary>
    [NeoDoc("Abilities/AbilityNoCodeAction.md")]
    [CreateFromMenu("Neoxider/Abilities/Ability NoCode Action")]
    [AddComponentMenu("Neoxider/Abilities/Ability NoCode Action")]
    public sealed class AbilityNoCodeAction : MonoBehaviour
    {
        [Header("Action")] [SerializeField]
        private AbilityNoCodeActionType _actionType = AbilityNoCodeActionType.CastFirstAbility;

        [Header("Target")]
        [Tooltip("Caster for cast actions. Empty = searched on this GameObject and its parents.")]
        [SerializeField] private AbilityCasterBehaviour _caster;

        [Tooltip("Acting unit for grant/level/modifier actions. Empty = the caster's unit, else searched on this GameObject and its parents.")]
        [SerializeField] private AbilityUnitBehaviour _unit;

        [Tooltip("Explicit target for CastAtUnit / ApplyModifier / RemoveModifier / ApplyDamage / Heal. Empty = the acting unit (CastAtUnit requires it).")]
        [SerializeField] private AbilityUnitBehaviour _targetUnit;

        [Header("Parameters")]
        [Tooltip("Ability asset. When set it overrides Ability Id, and GrantAbility registers its blueprint first.")]
        [SerializeField] private AbilityDefinition _ability;

        [Tooltip("Ability id used when no Ability asset is assigned.")]
        [SerializeField] private string _abilityId = string.Empty;

        [Tooltip("Modifier asset. When set it overrides Modifier Id, and ApplyModifier registers its blueprint first.")]
        [SerializeField] private ModifierDefinition _modifier;

        [Tooltip("Modifier id used when no Modifier asset is assigned.")]
        [SerializeField] private string _modifierId = string.Empty;

        [SerializeField] [Min(1)] private int _level = 1;
        [SerializeField] [Min(0f)] private float _amount = 25f;

        [Header("Events")] [SerializeField] private UnityEvent _onSuccess = new UnityEvent();
        [SerializeField] private UnityEvent<string> _onFailed = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onResultMessage = new UnityEvent<string>();

        private string _capturedCastFailure;
        private UnityAction<string> _castFailureCapture;
        private AbilityCasterBehaviour _resolvedCaster;
        private bool _casterSearched;

        public UnityEvent OnSuccess => _onSuccess;
        public UnityEvent<string> OnFailed => _onFailed;
        public UnityEvent<string> OnResultMessage => _onResultMessage;

        private void OnEnable()
        {
            // WHY: caster resolution (found or not) is cached; re-enable is the re-search point.
            _casterSearched = false;
            _resolvedCaster = null;
        }

        /// <summary>
        ///     Executes the configured action on the resolved caster/unit.
        /// </summary>
        [Button]
        public void Execute()
        {
            switch (_actionType)
            {
                case AbilityNoCodeActionType.CastFirstAbility:
                    ExecuteCastFirstAbility();
                    break;
                case AbilityNoCodeActionType.CastById:
                    ExecuteCast(atSelf: false);
                    break;
                case AbilityNoCodeActionType.CastAtUnit:
                    ExecuteCastAtUnit();
                    break;
                case AbilityNoCodeActionType.CastAtSelf:
                    ExecuteCast(atSelf: true);
                    break;
                case AbilityNoCodeActionType.GrantAbility:
                    ExecuteGrantAbility();
                    break;
                case AbilityNoCodeActionType.RevokeAbility:
                    ExecuteRevokeAbility();
                    break;
                case AbilityNoCodeActionType.SetAbilityLevel:
                    ExecuteSetAbilityLevel();
                    break;
                case AbilityNoCodeActionType.SetUnitLevel:
                    ExecuteSetUnitLevel();
                    break;
                case AbilityNoCodeActionType.ApplyModifier:
                    ExecuteApplyModifier();
                    break;
                case AbilityNoCodeActionType.RemoveModifier:
                    ExecuteRemoveModifier();
                    break;
                case AbilityNoCodeActionType.ApplyDamage:
                    ExecuteApplyDamage();
                    break;
                case AbilityNoCodeActionType.Heal:
                    ExecuteHeal();
                    break;
            }
        }

        private string ResolvedAbilityId => _ability != null ? _ability.Id : _abilityId?.Trim();

        private string ResolvedModifierId => _modifier != null ? _modifier.Id : _modifierId?.Trim();

        private void ExecuteCastFirstAbility()
        {
            if (!TryGetCaster(out AbilityCasterBehaviour caster))
            {
                return;
            }

            // WHY: the caster owns the "first ability" rule; re-deriving it here would drift.
            string abilityId = caster.FirstAbilityId;
            if (string.IsNullOrEmpty(abilityId))
            {
                EmitFailed("Caster has no abilities to cast.");
                return;
            }

            CastThroughCaster(caster, abilityId, null);
        }

        private void ExecuteCast(bool atSelf)
        {
            if (!TryGetCaster(out AbilityCasterBehaviour caster) || !TryGetAbilityId(out string abilityId))
            {
                return;
            }

            CastThroughCaster(caster, abilityId, atSelf ? caster.UnitBehaviour : null);
        }

        private void ExecuteCastAtUnit()
        {
            if (_targetUnit == null)
            {
                EmitFailed("Target unit is not assigned.");
                return;
            }

            if (!TryGetCaster(out AbilityCasterBehaviour caster) || !TryGetAbilityId(out string abilityId))
            {
                return;
            }

            CastThroughCaster(caster, abilityId, _targetUnit);
        }

        private void CastThroughCaster(AbilityCasterBehaviour caster, string abilityId, AbilityUnitBehaviour target)
        {
            // WHY: the precise CastFailureReason surfaces only through the caster's OnCastFailed;
            // a scoped listener captures it so _onFailed carries the same reason without a second cast.
            _capturedCastFailure = null;
            _castFailureCapture ??= reason => _capturedCastFailure = reason;
            caster.OnCastFailed.AddListener(_castFailureCapture);
            bool success;
            try
            {
                success = target != null ? caster.TryCastAtUnit(abilityId, target) : caster.TryCast(abilityId);
            }
            finally
            {
                // WHY: a throwing user handler on the cast events must not leak the capture listener
                // (it would stack duplicates across Execute calls).
                caster.OnCastFailed.RemoveListener(_castFailureCapture);
            }

            if (success)
            {
                EmitSuccess($"Cast: {abilityId}");
            }
            else
            {
                EmitFailed($"Cast failed: {abilityId} ({_capturedCastFailure ?? "unknown"})");
            }
        }

        private void ExecuteGrantAbility()
        {
            if (!TryGetUnit(out AbilityUnitBehaviour unit) || !TryGetAbilityId(out string abilityId))
            {
                return;
            }

            AbilitySystem system = unit.Unit.System;
            if (!EnsureRegistered(system, abilityId, isAbility: true))
            {
                return;
            }

            system.GrantAbility(unit.UnitId, abilityId);
            EmitSuccess($"Granted ability: {abilityId}");
        }

        private void ExecuteRevokeAbility()
        {
            if (!TryGetUnit(out AbilityUnitBehaviour unit) || !TryGetAbilityId(out string abilityId))
            {
                return;
            }

            if (unit.Unit.System.RevokeAbility(unit.UnitId, abilityId))
            {
                EmitSuccess($"Revoked ability: {abilityId}");
            }
            else
            {
                EmitFailed($"Ability not granted: {abilityId}");
            }
        }

        private void ExecuteSetAbilityLevel()
        {
            if (!TryGetUnit(out AbilityUnitBehaviour unit) || !TryGetAbilityId(out string abilityId))
            {
                return;
            }

            if (unit.Unit.System.SetAbilityLevel(unit.UnitId, abilityId, _level))
            {
                EmitSuccess($"Set ability level: {abilityId} -> {_level}");
            }
            else
            {
                EmitFailed($"Ability not granted: {abilityId}");
            }
        }

        private void ExecuteSetUnitLevel()
        {
            if (!TryGetUnit(out AbilityUnitBehaviour unit))
            {
                return;
            }

            unit.SetLevel(_level);
            EmitSuccess($"Set unit level: {_level}");
        }

        private void ExecuteApplyModifier()
        {
            if (!TryGetAffectedUnit(out AbilityUnitBehaviour target) || !TryGetModifierId(out string modifierId))
            {
                return;
            }

            AbilitySystem system = target.Unit.System;
            if (!EnsureRegistered(system, modifierId, isAbility: false))
            {
                return;
            }

            if (system.ApplyModifier(modifierId, ResolveSourceUnitId(), target.UnitId).Succeeded)
            {
                EmitSuccess($"Applied modifier: {modifierId}");
            }
            else
            {
                EmitFailed($"Modifier not applied: {modifierId}");
            }
        }

        private void ExecuteRemoveModifier()
        {
            if (!TryGetAffectedUnit(out AbilityUnitBehaviour target) || !TryGetModifierId(out string modifierId))
            {
                return;
            }

            if (target.Unit.System.Modifiers.RemoveById(target.UnitId, modifierId) > 0)
            {
                EmitSuccess($"Removed modifier: {modifierId}");
            }
            else
            {
                EmitFailed($"Modifier not active: {modifierId}");
            }
        }

        private void ExecuteApplyDamage()
        {
            if (!TryGetAffectedUnit(out AbilityUnitBehaviour target))
            {
                return;
            }

            target.ApplyDamage(_amount);
            EmitSuccess($"Damage: {_amount}");
        }

        private void ExecuteHeal()
        {
            if (!TryGetAffectedUnit(out AbilityUnitBehaviour target))
            {
                return;
            }

            target.ApplyHeal(_amount);
            EmitSuccess($"Healed: {_amount}");
        }

        private bool TryGetCaster(out AbilityCasterBehaviour caster)
        {
            caster = AbilityCasterBehaviour.Resolve(this, _caster, ref _resolvedCaster, ref _casterSearched);
            if (caster != null)
            {
                return true;
            }

            EmitFailed("AbilityCasterBehaviour not found.");
            return false;
        }

        private bool TryGetAbilityId(out string abilityId)
        {
            abilityId = ResolvedAbilityId;
            if (!string.IsNullOrEmpty(abilityId))
            {
                return true;
            }

            EmitFailed("Ability id is empty.");
            return false;
        }

        private bool TryGetModifierId(out string modifierId)
        {
            modifierId = ResolvedModifierId;
            if (!string.IsNullOrEmpty(modifierId))
            {
                return true;
            }

            EmitFailed("Modifier id is empty.");
            return false;
        }

        /// <summary>
        ///     Registers the assigned definition asset (if any) with the system, then verifies the id is
        ///     known. Emits the shared "Unknown ... id" failure when it is not.
        /// </summary>
        private bool EnsureRegistered(AbilitySystem system, string id, bool isAbility)
        {
            bool known;
            if (isAbility)
            {
                if (_ability != null)
                {
                    system.RegisterAbility(_ability.Blueprint);
                }

                known = system.TryGetAbility(id, out _);
            }
            else
            {
                if (_modifier != null)
                {
                    system.RegisterModifier(_modifier.Blueprint);
                }

                known = system.TryGetModifier(id, out _);
            }

            if (!known)
            {
                EmitFailed($"Unknown {(isAbility ? "ability" : "modifier")} id: {id}");
            }

            return known;
        }

        private bool TryGetUnit(out AbilityUnitBehaviour unit)
        {
            unit = ResolveActingUnit();
            if (unit == null)
            {
                EmitFailed("AbilityUnitBehaviour not found.");
                return false;
            }

            if (unit.Unit == null)
            {
                EmitFailed("Unit is not registered (enable it in Play mode first).");
                return false;
            }

            return true;
        }

        private bool TryGetAffectedUnit(out AbilityUnitBehaviour target)
        {
            if (_targetUnit == null)
            {
                return TryGetUnit(out target);
            }

            target = _targetUnit;
            if (target.Unit == null)
            {
                EmitFailed("Target unit is not registered (enable it in Play mode first).");
                return false;
            }

            return true;
        }

        private AbilityUnitBehaviour ResolveActingUnit()
        {
            if (_unit != null)
            {
                return _unit;
            }

            return _caster != null ? _caster.UnitBehaviour : GetComponentInParent<AbilityUnitBehaviour>();
        }

        private UnitId ResolveSourceUnitId()
        {
            AbilityUnitBehaviour source = ResolveActingUnit();
            return source != null && source.Unit != null ? source.UnitId : UnitId.None;
        }

        private void EmitSuccess(string message)
        {
            _onSuccess?.Invoke();
            _onResultMessage?.Invoke(message);
        }

        private void EmitFailed(string message)
        {
            string resolved = string.IsNullOrWhiteSpace(message) ? "Ability action failed." : message;
            _onFailed?.Invoke(resolved);
            _onResultMessage?.Invoke(resolved);
        }
    }
}
