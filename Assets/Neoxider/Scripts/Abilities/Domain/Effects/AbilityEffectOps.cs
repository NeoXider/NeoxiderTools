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

        // WHY: damage hooks; they adjust the in-flight DamageContext of before_deal_damage / before_take_damage.
        public const string ModifyDamage = "modify_damage";
        public const string Reflect = "reflect";

        // WHY: payload family; each runs the impact effects of the ability named by EffectNodeData.AbilityId.
        public const string Projectile = "projectile";
        public const string AreaApply = "area_apply";
        public const string Zone = "zone";
        public const string CastAbility = "cast_ability";
    }
}
