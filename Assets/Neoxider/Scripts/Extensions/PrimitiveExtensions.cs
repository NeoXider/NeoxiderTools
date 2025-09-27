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
                throw new ArgumentException("Number of decimal places cannot be negative", nameof(places));

            var multiplier = Mathf.Pow(10.0f, places);
            return Mathf.Round(value * multiplier) / multiplier;
        }

        /// <summary>
        ///     Formats a time value in seconds to a string representation.
        /// </summary>
        public static string FormatTime(this float timeSeconds, TimeFormat format = TimeFormat.Seconds,
            string separator = ":")
        {
            if (timeSeconds < 0)
                timeSeconds = 0;

            var days = (int)(timeSeconds / 86400);
            var hours = (int)(timeSeconds % 86400 / 3600);
            var minutes = (int)(timeSeconds % 3600 / 60);
            var seconds = (int)(timeSeconds % 60);
            var milliseconds = (int)((timeSeconds - (int)timeSeconds) * 100);

            return format switch
            {
                TimeFormat.Milliseconds => $"{milliseconds:D2}",
                TimeFormat.SecondsMilliseconds => $"{seconds:D2}{separator}{milliseconds:D2}",
                TimeFormat.Seconds => $"{seconds:D2}",
                TimeFormat.Minutes => $"{minutes:D2}",
                TimeFormat.MinutesSeconds => $"{minutes:D2}{separator}{seconds:D2}",
                TimeFormat.Hours => $"{hours:D2}",
                TimeFormat.HoursMinutes => $"{hours:D2}{separator}{minutes:D2}",
                TimeFormat.HoursMinutesSeconds => $"{hours:D2}{separator}{minutes:D2}{separator}{seconds:D2}",
                TimeFormat.Days => $"{days:D2}",
                TimeFormat.DaysHours => $"{days:D2}{separator}{hours:D2}",
                TimeFormat.DaysHoursMinutes => $"{days:D2}{separator}{hours:D2}{separator}{minutes:D2}",
                TimeFormat.DaysHoursMinutesSeconds =>
                    $"{days:D2}{separator}{hours:D2}{separator}{minutes:D2}{separator}{seconds:D2}",
                _ => "00"
            };
        }

        /// <summary>
        ///     Formats a float number with a separator every three digits and specified decimal places.
        /// </summary>
        public static string FormatWithSeparator(this float number, string separator = "", int decimalPlaces = 2)
        {
            if (decimalPlaces < 0)
                throw new ArgumentException("Decimal places cannot be negative", nameof(decimalPlaces));

            if (string.IsNullOrEmpty(separator))
                return number.ToString($"F{decimalPlaces}");

            var format = $"N{decimalPlaces}";
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
                throw new ArgumentException($"Min value ({min}) must be less than max value ({max})");

            return Mathf.Clamp01((x - min) / (max - min));
        }

        /// <summary>
        ///     Denormalizes a value from [0, 1] range to the specified range.
        /// </summary>
        public static float Denormalize(this float normalizedValue, float min, float max)
        {
            if (normalizedValue < 0f || normalizedValue > 1f)
                throw new ArgumentException("Normalized value must be between 0 and 1", nameof(normalizedValue));
            if (min >= max)
                throw new ArgumentException($"Min value ({min}) must be less than max value ({max})");

            return min + (max - min) * normalizedValue;
        }

        /// <summary>
        ///     Remaps a value from one range to another.
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            var normalizedValue = NormalizeToUnit(value, fromMin, fromMax);
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
                return number.ToString();

            return string.Format("{0:N0}", number).Replace(",", separator);
        }

        #endregion
    }
}