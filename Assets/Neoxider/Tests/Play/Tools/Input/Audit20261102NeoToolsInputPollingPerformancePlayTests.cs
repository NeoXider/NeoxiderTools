using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     CPU and allocation benchmark of compatibility input polling against real virtual Input System devices,
    ///     sampled inside one frame after the real PlayerLoop update, plus <see cref="MouseInputManager" /> dispatch.
    ///     Prints one parsable <c>NEO_PERF</c> line per case and asserts only correctness, never timing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Every device case exists twice on the same devices and state: <c>role=current</c> runs
    ///         <see cref="OptionalInputSystemAdapter" />, <c>role=head_reference</c> runs
    ///         <see cref="FrozenInputReader1101" />, the 11.0.1 read path. Both are called at adapter level, so the
    ///         ratio is the adapter change alone. <c>KeyDownUp</c> is the keyboard <c>InteractiveObject</c> route
    ///         (one down and one up read per object and frame); its head bytes compare with the 2026-11-02 Editor
    ///         profiler baseline of that route, 3384 bytes per object per frame.
    ///     </para>
    ///     <para>
    ///         <c>gc_calls_per_op</c> counts native <c>GC.Alloc</c> events in one calibrated synchronous window.
    ///         <c>bytes_per_op_est</c> is the median <c>GC Allocated In Frame</c> of frames that run one sample minus
    ///         the median of interleaved idle frames, divided by the sample size; validate it with
    ///         <c>Input.Play.Calibration.Bytes4096</c> before trusting small values.
    ///     </para>
    /// </remarks>
    [TestFixture]
    [Category(NeoPerf.Category)]
    [Explicit(NeoPerf.ExplicitReason)]
    public sealed class Audit20261102NeoToolsInputPollingPerformancePlayTests
    {
        private const int CurrentOperationsPerSample = 1024;
        private const int HeadOperationsPerSample = 128;
        private const int WarmSamples = 8;
        private const int Samples = 120;
        private const int ByteFrames = 16;
        private const int CalibrationPayloadLength = 4096;

#if ENABLE_IL2CPP
        private const string ScriptingBackend = "il2cpp";
#else
        private const string ScriptingBackend = "mono";
#endif

        private static readonly Vector2 MovedPosition = new(220f, 160f);
        private static readonly Vector2 LeftStickInput = new(0.6f, -0.45f);
        private static readonly Vector2 RightStickInput = new(-0.3f, 0.7f);
        private static byte[] _calibrationSink;
        private static Vector2 _expectedLeftStick;

        [SetUp]
        public void SetUp()
        {
            OptionalInputSystemAdapter.ResetStaticState();
            FrozenInputReader1101.Reset();
        }

        [UnityTest]
        public IEnumerator Calibration_Bytes4096()
        {
            yield return null;
            long allocationCalls;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = RunSample(AllocateCalibrationPayload, CurrentOperationsPerSample);
                allocationCalls = counter.End();
                Assert.That(correct, Is.EqualTo(CurrentOperationsPerSample));
            }

            NeoPerf.ByteEstimate bytes = new();
            yield return EstimateBytes(AllocateCalibrationPayload, CurrentOperationsPerSample, bytes);
            new NeoPerf.Line("Input.Play.Calibration.Bytes4096")
                .Add("gc_calls_per_op", allocationCalls / (double)CurrentOperationsPerSample)
                .Add("bytes_per_op_est", FormatBytes(bytes))
                .Add("payload_bytes", CalibrationPayloadLength)
                .Add("ops_per_sample", CurrentOperationsPerSample)
                .Add("scripting_backend", ScriptingBackend)
                .Emit();
        }

        [UnityTest]
        public IEnumerator KeyDownUp_Current()
        {
            return KeyboardCase("Input.Play.KeyDownUp.Current", KeyDownUpCurrent, CurrentOperationsPerSample,
                "reads_per_op=2 shape=InteractiveObject.keyboard role=current");
        }

        [UnityTest]
        public IEnumerator KeyDownUp_HeadReference()
        {
            return KeyboardCase("Input.Play.KeyDownUp.Head", KeyDownUpHead, HeadOperationsPerSample,
                "reads_per_op=2 shape=InteractiveObject.keyboard role=head_reference");
        }

        [UnityTest]
        public IEnumerator KeyCompatTriple_OptionalBackend()
        {
            return KeyboardCase("Input.Play.KeyCompatTriple.Optional", KeyCompatTriple, CurrentOperationsPerSample,
                "reads_per_op=3 api=KeyInputCompat role=current");
        }

        [UnityTest]
        public IEnumerator KeyCompatTriple_LegacyReference()
        {
            yield return null;
            if (!InputBackendCompat.UsesLegacyInput)
            {
                Assert.Ignore("The legacy Input Manager is not enabled in this host; no legacy reference is claimed.");
            }

            yield return MeasureDeviceCase("Input.Play.KeyCompatTriple.Legacy", iteration =>
                    !KeyInputCompat.GetKey(KeyCode.F12) && !KeyInputCompat.GetKeyDown(KeyCode.F12) &&
                    !KeyInputCompat.GetKeyUp(KeyCode.F12), CurrentOperationsPerSample,
                "reads_per_op=3 api=KeyInputCompat role=legacy_reference");
        }

        [UnityTest]
        public IEnumerator MousePass_Current()
        {
            return MouseCase("Input.Play.MousePass.Current", MousePassCurrent, CurrentOperationsPerSample,
                "reads_per_op=4 shape=MouseInputManager.Update role=current");
        }

        [UnityTest]
        public IEnumerator MousePass_HeadReference()
        {
            return MouseCase("Input.Play.MousePass.Head", MousePassHead, HeadOperationsPerSample,
                "reads_per_op=4 shape=MouseInputManager.Update role=head_reference");
        }

        [UnityTest]
        public IEnumerator PrimaryTouch_Current()
        {
            return TouchCase("Input.Play.PrimaryTouch.Current", PrimaryTouchCurrent, CurrentOperationsPerSample,
                "reads_per_op=1 shape=SwipeController.Update role=current");
        }

        [UnityTest]
        public IEnumerator PrimaryTouch_HeadReference()
        {
            return TouchCase("Input.Play.PrimaryTouch.Head", PrimaryTouchHead, HeadOperationsPerSample,
                "reads_per_op=1 shape=SwipeController.Update role=head_reference");
        }

        [UnityTest]
        public IEnumerator ReadMove_Current()
        {
            return GamepadCase("Input.Play.ReadMove.Current", ReadMoveCurrent, CurrentOperationsPerSample,
                "reads_per_op=8_keys_plus_left_stick role=current");
        }

        [UnityTest]
        public IEnumerator ReadMove_HeadReference()
        {
            return GamepadCase("Input.Play.ReadMove.Head", ReadMoveHead, HeadOperationsPerSample,
                "reads_per_op=8_keys_plus_left_stick role=head_reference");
        }

        [Test]
        public void MouseManagerHold_OneSubscriber()
        {
            MeasureManager("Input.Play.MouseManager.Hold.Subscribers1", 1, false);
        }

        [Test]
        public void MouseManagerHold_EightSubscribers()
        {
            MeasureManager("Input.Play.MouseManager.Hold.Subscribers8", 8, false);
        }

        [Test]
        public void MouseManagerCycle_EightSubscribers()
        {
            MeasureManager("Input.Play.MouseManager.Cycle.Subscribers8", 8, true);
        }

        private static IEnumerator KeyboardCase(string caseId, Func<int, bool> operation, int operationsPerSample,
            string extra)
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            {
                yield return null;
                yield return null;
                input.QueueKeyboard(true);
                yield return null;
                Assert.That(InputBackendCompat.UsesLegacyInput, Is.False, input.Diagnostics);
                yield return MeasureDeviceCase(caseId, operation, operationsPerSample, extra);
            }
        }

        private static IEnumerator MouseCase(string caseId, Func<int, bool> operation, int operationsPerSample,
            string extra)
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            {
                yield return null;
                yield return null;
                input.QueueMouse(true);
                yield return null;
                Assert.That(InputBackendCompat.UsesLegacyInput, Is.False, input.Diagnostics);
                yield return MeasureDeviceCase(caseId, operation, operationsPerSample, extra);
            }
        }

        private static IEnumerator TouchCase(string caseId, Func<int, bool> operation, int operationsPerSample,
            string extra)
        {
            using (new Audit20261023NeoToolsInteractableObjectInputDriver())
            using (InputVirtualTouchscreen touch = new())
            {
                yield return null;
                yield return null;
                touch.Queue(1, "Began", new Vector2(100f, 100f));
                yield return null;
                touch.Queue(1, "Moved", MovedPosition);
                yield return null;
                Assert.That(operation(0), Is.True, touch.DescribePrimaryTouch());
                yield return MeasureDeviceCase(caseId, operation, operationsPerSample, extra);
            }
        }

        private static IEnumerator GamepadCase(string caseId, Func<int, bool> operation, int operationsPerSample,
            string extra)
        {
            using (new Audit20261023NeoToolsInteractableObjectInputDriver())
            using (InputVirtualGamepad gamepad = new())
            {
                yield return null;
                yield return null;
                gamepad.Queue(LeftStickInput, RightStickInput, true);
                yield return null;
                _expectedLeftStick = gamepad.ReadNativeStick("leftStick");
                Assert.That(_expectedLeftStick.sqrMagnitude, Is.GreaterThan(0.01f), gamepad.Describe());
                Assert.That(operation(0), Is.True, gamepad.Describe());
                yield return MeasureDeviceCase(caseId, operation, operationsPerSample, extra);
            }
        }

        private static bool KeyDownUpCurrent(int iteration)
        {
            bool downAvailable =
                OptionalInputSystemAdapter.TryReadKeyState(KeyCode.E, "wasPressedThisFrame", out bool down);
            bool upAvailable = OptionalInputSystemAdapter.TryReadKeyState(KeyCode.E, "wasReleasedThisFrame", out bool up);
            return downAvailable && upAvailable && down && !up;
        }

        private static bool KeyDownUpHead(int iteration)
        {
            bool downAvailable = FrozenInputReader1101.TryReadKeyState(KeyCode.E, "wasPressedThisFrame", out bool down);
            bool upAvailable = FrozenInputReader1101.TryReadKeyState(KeyCode.E, "wasReleasedThisFrame", out bool up);
            return downAvailable && upAvailable && down && !up;
        }

        private static bool KeyCompatTriple(int iteration)
        {
            return KeyInputCompat.GetKey(KeyCode.E) && KeyInputCompat.GetKeyDown(KeyCode.E) &&
                   !KeyInputCompat.GetKeyUp(KeyCode.E);
        }

        private static bool MousePassCurrent(int iteration)
        {
            return OptionalInputSystemAdapter.TryGetMousePosition(out Vector3 position) &&
                   OptionalInputSystemAdapter.TryGetMouseButtonState(0, "wasPressedThisFrame", out bool _) &&
                   OptionalInputSystemAdapter.TryGetMouseButtonState(0, "isPressed", out bool held) &&
                   OptionalInputSystemAdapter.TryGetMouseButtonState(0, "wasReleasedThisFrame", out bool _) &&
                   held && position.x == 128f && position.y == 128f;
        }

        private static bool MousePassHead(int iteration)
        {
            return FrozenInputReader1101.TryGetMousePosition(out Vector3 position) &&
                   FrozenInputReader1101.TryGetMouseButtonState(0, "wasPressedThisFrame", out bool _) &&
                   FrozenInputReader1101.TryGetMouseButtonState(0, "isPressed", out bool held) &&
                   FrozenInputReader1101.TryGetMouseButtonState(0, "wasReleasedThisFrame", out bool _) &&
                   held && position.x == 128f && position.y == 128f;
        }

        private static bool PrimaryTouchCurrent(int iteration)
        {
            return OptionalInputSystemAdapter.TryGetPrimaryTouch(out int fingerId, out Vector2 position,
                out TouchPhase phase) && fingerId == 1 && phase == TouchPhase.Moved && position == MovedPosition;
        }

        private static bool PrimaryTouchHead(int iteration)
        {
            return FrozenInputReader1101.TryGetPrimaryTouch(out int fingerId, out Vector2 position,
                out TouchPhase phase) && fingerId == 1 && phase == TouchPhase.Moved && position == MovedPosition;
        }

        private static bool ReadMoveCurrent(int iteration)
        {
            Vector2 move = OptionalInputSystemAdapter.ReadMove();
            return move.x == _expectedLeftStick.x && move.y == _expectedLeftStick.y;
        }

        private static bool ReadMoveHead(int iteration)
        {
            Vector2 move = FrozenInputReader1101.ReadMove();
            return move.x == _expectedLeftStick.x && move.y == _expectedLeftStick.y;
        }

        private static bool AllocateCalibrationPayload(int iteration)
        {
            _calibrationSink = new byte[CalibrationPayloadLength];
            return _calibrationSink.Length == CalibrationPayloadLength;
        }

        private static void MeasureManager(string caseId, int subscribers, bool cycle)
        {
            using (InputMouseManagerRig rig = new(subscribers, true))
            {
                if (!cycle)
                {
                    rig.Press();
                }

                Func<int, bool> operation = cycle ? _ => rig.Cycle() : _ => rig.Hold();
                SyncResult result = MeasureSync(caseId, operation, CurrentOperationsPerSample);
                Emit(caseId, result, null, CurrentOperationsPerSample,
                    "subscribers_per_event=" + subscribers.ToString(CultureInfo.InvariantCulture) +
                    "_action_plus_" + subscribers.ToString(CultureInfo.InvariantCulture) + "_in " +
                    (cycle ? "shape=press_hold_hold_release_click" : "shape=hold_frame") + " role=current");
            }
        }

        private static IEnumerator MeasureDeviceCase(string caseId, Func<int, bool> operation, int operationsPerSample,
            string extra)
        {
            SyncResult result = MeasureSync(caseId, operation, operationsPerSample);
            NeoPerf.ByteEstimate bytes = new();
            yield return EstimateBytes(operation, operationsPerSample, bytes);
            Emit(caseId, result, bytes, operationsPerSample, extra);
        }

        private static SyncResult MeasureSync(string caseId, Func<int, bool> operation, int operationsPerSample)
        {
            int frame = Time.frameCount;
            for (int sample = 0; sample < WarmSamples; sample++)
            {
                Assert.That(RunSample(operation, operationsPerSample), Is.EqualTo(operationsPerSample),
                    caseId + " warm-up oracle");
            }

            long allocationCalls;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = RunSample(operation, operationsPerSample);
                allocationCalls = counter.End();
                Assert.That(correct, Is.EqualTo(operationsPerSample), caseId + " counted-sample oracle");
            }

            double[] nanoseconds = new double[Samples];
            Stopwatch watch = new();
            for (int sample = 0; sample < Samples; sample++)
            {
                watch.Restart();
                int correct = RunSample(operation, operationsPerSample);
                watch.Stop();
                Assert.That(correct, Is.EqualTo(operationsPerSample), caseId + " timed-sample oracle");
                nanoseconds[sample] = watch.Elapsed.TotalMilliseconds * 1000000d / operationsPerSample;
            }

            Assert.That(Time.frameCount, Is.EqualTo(frame), "All samples must read one Input System update.");
            NeoPerf.Distribution distribution = NeoPerf.Summarize(nanoseconds, Samples);
            return new SyncResult(distribution.P50, distribution.P95, distribution.P99,
                allocationCalls / (double)operationsPerSample);
        }

        /// <remarks>
        ///     WHY: the frames after the synchronous part no longer carry the queued edges, so the oracle result is
        ///     ignored here; every measured path performs the same reads whatever the sampled value is.
        /// </remarks>
        private static IEnumerator EstimateBytes(Func<int, bool> operation, int operationsPerSample,
            NeoPerf.ByteEstimate result)
        {
            yield return NeoPerf.EstimateBytesPerOperation(() => RunSample(operation, operationsPerSample), 1,
                ByteFrames, result);
            result.BytesPerOperation /= operationsPerSample;
        }

        private static void Emit(string caseId, SyncResult result, NeoPerf.ByteEstimate bytes,
            int operationsPerSample, string extra)
        {
            new NeoPerf.Line(caseId)
                .Add("ns_per_op_p50", result.P50)
                .Add("ns_per_op_p95", result.P95)
                .Add("ns_per_op_p99", result.P99)
                .Add("gc_calls_per_op", result.GcCallsPerOperation)
                .Add("bytes_per_op_est", FormatBytes(bytes))
                .Add("ops_per_sample", operationsPerSample)
                .Add("samples", Samples)
                .Add("scripting_backend", ScriptingBackend)
                .AddPairs(extra)
                .Emit();
        }

        private static string FormatBytes(NeoPerf.ByteEstimate bytes)
        {
            if (bytes == null)
            {
                return "not_measured";
            }

            return bytes.Available
                ? bytes.BytesPerOperation.ToString("0.#", CultureInfo.InvariantCulture)
                : "unavailable";
        }

        private static int RunSample(Func<int, bool> operation, int operationsPerSample)
        {
            int correct = 0;
            for (int i = 0; i < operationsPerSample; i++)
            {
                if (operation(i))
                {
                    correct++;
                }
            }

            return correct;
        }

        private readonly struct SyncResult
        {
            public readonly double P50;
            public readonly double P95;
            public readonly double P99;
            public readonly double GcCallsPerOperation;

            public SyncResult(double p50, double p95, double p99, double gcCallsPerOperation)
            {
                P50 = p50;
                P95 = p95;
                P99 = p99;
                GcCallsPerOperation = gcCallsPerOperation;
            }
        }
    }
}
