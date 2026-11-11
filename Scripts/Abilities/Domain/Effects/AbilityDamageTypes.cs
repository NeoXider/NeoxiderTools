namespace Neo.Abilities
{
    /// <summary>
    ///     Well-known damage type ids. Open registry: any string is valid; these names are what the
    ///     built-in damage pipeline understands specially (armor vs magic resist vs nothing).
    /// </summary>
    public static class AbilityDamageTypes
    {
        /// <summary>Reduced by armor (Dota-style diminishing formula).</summary>
        public const string Physical = "physical";

        /// <summary>Reduced by magic_resist_percent; blocked entirely by the magic_immune state.</summary>
        public const string Magical = "magical";

        /// <summary>
        ///     True damage: ignores armor, resistances and shield absorption. Only the invulnerable
        ///     state stops it. Use sparingly for execute/percent-health style effects.
        /// </summary>
        public const string Pure = "pure";
    }
}
