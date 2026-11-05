using System;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     Integer-only arithmetic of the fixed core, bit-identical on every platform: saturating add / subtract /
    ///     negate, round half away from zero, <see cref="MulDiv" /> with a 128-bit intermediate (no silent overflow),
    ///     integer square root. The rounding rules are the ones of VampireDota's DeterministicMath.
    /// </summary>
    public static class FixedMath
    {
        public static int Abs(int value)
        {
            return value < 0 ? SaturatingNegate(value) : value;
        }

        public static long Abs(long value)
        {
            return value < 0L ? SaturatingNegate(value) : value;
        }

        public static int Min(int left, int right)
        {
            return left < right ? left : right;
        }

        public static int Max(int left, int right)
        {
            return left > right ? left : right;
        }

        public static long Min(long left, long right)
        {
            return left < right ? left : right;
        }

        public static long Max(long left, long right)
        {
            return left > right ? left : right;
        }

        /// <summary>Clamps into [min, max]; a reversed range returns <paramref name="min" />.</summary>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? (max < min ? min : max) : value;
        }

        public static long Clamp(long value, long min, long max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? (max < min ? min : max) : value;
        }

        public static int ClampToInt(long value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            return value < int.MinValue ? int.MinValue : (int)value;
        }

        public static int Sign(long value)
        {
            if (value > 0L)
            {
                return 1;
            }

            return value < 0L ? -1 : 0;
        }

        public static int SaturatingAdd(int left, int right)
        {
            return ClampToInt((long)left + right);
        }

        public static long SaturatingAdd(long left, long right)
        {
            long sum = unchecked(left + right);
            // WHY: an overflow happened exactly when the sum disagrees in sign with both operands.
            if (((left ^ sum) & (right ^ sum)) < 0L)
            {
                return sum < 0L ? long.MaxValue : long.MinValue;
            }

            return sum;
        }

        public static int SaturatingSubtract(int left, int right)
        {
            return ClampToInt((long)left - right);
        }

        public static int SaturatingNegate(int value)
        {
            return value == int.MinValue ? int.MaxValue : -value;
        }

        public static long SaturatingNegate(long value)
        {
            return value == long.MinValue ? long.MaxValue : -value;
        }

        public static int SaturatingMultiply(int left, int right)
        {
            return ClampToInt((long)left * right);
        }

        /// <summary><paramref name="value" /> / <paramref name="divisor" /> rounded half away from zero.</summary>
        public static long DivRoundHalfAwayFromZero(long value, long divisor)
        {
            if (divisor == 0L)
            {
                throw new DivideByZeroException();
            }

            if (divisor == -1L)
            {
                return SaturatingNegate(value);
            }

            long quotient = value / divisor;
            long remainder = value % divisor;
            long remainderMagnitude = Abs(remainder);
            long divisorMagnitude = Abs(divisor);
            if (remainderMagnitude >= divisorMagnitude - remainderMagnitude)
            {
                quotient += (value < 0L) != (divisor < 0L) ? -1L : 1L;
            }

            return quotient;
        }

        public static int DivRoundHalfAwayFromZeroToInt(long value, long divisor)
        {
            return ClampToInt(DivRoundHalfAwayFromZero(value, divisor));
        }

        /// <summary>
        ///     <paramref name="value" /> × <paramref name="multiplier" /> / <paramref name="divisor" /> rounded half away
        ///     from zero. The product is exact (128 bits); a quotient beyond the long range saturates.
        /// </summary>
        public static long MulDiv(long value, long multiplier, long divisor)
        {
            if (divisor == 0L)
            {
                throw new DivideByZeroException();
            }

            // WHY: both factors within ±(2^31 - 1) cannot overflow 63 bits, the common case of raw values.
            if ((ulong)(value + int.MaxValue) <= 2UL * int.MaxValue &&
                (ulong)(multiplier + int.MaxValue) <= 2UL * int.MaxValue)
            {
                return DivRoundHalfAwayFromZero(value * multiplier, divisor);
            }

            bool negative = (value < 0L) ^ (multiplier < 0L) ^ (divisor < 0L);
            ulong high = MulWide(Magnitude(value), Magnitude(multiplier), out ulong low);
            ulong divisorMagnitude = Magnitude(divisor);
            if (high >= divisorMagnitude)
            {
                return negative ? long.MinValue : long.MaxValue;
            }

            ulong quotient = Divide128(high, low, divisorMagnitude, out ulong remainder);
            if (remainder >= divisorMagnitude - remainder)
            {
                quotient++;
            }

            if (quotient > long.MaxValue)
            {
                return negative ? long.MinValue : long.MaxValue;
            }

            return negative ? -(long)quotient : (long)quotient;
        }

        public static int MulDivToInt(long value, long multiplier, long divisor)
        {
            return ClampToInt(MulDiv(value, multiplier, divisor));
        }

        /// <summary>floor(sqrt(value)); 0 for non-positive values.</summary>
        public static long SqrtFloor(long value)
        {
            if (value <= 0L)
            {
                return 0L;
            }

            long remainder = value;
            long root = 0L;
            long bit = 1L << 62;
            while (bit > remainder)
            {
                bit >>= 2;
            }

            while (bit != 0L)
            {
                if (remainder >= root + bit)
                {
                    remainder -= root + bit;
                    root = (root >> 1) + bit;
                }
                else
                {
                    root >>= 1;
                }

                bit >>= 2;
            }

            return root;
        }

        /// <summary>floor(sqrt(value)) of an unsigned value (squared distances).</summary>
        public static ulong SqrtFloor(ulong value)
        {
            if (value <= long.MaxValue)
            {
                return (ulong)SqrtFloor((long)value);
            }

            ulong remainder = value;
            ulong root = 0UL;
            ulong bit = 1UL << 62;
            while (bit != 0UL)
            {
                if (remainder >= root + bit)
                {
                    remainder -= root + bit;
                    root = (root >> 1) + bit;
                }
                else
                {
                    root >>= 1;
                }

                bit >>= 2;
            }

            return root;
        }

        /// <summary>Greatest common divisor of two magnitudes (0 when both are 0).</summary>
        public static long Gcd(long left, long right)
        {
            ulong a = Magnitude(left);
            ulong b = Magnitude(right);
            while (b != 0UL)
            {
                ulong t = a % b;
                a = b;
                b = t;
            }

            return a > long.MaxValue ? long.MaxValue : (long)a;
        }

        /// <summary>Full 64 × 64 → 128-bit unsigned product; returns the high half.</summary>
        public static ulong MulWide(ulong left, ulong right, out ulong low)
        {
            ulong leftLow = left & 0xFFFFFFFFUL;
            ulong leftHigh = left >> 32;
            ulong rightLow = right & 0xFFFFFFFFUL;
            ulong rightHigh = right >> 32;

            ulong lowLow = leftLow * rightLow;
            ulong highLow = leftHigh * rightLow;
            ulong lowHigh = leftLow * rightHigh;
            ulong highHigh = leftHigh * rightHigh;

            ulong cross = (lowLow >> 32) + (highLow & 0xFFFFFFFFUL) + (lowHigh & 0xFFFFFFFFUL);
            low = (cross << 32) | (lowLow & 0xFFFFFFFFUL);
            return highHigh + (highLow >> 32) + (lowHigh >> 32) + (cross >> 32);
        }

        internal static ulong Magnitude(long value)
        {
            return value < 0L ? unchecked((ulong)(-(value + 1L)) + 1UL) : (ulong)value;
        }

        // WHY: shift-subtract division of a 128-bit value whose high half is below the divisor; only the
        // overflow path of MulDiv reaches it.
        private static ulong Divide128(ulong high, ulong low, ulong divisor, out ulong remainder)
        {
            ulong quotient = 0UL;
            ulong rest = high;
            for (int i = 63; i >= 0; i--)
            {
                bool carry = (rest >> 63) != 0UL;
                rest = (rest << 1) | ((low >> i) & 1UL);
                quotient <<= 1;
                if (carry || rest >= divisor)
                {
                    rest = unchecked(rest - divisor);
                    quotient |= 1UL;
                }
            }

            remainder = rest;
            return quotient;
        }
    }

    /// <summary>
    ///     The Q format of a fixed system: <see cref="FractionBits" /> fraction bits, so 1.0 is
    ///     <see cref="One" /> raw (Q8.8: 256). Every amount, pool, property and multiplier of the fixed core is a raw
    ///     <c>int</c> in this format; times are whole ticks.
    /// </summary>
    public readonly struct FixedFormat : IEquatable<FixedFormat>
    {
        /// <summary>Largest supported fraction width; keeps one, products and squares inside 64 bits.</summary>
        public const int MaxFractionBits = 16;

        /// <summary>Q8.8, VampireDota's format.</summary>
        public static readonly FixedFormat Q8 = new FixedFormat(8);

        /// <summary>Q16.16, for content that needs finer multipliers.</summary>
        public static readonly FixedFormat Q16 = new FixedFormat(16);

        public readonly int FractionBits;
        public readonly int One;

        public FixedFormat(int fractionBits)
        {
            if (fractionBits < 0 || fractionBits > MaxFractionBits)
            {
                throw new ArgumentOutOfRangeException(nameof(fractionBits));
            }

            FractionBits = fractionBits;
            One = 1 << fractionBits;
        }

        /// <summary>Raw value of half a unit (0 when there are no fraction bits).</summary>
        public int Half => One >> 1;

        public int FromInt(int value)
        {
            return FixedMath.ClampToInt((long)value << FractionBits);
        }

        /// <summary><paramref name="numerator" /> / <paramref name="denominator" /> as raw, rounded half away from zero.</summary>
        public int FromRatio(long numerator, long denominator)
        {
            return FixedMath.MulDivToInt(numerator, One, denominator);
        }

        /// <summary>Percent (e.g. 25 → 0.25) as raw.</summary>
        public int FromPercent(int percent)
        {
            return FromRatio(percent, 100);
        }

        /// <summary>Product of two raw values, rounded half away from zero.</summary>
        public int Mul(int left, int right)
        {
            return FixedMath.MulDivToInt(left, right, One);
        }

        /// <summary>Quotient of two raw values, rounded half away from zero; a zero divisor saturates.</summary>
        public int Div(int left, int right)
        {
            if (right == 0)
            {
                return left == 0 ? 0 : left > 0 ? int.MaxValue : int.MinValue;
            }

            return FixedMath.MulDivToInt(left, One, right);
        }

        /// <summary>Whole units, rounded half away from zero.</summary>
        public int ToIntRounded(int raw)
        {
            return FixedMath.DivRoundHalfAwayFromZeroToInt(raw, One);
        }

        /// <summary>
        ///     Parses an invariant decimal ("0.5", "-12", "3.125") into raw without floating point, rounded half away
        ///     from zero. False for anything else.
        /// </summary>
        public bool TryParse(string text, out int raw)
        {
            raw = 0;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int index = 0;
            bool negative = false;
            if (text[0] == '-' || text[0] == '+')
            {
                negative = text[0] == '-';
                index = 1;
            }

            long whole = 0L;
            long fraction = 0L;
            long scale = 1L;
            bool digits = false;
            bool point = false;
            for (; index < text.Length; index++)
            {
                char c = text[index];
                if (c == '.' && !point)
                {
                    point = true;
                    continue;
                }

                if (c < '0' || c > '9')
                {
                    return false;
                }

                digits = true;
                if (!point)
                {
                    whole = whole > 100000000L ? whole : whole * 10L + (c - '0');
                }
                else if (scale < 1000000000L)
                {
                    fraction = fraction * 10L + (c - '0');
                    scale *= 10L;
                }
            }

            if (!digits)
            {
                return false;
            }

            long value = FixedMath.SaturatingAdd(whole << FractionBits, FixedMath.MulDiv(fraction, One, scale));
            raw = FixedMath.ClampToInt(negative ? -value : value);
            return true;
        }

        public bool Equals(FixedFormat other)
        {
            return FractionBits == other.FractionBits;
        }

        public override bool Equals(object obj)
        {
            return obj is FixedFormat other && Equals(other);
        }

        public override int GetHashCode()
        {
            return FractionBits;
        }

        public override string ToString()
        {
            return "Q" + FractionBits + " (one = " + One + ")";
        }
    }

    /// <summary>
    ///     A raw fixed-point number: an <c>int</c> whose meaning depends on the <see cref="FixedFormat" /> of the
    ///     system (Q8.8 by default). Addition and comparison need no format; products go through
    ///     <see cref="FixedFormat.Mul" />. The fixed core passes raw ints directly; this wrapper is for code that wants
    ///     the type to say "fixed-point".
    /// </summary>
    public readonly struct FixedValue : IEquatable<FixedValue>, IComparable<FixedValue>
    {
        public readonly int Raw;

        public FixedValue(int raw)
        {
            Raw = raw;
        }

        public static FixedValue FromRaw(int raw)
        {
            return new FixedValue(raw);
        }

        public static FixedValue FromInt(int value, in FixedFormat format)
        {
            return new FixedValue(format.FromInt(value));
        }

        public static FixedValue FromRatio(long numerator, long denominator, in FixedFormat format)
        {
            return new FixedValue(format.FromRatio(numerator, denominator));
        }

        public FixedValue Mul(FixedValue other, in FixedFormat format)
        {
            return new FixedValue(format.Mul(Raw, other.Raw));
        }

        public FixedValue Div(FixedValue other, in FixedFormat format)
        {
            return new FixedValue(format.Div(Raw, other.Raw));
        }

        public static FixedValue operator +(FixedValue left, FixedValue right)
        {
            return new FixedValue(FixedMath.SaturatingAdd(left.Raw, right.Raw));
        }

        public static FixedValue operator -(FixedValue left, FixedValue right)
        {
            return new FixedValue(FixedMath.SaturatingSubtract(left.Raw, right.Raw));
        }

        public static FixedValue operator -(FixedValue value)
        {
            return new FixedValue(FixedMath.SaturatingNegate(value.Raw));
        }

        public static bool operator <(FixedValue left, FixedValue right)
        {
            return left.Raw < right.Raw;
        }

        public static bool operator >(FixedValue left, FixedValue right)
        {
            return left.Raw > right.Raw;
        }

        public static bool operator <=(FixedValue left, FixedValue right)
        {
            return left.Raw <= right.Raw;
        }

        public static bool operator >=(FixedValue left, FixedValue right)
        {
            return left.Raw >= right.Raw;
        }

        public static bool operator ==(FixedValue left, FixedValue right)
        {
            return left.Raw == right.Raw;
        }

        public static bool operator !=(FixedValue left, FixedValue right)
        {
            return left.Raw != right.Raw;
        }

        public bool Equals(FixedValue other)
        {
            return Raw == other.Raw;
        }

        public override bool Equals(object obj)
        {
            return obj is FixedValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Raw;
        }

        public int CompareTo(FixedValue other)
        {
            return Raw.CompareTo(other.Raw);
        }

        public override string ToString()
        {
            return "raw " + Raw;
        }
    }
}
