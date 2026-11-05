namespace Neo.Abilities
{
    /// <summary>How the scene hub advances its system every frame.</summary>
    public enum AbilityTimeMode
    {
        /// <summary>One step of Time.deltaTime per frame.</summary>
        Variable = 0,

        /// <summary>Whole fixed ticks (FixedStepTimeSource).</summary>
        FixedTicks = 1
    }
}
