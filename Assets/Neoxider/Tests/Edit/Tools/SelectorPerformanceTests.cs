using System.Collections.Generic;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    /// <summary>
    ///     EditMode timing benchmark for Selector reads and commands on the 2026-11-02 baseline pool. Prints one
    ///     <c>NEO_PERF</c> line per metric through <see cref="NeoPerf"/>; asserts outcomes only, never
    ///     timing.
    /// </summary>
    /// <remarks>
    ///     One sample is 100 operations (for getters, 100 groups of IsAtStart + IsAtEnd + CountActive), the unit of the
    ///     baseline capture. <c>legacy_emulated</c> rows repeat the pre-optimization getter work (a full
    ///     <see cref="SelectorModel.Configure"/> with set copies per getter) through public APIs, so they give a
    ///     same-harness before/after for CPU and GC.Alloc calls. <c>gc_allocs_per_sample</c> counts the same GC.Alloc
    ///     events as the call column of Docs/Audits/2026-11-02/Performance/MeasuredGridSelectorBaseline.md; the
    ///     authoritative timing comparison with that PlayMode Profiler baseline is the PlayMode producer
    ///     <c>Neo.Tests.Play.Audit20261102LibraryHotpathMeasurementTests</c>, not these EditMode Stopwatch numbers.
    /// </remarks>
    [TestFixture]
    [Category(NeoPerf.Category)]
    [Explicit(NeoPerf.ExplicitReason)]
    public sealed class SelectorPerformanceTests
    {
        private const int OperationsPerSample = 100;
        private const int Samples = 120;

        private readonly List<GameObject> _objects = new();

        [OneTimeSetUp]
        public void AdmitAllocationProbe()
        {
            NeoPerf.EmitCalibration("view.selector", Samples);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Getters_BaselinePool(int count)
        {
            Selector selector = CreateSelector(false);
            int current = ConfigureMeasuredPool(selector, count);
            SelectorModel model = selector.CreateModelSnapshot();
            SelectorModel legacy = new();
            HashSet<int> excluded = new(selector.ExcludedIndices);
            HashSet<int> used = new(selector.UsedIndicesForUnique);
            int checksum = 0;

            string pool = "view.selector.getters.n" + count;
            NeoPerf.MeasureSamples(pool + ".component", OperationsPerSample, Samples, () =>
            {
                for (int group = 0; group < OperationsPerSample; group++)
                {
                    checksum += Group(selector.IsAtStart, selector.IsAtEnd, selector.CountActive);
                }
            }).Emit();
            NeoPerf.MeasureSamples(pool + ".pure_model", OperationsPerSample, Samples, () =>
            {
                for (int group = 0; group < OperationsPerSample; group++)
                {
                    checksum += Group(model.IsAtStart, model.IsAtEnd, model.GetLogicalActiveCount());
                }
            }).Emit();
            NeoPerf.MeasureSamples(pool + ".component.legacy_emulated", OperationsPerSample, Samples, () =>
            {
                for (int group = 0; group < OperationsPerSample; group++)
                {
                    LegacyConfigure(legacy, selector, excluded, used);
                    bool atStart = legacy.IsAtStart;
                    LegacyConfigure(legacy, selector, excluded, used);
                    bool atEnd = legacy.IsAtEnd;
                    LegacyConfigure(legacy, selector, excluded, used);
                    checksum += Group(atStart, atEnd, legacy.GetLogicalActiveCount());
                }
            }).Emit();

            int expectedGroup = Group(current == 0, current == count - 1, current + 1);
            int groups = NeoPerf.CallsPerMeasure(NeoPerf.DefaultWarmupSamples, Samples) * OperationsPerSample * 3;
            Assert.That(checksum, Is.EqualTo(unchecked(expectedGroup * groups)));
            Assert.That(selector.Value, Is.EqualTo(current));
        }

        [TestCase(1, false)]
        [TestCase(100, false)]
        [TestCase(1000, false)]
        [TestCase(1000, true)]
        public void Set_BaselinePool(int count, bool autoUpdateFromChildren)
        {
            Selector selector = CreateSelector(autoUpdateFromChildren);
            ConfigureMeasuredPool(selector, count);
            int next = 0;

            NeoPerf.MeasureSamples(
                "view.selector.set.n" + count + (autoUpdateFromChildren ? ".auto_update_children" : string.Empty),
                OperationsPerSample, Samples, () =>
                {
                    for (int operation = 0; operation < OperationsPerSample; operation++)
                    {
                        selector.Set(next);
                        next = next + 1 == count ? 0 : next + 1;
                    }
                }).Emit();

            Assert.That(selector.Value, Is.EqualTo((next + count - 1) % count));
            Assert.That(selector.Count, Is.EqualTo(count));
        }

        private Selector CreateSelector(bool autoUpdateFromChildren)
        {
            GameObject owner = new("SelectorPerformance");
            _objects.Add(owner);
            owner.SetActive(false);
            Selector selector = owner.AddComponent<Selector>();
            JsonUtility.FromJsonOverwrite("{\"_uniqueSelectionMode\":true,\"_resetUniqueWhenCycleComplete\":false," +
                                          "\"_saveEnabled\":false,\"_autoUpdateFromChildren\":" +
                                          (autoUpdateFromChildren ? "true" : "false") + "," +
                                          "\"_controlGameObjectActive\":false,\"_loop\":false," +
                                          "\"_allowEmptyEffectiveIndex\":false,\"_changeDebug\":false}", selector);
            selector.startOnAwake = false;
            selector.isNetworked = false;
            return selector;
        }

        /// <summary>
        ///     The workload of the 2026-11-02 baseline: fill mode, every fourth index excluded, a sparse unique history
        ///     and the middle index selected.
        /// </summary>
        private static int ConfigureMeasuredPool(Selector selector, int count)
        {
            selector.Count = count;
            selector.FillMode = true;
            selector.IndexOffset = 0;
            List<int> excluded = new();
            for (int i = 0; i < count; i += 4)
            {
                excluded.Add(i);
            }

            selector.SetExcludedIndices(excluded);
            for (int i = count == 1 ? 0 : 1; i < count; i += 4)
            {
                selector.Set(i);
            }

            int current = count / 2;
            selector.Set(current);
            return current;
        }

        private static void LegacyConfigure(SelectorModel legacy, Selector selector, HashSet<int> excluded,
            HashSet<int> used)
        {
            legacy.Configure(selector.Count, selector.Value, selector.IndexOffset, false, selector.FillMode, false,
                selector.UniqueSelectionMode, false, excluded, used);
        }

        private static int Group(bool atStart, bool atEnd, int active)
        {
            return (atStart ? 1 : 0) + (atEnd ? 2 : 0) + active * 4;
        }
    }
}
