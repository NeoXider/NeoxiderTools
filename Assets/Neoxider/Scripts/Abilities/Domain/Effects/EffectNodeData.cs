using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     One data-authored effect step: which operation runs, on whom, with which parameters.
    ///     The named fields cover the built-in ops; custom ops read <see cref="CustomParam" /> /
    ///     the numeric fields as they see fit. Designed for clean inspector editing — no dictionaries.
    /// </summary>
    [Serializable]
    public class EffectNodeData
    {
        [Tooltip("Operation id (see AbilityEffectOps for built-ins: damage, heal, apply_modifier...).")]
        public string OpId = AbilityEffectOps.Damage;

        [Tooltip("Whom the operation applies to, resolved in the current context.")]
        public EffectTargetSelector Target = EffectTargetSelector.Target;

        [Tooltip("Team filter for area selectors.")]
        public AbilityTeamFilter TeamFilter = AbilityTeamFilter.Enemies;

        [Tooltip("Radius for area selectors (world units).")]
        public float Radius;

        [Tooltip("Per-ability-level radii for area selectors. Empty ⇒ use Radius.")]
        public float[] RadiusByLevel;

        [Tooltip("Cap on affected units for area selectors and the chain op (0 = unlimited; chain treats 0 as 4). " +
                 "Area selectors keep the nearest N; chain hops up to N distinct targets.")]
        public int MaxTargets;

        [Tooltip("Primary amount: damage, heal, resource delta... Scaled by amount formulas where supported.")]
        public float Amount;

        [Header("Leveled amount (optional, all-default == flat Amount)")]
        [Tooltip("Per-level amounts keyed by AmountLevelSource. Empty ⇒ use Amount. E.g. [5,8,12,17].")]
        public float[] AmountByLevel;

        [Tooltip("Named special value on the ability blueprint (Specials). '' ⇒ use the inline fields.")]
        public string AmountKey;

        [Tooltip("Optional property id (e.g. spell_power) added as a scaling term. '' ⇒ no scaling.")]
        public string AmountScaleProperty;

        [Tooltip("Additive coefficient: amount += AmountScalePerPoint * property value.")]
        public float AmountScalePerPoint;

        [Tooltip("Whether the scaling property is read from the caster or the target.")]
        public ScaleAmountSource AmountScaleSource;

        [Tooltip("Which level indexes AmountByLevel. None ⇒ level 1 ⇒ Amount unchanged.")]
        public LevelSource AmountLevelSource;

        [Tooltip("Damage type for the damage op (physical/magical/pure or custom).")]
        public string DamageType = AbilityDamageTypes.Magical;

        [Tooltip("Modifier id for apply_modifier / remove_modifier (resolved through the system's modifier catalog).")]
        public string ModifierId;

        [Tooltip("Resource pool id for resource_change (e.g. mana).")]
        public string ResourceId;

        [Tooltip("Spawn archetype id for the spawn op (resolved by the host: prefab/pool binding).")]
        public string ArchetypeId;

        [Tooltip("Free-form parameter for custom operations.")]
        public string CustomParam;

        [Tooltip("Chance in [0..1] that this node executes. 1 = always. Rolled on the cast's deterministic RNG.")]
        [Range(0f, 1f)]
        public float Chance = 1f;

        /// <summary>
        ///     Maps the inline leveled-amount fields to a <see cref="LeveledValue" />. When every leveled
        ///     field is default this is <c>LeveledValue.Flat(Amount)</c>, so resolution returns
        ///     <see cref="Amount" /> unchanged (backward compatible).
        /// </summary>
        public LeveledValue ToLeveledValue()
        {
            return new LeveledValue
            {
                Base = Amount,
                Levels = AmountByLevel,
                PerLevel = 0f,
                ScaleProperty = AmountScaleProperty,
                ScalePerPoint = AmountScalePerPoint,
                ScaleFrom = AmountScaleSource,
                LevelFrom = AmountLevelSource
            };
        }
    }
}
