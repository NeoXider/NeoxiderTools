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

        [Tooltip("Maximum cast range in world units, plus the caster's cast_range_bonus. 0 = unlimited. " +
                 "Direction casts reach exactly this far; with 0 they reach the aim vector's length, at least 10.")]
        public float Range;

        [Tooltip("Per-ability-level ranges (index = level - 1, clamped). Empty ⇒ use Range.")]
        public float[] RangeByLevel;

        [Header("Costs & cooldown")]
        [Tooltip("Resource costs paid on cast.")]
        public List<AbilityCost> Costs = new List<AbilityCost>();

        [Tooltip("Cooldown seconds after cast. Reduced by cooldown_reduction_percent.")]
        public float Cooldown;

        [Tooltip("Per-ability-level cooldowns in seconds, e.g. [10, 8, 6]. Empty ⇒ use Cooldown. " +
                 "A cooldown already running keeps its remaining time when the level changes.")]
        public float[] CooldownByLevel;

        [Tooltip("Charge count. 0 or 1 = classic single cooldown; >1 = charge system.")]
        public int MaxCharges = 1;

        [Tooltip("Per-ability-level charge counts. Empty ⇒ use MaxCharges. A raised cap grants the new charges at " +
                 "once; a lowered cap clamps the available charges.")]
        public int[] MaxChargesByLevel;

        [Tooltip("Seconds to restore one charge when MaxCharges > 1. 0 = use Cooldown.")]
        public float ChargeRestoreTime;

        [Tooltip("Per-ability-level charge restore seconds. Empty ⇒ use ChargeRestoreTime; a non-positive entry uses " +
                 "the level's cooldown.")]
        public float[] ChargeRestoreTimeByLevel;

        [Header("Delivery")]
        [Tooltip("Instant impact or host-driven projectile.")]
        public AbilityDeliveryType Delivery = AbilityDeliveryType.Instant;

        [Tooltip("Spawn archetype id for the projectile (host maps to prefab/pool).")]
        public string ProjectileArchetypeId;

        [Tooltip("Projectile speed hint for the host (world units/second).")]
        public float ProjectileSpeed = 20f;

        [Tooltip("Per-ability-level projectile speeds. Empty ⇒ use ProjectileSpeed.")]
        public float[] ProjectileSpeedByLevel;

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

        /// <summary>
        ///     True when the flat <see cref="MaxCharges" /> uses the charge system. Level-aware code uses
        ///     <see cref="UsesChargesAt" /> or <see cref="AbilitySlot.UsesCharges" />.
        /// </summary>
        public bool UsesCharges => MaxCharges > 1;

        /// <summary>Authored cast range at an ability level (before cast_range_bonus): <see cref="RangeByLevel" />, else <see cref="Range" />.</summary>
        public float ResolveRange(int level)
        {
            return LeveledValueResolver.SampleByLevel(RangeByLevel, level, Range);
        }

        /// <summary>Authored cooldown seconds at an ability level (before the cooldown rule): <see cref="CooldownByLevel" />, else <see cref="Cooldown" />.</summary>
        public float ResolveCooldown(int level)
        {
            return LeveledValueResolver.SampleByLevel(CooldownByLevel, level, Cooldown);
        }

        /// <summary>Charge cap at an ability level: <see cref="MaxChargesByLevel" />, else <see cref="MaxCharges" />. 0 or 1 = no charges.</summary>
        public int ResolveMaxCharges(int level)
        {
            return LeveledValueResolver.SampleByLevel(MaxChargesByLevel, level, MaxCharges);
        }

        /// <summary>True when the ability uses the charge system at <paramref name="level" />.</summary>
        public bool UsesChargesAt(int level)
        {
            return ResolveMaxCharges(level) > 1;
        }

        /// <summary>
        ///     Seconds to restore one charge at an ability level (before the cooldown rule):
        ///     <see cref="ChargeRestoreTimeByLevel" /> / <see cref="ChargeRestoreTime" /> when positive, else the level's
        ///     cooldown (<see cref="ResolveCooldown" />).
        /// </summary>
        public float ResolveChargeRestoreTime(int level)
        {
            float restore = LeveledValueResolver.SampleByLevel(ChargeRestoreTimeByLevel, level, ChargeRestoreTime);
            return restore > 0f ? restore : ResolveCooldown(level);
        }

        /// <summary>Projectile speed hint at an ability level: <see cref="ProjectileSpeedByLevel" />, else <see cref="ProjectileSpeed" />.</summary>
        public float ResolveProjectileSpeed(int level)
        {
            return LeveledValueResolver.SampleByLevel(ProjectileSpeedByLevel, level, ProjectileSpeed);
        }
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
