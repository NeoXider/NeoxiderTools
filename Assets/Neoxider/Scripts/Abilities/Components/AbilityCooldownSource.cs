using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Read-only binding source for one ability's cooldown: exposes cheap, allocation-free
    ///     properties (<see cref="CooldownNormalized" />, <see cref="ReadyNormalized" />,
    ///     <see cref="SecondsRemaining" />, <see cref="IsReady" />) computed from the caster on every
    ///     read, so NoCode bindings (SetProgress, NoCodeBindText) can poll them for cooldown UI.
    /// </summary>
    [NeoDoc("Abilities/AbilityCooldownSource.md")]
    [CreateFromMenu("Neoxider/Abilities/Ability Cooldown Source")]
    [AddComponentMenu("Neoxider/Abilities/Ability Cooldown Source")]
    public sealed class AbilityCooldownSource : MonoBehaviour
    {
        [Tooltip("Caster to read from. Empty = the AbilityCasterBehaviour on this GameObject or its parents.")]
        [SerializeField] private AbilityCasterBehaviour _caster;

        [Tooltip("Ability id whose cooldown is exposed.")]
        [SerializeField] private string _abilityId = string.Empty;

        private AbilityCasterBehaviour _resolvedCaster;
        private bool _casterSearched;

        /// <summary>Ability id whose cooldown is exposed. Assignable at runtime (e.g. per hotbar slot).</summary>
        public string AbilityId
        {
            get => _abilityId;
            set => _abilityId = value;
        }

        /// <summary>
        ///     Remaining cooldown, 0..1: 1 = just cast, 0 = ready. Delegates to
        ///     <see cref="AbilityCasterBehaviour.GetCooldownNormalized" />, so unknown/ungranted ids
        ///     read 0 ("reads as ready").
        /// </summary>
        public float CooldownNormalized
        {
            get
            {
                AbilityCasterBehaviour caster = ResolveCaster();
                return caster != null ? Mathf.Clamp01(caster.GetCooldownNormalized(_abilityId)) : 0f;
            }
        }

        /// <summary>Readiness, 0..1: 1 = ready, 0 = just cast. Fill amount for "charging up" bars.</summary>
        public float ReadyNormalized => 1f - CooldownNormalized;

        /// <summary>Seconds until ready. 0 when ready or when the id is unknown/ungranted.</summary>
        public float SecondsRemaining =>
            TryGetSlot(out AbilitySlot slot) ? Mathf.Max(0f, slot.CooldownRemaining) : 0f;

        /// <summary>True when the ability is granted and can be cast right now.</summary>
        public bool IsReady => TryGetSlot(out AbilitySlot slot) && slot.IsReady;

        private void OnEnable()
        {
            // WHY: caster resolution (found or not) is cached; re-enable is the re-search point.
            _casterSearched = false;
            _resolvedCaster = null;
        }

        private AbilityCasterBehaviour ResolveCaster()
        {
            return AbilityCasterBehaviour.Resolve(this, _caster, ref _resolvedCaster, ref _casterSearched);
        }

        // WHY: slot access remains only for SecondsRemaining/IsReady — the caster API has no
        // equivalents for those.
        private bool TryGetSlot(out AbilitySlot slot)
        {
            slot = null;
            AbilityCasterBehaviour caster = ResolveCaster();
            AbilityUnit unit = caster != null ? caster.UnitBehaviour.Unit : null;
            return unit != null && unit.System.TryGetSlot(unit.Id, _abilityId, out slot);
        }
    }
}
