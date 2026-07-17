using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Pure-data definition of an ability: targeting, costs, cooldown/charges, delivery and
    ///     effect nodes. Fully authorable without code; the ScriptableObject wrapper is
    ///     <c>AbilityDefinition</c>. Serializable over the network by id — clients and server
    ///     register the same blueprints.
    /// </summary>
    [Serializable]
    public class AbilityBlueprint
    {
        [Tooltip("Unique ability id, e.g. 'fireball'.")]
        public string Id;

        [Tooltip("Display name for UI.")]
        public string DisplayName;

        [TextArea]
        [Tooltip("Description for UI and the Ability Designer.")]
        public string Description;

        [Header("Targeting")]
        [Tooltip("How the ability acquires its target.")]
        public TargetingMode Targeting = TargetingMode.NoTarget;

        [Tooltip("Valid unit targets relative to the caster (Unit targeting and area filters).")]
        public AbilityTeamFilter TeamFilter = AbilityTeamFilter.Enemies;

        [Tooltip("Maximum cast range in world units. 0 = unlimited.")]
        public float Range;

        [Header("Costs & cooldown")]
        [Tooltip("Resource costs paid on cast.")]
        public List<AbilityCost> Costs = new List<AbilityCost>();

        [Tooltip("Cooldown seconds after cast. Reduced by cooldown_reduction_percent.")]
        public float Cooldown;

        [Tooltip("Charge count. 0 or 1 = classic single cooldown; >1 = charge system.")]
        public int MaxCharges = 1;

        [Tooltip("Seconds to restore one charge when MaxCharges > 1. 0 = use Cooldown.")]
        public float ChargeRestoreTime;

        [Header("Delivery")]
        [Tooltip("Instant impact or host-driven projectile.")]
        public AbilityDeliveryType Delivery = AbilityDeliveryType.Instant;

        [Tooltip("Spawn archetype id for the projectile (host maps to prefab/pool).")]
        public string ProjectileArchetypeId;

        [Tooltip("Projectile speed hint for the host (world units/second).")]
        public float ProjectileSpeed = 20f;

        [Header("Effects")]
        [Tooltip("Effect nodes executed immediately on cast (at the caster, before delivery).")]
        public List<EffectNodeData> CastEffects = new List<EffectNodeData>();

        [Tooltip("Effect nodes executed on impact (instantly for Instant delivery, on hit for projectiles).")]
        public List<EffectNodeData> ImpactEffects = new List<EffectNodeData>();

        [Header("Leveled values")]
        [Tooltip("Named leveled values (Dota AbilitySpecial). Effect nodes reference them by AmountKey.")]
        public List<AbilitySpecialValue> Specials = new List<AbilitySpecialValue>();

        [Header("Presentation")]
        [Tooltip("Optional presentation cue id consumed by presentation listeners on cast.")]
        public string CastCue;

        [Tooltip("Optional presentation cue id consumed by presentation listeners on impact.")]
        public string ImpactCue;

        public bool UsesCharges => MaxCharges > 1;
    }

    /// <summary>
    ///     A named, reusable <see cref="LeveledValue" /> on an ability (Dota <c>%value%</c> reuse):
    ///     several effect nodes can point at the same leveled array via <see cref="EffectNodeData.AmountKey" />.
    /// </summary>
    [Serializable]
    public struct AbilitySpecialValue
    {
        [Tooltip("Name referenced by EffectNodeData.AmountKey (e.g. 'damage').")]
        public string Name;

        public LeveledValue Value;
    }
}
