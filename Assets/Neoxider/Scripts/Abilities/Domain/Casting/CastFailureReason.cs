namespace Neo.Abilities
{
    /// <summary>
    ///     Why a cast request was rejected. <see cref="None" /> means the cast succeeded.
    /// </summary>
    public enum CastFailureReason
    {
        None = 0,
        UnknownCaster = 1,
        UnknownAbility = 2,
        NotGranted = 3,
        CasterDead = 4,
        Stunned = 5,
        Silenced = 6,
        OnCooldown = 7,
        NoCharges = 8,
        NotEnoughResources = 9,
        InvalidTarget = 10,
        TargetDead = 11,
        TargetUntargetable = 12,
        WrongTeam = 13,
        OutOfRange = 14
    }
}
