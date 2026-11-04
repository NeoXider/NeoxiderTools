using System.Collections.Generic;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     <see cref="NeoObjectPool" /> contracts that need a runtime <see cref="IPoolable" /> probe: the per-instance
    ///     callback cache on <see cref="PooledObjectInfo" />, idempotent returns, destroyed cached callbacks and an
    ///     allocation-free warm Get/Return cycle that runs real callbacks.
    /// </summary>
    /// <remarks>
    ///     WHY: these cases moved here from the EditMode PoolingHotPathTests, whose Editor-only probe Unity refused to
    ///     attach, which left the cache test failing with a null probe and the warm-cycle guard without callbacks.
    /// </remarks>
    [TestFixture]
    public class PoolCallbackCacheTests
    {
        private const int Cycles = 1000;

        // WHY: Counts GC.Alloc calls, not bytes. A per-cycle allocation over Cycles cycles costs at least Cycles calls;
        // a tenth of that tolerates one-off editor bookkeeping while still failing on any per-cycle cost.
        private const long MaxAllocationsPerWindow = Cycles / 10;

        private readonly List<GameObject> _objects = new();
        private readonly List<NeoObjectPool> _pools = new();
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _prefab = CreateObject("PoolCallbackCachePrefab");
            _prefab.SetActive(false);
            _prefab.AddComponent<PoolCallbackProbe>();
            GameObject child = new("InactiveProbeChild");
            child.transform.SetParent(_prefab.transform, false);
            child.SetActive(false);
            child.AddComponent<PoolCallbackProbe>();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (NeoObjectPool pool in _pools)
                {
                    pool.Clear();
                }

                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }
            }
            finally
            {
                _pools.Clear();
                _objects.Clear();
            }
        }

        [Test]
        public void PoolableCallbacks_UseThePerInstanceCache()
        {
            NeoObjectPool pool = CreatePool(0);
            GameObject rental = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(rental);
            PoolCallbackProbe[] probes = rental.GetComponentsInChildren<PoolCallbackProbe>(true);
            Assert.That(probes.Length, Is.EqualTo(2), "The root probe and the inactive child probe are both cloned.");
            PoolCallbackProbe lateProbe = rental.AddComponent<PoolCallbackProbe>();

            pool.ReturnObject(rental);
            pool.ReturnObject(rental);
            GameObject reused = pool.GetObject(Vector3.zero, Quaternion.identity);

            Assert.That(reused, Is.SameAs(rental));
            foreach (PoolCallbackProbe probe in probes)
            {
                Assert.That(probe.Created, Is.EqualTo(1), probe.name + " OnPoolCreate");
                Assert.That(probe.Got, Is.EqualTo(2), probe.name + " OnPoolGet");
                Assert.That(probe.Released, Is.EqualTo(1),
                    probe.name + ": the ignored second return must not call OnPoolRelease.");
            }

            Assert.That(lateProbe.Created + lateProbe.Got + lateProbe.Released, Is.Zero,
                "The IPoolable set is cached at creation; a component added to a rental later is not searched for.");
            Assert.That(rental.GetComponent<PooledObjectInfo>().SpawnGeneration, Is.EqualTo(2));
            Assert.That(pool.Prefab, Is.SameAs(_prefab));
        }

        [Test]
        public void PoolableCallbacks_DestroyedCachedComponent_IsSkipped()
        {
            NeoObjectPool pool = CreatePool(0);
            GameObject rental = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(rental);
            PoolCallbackProbe[] probes = rental.GetComponentsInChildren<PoolCallbackProbe>(true);
            Assert.That(probes.Length, Is.EqualTo(2));
            PoolCallbackProbe destroyed = probes[0];
            PoolCallbackProbe survivor = probes[1];
            Object.DestroyImmediate(destroyed);

            pool.ReturnObject(rental);
            GameObject reused = pool.GetObject(Vector3.zero, Quaternion.identity);

            Assert.That(reused, Is.SameAs(rental));
            Assert.That(destroyed.Released, Is.Zero, "A destroyed cached IPoolable must not be called.");
            Assert.That(destroyed.Got, Is.EqualTo(1));
            Assert.That(survivor.Released, Is.EqualTo(1));
            Assert.That(survivor.Got, Is.EqualTo(2));
        }

        [Test]
        public void WarmGetReturnCycle_WithPoolableCallbacks_DoesNotAllocatePerCycle()
        {
            NeoObjectPool pool = CreatePool(1);
            Vector3 position = new(1f, 2f, 3f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
            GameObject warm = pool.GetObject(position, rotation);
            PoolCallbackProbe probe = warm.GetComponent<PoolCallbackProbe>();
            pool.ReturnObject(warm);
            int gotBefore = probe.Got;

            long allocations;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                for (int i = 0; i < Cycles; i++)
                {
                    GameObject rented = pool.GetObject(position, rotation);
                    pool.ReturnObject(rented);
                }

                allocations = counter.End();
            }

            Assert.That(probe.Got - gotBefore, Is.EqualTo(Cycles), "Every warm cycle runs the cached callbacks.");
            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(allocations, Is.LessThan(MaxAllocationsPerWindow),
                "A warm Get/Return with IPoolable callbacks must not allocate per cycle.");
        }

        private NeoObjectPool CreatePool(int initialSize)
        {
            NeoObjectPool pool = new(_prefab, initialSize, true);
            _pools.Add(pool);
            return pool;
        }

        private GameObject CreateObject(string name)
        {
            GameObject created = new(name);
            _objects.Add(created);
            return created;
        }
    }
}
