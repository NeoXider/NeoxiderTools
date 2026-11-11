using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Defines HOW a character grows when its level changes (Dota-style auto-growth
    ///     vs Dark-Souls-style manual upgrade points, or hybrid).
    /// </summary>
    [NeoDoc("Rpg/RpgProgressionDefinition.md")]
    [CreateAssetMenu(menuName = "Neoxider/RPG/Progression Definition", fileName = "ProgressionDefinition")]
    public sealed class RpgProgressionDefinition : ScriptableObject
    {
        [Tooltip("Which level-up flow is active.")]
        public RpgLevelGrowthMode growthMode = RpgLevelGrowthMode.AllStatsEveryLevel;

        [Tooltip("Upgrade points granted on every level-up when growthMode = ManualUpgradePoints or Hybrid.")] [Min(0)]
        public int upgradePointsPerLevel = 1;

        [Tooltip("Auto-apply stat growth on level-up. Set to false to defer growth (turn-based combat, " +
                 "tutorials).")]
        public bool autoApplyGrowthOnLevelUp = true;

        [Header("Optional Catalogue")]
        [Tooltip("Upgrade rules available to spend upgrade points on (Dark-Souls-style). " +
                 "Empty for Dota-style auto-growth only.")]
        public RpgStatUpgradeRule[] upgradeRules;
    }
}
