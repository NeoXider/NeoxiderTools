namespace Neo.Core.Level
{
    /// <summary>
    ///     How level is computed from total XP: formula or custom list.
    /// </summary>
    public enum LevelCurveType
    {
        /// <summary>Level = 1 + TotalXp / XpPerLevel</summary>
        Linear,

        /// <summary>Level from quadratic curve (e.g. RequiredXp = base * level^2)</summary>
        Quadratic,

        /// <summary>Level from exponential curve (e.g. RequiredXp = base * factor^level)</summary>
        Exponential,

        /// <summary>Level from manually defined list of LevelCurveEntry (Level, RequiredXp)</summary>
        Custom
    }
}
