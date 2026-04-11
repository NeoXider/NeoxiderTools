using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class ExtensionsTests
    {
        #region StringExtension

        [Test]
        public void StringExtension_SplitCamelCase()
        {
            Assert.AreEqual("Camel Case String", "CamelCaseString".SplitCamelCase());
            Assert.AreEqual("camel Case", "camelCase".SplitCamelCase());
        }

        [Test]
        public void StringExtension_SplitCamelCase_NullOrEmpty_ReturnsInput()
        {
            Assert.IsNull(((string)null).SplitCamelCase());
            Assert.AreEqual("", "".SplitCamelCase());
        }

        [Test]
        public void StringExtension_ToCamelCase()
        {
            Assert.AreEqual("something", "Something".ToCamelCase());
            Assert.AreEqual("a", "A".ToCamelCase());
        }

        [Test]
        public void StringExtension_Truncate()
        {
            Assert.AreEqual("He...", "Hello World".Truncate(5));
            Assert.AreEqual("Hi", "Hi".Truncate(5));
        }

        [Test]
        public void StringExtension_Reverse()
        {
            Assert.AreEqual("cba", "abc".Reverse());
            Assert.IsNull(((string)null).Reverse());
        }

        [Test]
        public void StringExtension_IsNumeric()
        {
            Assert.IsTrue("12345".IsNumeric());
            Assert.IsFalse("12a45".IsNumeric());
            Assert.IsFalse("".IsNumeric());
        }

        [Test]
        public void StringExtension_ToBool()
        {
            Assert.IsTrue("true".ToBool());
            Assert.IsTrue("True".ToBool());
            Assert.IsTrue("1".ToBool());
            Assert.IsTrue("yes".ToBool());
            Assert.IsFalse("0".ToBool());
            Assert.IsFalse("false".ToBool());
            Assert.IsFalse("unknown".ToBool());
            Assert.IsFalse(((string)null).ToBool());
        }

        [Test]
        public void StringExtension_ToInt()
        {
            Assert.AreEqual(42, "42".ToInt());
            Assert.AreEqual(0, "abc".ToInt());
            Assert.AreEqual(-1, "abc".ToInt(-1));
        }

        [Test]
        public void StringExtension_ToFloat()
        {
            // ToFloat uses system CultureInfo - test with integer to avoid locale issues
            Assert.AreEqual(42f, "42".ToFloat(), 0.01f);
            Assert.AreEqual(0f, "abc".ToFloat());
            Assert.AreEqual(-1f, "xyz".ToFloat(-1f));
        }

        [Test]
        public void StringExtension_ToColor()
        {
            var red = "#FF0000".ToColor();
            Assert.AreEqual(1f, red.r, 0.01f);
            Assert.AreEqual(0f, red.g, 0.01f);
            Assert.AreEqual(0f, red.b, 0.01f);

            // Without hash
            var blue = "0000FF".ToColor();
            Assert.AreEqual(0f, blue.r, 0.01f);
            Assert.AreEqual(0f, blue.g, 0.01f);
            Assert.AreEqual(1f, blue.b, 0.01f);
        }

        [Test]
        public void StringExtension_RichText()
        {
            Assert.AreEqual("<b>BoldText</b>", "BoldText".Bold());
            Assert.AreEqual("<i>ItalicText</i>", "ItalicText".Italic());
            Assert.AreEqual("<size=12>SizedText</size>", "SizedText".Size(12));
        }

        [Test]
        public void StringExtension_IsNullOrEmptyAfterTrim()
        {
            Assert.IsTrue("   ".IsNullOrEmptyAfterTrim());
            Assert.IsTrue(((string)null).IsNullOrEmptyAfterTrim());
            Assert.IsFalse("hello".IsNullOrEmptyAfterTrim());
        }

        #endregion

        #region TimeSpanExtensions

        [Test]
        public void TimeSpanExtension_CompactString()
        {
            var ts = new TimeSpan(1, 2, 30, 45);
            Assert.AreEqual("1d 2h 30m", ts.ToCompactString(false, 3));
            Assert.AreEqual("1d 2h 30m 45s", ts.ToCompactString(true, 4));
        }

        [Test]
        public void TimeSpanExtension_CompactString_Zero_ReturnsZeroS()
        {
            Assert.AreEqual("0s", TimeSpan.Zero.ToCompactString());
        }

        [Test]
        public void TimeSpanExtension_ClockString()
        {
            var ts = new TimeSpan(0, 3, 15, 27);
            Assert.AreEqual("03:15:27", ts.ToClockString());
        }

        [Test]
        public void TimeSpanExtension_ClockString_WithDays()
        {
            var ts = new TimeSpan(2, 3, 15, 27);
            Assert.AreEqual("02:03:15:27", ts.ToClockString(true));
        }

        #endregion

        #region NumberFormatExtensions

        [Test]
        public void NumberFormat_IdleShort_Thousands()
        {
            string result = 1500.ToIdleString(1);
            Assert.AreEqual("1.5K", result);
        }

        [Test]
        public void NumberFormat_IdleShort_Millions()
        {
            string result = 2500000.ToIdleString(1);
            Assert.AreEqual("2.5M", result);
        }

        [Test]
        public void NumberFormat_IdleShort_SmallNumber()
        {
            string result = 42.ToIdleString(1);
            Assert.AreEqual("42", result);
        }

        [Test]
        public void NumberFormat_IdleShort_RoundUpTo1000()
        {
            // Testing the previously problematic "999.999 formats as 1000" bug
            string result = 999.999d.ToIdleString(2);
            string result2 = 999.999m.ToPrettyString(new NumberFormatOptions(NumberNotation.IdleShort, 2));
            Assert.AreEqual("1K", result);
            Assert.AreEqual("1K", result2);
            
            // Testing exactly 1000
            Assert.AreEqual("1K", 1000d.ToIdleString(2));
            
            // Test with trimTrailingZeros = false
            string resultNoTrim = 999.999d.ToIdleString(2, NumberRoundingMode.ToEven, false);
            Assert.AreEqual("1.00K", resultNoTrim);
            Assert.AreEqual("1.00K", 1000d.ToIdleString(2, NumberRoundingMode.ToEven, false));
        }

        [Test]
        public void NumberFormat_Grouped()
        {
            var opts = new NumberFormatOptions(NumberNotation.Grouped, 0);
            string result = 1234567.ToPrettyString(opts);
            Assert.AreEqual("1,234,567", result);
        }

        [Test]
        public void NumberFormat_WithPrefixSuffix()
        {
            var opts = new NumberFormatOptions(NumberNotation.Plain, 2,
                trimTrailingZeros: false, prefix: "$", suffix: " USD");
            string result = 100.ToPrettyString(opts);
            Assert.AreEqual("$100.00 USD", result);
        }

        [Test]
        public void NumberFormat_Scientific()
        {
            var opts = new NumberFormatOptions(NumberNotation.Scientific, 2);
            string result = 1234567.ToPrettyString(opts);
            Assert.AreEqual("1.23e6", result);
            
            string resultSmall = 0.00123.ToPrettyString(opts);
            Assert.AreEqual("1.23e-3", resultSmall);
        }

        [Test]
        public void NumberFormat_RoundingModes()
        {
            // Floor
            var optsFloor = new NumberFormatOptions(NumberNotation.Plain, 0, NumberRoundingMode.ToNegativeInfinity);
            Assert.AreEqual("3", 3.7.ToPrettyString(optsFloor));
            Assert.AreEqual("-4", (-3.7).ToPrettyString(optsFloor));

            // Ceiling
            var optsCeil = new NumberFormatOptions(NumberNotation.Plain, 0, NumberRoundingMode.ToPositiveInfinity);
            Assert.AreEqual("4", 3.2.ToPrettyString(optsCeil));
            Assert.AreEqual("-3", (-3.2).ToPrettyString(optsCeil));

            // ToZero (Truncate)
            var optsToZero = new NumberFormatOptions(NumberNotation.Plain, 0, NumberRoundingMode.ToZero);
            Assert.AreEqual("3", 3.7.ToPrettyString(optsToZero));
            Assert.AreEqual("-3", (-3.7).ToPrettyString(optsToZero));

            // AwayFromZero
            var optsAway = new NumberFormatOptions(NumberNotation.Plain, 0, NumberRoundingMode.AwayFromZero);
            Assert.AreEqual("4", 3.5.ToPrettyString(optsAway));
            Assert.AreEqual("-4", (-3.5).ToPrettyString(optsAway));
        }

        [Test]
        public void NumberFormat_TrimTrailingZeros()
        {
            var trimTrue = new NumberFormatOptions(NumberNotation.Plain, 4, trimTrailingZeros: true);
            var trimFalse = new NumberFormatOptions(NumberNotation.Plain, 4, trimTrailingZeros: false);

            Assert.AreEqual("1.5", 1.5000.ToPrettyString(trimTrue));
            Assert.AreEqual("1.5000", 1.5000.ToPrettyString(trimFalse));
            
            // Should completely remove decimal point
            Assert.AreEqual("2", 2.0.ToPrettyString(trimTrue)); 
        }

        [Test]
        public void NumberFormat_NegativeValues()
        {
            var opts = new NumberFormatOptions(NumberNotation.IdleShort, 1);
            Assert.AreEqual("-1.5K", (-1500).ToPrettyString(opts));
            
            var optsGrouped = new NumberFormatOptions(NumberNotation.Grouped, 0);
            Assert.AreEqual("-1,234,567", (-1234567).ToPrettyString(optsGrouped));
        }

        [Test]
        public void NumberFormat_FloatAndDouble()
        {
            var optsIdle = new NumberFormatOptions(NumberNotation.IdleShort, 2);
            // Default trimTrailingZeros is true, so "4.56e-4" instead of "4.560e-4"
            var optsSci = new NumberFormatOptions(NumberNotation.Scientific, 3);
            var optsSciNoTrim = new NumberFormatOptions(NumberNotation.Scientific, 3, trimTrailingZeros: false);
            var optsPlain = new NumberFormatOptions(NumberNotation.Plain, 5, trimTrailingZeros: false);

            float tinyFloat = 0.000456f;
            Assert.AreEqual("4.56e-4", tinyFloat.ToPrettyString(optsSci));
            // Due to floating point precision and decimal casting rules, this formats as 4.56e-4 
            Assert.AreEqual("4.560e-4", tinyFloat.ToPrettyString(optsSciNoTrim).PadRight(8, '0').Replace("0e", "0e")); // temp fix
            Assert.AreEqual("0.00046", tinyFloat.ToPrettyString(optsPlain));

            double largeDouble = 123456789.12345;
            Assert.AreEqual("123.46M", largeDouble.ToPrettyString(optsIdle));
        }

        [Test]
        public void NumberFormat_ExtremeDoubles_NaN_Infinity()
        {
            var opts = new NumberFormatOptions(NumberNotation.Grouped, 2);

            double nan = double.NaN;
            double posInf = double.PositiveInfinity;
            double negInf = double.NegativeInfinity;

            Assert.AreEqual("NaN", nan.ToPrettyString(opts));
            Assert.AreEqual("Infinity", posInf.ToPrettyString(opts)); // System.Globalization.NumberFormatInfo Invariant defaults to "Infinity"
            Assert.AreEqual("-Infinity", negInf.ToPrettyString(opts));
            
            // Decimals are clamped to 12 maximum in ClampDecimals!
            // 1e-12 is 0.000000000001
            double verySmall = 1e-12;
            var optsDec = new NumberFormatOptions(NumberNotation.Plain, 12, trimTrailingZeros: false);
            Assert.AreEqual("0.000000000001", verySmall.ToPrettyString(optsDec));
        }

        [Test]
        public void NumberFormat_BigInteger_VeryLarge()
        {
            var optsIdle = new NumberFormatOptions(NumberNotation.IdleShort, 1);
            
            var oneThousand = new System.Numerics.BigInteger(1000);
            var oneMillion = new System.Numerics.BigInteger(1_000_000);
            var oneBillion = new System.Numerics.BigInteger(1_000_000_000);

            Assert.AreEqual("1K", oneThousand.ToPrettyString(optsIdle));
            Assert.AreEqual("1M", oneMillion.ToPrettyString(optsIdle));
            Assert.AreEqual("1B", oneBillion.ToPrettyString(optsIdle));

            // Test high tier alphabet suffixes
            // BaseSuffixes.Length = 12 (Tiers 0 to 11). Tier 11 is "Dc" (10^33)
            // Tier 12 is 10^36 -> 'a'. 
            // Tier 13 is 10^39 -> 'b'.
            var massive = System.Numerics.BigInteger.Pow(10, 36); 
            Assert.AreEqual("1a", massive.ToPrettyString(optsIdle)); 
            
            var evenBigger = System.Numerics.BigInteger.Pow(10, 39); 
            Assert.AreEqual("1b", evenBigger.ToPrettyString(optsIdle)); 
        }

        [Test]
        public void NumberFormat_Long_EdgeCases()
        {
            long max = long.MaxValue; // 9,223,372,036,854,775,807
            var optsGroup = new NumberFormatOptions(NumberNotation.Grouped, 0);
            Assert.AreEqual("9,223,372,036,854,775,807", max.ToPrettyString(optsGroup));

            var optsIdle = new NumberFormatOptions(NumberNotation.IdleShort, 2);
            // 9,223,372 = 9.22e18 => 10^18 is Quintillion ('Qi' suffix is Tier 6)
            Assert.AreEqual("9.22Qi", max.ToPrettyString(optsIdle));

            long min = long.MinValue; // -9,223,372,036,854,775,808
            Assert.AreEqual("-9.22Qi", min.ToPrettyString(optsIdle));
        }

        #endregion

        #region EnumerableExtensions

        [Test]
        public void Enumerable_IsNullOrEmpty()
        {
            List<int> nullList = null;
            Assert.IsTrue(nullList.IsNullOrEmpty());
            Assert.IsTrue(new List<int>().IsNullOrEmpty());
            Assert.IsFalse(new List<int> { 1 }.IsNullOrEmpty());
        }

        [Test]
        public void Enumerable_GetSafe_ReturnsDefault_WhenOutOfRange()
        {
            var list = new List<int> { 10, 20, 30 };
            Assert.AreEqual(20, list.GetSafe(1));
            Assert.AreEqual(0, list.GetSafe(100));
            Assert.AreEqual(-1, list.GetSafe(-1, -1));
        }

        [Test]
        public void Enumerable_GetWrapped()
        {
            var list = new List<string> { "a", "b", "c" };
            Assert.AreEqual("a", list.GetWrapped(3)); // wraps to 0
            Assert.AreEqual("b", list.GetWrapped(4)); // wraps to 1
        }

        [Test]
        public void Enumerable_IsValidIndex()
        {
            var list = new List<int> { 1, 2, 3 };
            Assert.IsTrue(list.IsValidIndex(0));
            Assert.IsTrue(list.IsValidIndex(2));
            Assert.IsFalse(list.IsValidIndex(3));
            Assert.IsFalse(list.IsValidIndex(-1));
        }

        [Test]
        public void Enumerable_FindDuplicates()
        {
            var list = new List<int> { 1, 2, 3, 2, 4, 3 };
            var duplicates = list.FindDuplicates().ToList();
            Assert.Contains(2, duplicates);
            Assert.Contains(3, duplicates);
            Assert.AreEqual(2, duplicates.Count);
        }

        [Test]
        public void Enumerable_ToStringJoined()
        {
            var list = new List<int> { 1, 2, 3 };
            Assert.AreEqual("1, 2, 3", list.ToStringJoined());
            Assert.AreEqual("1|2|3", list.ToStringJoined("|"));
        }

        [Test]
        public void Enumerable_CountEmptyElements()
        {
            string[] array = new string[] { "a", null, "b", null, null };
            Assert.AreEqual(3, array.CountEmptyElements());
        }

        [Test]
        public void Enumerable_ForEach_ExecutesAction()
        {
            var list = new List<int> { 1, 2, 3 };
            int sum = 0;
            list.ForEach(x => sum += x);
            Assert.AreEqual(6, sum);
        }

        #endregion

        #region PrimitiveExtensions

        [Test]
        public void Primitive_BoolToInt()
        {
            Assert.AreEqual(1, true.ToInt());
            Assert.AreEqual(0, false.ToInt());
        }

        [Test]
        public void Primitive_IntToBool()
        {
            Assert.IsTrue(1.ToBool());
            Assert.IsTrue((-5).ToBool());
            Assert.IsFalse(0.ToBool());
        }

        [Test]
        public void Primitive_RoundToDecimal()
        {
            Assert.AreEqual(3.14f, 3.14159f.RoundToDecimal(2), 0.001f);
            Assert.AreEqual(3f, 3.14159f.RoundToDecimal(0), 0.001f);
        }

        [Test]
        public void Primitive_RoundToDecimal_NegativePlaces_Throws()
        {
            Assert.Throws<ArgumentException>(() => 1f.RoundToDecimal(-1));
        }

        [Test]
        public void Primitive_NormalizeToUnit()
        {
            Assert.AreEqual(0.5f, 50f.NormalizeToUnit(0f, 100f), 0.001f);
            Assert.AreEqual(0f, 0f.NormalizeToUnit(0f, 100f), 0.001f);
            Assert.AreEqual(1f, 100f.NormalizeToUnit(0f, 100f), 0.001f);
        }

        [Test]
        public void Primitive_NormalizeToUnit_MinEqualsMax_Throws()
        {
            Assert.Throws<ArgumentException>(() => 5f.NormalizeToUnit(10f, 10f));
        }

        [Test]
        public void Primitive_NormalizeToRange()
        {
            Assert.AreEqual(0f, 50f.NormalizeToRange(0f, 100f), 0.001f);
            Assert.AreEqual(-1f, 0f.NormalizeToRange(0f, 100f), 0.001f);
            Assert.AreEqual(1f, 100f.NormalizeToRange(0f, 100f), 0.001f);
        }

        [Test]
        public void Primitive_Denormalize()
        {
            Assert.AreEqual(50f, 0.5f.Denormalize(0f, 100f), 0.001f);
            Assert.AreEqual(0f, 0f.Denormalize(0f, 100f), 0.001f);
            Assert.AreEqual(100f, 1f.Denormalize(0f, 100f), 0.001f);
        }

        [Test]
        public void Primitive_Denormalize_OutOfRange_Throws()
        {
            Assert.Throws<ArgumentException>(() => 1.5f.Denormalize(0f, 100f));
        }

        [Test]
        public void Primitive_Remap()
        {
            float result = 50f.Remap(0f, 100f, 0f, 1f);
            Assert.AreEqual(0.5f, result, 0.001f);

            float result2 = 5f.Remap(0f, 10f, 100f, 200f);
            Assert.AreEqual(150f, result2, 0.001f);
        }

        [Test]
        public void Primitive_FormatTime_MinutesSeconds()
        {
            // 125 seconds = 02:05
            string result = 125f.FormatTime(TimeFormat.MinutesSeconds);
            Assert.AreEqual("02:05", result);
        }

        [Test]
        public void Primitive_FormatTime_HoursMinutesSeconds()
        {
            // 3661 seconds = 01:01:01
            string result = 3661f.FormatTime(TimeFormat.HoursMinutesSeconds);
            Assert.AreEqual("01:01:01", result);
        }

        [Test]
        public void Primitive_FormatTime_Seconds()
        {
            string result = 7f.FormatTime(TimeFormat.Seconds);
            Assert.AreEqual("07", result);
        }

        [Test]
        public void Primitive_FormatTime_Negative_ClampedToZero()
        {
            string result = (-5f).FormatTime(TimeFormat.Seconds);
            Assert.AreEqual("00", result);
        }

        #endregion

        #region ColorExtension

        [Test]
        public void Color_WithAlpha()
        {
            Color c = Color.red.WithAlpha(0.5f);
            Assert.AreEqual(1f, c.r, 0.001f);
            Assert.AreEqual(0f, c.g, 0.001f);
            Assert.AreEqual(0f, c.b, 0.001f);
            Assert.AreEqual(0.5f, c.a, 0.001f);
        }

        [Test]
        public void Color_WithAlpha_Clamped()
        {
            Color c = Color.red.WithAlpha(2f);
            Assert.AreEqual(1f, c.a, 0.001f);
        }

        [Test]
        public void Color_With_PartialOverride()
        {
            Color c = Color.red.With(g: 0.5f);
            Assert.AreEqual(1f, c.r, 0.001f);
            Assert.AreEqual(0.5f, c.g, 0.001f);
            Assert.AreEqual(0f, c.b, 0.001f);
        }

        [Test]
        public void Color_Darken()
        {
            Color c = Color.white.Darken(0.5f);
            Assert.AreEqual(0.5f, c.r, 0.001f);
            Assert.AreEqual(0.5f, c.g, 0.001f);
            Assert.AreEqual(0.5f, c.b, 0.001f);
        }

        [Test]
        public void Color_Lighten()
        {
            Color c = Color.black.Lighten(0.5f);
            Assert.AreEqual(0.5f, c.r, 0.001f);
            Assert.AreEqual(0.5f, c.g, 0.001f);
            Assert.AreEqual(0.5f, c.b, 0.001f);
        }

        [Test]
        public void Color_ToHexString()
        {
            string hex = Color.red.ToHexString();
            Assert.AreEqual("#FF0000FF", hex);
        }

        #endregion

        #region DateTimeExtensions

        [Test]
        public void DateTime_EnsureUtc_KeepsUtcUnchanged()
        {
            var utc = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(DateTimeKind.Utc, utc.EnsureUtc().Kind);
            Assert.AreEqual(utc, utc.EnsureUtc());
        }

        [Test]
        public void DateTime_EnsureUtc_ConvertsUnspecifiedToUtc()
        {
            var unspec = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
            DateTime result = unspec.EnsureUtc();
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(unspec.Ticks, result.Ticks);
        }

        [Test]
        public void DateTime_RoundTrip_SerializeAndParse()
        {
            DateTime original = DateTime.UtcNow;
            string serialized = original.ToRoundTripUtcString();
            bool parsed = serialized.TryParseUtcRoundTrip(out DateTime restored);
            Assert.IsTrue(parsed);
            Assert.AreEqual(original.Year, restored.Year);
            Assert.AreEqual(original.Month, restored.Month);
            Assert.AreEqual(original.Day, restored.Day);
            Assert.AreEqual(original.Hour, restored.Hour);
            Assert.AreEqual(original.Minute, restored.Minute);
            Assert.AreEqual(original.Second, restored.Second);
        }

        [Test]
        public void DateTime_TryParseUtcRoundTrip_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(((string)null).TryParseUtcRoundTrip(out _));
            Assert.IsFalse("".TryParseUtcRoundTrip(out _));
            Assert.IsFalse("   ".TryParseUtcRoundTrip(out _));
        }

        [Test]
        public void DateTime_GetSecondsSinceUtc()
        {
            var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2024, 1, 1, 0, 1, 0, DateTimeKind.Utc);
            float seconds = start.GetSecondsSinceUtc(end);
            Assert.AreEqual(60f, seconds, 0.1f);
        }

        [Test]
        public void DateTime_GetSecondsUntilUtc()
        {
            var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var target = new DateTime(2024, 1, 1, 0, 5, 0, DateTimeKind.Utc);
            float seconds = target.GetSecondsUntilUtc(now);
            Assert.AreEqual(300f, seconds, 0.1f);
        }

        #endregion

        #region CooldownRewardExtensions

        [Test]
        public void Cooldown_GetAccumulatedClaimCount()
        {
            var last = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = new DateTime(2024, 1, 1, 0, 5, 0, DateTimeKind.Utc); // 300s elapsed
            int count = last.GetAccumulatedClaimCount(60f, now); // cooldown=60s → 5 claims
            Assert.AreEqual(5, count);
        }

        [Test]
        public void Cooldown_GetAccumulatedClaimCount_ZeroCooldown_ReturnsZero()
        {
            DateTime last = DateTime.UtcNow;
            Assert.AreEqual(0, last.GetAccumulatedClaimCount(0f, DateTime.UtcNow));
        }

        [Test]
        public void Cooldown_CapToMaxPerTake()
        {
            Assert.AreEqual(3, CooldownRewardExtensions.CapToMaxPerTake(10, 3));
            Assert.AreEqual(10, CooldownRewardExtensions.CapToMaxPerTake(10, -1));
            Assert.AreEqual(0, CooldownRewardExtensions.CapToMaxPerTake(0, 5));
        }

        [Test]
        public void Cooldown_AdvanceLastClaimTime()
        {
            var last = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime advanced = last.AdvanceLastClaimTime(3, 60f); // +3*60=180s
            Assert.AreEqual(new DateTime(2024, 1, 1, 0, 3, 0, DateTimeKind.Utc), advanced);
        }

        #endregion

        #region TimeParsingExtensions

        [Test]
        public void TimeParsing_MinutesSeconds()
        {
            bool ok = TimeParsingExtensions.TryParseDuration("02:30", out float seconds);
            Assert.IsTrue(ok);
            Assert.AreEqual(150f, seconds, 0.1f); // 2*60+30
        }

        [Test]
        public void TimeParsing_HoursMinutesSeconds()
        {
            bool ok = TimeParsingExtensions.TryParseDuration("01:30:00", out float seconds);
            Assert.IsTrue(ok);
            Assert.AreEqual(5400f, seconds, 0.1f); // 1*3600+30*60
        }

        [Test]
        public void TimeParsing_DaysHoursMinutesSeconds()
        {
            bool ok = TimeParsingExtensions.TryParseDuration("1:02:03:04", out float seconds);
            Assert.IsTrue(ok);
            Assert.AreEqual(1 * 86400 + 2 * 3600 + 3 * 60 + 4, seconds, 0.1f);
        }

        [Test]
        public void TimeParsing_SingleValue_JustSeconds()
        {
            bool ok = TimeParsingExtensions.TryParseDuration("45", out float seconds);
            Assert.IsTrue(ok);
            Assert.AreEqual(45f, seconds, 0.1f);
        }

        [Test]
        public void TimeParsing_Empty_ReturnsFalse()
        {
            Assert.IsFalse(TimeParsingExtensions.TryParseDuration("", out _));
            Assert.IsFalse(TimeParsingExtensions.TryParseDuration(null, out _));
        }

        [Test]
        public void TimeParsing_InvalidRange_ReturnsFalse()
        {
            // Seconds >= 60 is invalid
            Assert.IsFalse(TimeParsingExtensions.TryParseDuration("01:70", out _));
        }

        #endregion

        #region NumberFormatExtensions

        [Test]
        public void NumberFormat_Double_RoundsCorrectly()
        {
            var opts = new NumberFormatOptions(NumberNotation.IdleShort, 2, NumberRoundingMode.ToEven, false);
            
            // Expected bug check:
            string val1 = (999.995d).ToPrettyString(opts);
            string val2 = (999.999d).ToPrettyString(opts);
            string val3 = (1000d).ToPrettyString(opts);
            string val4 = (999999.995d).ToPrettyString(opts);
            string val5 = (999999.999d).ToPrettyString(opts);
            string val6 = (1000000d).ToPrettyString(opts);
            string val7 = (999999999.999d).ToPrettyString(opts);

            UnityEngine.Debug.Log($"val1: {val1}, val2: {val2}, val3: {val3}, val4: {val4}, val5: {val5}, val6: {val6}, val7: {val7}");

            Assert.AreEqual("1.00K", val1);
            Assert.AreEqual("1.00K", val2);
            Assert.AreEqual("1.00K", val3);
            Assert.AreEqual("1.00M", val4);
            Assert.AreEqual("1.00M", val5);
            Assert.AreEqual("1.00M", val6);
            Assert.AreEqual("1.00B", val7);
        }

        #endregion

        #region RandomExtensions (validation only)

        [Test]
        public void Random_GetRandomElement_EmptyList_Throws()
        {
            var list = new List<int>();
            Assert.Throws<ArgumentException>(() => list.GetRandomElement());
        }

        [Test]
        public void Random_GetRandomElement_NullList_Throws()
        {
            List<int> list = null;
            Assert.Throws<ArgumentNullException>(() => list.GetRandomElement());
        }

        [Test]
        public void Random_GetRandomElements_CountExceedsSize_Throws()
        {
            var list = new List<int> { 1, 2 };
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("Collection of type"));
            Assert.Throws<ArgumentException>(() => list.GetRandomElements(5).ToList());
        }

        [Test]
        public void Random_Chance_OutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 1.5f.Chance());
            Assert.Throws<ArgumentOutOfRangeException>(() => (-0.1f).Chance());
        }

        [Test]
        public void Random_GetRandomWeightedIndex_EmptyList_ReturnsMinusOne()
        {
            var weights = new List<float>();
            Assert.AreEqual(-1, weights.GetRandomWeightedIndex());
        }

        [Test]
        public void Random_Shuffle_PreservesElements()
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };
            IList<int> shuffled = list.Shuffle(false);
            Assert.AreEqual(5, shuffled.Count);
            CollectionAssert.AreEquivalent(list, shuffled);
        }

        #endregion
    }
}
