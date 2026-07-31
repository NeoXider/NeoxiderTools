namespace Neo.Abilities
{
    /// <summary>
    ///     Well-known boolean state ids. States aggregate with any-true-wins across all active modifiers.
    ///     The registry is open: any string is a valid state id.
    /// </summary>
    public static class AbilityStates
    {
        /// <summary>Cannot move, attack, or cast.</summary>
        public const string Stunned = "stunned";

        /// <summary>Cannot move; can still attack and cast.</summary>
        public const string Rooted = "rooted";

        /// <summary>Cannot cast abilities.</summary>
        public const string Silenced = "silenced";

        /// <summary>Cannot attack.</summary>
        public const string Disarmed = "disarmed";

        /// <summary>Takes no damage.</summary>
        public const string Invulnerable = "invulnerable";

        /// <summary>Cannot be selected as a unit target.</summary>
        public const string Untargetable = "untargetable";

        /// <summary>Hidden from enemies (e.g. invisibility).</summary>
        public const string Hidden = "hidden";

        /// <summary>Airborne / lifted; movement systems may suspend ground logic.</summary>
        public const string Airborne = "airborne";

        /// <summary>Frozen solid: gameplay shorthand commonly paired with move_speed x0.</summary>
        public const string Frozen = "frozen";

        /// <summary>Immune to magical damage.</summary>
        public const string MagicImmune = "magic_immune";

        /// <summary>Cannot be displaced by motion ops (knockback / pull / forced teleport).</summary>
        public const string Unmovable = "unmovable";
    }
}
