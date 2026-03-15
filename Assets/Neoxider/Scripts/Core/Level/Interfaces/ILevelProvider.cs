namespace Neo.Core.Level
{
    /// <summary>
    ///     Generic contract for level and optional XP. Used by Progression, RPG, and NoCode.
    /// </summary>
    public interface ILevelProvider
    {
        /// <summary>Current level (at least 1).</summary>
        int Level { get; }

        /// <summary>Total accumulated XP (when UseXp is true).</summary>
        int TotalXp { get; }

        /// <summary>XP required to reach the next level (0 if at max or no curve).</summary>
        int XpToNextLevel { get; }

        /// <summary>Whether level is derived from XP curve.</summary>
        bool UseXp { get; }

        /// <summary>True if a maximum level cap is set.</summary>
        bool HasMaxLevel { get; }

        /// <summary>Maximum level (0 = no cap).</summary>
        int MaxLevel { get; }

        /// <summary>Add experience points; level is recomputed from curve if UseXp.</summary>
        void AddXp(int amount);

        /// <summary>Set level directly (clamped to 1 and MaxLevel if set).</summary>
        void SetLevel(int level);
    }
}
