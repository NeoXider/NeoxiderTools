using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Neo.Extensions
{
    /// <summary>
    /// Defines output notation style for numeric formatting.
    /// </summary>
    public enum NumberNotation
    {
        /// <summary>
        /// Plain number without thousand grouping and suffixes.
        /// </summary>
        Plain = 0,
        /// <summary>
        /// Number with grouped thousands (for example: 1,234,567).
        /// </summary>
        Grouped = 1,
        /// <summary>
        /// Idle/clicker short notation with suffixes (K, M, B, T...).
        /// </summary>
        IdleShort = 2,
        /// <summary>
        /// Scientific notation (for example: 1.23e6).
        /// </summary>
        Scientific = 3
    }

    /// <summary>
    /// Defines rounding behavior used before number-to-string conversion.
    /// </summary>
    public enum NumberRoundingMode
    {
        /// <summary>
        /// Rounds to nearest value; midpoint goes to even digit.
        /// </summary>
        ToEven = 0,
        /// <summary>
        /// Rounds midpoint values away from zero.
        /// </summary>
        AwayFromZero = 1,
        /// <summary>
        /// Truncates fractional part toward zero.
        /// </summary>
        ToZero = 2,
        /// <summary>
        /// Rounds toward positive infinity (ceiling).
        /// </summary>
        ToPositiveInfinity = 3,
        /// <summary>
        /// Rounds toward negative infinity (floor).
        /// </summary>
        ToNegativeInfinity = 4
    }

    /// <summary>
    /// Formatting options for NumberFormatExtensions.
    /// </summary>
    [Serializable]
    public struct NumberFormatOptions
    {
        /// <summary>
        /// Output notation style.
        /// </summary>
        public NumberNotation Notation;
        /// <summary>
        /// Rounding mode used before string conversion.
        /// </summary>
        public NumberRoundingMode RoundingMode;
        /// <summary>
        /// Count of digits after decimal separator.
        /// </summary>
        public int Decimals;
        /// <summary>
        /// When true, trailing zeros in fractional part are removed.
        /// </summary>
        public bool TrimTrailingZeros;
        /// <summary>
        /// Thousands group separator.
        /// </summary>
        public string GroupSeparator;
        /// <summary>
        /// Fractional separator.
        /// </summary>
        public string DecimalSeparator;
        /// <summary>
        /// Prefix appended before formatted numeric value.
        /// </summary>
        public string Prefix;
        /// <summary>
        /// Suffix appended after formatted numeric value.
        /// </summary>
        public string Suffix;

        /// <summary>
        /// Creates custom number formatting options.
        /// </summary>
        public NumberFormatOptions(
            NumberNotation notation,
            int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven,
            bool trimTrailingZeros = true,
            string groupSeparator = ",",
            string decimalSeparator = ".",
            string prefix = "",
            string suffix = "")
        {
            Notation = notation;
            Decimals = ClampDecimals(decimals);
            RoundingMode = roundingMode;
            TrimTrailingZeros = trimTrailingZeros;
            GroupSeparator = string.IsNullOrEmpty(groupSeparator) ? "," : groupSeparator;
            DecimalSeparator = string.IsNullOrEmpty(decimalSeparator) ? "." : decimalSeparator;
            Prefix = prefix ?? string.Empty;
            Suffix = suffix ?? string.Empty;
        }

        /// <summary>
        /// Default grouped format preset.
        /// </summary>
        public static NumberFormatOptions Default =>
            new NumberFormatOptions(NumberNotation.Grouped, 0, NumberRoundingMode.ToEven, false, ",", ".");

        /// <summary>
        /// Default idle short notation preset.
        /// </summary>
        public static NumberFormatOptions IdleShort =>
            new NumberFormatOptions(NumberNotation.IdleShort, 1, NumberRoundingMode.ToEven, true, ".", ".");

        /// <summary>
        /// Clamps decimals to valid range [0..12].
        /// </summary>
        public static int ClampDecimals(int decimals)
        {
            return Math.Max(0, Math.Min(12, decimals));
        }
    }

    /// <summary>
    /// Extension helpers and core formatter for large and regular numeric values.
    /// </summary>
    public static class NumberFormatExtensions
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static readonly string[] BaseSuffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
        };

        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Formats an int using provided format options.
        /// </summary>
        /// <param name="value">Source integer value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this int value, NumberFormatOptions options)
        {
            return FormatNumber(new decimal(value), options);
        }

        /// <summary>
        /// Formats a long using provided format options.
        /// </summary>
        /// <param name="value">Source long value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this long value, NumberFormatOptions options)
        {
            return FormatNumber(new decimal(value), options);
        }

        /// <summary>
        /// Formats a float using provided format options.
        /// </summary>
        /// <param name="value">Source float value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this float value, NumberFormatOptions options)
        {
            return FormatNumber((double)value, options);
        }

        /// <summary>
        /// Formats a double using provided format options.
        /// </summary>
        /// <param name="value">Source double value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this double value, NumberFormatOptions options)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return value.ToString(Invariant);
            }

            return FormatNumber(value, options);
        }

        /// <summary>
        /// Formats a decimal using provided format options.
        /// </summary>
        /// <param name="value">Source decimal value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this decimal value, NumberFormatOptions options)
        {
            return FormatNumber(value, options);
        }

        /// <summary>
        /// Formats a BigInteger using provided format options.
        /// </summary>
        /// <param name="value">Source BigInteger value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this BigInteger value, NumberFormatOptions options)
        {
            return FormatNumber(value, options);
        }

        /// <summary>
        /// Formats BigInteger with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source BigInteger value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this BigInteger value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven)
        {
            NumberFormatOptions options = NumberFormatOptions.IdleShort;
            options.Decimals = NumberFormatOptions.ClampDecimals(decimals);
            options.RoundingMode = roundingMode;
            return FormatNumber(value, options);
        }

        /// <summary>
        /// Formats long with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source long value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this long value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven)
        {
            return new BigInteger(value).ToIdleString(decimals, roundingMode);
        }

        /// <summary>
        /// Formats int with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source int value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this int value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven)
        {
            return new BigInteger(value).ToIdleString(decimals, roundingMode);
        }

        /// <summary>
        /// Formats double with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source double value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this double value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven)
        {
            NumberFormatOptions options = NumberFormatOptions.IdleShort;
            options.Decimals = NumberFormatOptions.ClampDecimals(decimals);
            options.RoundingMode = roundingMode;
            return value.ToPrettyString(options);
        }

        /// <summary>
        /// Formats float with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source float value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this float value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven)
        {
            NumberFormatOptions options = NumberFormatOptions.IdleShort;
            options.Decimals = NumberFormatOptions.ClampDecimals(decimals);
            options.RoundingMode = roundingMode;
            return value.ToPrettyString(options);
        }

        /// <summary>
        /// Core formatter for BigInteger values.
        /// </summary>
        /// <param name="value">Source BigInteger value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string FormatNumber(BigInteger value, NumberFormatOptions options)
        {
            options.Decimals = NumberFormatOptions.ClampDecimals(options.Decimals);

            string raw = options.Notation switch
            {
                NumberNotation.Plain => FormatPlain(value, options),
                NumberNotation.Grouped => FormatGrouped(value, options),
                NumberNotation.Scientific => FormatScientific(value, options),
                _ => FormatIdle(value, options)
            };

            return ApplyAffixes(raw, options);
        }

        /// <summary>
        /// Core formatter for decimal values.
        /// </summary>
        /// <param name="value">Source decimal value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string FormatNumber(decimal value, NumberFormatOptions options)
        {
            options.Decimals = NumberFormatOptions.ClampDecimals(options.Decimals);

            string raw = options.Notation switch
            {
                NumberNotation.Plain => FormatPlain(value, options),
                NumberNotation.Grouped => FormatGrouped(value, options),
                NumberNotation.IdleShort => FormatIdle(value, options),
                NumberNotation.Scientific => FormatScientific(value, options),
                _ => FormatGrouped(value, options)
            };

            return ApplyAffixes(raw, options);
        }

        /// <summary>
        /// Core formatter for double values.
        /// </summary>
        /// <param name="value">Source double value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string FormatNumber(double value, NumberFormatOptions options)
        {
            options.Decimals = NumberFormatOptions.ClampDecimals(options.Decimals);

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return ApplyAffixes(value.ToString(Invariant), options);
            }

            try
            {
                return FormatNumber((decimal)value, options);
            }
            catch (OverflowException)
            {
                bool separatorsApplied = false;
                string raw = options.Notation switch
                {
                    NumberNotation.Plain => value.ToString("F" + options.Decimals, Invariant),
                    NumberNotation.Grouped => ApplySeparators(value.ToString("N" + options.Decimals, Invariant), options),
                    NumberNotation.IdleShort => FormatIdle(value, options),
                    NumberNotation.Scientific => value.ToString("E" + options.Decimals, Invariant).Replace("E", "e"),
                    _ => value.ToString(Invariant)
                };

                if (options.Notation == NumberNotation.Grouped || options.Notation == NumberNotation.IdleShort)
                {
                    separatorsApplied = true;
                }

                if (!separatorsApplied)
                {
                    raw = ApplySeparators(raw, options);
                }

                raw = NormalizeDecimalPart(raw, options);
                return ApplyAffixes(raw, options);
            }
        }

        private static string FormatPlain(BigInteger value, NumberFormatOptions options)
        {
            string text = value.ToString(Invariant);
            return NormalizeDecimalPart(text, options);
        }

        private static string FormatGrouped(BigInteger value, NumberFormatOptions options)
        {
            bool isNegative = value < 0;
            string digits = BigInteger.Abs(value).ToString(Invariant);

            if (digits.Length <= 3)
            {
                return (isNegative ? "-" : string.Empty) + digits;
            }

            string separator = string.IsNullOrEmpty(options.GroupSeparator) ? "," : options.GroupSeparator;
            int groupCount = (digits.Length - 1) / 3;
            var sb = new StringBuilder(digits.Length + groupCount * separator.Length + 1);

            if (isNegative)
            {
                sb.Append('-');
            }

            int firstGroupLength = digits.Length % 3;
            if (firstGroupLength == 0)
            {
                firstGroupLength = 3;
            }

            sb.Append(digits, 0, firstGroupLength);

            for (int i = firstGroupLength; i < digits.Length; i += 3)
            {
                sb.Append(separator);
                sb.Append(digits, i, 3);
            }

            return sb.ToString();
        }

        private static string FormatIdle(BigInteger value, NumberFormatOptions options)
        {
            bool isNegative = value < 0;
            BigInteger absValue = BigInteger.Abs(value);

            if (absValue < 1000)
            {
                return (isNegative ? "-" : string.Empty) + absValue.ToString(Invariant);
            }

            string digits = absValue.ToString(Invariant);
            int tier = (digits.Length - 1) / 3;
            int leadLen = digits.Length - tier * 3;
            int decimals = NumberFormatOptions.ClampDecimals(options.Decimals);

            int neededDigits = Math.Min(digits.Length, leadLen + decimals + 1);
            string prefixDigits = digits.Substring(0, neededDigits);
            decimal prefix = decimal.Parse(prefixDigits, Invariant);
            decimal divisor = Pow10Decimal(neededDigits - leadLen);
            decimal compact = prefix / divisor;
            compact = Round(compact, decimals, options.RoundingMode);

            if (compact >= 1000m)
            {
                compact /= 1000m;
                tier++;
            }

            string numberText = FormatDecimalValue(compact, decimals, options);
            string suffix = GetSuffix(tier);
            return (isNegative ? "-" : string.Empty) + numberText + suffix;
        }

        private static string FormatScientific(BigInteger value, NumberFormatOptions options)
        {
            if (value.IsZero)
            {
                return "0";
            }

            bool isNegative = value < 0;
            string digits = BigInteger.Abs(value).ToString(Invariant);
            int exponent = digits.Length - 1;
            int decimals = NumberFormatOptions.ClampDecimals(options.Decimals);

            int neededDigits = Math.Min(digits.Length, decimals + 2);
            string prefixDigits = digits.Substring(0, neededDigits);
            decimal prefix = decimal.Parse(prefixDigits, Invariant);
            decimal divisor = Pow10Decimal(neededDigits - 1);
            decimal mantissa = prefix / divisor;
            mantissa = Round(mantissa, decimals, options.RoundingMode);

            if (mantissa >= 10m)
            {
                mantissa /= 10m;
                exponent++;
            }

            string mantissaText = FormatDecimalValue(mantissa, decimals, options);
            return (isNegative ? "-" : string.Empty) + mantissaText + "e" + exponent.ToString(Invariant);
        }

        private static string FormatPlain(decimal value, NumberFormatOptions options)
        {
            decimal rounded = Round(value, options.Decimals, options.RoundingMode);
            return FormatDecimalValue(rounded, options.Decimals, options);
        }

        private static string FormatGrouped(decimal value, NumberFormatOptions options)
        {
            decimal rounded = Round(value, options.Decimals, options.RoundingMode);
            string pattern = "N" + options.Decimals;
            string raw = rounded.ToString(pattern, Invariant);
            return ApplySeparators(raw, options);
        }

        private static string FormatIdle(decimal value, NumberFormatOptions options)
        {
            decimal abs = Math.Abs(value);
            if (abs < 1000m)
            {
                decimal rounded = Round(value, options.Decimals, options.RoundingMode);
                return FormatDecimalValue(rounded, options.Decimals, options);
            }

            bool isNegative = value < 0m;
            int tier = 0;
            decimal compact = abs;

            while (compact >= 1000m)
            {
                compact /= 1000m;
                tier++;
            }

            compact = Round(compact, options.Decimals, options.RoundingMode);

            if (compact >= 1000m)
            {
                compact /= 1000m;
                tier++;
            }

            string numberText = FormatDecimalValue(compact, options.Decimals, options);
            return (isNegative ? "-" : string.Empty) + numberText + GetSuffix(tier);
        }

        private static string FormatScientific(decimal value, NumberFormatOptions options)
        {
            if (value == 0m)
            {
                return "0";
            }

            bool isNegative = value < 0m;
            decimal abs = Math.Abs(value);
            int exponent = 0;

            while (abs >= 10m)
            {
                abs /= 10m;
                exponent++;
            }

            while (abs > 0m && abs < 1m)
            {
                abs *= 10m;
                exponent--;
            }

            abs = Round(abs, options.Decimals, options.RoundingMode);
            if (abs >= 10m)
            {
                abs /= 10m;
                exponent++;
            }

            string mantissa = FormatDecimalValue(abs, options.Decimals, options);
            return (isNegative ? "-" : string.Empty) + mantissa + "e" + exponent.ToString(Invariant);
        }

        private static string FormatIdle(double value, NumberFormatOptions options)
        {
            if (Math.Abs(value) < 1000d)
            {
                double roundedSmall = Round(value, options.Decimals, options.RoundingMode);
                return NormalizeDecimalPart(
                    ApplySeparators(roundedSmall.ToString("F" + options.Decimals, Invariant), options),
                    options);
            }

            bool isNegative = value < 0d;
            double compact = Math.Abs(value);
            int tier = 0;

            while (compact >= 1000d && tier < int.MaxValue - 2)
            {
                compact /= 1000d;
                tier++;
            }

            compact = Round(compact, options.Decimals, options.RoundingMode);

            if (compact >= 1000d)
            {
                compact /= 1000d;
                tier++;
            }

            string numberText = NormalizeDecimalPart(
                ApplySeparators(compact.ToString("F" + options.Decimals, Invariant), options),
                options);

            return (isNegative ? "-" : string.Empty) + numberText + GetSuffix(tier);
        }

        private static string GetSuffix(int tier)
        {
            if (tier < BaseSuffixes.Length)
            {
                return BaseSuffixes[tier];
            }

            int n = tier - BaseSuffixes.Length;
            var sb = new StringBuilder();

            do
            {
                sb.Insert(0, Alphabet[n % 26]);
                n = (n / 26) - 1;
            } while (n >= 0);

            return sb.ToString();
        }

        private static decimal Round(decimal value, int decimals, NumberRoundingMode mode)
        {
            decimals = NumberFormatOptions.ClampDecimals(decimals);
            decimal factor = Pow10Decimal(decimals);

            return mode switch
            {
                NumberRoundingMode.ToEven => Math.Round(value, decimals, MidpointRounding.ToEven),
                NumberRoundingMode.AwayFromZero => Math.Round(value, decimals, MidpointRounding.AwayFromZero),
                NumberRoundingMode.ToZero => Math.Truncate(value * factor) / factor,
                NumberRoundingMode.ToPositiveInfinity => Math.Ceiling(value * factor) / factor,
                NumberRoundingMode.ToNegativeInfinity => Math.Floor(value * factor) / factor,
                _ => Math.Round(value, decimals, MidpointRounding.ToEven)
            };
        }

        private static double Round(double value, int decimals, NumberRoundingMode mode)
        {
            decimals = NumberFormatOptions.ClampDecimals(decimals);
            double factor = Math.Pow(10d, decimals);

            return mode switch
            {
                NumberRoundingMode.ToEven => Math.Round(value, decimals, MidpointRounding.ToEven),
                NumberRoundingMode.AwayFromZero => Math.Round(value, decimals, MidpointRounding.AwayFromZero),
                NumberRoundingMode.ToZero => Math.Truncate(value * factor) / factor,
                NumberRoundingMode.ToPositiveInfinity => Math.Ceiling(value * factor) / factor,
                NumberRoundingMode.ToNegativeInfinity => Math.Floor(value * factor) / factor,
                _ => Math.Round(value, decimals, MidpointRounding.ToEven)
            };
        }

        private static decimal Pow10Decimal(int power)
        {
            decimal result = 1m;
            for (int i = 0; i < power; i++)
            {
                result *= 10m;
            }

            return result;
        }

        private static string FormatDecimalValue(decimal value, int decimals, NumberFormatOptions options)
        {
            string pattern = "F" + decimals;
            string raw = value.ToString(pattern, Invariant);
            string withSeparators = ApplySeparators(raw, options);
            return NormalizeDecimalPart(withSeparators, options);
        }

        private static string ApplySeparators(string text, NumberFormatOptions options)
        {
            string group = options.GroupSeparator ?? ",";
            string decimalSeparator = options.DecimalSeparator ?? ".";

            if (group == "," && decimalSeparator == ".")
            {
                return text;
            }

            var sb = new StringBuilder(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ',')
                {
                    sb.Append(group);
                }
                else if (c == '.')
                {
                    sb.Append(decimalSeparator);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static string NormalizeDecimalPart(string text, NumberFormatOptions options)
        {
            if (!options.TrimTrailingZeros)
            {
                return text;
            }

            string decimalSeparator = string.IsNullOrEmpty(options.DecimalSeparator) ? "." : options.DecimalSeparator;
            int separatorIndex = text.IndexOf(decimalSeparator, StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return text;
            }

            int end = text.Length - 1;
            while (end > separatorIndex && text[end] == '0')
            {
                end--;
            }

            if (end == separatorIndex)
            {
                end--;
            }

            return text.Substring(0, end + 1);
        }

        private static string ApplyAffixes(string raw, NumberFormatOptions options)
        {
            string prefix = options.Prefix ?? string.Empty;
            string suffix = options.Suffix ?? string.Empty;
            return prefix + raw + suffix;
        }
    }
}
