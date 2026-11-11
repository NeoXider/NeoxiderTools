namespace Neo.Abilities
{
    /// <summary>
    ///     How an ability acquires its target when cast.
    /// </summary>
    public enum TargetingMode
    {
        /// <summary>No explicit target; effects resolve around/on the caster.</summary>
        NoTarget = 0,

        /// <summary>Always targets the caster.</summary>
        Self = 1,

        /// <summary>Requires a unit target matching the team filter.</summary>
        Unit = 2,

        /// <summary>Requires a world point.</summary>
        Point = 3,

        /// <summary>Requires a direction from the caster (skillshots).</summary>
        Direction = 4
    }
}
