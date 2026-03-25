namespace Neo.Core.Level
{
    /// <summary>
    ///     Formula type for required XP per level (Formula mode).
    ///     RequiredXp(level) is cumulative XP to reach that level.
    /// </summary>
    public enum LevelFormulaType
    {
        /// <summary>RequiredXp(level) = (level - 1) * xpPerLevel. Linear growth.</summary>
        Linear = 0,

        /// <summary>RequiredXp(level) = base * level^2. Quadratic growth.</summary>
        Quadratic = 1,

        /// <summary>RequiredXp(level) = base * factor^level. Exponential growth.</summary>
        Exponential = 2,

        /// <summary>RequiredXp(level) = base * level^exponent. Power growth (configurable exponent).</summary>
        Power = 3,

        /// <summary>RequiredXp(level) = constant + (level - 1) * xpPerLevel. Linear with offset.</summary>
        LinearWithOffset = 4,

        /// <summary>RequiredXp(level) = base * (level^exponent). Same as Power, explicit name.</summary>
        PolynomialSingle = 5
    }
}
