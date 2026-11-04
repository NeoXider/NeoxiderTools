using System;
using System.Collections.Generic;
using System.IO;
using Neo.Tests.Performance;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Self-tests of the shared allocation counter and NEO_PERF writer of Neo.Tests.Performance.
    /// </summary>
    /// <remarks>
    ///     WHY: module allocation guards ignore themselves when the counter is not admitted, so a recorder that stopped
    ///     calibrating in the Editor would quietly turn every guard into an ignored test. The admission test here fails
    ///     instead, which keeps that regression visible in one place.
    /// </remarks>
    [TestFixture]
    public sealed class GcAllocCounterTests
    {
        private static object _sink;

        [TearDown]
        public void TearDown()
        {
            _sink = null;
        }

        [Test]
        public void Counter_IsAdmittedInTheEditor()
        {
            bool admitted = GcAllocCounter.TryCreate(out GcAllocCounter counter, out string failure);
            try
            {
                Assert.That(admitted, Is.True, "The Editor GC.Alloc recorder must calibrate: " + failure);
                Assert.That(failure, Is.Null);
                Assert.That(counter.CalibrationPasses, Is.EqualTo(2));
                Assert.That(counter.PayloadAllocationCount, Is.EqualTo(1));
                Assert.That(counter.RetainedPayloadLength, Is.EqualTo(GcAllocCounter.CalibrationPayloadLength));
            }
            finally
            {
                counter?.Dispose();
            }
        }

        [Test]
        public void Count_SeesEveryAllocationInsideAndNothingOutsideTheWindow()
        {
            Action none = DoNothing;
            Action three = AllocateThree;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                Assert.That(counter.Count(none), Is.Zero);
                _sink = new byte[64];
                Assert.That(counter.Count(three), Is.EqualTo(3));
                Assert.That(counter.Count(none), Is.Zero, "No sample may leak into the next window.");

                counter.Begin();
                AllocateThree();
                long manual = counter.End();
                Assert.That(manual, Is.EqualTo(3), "Begin/End and Count read the same window.");
            }
        }

        [Test]
        public void Counter_RejectsMisuse()
        {
            GcAllocCounter counter = GcAllocCounter.CreateOrIgnore();
            TestDelegate begin = counter.Begin;
            TestDelegate end = () => counter.End();
            Assert.Throws<InvalidOperationException>(end, "End without Begin.");
            counter.Begin();
            Assert.Throws<InvalidOperationException>(begin, "Nested windows are not supported.");
            counter.End();
            Assert.Throws<ArgumentNullException>(() => counter.Count(null));
            counter.Dispose();
            Assert.Throws<InvalidOperationException>(begin, "A disposed counter opens no window.");
            Assert.DoesNotThrow(counter.Dispose, "Dispose is idempotent.");
        }

        [Test]
        public void GcAllocHelpers_CountBoundAndTryCount()
        {
            Action three = AllocateThree;
            Assert.That(GcAlloc.Count(three), Is.EqualTo(3));
            GcAlloc.AssertAtMost(3, three, "Three allocations fit a bound of three.");
            GcAlloc.AssertNone(DoNothing, "An empty body allocates nothing.");

            long calls = GcAlloc.TryCount(three, out string notAdmitted);
            Assert.That(_sink, Is.TypeOf<int[]>(), "TryCount runs the body.");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.EqualTo(3));
        }

        [Test]
        public void NeoPerfLine_PrintsOneInvariantKeyValueLine()
        {
            string text = new NeoPerf.Line("tests.neo_perf.format")
                .Add("k", 3)
                .Add("ms", 1.5)
                .Add("label", "a b")
                .AddPairs("extra=1 role=control")
                .Emit();

            Assert.That(text, Does.Match(
                @"^NEO_PERF tests\.neo_perf\.format k=3 ms=1\.5 label=a_b extra=1 role=control profiler_enabled=[01]$"));
        }

        [Test]
        public void MeasureSynchronous_RunsEveryPassAndCountsAllocationsPerOperation()
        {
            int calls = 0;
            NeoPerf.Line line = NeoPerf.MeasureSynchronous("tests.neo_perf.measure", 1, 4, 2, 3, () =>
            {
                calls++;
                _sink = new object();
            });

            Assert.That(calls, Is.EqualTo(NeoPerf.CallsPerMeasure(2, 3) * 4));
            Assert.That(line.GcAllocSamples, Is.EqualTo(3));
            if (line.GcAllocCalls < 0)
            {
                Assert.Ignore(GcAllocCounter.NotAdmittedMessage("MeasureSynchronous reported unavailable"));
            }

            Assert.That(line.GcAllocCalls, Is.EqualTo(12), "3 allocation samples x 4 operations x 1 object.");
            Assert.That(line.Emit(), Does.Contain(" gc_allocs_per_op=1 "));
        }

        [Test]
        public void TestSources_DoNotMeasureWithTheManagedByteCounter()
        {
            string testsRoot = Path.Combine(Audit20261023NeoEditorTestsPaths.PackageDiskRoot, "Tests");
            Assume.That(Directory.Exists(testsRoot), Is.True, "The package Tests folder is not on disk: " + testsRoot);
            string call = "GetAllocatedBytesForCurrentThread" + "()";
            List<string> offenders = new();
            foreach (string file in Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (File.ReadAllText(file).Contains(call))
                {
                    offenders.Add(file.Substring(testsRoot.Length + 1).Replace('\\', '/'));
                }
            }

            Assert.That(offenders, Is.Empty,
                "The managed byte counter reads 0 in the Unity 6 Editor; use GcAlloc or GcAllocCounter instead.");
        }

        private static void DoNothing()
        {
        }

        private static void AllocateThree()
        {
            _sink = new object();
            _sink = new byte[8];
            _sink = new int[2];
        }
    }
}
