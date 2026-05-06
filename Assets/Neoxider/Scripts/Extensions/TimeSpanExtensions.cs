using System;
using System.Text;

namespace Neo.Extensions
{
    /// <summary>
    ///     Provides formatting helpers for <see cref="TimeSpan" />.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        ///     Converts a <see cref="TimeSpan" /> to a compact human-readable string.
        /// </summary>
        /// <param name="value">Source time span.</param>
        /// <param name="includeSeconds">Whether to include seconds in output.</param>
        /// <param name="maxParts">Maximum number of output units.</param>
        /// <returns>Compact string such as "2d 3h 15m".</returns>
        public static string ToCompactString(this TimeSpan value, bool includeSeconds = false, int maxParts = 3)
        {
            if (maxParts < 1)
            {
                maxParts = 1;
            }

            TimeSpan abs = value.Duration();
            if (abs == TimeSpan.Zero)
            {
                return "0s";
            }

            int added = 0;
            StringBuilder builder = new(24);
            AppendPart(abs.Days, "d");
            AppendPart(abs.Hours, "h");
            AppendPart(abs.Minutes, "m");
            if (includeSeconds)
            {
                AppendPart(abs.Seconds, "s");
            }

            string result = builder.Length > 0 ? builder.ToString() : "0s";
            return value.Ticks < 0 ? "-" + result : result;

            void AppendPart(int number, string suffix)
            {
                if (added >= maxParts)
                {
                    return;
                }

                if (number <= 0 && added == 0)
                {
                    return;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                if (added > 0)
                {
                    builder.Append(number.ToString("D2"));
                }
                else
                {
                    builder.Append(number);
                }

                builder.Append(suffix);
                added++;
            }
        }

        /// <summary>
        ///     Converts a <see cref="TimeSpan" /> to a compact string based on a specific <see cref="TimeFormat" />.
        /// </summary>
        /// <param name="value">Source time span.</param>
        /// <param name="format">The time format specifying which units to include.</param>
        /// <returns>Compact string such as "0h 05m".</returns>
        public static string ToCompactString(this TimeSpan value, TimeFormat format)
        {
            TimeSpan abs = value.Duration();
            StringBuilder builder = new(24);
            bool added = false;

            void Append(int number, string suffix, bool force2Digits)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                if (added && force2Digits)
                {
                    builder.Append(number.ToString("D2"));
                }
                else
                {
                    builder.Append(number);
                }

                builder.Append(suffix);
                added = true;
            }

            switch (format)
            {
                case TimeFormat.Milliseconds:
                    Append((int)abs.TotalMilliseconds, "ms", false);
                    break;
                case TimeFormat.SecondsMilliseconds:
                    Append((int)abs.TotalSeconds, "s", false);
                    Append(abs.Milliseconds, "ms", true);
                    break;
                case TimeFormat.Seconds:
                    Append((int)abs.TotalSeconds, "s", false);
                    break;
                case TimeFormat.Minutes:
                    Append((int)abs.TotalMinutes, "m", false);
                    break;
                case TimeFormat.MinutesSeconds:
                    Append((int)abs.TotalMinutes, "m", false);
                    Append(abs.Seconds, "s", true);
                    break;
                case TimeFormat.MinutesSecondsMilliseconds:
                    Append((int)abs.TotalMinutes, "m", false);
                    Append(abs.Seconds, "s", true);
                    Append(abs.Milliseconds, "ms", true);
                    break;
                case TimeFormat.Hours:
                    Append((int)abs.TotalHours, "h", false);
                    break;
                case TimeFormat.HoursMinutes:
                    Append((int)abs.TotalHours, "h", false);
                    Append(abs.Minutes, "m", true);
                    break;
                case TimeFormat.HoursMinutesSeconds:
                    Append((int)abs.TotalHours, "h", false);
                    Append(abs.Minutes, "m", true);
                    Append(abs.Seconds, "s", true);
                    break;
                case TimeFormat.Days:
                    Append((int)abs.TotalDays, "d", false);
                    break;
                case TimeFormat.DaysHours:
                    Append((int)abs.TotalDays, "d", false);
                    Append(abs.Hours, "h", true);
                    break;
                case TimeFormat.DaysHoursMinutes:
                    Append((int)abs.TotalDays, "d", false);
                    Append(abs.Hours, "h", true);
                    Append(abs.Minutes, "m", true);
                    break;
                case TimeFormat.DaysHoursMinutesSeconds:
                    Append((int)abs.TotalDays, "d", false);
                    Append(abs.Hours, "h", true);
                    Append(abs.Minutes, "m", true);
                    Append(abs.Seconds, "s", true);
                    break;
                default:
                    Append((int)abs.TotalSeconds, "s", false);
                    break;
            }

            string result = builder.ToString();
            return value.Ticks < 0 ? "-" + result : result;
        }

        /// <summary>
        ///     Converts a <see cref="TimeSpan" /> to a clock-like string.
        /// </summary>
        /// <param name="value">Source time span.</param>
        /// <param name="includeDays">Whether to prefix output with days.</param>
        /// <param name="separator">Time unit separator.</param>
        /// <returns>Clock string such as "03:15:27" or "02:03:15:27".</returns>
        public static string ToClockString(this TimeSpan value, bool includeDays = false, string separator = ":")
        {
            if (string.IsNullOrEmpty(separator))
            {
                separator = ":";
            }

            TimeSpan abs = value.Duration();
            string body = includeDays
                ? abs.Days.ToString("D2") + separator + abs.Hours.ToString("D2") + separator +
                  abs.Minutes.ToString("D2") + separator + abs.Seconds.ToString("D2")
                : ((int)abs.TotalHours).ToString("D2") + separator + abs.Minutes.ToString("D2") + separator +
                  abs.Seconds.ToString("D2");

            return value.Ticks < 0 ? "-" + body : body;
        }
    }
}
