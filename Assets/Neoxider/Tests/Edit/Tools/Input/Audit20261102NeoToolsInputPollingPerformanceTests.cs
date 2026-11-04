using System;
using System.Diagnostics;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using AdapterTouchPhase = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.AdapterTouchPhase;
using ButtonDevice = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.ButtonDevice;
using TouchControls = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.TouchControls;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     CPU and allocation benchmark of <see cref="OptionalInputSystemAdapter" /> read paths on fake controls, so
    ///     the adapter's own cost is isolated from device processing. Prints one parsable <c>NEO_PERF</c> line per
    ///     case and asserts only workload correctness, never timing.
    /// </summary>
    /// <remarks>
    ///     The reflected-fallback case reads a byte-based phase enum and an object-returning position, which cannot
    ///     be bound to typed getters, so it prices the current adapter's own reflection fallback. It is not the
    ///     11.0.1 algorithm (press edges stay bound and no string cache key is built): the before/after reference
    ///     is <c>role=head_reference</c> in the Play Mode benchmark, which runs a frozen 11.0.1 reader on real
    ///     virtual devices.
    /// </remarks>
    [TestFixture]
    [Category(NeoPerf.Category)]
    [Explicit(NeoPerf.ExplicitReason)]
    public sealed class Audit20261102NeoToolsInputPollingPerformanceTests
    {
        private const int OperationsPerSample = 1024;
        private const int WarmSamples = 8;
        private const int Samples = 120;

        [SetUp]
        public void SetUp()
        {
            OptionalInputSystemAdapter.ResetStaticState();
        }

        [Test]
        public void BoolTriple_HeldDownUp()
        {
            ButtonDevice device = new();
            Measure("Input.Edit.BoolTriple", i =>
            {
                device.leftButton.isPressed = (i & 1) != 0;
                device.leftButton.wasPressedThisFrame = (i & 2) != 0;
                device.leftButton.wasReleasedThisFrame = (i & 4) != 0;
                bool held = OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "isPressed", out bool h) && h;
                bool down = OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "wasPressedThisFrame",
                    out bool d) && d;
                bool up = OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "wasReleasedThisFrame",
                    out bool u) && u;
                return ((held ? 1 : 0) | (down ? 2 : 0) | (up ? 4 : 0)) == (i & 7);
            }, "reads_per_op=3 role=current");
        }

        [Test]
        public void KeyMapping_Letters()
        {
            Measure("Input.Edit.KeyMapping", i =>
                OptionalInputSystemAdapter.GetInputSystemKeyPropertyName((KeyCode)((int)KeyCode.A + i % 26)) != null,
                "reads_per_op=1 role=current");
        }

        [Test]
        public void Touch_BoundGetters()
        {
            TouchControls touch = new();
            touch.touchId.Value = 4;
            touch.phase.Value = AdapterTouchPhase.Moved;
            Measure("Input.Edit.TouchBound", i =>
            {
                touch.position.Value = new Vector2(i, 1f);
                return OptionalInputSystemAdapter.TryReadTouch(touch, out int id, out Vector2 position,
                    out TouchPhase phase) && id == 4 && position.x == i && phase == TouchPhase.Moved;
            }, "reads_per_op=1 role=current");
        }

        [Test]
        public void Touch_ReflectedFallback()
        {
            Audit20261102NeoToolsInputValueReaderTests.ObjectControl position = new();
            Audit20261102NeoToolsInputValueReaderTests.FlexibleTouch touch = new()
            {
                touchId = new Audit20261102NeoToolsInputValueReaderTests.SampleControl<int> { Value = 4 },
                position = position,
                phase = new Audit20261102NeoToolsInputValueReaderTests.SampleControl<
                    Audit20261102NeoToolsInputValueReaderTests.ByteTouchPhase>
                {
                    Value = Audit20261102NeoToolsInputValueReaderTests.ByteTouchPhase.Moved
                }
            };
            Vector2 sample = new(3f, 1f);
            position.Value = sample;
            Measure("Input.Edit.TouchReflectedFallback", i =>
                OptionalInputSystemAdapter.TryReadTouch(touch, out int id, out Vector2 read, out TouchPhase phase) &&
                id == 4 && read == sample && phase == TouchPhase.Moved, "reads_per_op=1 role=fallback_reference");
        }

        private static void Measure(string caseId, Func<int, bool> operation, string extra)
        {
            int index = 0;
            for (int sample = 0; sample < WarmSamples; sample++)
            {
                Assert.That(RunSample(operation, ref index), Is.EqualTo(OperationsPerSample), caseId + " warm-up oracle");
            }

            long allocationCalls;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = RunSample(operation, ref index);
                allocationCalls = counter.End();
                Assert.That(correct, Is.EqualTo(OperationsPerSample), caseId + " counted-sample oracle");
            }

            double[] nanoseconds = new double[Samples];
            Stopwatch watch = new();
            for (int sample = 0; sample < Samples; sample++)
            {
                watch.Restart();
                int correct = RunSample(operation, ref index);
                watch.Stop();
                Assert.That(correct, Is.EqualTo(OperationsPerSample), caseId + " timed-sample oracle");
                nanoseconds[sample] = watch.Elapsed.TotalMilliseconds * 1000000d / OperationsPerSample;
            }

            NeoPerf.Distribution distribution = NeoPerf.Summarize(nanoseconds, Samples);
            new NeoPerf.Line(caseId)
                .Add("ns_per_op_p50", distribution.P50)
                .Add("ns_per_op_p95", distribution.P95)
                .Add("ns_per_op_p99", distribution.P99)
                .Add("gc_calls_per_op", allocationCalls / (double)OperationsPerSample)
                .Add("ops_per_sample", OperationsPerSample)
                .Add("samples", Samples)
                .AddPairs(extra)
                .Emit();
        }

        private static int RunSample(Func<int, bool> operation, ref int index)
        {
            int correct = 0;
            for (int i = 0; i < OperationsPerSample; i++, index++)
            {
                if (operation(index))
                {
                    correct++;
                }
            }

            return correct;
        }
    }
}
