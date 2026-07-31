namespace Neo.Samples.Survivor
{
    /// <summary>What a <see cref="SurvivorUpgrade" /> does when the player picks it.</summary>
    public enum SurvivorUpgradeKind
    {
        /// <summary>Applies a permanent modifier to the player (stat boost via property contributions).</summary>
        PermanentModifier = 0,

        /// <summary>Grants a new auto-cast ability to the player.</summary>
        GrantAbility = 1,

        /// <summary>Raises the player's maximum health pool and heals to full.</summary>
        MaxHealth = 2
    }
}
