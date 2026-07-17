using System;

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

    /// <summary>
    ///     Data-authored "special value" (Dota <c>AbilitySpecial</c>): resolves a single float from a base,
    ///     an optional per-level array, an optional linear-per-level term, and an optional additive
    ///     property-scaling term (<c>value += ScalePerPoint * property</c>). All-default resolves to
    ///     <see cref="Base" />, so a node that only sets its flat amount is untouched.
    ///     Pure data, resolved deterministically by <see cref="LeveledValueResolver" />.
    /// </summary>
    [Serializable]
    public struct LeveledValue
    {
        /// <summary>Value used when <see cref="Levels" /> is empty (equals today's flat amount).</summary>
        public float Base;

        /// <summary>Per-level array; value = Levels[clamp(level - 1)]. Empty ⇒ use Base + linear term.</summary>
        public float[] Levels;

        /// <summary>Linear growth used only when <see cref="Levels" /> is empty: Base + PerLevel * (level - 1).</summary>
        public float PerLevel;

        /// <summary>Optional property id (e.g. spell_power) added as a scaling term. "" = no scaling.</summary>
        public string ScaleProperty;

        /// <summary>Additive coefficient: value += ScalePerPoint * property value.</summary>
        public float ScalePerPoint;

        /// <summary>Whether the scaling property is read from the caster or the target.</summary>
        public ScaleAmountSource ScaleFrom;

        /// <summary>Which level indexes <see cref="Levels" /> / drives the linear term.</summary>
        public LevelSource LevelFrom;

        /// <summary>A flat value (matches the legacy inline amount).</summary>
        public static LeveledValue Flat(float amount)
        {
            return new LeveledValue { Base = amount };
        }
    }
}
