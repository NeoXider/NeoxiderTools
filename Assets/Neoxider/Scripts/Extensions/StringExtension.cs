using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        ///     Splits camelCase string into spaced words.
        /// </summary>
        public static string SplitCamelCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new StringBuilder();
            sb.Append(input[0]);

            for (var i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i])) sb.Append(' ');
                sb.Append(input[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Checks if string is null or empty after trimming whitespace.
        /// </summary>
        public static bool IsNullOrEmptyAfterTrim(this string input)
        {
            return string.IsNullOrEmpty(input?.Trim());
        }

        /// <summary>
        ///     Converts HEX string to Unity Color.
        /// </summary>
        public static Color ToColor(this string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        /// <summary>
        ///     Converts string to camelCase format.
        /// </summary>
        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        ///     Truncates string to max length with ellipsis.
        /// </summary>
        public static string Truncate(this string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength) return input;
            return input.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        ///     Checks if string contains only digits.
        /// </summary>
        public static bool IsNumeric(this string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.All(char.IsDigit);
        }

        /// <summary>
        ///     Generates a random string with specified length.
        /// </summary>
        public static string RandomString(int length, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++) sb.Append(chars[Random.Range(0, chars.Length)]);
            return sb.ToString();
        }

        /// <summary>
        ///     Reverses the string.
        /// </summary>
        public static string Reverse(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var arr = input.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        /// <summary>
        ///     Converts string to boolean (case insensitive).
        /// </summary>
        public static bool ToBool(this string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.ToLower() switch
            {
                "true" => true,
                "yes" => true,
                "1" => true,
                _ => false
            };
        }

        /// <summary>
        ///     Safely parses a string to an integer.
        /// </summary>
        /// <returns>The parsed integer, or the default value if parsing fails.</returns>
        public static int ToInt(this string input, int defaultValue = 0)
        {
            if (int.TryParse(input, out var result)) return result;
            return defaultValue;
        }

        /// <summary>
        ///     Safely parses a string to a float.
        /// </summary>
        /// <returns>The parsed float, or the default value if parsing fails.</returns>
        public static float ToFloat(this string input, float defaultValue = 0f)
        {
            if (float.TryParse(input, out var result)) return result;
            return defaultValue;
        }

        #region Rich Text

        /// <summary>
        ///     Wraps the string in <b></b> tags.
        /// </summary>
        public static string Bold(this string input)
        {
            return $"<b>{input}</b>";
        }

        /// <summary>
        ///     Wraps the string in <i></i> tags.
        /// </summary>
        public static string Italic(this string input)
        {
            return $"<i>{input}</i>";
        }

        /// <summary>
        ///     Wraps the string in <size=></size> tags.
        /// </summary>
        public static string Size(this string input, int size)
        {
            return $"<size={size}>{input}</size>";
        }

        /// <summary>
        ///     Wraps the string in <color=></color> tags.
        /// </summary>
        public static string SetColor(this string input, Color color)
        {
            var hexColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hexColor}>{input}</color>";
        }

        /// <summary>
        ///     Applies a rainbow effect to the text, coloring each character differently.
        /// </summary>
        public static string Rainbow(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                var color = Color.HSVToRGB((float)i / input.Length, 1, 1);
                sb.Append(input[i].ToString().SetColor(color));
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Applies a gradient to the text between two colors.
        /// </summary>
        public static string Gradient(this string input, Color startColor, Color endColor)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                var color = Color.Lerp(startColor, endColor, (float)i / (input.Length - 1));
                sb.Append(input[i].ToString().SetColor(color));
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Applies a random color to each character of the text.
        /// </summary>
        public static string RandomColors(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                var color = new Color(Random.value, Random.value, Random.value);
                sb.Append(input[i].ToString().SetColor(color));
            }

            return sb.ToString();
        }

        #endregion
    }
}