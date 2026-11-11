namespace Neo.Rpg
{
    /// <summary>
    ///     How a target is selected from available candidates.
    /// </summary>
    public enum RpgTargetSelectionMode
    {
        Nearest,
        Farthest,
        LowestCurrentHp,
        HighestCurrentHp,
        LowestHpPercent,
        HighestLevel,
        Random
    }
}
