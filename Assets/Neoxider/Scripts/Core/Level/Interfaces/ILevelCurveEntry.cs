namespace Neo.Core.Level
{
    /// <summary>
    ///     Minimal contract for a level curve entry (Custom list).
    /// </summary>
    public interface ILevelCurveEntry
    {
        int Level { get; }
        int RequiredXp { get; }
    }
}
