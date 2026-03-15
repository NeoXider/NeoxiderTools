namespace Neo.Rpg
{
    /// <summary>
    /// Action type for RPG NoCode bridge.
    /// </summary>
    public enum RpgNoCodeActionType
    {
        TakeDamage,
        Heal,
        SetMaxHp,
        SetLevel,
        ApplyBuff,
        ApplyStatus,
        RemoveBuff,
        RemoveStatus,
        UseAttackById,
        UsePrimaryAttack,
        UsePresetById,
        UsePrimaryPreset,
        StartEvade,
        ResetProfile,
        SaveProfile,
        LoadProfile
    }
}
