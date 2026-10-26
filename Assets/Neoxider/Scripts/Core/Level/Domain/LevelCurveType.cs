namespace Neo.Core.Level
{
    /// <summary>
    ///     How level is computed from total XP: formula or custom list.
    /// </summary>
    public enum LevelCurveType
    {
        /// <summary>Level = 1 + TotalXp / XpPerLevel</summary>
        Linear,

        /// <summary>Level from quadratic curve (RequiredXp = base * (level - 1)^2).</summary>
        Quadratic,

        /// <summary>Level from exponential curve (RequiredXp = base * factor^(level - 1)).</summary>
        Exponential,

        /// <summary>Level from manually defined list of LevelCurveEntry (Level, RequiredXp)</summary>
        Custom
    }
}
