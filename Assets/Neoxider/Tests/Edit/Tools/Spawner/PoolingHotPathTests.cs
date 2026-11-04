using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Hot-path contracts of the pooling layer: allocation-free warm Get/Return, idempotent returns without the
    ///     inner collection scan, scene-load cleanup of fallback pools and the Spawner's allocation-free area sampling
    ///     and duplicate-free tracking.
    /// </summary>
    /// <remarks>
    ///     WHY: the IPoolable callback cases (per-instance cache, warm cycle with callbacks) need a runtime probe
    ///     MonoBehaviour, which Unity refuses to attach from this Editor-only assembly; they live in the Play test
    ///     Neo.Tests.Play.PoolCallbackCacheTests.
    /// </remarks>
    [TestFixture]
    public class PoolingHotPathTests
    {
        private const int Cycles = 1000;

        // WHY: Counts GC.Alloc calls, not bytes. A per-call allocation over Cycles calls costs at least Cycles
        // calls; a tenth of that tolerates one-off editor bookkeeping while still failing on any per-call cost.
        private const long MaxAllocationsPerWindow = Cycles / 10;

        private static readonly FieldInfo SingletonInstanceField =
            typeof(Singleton<PoolManager>).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo SingletonSearchFailedField =
            typeof(Singleton<PoolManager>).GetField("_searchFailed", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly List<GameObject> _objects = new();
        private readonly List<NeoObjectPool> _pools = new();
        private GameObject _prefab;
        private object _previousManager;
        private object _previousSearchFailed;
        private bool _previousCreateInstance;
        private bool _previousFallbackPolicy;

        [SetUp]
        public void SetUp()
        {
            Assert.That(SingletonInstanceField, Is.Not.Null, "Singleton<PoolManager>._instance expected");
            Assert.That(SingletonSearchFailedField, Is.Not.Null, "Singleton<PoolManager>._searchFailed expected");
            _previousManager = SingletonInstanceField.GetValue(null);
            _previousSearchFailed = SingletonSearchFailedField.GetValue(null);
            _previousCreateInstance = PoolManager.CreateInstance;
            _previousFallbackPolicy = SpawnUtility.DestroyFallbackPoolsOnSceneLoad;

            try
            {
                // WHY: These tests exercise the fallback routing; a PoolManager leaked by an earlier test (or found
                // by a scene search) would silently route spawns through the manager instead.
                SingletonInstanceField.SetValue(null, null);
                SingletonSearchFailedField.SetValue(null, true);
                PoolManager.CreateInstance = false;
                Assert.That(PoolManager.I, Is.Null, "Fallback routing requires no resolvable PoolManager.");

                SpawnUtility.DestroyFallbackPoolsOnSceneLoad = true;
                SpawnUtility.ClearFallbackPools();
                _prefab = CreatePrefab("PoolingHotPathPrefab");
            }
            catch
            {
                // WHY: NUnit skips TearDown when SetUp fails, so the shared statics are restored here.
                RestoreStatics();
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                SpawnUtility.ClearFallbackPools();
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
                RestoreStatics();
            }
        }

        private void RestoreStatics()
        {
            SpawnUtility.DestroyFallbackPoolsOnSceneLoad = _previousFallbackPolicy;
            PoolManager.CreateInstance = _previousCreateInstance;
            SingletonInstanceField.SetValue(null, _previousManager);
            SingletonSearchFailedField.SetValue(null, _previousSearchFailed);
        }

        [Test]
        public void AllocationCounter_SeesEveryDeliberateAllocation()
        {
            Assert.That(AllocationCounter.Count(AllocationCounter.AllocatePerCall, Cycles),
                Is.GreaterThanOrEqualTo(Cycles), "A per-call allocation must exceed MaxAllocationsPerWindow.");
            Assert.That(AllocationCounter.Count(_ => { }, Cycles), Is.LessThan(MaxAllocationsPerWindow));
        }

        [Test]
        public void WarmGetReturnCycle_DoesNotAllocatePerCall()
        {
            AllocationCounter.RequireWorkingCounter(Cycles);
            NeoObjectPool pool = CreatePool(1);
            Vector3 position = new(1f, 2f, 3f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
            GameObject warm = pool.GetObject(position, rotation);
            pool.ReturnObject(warm);

            long allocations = AllocationCounter.Count(_ =>
            {
                GameObject rented = pool.GetObject(position, rotation);
                pool.ReturnObject(rented);
            }, Cycles);

            Assert.That(allocations, Is.LessThan(MaxAllocationsPerWindow));
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void DoubleReturn_StoresTheInstanceOnlyOnce()
        {
            NeoObjectPool pool = CreatePool(0);
            GameObject rental = pool.GetObject(Vector3.zero, Quaternion.identity);

            pool.ReturnObject(rental);
            pool.ReturnObject(rental);

            Assert.That(pool.CountInactive, Is.EqualTo(1));
            GameObject first = pool.GetObject(Vector3.zero, Quaternion.identity);
            GameObject second = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(first);
            _objects.Add(second);
            Assert.That(first, Is.SameAs(rental));
            Assert.That(second, Is.Not.SameAs(first), "A double return must not hand one instance out twice.");
            Assert.That(first.activeSelf && second.activeSelf, Is.True);
        }

        [Test]
        public void DoubleReturn_AdvancesTheSpawnGenerationOncePerRental()
        {
            NeoObjectPool pool = CreatePool(0);
            GameObject rental = pool.GetObject(Vector3.zero, Quaternion.identity);

            pool.ReturnObject(rental);
            pool.ReturnObject(rental);
            GameObject reused = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(reused);

            Assert.That(reused, Is.SameAs(rental));
            Assert.That(rental.GetComponent<PooledObjectInfo>().SpawnGeneration, Is.EqualTo(2));
            Assert.That(pool.Prefab, Is.SameAs(_prefab));
        }

        [Test]
        public void SceneLoaded_KeepsTheFallbackRootThatTheLoadedSceneCreated()
        {
            GameObject rental = SpawnUtility.Spawn(_prefab);
            Assert.That(rental, Is.Not.Null);
            Transform root = rental.transform.parent;
            NeoObjectPool owner = rental.GetComponent<PooledObjectInfo>().OwnerPool;

            InvokeSceneLoaded(root.gameObject.scene);

            Assert.That(root != null, Is.True, "A root created by the incoming scene must survive its sceneLoaded.");
            Assert.That(rental != null && rental.activeSelf, Is.True);
            GameObject next = SpawnUtility.Spawn(_prefab);
            Assert.That(next.GetComponent<PooledObjectInfo>().OwnerPool, Is.SameAs(owner));
        }

        [Test]
        public void ClearFallbackPools_RetiresThePools_SoALateExternalReturnIsDestroyed()
        {
            Transform externalOwner = CreateObject("PoolingHotPathExternalOwner").transform;
            GameObject rental = SpawnUtility.Spawn(_prefab, Vector3.zero, Quaternion.identity, externalOwner);
            NeoObjectPool owner = rental.GetComponent<PooledObjectInfo>().OwnerPool;
            Assert.That(rental.transform.parent, Is.SameAs(externalOwner), "positive control: the rental is outside");

            SpawnUtility.ClearFallbackPools();

            Assert.That(owner.IsRetired, Is.True);
            Assert.That(rental != null && rental.activeSelf, Is.True, "A rental still out survives the clear.");
            SpawnUtility.Despawn(rental);
            Assert.That(rental == null, Is.True,
                "A late return must not park the rental inactive in a pool nobody can reach any more.");
            GameObject next = SpawnUtility.Spawn(_prefab);
            Assert.That(next, Is.Not.Null, "The prefab stays spawnable after a clear.");
            Assert.That(next.GetComponent<PooledObjectInfo>().OwnerPool, Is.Not.SameAs(owner));
        }

        [Test]
        public void SceneLoaded_ForAnotherScene_StillClearsTheFallbackPools()
        {
            GameObject rental = SpawnUtility.Spawn(_prefab);
            Transform root = rental.transform.parent;
            NeoObjectPool owner = rental.GetComponent<PooledObjectInfo>().OwnerPool;

            InvokeSceneLoaded(default);

            Assert.That(root == null, Is.True);
            Assert.That(rental == null, Is.True);
            GameObject next = SpawnUtility.Spawn(_prefab);
            Assert.That(next.GetComponent<PooledObjectInfo>().OwnerPool, Is.Not.SameAs(owner));
        }

        [Test]
        public void SceneLoaded_DropsOnlyPoolsWhoseStorageDiedWithThePreviousScene()
        {
            GameObject oldSource = CreatePrefab("PoolingHotPathOldScenePrefab");
            GameObject oldRental = SpawnUtility.Spawn(oldSource);
            NeoObjectPool oldPool = oldRental.GetComponent<PooledObjectInfo>().OwnerPool;
            Object.DestroyImmediate(oldRental.transform.parent.gameObject);

            GameObject freshRental = SpawnUtility.Spawn(_prefab);
            NeoObjectPool freshPool = freshRental.GetComponent<PooledObjectInfo>().OwnerPool;
            Transform liveRoot = freshRental.transform.parent;

            InvokeSceneLoaded(liveRoot.gameObject.scene);

            Assert.That(freshRental != null && freshRental.activeSelf, Is.True);
            Assert.That(oldPool.IsRetired, Is.True, "A pool whose storage died is retired, not just cleared.");
            Assert.That(SpawnUtility.Spawn(_prefab).GetComponent<PooledObjectInfo>().OwnerPool, Is.SameAs(freshPool));
            Assert.That(SpawnUtility.Spawn(oldSource).GetComponent<PooledObjectInfo>().OwnerPool,
                Is.Not.SameAs(oldPool));
        }

        [Test]
        public void Spawn_BeforeSceneLoaded_ReplacesAPoolWhoseStorageDied_AndReusesItsReturns()
        {
            GameObject source = CreatePrefab("PoolingHotPathReloadedScenePrefab");
            GameObject oldRental = SpawnUtility.Spawn(source);
            NeoObjectPool oldPool = oldRental.GetComponent<PooledObjectInfo>().OwnerPool;
            // WHY: Simulates a Single load: the previous scene's fallback root dies with every rental parented under
            // it while the registered pool still points at it, then the incoming scene spawns the same source from
            // its Awake/OnEnable, before sceneLoaded runs.
            Object.DestroyImmediate(oldRental.transform.parent.gameObject);
            GameObject rental = SpawnUtility.Spawn(source);
            NeoObjectPool pool = rental.GetComponent<PooledObjectInfo>().OwnerPool;
            Transform liveRoot = rental.transform.parent;

            Assert.That(pool, Is.Not.SameAs(oldPool), "A pool with dead storage must not be reused.");
            Assert.That(oldPool.IsRetired, Is.True);
            Assert.That(liveRoot != null, Is.True);

            InvokeSceneLoaded(liveRoot.gameObject.scene);
            int inactiveBefore = pool.CountInactive;
            SpawnUtility.Despawn(rental);

            Assert.That(rental != null && !rental.activeSelf, Is.True);
            Assert.That(rental.transform.parent, Is.SameAs(liveRoot), "The return is stored under the live root.");
            Assert.That(pool.CountInactive, Is.EqualTo(inactiveBefore + 1));
            Assert.That(SpawnUtility.Spawn(source).GetComponent<PooledObjectInfo>().OwnerPool, Is.SameAs(pool));
        }

        [Test]
        public void SpawnerAreaSampling_DoesNotAllocatePerCall()
        {
            Spawner spawner = CreateSpawner();
            BoxCollider area = CreateObject("PoolingHotPathArea").AddComponent<BoxCollider>();
            area.size = new Vector3(4f, 2f, 6f);
            SetPrivate(spawner, "_spawnAreaCollider", area);
            Bounds bounds = new(area.transform.position, area.size);
            AllocationCounter.RequireWorkingCounter(Cycles);
            spawner.GetSpawnPosition();

            Vector3 last = Vector3.zero;
            long allocations = AllocationCounter.Count(_ => last = spawner.GetSpawnPosition(), Cycles);

            Assert.That(allocations, Is.LessThan(MaxAllocationsPerWindow));
            Assert.That(bounds.Contains(last), Is.True);
        }

        [Test]
        public void Spawner_GetActiveObjectCount_DoesNotAllocatePerCall()
        {
            const int spawned = 8;
            Spawner spawner = CreateSpawner();
            for (int i = 0; i < spawned; i++)
            {
                _objects.Add(spawner.SpawnObject(_prefab, Vector3.zero, Quaternion.identity, null));
            }

            AllocationCounter.RequireWorkingCounter(Cycles);
            Assert.That(spawner.GetActiveObjectCount(), Is.EqualTo(spawned));

            int last = -1;
            long allocations = AllocationCounter.Count(_ => last = spawner.GetActiveObjectCount(), Cycles);

            Assert.That(allocations, Is.LessThan(MaxAllocationsPerWindow),
                "Counting active spawns prunes and counts in place (no LINQ, no copies).");
            Assert.That(last, Is.EqualTo(spawned));
        }

        [Test]
        public void Spawner_ReusedPoolRental_IsTrackedOnce()
        {
            Spawner spawner = CreateSpawner();

            GameObject first = spawner.SpawnObject(_prefab, Vector3.zero, Quaternion.identity, null);
            SpawnUtility.Despawn(first);
            GameObject second = spawner.SpawnObject(_prefab, Vector3.zero, Quaternion.identity, null);

            Assert.That(second, Is.SameAs(first));
            Assert.That(spawner.SpawnedObjects.Count, Is.EqualTo(1));
            Assert.That(spawner.GetActiveObjectCount(), Is.EqualTo(1));
        }

        [Test]
        public void Spawner_InstantiatePath_CountsOnlyActiveSpawns()
        {
            Spawner spawner = CreateSpawner();
            using (SerializedObject serialized = new(spawner))
            {
                serialized.FindProperty("_useObjectPool").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject first = spawner.SpawnObject(_prefab, Vector3.zero, Quaternion.identity, null);
            GameObject second = spawner.SpawnObject(_prefab, Vector3.zero, Quaternion.identity, null);
            _objects.Add(first);
            _objects.Add(second);
            first.SetActive(true);
            second.SetActive(false);

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(spawner.SpawnedObjects.Count, Is.EqualTo(2));
            Assert.That(spawner.GetActiveObjectCount(), Is.EqualTo(1));
        }

        [Test]
        public void SimpleSpawner_SpawnInstance_ReturnsTheSpawnedObject()
        {
            SimpleSpawner spawner = CreateObject("PoolingHotPathSimpleSpawner").AddComponent<SimpleSpawner>();
            spawner.transform.position = new Vector3(1f, 0f, 0f);
            spawner.prefab = _prefab;
            spawner.offset = new Vector3(0f, 2f, 0f);
            spawner.useParent = false;

            GameObject pooled = spawner.SpawnInstance();
            spawner.useObjectPool = false;
            GameObject instantiated = spawner.SpawnInstance();
            _objects.Add(instantiated);

            Assert.That(pooled, Is.Not.Null);
            Assert.That(Vector3.Distance(pooled.transform.position, new Vector3(1f, 2f, 0f)), Is.LessThan(0.001f));
            Assert.That(pooled.TryGetComponent(out PooledObjectInfo _), Is.True);
            Assert.That(instantiated, Is.Not.Null);
            Assert.That(instantiated.TryGetComponent(out PooledObjectInfo _), Is.False);
        }

        private static void InvokeSceneLoaded(Scene scene)
        {
            MethodInfo handler = typeof(SpawnUtility).GetMethod("OnSceneLoadedForHelper",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(handler, Is.Not.Null, "SpawnUtility.OnSceneLoadedForHelper(Scene) expected");
            handler.Invoke(null, new object[] { scene });
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName + " expected");
            field.SetValue(target, value);
        }

        private NeoObjectPool CreatePool(int initialSize)
        {
            NeoObjectPool pool = new(_prefab, initialSize, true);
            _pools.Add(pool);
            return pool;
        }

        private GameObject CreatePrefab(string name)
        {
            GameObject prefab = CreateObject(name);
            prefab.SetActive(false);
            return prefab;
        }

        private Spawner CreateSpawner()
        {
#if MIRROR
            // WHY: Spawner is a NetworkBehaviour with Mirror installed; OnValidate demands an identity.
            GameObject host = CreateObject("PoolingHotPathSpawner");
            host.AddComponent<NetworkIdentity>();
#else
            GameObject host = CreateObject("PoolingHotPathSpawner");
#endif
            return host.AddComponent<Spawner>();
        }

        private GameObject CreateObject(string name)
        {
            GameObject created = new(name);
            _objects.Add(created);
            return created;
        }

        /// <summary>
        ///     Counts GC.Alloc calls of <c>iterations</c> calls of a body inside one window of the shared calibrated
        ///     <see cref="GcAllocCounter" />.
        /// </summary>
        /// <remarks>
        ///     WHY: <see cref="GC.GetAllocatedBytesForCurrentThread" /> reads 0 in the Mono Editor, which made the
        ///     earlier byte-based guards unable to fail; every guard first proves the counter with
        ///     <see cref="RequireWorkingCounter" />.
        /// </remarks>
        private static class AllocationCounter
        {
            private static object _sink;

            public static long Count(Action<int> body, int iterations)
            {
                long calls;
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    try
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            body(i);
                        }
                    }
                    finally
                    {
                        calls = counter.End();
                        _sink = null;
                    }
                }

                return calls;
            }

            public static void AllocatePerCall(int index)
            {
                _sink = new object();
            }

            public static void RequireWorkingCounter(int iterations)
            {
                Assert.That(Count(AllocatePerCall, iterations), Is.GreaterThanOrEqualTo(iterations),
                    "The GC.Alloc counter missed deliberate allocations, so this allocation guard would be vacuous.");
            }
        }
    }
}
