namespace Neo.Rpg
{
    /// <summary>
    ///     Common resource / stat presets used by <see cref="RpgStatId"/>.
    ///     <para><see cref="Custom"/> means the user provides an arbitrary string id
    ///     so any project-specific resource (DarkMana, Rage, MysticEnergy, …) is supported
    ///     without changing this enum.</para>
    /// </summary>
    public enum RpgStatPreset
    {
        Custom = 0,

        Hp,
        Mana,
        Stamina,
        Shield,
        Rage,
        Energy,
        Ammo,
        Poise,

        Strength,
        Dexterity,
        Intelligence,
        Vitality,
        Endurance,
        Wisdom,
        Luck,

        Defense,
        MoveSpeed,
        AttackDamage,
        AttackSpeed,
        CastSpeed,
        CritChance,
        CritDamage,

        FireResist,
        IceResist,
        LightningResist,
        PoisonResist,
        ArcaneResist,
        PhysicalResist
    }
}
