namespace Neo.Extensions
{
    /// <summary>
    ///     Defines various time formatting options for use with extension methods.
    /// </summary>
    public enum TimeFormat
    {
        /// <summary>
        ///     Milliseconds only.
        /// </summary>
        Milliseconds,

        /// <summary>
        ///     Seconds and milliseconds.
        /// </summary>
        SecondsMilliseconds,

        /// <summary>
        ///     Seconds only.
        /// </summary>
        Seconds,

        /// <summary>
        ///     Minutes only.
        /// </summary>
        Minutes,

        /// <summary>
        ///     Minutes and seconds.
        /// </summary>
        MinutesSeconds,

        /// <summary>
        ///     Minutes, seconds and milliseconds.
        /// </summary>
        MinutesSecondsMilliseconds,

        /// <summary>
        ///     Hours only.
        /// </summary>
        Hours,

        /// <summary>
        ///     Hours and minutes.
        /// </summary>
        HoursMinutes,

        /// <summary>
        ///     Hours, minutes and seconds.
        /// </summary>
        HoursMinutesSeconds,

        /// <summary>
        ///     Days only.
        /// </summary>
        Days,

        /// <summary>
        ///     Days and hours.
        /// </summary>
        DaysHours,

        /// <summary>
        ///     Days, hours and minutes.
        /// </summary>
        DaysHoursMinutes,

        /// <summary>
        ///     Days, hours, minutes and seconds.
        /// </summary>
        DaysHoursMinutesSeconds
    }

    /// <summary>
    ///     Represents an edge or corner of the screen.
    /// </summary>
    public enum ScreenEdge
    {
        /// <summary>
        ///     Left edge of the screen.
        /// </summary>
        Left,

        /// <summary>
        ///     Right edge of the screen.
        /// </summary>
        Right,

        /// <summary>
        ///     Top edge of the screen.
        /// </summary>
        Top,

        /// <summary>
        ///     Bottom edge of the screen.
        /// </summary>
        Bottom,

        /// <summary>
        ///     Top-left corner of the screen.
        /// </summary>
        TopLeft,

        /// <summary>
        ///     Top-right corner of the screen.
        /// </summary>
        TopRight,

        /// <summary>
        ///     Bottom-left corner of the screen.
        /// </summary>
        BottomLeft,

        /// <summary>
        ///     Bottom-right corner of the screen.
        /// </summary>
        BottomRight,

        /// <summary>
        ///     Center point of the screen.
        /// </summary>
        Center,

        /// <summary>
        ///     Front direction.
        /// </summary>
        Front,

        /// <summary>
        ///     Back direction.
        /// </summary>
        Back
    }
}