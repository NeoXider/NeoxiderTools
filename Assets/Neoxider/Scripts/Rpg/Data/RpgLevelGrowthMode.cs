namespace Neo.Rpg
{
    /// <summary>
    ///     How a character grows when its level increases.
    /// </summary>
    public enum RpgLevelGrowthMode
    {
        /// <summary>Level changes only update the LevelState reactive; stats do not auto-adjust.</summary>
        None = 0,

        /// <summary>Dota-like: every stat with <c>affectedByLevel = true</c> is auto-applied each level-up.</summary>
        AllStatsEveryLevel = 1,

        /// <summary>Dark-Souls-like: the player earns upgrade points and chooses which stat to spend them on.</summary>
        ManualUpgradePoints = 2,

        /// <summary>Both: every level applies the auto-growth AND grants upgrade points.</summary>
        Hybrid = 3
    }
}
