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

        /// <summary>
        ///     Typed outcome of the most recent TryCast* call on this caster: success, the precise
        ///     <see cref="CastFailureReason" /> and the cast id. Default (failed, None) before the first call.
        /// </summary>
        public CastResult LastCastResult { get; private set; }

        /// <summary>Casts a no-target/self ability. Returns false and fires OnCastFailed with the reason name on failure.</summary>
        public bool TryCast(string abilityId)
        {
            return Execute(CastRequest.NoTarget(UnitBehaviour.UnitId, abilityId));
        }

        /// <summary>Casts at another unit's scene presence. A null target fails with <see cref="CastFailureReason.InvalidTarget" />.</summary>
        public bool TryCastAtUnit(string abilityId, AbilityUnitBehaviour target)
        {
            if (target == null)
            {
                LastCastResult = CastResult.Fail(CastFailureReason.InvalidTarget);
                _onCastFailed.Invoke(CastFailureReasonNames.Of(CastFailureReason.InvalidTarget));
                return false;
            }

            return Execute(CastRequest.AtUnit(UnitBehaviour.UnitId, abilityId, target.UnitId));
        }

        /// <summary>Casts a point-targeted ability at a world position.</summary>
        public bool TryCastAtPoint(string abilityId, Vector3 point)
        {
            return Execute(CastRequest.AtPoint(UnitBehaviour.UnitId, abilityId, point));
        }

        /// <summary>Casts a direction-targeted ability (skillshot) along <paramref name="direction" />.</summary>
        public bool TryCastTowards(string abilityId, Vector3 direction)
        {
            return Execute(CastRequest.Towards(UnitBehaviour.UnitId, abilityId, direction));
        }

        /// <summary>
        ///     Casts a fully specified request (custom seed, explicit caster/target ids) through this
        ///     component so its UnityEvents and <see cref="LastCastResult" /> stay in sync. The request's
        ///     Caster is replaced by this component's unit.
        /// </summary>
        public CastResult Cast(CastRequest request)
        {
            request.Caster = UnitBehaviour.UnitId;
            return ExecuteRequest(request);
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

        [Button(PlayModeOnly = true)]
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
            return ExecuteRequest(request).Success;
        }

        private CastResult ExecuteRequest(CastRequest request)
        {
            AbilityUnit unit = UnitBehaviour.Unit;
            if (unit == null)
            {
                CastResult unknown = CastResult.Fail(CastFailureReason.UnknownCaster);
                LastCastResult = unknown;
                _onCastFailed.Invoke(CastFailureReasonNames.Of(CastFailureReason.UnknownCaster));
                return unknown;
            }

            CastResult result = unit.System.Cast(request);
            LastCastResult = result;
            if (result.Success)
            {
                _onCastSuccess.Invoke(request.AbilityId);
            }
            else
            {
                _onCastFailed.Invoke(CastFailureReasonNames.Of(result.Failure));
            }

            return result;
        }
    }
}
