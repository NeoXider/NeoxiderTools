namespace Neo.Rpg
{
    /// <summary>
    ///     How a resource regenerates over time.
    /// </summary>
    public enum RpgRegenMode
    {
        /// <summary>No automatic regeneration.</summary>
        None = 0,

        /// <summary>Adds <c>value</c> units per second continuously.</summary>
        FlatPerSecond = 1,

        /// <summary>Adds <c>value%</c> of Max per second (0.05 = +5% of max each second).</summary>
        PercentMaxPerSecond = 2,

        /// <summary>Adds <c>value</c> units every <c>tickInterval</c> seconds.</summary>
        FlatPerTick = 3,

        /// <summary>Adds <c>value%</c> of Max every <c>tickInterval</c> seconds.</summary>
        PercentMaxPerTick = 4,

        /// <summary>Regen rate = <c>scalingStat</c> value * <c>scalingMultiplier</c> per second
        /// (e.g. Mana regen = Intelligence * 0.2).</summary>
        FromStat = 5
    }
}
