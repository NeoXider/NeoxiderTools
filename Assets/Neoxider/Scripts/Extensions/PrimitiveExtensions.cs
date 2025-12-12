using System;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for primitive data types like float, int, and bool.
    /// </summary>
    public static class PrimitiveExtensions
    {
        #region Bool Extensions

        /// <summary>
        ///     Converts a boolean value to an integer (1 for true, 0 for false).
        /// </summary>
        public static int ToInt(this bool value)
        {
            return value ? 1 : 0;
        }

        #endregion

        #region Constants

        /// <summary>
        ///     Default minimum value for normalization operations.
        /// </summary>
        public const float DefaultMinValue = -1000000f;

        /// <summary>
        ///     Default maximum value for normalization operations.
        /// </summary>
        public const float DefaultMaxValue = 1000000f;

        #endregion

        #region Float Extensions

        /// <summary>
        ///     Rounds a float value to a specified number of decimal places.
        /// </summary>
        public static float RoundToDecimal(this float value, int places)
        {
            if (places < 0)
            {
                throw new ArgumentException("Number of decimal places cannot be negative", nameof(places));
            }

            float multiplier = Mathf.Pow(10.0f, places);
            return Mathf.Round(value * multiplier) / multiplier;
        }

        /// <summary>
        ///     Formats a time value in seconds to a string representation.
        /// </summary>
        public static string FormatTime(this float timeSeconds, TimeFormat format = TimeFormat.Seconds,
            string separator = ":")
        {
            if (timeSeconds < 0)
            {
                timeSeconds = 0;
            }

            if (string.IsNullOrEmpty(separator))
            {
                separator = ":";
            }

            int totalSeconds = (int)timeSeconds;
            float fractional = timeSeconds - totalSeconds;
            if (fractional < 0f)
            {
                fractional = 0f;
            }

            // NOTE: existing formats use 2-digit fractional (centiseconds). Keep behavior for compatibility.
            int centiseconds = (int)(fractional * 100f);
            if (centiseconds < 0) centiseconds = 0;
            if (centiseconds > 99) centiseconds = 99;

            // True milliseconds (0..999) for the new MM:SS:ms mode.
            int milliseconds = (int)(fractional * 1000f);
            if (milliseconds < 0) milliseconds = 0;
            if (milliseconds > 999) milliseconds = 999;

            int totalMinutes = totalSeconds / 60;
            int totalHours = totalSeconds / 3600;
            int days = totalSeconds / 86400;

            int hoursPart = (totalSeconds % 86400) / 3600;
            int minutesPart = (totalSeconds % 3600) / 60;
            int secondsPart = totalSeconds % 60;

            if (separator.Length == 1)
            {
                char sep = separator[0];

                // Fast path only when all components fit into 2 digits (and ms into 3 digits).
                return format switch
                {
                    TimeFormat.Milliseconds => centiseconds < 100 ? Create2(centiseconds) : centiseconds.ToString("D2"),
                    TimeFormat.SecondsMilliseconds => totalSeconds < 100
                        ? Create2Sep2(totalSeconds, sep, centiseconds)
                        : totalSeconds.ToString("D2") + sep + centiseconds.ToString("D2"),
                    TimeFormat.Seconds => totalSeconds < 100 ? Create2(totalSeconds) : totalSeconds.ToString("D2"),
                    TimeFormat.Minutes => totalMinutes < 100 ? Create2(totalMinutes) : totalMinutes.ToString("D2"),
                    TimeFormat.MinutesSeconds => totalMinutes < 100
                        ? Create2Sep2(totalMinutes, sep, secondsPart)
                        : totalMinutes.ToString("D2") + sep + secondsPart.ToString("D2"),
                    TimeFormat.MinutesSecondsMilliseconds => totalMinutes < 100
                        ? Create2Sep2Sep3(totalMinutes, sep, secondsPart, milliseconds)
                        : totalMinutes.ToString("D2") + sep + secondsPart.ToString("D2") + sep + milliseconds.ToString("D3"),
                    TimeFormat.Hours => totalHours < 100 ? Create2(totalHours) : totalHours.ToString("D2"),
                    TimeFormat.HoursMinutes => totalHours < 100
                        ? Create2Sep2(totalHours, sep, minutesPart)
                        : totalHours.ToString("D2") + sep + minutesPart.ToString("D2"),
                    TimeFormat.HoursMinutesSeconds => totalHours < 100
                        ? Create2Sep2Sep2(totalHours, sep, minutesPart, secondsPart)
                        : totalHours.ToString("D2") + sep + minutesPart.ToString("D2") + sep + secondsPart.ToString("D2"),
                    TimeFormat.Days => days < 100 ? Create2(days) : days.ToString("D2"),
                    TimeFormat.DaysHours => days < 100
                        ? Create2Sep2(days, sep, hoursPart)
                        : days.ToString("D2") + sep + hoursPart.ToString("D2"),
                    TimeFormat.DaysHoursMinutes => days < 100
                        ? Create2Sep2Sep2(days, sep, hoursPart, minutesPart)
                        : days.ToString("D2") + sep + hoursPart.ToString("D2") + sep + minutesPart.ToString("D2"),
                    TimeFormat.DaysHoursMinutesSeconds => days < 100
                        ? Create2Sep2Sep2Sep2(days, sep, hoursPart, minutesPart, secondsPart)
                        : days.ToString("D2") + sep + hoursPart.ToString("D2") + sep + minutesPart.ToString("D2") + sep +
                          secondsPart.ToString("D2"),
                    _ => "00"
                };
            }

            // Fallback (multi-character separator). Less efficient but keeps compatibility.
            return format switch
            {
                TimeFormat.Milliseconds => centiseconds.ToString("D2"),
                TimeFormat.SecondsMilliseconds => totalSeconds.ToString("D2") + separator + centiseconds.ToString("D2"),
                TimeFormat.Seconds => totalSeconds.ToString("D2"),
                TimeFormat.Minutes => totalMinutes.ToString("D2"),
                TimeFormat.MinutesSeconds => totalMinutes.ToString("D2") + separator + secondsPart.ToString("D2"),
                TimeFormat.MinutesSecondsMilliseconds =>
                    totalMinutes.ToString("D2") + separator + secondsPart.ToString("D2") + separator + milliseconds.ToString("D3"),
                TimeFormat.Hours => totalHours.ToString("D2"),
                TimeFormat.HoursMinutes => totalHours.ToString("D2") + separator + minutesPart.ToString("D2"),
                TimeFormat.HoursMinutesSeconds =>
                    totalHours.ToString("D2") + separator + minutesPart.ToString("D2") + separator + secondsPart.ToString("D2"),
                TimeFormat.Days => days.ToString("D2"),
                TimeFormat.DaysHours => days.ToString("D2") + separator + hoursPart.ToString("D2"),
                TimeFormat.DaysHoursMinutes =>
                    days.ToString("D2") + separator + hoursPart.ToString("D2") + separator + minutesPart.ToString("D2"),
                TimeFormat.DaysHoursMinutesSeconds =>
                    days.ToString("D2") + separator + hoursPart.ToString("D2") + separator + minutesPart.ToString("D2") +
                    separator + secondsPart.ToString("D2"),
                _ => "00"
            };
        }

        private static string Create2(int a)
        {
            return string.Create(2, a, static (span, value) => Write2(span, value));
        }

        private static string Create2Sep2(int a, char sep, int b)
        {
            return string.Create(5, (a, sep, b), static (span, state) =>
            {
                Write2(span.Slice(0, 2), state.a);
                span[2] = state.sep;
                Write2(span.Slice(3, 2), state.b);
            });
        }

        private static string Create2Sep2Sep2(int a, char sep, int b, int c)
        {
            return string.Create(8, (a, sep, b, c), static (span, state) =>
            {
                Write2(span.Slice(0, 2), state.a);
                span[2] = state.sep;
                Write2(span.Slice(3, 2), state.b);
                span[5] = state.sep;
                Write2(span.Slice(6, 2), state.c);
            });
        }

        private static string Create2Sep2Sep3(int minutes, char sep, int seconds, int milliseconds)
        {
            return string.Create(9, (minutes, sep, seconds, milliseconds), static (span, state) =>
            {
                Write2(span.Slice(0, 2), state.minutes);
                span[2] = state.sep;
                Write2(span.Slice(3, 2), state.seconds);
                span[5] = state.sep;
                Write3(span.Slice(6, 3), state.milliseconds);
            });
        }

        private static string Create2Sep2Sep2Sep2(int a, char sep, int b, int c, int d)
        {
            return string.Create(11, (a, sep, b, c, d), static (span, state) =>
            {
                Write2(span.Slice(0, 2), state.a);
                span[2] = state.sep;
                Write2(span.Slice(3, 2), state.b);
                span[5] = state.sep;
                Write2(span.Slice(6, 2), state.c);
                span[8] = state.sep;
                Write2(span.Slice(9, 2), state.d);
            });
        }

        private static void Write2(Span<char> span, int value)
        {
            value = Mathf.Abs(value) % 100;
            span[0] = (char)('0' + value / 10);
            span[1] = (char)('0' + value % 10);
        }

        private static void Write3(Span<char> span, int value)
        {
            value = Mathf.Abs(value) % 1000;
            span[0] = (char)('0' + value / 100);
            span[1] = (char)('0' + (value / 10) % 10);
            span[2] = (char)('0' + value % 10);
        }

        /// <summary>
        ///     Formats a float number with a separator every three digits and specified decimal places.
        /// </summary>
        public static string FormatWithSeparator(this float number, string separator = "", int decimalPlaces = 2)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentException("Decimal places cannot be negative", nameof(decimalPlaces));
            }

            if (string.IsNullOrEmpty(separator))
            {
                return number.ToString($"F{decimalPlaces}");
            }

            string format = $"N{decimalPlaces}";
            return number.ToString(format).Replace(",", separator);
        }

        #endregion

        #region Normalization Extensions (for Float)

        /// <summary>
        ///     Normalizes a value to the range [0, 1] using default min/max values.
        /// </summary>
        public static float NormalizeToUnit(this float x)
        {
            return NormalizeToUnit(x, DefaultMinValue, DefaultMaxValue);
        }

        /// <summary>
        ///     Normalizes a value to the range [-1, 1] using default min/max values.
        /// </summary>
        public static float NormalizeToRange(this float x)
        {
            return NormalizeToRange(x, DefaultMinValue, DefaultMaxValue);
        }

        /// <summary>
        ///     Normalizes a value to the range [-1, 1] using specified min/max values.
        /// </summary>
        public static float NormalizeToRange(this float x, float min, float max)
        {
            return Mathf.Clamp(2.0f * NormalizeToUnit(x, min, max) - 1.0f, -1f, 1f);
        }

        /// <summary>
        ///     Normalizes a value to the range [0, 1] using specified min/max values.
        /// </summary>
        public static float NormalizeToUnit(this float x, float min, float max)
        {
            if (min >= max)
            {
                throw new ArgumentException($"Min value ({min}) must be less than max value ({max})");
            }

            return Mathf.Clamp01((x - min) / (max - min));
        }

        /// <summary>
        ///     Denormalizes a value from [0, 1] range to the specified range.
        /// </summary>
        public static float Denormalize(this float normalizedValue, float min, float max)
        {
            if (normalizedValue < 0f || normalizedValue > 1f)
            {
                throw new ArgumentException("Normalized value must be between 0 and 1", nameof(normalizedValue));
            }

            if (min >= max)
            {
                throw new ArgumentException($"Min value ({min}) must be less than max value ({max})");
            }

            return min + (max - min) * normalizedValue;
        }

        /// <summary>
        ///     Remaps a value from one range to another.
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float normalizedValue = NormalizeToUnit(value, fromMin, fromMax);
            return Denormalize(normalizedValue, toMin, toMax);
        }

        #endregion

        #region Int Extensions

        /// <summary>
        ///     Converts an integer value to a boolean (non-zero = true, zero = false).
        /// </summary>
        public static bool ToBool(this int value)
        {
            return value != 0;
        }

        /// <summary>
        ///     Formats an integer with a separator every three digits.
        /// </summary>
        public static string FormatWithSeparator(this int number, string separator)
        {
            if (string.IsNullOrEmpty(separator))
            {
                return number.ToString();
            }

            return string.Format("{0:N0}", number).Replace(",", separator);
        }

        #endregion
    }
}
