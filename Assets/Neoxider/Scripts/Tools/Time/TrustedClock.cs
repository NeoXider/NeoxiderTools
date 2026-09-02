using System;
using System.Diagnostics;
using Neo.Extensions;
using Neo.Save;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Time source used by <see cref="TrustedClock" />. Split out so tests can drive both the wall clock and the
    ///     monotonic clock without waiting in real time.
    /// </summary>
    public interface INeoClock
    {
        /// <summary>Wall-clock time in UTC. The player can change this from device settings.</summary>
        DateTime UtcNow { get; }

        /// <summary>
        ///     Seconds since the process started, from a source the player cannot change. Only differences within
        ///     one session are meaningful.
        /// </summary>
        double MonotonicSeconds { get; }
    }

    /// <summary>Default <see cref="INeoClock" />: the OS wall clock plus a process-wide stopwatch.</summary>
    public sealed class SystemNeoClock : INeoClock
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public DateTime UtcNow => DateTime.UtcNow;

        public double MonotonicSeconds => _stopwatch.Elapsed.TotalSeconds;
    }

    /// <summary>
    ///     Wall clock hardened against the "set the device clock forward, collect the daily reward, set it back"
    ///     exploit. Plain C# so it can be unit tested with a fake <see cref="INeoClock" />.
    ///     Two rules do the work:
    ///     a high-water mark that never decreases, so moving the clock backwards buys nothing and moving it forward
    ///     must still be waited out afterwards;
    ///     a per-session comparison against a monotonic source, so a forward jump inside a running session is capped
    ///     to the time that actually elapsed.
    ///     Neither rule can replace server-side validation - it only makes the offline exploit unprofitable.
    /// </summary>
    public sealed class TrustedClock
    {
        private readonly INeoClock _clock;
        private DateTime _highWaterUtc = DateTime.MinValue;
        private double _lastMonotonicSeconds;
        private bool _hasSessionAnchor;
        private DateTime _sessionAnchorUtc;

        /// <summary>Creates a clock over the real device time.</summary>
        public TrustedClock() : this(null)
        {
        }

        /// <summary>Creates a clock over an explicit time source. Pass <c>null</c> for the real device time.</summary>
        public TrustedClock(INeoClock clock)
        {
            _clock = clock ?? new SystemNeoClock();
        }

        /// <summary>Highest UTC value ever observed. Persist this between sessions to keep the guarantee.</summary>
        public DateTime HighWaterUtc => _highWaterUtc;

        /// <summary>True when the last <see cref="ReadUtcNow" /> saw the wall clock behind the high-water mark.</summary>
        public bool LastReadWasRewound { get; private set; }

        /// <summary>True when the last <see cref="ReadUtcNow" /> saw the wall clock jump ahead of real elapsed time.</summary>
        public bool LastReadWasJumpedForward { get; private set; }

        /// <summary>Restores the persisted high-water mark. Values older than the current one are ignored.</summary>
        public void Restore(DateTime highWaterUtc)
        {
            if (highWaterUtc > _highWaterUtc)
            {
                _highWaterUtc = DateTime.SpecifyKind(highWaterUtc, DateTimeKind.Utc);
            }
        }

        /// <summary>
        ///     Current UTC time, never earlier than any value returned before and never further ahead than the time
        ///     that really passed during this session.
        /// </summary>
        public DateTime ReadUtcNow()
        {
            DateTime rawUtc = DateTime.SpecifyKind(_clock.UtcNow, DateTimeKind.Utc);
            double monotonicSeconds = _clock.MonotonicSeconds;

            LastReadWasRewound = false;
            LastReadWasJumpedForward = false;

            if (!_hasSessionAnchor)
            {
                _hasSessionAnchor = true;
                _sessionAnchorUtc = rawUtc;
                _lastMonotonicSeconds = monotonicSeconds;
            }
            else
            {
                // WHY: inside one session real elapsed time is known exactly, so any wall-clock movement beyond it
                // is a manual change. Cap the reading at what the stopwatch allows.
                double monotonicDelta = Math.Max(0d, monotonicSeconds - _lastMonotonicSeconds);
                DateTime monotonicCeiling = _sessionAnchorUtc.AddSeconds(monotonicDelta + ForwardToleranceSeconds);
                if (rawUtc > monotonicCeiling)
                {
                    LastReadWasJumpedForward = true;
                    rawUtc = monotonicCeiling;
                }
            }

            if (rawUtc < _highWaterUtc)
            {
                // WHY: the clock went backwards. Hand out the high-water mark instead so a rewind neither grants
                // free progress nor freezes a cooldown that is already due.
                LastReadWasRewound = true;
                return _highWaterUtc;
            }

            _highWaterUtc = rawUtc;
            return rawUtc;
        }

        /// <summary>Non-negative seconds between <paramref name="sinceUtc" /> and <see cref="ReadUtcNow" />.</summary>
        public double SecondsSince(DateTime sinceUtc)
        {
            double seconds = (ReadUtcNow() - DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc)).TotalSeconds;
            return seconds < 0d ? 0d : seconds;
        }

        /// <summary>Drops session state so the next read re-anchors. Used by tests and by domain reload.</summary>
        public void ResetSessionAnchor()
        {
            _hasSessionAnchor = false;
            _lastMonotonicSeconds = 0d;
            LastReadWasRewound = false;
            LastReadWasJumpedForward = false;
        }

        /// <summary>
        ///     Slack allowed between the wall clock and the stopwatch before a forward jump is assumed. Covers NTP
        ///     corrections, suspended-process drift and timer granularity.
        /// </summary>
        private const double ForwardToleranceSeconds = 90d;
    }

    /// <summary>
    ///     Process-wide <see cref="TrustedClock" /> whose high-water mark survives restarts through
    ///     <see cref="SaveProvider" />. Offline reward components read time from here instead of
    ///     <see cref="DateTime.UtcNow" />.
    /// </summary>
    public static class NeoTrustedTime
    {
        /// <summary>Save key holding the highest UTC value the game has ever observed.</summary>
        public const string HighWaterSaveKey = "Neo.TrustedClock.HighWaterUtc";

        /// <summary>Only persist after the mark moved this far, so reading the time does not churn the save file.</summary>
        private const double PersistThresholdSeconds = 15d;

        private static TrustedClock _clock;
        private static bool _restored;
        private static DateTime _persistedHighWater = DateTime.MinValue;

        /// <summary>Shared clock instance. Replace it in tests through <see cref="SetClockForTests" />.</summary>
        public static TrustedClock Clock
        {
            get
            {
                _clock ??= new TrustedClock();
                EnsureRestored();
                return _clock;
            }
        }

        /// <summary>Tamper-resistant replacement for <see cref="DateTime.UtcNow" />.</summary>
        public static DateTime UtcNow
        {
            get
            {
                DateTime now = Clock.ReadUtcNow();
                PersistIfAdvanced();
                return now;
            }
        }

        /// <summary>Non-negative seconds elapsed since <paramref name="sinceUtc" />, guarded against clock changes.</summary>
        public static double SecondsSince(DateTime sinceUtc)
        {
            double seconds = Clock.SecondsSince(sinceUtc);
            PersistIfAdvanced();
            return seconds;
        }

        /// <summary>Injects a clock for tests; pass <c>null</c> to restore the default behaviour.</summary>
        public static void SetClockForTests(TrustedClock clock, bool markRestored = true)
        {
            _clock = clock;
            _restored = markRestored;
            _persistedHighWater = clock?.HighWaterUtc ?? DateTime.MinValue;
        }

        /// <summary>
        ///     Clears cached state across domain reloads, matching the reset convention used by the rest of the package.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStaticState()
        {
            _clock = null;
            _restored = false;
            _persistedHighWater = DateTime.MinValue;
        }

        private static void EnsureRestored()
        {
            if (_restored)
            {
                return;
            }

            _restored = true;
            string raw = SaveProvider.GetString(HighWaterSaveKey, string.Empty);
            if (!string.IsNullOrEmpty(raw) && raw.TryParseUtcRoundTrip(out DateTime stored))
            {
                _persistedHighWater = stored;
                _clock.Restore(stored);
            }
        }

        private static void PersistIfAdvanced()
        {
            DateTime current = _clock.HighWaterUtc;
            if ((current - _persistedHighWater).TotalSeconds < PersistThresholdSeconds)
            {
                return;
            }

            _persistedHighWater = current;
            SaveProvider.SetString(HighWaterSaveKey, current.ToRoundTripUtcString());
        }
    }
}
