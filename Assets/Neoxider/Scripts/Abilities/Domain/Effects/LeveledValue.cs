using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Which unit a scaling term reads its property from.
    /// </summary>
    public enum ScaleAmountSource
    {
        Caster = 0,
        Target = 1
    }

    /// <summary>
    ///     Which level drives a <see cref="LeveledValue" />: ability level, a unit level, or none (flat).
    /// </summary>
    public enum LevelSource
    {
        None = 0,
        AbilityLevel = 1,
        CasterUnitLevel = 2,
        TargetUnitLevel = 3
    }

    /// <summary>How the scaling term of a <see cref="LeveledValue" /> combines with the leveled value.</summary>
    public enum ScaleAmountMode
    {
        /// <summary>value += ScalePerPoint × stat (the default).</summary>
        Add = 0,

        /// <summary>value *= 1 + ScalePerPoint × stat (0.01 per point of a percent stat = +1% per point).</summary>
        Multiply = 1
    }

    /// <summary>What the scaling term of a <see cref="LeveledValue" /> reads from the scaling unit.</summary>
    public enum ScaleAmountStat
    {
        /// <summary>A unit property; the scale id is a property id (the default).</summary>
        Property = 0,

        /// <summary>The current amount of a resource pool; the scale id is a resource id (e.g. mana).</summary>
        ResourceCurrent = 1,

        /// <summary>The maximum of a resource pool (e.g. 10% of max mana).</summary>
        ResourceMax = 2,

        /// <summary>Maximum minus current of a resource pool (e.g. missing health), never negative.</summary>
        ResourceMissing = 3
    }

    /// <summary>
    ///     Data-authored "special value" (Dota <c>AbilitySpecial</c>): resolves a single float from a base,
    ///     an optional per-level array, an optional linear-per-level term, an optional term per resource the cast
    ///     spent (<c>value += SpentCoefficient * spent</c>) and an optional scaling term from a property or resource
    ///     of the caster or target, added (<c>value += ScalePerPoint * stat</c>) or multiplied
    ///     (<c>value *= 1 + ScalePerPoint * stat</c>). All-default resolves to <see cref="Base" />, so a node that only
    ///     sets its flat amount is untouched. Pure data, resolved deterministically by <see cref="LeveledValueResolver" />.
    /// </summary>
    [Serializable]
    public struct LeveledValue
    {
        /// <summary>Value used when <see cref="Levels" /> is empty (equals today's flat amount).</summary>
        [Tooltip("Value used when Levels is empty (plus PerLevel * (level - 1)).")]
        public float Base;

        /// <summary>Per-level array; value = Levels[clamp(level - 1)]. Empty ⇒ use Base + linear term.</summary>
        [Tooltip("Per-level values, indexed by the LevelFrom level (clamped). Empty ⇒ Base + PerLevel term.")]
        public float[] Levels;

        /// <summary>Linear growth used only when <see cref="Levels" /> is empty: Base + PerLevel * (level - 1).</summary>
        [Tooltip("Linear growth used only when Levels is empty: Base + PerLevel * (level - 1).")]
        public float PerLevel;

        /// <summary>
        ///     Optional property id (e.g. spell_power) added as a scaling term, or a resource id when
        ///     <see cref="ScaleStat" /> reads a resource. "" = no scaling.
        /// </summary>
        [Tooltip("Property id (e.g. spell_power), or a resource id (e.g. mana) when Scale Stat reads a resource. " +
                 "'' = no scaling term.")]
        public string ScaleProperty;

        /// <summary>Scaling coefficient: value += ScalePerPoint * stat (Add) or value *= 1 + ScalePerPoint * stat (Multiply).</summary>
        [Tooltip("Coefficient of the scaling term: Add ⇒ value += k * stat; Multiply ⇒ value *= 1 + k * stat.")]
        public float ScalePerPoint;

        /// <summary>Whether the scaling property is read from the caster or the target.</summary>
        [Tooltip("Whether the scaling stat is read from the caster or the target.")]
        public ScaleAmountSource ScaleFrom;

        /// <summary>Which level indexes <see cref="Levels" /> / drives the linear term.</summary>
        [Tooltip("Which level indexes Levels and drives PerLevel. None ⇒ level 1.")]
        public LevelSource LevelFrom;

        /// <summary>What the scaling term reads: a property (default) or a resource pool's current / max / missing amount.</summary>
        [Tooltip("What the scaling term reads: a property (default) or a resource pool's current, max or missing amount.")]
        public ScaleAmountStat ScaleStat;

        /// <summary>Add (default): value += ScalePerPoint * stat. Multiply: value *= 1 + ScalePerPoint * stat, applied last.</summary>
        [Tooltip("Add (default): value += k * stat. Multiply: value *= 1 + k * stat, applied after every other term.")]
        public ScaleAmountMode ScaleMode;

        /// <summary>
        ///     Resource id whose amount the cast actually spent drives the spent term (e.g. mana for "spend all mana").
        ///     "" = off. The amounts travel with the cast: cast and impact effects, projectile hits, payloads and the
        ///     tick / reaction effects of modifiers that cast applied.
        /// </summary>
        [Tooltip("Resource id whose spent amount (what the cast actually paid) adds a term. '' = off.")]
        public string SpentResource;

        /// <summary>Spent term: value += SpentCoefficient * amount of <see cref="SpentResource" /> the cast spent.</summary>
        [Tooltip("Spent term: value += coefficient * amount of Spent Resource the cast paid (2 = twice the spent mana).")]
        public float SpentCoefficient;

        /// <summary>A flat value (matches the legacy inline amount).</summary>
        public static LeveledValue Flat(float amount)
        {
            return new LeveledValue { Base = amount };
        }
    }
}
