using System;
using System.Globalization;

namespace Neo.Extensions
{
    /// <summary>
    /// Provides helper methods for safe UTC date-time serialization and calculations.
    /// </summary>
    public static class DateTimeExtensions
    {
        private const string RoundTripFormat = "o";

        /// <summary>
        /// Converts a <see cref="DateTime"/> value to a round-trip UTC string.
        /// </summary>
        /// <param name="utc">Source date-time value.</param>
        /// <returns>UTC string formatted with round-trip ISO format.</returns>
        public static string ToRoundTripUtcString(this DateTime utc)
        {
            return EnsureUtc(utc).ToString(RoundTripFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Tries to parse a persisted UTC string in round-trip format with legacy fallbacks.
        /// </summary>
        /// <param name="raw">Serialized date-time string.</param>
        /// <param name="utc">Parsed UTC value when successful.</param>
        /// <returns><c>true</c> when parsing succeeds; otherwise <c>false</c>.</returns>
        public static bool TryParseUtcRoundTrip(this string raw, out DateTime utc)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                utc = default;
                return false;
            }

            if (DateTime.TryParseExact(
                    raw,
                    RoundTripFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsedRoundTrip))
            {
                utc = EnsureUtc(parsedRoundTrip);
                return true;
            }

            if (DateTime.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime parsedInvariant) ||
                DateTime.TryParse(raw, out parsedInvariant))
            {
                utc = EnsureUtc(parsedInvariant);
                return true;
            }

            utc = default;
            return false;
        }

        /// <summary>
        /// Gets elapsed seconds from a UTC timestamp to another UTC timestamp.
        /// </summary>
        /// <param name="utc">Start UTC timestamp.</param>
        /// <param name="nowUtc">End UTC timestamp.</param>
        /// <returns>Elapsed seconds as a float.</returns>
        public static float GetSecondsSinceUtc(this DateTime utc, DateTime nowUtc)
        {
            DateTime start = EnsureUtc(utc);
            DateTime end = EnsureUtc(nowUtc);
            return (float)(end - start).TotalSeconds;
        }

        /// <summary>
        /// Gets remaining seconds until a UTC target timestamp.
        /// </summary>
        /// <param name="targetUtc">Target UTC timestamp.</param>
        /// <param name="nowUtc">Current UTC timestamp.</param>
        /// <returns>Remaining seconds as a float.</returns>
        public static float GetSecondsUntilUtc(this DateTime targetUtc, DateTime nowUtc)
        {
            DateTime target = EnsureUtc(targetUtc);
            DateTime now = EnsureUtc(nowUtc);
            return (float)(target - now).TotalSeconds;
        }

        /// <summary>
        /// Converts the value to UTC preserving semantic intent.
        /// </summary>
        /// <param name="value">Input date-time value.</param>
        /// <returns>UTC normalized date-time.</returns>
        public static DateTime EnsureUtc(this DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
