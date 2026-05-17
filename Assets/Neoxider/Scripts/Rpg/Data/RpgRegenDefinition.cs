using System;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Configurable regeneration rule attached to an <see cref="RpgResourceDefinition"/>.
    ///     <para>Covers the common combat patterns:</para>
    ///     <list type="bullet">
    ///         <item><b>HP regen</b>: <c>FlatPerSecond = 2</c></item>
    ///         <item><b>Stamina</b>: <c>FlatPerSecond = 20</c>, <c>pauseAfterSpendSeconds = 1</c></item>
    ///         <item><b>Shield</b>: <c>PercentMaxPerSecond = 5</c>, <c>pauseAfterDamageSeconds = 3</c></item>
    ///         <item><b>Mana</b> from <c>Intelligence</c>: <c>FromStat</c>, <c>scalingMultiplier = 0.2</c></item>
    ///         <item><b>Rage</b>: leave disabled — gained only via events.</item>
    ///     </list>
    /// </summary>
    [Serializable]
    public sealed class RpgRegenDefinition
    {
        [Tooltip("Master switch. Disable for resources that only change via gameplay events.")]
        public bool enabled;

        [Tooltip("How the regen value is interpreted.")]
        public RpgRegenMode mode = RpgRegenMode.FlatPerSecond;

        [Tooltip("Per-second amount (FlatPerSecond/FromStat), per-second percent (PercentMaxPerSecond), " +
                 "or per-tick amount/percent for the *PerTick modes.")]
        public float value = 1f;

        [Tooltip("Seconds between ticks for the *PerTick modes. Ignored for *PerSecond / FromStat.")]
        [Min(0.01f)] public float tickInterval = 1f;

        [Header("FromStat mode")]
        [Tooltip("When mode = FromStat, regen rate per second = stat value * scalingMultiplier.")]
        public RpgStatId scalingStat;

        [Tooltip("Multiplier applied to the scaling stat (e.g. 0.2 = +20% of stat per second).")]
        public float scalingMultiplier = 1f;

        [Header("Gates")]
        [Tooltip("If true, regen stops once the pool reaches Max.")]
        public bool onlyWhenNotFull = true;

        [Tooltip("If true, regen stops while HP is at zero (dead).")]
        public bool onlyWhenAlive = true;

        [Header("Pause Windows")]
        [Tooltip("If true, regen pauses for pauseAfterSpendSeconds after this resource is spent " +
                 "(useful for Stamina sprint mechanic).")]
        public bool pauseAfterSpend;

        [Min(0f)] public float pauseAfterSpendSeconds = 1f;

        [Tooltip("If true, regen pauses for pauseAfterDamageSeconds after the character takes any damage " +
                 "(common for Shield/Bloodborne-style HP regen).")]
        public bool pauseAfterDamage;

        [Min(0f)] public float pauseAfterDamageSeconds = 3f;
    }
}
