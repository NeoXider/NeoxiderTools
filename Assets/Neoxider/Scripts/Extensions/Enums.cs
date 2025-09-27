namespace Neo.Extensions
{
    /// <summary>
    ///     Defines various time formatting options for use with extension methods.
    /// </summary>
    public enum TimeFormat
    {
        Milliseconds,
        SecondsMilliseconds,
        Seconds,
        Minutes,
        MinutesSeconds,
        Hours,
        HoursMinutes,
        HoursMinutesSeconds,
        Days,
        DaysHours,
        DaysHoursMinutes,
        DaysHoursMinutesSeconds
    }

    /// <summary>
    ///     Represents an edge or corner of the screen.
    /// </summary>
    public enum ScreenEdge
    {
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center,
        Front,
        Back
    }
}