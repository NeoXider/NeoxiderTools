using System;
using System.Threading;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;

namespace Neo.Tests.Performance
{
    /// <summary>
    ///     Calibrated counter of native <c>GC.Alloc</c> events made by the calling thread inside an explicit synchronous
    ///     window. A count is the number of managed allocations, not bytes; zero calls means zero bytes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Admission protocol (<see cref="TryCreate" />, <see cref="CreateOrIgnore" />): a warm-up window, then an
    ///         empty window must read 0, an allocation made outside any window must not be counted, a retained
    ///         <c>byte[4096]</c> inside a window must read exactly 1 (twice), no sample may leak into the next window,
    ///         and the whole calibration must finish in one frame. Only an admitted counter opens measured windows.
    ///     </para>
    ///     <para>
    ///         WHY: <see cref="GC.GetAllocatedBytesForCurrentThread" /> returns 0 under the Boehm GC of the Unity 6
    ///         Editor and IL2CPP even for a retained 4096-byte array, so a byte delta built on it can never fail. The
    ///         native <c>GC.Alloc</c> profiler marker (the source of <c>Is.Not.AllocatingGCMemory()</c>) can.
    ///     </para>
    ///     <para>
    ///         A window must open and close in the same frame on the thread that created the counter: the recorder keeps
    ///         one frame sample, so a window across a frame boundary would lose counts and is rejected instead.
    ///     </para>
    /// </remarks>
    public sealed class GcAllocCounter : IDisposable
    {
        /// <summary>Length of the retained calibration array that must read exactly one GC.Alloc call.</summary>
        public const int CalibrationPayloadLength = 4096;

        private static readonly ProfilerMarker WindowMarker = new("Neo.Tests.GcAlloc.Window");
        private static readonly ProfilerMarker PayloadMarker = new("Neo.Tests.GcAlloc.Retained4096");
        private static readonly ProfilerMarker ExcludedMarker = new("Neo.Tests.GcAlloc.Excluded4096");

        private readonly int _ownerThread;
        private ProfilerRecorder _recorder;
        private bool _running;
        private bool _admitted;
        private bool _disposed;
        private int _windowFrame;

        private GcAllocCounter()
        {
            _ownerThread = Thread.CurrentThread.ManagedThreadId;
            _recorder = new ProfilerRecorder(ProfilerCategory.Memory, "GC.Alloc", 1,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread |
                ProfilerRecorderOptions.SumAllSamplesInFrame |
                ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
        }

        /// <summary>Length of the array retained by the last calibration pass (always <see cref="CalibrationPayloadLength" />).</summary>
        public int RetainedPayloadLength { get; private set; }

        /// <summary>GC.Alloc calls read for the retained calibration array (always 1 on an admitted counter).</summary>
        public long PayloadAllocationCount { get; private set; }

        /// <summary>Number of positive-control passes the admission ran (2 on an admitted counter).</summary>
        public int CalibrationPasses { get; private set; }

        /// <summary>
        ///     Creates an admitted counter, or ends the calling test as Ignored with a <c>NotSupported:</c> message when
        ///     the recorder is unavailable (for example a player without profiler support) or fails calibration.
        /// </summary>
        public static GcAllocCounter CreateOrIgnore()
        {
            if (TryCreate(out GcAllocCounter counter, out string failure))
            {
                return counter;
            }

            Assert.Ignore(NotAdmittedMessage(failure));
            return null;
        }

        /// <summary>
        ///     Creates an admitted counter. Returns false with the reason when the recorder is unavailable or fails
        ///     calibration, so a test can finish its functional asserts before ignoring the allocation claim.
        /// </summary>
        public static bool TryCreate(out GcAllocCounter counter, out string failure)
        {
            GcAllocCounter candidate = new();
            try
            {
                failure = candidate.Calibrate();
            }
            catch (Exception exception)
            {
                failure = exception.GetType().Name + ": " + exception.Message;
            }

            if (failure == null)
            {
                candidate._admitted = true;
                counter = candidate;
                return true;
            }

            candidate.Dispose();
            counter = null;
            return false;
        }

        /// <summary>Message used when no allocation result can be claimed.</summary>
        public static string NotAdmittedMessage(string failure)
        {
            return "NotSupported: the native GC.Alloc counter was not admitted (" + failure +
                   "); no allocation result is claimed.";
        }

        /// <summary>Opens the measured window. Setup, warm-up and assertions must stay outside it.</summary>
        public void Begin()
        {
            if (!_admitted)
            {
                throw new InvalidOperationException("The GC.Alloc counter is not admitted.");
            }

            BeginWindow();
        }

        /// <summary>Closes the window and returns the number of GC.Alloc calls recorded inside it.</summary>
        public long End()
        {
            RequireOwnerThread();
            if (!_running)
            {
                throw new InvalidOperationException("No GC.Alloc window is open.");
            }

            WindowMarker.End();
            _recorder.Stop();
            _running = false;
            if (Time.frameCount != _windowFrame)
            {
                throw new InvalidOperationException(
                    "A GC.Alloc window must open and close in the same frame; measure synchronous work only.");
            }

            long count = 0;
            for (int i = 0; i < _recorder.Count; i++)
            {
                count = checked(count + _recorder.GetSample(i).Count);
            }

            return count;
        }

        /// <summary>Runs <paramref name="body" /> inside one window and returns its GC.Alloc calls.</summary>
        /// <remarks>Create the delegate outside the window (a capturing lambda allocates where it is created).</remarks>
        public long Count(Action body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            long count;
            Begin();
            try
            {
                body();
            }
            finally
            {
                count = End();
            }

            return count;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            RequireOwnerThread();
            try
            {
                if (_running)
                {
                    WindowMarker.End();
                    _recorder.Stop();
                    _running = false;
                }
            }
            finally
            {
                _recorder.Dispose();
                _disposed = true;
            }
        }

        private string Calibrate()
        {
            if (!_recorder.Valid)
            {
                return "no valid GC.Alloc profiler recorder in this runtime";
            }

            int frame = Time.frameCount;

            // WHY: the first window warms only the recorder and marker boundary, never a measured path.
            BeginWindow();
            End();
            long empty = CountEmptyWindow();
            if (empty != 0)
            {
                return "an empty window read " + empty + " calls";
            }

            byte[] outside;
            using (ExcludedMarker.Auto())
            {
                outside = new byte[CalibrationPayloadLength];
            }

            if (CountEmptyWindow() != 0)
            {
                return "an allocation made outside the window was counted";
            }

            GC.KeepAlive(outside);
            for (int pass = 0; pass < 2; pass++)
            {
                string payloadFailure = CalibratePayload();
                if (payloadFailure != null)
                {
                    return payloadFailure;
                }

                if (CountEmptyWindow() != 0)
                {
                    return "a previous sample leaked into the next window";
                }
            }

            return Time.frameCount == frame ? null : "calibration spanned more than one frame";
        }

        private string CalibratePayload()
        {
            byte[] retained;
            long count;
            BeginWindow();
            try
            {
                using (PayloadMarker.Auto())
                {
                    retained = new byte[CalibrationPayloadLength];
                }
            }
            finally
            {
                count = End();
            }

            GC.KeepAlive(retained);
            RetainedPayloadLength = retained.Length;
            PayloadAllocationCount = count;
            if (count != 1)
            {
                return "a retained byte[" + CalibrationPayloadLength + "] read " + count + " calls instead of 1";
            }

            CalibrationPasses++;
            return null;
        }

        private long CountEmptyWindow()
        {
            BeginWindow();
            return End();
        }

        private void BeginWindow()
        {
            RequireOwnerThread();
            if (_disposed || _running)
            {
                throw new InvalidOperationException("A GC.Alloc window is already open or the counter is disposed.");
            }

            _windowFrame = Time.frameCount;

            // WHY: Reset clears only this private recorder; no global profiler state is touched.
            _recorder.Reset();
            _recorder.Start();
            WindowMarker.Begin();
            _running = true;
        }

        private void RequireOwnerThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _ownerThread)
            {
                throw new InvalidOperationException("GC.Alloc windows must stay on the thread that created the counter.");
            }
        }
    }
}
