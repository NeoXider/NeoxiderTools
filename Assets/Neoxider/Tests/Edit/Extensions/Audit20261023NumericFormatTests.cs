using System.Globalization;
using System.Numerics;
using Neo.Extensions;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NumericFormatTests
    {
        [TestCase(NumberNotation.IdleShort, NumberRoundingMode.ToPositiveInfinity, "-1.2K")]
        [TestCase(NumberNotation.IdleShort, NumberRoundingMode.ToNegativeInfinity, "-1.3K")]
        [TestCase(NumberNotation.Scientific, NumberRoundingMode.ToPositiveInfinity, "-1.2e3")]
        [TestCase(NumberNotation.Scientific, NumberRoundingMode.ToNegativeInfinity, "-1.3e3")]
        public void NegativeDirectedRounding_AgreesAcrossNumericTypes(
            NumberNotation notation, NumberRoundingMode mode, string expected)
        {
            NumberFormatOptions options = new(notation, 1, mode);
            Assert.AreEqual(expected, (-1251).ToPrettyString(options));
            Assert.AreEqual(expected, (-1251L).ToPrettyString(options));
            Assert.AreEqual(expected, (-1251f).ToPrettyString(options));
            Assert.AreEqual(expected, (-1251d).ToPrettyString(options));
            Assert.AreEqual(expected, (-1251m).ToPrettyString(options));
            Assert.AreEqual(expected, new BigInteger(-1251).ToPrettyString(options));
        }

        [TestCase(NumberRoundingMode.ToPositiveInfinity, "-1.2K")]
        [TestCase(NumberRoundingMode.ToNegativeInfinity, "-1.3K")]
        public void NegativeIdleConvenienceOverloads_PreserveDirection(NumberRoundingMode mode, string expected)
        {
            Assert.AreEqual(expected, (-1251).ToIdleString(1, mode));
            Assert.AreEqual(expected, (-1251L).ToIdleString(1, mode));
            Assert.AreEqual(expected, new BigInteger(-1251).ToIdleString(1, mode));
        }

        [TestCase(-1.251e100, NumberRoundingMode.ToPositiveInfinity, "-1.2e+100")]
        [TestCase(-1.251e100, NumberRoundingMode.ToNegativeInfinity, "-1.3e+100")]
        [TestCase(1.251e100, NumberRoundingMode.ToZero, "1.2e+100")]
        [TestCase(9.99e100, NumberRoundingMode.ToEven, "1e+101")]
        public void ScientificDoubleBeyondDecimalRange_HonorsRounding(double value, NumberRoundingMode mode,
            string expected)
        {
            NumberFormatOptions options = new(NumberNotation.Scientific, 1, mode);
            Assert.AreEqual(expected, value.ToPrettyString(options));
        }

        [TestCase(1.23e-30, "1.23e-030")]
        [TestCase(-1.23e-100, "-1.23e-100")]
        [TestCase(double.Epsilon, "4.94e-324")]
        public void ScientificTinyDouble_PreservesSignificand(double value, string expected)
        {
            NumberFormatOptions options = new(NumberNotation.Scientific, 2);
            Assert.AreEqual(expected, value.ToPrettyString(options));
        }

        [TestCase(1e-100, NumberRoundingMode.ToPositiveInfinity, "0.001")]
        [TestCase(-1e-100, NumberRoundingMode.ToNegativeInfinity, "-0.001")]
        public void TinyDouble_DirectedRoundingRetainsNonZeroRemainder(double value, NumberRoundingMode mode,
            string expected)
        {
            NumberFormatOptions options = new(NumberNotation.Plain, 3, mode);
            Assert.AreEqual(expected, value.ToPrettyString(options));
        }

        [TestCase(NumberNotation.Scientific, "12501", NumberRoundingMode.ToEven, "1.3e4")]
        [TestCase(NumberNotation.Scientific, "12500", NumberRoundingMode.ToEven, "1.2e4")]
        [TestCase(NumberNotation.Scientific, "10001", NumberRoundingMode.ToPositiveInfinity, "1.1e4")]
        [TestCase(NumberNotation.Scientific, "-10001", NumberRoundingMode.ToNegativeInfinity, "-1.1e4")]
        [TestCase(NumberNotation.IdleShort, "1250001", NumberRoundingMode.ToEven, "1.3M")]
        [TestCase(NumberNotation.IdleShort, "1250000", NumberRoundingMode.ToEven, "1.2M")]
        [TestCase(NumberNotation.IdleShort, "1000001", NumberRoundingMode.ToPositiveInfinity, "1.1M")]
        public void BigInteger_DiscardedTailParticipatesInRounding(NumberNotation notation, string value,
            NumberRoundingMode mode, string expected)
        {
            NumberFormatOptions options = new(notation, 1, mode);
            Assert.AreEqual(expected, BigInteger.Parse(value, CultureInfo.InvariantCulture).ToPrettyString(options));
        }

        [TestCase(NumberRoundingMode.ToZero)]
        [TestCase(NumberRoundingMode.ToPositiveInfinity)]
        [TestCase(NumberRoundingMode.ToNegativeInfinity)]
        public void DecimalLimits_DirectedRoundingDoesNotOverflow(NumberRoundingMode mode)
        {
            NumberFormatOptions options = new(NumberNotation.Plain, 12, mode);
            Assert.AreEqual(decimal.MaxValue.ToString(CultureInfo.InvariantCulture),
                decimal.MaxValue.ToPrettyString(options));
            Assert.AreEqual(decimal.MinValue.ToString(CultureInfo.InvariantCulture),
                decimal.MinValue.ToPrettyString(options));
        }

        [TestCase(NumberRoundingMode.ToZero)]
        [TestCase(NumberRoundingMode.ToPositiveInfinity)]
        [TestCase(NumberRoundingMode.ToNegativeInfinity)]
        public void LargeDouble_DirectedPlainFormattingPreservesValue(NumberRoundingMode mode)
        {
            NumberFormatOptions options = new(NumberNotation.Plain, 12, mode);
            double value = 1e200d;
            double result = double.Parse(value.ToPrettyString(options), CultureInfo.InvariantCulture);
            Assert.AreEqual(value, result);
        }

        [TestCase(true, "1,234.5")]
        [TestCase(false, "1,234.5000")]
        public void GroupedDecimal_HonorsTrailingZeroOption(bool trim, string expected)
        {
            NumberFormatOptions options = new(NumberNotation.Grouped, 4, trimTrailingZeros: trim);
            Assert.AreEqual(expected, 1234.5m.ToPrettyString(options));
        }

        [Test]
        public void GroupSeparatorMatchingDecimalSeparator_DoesNotTrimIntegerDigits()
        {
            NumberFormatOptions options = new(NumberNotation.Grouped, 2, groupSeparator: ".", decimalSeparator: ".");
            Assert.AreEqual("12.000", 12000m.ToPrettyString(options));
            Assert.AreEqual("1.234.5", 1234.5m.ToPrettyString(options));
            options.GroupSeparator = ".0";
            options.DecimalSeparator = "::";
            Assert.AreEqual("1.0234::5", 1234.5m.ToPrettyString(options));
        }

        [TestCase(double.NaN, "[NaN]")]
        [TestCase(double.PositiveInfinity, "[Infinity]")]
        [TestCase(double.NegativeInfinity, "[-Infinity]")]
        public void NonFiniteValues_ApplyAffixesAcrossEntryPoints(double value, string expected)
        {
            NumberFormatOptions options = new(NumberNotation.Plain, prefix: "[", suffix: "]");
            Assert.AreEqual(expected, value.ToPrettyString(options));
            Assert.AreEqual(expected, ((float)value).ToPrettyString(options));
            Assert.AreEqual(expected, NumberFormatExtensions.FormatNumber(value, options));
        }
    }
}
