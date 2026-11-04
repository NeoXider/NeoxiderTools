using UnityEngine;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Plain-C# progression state of one character: level, experience, unspent upgrade points and the level at
    ///     which per-level stat growth was last applied. Encodes the <see cref="RpgProgressionDefinition" /> rules
    ///     (automatic vs manual growth, points per level) with overflow-safe arithmetic.
    /// </summary>
    /// <remarks>
    ///     Values are raw state; the owning facade publishes them through its reactive properties and events, so
    ///     the service can be driven and tested without a scene.
    /// </remarks>
    public sealed class RpgCharacterProgressionService
    {
        /// <summary>Active progression rules; null disables growth and upgrade-point grants.</summary>
        public RpgProgressionDefinition Definition { get; set; }

        /// <summary>Current level, always at least 1.</summary>
        public int Level { get; private set; } = 1;

        /// <summary>Accumulated experience, always finite and non-negative.</summary>
        public float Xp { get; private set; }

        /// <summary>Unspent upgrade points, never negative.</summary>
        public int UpgradePoints { get; private set; }

        /// <summary>Level used for stat growth evaluation. Lags <see cref="Level" /> while growth is deferred.</summary>
        public int AppliedGrowthLevel { get; private set; } = 1;

        /// <summary>True when level changes immediately re-evaluate per-level stat growth.</summary>
        public bool UsesAutomaticGrowth =>
            Definition != null && Definition.autoApplyGrowthOnLevelUp &&
            Definition.growthMode is RpgLevelGrowthMode.AllStatsEveryLevel or RpgLevelGrowthMode.Hybrid;

        /// <summary>True when gaining levels grants upgrade points.</summary>
        public bool GrantsUpgradePoints =>
            Definition != null &&
            Definition.growthMode is RpgLevelGrowthMode.ManualUpgradePoints or RpgLevelGrowthMode.Hybrid;

        /// <summary>Clamps a requested level to the valid range (1 or more).</summary>
        public static int ClampLevel(long level)
        {
            return level < 1L ? 1 : level > int.MaxValue ? int.MaxValue : (int)level;
        }

        /// <summary>Upgrade points granted when moving from <paramref name="oldLevel" /> to <paramref name="newLevel" />.</summary>
        public int GetUpgradePointsForLevelChange(int oldLevel, int newLevel)
        {
            if (!GrantsUpgradePoints || newLevel <= oldLevel)
            {
                return 0;
            }

            long gained = ((long)newLevel - oldLevel) * Mathf.Max(0, Definition.upgradePointsPerLevel);
            return gained > int.MaxValue ? int.MaxValue : (int)gained;
        }

        /// <summary>
        ///     Sets the level (clamped). Automatic growth moves the applied-growth anchor along with it.
        /// </summary>
        /// <returns>True when the stored level changed.</returns>
        public bool SetLevel(int level)
        {
            int clamped = ClampLevel(level);
            if (clamped == Level)
            {
                return false;
            }

            Level = clamped;
            if (UsesAutomaticGrowth)
            {
                AppliedGrowthLevel = clamped;
            }

            return true;
        }

        /// <summary>Adds experience. Non-positive and non-finite amounts are rejected.</summary>
        public bool AddXp(float amount)
        {
            if (!(amount > 0f) || float.IsInfinity(amount))
            {
                return false;
            }

            float next = Xp + amount;
            Xp = float.IsInfinity(next) ? float.MaxValue : next;
            return true;
        }

        /// <summary>Adds (or removes, when negative) upgrade points, saturating at 0 and <see cref="int.MaxValue" />.</summary>
        public void AddUpgradePoints(int amount)
        {
            long next = (long)UpgradePoints + amount;
            UpgradePoints = next < 0L ? 0 : next > int.MaxValue ? int.MaxValue : (int)next;
        }

        /// <summary>Spends <paramref name="cost" /> points when affordable.</summary>
        public bool TrySpendUpgradePoints(int cost)
        {
            if (cost < 0 || UpgradePoints < cost)
            {
                return false;
            }

            UpgradePoints -= cost;
            return true;
        }

        /// <summary>Marks per-level growth as applied at the current level (explicit/deferred growth).</summary>
        public void ApplyGrowthAtCurrentLevel()
        {
            AppliedGrowthLevel = Level;
        }

        /// <summary>
        ///     Resets the growth anchor after a rebuild or profile load: the current level under automatic growth,
        ///     otherwise level 1 until growth is applied explicitly.
        /// </summary>
        public void ResetGrowthAnchor()
        {
            AppliedGrowthLevel = UsesAutomaticGrowth ? Level : 1;
        }

        /// <summary>Restores raw progression values (sanitized) without touching the growth anchor.</summary>
        public void Restore(int level, float xp, int upgradePoints)
        {
            Level = ClampLevel(level);
            Xp = float.IsNaN(xp) || xp < 0f ? 0f : float.IsInfinity(xp) ? float.MaxValue : xp;
            UpgradePoints = Mathf.Max(0, upgradePoints);
        }
    }
}
