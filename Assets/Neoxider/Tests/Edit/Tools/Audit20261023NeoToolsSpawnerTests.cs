using System;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NeoToolsSpawnerTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<NeoObjectPool> _pools = new();
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _prefab = CreateObject("AuditSpawnerPrefab");
            _prefab.SetActive(false);
            _prefab.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (NeoObjectPool pool in _pools)
            {
                pool.Clear();
            }

            foreach (GameObject instance in _objects)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            _pools.Clear();
            _objects.Clear();
        }

        [Test]
        public void DestroyedInactiveInstance_IsReplacedWithLiveRentalAtRequestedPose()
        {
            NeoObjectPool pool = CreatePool(1);
            GameObject first = Rent(pool);
            pool.ReturnObject(first);
            Object.DestroyImmediate(first);
            Vector3 position = new(3f, 5f, 7f);
            Quaternion rotation = Quaternion.Euler(12f, 34f, 56f);

            GameObject replacement = pool.GetObject(position, rotation);
            _objects.Add(replacement);

            Assert.That(replacement != null, Is.True);
            Assert.That(replacement.activeSelf, Is.True);
            Assert.That(replacement.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(replacement.transform.position, Is.EqualTo(position));
            Assert.That(Quaternion.Angle(replacement.transform.rotation, rotation), Is.LessThan(0.001f));
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(replacement.GetComponent<PooledObjectInfo>().OwnerPool, Is.SameAs(pool));
        }

        [Test]
        public void ReturnToWrongPool_DoesNotStealOrDeactivateRental()
        {
            NeoObjectPool owner = CreatePool(0);
            NeoObjectPool other = CreatePool(0);
            GameObject instance = Rent(owner);

            other.ReturnObject(instance);

            Assert.That(instance.activeSelf, Is.True);
            Assert.That(instance.GetComponent<PooledObjectInfo>().OwnerPool, Is.SameAs(owner));
            Assert.That(other.CountInactive, Is.Zero);
            Assert.That(owner.CountInactive, Is.Zero);
            owner.ReturnObject(instance);
            Assert.That(owner.CountInactive, Is.EqualTo(1));
            Assert.That(Rent(owner), Is.SameAs(instance));
            Assert.That(Rent(other), Is.Not.SameAs(instance));
        }

        [Test]
        public void ReturnUnownedObject_DoesNotAdoptIt()
        {
            NeoObjectPool pool = CreatePool(0);
            GameObject foreign = CreateObject("Unpooled");

            pool.ReturnObject(foreign);

            Assert.That(foreign.activeSelf, Is.True);
            Assert.That(foreign.GetComponent<PooledObjectInfo>(), Is.Null);
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void UnlimitedRetention_KeepsMoreThanOneHundredReturnedInstances()
        {
            NeoObjectPool pool = CreatePool(0, true, 0);
            List<GameObject> rentals = new();
            for (int i = 0; i < 101; i++)
            {
                rentals.Add(Rent(pool));
            }

            foreach (GameObject rental in rentals)
            {
                pool.ReturnObject(rental);
            }

            Assert.That(pool.CountInactive, Is.EqualTo(101));
            Assert.That(rentals.TrueForAll(instance => instance != null && !instance.activeSelf), Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void EmptyNonExpandingPool_CanRentAndRetainAnInstance(int initialSize)
        {
            NeoObjectPool pool = CreatePool(initialSize, false);
            Assert.That(pool.CountInactive, Is.Zero);

            GameObject instance = Rent(pool);
            Assert.That(instance.activeSelf, Is.True);
            pool.ReturnObject(instance);

            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(Rent(pool), Is.SameAs(instance));
        }

        [Test]
        public void NullPrefab_IsRejectedWithNamedArgument()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new NeoObjectPool(null, 0, true));

            Assert.That(exception.ParamName, Is.EqualTo("prefab"));
        }

        [Test]
        public void DespawnInactiveRental_ReturnsItAndInvokesEventOnce()
        {
            _prefab.AddComponent<Despawner>();
            NeoObjectPool pool = CreatePool(0);
            GameObject instance = Rent(pool);
            Despawner despawner = instance.GetComponent<Despawner>();
            int events = 0;
            despawner.OnDespawn.AddListener(() => events++);
            instance.SetActive(false);

            despawner.Despawn();
            despawner.Despawn();

            Assert.That(events, Is.EqualTo(1));
            Assert.That(instance.GetComponent<PooledObjectInfo>().IsInPool, Is.True);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void DespawnListenerReentry_InvokesEventOnceAndReturnsOnce()
        {
            _prefab.AddComponent<Despawner>();
            NeoObjectPool pool = CreatePool(0);
            GameObject instance = Rent(pool);
            Despawner despawner = instance.GetComponent<Despawner>();
            int events = 0;
            despawner.OnDespawn.AddListener(() =>
            {
                events++;
                if (events == 1)
                {
                    despawner.Despawn();
                }
            });

            despawner.Despawn();

            Assert.That(events, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(instance.activeSelf, Is.False);
        }

        [Test]
        public void DespawnListenerReissuesInstance_NewRentalStaysActive()
        {
            _prefab.AddComponent<Despawner>();
            NeoObjectPool pool = CreatePool(0);
            GameObject instance = Rent(pool);
            Despawner despawner = instance.GetComponent<Despawner>();
            GameObject reissued = null;
            despawner.OnDespawn.AddListener(() =>
            {
                pool.ReturnObject(instance);
                reissued = Rent(pool);
            });

            despawner.Despawn();

            Assert.That(reissued, Is.SameAs(instance));
            Assert.That(reissued.activeSelf, Is.True);
            Assert.That(reissued.GetComponent<PooledObjectInfo>().IsInPool, Is.False);
            Assert.That(pool.CountInactive, Is.Zero);
        }

        [Test]
        public void DespawnGuard_ResetsForNewPoolGenerationWithoutUnityEnableCallbacks()
        {
            _prefab.AddComponent<Despawner>();
            NeoObjectPool pool = CreatePool(0);
            GameObject instance = Rent(pool);
            Despawner despawner = instance.GetComponent<Despawner>();
            int events = 0;
            despawner.OnDespawn.AddListener(() => events++);
            despawner.Despawn();
            Assert.That(Rent(pool), Is.SameAs(instance));

            despawner.Despawn();

            Assert.That(events, Is.EqualTo(2));
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void CapsuleSpawnArea_ContainsEverySample_ForEachAxisAndScale(int direction)
        {
            GameObject spawnerObject = CreateObject("CapsuleSpawner");
            Spawner spawner = spawnerObject.AddComponent<Spawner>();
            GameObject area = CreateObject("CapsuleArea");
            area.transform.position = new Vector3(4f, -3f, 2f);
            area.transform.rotation = Quaternion.Euler(31f, 17f, 58f);
            CapsuleCollider capsule = area.AddComponent<CapsuleCollider>();
            capsule.direction = direction;
            capsule.radius = 0.5f;
            capsule.height = 6f;
            capsule.center = new Vector3(0.2f, -0.3f, 0.4f);
            using (SerializedObject settings = new(spawner))
            {
                settings.FindProperty("_spawnAreaCollider").objectReferenceValue = capsule;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            Random.State previousRandom = Random.state;
            try
            {
                Random.InitState(83419);
                Vector3[] scales = { Vector3.one, new(0.5f, 2f, 1f), new(-1f, 0.3f, 2f) };
                foreach (Vector3 scale in scales)
                {
                    area.transform.localScale = scale;
                    Physics.SyncTransforms();
                    float greatestOutsideDistance = 0f;
                    for (int i = 0; i < 256; i++)
                    {
                        Vector3 point = spawner.GetSpawnPosition();
                        greatestOutsideDistance = Mathf.Max(greatestOutsideDistance,
                            Vector3.Distance(point, capsule.ClosestPoint(point)));
                    }

                    Assert.That(greatestOutsideDistance, Is.LessThan(0.001f),
                        $"Capsule axis {direction}, scale {scale}");
                }
            }
            finally
            {
                Random.state = previousRandom;
            }
        }

        [Test]
        public void DespawnUnpooledInactiveObject_DestroysItInEditMode()
        {
            GameObject instance = CreateObject("InactiveUnpooled");
            Despawner despawner = instance.AddComponent<Despawner>();
            int events = 0;
            despawner.OnDespawn.AddListener(() => events++);
            instance.SetActive(false);

            Despawner.DespawnObject(instance);

            Assert.That(instance == null, Is.True);
            Assert.That(events, Is.EqualTo(1));
        }

        private GameObject CreateObject(string name)
        {
            GameObject instance = new(name);
            _objects.Add(instance);
            return instance;
        }

        private NeoObjectPool CreatePool(int initialSize, bool expandPool = true, int maxSize = 100)
        {
            NeoObjectPool pool = new(_prefab, initialSize, expandPool, maxSize);
            _pools.Add(pool);
            return pool;
        }

        private GameObject Rent(NeoObjectPool pool)
        {
            GameObject instance = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(instance);
            return instance;
        }
    }
}
