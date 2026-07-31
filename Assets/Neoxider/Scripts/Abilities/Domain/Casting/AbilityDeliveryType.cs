namespace Neo.Abilities
{
    /// <summary>
    ///     How the impact effects reach the target after a successful cast.
    /// </summary>
    public enum AbilityDeliveryType
    {
        /// <summary>Impact effects execute immediately on cast.</summary>
        Instant = 0,

        /// <summary>The host spawns a projectile; impact executes when it reports a hit.</summary>
        Projectile = 1
    }
}
