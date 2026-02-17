using System;
using System.Text;

namespace Neo.Extensions
{
    /// <summary>
    /// Provides formatting helpers for <see cref="TimeSpan"/>.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Converts a <see cref="TimeSpan"/> to a compact human-readable string.
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
            StringBuilder builder = new StringBuilder(24);
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
                if (number <= 0 || added >= maxParts)
                {
                    return;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(number);
                builder.Append(suffix);
                added++;
            }
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> to a clock-like string.
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
