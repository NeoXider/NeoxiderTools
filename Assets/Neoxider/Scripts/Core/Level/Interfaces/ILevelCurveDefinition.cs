namespace Neo.Core.Level
{
    /// <summary>
    ///     Contract for level curve definition (formula, curve, custom).
    ///     Lets LevelModel avoid UnityEngine and resolve level/XP through this interface.
    /// </summary>
    public interface ILevelCurveDefinition
    {
        /// <summary>
        ///     Computes a level of at least 1, honoring a positive maxLevel cap.
        ///     Results must be nondecreasing as totalXp grows so LevelModel can invert the curve.
        /// </summary>
        /// <param name="totalXp">Total experience</param>
        /// <param name="maxLevel">Max level (0 = no cap)</param>
        int EvaluateLevel(int totalXp, int maxLevel = 0);

        /// <summary>XP to next level (0 if at max level).</summary>
        int GetXpToNextLevel(int totalXp, int maxLevel = 0);
    }
}
