using System;
using System.Globalization;

namespace Neo.Extensions
{
    /// <summary>
    ///     Provides parsing helpers for converting text durations into seconds.
    /// </summary>
    public static class TimeParsingExtensions
    {
        /// <summary>
        ///     Tries to parse a duration string into total seconds.
        /// </summary>
        /// <param name="raw">Input duration text.</param>
        /// <param name="seconds">Parsed total seconds when successful.</param>
        /// <param name="separator">Token separator between time units.</param>
        /// <returns><c>true</c> when parsing succeeds; otherwise <c>false</c>.</returns>
        public static bool TryParseDuration(string raw, out float seconds, string separator = ":")
        {
            seconds = 0f;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string normalized = raw.Trim();
            if (string.IsNullOrEmpty(separator))
            {
                separator = ":";
            }

            if (!normalized.Contains(separator, StringComparison.Ordinal))
            {
                return TryParseSingleSeconds(normalized, out seconds);
            }

            string[] parts = normalized.Split(new[] { separator }, StringSplitOptions.None);
            if (parts.Length < 2 || parts.Length > 4)
            {
                return false;
            }

            double[] values = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                {
                    return false;
                }

                if (parsed < 0d)
                {
                    return false;
                }

                values[i] = parsed;
            }

            if (!ValidateRanges(values))
            {
                return false;
            }

            double total = parts.Length switch
            {
                2 => values[0] * 60d + values[1],
                3 => values[0] * 3600d + values[1] * 60d + values[2],
                4 => values[0] * 86400d + values[1] * 3600d + values[2] * 60d + values[3],
                _ => -1d
            };

            if (total < 0d || total > float.MaxValue)
            {
                return false;
            }

            seconds = (float)total;
            return true;
        }

        /// <summary>
        ///     Tries to parse a duration string into total seconds.
        /// </summary>
        /// <param name="raw">Input duration text.</param>
        /// <param name="seconds">Parsed total seconds when successful.</param>
        /// <param name="separator">Token separator between time units.</param>
        /// <returns><c>true</c> when parsing succeeds; otherwise <c>false</c>.</returns>
        public static bool TryParseDuration(string raw, out float seconds, char separator)
        {
            return TryParseDuration(raw, out seconds, separator.ToString());
        }

        private static bool TryParseSingleSeconds(string value, out float seconds)
        {
            seconds = 0f;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedInvariant) &&
                !double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsedInvariant))
            {
                return false;
            }

            if (parsedInvariant < 0d || parsedInvariant > float.MaxValue)
            {
                return false;
            }

            seconds = (float)parsedInvariant;
            return true;
        }

        private static bool ValidateRanges(double[] values)
        {
            switch (values.Length)
            {
                case 2:
                    return values[1] < 60d;
                case 3:
                    return values[1] < 60d && values[2] < 60d;
                case 4:
                    return values[1] < 24d && values[2] < 60d && values[3] < 60d;
                default:
                    return false;
            }
        }
    }
}