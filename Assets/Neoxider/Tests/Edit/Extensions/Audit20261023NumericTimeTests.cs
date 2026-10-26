using System;
using Neo.Extensions;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NumericTimeTests
    {
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        public void FloatClock_InvalidOrNegativeDurationFormatsAsZero(float value)
        {
            Assert.AreEqual("00:00:000", value.FormatTime(TimeFormat.MinutesSecondsMilliseconds));
        }

        [TestCase(float.MaxValue)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(2147483648f)]
        public void FloatClock_OverflowingDurationSaturatesBeforeIntegerConversion(float value)
        {
            Assert.AreEqual("2147483647", value.FormatTime(TimeFormat.Seconds));
            Assert.AreEqual("2147483647:00", value.FormatTime(TimeFormat.SecondsMilliseconds));
            Assert.AreEqual("24855:03:14:07", value.FormatTime(TimeFormat.DaysHoursMinutesSeconds));
            Assert.AreEqual("24855::03::14::07", value.FormatTime(TimeFormat.DaysHoursMinutesSeconds, "::", true));
        }

        [Test]
        public void MinimumTimeSpan_FormatsAllCompactAndClockEntryPoints()
        {
            Assert.AreEqual("-10675199d 02h 48m 05s", TimeSpan.MinValue.ToCompactString(true, 4));
            Assert.AreEqual("-256204778:48:05", TimeSpan.MinValue.ToClockString());
            Assert.AreEqual("-10675199:02:48:05", TimeSpan.MinValue.ToClockString(true));
        }

        [TestCase(TimeFormat.Milliseconds, "922337203685477ms")]
        [TestCase(TimeFormat.SecondsMilliseconds, "922337203685s 477ms")]
        [TestCase(TimeFormat.Seconds, "922337203685s")]
        [TestCase(TimeFormat.Minutes, "15372286728m")]
        [TestCase(TimeFormat.MinutesSeconds, "15372286728m 05s")]
        [TestCase(TimeFormat.MinutesSecondsMilliseconds, "15372286728m 05s 477ms")]
        [TestCase(TimeFormat.Hours, "256204778h")]
        [TestCase(TimeFormat.HoursMinutes, "256204778h 48m")]
        [TestCase(TimeFormat.HoursMinutesSeconds, "256204778h 48m 05s")]
        [TestCase(TimeFormat.Days, "10675199d")]
        [TestCase(TimeFormat.DaysHours, "10675199d 02h")]
        [TestCase(TimeFormat.DaysHoursMinutes, "10675199d 02h 48m")]
        [TestCase(TimeFormat.DaysHoursMinutesSeconds, "10675199d 02h 48m 05s")]
        public void TimeSpanExtremes_UseExactWholeUnitsWithoutIntegerOverflow(TimeFormat format, string expected)
        {
            Assert.AreEqual(expected, TimeSpan.MaxValue.ToCompactString(format));
            Assert.AreEqual("-" + expected, TimeSpan.MinValue.ToCompactString(format));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(0f)]
        [TestCase(-1f)]
        public void ClaimAdvance_InvalidCooldownPreservesUtcTimestamp(float cooldown)
        {
            DateTime last = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            DateTime result = last.AdvanceLastClaimTime(1, cooldown);
            Assert.AreEqual(last.Ticks, result.Ticks);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        }

        [Test]
        public void ClaimAdvance_CountBeyondFloatPrecisionPreservesEveryClaim()
        {
            DateTime last = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime result = last.AdvanceLastClaimTime(16777217, 1f);
            Assert.AreEqual(last.AddTicks(16777217L * TimeSpan.TicksPerSecond), result);
        }

        [Test]
        public void ClaimAdvance_UsesTicksForRepresentableSubMillisecondDuration()
        {
            DateTime last = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(last.AddTicks(152), last.AdvanceLastClaimTime(1, 1f / 65536f));
        }

        [TestCase(float.MaxValue)]
        [TestCase(1f)]
        public void ClaimAdvance_ExceedingDateRangeSaturatesInUtc(float cooldown)
        {
            DateTime last = DateTime.SpecifyKind(DateTime.MaxValue.AddTicks(-1), DateTimeKind.Utc);
            DateTime result = last.AdvanceLastClaimTime(int.MaxValue, cooldown);
            Assert.AreEqual(DateTime.MaxValue.Ticks, result.Ticks);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        }
    }
}
