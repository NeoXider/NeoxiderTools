using System.Diagnostics;

namespace Neo.Tests.Performance
{
    /// <summary>
    ///     Measures the Update phase of each frame between <see cref="NeoPerfUpdateWindowBegin" /> (first script) and
    ///     <see cref="NeoPerfUpdateWindowEnd" /> (last script): wall time and main-thread GC.Alloc calls.
    /// </summary>
    /// <remarks>
    ///     Allocation counts come from one admitted <see cref="GcAllocCounter" />; when it cannot be admitted every
    ///     <see cref="AllocationCounts" /> entry is -1 and <see cref="AllocationsAvailable" /> is false.
    /// </remarks>
    public sealed class NeoPerfUpdateWindow
    {
        private readonly GcAllocCounter _allocations;
        private readonly double[] _milliseconds;
        private readonly long[] _allocationCounts;
        private long _start;
        private bool _open;
        private int _count;

        private NeoPerfUpdateWindow(int frames)
        {
            _milliseconds = new double[frames];
            _allocationCounts = new long[frames];
            GcAllocCounter.TryCreate(out _allocations, out string _);
        }

        public static NeoPerfUpdateWindow Active { get; private set; }

        public int Count => _count;
        public bool AllocationsAvailable => _allocations != null;
        public double[] Milliseconds => _milliseconds;
        public long[] AllocationCounts => _allocationCounts;

        /// <summary>Records the next <paramref name="frames" /> complete Update phases.</summary>
        public static NeoPerfUpdateWindow Start(int frames)
        {
            Active?.Stop();
            Active = new NeoPerfUpdateWindow(frames);
            return Active;
        }

        public void Stop()
        {
            _open = false;
            _allocations?.Dispose();
            if (Active == this)
            {
                Active = null;
            }
        }

        public void Begin()
        {
            if (_open)
            {
                // WHY: an End missed in an earlier frame leaves a window that can no longer be counted; drop it.
                _open = false;
                CloseAllocationWindow();
            }

            if (_count >= _milliseconds.Length)
            {
                return;
            }

            _allocations?.Begin();
            _open = true;
            _start = Stopwatch.GetTimestamp();
        }

        public void End()
        {
            if (!_open)
            {
                return;
            }

            long end = Stopwatch.GetTimestamp();
            _open = false;
            long allocations = CloseAllocationWindow();
            _milliseconds[_count] = NeoPerf.TicksToMilliseconds(end - _start);
            _allocationCounts[_count] = allocations;
            _count++;
        }

        private long CloseAllocationWindow()
        {
            if (_allocations == null)
            {
                return -1;
            }

            try
            {
                return _allocations.End();
            }
            catch (System.InvalidOperationException)
            {
                return -1;
            }
        }
    }
}
