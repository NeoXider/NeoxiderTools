using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Neo.Extensions
{
    /// <summary>
    ///     Defines output notation style for numeric formatting.
    /// </summary>
    public enum NumberNotation
    {
        /// <summary>
        ///     Plain number without thousand grouping and suffixes.
        /// </summary>
        Plain = 0,

        /// <summary>
        ///     Number with grouped thousands (for example: 1,234,567).
        /// </summary>
        Grouped = 1,

        /// <summary>
        ///     Idle/clicker short notation with suffixes (K, M, B, T...).
        /// </summary>
        IdleShort = 2,

        /// <summary>
        ///     Scientific notation (for example: 1.23e6).
        /// </summary>
        Scientific = 3
    }

    /// <summary>
    ///     Defines rounding behavior used before number-to-string conversion.
    /// </summary>
    public enum NumberRoundingMode
    {
        /// <summary>
        ///     Rounds to nearest value; midpoint goes to even digit.
        /// </summary>
        ToEven = 0,

        /// <summary>
        ///     Rounds midpoint values away from zero.
        /// </summary>
        AwayFromZero = 1,

        /// <summary>
        ///     Truncates fractional part toward zero.
        /// </summary>
        ToZero = 2,

        /// <summary>
        ///     Rounds toward positive infinity (ceiling).
        /// </summary>
        ToPositiveInfinity = 3,

        /// <summary>
        ///     Rounds toward negative infinity (floor).
        /// </summary>
        ToNegativeInfinity = 4
    }

    /// <summary>
    ///     Formatting options for NumberFormatExtensions.
    /// </summary>
    [Serializable]
    public struct NumberFormatOptions
    {
        /// <summary>
        ///     Output notation style.
        /// </summary>
        public NumberNotation Notation;

        /// <summary>
        ///     Rounding mode used before string conversion.
        /// </summary>
        public NumberRoundingMode RoundingMode;

        /// <summary>
        ///     Count of digits after decimal separator.
        /// </summary>
        public int Decimals;

        /// <summary>
        ///     When true, trailing zeros in fractional part are removed.
        /// </summary>
        public bool TrimTrailingZeros;

        /// <summary>
        ///     Thousands group separator.
        /// </summary>
        public string GroupSeparator;

        /// <summary>
        ///     Fractional separator.
        /// </summary>
        public string DecimalSeparator;

        /// <summary>
        ///     Prefix appended before formatted numeric value.
        /// </summary>
        public string Prefix;

        /// <summary>
        ///     Suffix appended after formatted numeric value.
        /// </summary>
        public string Suffix;

        /// <summary>
        ///     Creates custom number formatting options.
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
        ///     Default grouped format preset.
        /// </summary>
        public static NumberFormatOptions Default =>
            new(NumberNotation.Grouped, 0, NumberRoundingMode.ToEven, false);

        /// <summary>
        ///     Default idle short notation preset.
        /// </summary>
        public static NumberFormatOptions IdleShort =>
            new(NumberNotation.IdleShort, 1, NumberRoundingMode.ToEven, true, ".");

        /// <summary>
        ///     Clamps decimals to valid range [0..12].
        /// </summary>
        public static int ClampDecimals(int decimals)
        {
            return Math.Max(0, Math.Min(12, decimals));
        }
    }

    /// <summary>
    ///     Extension helpers and core formatter for large and regular numeric values.
    /// </summary>
    public static class NumberFormatExtensions
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz";
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static readonly string[] BaseSuffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
        };

        /// <summary>
        ///     Formats an int using provided format options.
        /// </summary>
        /// <param name="value">Source integer value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this int value, NumberFormatOptions options)
        {
            return FormatNumber(new decimal(value), options);
        }

        /// <summary>
        ///     Formats a long using provided format options.
        /// </summary>
        /// <param name="value">Source long value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this long value, NumberFormatOptions options)
        {
            return FormatNumber(new decimal(value), options);
        }

        /// <summary>
        ///     Formats a float using provided format options.
        /// </summary>
        /// <param name="value">Source float value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this float value, NumberFormatOptions options)
        {
            return FormatNumber(value, options);
        }

        /// <summary>
        ///     Formats a double using provided format options.
        /// </summary>
        /// <param name="value">Source double value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this double value, NumberFormatOptions options)
        {
            return FormatNumber(value, options);
        }

        /// <summary>
        ///     Formats a decimal using provided format options.
        /// </summary>
        /// <param name="value">Source decimal value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this decimal value, NumberFormatOptions options)
        {
            return FormatNumber(value, options);
        }

        /// <summary>
        ///     Formats a BigInteger using provided format options.
        /// </summary>
        /// <param name="value">Source BigInteger value.</param>
        /// <param name="options">Formatting options.</param>
        /// <returns>Formatted string representation.</returns>
        public static string ToPrettyString(this BigInteger value, NumberFormatOptions options)
        {
            return FormatNumber(value, options);
        }

        /// <summary>
        ///     Formats BigInteger with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source BigInteger value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this BigInteger value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven,
            bool trimTrailingZeros = true)
        {
            NumberFormatOptions options = NumberFormatOptions.IdleShort;
            options.Decimals = NumberFormatOptions.ClampDecimals(decimals);
            options.RoundingMode = roundingMode;
            options.TrimTrailingZeros = trimTrailingZeros;
            return FormatNumber(value, options);
        }

        /// <summary>
        ///     Formats long with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source long value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this long value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven,
            bool trimTrailingZeros = true)
        {
            return new BigInteger(value).ToIdleString(decimals, roundingMode, trimTrailingZeros);
        }

        /// <summary>
        ///     Formats int with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source int value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this int value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven,
            bool trimTrailingZeros = true)
        {
            return new BigInteger(value).ToIdleString(decimals, roundingMode, trimTrailingZeros);
        }

        /// <summary>
        ///     Formats double with default idle short notation and selected rounding.
        /// </summary>
        /// <param name="value">Source double value.</param>
        /// <param name="decimals">Fraction digits count.</param>
        /// <param name="roundingMode">Rounding strategy.</param>
        /// <returns>Idle short formatted representation.</returns>
        public static string ToIdleString(this double value, int decimals = 1,
            NumberRoundingMode roundingMode = NumberRoundingMode.ToEven,
            bool trimTrailingZeros = true)
        {
            NumberFormatOptions options = NumberFormatOptions.IdleShort;
            options.Decimals = NumberFormatOptions.ClampDecimals(decimals);
            options.RoundingMode = roundingMode;
            options.TrimTrailingZeros = trimTrailingZeros;
            return value.ToPrettyString(options);
        }

        /// <summary>
        ///     Formats float with default idle short notation and selected rounding.
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
        ///     Core formatter for BigInteger values.
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
        ///     Core formatter for decimal values.
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
        ///     Core formatter for double values.
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

            // A decimal has at most 28 fractional places. Keep enough significant digits for
            // scientific notation (12 fractional digits plus a rounding digit) on the double path.
            if (value == 0d || Math.Abs(value) >= 1e-15d)
            {
                try
                {
                    return FormatNumber((decimal)value, options);
                }
                catch (OverflowException)
                {
                    // Finite doubles beyond the decimal range are formatted below.
                }
            }

            string raw = options.Notation switch
            {
                NumberNotation.Plain => NormalizeAndApplySeparators(
                    Round(value, options.Decimals, options.RoundingMode).ToString("F" + options.Decimals, Invariant),
                    options),
                NumberNotation.Grouped => NormalizeAndApplySeparators(
                    Round(value, options.Decimals, options.RoundingMode).ToString("N" + options.Decimals, Invariant),
                    options),
                NumberNotation.IdleShort => FormatIdle(value, options),
                NumberNotation.Scientific => FormatScientific(value, options),
                _ => value.ToString(Invariant)
            };

            return ApplyAffixes(raw, options);
        }

        private static string FormatPlain(BigInteger value, NumberFormatOptions options)
        {
            string text = value.ToString(Invariant);
            return NormalizeAndApplySeparators(text, options);
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
            StringBuilder sb = new(digits.Length + groupCount * separator.Length + 1);

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
            options.RoundingMode = ForMagnitude(options.RoundingMode, isNegative);
            BigInteger absValue = BigInteger.Abs(value);

            if (absValue < 1000)
            {
                return (isNegative ? "-" : string.Empty) + absValue.ToString(Invariant);
            }

            string digits = absValue.ToString(Invariant);
            int tier = (digits.Length - 1) / 3;
            int leadLen = digits.Length - tier * 3;
            int decimals = NumberFormatOptions.ClampDecimals(options.Decimals);

            decimal compact = ReadSignificand(digits, leadLen, decimals);
            compact = Round(compact, decimals, options.RoundingMode);

            while (compact >= 1000m)
            {
                compact /= 1000m;
                tier++;
                compact = Round(compact, decimals, options.RoundingMode);
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
            options.RoundingMode = ForMagnitude(options.RoundingMode, isNegative);
            string digits = BigInteger.Abs(value).ToString(Invariant);
            int exponent = digits.Length - 1;
            int decimals = NumberFormatOptions.ClampDecimals(options.Decimals);

            decimal mantissa = ReadSignificand(digits, 1, decimals);
            mantissa = Round(mantissa, decimals, options.RoundingMode);

            while (mantissa >= 10m)
            {
                mantissa /= 10m;
                exponent++;
                mantissa = Round(mantissa, decimals, options.RoundingMode);
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
            return NormalizeAndApplySeparators(raw, options);
        }

        private static string FormatIdle(decimal value, NumberFormatOptions options)
        {
            bool isNegative = value < 0m;
            options.RoundingMode = ForMagnitude(options.RoundingMode, isNegative);
            decimal abs = Math.Abs(value);
            decimal roundedInit = Round(abs, options.Decimals, options.RoundingMode);
            if (roundedInit < 1000m)
            {
                return (value < 0m ? "-" : string.Empty) + FormatDecimalValue(roundedInit, options.Decimals, options);
            }

            int tier = 0;
            decimal compact = abs;

            while (compact >= 1000m)
            {
                compact /= 1000m;
                tier++;
            }

            compact = Round(compact, options.Decimals, options.RoundingMode);

            while (compact >= 1000m)
            {
                compact /= 1000m;
                tier++;
                compact = Round(compact, options.Decimals, options.RoundingMode);
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
            options.RoundingMode = ForMagnitude(options.RoundingMode, isNegative);
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
            while (abs >= 10m)
            {
                abs /= 10m;
                exponent++;
                abs = Round(abs, options.Decimals, options.RoundingMode);
            }

            string mantissa = FormatDecimalValue(abs, options.Decimals, options);
            return (isNegative ? "-" : string.Empty) + mantissa + "e" + exponent.ToString(Invariant);
        }

        private static string FormatIdle(double value, NumberFormatOptions options)
        {
            bool isNegative = value < 0d;
            options.RoundingMode = ForMagnitude(options.RoundingMode, isNegative);
            double abs = Math.Abs(value);
            double roundedInit = Round(abs, options.Decimals, options.RoundingMode);
            if (roundedInit < 1000d)
            {
                return (value < 0d ? "-" : string.Empty) + NormalizeAndApplySeparators(
                    roundedInit.ToString("F" + options.Decimals, Invariant), options);
            }

            double compact = Math.Abs(value);
            int tier = 0;

            while (compact >= 1000d && tier < int.MaxValue - 2)
            {
                compact /= 1000d;
                tier++;
            }

            compact = Round(compact, options.Decimals, options.RoundingMode);

            while (compact >= 1000d && tier < int.MaxValue - 2)
            {
                compact /= 1000d;
                tier++;
                compact = Round(compact, options.Decimals, options.RoundingMode);
            }

            string numberText = NormalizeAndApplySeparators(
                compact.ToString("F" + options.Decimals, Invariant), options);

            return (isNegative ? "-" : string.Empty) + numberText + GetSuffix(tier);
        }

        private static string FormatScientific(double value, NumberFormatOptions options)
        {
            if (value == 0d)
            {
                return "0";
            }

            // The runtime's exponent conversion also handles subnormal doubles without a
            // power-of-ten divisor underflowing to zero. Retain 17 significant digits to round.
            string scientific = value.ToString("E16", Invariant);
            int exponentIndex = scientific.IndexOf('E');
            decimal mantissa = decimal.Parse(scientific.Substring(0, exponentIndex), Invariant);
            int exponent = int.Parse(scientific.Substring(exponentIndex + 1), Invariant);
            mantissa = Round(mantissa, options.Decimals, options.RoundingMode);
            if (Math.Abs(mantissa) >= 10m)
            {
                mantissa /= 10m;
                exponent++;
            }

            return FormatDecimalValue(mantissa, options.Decimals, options) + "e" +
                   exponent.ToString("+000;-000;+000", Invariant);
        }

        private static NumberRoundingMode ForMagnitude(NumberRoundingMode mode, bool isNegative)
        {
            if (!isNegative)
            {
                return mode;
            }

            return mode switch
            {
                NumberRoundingMode.ToPositiveInfinity => NumberRoundingMode.ToNegativeInfinity,
                NumberRoundingMode.ToNegativeInfinity => NumberRoundingMode.ToPositiveInfinity,
                _ => mode
            };
        }

        private static decimal ReadSignificand(string digits, int leadingDigits, int decimals)
        {
            int neededDigits = Math.Min(digits.Length, leadingDigits + decimals + 1);
            decimal prefix = decimal.Parse(digits.Substring(0, neededDigits), Invariant);
            // A non-zero discarded tail distinguishes an exact midpoint/integer from a value
            // just above it. One sticky digit is sufficient for every supported rounding mode.
            for (int i = neededDigits; i < digits.Length; i++)
            {
                if (digits[i] != '0')
                {
                    prefix += 0.1m;
                    break;
                }
            }

            return prefix / Pow10Decimal(neededDigits - leadingDigits);
        }

        private static string GetSuffix(int tier)
        {
            if (tier < BaseSuffixes.Length)
            {
                return BaseSuffixes[tier];
            }

            int n = tier - BaseSuffixes.Length;
            StringBuilder sb = new();

            do
            {
                sb.Insert(0, Alphabet[n % 26]);
                n = n / 26 - 1;
            } while (n >= 0);

            return sb.ToString();
        }

        private static decimal Round(decimal value, int decimals, NumberRoundingMode mode)
        {
            decimals = NumberFormatOptions.ClampDecimals(decimals);
            decimal factor = Pow10Decimal(decimals);
            decimal integral = Math.Truncate(value);
            decimal fractional = value - integral;

            return mode switch
            {
                NumberRoundingMode.ToEven => Math.Round(value, decimals, MidpointRounding.ToEven),
                NumberRoundingMode.AwayFromZero => Math.Round(value, decimals, MidpointRounding.AwayFromZero),
                NumberRoundingMode.ToZero => integral + Math.Truncate(fractional * factor) / factor,
                NumberRoundingMode.ToPositiveInfinity => integral + Math.Ceiling(fractional * factor) / factor,
                NumberRoundingMode.ToNegativeInfinity => integral + Math.Floor(fractional * factor) / factor,
                _ => Math.Round(value, decimals, MidpointRounding.ToEven)
            };
        }

        private static double Round(double value, int decimals, NumberRoundingMode mode)
        {
            decimals = NumberFormatOptions.ClampDecimals(decimals);
            double factor = Math.Pow(10d, decimals);
            double integral = Math.Truncate(value);
            double scaledFraction = (value - integral) * factor;

            return mode switch
            {
                NumberRoundingMode.ToEven => Math.Round(value, decimals, MidpointRounding.ToEven),
                NumberRoundingMode.AwayFromZero => Math.Round(value, decimals, MidpointRounding.AwayFromZero),
                NumberRoundingMode.ToZero => integral + Math.Truncate(scaledFraction) / factor,
                NumberRoundingMode.ToPositiveInfinity => integral + Math.Ceiling(scaledFraction) / factor,
                NumberRoundingMode.ToNegativeInfinity => integral + Math.Floor(scaledFraction) / factor,
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
            return NormalizeAndApplySeparators(raw, options);
        }

        private static string NormalizeAndApplySeparators(string raw, NumberFormatOptions options)
        {
            // Trim the invariant fraction before inserting user separators. A group separator
            // can contain the decimal separator too, without changing the integer digits.
            NumberFormatOptions invariantOptions = options;
            invariantOptions.DecimalSeparator = ".";
            return ApplySeparators(NormalizeDecimalPart(raw, invariantOptions), options);
        }

        private static string ApplySeparators(string text, NumberFormatOptions options)
        {
            string group = options.GroupSeparator ?? ",";
            string decimalSeparator = options.DecimalSeparator ?? ".";

            if (group == "," && decimalSeparator == ".")
            {
                return text;
            }

            StringBuilder sb = new(text.Length + 8);
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

            int fractionalStart = separatorIndex + decimalSeparator.Length;
            int exponentIndex = options.Notation == NumberNotation.Scientific
                ? text.IndexOf('e', fractionalStart)
                : -1;
            if (exponentIndex < 0 && options.Notation == NumberNotation.Scientific)
            {
                exponentIndex = text.IndexOf('E', fractionalStart);
            }

            int fractionalEnd = exponentIndex > separatorIndex ? exponentIndex : text.Length;
            int end = fractionalEnd;
            while (end > fractionalStart && text[end - 1] == '0')
            {
                end--;
            }

            if (end == fractionalStart)
            {
                end = separatorIndex;
            }

            return text.Substring(0, end) + text.Substring(fractionalEnd);
        }

        private static string ApplyAffixes(string raw, NumberFormatOptions options)
        {
            string prefix = options.Prefix ?? string.Empty;
            string suffix = options.Suffix ?? string.Empty;
            return prefix + raw + suffix;
        }
    }
}
