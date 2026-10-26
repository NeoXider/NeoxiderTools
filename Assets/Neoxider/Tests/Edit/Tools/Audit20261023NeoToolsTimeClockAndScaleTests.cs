using System;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoToolsTimeClockAndScaleTests
    {
        private sealed class FakeClock : INeoClock
        {
            public DateTime UtcNow { get; set; }
            public double MonotonicSeconds { get; set; }
        }

        [Test]
        public void ClockNearMaximumDate_StillAdvancesWithoutOverflow()
        {
            DateTime maximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            FakeClock source = new() { UtcNow = maximum.AddSeconds(-10d) };
            TrustedClock clock = new(source);
            DateTime start = clock.ReadUtcNow();
            source.UtcNow = maximum.AddSeconds(-5d);
            source.MonotonicSeconds = 5d;

            Assert.That(clock.ReadUtcNow(), Is.EqualTo(source.UtcNow));
            Assert.That(clock.SecondsSince(start), Is.EqualTo(5d).Within(0.001d));
            Assert.That(clock.HighWaterUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void LargeMonotonicAdvance_SaturatesCeilingAndPreservesRawUtc()
        {
            FakeClock source = new() { UtcNow = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc) };
            TrustedClock clock = new(source);
            DateTime start = clock.ReadUtcNow();
            source.MonotonicSeconds = double.MaxValue;
            source.UtcNow = start.AddSeconds(10d);

            Assert.That(clock.ReadUtcNow(), Is.EqualTo(source.UtcNow));
            Assert.That(clock.LastReadWasJumpedForward, Is.False);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteTimeScale_PreservesTheLastValidGameSpeed(float invalidScale)
        {
            float previousScale = Time.timeScale;
            GameObject gameObject = new(nameof(NonFiniteTimeScale_PreservesTheLastValidGameSpeed));
            try
            {
                GameTimeController controller = gameObject.AddComponent<GameTimeController>();
                controller.SetTimeScale(0.5f);

                controller.SetTimeScale(invalidScale);

                Assert.That(Time.timeScale, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Time.timeScale = previousScale;
            }
        }
    }
}
