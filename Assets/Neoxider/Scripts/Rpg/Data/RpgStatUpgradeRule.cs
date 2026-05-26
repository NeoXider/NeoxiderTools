using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     One row of an upgrade catalogue (used by <see cref="RpgUpgradeDefinition"/> and
    ///     <see cref="RpgCharacterTemplate"/>): cost, cap and derived resource impact when
    ///     the player spends an upgrade point on a given stat.
    /// </summary>
    [Serializable]
    public sealed class RpgStatUpgradeRule
    {
        [Tooltip("Stat the player invests points into.")]
        public RpgStatId statId;

        [Tooltip("How much the stat increases per upgrade point spent.")]
        public float increasePerPoint = 1f;

        [Tooltip("How many upgrade points one purchase consumes. 0 = invalid, 1 = standard.")] [Min(1)]
        public int costPerUpgrade = 1;

        [Tooltip("Maximum number of times this stat can be upgraded. -1 = unlimited.")]
        public int maxUpgradeCount = -1;

        [Tooltip("Side effects on resource pools when this stat is upgraded. Example: Vitality +1 → Hp Max +15.")]
        public RpgResourceModifier[] derivedResourceModifiers = Array.Empty<RpgResourceModifier>();
    }
}
