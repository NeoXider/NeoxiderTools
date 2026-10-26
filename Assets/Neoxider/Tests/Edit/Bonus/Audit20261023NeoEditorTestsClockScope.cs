using System;
using System.Reflection;
using Neo.Tools;

namespace Neo.Editor.Tests
{
    /// <summary>Owns a deterministic reward clock and its temporary save store, preserving lazy clock state.</summary>
    internal sealed class Audit20261023NeoEditorTestsClockScope : IDisposable, INeoClock
    {
        private static readonly FieldInfo ClockField = Field("_clock");
        private static readonly FieldInfo RestoredField = Field("_restored");
        private static readonly FieldInfo HighWaterField = Field("_persistedHighWater");
        private readonly object _previousClock;
        private readonly object _previousRestored;
        private readonly object _previousHighWater;
        private readonly Audit20261023NeoEditorTestsSaveScope _saveScope;
        private bool _disposed;

        public Audit20261023NeoEditorTestsClockScope()
        {
            // Do not read NeoTrustedTime.Clock: its getter restores and mutates the state being borrowed.
            _previousClock = ClockField.GetValue(null);
            _previousRestored = RestoredField.GetValue(null);
            _previousHighWater = HighWaterField.GetValue(null);
            _saveScope = new Audit20261023NeoEditorTestsSaveScope();
            UtcNow = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            NeoTrustedTime.SetClockForTests(new TrustedClock(this));
        }

        public DateTime UtcNow { get; private set; }
        public double MonotonicSeconds { get; private set; }

        public void Advance(double seconds)
        {
            UtcNow = UtcNow.AddSeconds(seconds);
            MonotonicSeconds += seconds;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                ClockField.SetValue(null, _previousClock);
                RestoredField.SetValue(null, _previousRestored);
                HighWaterField.SetValue(null, _previousHighWater);
            }
            finally
            {
                _saveScope.Dispose();
            }
        }

        private static FieldInfo Field(string name)
        {
            return typeof(NeoTrustedTime).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(NeoTrustedTime).FullName, name);
        }
    }
}
