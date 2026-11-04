using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Core.Resources;
using Neo.Reactive;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Library hot paths without an owner suite of their own: Reactive notification, ResourcePool ticks and the
    ///     shared interaction query backend, plus a GC-byte calibration case. Each case drives public APIs read-only,
    ///     checks outside the measured scope that the workload is the intended one, and prints one NEO_PERF line.
    /// </summary>
    /// <remarks>
    ///     Grid (Match3 TryFindValidSwap) and Selector getter baselines of the 2026-11-02 audit are measured by their
    ///     owners' suites, Tests/Edit/GridSystem/Match3SearchPerformanceTests.cs and
    ///     Tests/Edit/Tools/SelectorPerformanceTests.cs, so one Performance run prints a single set of those numbers.
    ///     <c>gc_allocs_per_sample</c> counts managed allocations; <c>gc_bytes_per_op_est</c> estimates their bytes
    ///     from the frame-scoped profiler counter (validate it with <c>perf.calibration.gc_bytes_4096</c>).
    /// </remarks>
    public sealed class Audit20261102LibraryHotpathMeasurementTests
    {
        private const int Samples = 120;
        private const int WarmupSamples = 8;
        private const int ByteFrames = 15;
        private const int ReactiveWrites = 100;
        private const int PoolTicks = 100;
        private const int QueryCalls = 100;
        private static readonly Vector3 QueryOrigin = new(-9600f, 9600f, 0f);
        private readonly List<GameObject> _owned = new();
        private int _sink;
        private byte[] _retained;

        [UnityTearDown]
        public IEnumerator DestroyOwned()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i] != null)
                {
                    Object.Destroy(_owned[i]);
                }
            }

            _owned.Clear();
            _retained = null;
            InteractionPhysicsQueries.Invalidate();
            yield return null;
        }

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Calibration_GcBytes4096() => MeasureCalibration();

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Reactive_FloatNotify_0() => MeasureReactive(0);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Reactive_FloatNotify_1() => MeasureReactive(1);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Reactive_FloatNotify_16() => MeasureReactive(16);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Reactive_FloatNotify_128() => MeasureReactive(128);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Core_ResourcePoolTick_1() => MeasureResourcePools(1);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Core_ResourcePoolTick_8() => MeasureResourcePools(8);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Core_ResourcePoolTick_64() => MeasureResourcePools(64);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_QueryBackend_Raycast3D() => MeasureQueryBackend();

        private IEnumerator MeasureCalibration()
        {
            Action allocate = () => _retained = new byte[4096];
            NeoPerf.Line line = NeoPerf.MeasureSynchronous("perf.calibration.gc_bytes_4096", 4096, 16, WarmupSamples,
                Samples, allocate);
            yield return EmitWithBytes(line, allocate, 16);
            Assert.That(_retained.Length, Is.EqualTo(4096));
        }

        private IEnumerator MeasureReactive(int listeners)
        {
            ReactivePropertyFloat property = new(0f);
            // WHY: AddListener ignores a delegate that is already subscribed, so every listener needs its own target.
            DeliveryCounter[] counters = new DeliveryCounter[listeners];
            for (int i = 0; i < listeners; i++)
            {
                counters[i] = new DeliveryCounter();
                property.AddListener(counters[i].OnValue);
            }

            float next = 0f;
            long writes = 0;
            Action write = () =>
            {
                next = next >= 1000000f ? 0f : next + 1f;
                property.Value = next;
                writes++;
            };
            yield return EmitWithBytes(NeoPerf.MeasureSynchronous("reactive.float.set_notify.listeners" + listeners,
                listeners, ReactiveWrites, WarmupSamples, Samples, write), write, ReactiveWrites);
            long delivered = 0;
            foreach (DeliveryCounter counter in counters)
            {
                delivered += counter.Count;
            }

            Assert.That(delivered, Is.EqualTo(writes * listeners), "Every changed value reaches every listener once.");
        }

        private sealed class DeliveryCounter
        {
            public long Count;

            public void OnValue(float value)
            {
                Count++;
            }
        }

        private IEnumerator MeasureResourcePools(int poolCount)
        {
            ResourcePoolModel model = new();
            for (int i = 0; i < poolCount; i++)
            {
                model.AddPool("pool" + i, new ResourcePoolEntry
                {
                    Current = 100f,
                    Max = 100f,
                    RegenPerSecond = 5f,
                    RegenInterval = 0.1f,
                    HealAmount = 1f,
                    HealDelay = 0.5f,
                    MaxDecreaseAmount = -1f,
                    MaxIncreaseAmount = -1f
                });
            }

            int changes = 0;
            model.OnResourceChanged += (_, _, _) => changes++;
            Action tick = () => model.Tick(0.016f);
            yield return EmitWithBytes(NeoPerf.MeasureSynchronous("core.resource_pool.tick_full.pools" + poolCount,
                poolCount, PoolTicks, WarmupSamples, Samples, tick), tick, PoolTicks);
            Assert.That(changes, Is.Zero, "Full pools complete regen cycles without publishing unchanged values.");
            Assert.That(model.GetCurrent("pool0"), Is.EqualTo(100f));
        }

        private IEnumerator MeasureQueryBackend()
        {
            GameObject wall = Own("PerfQueryWall");
            wall.transform.position = QueryOrigin + Vector3.forward * 4f;
            wall.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
            Physics.SyncTransforms();
            Ray ray = new(QueryOrigin, Vector3.forward);
            InteractionPhysicsQueries.Hits3D probe = InteractionPhysicsQueries.Raycast3D(null, ray, 10f, ~0,
                QueryTriggerInteraction.Collide);
            Assert.That(probe.IsComplete && probe.Count == 1, Is.True, "One real collider on the measured ray.");

            Action shared = () => _sink += InteractionPhysicsQueries.Raycast3D(null, ray, 10f, ~0,
                QueryTriggerInteraction.Collide, InteractionPhysicsQueries.QueryMode.Shared).Count;
            Action fresh = () => _sink += InteractionPhysicsQueries.Raycast3D(null, ray, 10f, ~0,
                QueryTriggerInteraction.Collide).Count;
            yield return EmitWithBytes(NeoPerf.MeasureSynchronous("interaction.queries.raycast3d.shared_hit", 1,
                QueryCalls, WarmupSamples, Samples, shared), shared, QueryCalls);
            yield return EmitWithBytes(NeoPerf.MeasureSynchronous("interaction.queries.raycast3d.fresh", 1,
                QueryCalls, WarmupSamples, Samples, fresh), fresh, QueryCalls);
        }

        private static IEnumerator EmitWithBytes(NeoPerf.Line line, Action operation, int operationsPerFrame)
        {
            NeoPerf.ByteEstimate bytes = new();
            yield return NeoPerf.EstimateBytesPerOperation(operation, operationsPerFrame, ByteFrames, bytes);
            if (bytes.Available)
            {
                line.Add("gc_bytes_per_op_est", bytes.BytesPerOperation);
            }
            else
            {
                line.Add("gc_bytes_per_op_est", "unavailable");
            }

            line.Emit();
        }

        private GameObject Own(string name)
        {
            GameObject instance = new(name);
            _owned.Add(instance);
            return instance;
        }
    }
}
