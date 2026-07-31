namespace Neo.Abilities
{
    /// <summary>
    ///     Built-in effect operation ids. The registry is open — games register custom ops once in code,
    ///     then designers compose them in data.
    /// </summary>
    public static class AbilityEffectOps
    {
        public const string Damage = "damage";
        public const string Heal = "heal";
        public const string ApplyModifier = "apply_modifier";
        public const string RemoveModifier = "remove_modifier";
        public const string Dispel = "dispel";
        public const string ResourceChange = "resource_change";
        public const string Spawn = "spawn";

        // WHY: motion family (push/pull/blink) all route through the world adapter's TryMoveUnit seam.
        public const string Knockback = "knockback";
        public const string Pull = "pull";
        public const string Teleport = "teleport";

        public const string Execute = "execute";
        public const string Chain = "chain";
    }
}
