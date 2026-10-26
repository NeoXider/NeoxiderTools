using System;
using System.Globalization;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoToolsTimeClockRestoreTests
    {
        private sealed class FixedClock : INeoClock
        {
            public DateTime UtcNow => new(2035, 4, 5, 12, 0, 0, DateTimeKind.Utc);
            public double MonotonicSeconds => 0d;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NullClock_RestoresPersistedHighWaterForEitherMarkFlag(bool markRestored)
        {
            using Audit20261023NeoToolsTimePersistenceScope scope = new();
            DateTime persisted = new(2034, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            SaveProvider.SetString(NeoTrustedTime.HighWaterSaveKey, persisted.ToString("o", CultureInfo.InvariantCulture));
            TrustedClock supplied = new(new FixedClock());
            supplied.ReadUtcNow();
            NeoTrustedTime.SetClockForTests(supplied);

            NeoTrustedTime.SetClockForTests(null, markRestored);

            Assert.That(NeoTrustedTime.Clock, Is.Not.SameAs(supplied));
            Assert.That(NeoTrustedTime.Clock.HighWaterUtc, Is.EqualTo(persisted));
            Assert.That(SaveProvider.GetString(NeoTrustedTime.HighWaterSaveKey),
                Is.EqualTo(persisted.ToString("o", CultureInfo.InvariantCulture)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SuppliedClock_PreservesMarkRestoredSemantics(bool markRestored)
        {
            using Audit20261023NeoToolsTimePersistenceScope scope = new();
            DateTime persisted = new(2034, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            SaveProvider.SetString(NeoTrustedTime.HighWaterSaveKey, persisted.ToString("o", CultureInfo.InvariantCulture));
            TrustedClock supplied = new(new FixedClock());

            NeoTrustedTime.SetClockForTests(supplied, markRestored);

            Assert.That(NeoTrustedTime.Clock, Is.SameAs(supplied));
            Assert.That(NeoTrustedTime.Clock.HighWaterUtc,
                Is.EqualTo(markRestored ? DateTime.MinValue : persisted));
        }
    }
}
