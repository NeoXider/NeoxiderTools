using System;

namespace Neo.Network.Realtime
{
    /// <summary>What <see cref="SnapshotBuffer{T}.TrySample"/> found for a render time.</summary>
    public enum SnapshotSampleKind
    {
        /// <summary>The buffer is empty: nothing to draw.</summary>
        None = 0,

        /// <summary>
        ///     The render time is older than every stored frame (just connected, or the buffer is shorter than the
        ///     delay): hold the oldest frame. <c>from == to</c>, alpha 0.
        /// </summary>
        BeforeOldest = 1,

        /// <summary>The render time lies between two frames: blend <c>from</c> to <c>to</c> by alpha in 0..1.</summary>
        Between = 2,

        /// <summary>
        ///     The render time is past the newest frame (packet loss, the clock outran the data): hold the newest
        ///     frame. <c>from == to</c>, alpha 1. This is the "buffer ran dry" signal worth counting.
        /// </summary>
        AfterNewest = 3
    }

    /// <summary>
    ///     A small ring of timestamped snapshots with bracket search, the data structure behind snapshot interpolation:
    ///     keep the last few frames, ask "which two frames surround render time <c>t</c>, and how far between them?",
    ///     blend. Allocation-free after construction; <typeparamref name="T"/> is typically a struct (or a pooled class,
    ///     see <see cref="OnEvicted"/>).
    ///     <para>
    ///         Pair it with <see cref="SnapshotTimeline"/>: <c>TrySample(timeline.RenderTimeExact, ...)</c> every rendered
    ///         frame. Frames must be added in increasing time order; an older or duplicate stamp is rejected, which also
    ///         discards reordered packets on an unreliable channel.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The frame payload.</typeparam>
    public sealed class SnapshotBuffer<T>
    {
        private readonly double[] _times;
        private readonly T[] _frames;
        private int _head;
        private int _count;

        /// <summary>Creates a buffer holding the last <paramref name="capacity"/> frames (minimum 2).</summary>
        public SnapshotBuffer(
            int capacity = 4)
        {
            int size = capacity < 2 ? 2 : capacity;
            _times = new double[size];
            _frames = new T[size];
        }

        /// <summary>How many frames are stored.</summary>
        public int Count => _count;

        /// <summary>The most frames the buffer holds.</summary>
        public int Capacity => _frames.Length;

        /// <summary>
        ///     Called with a frame that was pushed out by a newer one, or dropped by <see cref="Clear"/>, so a pooled
        ///     payload can be returned to its pool. Optional.
        /// </summary>
        public Action<T> OnEvicted { get; set; }

        /// <summary>Stamp of the newest frame, or <c>double.NegativeInfinity</c> when empty.</summary>
        public double NewestTime => _count == 0 ? double.NegativeInfinity : _times[Slot(_count - 1)];

        /// <summary>Stamp of the oldest frame, or <c>double.PositiveInfinity</c> when empty.</summary>
        public double OldestTime => _count == 0 ? double.PositiveInfinity : _times[Slot(0)];

        /// <summary>
        ///     Stores a frame. Returns <see langword="false"/> (and stores nothing) when <paramref name="time"/> is not
        ///     newer than the newest stored stamp: that frame is stale, a duplicate or reordered.
        /// </summary>
        public bool TryAdd(double time, T frame)
        {
            if (_count > 0 && time <= _times[Slot(_count - 1)])
            {
                return false;
            }

            if (_count == _frames.Length)
            {
                T evicted = _frames[_head];
                _frames[_head] = default;
                _head = (_head + 1) % _frames.Length;
                _count--;
                OnEvicted?.Invoke(evicted);
            }

            int slot = Slot(_count);
            _times[slot] = time;
            _frames[slot] = frame;
            _count++;
            return true;
        }

        /// <summary>The frame <paramref name="index"/> places after the oldest (0 = oldest, <c>Count - 1</c> = newest).</summary>
        public T GetFrame(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _frames[Slot(index)];
        }

        /// <summary>The stamp of the frame <paramref name="index"/> places after the oldest.</summary>
        public double GetTime(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _times[Slot(index)];
        }

        /// <summary>The newest frame. Returns <see langword="false"/> when the buffer is empty.</summary>
        public bool TryGetNewest(out T frame)
        {
            if (_count == 0)
            {
                frame = default;
                return false;
            }

            frame = _frames[Slot(_count - 1)];
            return true;
        }

        /// <summary>
        ///     Finds the two frames surrounding <paramref name="renderTime"/>.
        ///     <para>
        ///         <paramref name="alpha"/> is 0..1 between <paramref name="from"/> and <paramref name="to"/>. Outside
        ///         the stored range both frames are the nearest edge (see <see cref="SnapshotSampleKind"/>); there is
        ///         deliberately no extrapolation, because a held frame is always a valid world state and a guessed
        ///         one is not.
        ///     </para>
        /// </summary>
        public SnapshotSampleKind TrySample(double renderTime, out T from, out T to, out float alpha)
        {
            if (_count == 0)
            {
                from = default;
                to = default;
                alpha = 0f;
                return SnapshotSampleKind.None;
            }

            if (renderTime < _times[Slot(0)])
            {
                from = to = _frames[Slot(0)];
                alpha = 0f;
                return SnapshotSampleKind.BeforeOldest;
            }

            int last = _count - 1;
            if (renderTime >= _times[Slot(last)])
            {
                from = to = _frames[Slot(last)];
                alpha = 1f;
                return SnapshotSampleKind.AfterNewest;
            }

            for (int i = last; i > 0; i--)
            {
                double newer = _times[Slot(i)];
                double older = _times[Slot(i - 1)];
                if (renderTime >= older)
                {
                    from = _frames[Slot(i - 1)];
                    to = _frames[Slot(i)];
                    double span = newer - older;
                    alpha = span > 0d ? (float)((renderTime - older) / span) : 1f;
                    return SnapshotSampleKind.Between;
                }
            }

            from = to = _frames[Slot(0)];
            alpha = 0f;
            return SnapshotSampleKind.BeforeOldest;
        }

        /// <summary>Drops every frame (disconnect, restarted match), reporting each one to <see cref="OnEvicted"/>.</summary>
        public void Clear()
        {
            Action<T> evicted = OnEvicted;
            for (int i = 0; i < _count; i++)
            {
                int slot = Slot(i);
                T frame = _frames[slot];
                _frames[slot] = default;
                evicted?.Invoke(frame);
            }

            _head = 0;
            _count = 0;
        }

        private int Slot(int index)
        {
            return (_head + index) % _frames.Length;
        }
    }

    /// <summary>Blend helpers for snapshot interpolation. Unity-free, no allocation.</summary>
    public static class NetInterpolation
    {
        /// <summary>Linear blend, <paramref name="alpha"/> clamped to 0..1.</summary>
        public static float Lerp(float from, float to, float alpha)
        {
            float t = alpha < 0f ? 0f : alpha > 1f ? 1f : alpha;
            return from + (to - from) * t;
        }

        /// <summary>Blend of two angles in radians along the shortest way round, result in the range of the inputs.</summary>
        public static float LerpAngleRadians(float from, float to, float alpha)
        {
            const float twoPi = (float)(Math.PI * 2d);
            float delta = (to - from) % twoPi;
            if (delta > (float)Math.PI)
            {
                delta -= twoPi;
            }
            else if (delta < -(float)Math.PI)
            {
                delta += twoPi;
            }

            float t = alpha < 0f ? 0f : alpha > 1f ? 1f : alpha;
            return from + delta * t;
        }

        /// <summary>Blend of two angles in degrees along the shortest way round.</summary>
        public static float LerpAngleDegrees(float from, float to, float alpha)
        {
            float delta = (to - from) % 360f;
            if (delta > 180f)
            {
                delta -= 360f;
            }
            else if (delta < -180f)
            {
                delta += 360f;
            }

            float t = alpha < 0f ? 0f : alpha > 1f ? 1f : alpha;
            return from + delta * t;
        }
    }
}
