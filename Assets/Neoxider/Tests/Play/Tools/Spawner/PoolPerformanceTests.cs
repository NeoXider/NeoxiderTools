using System.Collections.Generic;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Explicit benchmarks of the pooling hot path (Performance/Tools.md P4): a warm Get/Return on a
    ///     <see cref="NeoObjectPool" /> with 0, 1 and 10 cached <see cref="IPoolable" /> components, and the same cycle
    ///     through <see cref="SpawnUtility" />'s fallback routing. They print NEO_PERF rows and assert only the workload.
    /// </summary>
    [TestFixture]
    [Category(NeoPerf.Category)]
    [Explicit(NeoPerf.ExplicitReason)]
    public class PoolPerformanceTests
    {
        private const int OperationsPerSample = 100;
        private const int Samples = 120;

        private readonly List<GameObject> _objects = new();
        private bool _previousFallbackPolicy;

        [OneTimeSetUp]
        public void EmitCalibration()
        {
            NeoPerf.EmitCalibration("tools.spawner", NeoPerf.MaxAllocationSamples);
        }

        [SetUp]
        public void SetUp()
        {
            _previousFallbackPolicy = SpawnUtility.DestroyFallbackPoolsOnSceneLoad;
        }

        [TearDown]
        public void TearDown()
        {
            SpawnUtility.ClearFallbackPools();
            SpawnUtility.DestroyFallbackPoolsOnSceneLoad = _previousFallbackPolicy;
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void Performance_PoolWarmGetReturn_ByPoolableCount([Values(0, 1, 10)] int poolables)
        {
            GameObject prefab = CreatePrefab("PoolPerfPrefab", poolables);
            NeoObjectPool pool = new(prefab, 1, true);
            try
            {
                Vector3 position = new(1f, 2f, 3f);
                Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
                NeoPerf.MeasureSynchronous("tools.spawner.pool.warm_get_return.poolables" + poolables, poolables,
                        OperationsPerSample, NeoPerf.DefaultWarmupSamples, Samples, () =>
                        {
                            GameObject rented = pool.GetObject(position, rotation);
                            pool.ReturnObject(rented);
                        })
                    .Add("route", "neo_object_pool")
                    .Emit();

                Assert.That(pool.CountInactive, Is.EqualTo(1), "Every measured rental went back to the pool.");
            }
            finally
            {
                pool.Clear();
            }
        }

        [Test]
        public void Performance_FallbackSpawnDespawn_ByPoolableCount([Values(0, 10)] int poolables)
        {
            Assume.That(PoolManager.I, Is.Null, "The fallback route needs a scene without a PoolManager.");
            GameObject prefab = CreatePrefab("PoolPerfFallbackPrefab", poolables);
            GameObject warm = SpawnUtility.Spawn(prefab);
            Assert.That(warm, Is.Not.Null);
            SpawnUtility.Despawn(warm);
            GameObject last = null;

            NeoPerf.MeasureSynchronous("tools.spawner.fallback.warm_spawn_despawn.poolables" + poolables, poolables,
                    OperationsPerSample, NeoPerf.DefaultWarmupSamples, Samples, () =>
                    {
                        last = SpawnUtility.Spawn(prefab);
                        SpawnUtility.Despawn(last);
                    })
                .Add("route", "spawn_utility_fallback")
                .Emit();

            Assert.That(last, Is.SameAs(warm), "The fallback pool reuses its one warm instance.");
            Assert.That(last.activeSelf, Is.False);
        }

        private GameObject CreatePrefab(string name, int poolables)
        {
            GameObject prefab = new(name);
            prefab.SetActive(false);
            _objects.Add(prefab);
            for (int i = 0; i < poolables; i++)
            {
                GameObject child = new("Poolable" + i);
                child.transform.SetParent(prefab.transform, false);
                child.AddComponent<PoolCallbackProbe>();
            }

            return prefab;
        }
    }
}
