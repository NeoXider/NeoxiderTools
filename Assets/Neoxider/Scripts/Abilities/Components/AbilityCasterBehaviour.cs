using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Abilities
{
    /// <summary>
    ///     Cast API of a unit for gameplay code, UI buttons and NPC brains: grants extra abilities and
    ///     exposes TryCast* methods plus success/failure UnityEvents. Input binding stays in game code —
    ///     call the TryCast methods from your controls.
    /// </summary>
    [NeoDoc("Abilities/AbilityCasterBehaviour.md")]
    [CreateFromMenu("Neoxider/Abilities/Ability Caster")]
    [AddComponentMenu("Neoxider/Abilities/Ability Caster")]
    [RequireComponent(typeof(AbilityUnitBehaviour))]
    public sealed class AbilityCasterBehaviour : MonoBehaviour
    {
        [Tooltip("Abilities granted on enable, in addition to the unit template's list.")]
        [SerializeField] private List<AbilityDefinition> _abilities = new List<AbilityDefinition>();

        [Header("Events")]
        [SerializeField] private UnityEvent<string> _onCastSuccess = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onCastFailed = new UnityEvent<string>();

        private AbilityUnitBehaviour _unit;

        public UnityEvent<string> OnCastSuccess => _onCastSuccess;
        public UnityEvent<string> OnCastFailed => _onCastFailed;

        public AbilityUnitBehaviour UnitBehaviour => _unit != null ? _unit : _unit = GetComponent<AbilityUnitBehaviour>();

        public IReadOnlyList<AbilityDefinition> Abilities => _abilities;

        /// <summary>
        ///     Canonical "first ability" of this caster (slot 0 of <see cref="Abilities" />), or null
        ///     when the list is empty or the slot is unassigned. <see cref="CastFirstAbility" /> and
        ///     NoCode bridges both cast this id.
        /// </summary>
        public string FirstAbilityId =>
            _abilities.Count > 0 && _abilities[0] != null ? _abilities[0].Id : null;

        /// <summary>
        ///     Shared caster resolution for companion components (auto-caster, cooldown source, NoCode
        ///     bridges): returns <paramref name="explicitCaster" /> when assigned, otherwise one cached
        ///     GetComponentInParent search from <paramref name="host" />. Found and not-found results are
        ///     both cached; reset <paramref name="searched" /> (e.g. in OnEnable) to search again.
        /// </summary>
        public static AbilityCasterBehaviour Resolve(Component host, AbilityCasterBehaviour explicitCaster,
            ref AbilityCasterBehaviour cached, ref bool searched)
        {
            if (explicitCaster != null)
            {
                return explicitCaster;
            }

            if (!searched)
            {
                searched = true;
                cached = host.GetComponentInParent<AbilityCasterBehaviour>();
            }

            return cached;
        }

        private void OnEnable()
        {
            GrantConfiguredAbilities();
        }

        // WHY: at scene load the hub/unit may not be ready when this component's OnEnable runs, so the
        // OnEnable grant can be skipped; Start runs after every OnEnable (unit registered by then), so a
        // second idempotent grant guarantees scene-authored casters always end up with their abilities.
        private void Start()
        {
            GrantConfiguredAbilities();
        }

        private void GrantConfiguredAbilities()
        {
            AbilityUnitBehaviour unit = UnitBehaviour;
            if (unit.Unit == null)
            {
                // WHY: OnEnable order across sibling components is undefined; register the unit
                // first so grants are never silently skipped (fresh enable and pooled re-activation).
                unit.EnsureRegistered();
            }

            if (unit.Unit == null)
            {
                return;
            }

            AbilitySystem system = unit.Unit.System;
            for (int i = 0; i < _abilities.Count; i++)
            {
                AbilityDefinition ability = _abilities[i];
                if (ability != null && !string.IsNullOrEmpty(ability.Id))
                {
                    system.RegisterAbility(ability.Blueprint);
                    system.GrantAbility(unit.UnitId, ability.Id);
                }
            }
        }

        public bool TryCast(string abilityId)
        {
            return Execute(CastRequest.NoTarget(UnitBehaviour.UnitId, abilityId));
        }

        public bool TryCastAtUnit(string abilityId, AbilityUnitBehaviour target)
        {
            if (target == null)
            {
                _onCastFailed.Invoke(CastFailureReason.InvalidTarget.ToString());
                return false;
            }

            return Execute(CastRequest.AtUnit(UnitBehaviour.UnitId, abilityId, target.UnitId));
        }

        public bool TryCastAtPoint(string abilityId, Vector3 point)
        {
            return Execute(CastRequest.AtPoint(UnitBehaviour.UnitId, abilityId, point));
        }

        public bool TryCastTowards(string abilityId, Vector3 direction)
        {
            return Execute(CastRequest.Towards(UnitBehaviour.UnitId, abilityId, direction));
        }

        /// <summary>Cooldown state of one granted ability for UI (0 = ready).</summary>
        public float GetCooldownNormalized(string abilityId)
        {
            AbilityUnit unit = UnitBehaviour.Unit;
            if (unit != null && unit.System.TryGetSlot(unit.Id, abilityId, out AbilitySlot slot))
            {
                return slot.NormalizedCooldown;
            }

            return 0f;
        }

        [Button]
        public void CastFirstAbility()
        {
            string abilityId = FirstAbilityId;
            if (!string.IsNullOrEmpty(abilityId))
            {
                TryCast(abilityId);
            }
        }

        private bool Execute(CastRequest request)
        {
            AbilityUnit unit = UnitBehaviour.Unit;
            if (unit == null)
            {
                _onCastFailed.Invoke(CastFailureReason.UnknownCaster.ToString());
                return false;
            }

            CastResult result = unit.System.Cast(request);
            if (result.Success)
            {
                _onCastSuccess.Invoke(request.AbilityId);
            }
            else
            {
                _onCastFailed.Invoke(result.Failure.ToString());
            }

            return result.Success;
        }
    }
}
