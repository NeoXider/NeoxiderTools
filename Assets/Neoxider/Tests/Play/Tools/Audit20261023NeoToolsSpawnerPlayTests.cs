using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    [TestFixture]
    public class Audit20261023NeoToolsSpawnerPlayTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<Spawner> _spawners = new();
        private readonly List<NeoObjectPool> _pools = new();
        private GameObject _prefab;
        private float _previousTimeScale;
        private bool _previousFallbackPolicy;

        private enum PoolAdmissionRoute
        {
            Manager,
            Fallback,
            Startup
        }

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _previousFallbackPolicy = SpawnUtility.DestroyFallbackPoolsOnSceneLoad;
            SpawnUtility.DestroyFallbackPoolsOnSceneLoad = true;
            Audit20261023NeoToolsSpawnerPoolProbe.CreatedCount = 0;
            Audit20261023NeoToolsSpawnerPoolProbe.Created = null;
            Audit20261023NeoToolsSpawnerPoolProbe.Got = null;
            _prefab = CreateObject("AuditSpawnerPlayPrefab");
            _prefab.SetActive(false);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Audit20261023NeoToolsSpawnerPoolProbe.Created = null;
            Audit20261023NeoToolsSpawnerPoolProbe.Got = null;
            foreach (GameObject instance in _objects)
            {
                if (instance == null)
                {
                    continue;
                }

                if (instance.TryGetComponent(out Audit20261023NeoToolsSpawnerPoolProbe probe))
                {
                    probe.Released = null;
                }

                if (instance.TryGetComponent(out Despawner despawner))
                {
                    SetField(despawner, "_despawnOnDisable", false);
                }
            }

            foreach (Spawner spawner in _spawners)
            {
                if (spawner != null)
                {
                    spawner.Clear();
                }
            }

            foreach (NeoObjectPool pool in _pools)
            {
                pool.Clear();
            }

            SpawnUtility.ClearFallbackPools();
            foreach (GameObject instance in _objects)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }

            yield return null;
            SpawnUtility.DestroyFallbackPoolsOnSceneLoad = _previousFallbackPolicy;
            Time.timeScale = _previousTimeScale;
            _objects.Clear();
            _spawners.Clear();
            _pools.Clear();
        }

        [UnityTest]
        public IEnumerator Manager_FirstCreateCallbackRetirementRejectsInitialRental()
        {
            return RetireDuringFirstCallback(PoolAdmissionRoute.Manager, true);
        }

        [UnityTest]
        public IEnumerator Manager_FirstGetCallbackRetirementRejectsInitialRental()
        {
            return RetireDuringFirstCallback(PoolAdmissionRoute.Manager, false);
        }

        [UnityTest]
        public IEnumerator Fallback_FirstCreateCallbackRetirementRejectsInitialRental()
        {
            return RetireDuringFirstCallback(PoolAdmissionRoute.Fallback, true);
        }

        [UnityTest]
        public IEnumerator Fallback_FirstGetCallbackRetirementRejectsInitialRental()
        {
            return RetireDuringFirstCallback(PoolAdmissionRoute.Fallback, false);
        }

        [UnityTest]
        public IEnumerator Startup_FirstCreateCallbackRetirementRejectsLaterRental()
        {
            return RetireDuringFirstCallback(PoolAdmissionRoute.Startup, true);
        }

        [UnityTest]
        public IEnumerator Startup_FirstGetCallbackRetirementRejectsLaterRental()
        {
            return RetireDuringFirstCallback(PoolAdmissionRoute.Startup, false);
        }

        [Test]
        public void Manager_NormalAdmissionPreservesPrewarmAndCallbackOrder()
        {
            AssertNormalAdmission(PoolAdmissionRoute.Manager);
        }

        [Test]
        public void Fallback_NormalAdmissionPreservesPrewarmAndCallbackOrder()
        {
            AssertNormalAdmission(PoolAdmissionRoute.Fallback);
        }

        [Test]
        public void Startup_NormalAdmissionPreservesPrewarmAndCallbackOrder()
        {
            AssertNormalAdmission(PoolAdmissionRoute.Startup);
        }

        [Test]
        public void PublicConstructor_PreservesSynchronousPrewarmAndCallbackOrder()
        {
            List<string> order = new();
            ConfigureAdmissionProbe(order);
            NeoObjectPool pool = new(_prefab, 2, true);
            _pools.Add(pool);

            Assert.That(order, Is.EqualTo(new[] { "create", "get", "create", "get", "release", "release" }));
            Assert.That(pool.CountInactive, Is.EqualTo(2));
            Assert.That(pool.IsRetired, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstObjectCallbackRestart_PublishesOnlyLatestHandleAndKeepsExpiry()
        {
            Spawner spawner = CreateSpawner();
            spawner.minSpawnDelay = spawner.maxSpawnDelay = 0.02f;
            spawner.destroyDelay = 0.02f;
            int events = 0;
            bool restarted = false;
            spawner.OnObjectSpawned.AddListener(instance =>
            {
                events++;
                if (!restarted)
                {
                    restarted = true;
                    spawner.StopSpawn();
                    spawner.StartSpawn();
                }
            });

            spawner.StartSpawn();
            Assert.That(events, Is.EqualTo(2), "The original and callback-started sessions each issue their first object.");
            Assert.That(spawner.isSpawning, Is.True, "A stale completion must not clear the newer session's flag.");
            GameObject first = spawner.SpawnedObjects[0];
            GameObject second = spawner.SpawnedObjects[1];

            // If the outer Start overwrote the callback's handle, this Stop misses that sleeping inner session.
            spawner.StopSpawn();
            spawner.minSpawnDelay = spawner.maxSpawnDelay = 10f;
            spawner.destroyDelay = 0f;
            spawner.StartSpawn();
            yield return new WaitForSeconds(0.06f);
            yield return null;

            Assert.That(events, Is.EqualTo(3), "Only the latest ten-second session may remain; the old inner loop cannot wake.");
            Assert.That(first.GetComponent<PooledObjectInfo>().IsInPool, Is.True);
            Assert.That(second.GetComponent<PooledObjectInfo>().IsInPool, Is.True);
            Assert.That(spawner.GetActiveObjectCount(), Is.EqualTo(1));
            Assert.That(spawner.isSpawning, Is.True);
            spawner.StopSpawn();
            yield return new WaitForSeconds(0.06f);
            yield return null;
            Assert.That(events, Is.EqualTo(3));
            Assert.That(spawner.isSpawning, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstWaveCallbackRestart_DoesNotProduceTheSupersededWaveBody()
        {
            Spawner spawner = CreateSpawner();
            spawner.spawnMode = SpawnMode.Waves;
            spawner.maxWaves = 1;
            spawner.minSpawnDelay = spawner.maxSpawnDelay = 10f;
            SetField(spawner, "_baseWaveCount", 1);
            int waves = 0;
            int objects = 0;
            spawner.OnWaveStarted = new UnityEvent<int>();
            spawner.OnWaveStarted.AddListener(wave =>
            {
                waves++;
                if (waves == 1)
                {
                    spawner.StopSpawn();
                    spawner.StartSpawn();
                }
            });
            spawner.OnObjectSpawned.AddListener(instance => objects++);

            spawner.StartSpawn();
            yield return null;

            Assert.That(waves, Is.EqualTo(2));
            Assert.That(objects, Is.EqualTo(1), "Only the replacement wave may issue an object.");
            Assert.That(spawner.SpawnedObjects.Count, Is.EqualTo(1));
            Assert.That(spawner.CurrentWave, Is.EqualTo(1));
            Assert.That(spawner.isSpawning, Is.True);
            spawner.StopSpawn();
            yield return null;
            Assert.That(spawner.isSpawning, Is.False);
            Assert.That(objects, Is.EqualTo(1));
        }

        [Test]
        public void GenerationCleanup_RunsAfterCachedReleaseAndOnlyOnceBeforeEviction()
        {
            _prefab.AddComponent<Audit20261023NeoToolsSpawnerPoolProbe>();
            NeoObjectPool pool = new(_prefab, 0, true);
            _pools.Add(pool);
            GameObject rental = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(rental);
            List<string> order = new();
            rental.GetComponent<Audit20261023NeoToolsSpawnerPoolProbe>().Released = target => order.Add("pool");
            PooledObjectInfo info = rental.GetComponent<PooledObjectInfo>();
            Assert.That(info.TryRegisterReleaseCleanup(pool, info.SpawnGeneration, target => order.Add("cleanup")), Is.True);

            pool.ReturnObject(rental);
            pool.ReturnObject(rental);
            pool.Clear();

            Assert.That(order, Is.EqualTo(new[] { "pool", "cleanup" }));
        }

        [UnityTest]
        public IEnumerator GenerationCleanup_ExternalDestructionConsumesHookOnce()
        {
            NeoObjectPool pool = new(_prefab, 0, true);
            _pools.Add(pool);
            GameObject rental = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(rental);
            PooledObjectInfo info = rental.GetComponent<PooledObjectInfo>();
            int cleanups = 0;
            Assert.That(info.TryRegisterReleaseCleanup(pool, info.SpawnGeneration, target => cleanups++), Is.True);

            Object.Destroy(rental);
            yield return null;
            pool.Clear();
            yield return null;

            Assert.That(rental == null, Is.True);
            Assert.That(cleanups, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ExactSourceRetirement_RepeatedGeneratedSourcesLeaveSharedPoolIntact()
        {
            PoolManager manager = CreateManager(false);
            SetField(manager, "_defaultInitialSize", 1);
            manager.gameObject.SetActive(true);
            Assert.That(PoolManager.I, Is.SameAs(manager));
            GameObject shared = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);
            _objects.Add(shared);
            PoolManager.Release(shared);
            Transform sharedRoot = shared.transform.parent;
            NeoObjectPool sharedPool = shared.GetComponent<PooledObjectInfo>().OwnerPool;
            int baseline = Object.FindObjectsByType<PooledObjectInfo>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            for (int i = 0; i < 100; i++)
            {
                GameObject source = CreateObject(_prefab.name);
                source.SetActive(false);
                GameObject rental = PoolManager.Get(source, Vector3.zero, Quaternion.identity);
                _objects.Add(rental);
                NeoObjectPool generatedPool = rental.GetComponent<PooledObjectInfo>().OwnerPool;
                PoolManager.Release(rental);

                Assert.That(SpawnUtility.RetirePool(source), Is.True);
                Assert.That(SpawnUtility.RetirePool(source), Is.False);
                Assert.That(generatedPool.IsRetired, Is.True);
                Assert.That(generatedPool.CountInactive, Is.Zero);
                Assert.That(PoolManager.Get(source, Vector3.zero, Quaternion.identity), Is.Null);
                Object.Destroy(source);
            }

            yield return null;
            Assert.That(sharedRoot != null, Is.True, "A shared name is not exclusive ownership of the storage root.");
            Assert.That(sharedRoot.childCount, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PooledObjectInfo>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(baseline));
            Assert.That(sharedPool.IsRetired, Is.False);
            Assert.That(PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity), Is.SameAs(shared));
        }

        [UnityTest]
        public IEnumerator ExactSourceRetirement_AcceptsDestroyedSourceAndDestroysLateExternalRental()
        {
            PoolManager manager = CreateManager(false);
            SetField(manager, "_defaultInitialSize", 0);
            manager.gameObject.SetActive(true);
            GameObject source = CreateObject("DestroyedGeneratedSource");
            source.SetActive(false);
            Transform externalParent = CreateObject("ExternalRentalOwner").transform;
            GameObject rental = PoolManager.Get(source, Vector3.zero, Quaternion.identity, externalParent);
            _objects.Add(rental);
            PooledObjectInfo info = rental.GetComponent<PooledObjectInfo>();
            NeoObjectPool pool = info.OwnerPool;
            int cleanups = 0;
            Assert.That(info.TryRegisterReleaseCleanup(pool, info.SpawnGeneration, target => cleanups++), Is.True);
            Object.Destroy(source);
            yield return null;
            Assert.That(source == null, Is.True);

            Assert.That(SpawnUtility.RetirePool(source), Is.True);
            Assert.That(rental.activeSelf, Is.True);
            Assert.That(rental.transform.parent, Is.SameAs(externalParent));
            Assert.That(cleanups, Is.Zero);
            Assert.That(pool.GetObject(Vector3.zero, Quaternion.identity), Is.Null);
            SpawnUtility.Despawn(rental);
            SpawnUtility.Despawn(rental);
            yield return null;

            Assert.That(rental == null, Is.True);
            Assert.That(cleanups, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ExactSourceRetirement_FallbackPreservesAnotherSourceAndSharedRoot()
        {
            Assert.That(PoolManager.I, Is.Null, "This scenario exercises fallback registration retirement.");
            GameObject first = SpawnUtility.Spawn(_prefab);
            _objects.Add(first);
            NeoObjectPool firstPool = first.GetComponent<PooledObjectInfo>().OwnerPool;
            Transform sharedRoot = first.transform.parent;
            GameObject secondSource = CreateObject(_prefab.name);
            secondSource.SetActive(false);
            GameObject second = SpawnUtility.Spawn(secondSource);
            _objects.Add(second);
            Assert.That(second.transform.parent, Is.SameAs(sharedRoot));
            SpawnUtility.Despawn(first);

            Assert.That(SpawnUtility.RetirePool(_prefab), Is.True);
            Assert.That(SpawnUtility.Spawn(_prefab), Is.Null);
            yield return null;

            Assert.That(first == null, Is.True);
            Assert.That(firstPool.IsRetired, Is.True);
            Assert.That(firstPool.CountInactive, Is.Zero);
            Assert.That(sharedRoot != null, Is.True);
            Assert.That(second.activeSelf, Is.True);
            Assert.That(second.GetComponent<PooledObjectInfo>().OwnerPool.IsRetired, Is.False);
        }

        [UnityTest]
        public IEnumerator StopThenRestart_DoesNotWakePreviousSleepingLoop()
        {
            Spawner spawner = CreateSpawner();
            SetField(spawner, "_useObjectPool", false);
            spawner.minSpawnDelay = spawner.maxSpawnDelay = 0.02f;
            spawner.StartSpawn();
            spawner.StopSpawn();
            spawner.minSpawnDelay = spawner.maxSpawnDelay = 10f;
            spawner.StartSpawn();

            yield return new WaitForSeconds(0.06f);
            yield return null;

            Assert.That(spawner.SpawnedObjects.Count, Is.EqualTo(2),
                "Only the first spawn from each session should exist; the restarted loop is waiting ten seconds.");
            Assert.That(spawner.isSpawning, Is.True);
        }

        [UnityTest]
        public IEnumerator StopSpawn_KeepsPendingRentalExpiry()
        {
            Spawner spawner = CreateSpawner();
            spawner.destroyDelay = 0.02f;
            spawner.minSpawnDelay = spawner.maxSpawnDelay = 10f;
            spawner.StartSpawn();
            GameObject instance = spawner.SpawnedObjects[0];
            spawner.StopSpawn();

            yield return new WaitForSeconds(0.06f);
            yield return null;

            Assert.That(instance.activeSelf, Is.False);
            Assert.That(instance.GetComponent<PooledObjectInfo>().IsInPool, Is.True);
            Assert.That(spawner.GetActiveObjectCount(), Is.Zero);
        }

        [Test]
        public void Clear_DoesNotReleaseRentalReissuedToAnotherSpawner()
        {
            Spawner first = CreateSpawner();
            Spawner second = CreateSpawner();
            GameObject oldRental = Spawn(first);
            SpawnUtility.Despawn(oldRental);
            GameObject newRental = Spawn(second);
            Assert.That(newRental, Is.SameAs(oldRental));

            first.Clear();

            Assert.That(newRental.activeSelf, Is.True);
            Assert.That(newRental.GetComponent<PooledObjectInfo>().IsInPool, Is.False);
            Assert.That(second.GetActiveObjectCount(), Is.EqualTo(1));
            Assert.That(first.SpawnedObjects, Is.Empty);
        }

        [Test]
        public void ActiveCount_IgnoresRentalReissuedElsewhere()
        {
            Spawner first = CreateSpawner();
            Spawner second = CreateSpawner();
            GameObject instance = Spawn(first);
            SpawnUtility.Despawn(instance);
            Assert.That(Spawn(second), Is.SameAs(instance));

            Assert.That(first.GetActiveObjectCount(), Is.Zero);
            Assert.That(first.SpawnedObjects, Is.Empty);
            Assert.That(second.GetActiveObjectCount(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StaleExpiry_DoesNotForgetNewRentalFromSameSpawner()
        {
            Spawner spawner = CreateSpawner();
            spawner.destroyDelay = 0.02f;
            GameObject first = Spawn(spawner);
            SpawnUtility.Despawn(first);
            spawner.destroyDelay = 0f;
            GameObject second = Spawn(spawner);

            Assert.That(second, Is.SameAs(first));
            Assert.That(spawner.SpawnedObjects.Count, Is.EqualTo(1));
            yield return new WaitForSeconds(0.06f);
            yield return null;

            Assert.That(second.activeSelf, Is.True);
            Assert.That(spawner.SpawnedObjects, Is.EqualTo(new[] { second }));
            spawner.Clear();
            Assert.That(second.GetComponent<PooledObjectInfo>().IsInPool, Is.True);
        }

        [Test]
        public void Clear_ReleaseCallbackCanSpawnWithoutLosingNewObject()
        {
            _prefab.AddComponent<Audit20261023NeoToolsSpawnerPoolProbe>();
            Spawner spawner = CreateSpawner();
            GameObject first = Spawn(spawner);
            GameObject createdDuringClear = null;
            first.GetComponent<Audit20261023NeoToolsSpawnerPoolProbe>().Released =
                released => createdDuringClear = Spawn(spawner);

            spawner.Clear();

            Assert.That(first.GetComponent<PooledObjectInfo>().IsInPool, Is.True);
            Assert.That(createdDuringClear != null, Is.True);
            Assert.That(createdDuringClear.activeSelf, Is.True);
            Assert.That(spawner.SpawnedObjects, Is.EqualTo(new[] { createdDuringClear }));
        }

        [Test]
        public void DisableRental_ReturnsItAndDoesNotRepeatDespawnDuringNormalReturn()
        {
            Despawner template = _prefab.AddComponent<Despawner>();
            SetField(template, "_despawnOnDisable", true);
            NeoObjectPool pool = new(_prefab, 0, true);
            _pools.Add(pool);
            GameObject instance = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(instance);
            Despawner despawner = instance.GetComponent<Despawner>();
            int events = 0;
            despawner.OnDespawn.AddListener(() => events++);

            instance.SetActive(false);

            Assert.That(events, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(pool.GetObject(Vector3.zero, Quaternion.identity), Is.SameAs(instance));
            pool.ReturnObject(instance);
            Assert.That(events, Is.EqualTo(1), "A pool return must not trigger a second effect/event through OnDisable.");
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ClearFallbackPools_DestroysAllPrewarmedInstances()
        {
            Assert.That(PoolManager.I, Is.Null, "This scenario needs the automatic fallback path.");
            GameObject instance = SpawnUtility.Spawn(_prefab);
            NeoObjectPool owner = instance.GetComponent<PooledObjectInfo>().OwnerPool;
            List<GameObject> instances = new();
            foreach (PooledObjectInfo info in Object.FindObjectsByType<PooledObjectInfo>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (info.OwnerPool == owner)
                {
                    instances.Add(info.gameObject);
                    _objects.Add(info.gameObject);
                }
            }

            Assert.That(instances.Count, Is.EqualTo(10));
            SpawnUtility.Despawn(instance);
            SpawnUtility.ClearFallbackPools();
            yield return null;

            Assert.That(instances.TrueForAll(item => item == null), Is.True,
                "Clearing the fallback root must also destroy the nine instances never handed to callers.");
        }

        [Test]
        public void PoolManager_NullPrefabReturnsNullWithoutResolvingAPool()
        {
            PoolManager manager = CreateManager(false);
            manager.gameObject.SetActive(true);

            GameObject instance = PoolManager.Get(null, Vector3.zero, Quaternion.identity);

            Assert.That(instance, Is.Null);
            Assert.That(manager.transform.childCount, Is.Zero);
        }

        [Test]
        public void PoolManager_ZeroMaximumRetainsMoreThanOneHundredInstances()
        {
            PoolManager manager = CreateManager(false);
            SetField(manager, "_defaultInitialSize", 0);
            SetField(manager, "_defaultMaxSize", 0);
            manager.gameObject.SetActive(true);
            List<GameObject> rentals = new();
            for (int i = 0; i < 101; i++)
            {
                GameObject rental = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);
                _objects.Add(rental);
                rentals.Add(rental);
            }

            NeoObjectPool pool = rentals[0].GetComponent<PooledObjectInfo>().OwnerPool;
            foreach (GameObject rental in rentals)
            {
                PoolManager.Release(rental);
            }

            Assert.That(pool.CountInactive, Is.EqualTo(101));
        }

        [Test]
        public void PoolManager_NullPreconfigurationDoesNotPreventValidPrewarm()
        {
            PoolManager manager = CreateManager(false);
            SetField(manager, "_preconfiguredPools", new List<PoolConfig>
            {
                null,
                new() { prefab = _prefab, initialSize = 2 }
            });
            manager.gameObject.SetActive(true);

            GameObject rental = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);
            _objects.Add(rental);

            Assert.That(rental.GetComponent<PooledObjectInfo>().OwnerPool.CountInactive, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DuplicatePoolManager_DoesNotPrewarmBeforeBeingDestroyed()
        {
            PoolManager primary = CreateManager(false);
            primary.gameObject.SetActive(true);
            _prefab.AddComponent<Audit20261023NeoToolsSpawnerPoolProbe>();
            PoolManager duplicate = CreateManager(false);
            SetField(duplicate, "_preconfiguredPools", new List<PoolConfig>
            {
                new() { prefab = _prefab, initialSize = 2 }
            });
            duplicate.gameObject.SetActive(true);
            yield return null;

            Assert.That(duplicate == null, Is.True);
            Assert.That(PoolManager.I, Is.SameAs(primary));
            Assert.That(Audit20261023NeoToolsSpawnerPoolProbe.CreatedCount, Is.Zero);
        }

        private IEnumerator RetireDuringFirstCallback(PoolAdmissionRoute route, bool retireOnCreate)
        {
            Assert.That(PoolManager.I, Is.Null, "Each admission starts without another manager.");
            const int managerInitialSize = 3;
            GameObject source = CreateObject("AuditAdmissionSameNamedSource");
            source.SetActive(false);
            source.AddComponent<Audit20261023NeoToolsSpawnerPoolProbe>();
            GameObject survivorSource = CreateObject(source.name);
            survivorSource.SetActive(false);
            Transform externalParent = CreateObject("AuditAdmissionExternalOwner").transform;
            PoolManager manager = route == PoolAdmissionRoute.Fallback ? null : CreateManager(false);
            if (manager != null)
            {
                SetField(manager, "_defaultInitialSize", managerInitialSize);
                if (route == PoolAdmissionRoute.Startup)
                {
                    SetField(manager, "_preconfiguredPools", new List<PoolConfig>
                    {
                        new() { prefab = survivorSource, initialSize = managerInitialSize },
                        new() { prefab = source, initialSize = managerInitialSize },
                        new() { prefab = source, initialSize = managerInitialSize }
                    });
                }
                else
                {
                    manager.gameObject.SetActive(true);
                }
            }

            List<GameObject> created = new();
            NeoObjectPool retiredPool = null;
            NeoObjectPool survivorPool = null;
            GameObject outstanding = null;
            GameObject nestedResult = null;
            Transform storageRoot = null;
            bool removedInCallback = false;
            int getCallbacks = 0;
            System.Action<GameObject> retire = instance =>
            {
                if (retiredPool != null)
                {
                    return;
                }

                retiredPool = instance.GetComponent<PooledObjectInfo>().OwnerPool;
                _pools.Add(retiredPool);
                // An independent exact source shares the name/root and has an outstanding external rental.
                outstanding = RentForAdmission(route, survivorSource);
                _objects.Add(outstanding);
                survivorPool = outstanding.GetComponent<PooledObjectInfo>().OwnerPool;
                _pools.Add(survivorPool);
                storageRoot = manager != null
                    ? manager.transform.Find(source.name + "_Pool")
                    : outstanding.transform.parent;
                outstanding.transform.SetParent(externalParent, true);
                removedInCallback = RetireForAdmission(route, source);
                // This request must be rejected before it can re-enter the transitioning instance.
                nestedResult = RentForAdmission(route, source);
                _objects.Add(nestedResult);
            };
            Audit20261023NeoToolsSpawnerPoolProbe.Created = instance =>
            {
                created.Add(instance);
                _objects.Add(instance);
                if (retireOnCreate)
                {
                    retire(instance);
                }
            };
            Audit20261023NeoToolsSpawnerPoolProbe.Got = instance =>
            {
                getCallbacks++;
                if (!retireOnCreate)
                {
                    retire(instance);
                }
            };

            if (route == PoolAdmissionRoute.Startup)
            {
                manager.gameObject.SetActive(true);
            }

            GameObject result = RentForAdmission(route, source);
            _objects.Add(result);

            Assert.That(removedInCallback, Is.True, "The first lifecycle callback must see the registered pool.");
            Assert.That(result, Is.Null, "Retirement during prewarm cancels the outer rental request.");
            Assert.That(nestedResult, Is.Null);
            Assert.That(created.Count, Is.EqualTo(1), "Retirement must stop prewarm and duplicate startup entries.");
            Assert.That(getCallbacks, Is.EqualTo(1));
            Assert.That(retiredPool.IsRetired, Is.True);
            Assert.That(retiredPool.CountInactive, Is.Zero);
            Assert.That(retiredPool.GetObject(Vector3.zero, Quaternion.identity), Is.Null);
            Assert.That(RentForAdmission(route, source), Is.Null);
            Assert.That(RetireForAdmission(route, source), Is.False, "The outer factory must not reinsert the pool.");
            yield return null;

            Assert.That(created.TrueForAll(instance => instance == null), Is.True);
            Assert.That(CountOwnedInstances(retiredPool), Is.Zero, "No retired clone may survive destruction timing.");
            Assert.That(storageRoot != null, Is.True);
            Assert.That(survivorPool.IsRetired, Is.False);
            int survivorInactive = (route == PoolAdmissionRoute.Fallback ? 10 : managerInitialSize) - 1;
            Assert.That(survivorPool.CountInactive, Is.EqualTo(survivorInactive));
            Assert.That(storageRoot.childCount, Is.EqualTo(survivorInactive));
            Assert.That(outstanding.activeSelf, Is.True);
            Assert.That(outstanding.transform.parent, Is.SameAs(externalParent));
            GameObject survivorReuse = RentForAdmission(route, survivorSource);
            _objects.Add(survivorReuse);
            Assert.That(survivorReuse.GetComponent<PooledObjectInfo>().OwnerPool, Is.SameAs(survivorPool));
            SpawnUtility.Despawn(survivorReuse);

            // Destructive late-return control: retirement must leave this external rental alive until return.
            PooledObjectInfo survivorInfo = outstanding.GetComponent<PooledObjectInfo>();
            int lateCleanups = 0;
            Assert.That(survivorInfo.TryRegisterReleaseCleanup(survivorPool, survivorInfo.SpawnGeneration,
                instance => lateCleanups++), Is.True);
            Assert.That(RetireForAdmission(route, survivorSource), Is.True);
            Assert.That(outstanding.activeSelf, Is.True);
            Assert.That(outstanding.transform.parent, Is.SameAs(externalParent));
            Assert.That(lateCleanups, Is.Zero);
            Assert.That(RentForAdmission(route, survivorSource), Is.Null);
            SpawnUtility.Despawn(outstanding);
            SpawnUtility.Despawn(outstanding);
            yield return null;

            Assert.That(outstanding == null, Is.True);
            Assert.That(lateCleanups, Is.EqualTo(1));
            Assert.That(survivorPool.CountInactive, Is.Zero);
            Assert.That(CountOwnedInstances(survivorPool), Is.Zero);
            Assert.That(storageRoot != null, Is.True);
            Assert.That(storageRoot.childCount, Is.Zero);
            Assert.That(created.Count, Is.EqualTo(1));
        }

        private void AssertNormalAdmission(PoolAdmissionRoute route)
        {
            Assert.That(PoolManager.I, Is.Null);
            List<string> order = new();
            ConfigureAdmissionProbe(order);
            int initialSize = route == PoolAdmissionRoute.Fallback ? 10 : 3;
            if (route != PoolAdmissionRoute.Fallback)
            {
                PoolManager manager = CreateManager(false);
                SetField(manager, "_defaultInitialSize", initialSize);
                if (route == PoolAdmissionRoute.Startup)
                {
                    SetField(manager, "_preconfiguredPools", new List<PoolConfig>
                    {
                        new() { prefab = _prefab, initialSize = initialSize }
                    });
                }

                manager.gameObject.SetActive(true);
            }

            GameObject rental = RentForAdmission(route, _prefab);
            _objects.Add(rental);
            NeoObjectPool pool = rental.GetComponent<PooledObjectInfo>().OwnerPool;
            _pools.Add(pool);
            List<string> expected = new();
            for (int i = 0; i < initialSize; i++)
            {
                expected.Add("create");
                expected.Add("get");
            }

            for (int i = 0; i < initialSize; i++)
            {
                expected.Add("release");
            }

            expected.Add("get");
            Assert.That(order, Is.EqualTo(expected), "All prewarm acquisitions precede their returns and public rent.");
            Assert.That(pool.IsRetired, Is.False);
            Assert.That(pool.CountInactive, Is.EqualTo(initialSize - 1));
            Assert.That(rental.GetComponent<PooledObjectInfo>().SpawnGeneration, Is.EqualTo(2));
            SpawnUtility.Despawn(rental);
            expected.Add("release");
            Assert.That(order, Is.EqualTo(expected));
            Assert.That(pool.CountInactive, Is.EqualTo(initialSize));
        }

        private void ConfigureAdmissionProbe(List<string> order)
        {
            _prefab.AddComponent<Audit20261023NeoToolsSpawnerPoolProbe>();
            Audit20261023NeoToolsSpawnerPoolProbe.Created = instance =>
            {
                order.Add("create");
                instance.GetComponent<Audit20261023NeoToolsSpawnerPoolProbe>().Released =
                    target => order.Add("release");
            };
            Audit20261023NeoToolsSpawnerPoolProbe.Got = instance => order.Add("get");
        }

        private static GameObject RentForAdmission(PoolAdmissionRoute route, GameObject source)
        {
            return route == PoolAdmissionRoute.Fallback
                ? SpawnUtility.Spawn(source)
                : PoolManager.Get(source, Vector3.zero, Quaternion.identity);
        }

        private static bool RetireForAdmission(PoolAdmissionRoute route, GameObject source)
        {
            return route == PoolAdmissionRoute.Fallback
                ? SpawnUtility.RetirePool(source)
                : PoolManager.RetirePool(source);
        }

        private static int CountOwnedInstances(NeoObjectPool owner)
        {
            int count = 0;
            foreach (PooledObjectInfo info in Object.FindObjectsByType<PooledObjectInfo>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (info.OwnerPool == owner)
                {
                    count++;
                }
            }

            return count;
        }

        private GameObject CreateObject(string name)
        {
            GameObject instance = new(name);
            _objects.Add(instance);
            return instance;
        }

        private Spawner CreateSpawner()
        {
            Spawner spawner = CreateObject("AuditSpawnerPlay").AddComponent<Spawner>();
            spawner.prefabs = new[] { _prefab };
            spawner.OnObjectSpawned = new UnityEvent<GameObject>();
            _spawners.Add(spawner);
            return spawner;
        }

        private GameObject Spawn(Spawner spawner)
        {
            GameObject instance = spawner.SpawnObject(_prefab, Vector3.zero, Quaternion.identity, null);
            _objects.Add(instance);
            return instance;
        }

        private PoolManager CreateManager(bool active)
        {
            GameObject managerObject = CreateObject("AuditPoolManager");
            managerObject.SetActive(false);
            PoolManager manager = managerObject.AddComponent<PoolManager>();
            managerObject.SetActive(active);
            return manager;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            // Configure existing serialized Inspector inputs; assertions observe public runtime behavior.
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
