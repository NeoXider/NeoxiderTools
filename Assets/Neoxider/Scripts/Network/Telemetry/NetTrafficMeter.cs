using System;
using System.Collections.Generic;

namespace Neo.Network
{
    /// <summary>
    ///     Counts bytes and messages per second, in total and per message kind, over a short rolling window. Pure C#
    ///     with an injectable clock (<see cref="Tick"/> takes the time), so it is unit-testable and costs one dictionary
    ///     lookup per recorded message; no allocation after a kind was seen once.
    ///     <para>
    ///         <see cref="NeoNetworkTelemetry"/> feeds one meter with Mirror's outgoing messages and one with its
    ///         incoming messages; a game can also feed it by hand (for example the byte size of each snapshot frame).
    ///     </para>
    /// </summary>
    public sealed class NetTrafficMeter
    {
        /// <summary>Traffic of one message kind.</summary>
        public readonly struct KindStat
        {
            /// <summary>The message type (a Mirror <c>NetworkMessage</c> struct) or any key the caller records under.</summary>
            public readonly Type Kind;

            /// <summary>Bytes since the meter was created or reset.</summary>
            public readonly long TotalBytes;

            /// <summary>Messages since the meter was created or reset.</summary>
            public readonly long TotalMessages;

            /// <summary>Bytes per second over the last completed window.</summary>
            public readonly float BytesPerSecond;

            /// <summary>Messages per second over the last completed window.</summary>
            public readonly float MessagesPerSecond;

            public KindStat(Type kind, long totalBytes, long totalMessages, float bytesPerSecond, float messagesPerSecond)
            {
                Kind = kind;
                TotalBytes = totalBytes;
                TotalMessages = totalMessages;
                BytesPerSecond = bytesPerSecond;
                MessagesPerSecond = messagesPerSecond;
            }
        }

        private sealed class Counter
        {
            public Type Kind;
            public long TotalBytes;
            public long TotalMessages;
            public long WindowBytes;
            public long WindowMessages;
            public float BytesPerSecond;
            public float MessagesPerSecond;
        }

        private readonly float _windowSeconds;
        private readonly Dictionary<Type, Counter> _byKind = new();
        private readonly List<Counter> _counters = new();
        private double _windowStart;
        private bool _started;
        private long _windowBytes;
        private long _windowMessages;

        /// <summary>Creates a meter whose rates are averaged over <paramref name="windowSeconds"/> (default 1 s).</summary>
        public NetTrafficMeter(float windowSeconds = 1f)
        {
            _windowSeconds = windowSeconds < 0.1f ? 0.1f : windowSeconds;
        }

        /// <summary>Bytes per second over the last completed window, all kinds together.</summary>
        public float BytesPerSecond { get; private set; }

        /// <summary>Messages per second over the last completed window, all kinds together.</summary>
        public float MessagesPerSecond { get; private set; }

        /// <summary>Bytes since the meter was created or reset.</summary>
        public long TotalBytes { get; private set; }

        /// <summary>Messages since the meter was created or reset.</summary>
        public long TotalMessages { get; private set; }

        /// <summary>How many distinct kinds were recorded.</summary>
        public int KindCount => _counters.Count;

        /// <summary>
        ///     Records traffic of one kind. Safe to call from a hot path.
        /// </summary>
        /// <param name="kind">The message type (or any key) to count under; <see langword="null"/> counts only in the totals.</param>
        /// <param name="bytes">Payload size of one message; negative values are ignored.</param>
        /// <param name="messages">How many messages of that size (an out message sent to N connections is N).</param>
        public void Record(Type kind, int bytes, int messages = 1)
        {
            if (bytes < 0 || messages < 1)
            {
                return;
            }

            long total = (long)bytes * messages;
            TotalBytes += total;
            TotalMessages += messages;
            _windowBytes += total;
            _windowMessages += messages;

            if (kind == null)
            {
                return;
            }

            if (!_byKind.TryGetValue(kind, out Counter counter))
            {
                counter = new Counter { Kind = kind };
                _byKind.Add(kind, counter);
                _counters.Add(counter);
            }

            counter.TotalBytes += total;
            counter.TotalMessages += messages;
            counter.WindowBytes += total;
            counter.WindowMessages += messages;
        }

        /// <summary>
        ///     Advances the clock. When a window has elapsed the per-second rates are recomputed and the window restarts.
        ///     Call every frame (or at least a few times per window).
        /// </summary>
        /// <param name="nowSeconds">Any monotonic clock in seconds, for example <c>Time.realtimeSinceStartupAsDouble</c>.</param>
        public void Tick(double nowSeconds)
        {
            if (!_started)
            {
                _started = true;
                _windowStart = nowSeconds;
                return;
            }

            double elapsed = nowSeconds - _windowStart;
            if (elapsed < _windowSeconds)
            {
                return;
            }

            BytesPerSecond = (float)(_windowBytes / elapsed);
            MessagesPerSecond = (float)(_windowMessages / elapsed);
            for (int i = 0; i < _counters.Count; i++)
            {
                Counter counter = _counters[i];
                counter.BytesPerSecond = (float)(counter.WindowBytes / elapsed);
                counter.MessagesPerSecond = (float)(counter.WindowMessages / elapsed);
                counter.WindowBytes = 0;
                counter.WindowMessages = 0;
            }

            _windowBytes = 0;
            _windowMessages = 0;
            _windowStart = nowSeconds;
        }

        /// <summary>Stats of one kind; <see langword="false"/> when it was never recorded.</summary>
        public bool TryGetKind(Type kind, out KindStat stat)
        {
            if (kind != null && _byKind.TryGetValue(kind, out Counter counter))
            {
                stat = ToStat(counter);
                return true;
            }

            stat = default;
            return false;
        }

        /// <summary>
        ///     Copies the busiest kinds into <paramref name="buffer"/>, highest bytes per second first (ties by total
        ///     bytes). No allocation.
        /// </summary>
        /// <returns>How many entries were written (at most <c>buffer.Length</c>).</returns>
        public int GetTopKinds(KindStat[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return 0;
            }

            int filled = 0;
            for (int i = 0; i < _counters.Count; i++)
            {
                KindStat candidate = ToStat(_counters[i]);
                int slot = filled;
                while (slot > 0 && IsBusier(candidate, buffer[slot - 1]))
                {
                    slot--;
                }

                if (slot >= buffer.Length)
                {
                    continue;
                }

                int last = filled < buffer.Length ? filled : buffer.Length - 1;
                for (int j = last; j > slot; j--)
                {
                    buffer[j] = buffer[j - 1];
                }

                buffer[slot] = candidate;
                if (filled < buffer.Length)
                {
                    filled++;
                }
            }

            return filled;
        }

        /// <summary>Forgets every count and rate.</summary>
        public void Reset()
        {
            _byKind.Clear();
            _counters.Clear();
            _started = false;
            _windowBytes = 0;
            _windowMessages = 0;
            BytesPerSecond = 0f;
            MessagesPerSecond = 0f;
            TotalBytes = 0;
            TotalMessages = 0;
        }

        private static KindStat ToStat(Counter counter)
        {
            return new KindStat(counter.Kind, counter.TotalBytes, counter.TotalMessages, counter.BytesPerSecond,
                counter.MessagesPerSecond);
        }

        private static bool IsBusier(KindStat a, KindStat b)
        {
            return a.BytesPerSecond > b.BytesPerSecond
                   || (a.BytesPerSecond == b.BytesPerSecond && a.TotalBytes > b.TotalBytes);
        }
    }
}
