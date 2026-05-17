namespace Neo.Rpg
{
    /// <summary>
    ///     Stat types that can be modified by buffs.
    ///     <para>Legacy values (<see cref="DamagePercent"/>, <see cref="DefensePercent"/>,
    ///     <see cref="SpecificDefensePercent"/>, <see cref="HpRegenPerSecond"/>,
    ///     <see cref="MovementSpeedPercent"/>, <see cref="Custom"/>) operate on hardcoded character fields
    ///     and ignore the <c>targetId</c> on <see cref="BuffStatModifier"/>.</para>
    ///     <para>Universal values (the <c>Add*</c>, <c>Regen*</c> kinds) read <c>targetId</c> and apply
    ///     to ANY stat or resource id — including project-specific ones like <c>"DarkMana"</c> /
    ///     <c>"Rage"</c>. Prefer the universal kinds for new content.</para>
    /// </summary>
    public enum BuffStatType
    {
        // ── Legacy (hardcoded targets) ──
        DamagePercent = 0,
        DefensePercent = 1,
        SpecificDefensePercent = 2,
        HpRegenPerSecond = 3,
        MovementSpeedPercent = 4,
        Custom = 5,

        // ── Universal stat modifiers (use BuffStatModifier.targetId) ──
        /// <summary>+value flat added to the stat with id = targetId (final value).</summary>
        AddStatFlat = 100,

        /// <summary>+value% multiplicative on top of the stat with id = targetId.</summary>
        AddStatPercent = 101,

        // ── Universal resource modifiers ──
        /// <summary>+value flat to the Max of the resource with id = targetId (Vitality → Max HP +15).</summary>
        AddResourceMaxFlat = 200,

        /// <summary>+value% to the Max of the resource with id = targetId.</summary>
        AddResourceMaxPercent = 201,

        /// <summary>+value flat to the Current of the resource (one-shot, e.g. on apply).</summary>
        AddResourceCurrentFlat = 202,

        /// <summary>+value flat per second to the resource regen rate.</summary>
        RegenFlat = 210,

        /// <summary>+value% multiplicative on top of the resource regen rate.</summary>
        RegenPercent = 211,

        // ── Universal combat modifiers (targetId = damage-type string or empty for all) ──
        /// <summary>+value% to incoming damage (negative = damage reduction).</summary>
        IncomingDamagePercent = 300,

        /// <summary>+value% to outgoing damage.</summary>
        OutgoingDamagePercent = 301,

        /// <summary>+value% resist for the damageType in BuffStatModifier.damageType.</summary>
        DamageTypeResistPercent = 302,

        /// <summary>+value% to move speed.</summary>
        MoveSpeedPercent = 310,

        /// <summary>+value% to attack speed.</summary>
        AttackSpeedPercent = 311
    }
}
