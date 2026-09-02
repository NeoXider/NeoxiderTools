using System;
using Neo.Tools;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers <see cref="TrustedClock" />, which protects offline rewards from device clock tampering.
    /// </summary>
    [TestFixture]
    public class TrustedClockTests
    {
        private sealed class FakeClock : INeoClock
        {
            public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            public double MonotonicSeconds { get; set; }

            /// <summary>Advances both clocks together, the way real time passes.</summary>
            public void AdvanceReal(double seconds)
            {
                UtcNow = UtcNow.AddSeconds(seconds);
                MonotonicSeconds += seconds;
            }

            /// <summary>Moves only the wall clock, the way a player editing device settings does.</summary>
            public void TamperWallClock(double seconds)
            {
                UtcNow = UtcNow.AddSeconds(seconds);
            }
        }

        [Test]
        public void NormalProgression_IsReportedAsIs()
        {
            FakeClock fake = new();
            TrustedClock clock = new(fake);
            DateTime start = clock.ReadUtcNow();

            fake.AdvanceReal(3600);

            Assert.AreEqual(3600d, clock.SecondsSince(start), 1d);
            Assert.IsFalse(clock.LastReadWasRewound);
            Assert.IsFalse(clock.LastReadWasJumpedForward);
        }

        [Test]
        public void ForwardJumpWithinSession_IsCappedToRealElapsedTime()
        {
            FakeClock fake = new();
            TrustedClock clock = new(fake);
            DateTime start = clock.ReadUtcNow();

            fake.AdvanceReal(10);
            // The player sets the device clock a full day ahead to skip a cooldown.
            fake.TamperWallClock(TimeSpan.FromDays(1).TotalSeconds);

            double credited = clock.SecondsSince(start);

            Assert.IsTrue(clock.LastReadWasJumpedForward, "the jump should be detected");
            Assert.Less(credited, 200d, "a one-day jump must not be credited as a day of waiting");
        }

        [Test]
        public void RewindingTheClock_GrantsNoTimeAndNeverGoesBackwards()
        {
            FakeClock fake = new();
            TrustedClock clock = new(fake);
            clock.ReadUtcNow();
            fake.AdvanceReal(600);
            DateTime peak = clock.ReadUtcNow();

            fake.TamperWallClock(-TimeSpan.FromDays(2).TotalSeconds);
            DateTime afterRewind = clock.ReadUtcNow();

            Assert.IsTrue(clock.LastReadWasRewound, "the rewind should be detected");
            Assert.AreEqual(peak, afterRewind, "reading time must never move backwards");
        }

        [Test]
        public void RewindThenForward_CannotFarmTheSameCooldownTwice()
        {
            FakeClock fake = new();
            TrustedClock clock = new(fake);
            DateTime claimedAt = clock.ReadUtcNow();

            // Wait out a real hour and claim.
            fake.AdvanceReal(3600);
            Assert.GreaterOrEqual(clock.SecondsSince(claimedAt), 3600d);
            DateTime secondClaimAt = clock.ReadUtcNow();

            // Now rewind and jump forward again, hoping for another free hour.
            fake.TamperWallClock(-3600);
            fake.TamperWallClock(3600);

            Assert.AreEqual(0d, clock.SecondsSince(secondClaimAt), 1d,
                "a rewind followed by a forward jump must not produce a second free hour");
        }

        [Test]
        public void SecondsSince_IsNeverNegative()
        {
            FakeClock fake = new();
            TrustedClock clock = new(fake);
            DateTime future = fake.UtcNow.AddHours(5);

            Assert.AreEqual(0d, clock.SecondsSince(future));
        }

        [Test]
        public void Restore_CarriesTheHighWaterMarkAcrossSessions()
        {
            FakeClock fake = new();
            TrustedClock first = new(fake);
            first.ReadUtcNow();
            fake.AdvanceReal(7200);
            DateTime highWater = first.ReadUtcNow();

            // New session on a device whose clock was moved back while the game was closed.
            FakeClock secondFake = new() { UtcNow = highWater.AddDays(-3) };
            TrustedClock second = new(secondFake);
            second.Restore(highWater);

            DateTime observed = second.ReadUtcNow();

            Assert.AreEqual(highWater, observed,
                "a persisted high-water mark must survive a restart with a rewound clock");
        }

        [Test]
        public void Restore_IgnoresMarksOlderThanTheCurrentOne()
        {
            FakeClock fake = new();
            TrustedClock clock = new(fake);
            fake.AdvanceReal(1000);
            DateTime current = clock.ReadUtcNow();

            clock.Restore(current.AddDays(-1));

            Assert.AreEqual(current, clock.HighWaterUtc);
        }
    }
}
