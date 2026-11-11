namespace Neo.Abilities
{
    /// <summary>
    ///     Action type for the abilities NoCode bridge.
    /// </summary>
    public enum AbilityNoCodeActionType
    {
        CastFirstAbility,
        CastById,
        CastAtUnit,
        CastAtSelf,
        GrantAbility,
        RevokeAbility,
        SetAbilityLevel,
        SetUnitLevel,
        ApplyModifier,
        RemoveModifier,
        ApplyDamage,
        Heal,

        /// <summary>Grants the ability when the unit lacks it, otherwise raises its level by one (up to Max Level).</summary>
        LevelUpAbility
    }
}
