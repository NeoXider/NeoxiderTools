using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Neo.Tests.Performance
{
    /// <summary>
    ///     Shared helpers of the explicit Performance suites of every test assembly. Each measured case prints exactly
    ///     one parsable line <c>NEO_PERF &lt;case&gt; key=value ...</c> (invariant culture, no spaces inside values) to
    ///     the Unity log and the NUnit output, so successive library passes can be compared by case id.
    /// </summary>
    /// <remarks>
    ///     Timings use <see cref="Stopwatch" /> or a <see cref="ProfilerRecorder" /> on an existing product marker.
    ///     Allocation counts come from an admitted <see cref="GcAllocCounter" />; a count is the number of managed
    ///     allocations, not bytes. Benchmarks assert no timing threshold: they record numbers, while correctness of each
    ///     workload is asserted outside the measured scopes. Every line ends with <c>profiler_enabled=0|1</c>:
    ///     profiler-on runs add marker overhead and compare only with each other. Mark every benchmark
    ///     <c>[Explicit(NeoPerf.ExplicitReason)]</c> and <c>[Category(NeoPerf.Category)]</c> so normal runs skip it.
    /// </remarks>
    public static class NeoPerf
    {
        /// <summary>NUnit category of every benchmark.</summary>
        public const string Category = "Performance";

        /// <summary>Reason text for <c>[Explicit]</c> on every benchmark.</summary>
        public const string ExplicitReason =
            "Performance measurement: run explicitly with -testCategory Performance; prints NEO_PERF lines.";

        /// <summary>First token of every measurement line.</summary>
        public const string LinePrefix = "NEO_PERF";

        /// <summary>Default warm-up batches of <see cref="MeasureSynchronous" />.</summary>
        public const int DefaultWarmupSamples = 8;

        /// <summary>Upper bound of the batches the allocation pass of <see cref="MeasureSynchronous" /> runs.</summary>
        public const int MaxAllocationSamples = 8;

        /// <summary>Empirical distribution of per-sample values, nearest-rank percentiles.</summary>
        public readonly struct Distribution
        {
            public Distribution(int samples, double p50, double p95, double p99, double max, double mean)
            {
                Samples = samples;
                P50 = p50;
                P95 = p95;
                P99 = p99;
                Max = max;
                Mean = mean;
            }

            public int Samples { get; }
            public double P50 { get; }
            public double P95 { get; }
            public double P99 { get; }
            public double Max { get; }
            public double Mean { get; }
        }

        public static Distribution Summarize(double[] values, int count)
        {
            if (values == null || count <= 0)
            {
                return new Distribution(0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            }

            double[] sorted = new double[count];
            Array.Copy(values, sorted, count);
            Array.Sort(sorted);
            double sum = 0d;
            for (int i = 0; i < count; i++)
            {
                sum += sorted[i];
            }

            return new Distribution(count, NearestRank(sorted, 0.50), NearestRank(sorted, 0.95),
                NearestRank(sorted, 0.99), sorted[count - 1], sum / count);
        }

        public static double TicksToMilliseconds(long ticks)
        {
            return ticks * 1000d / Stopwatch.Frequency;
        }

        /// <summary>
        ///     Counts GC.Alloc calls made by <paramref name="body" /> on the calling thread, or returns -1 (after running
        ///     the body) when no <see cref="GcAllocCounter" /> can be admitted in this runtime.
        /// </summary>
        public static long CountAllocations(Action body)
        {
            if (!GcAllocCounter.TryCreate(out GcAllocCounter counter, out string _))
            {
                body();
                return -1;
            }

            using (counter)
            {
                return counter.Count(body);
            }
        }

        /// <summary>
        ///     Samples one <see cref="MeasureSynchronous" /> or <see cref="MeasureSamples(string,int,int,int,int,Action)" />
        ///     runs in total: warm-up, timing and allocation pass (for checksum asserts).
        /// </summary>
        public static int CallsPerMeasure(int warmupSamples, int samples)
        {
            return warmupSamples + samples + Math.Min(samples, MaxAllocationSamples);
        }

        /// <summary>
        ///     Measures a synchronous operation: <paramref name="warmupSamples" /> warm-up batches,
        ///     <paramref name="samples" /> timed batches of <paramref name="operationsPerSample" /> calls, then a separate
        ///     allocation pass of up to <see cref="MaxAllocationSamples" /> batches (so the recorder never sits inside a
        ///     timed sample). The caller adds its own keys and calls <see cref="Line.Emit" />.
        /// </summary>
        public static Line MeasureSynchronous(string caseId, int scale, int operationsPerSample, int warmupSamples,
            int samples, Action operation)
        {
            Assert.That(operation, Is.Not.Null);
            return MeasureSamples(caseId, scale, operationsPerSample, warmupSamples, samples,
                () => RunBatch(operation, operationsPerSample));
        }

        /// <summary>
        ///     <see cref="MeasureSynchronous" /> for a <paramref name="sample" /> delegate that itself performs
        ///     <paramref name="operationsPerSample" /> operations (the value only labels <c>k</c> and per-op keys), with
        ///     <c>scale=1</c> and <see cref="DefaultWarmupSamples" />.
        /// </summary>
        public static Line MeasureSamples(string caseId, int operationsPerSample, int samples, Action sample)
        {
            return MeasureSamples(caseId, 1, operationsPerSample, DefaultWarmupSamples, samples, sample);
        }

        /// <summary>
        ///     <see cref="MeasureSynchronous" /> for a <paramref name="sample" /> delegate that itself performs
        ///     <paramref name="operationsPerSample" /> operations (the value only labels <c>k</c> and per-op keys).
        /// </summary>
        public static Line MeasureSamples(string caseId, int scale, int operationsPerSample, int warmupSamples,
            int samples, Action sample)
        {
            Assert.That(sample, Is.Not.Null);
            Assert.That(operationsPerSample, Is.GreaterThan(0));
            Assert.That(samples, Is.GreaterThan(0));
            for (int i = 0; i < warmupSamples; i++)
            {
                sample();
            }

            GC.Collect();
            double[] milliseconds = new double[samples];
            for (int s = 0; s < samples; s++)
            {
                long start = Stopwatch.GetTimestamp();
                sample();
                milliseconds[s] = TicksToMilliseconds(Stopwatch.GetTimestamp() - start);
            }

            int allocationSamples = Math.Min(samples, MaxAllocationSamples);
            long allocations = CountAllocations(() =>
            {
                for (int s = 0; s < allocationSamples; s++)
                {
                    sample();
                }
            });

            Distribution distribution = Summarize(milliseconds, samples);
            Line line = new Line(caseId)
                .Add("scale", scale)
                .Add("k", operationsPerSample)
                .Add("samples", samples)
                .AddMilliseconds("", distribution)
                .Add("op_p50_us", distribution.P50 * 1000d / operationsPerSample);
            if (allocations < 0)
            {
                line.Add("gc_allocs_per_sample", "unavailable");
            }
            else
            {
                line.Add("gc_allocs_per_sample", (double)allocations / allocationSamples)
                    .Add("gc_allocs_per_op", (double)allocations / ((long)allocationSamples * operationsPerSample));
            }

            line.GcAllocCalls = allocations;
            line.GcAllocSamples = allocationSamples;
            return line;
        }

        /// <summary>
        ///     Prints the positive-control row <c>&lt;fixture&gt;.calibration.gc_allocs_byte4096</c>, which must read
        ///     exactly one GC.Alloc call per sample; otherwise the calling benchmark is ignored and nothing is claimed.
        /// </summary>
        public static void EmitCalibration(string fixture, int samples)
        {
            using (GcAllocCounter.CreateOrIgnore())
            {
            }

            byte[] retained = null;
            Line line = MeasureSamples(fixture + ".calibration.gc_allocs_byte4096", 1, samples,
                () => retained = new byte[GcAllocCounter.CalibrationPayloadLength]);
            line.Emit();
            Assert.That(retained, Has.Length.EqualTo(GcAllocCounter.CalibrationPayloadLength));
            if (line.GcAllocCalls != line.GcAllocSamples)
            {
                Assert.Ignore("NotSupported: the byte[4096] control row read " + line.GcAllocCalls +
                              " GC.Alloc calls for " + line.GcAllocSamples + " samples; no benchmark is run.");
            }
        }

        /// <summary>Result holder of <see cref="EstimateBytesPerOperation" />.</summary>
        public sealed class ByteEstimate
        {
            public bool Available { get; set; }
            public double BytesPerOperation { get; set; }
        }

        /// <summary>
        ///     Estimates managed bytes per call from the <c>GC Allocated In Frame</c> counter: the median of frames that
        ///     run <paramref name="operationsPerFrame" /> calls minus the median of interleaved idle frames. Coroutine
        ///     only (PlayMode or <c>[UnityTest]</c>).
        /// </summary>
        /// <remarks>
        ///     The profiler counter is frame-scoped, so the idle control removes harness noise; validate it with a
        ///     calibration case before trusting small values.
        /// </remarks>
        public static IEnumerator EstimateBytesPerOperation(Action operation, int operationsPerFrame, int frames,
            ByteEstimate result)
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            try
            {
                if (!recorder.Valid)
                {
                    result.Available = false;
                    yield break;
                }

                double[] loaded = new double[frames];
                double[] idle = new double[frames];
                yield return null;
                for (int f = 0; f < frames; f++)
                {
                    RunBatch(operation, operationsPerFrame);
                    yield return null;
                    loaded[f] = recorder.LastValue;
                    yield return null;
                    idle[f] = recorder.LastValue;
                }

                double net = Summarize(loaded, frames).P50 - Summarize(idle, frames).P50;
                result.Available = true;
                result.BytesPerOperation = Math.Max(0d, net) / operationsPerFrame;
            }
            finally
            {
                recorder.Dispose();
            }
        }

        private static void RunBatch(Action operation, int count)
        {
            for (int i = 0; i < count; i++)
            {
                operation();
            }
        }

        private static double NearestRank(double[] sorted, double quantile)
        {
            int rank = (int)Math.Ceiling(quantile * sorted.Length);
            return sorted[Math.Max(0, Math.Min(sorted.Length - 1, rank - 1))];
        }

        /// <summary>Builder of one NEO_PERF line; <see cref="Emit" /> prints it once.</summary>
        public sealed class Line
        {
            private readonly StringBuilder _text = new(256);
            private bool _emitted;

            public Line(string caseId)
            {
                Assert.That(string.IsNullOrEmpty(caseId), Is.False, "A NEO_PERF case id is required.");
                Assert.That(caseId.IndexOf(' ') < 0, Is.True, "Case ids must not contain spaces.");
                _text.Append(LinePrefix).Append(' ').Append(caseId);
            }

            /// <summary>GC.Alloc calls of the allocation pass of <see cref="MeasureSynchronous" />, -1 when unavailable.</summary>
            public long GcAllocCalls { get; internal set; } = -1;

            /// <summary>Batches the allocation pass of <see cref="MeasureSynchronous" /> ran.</summary>
            public int GcAllocSamples { get; internal set; }

            public Line Add(string key, double value)
            {
                _text.Append(' ').Append(key).Append('=')
                    .Append(double.IsNaN(value) ? "nan" : value.ToString("0.######", CultureInfo.InvariantCulture));
                return this;
            }

            public Line Add(string key, long value)
            {
                _text.Append(' ').Append(key).Append('=').Append(value.ToString(CultureInfo.InvariantCulture));
                return this;
            }

            public Line Add(string key, string value)
            {
                _text.Append(' ').Append(key).Append('=').Append(value.Replace(' ', '_'));
                return this;
            }

            /// <summary>
            ///     Appends preformatted space-separated <c>key=value</c> pairs (for example
            ///     <c>"evaluations_per_tick=16 update_subscribed=1"</c>); null or empty appends nothing.
            /// </summary>
            public Line AddPairs(string pairs)
            {
                if (!string.IsNullOrEmpty(pairs))
                {
                    _text.Append(' ').Append(pairs.Trim());
                }

                return this;
            }

            /// <summary>Adds p50/p95/p99/max/mean in milliseconds, keys prefixed by <paramref name="prefix" />.</summary>
            public Line AddMilliseconds(string prefix, Distribution distribution)
            {
                string head = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + "_";
                return Add(head + "p50_ms", distribution.P50)
                    .Add(head + "p95_ms", distribution.P95)
                    .Add(head + "p99_ms", distribution.P99)
                    .Add(head + "max_ms", distribution.Max)
                    .Add(head + "mean_ms", distribution.Mean);
            }

            /// <summary>Appends <c>profiler_enabled</c>, prints the line to the Unity log and NUnit output, returns it.</summary>
            public string Emit()
            {
                Assert.That(_emitted, Is.False, "A NEO_PERF line is printed once.");
                _emitted = true;
                Add("profiler_enabled", Profiler.enabled ? 1L : 0L);
                string line = _text.ToString();
                Debug.Log(line);
                TestContext.WriteLine(line);
                return line;
            }
        }
    }
}
