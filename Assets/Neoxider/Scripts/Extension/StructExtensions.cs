using System;
using Neo.Tools;
using UnityEngine;

namespace Neo
{
    /// <summary>
    /// Extension methods for basic data types and structures
    /// </summary>
    public static class StructExtensions
    {
        /// <summary>
        /// Rounds a float value to a specified number of decimal places
        /// </summary>
        /// <param name="value">The value to round</param>
        /// <param name="places">Number of decimal places (must be non-negative)</param>
        /// <returns>Rounded value</returns>
        /// <exception cref="ArgumentException">Thrown when places is negative</exception>
        public static float RoundToDecimal(this float value, int places)
        {
            if (places < 0)
                throw new ArgumentException("Number of decimal places cannot be negative", nameof(places));

            float multiplier = Mathf.Pow(10.0f, places);
            return Mathf.Round(value * multiplier) / multiplier;
        }

        /// <summary>
        /// Converts a boolean value to an integer (1 for true, 0 for false)
        /// </summary>
        /// <param name="value">Boolean value to convert</param>
        /// <returns>1 if true, 0 if false</returns>
        public static int ToInt(this bool value)
        {
            return value ? 1 : 0;
        }

        /// <summary>
        /// Converts an integer value to a boolean (non-zero = true, zero = false)
        /// </summary>
        /// <param name="value">Integer value to convert</param>
        /// <returns>True if value is non-zero, false otherwise</returns>
        public static bool ToBool(this int value)
        {
            return value != 0;
        }

        /// <summary>
        /// Formats a time value in seconds to a string representation
        /// </summary>
        /// <param name="timeSeconds">Time in seconds</param>
        /// <param name="format">Desired time format</param>
        /// <param name="separator">Separator between time components</param>
        /// <returns>Formatted time string</returns>
        public static string FormatTime(this float timeSeconds, TimeFormat format = TimeFormat.Seconds, string separator = ":")
        {
            if (timeSeconds < 0)
                timeSeconds = 0;

            int days = (int)(timeSeconds / 86400);
            int hours = (int)((timeSeconds % 86400) / 3600);
            int minutes = (int)((timeSeconds % 3600) / 60);
            int seconds = (int)(timeSeconds % 60);
            int milliseconds = (int)((timeSeconds - (int)timeSeconds) * 100);

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
                TimeFormat.DaysHoursMinutesSeconds => $"{days:D2}{separator}{hours:D2}{separator}{minutes:D2}{separator}{seconds:D2}",
                _ => "00"
            };
        }

        /// <summary>
        /// Counts null elements in an array
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Array to check</param>
        /// <returns>Number of null elements</returns>
        /// <exception cref="ArgumentNullException">Thrown when array is null</exception>
        public static int CountEmptyElements<T>(this T[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int emptyCount = 0;
            foreach (T element in array)
            {
                if (element == null)
                {
                    emptyCount++;
                }
            }
            return emptyCount;
        }

        /// <summary>
        /// Formats an integer with a separator every three digits
        /// </summary>
        /// <param name="number">Number to format</param>
        /// <param name="separator">Separator to use between groups</param>
        /// <returns>Formatted number string</returns>
        public static string FormatWithSeparator(this int number, string separator)
        {
            if (string.IsNullOrEmpty(separator))
                return number.ToString();

            return string.Format("{0:N0}", number).Replace(",", separator);
        }

        /// <summary>
        /// Formats a float number with a separator every three digits and specified decimal places
        /// </summary>
        /// <param name="number">Number to format</param>
        /// <param name="separator">Separator to use between groups</param>
        /// <param name="decimalPlaces">Number of decimal places to show</param>
        /// <returns>Formatted number string</returns>
        /// <exception cref="ArgumentException">Thrown when decimalPlaces is negative</exception>
        public static string FormatWithSeparator(this float number, string separator = "", int decimalPlaces = 2)
        {
            if (decimalPlaces < 0)
                throw new ArgumentException("Decimal places cannot be negative", nameof(decimalPlaces));

            if (string.IsNullOrEmpty(separator))
                return number.ToString($"F{decimalPlaces}");

            string format = $"N{decimalPlaces}";
            return number.ToString(format).Replace(",", separator);
        }
    }
}
