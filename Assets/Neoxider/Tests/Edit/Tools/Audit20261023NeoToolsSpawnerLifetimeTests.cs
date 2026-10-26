using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NeoToolsSpawnerLifetimeTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<Material> _materials = new();
        private readonly List<NeoObjectPool> _pools = new();

        [TearDown]
        public void TearDown()
        {
            foreach (NeoObjectPool pool in _pools)
            {
                pool.Retire();
            }

            foreach (GameObject instance in _objects)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            foreach (Material material in _materials)
            {
                if (material != null)
                {
                    Object.DestroyImmediate(material);
                }
            }

            _pools.Clear();
            _objects.Clear();
            _materials.Clear();
        }

        [Test]
        public void Retire_ClearsInactiveRejectsGetAndDestroysValidLateReturn()
        {
            NeoObjectPool pool = CreatePool(CreateSource("RetiredSource"), 2);
            GameObject rental = Rent(pool);
            PooledObjectInfo info = rental.GetComponent<PooledObjectInfo>();
            int cleanups = 0;
            Assert.That(info.TryRegisterReleaseCleanup(pool, info.SpawnGeneration, target => cleanups++), Is.True);

            pool.Retire();

            Assert.That(pool.IsRetired, Is.True);
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(pool.GetObject(Vector3.one, Quaternion.identity), Is.Null);
            Assert.That(rental.activeSelf, Is.True);
            Assert.That(info.IsInPool, Is.False);
            Assert.That(cleanups, Is.Zero);

            pool.ReturnObject(rental);
            pool.ReturnObject(rental);
            pool.Retire();

            Assert.That(rental == null, Is.True);
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(cleanups, Is.EqualTo(1));
        }

        [Test]
        public void Retire_PreservesSharedStorageRootAndDifferentSameNamedSource()
        {
            GameObject storage = CreateSource("SharedStorage");
            storage.SetActive(true);
            GameObject firstSource = CreateSource("SameName");
            GameObject secondSource = CreateSource("SameName");
            NeoObjectPool first = CreatePool(firstSource, 2, storageRoot: storage.transform);
            NeoObjectPool second = CreatePool(secondSource, 1, storageRoot: storage.transform);
            GameObject outstanding = Rent(first);
            Assert.That(storage.transform.childCount, Is.EqualTo(2));

            first.Retire();

            Assert.That(storage != null, Is.True);
            Assert.That(storage.transform.childCount, Is.EqualTo(1));
            Assert.That(second.IsRetired, Is.False);
            Assert.That(second.CountInactive, Is.EqualTo(1));
            GameObject otherRental = Rent(second);
            Assert.That(otherRental.activeSelf, Is.True);
            Assert.That(otherRental.GetComponent<PooledObjectInfo>().OwnerPool, Is.SameAs(second));
            first.ReturnObject(outstanding);
            Assert.That(outstanding == null, Is.True);
            Assert.That(otherRental.activeSelf, Is.True);
            Assert.That(storage != null, Is.True);
        }

        [Test]
        public void ReleaseCleanup_RestoresBorrowedMaterialBeforeReuse_RejectsStaleRegistration()
        {
            GameObject source = CreateSource("MaterialSource");
            LineRenderer template = source.AddComponent<LineRenderer>();
            Material borrowed = CreateMaterial("Borrowed");
            template.sharedMaterial = borrowed;
            NeoObjectPool pool = CreatePool(source, 0);
            GameObject first = Rent(pool);
            LineRenderer renderer = first.GetComponent<LineRenderer>();
            PooledObjectInfo info = first.GetComponent<PooledObjectInfo>();
            int firstGeneration = info.SpawnGeneration;
            Material firstClone = CreateMaterial("FirstClone");
            renderer.sharedMaterial = firstClone;
            int cleanups = 0;
            Assert.That(info.TryRegisterReleaseCleanup(pool, firstGeneration, target =>
            {
                cleanups++;
                if (renderer != null && renderer.sharedMaterial == firstClone)
                {
                    renderer.sharedMaterial = borrowed;
                }

                Object.DestroyImmediate(firstClone);
            }), Is.True);

            pool.ReturnObject(first);

            Assert.That(cleanups, Is.EqualTo(1));
            Assert.That(firstClone == null, Is.True);
            Assert.That(borrowed != null, Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(borrowed));
            GameObject second = Rent(pool);
            Assert.That(second, Is.SameAs(first));
            Assert.That(info.SpawnGeneration, Is.Not.EqualTo(firstGeneration));
            Material secondClone = CreateMaterial("SecondClone");
            renderer.sharedMaterial = secondClone;
            Assert.That(info.TryRegisterReleaseCleanup(pool, info.SpawnGeneration, target =>
            {
                cleanups++;
                if (renderer != null)
                {
                    renderer.sharedMaterial = borrowed;
                }

                Object.DestroyImmediate(secondClone);
            }), Is.True);
            Assert.That(info.TryRegisterReleaseCleanup(pool, firstGeneration,
                target => Object.DestroyImmediate(borrowed)), Is.False);
            Assert.That(renderer.sharedMaterial, Is.SameAs(secondClone));
            Assert.That(borrowed != null, Is.True);

            pool.ReturnObject(second);
            pool.Clear();

            Assert.That(secondClone == null, Is.True);
            Assert.That(borrowed != null, Is.True);
            Assert.That(cleanups, Is.EqualTo(2));
        }

        [Test]
        public void ReleaseCleanup_RejectsForeignPendingAndReturnedRegistrations()
        {
            GameObject source = CreateSource("HookSource");
            NeoObjectPool owner = CreatePool(source, 0);
            NeoObjectPool other = CreatePool(source, 0);
            GameObject rental = Rent(owner);
            PooledObjectInfo info = rental.GetComponent<PooledObjectInfo>();
            int cleanups = 0;
            int rejectedCallbacks = 0;
            Assert.That(info.TryRegisterReleaseCleanup(other, info.SpawnGeneration,
                target => rejectedCallbacks++), Is.False);
            Assert.That(info.TryRegisterReleaseCleanup(owner, info.SpawnGeneration,
                target => cleanups++), Is.True);
            Assert.That(info.TryRegisterReleaseCleanup(owner, info.SpawnGeneration,
                target => rejectedCallbacks++), Is.False);

            owner.ReturnObject(rental);
            Assert.That(info.TryRegisterReleaseCleanup(owner, info.SpawnGeneration,
                target => rejectedCallbacks++), Is.False);
            owner.ReturnObject(rental);
            owner.Clear();

            Assert.That(cleanups, Is.EqualTo(1));
            Assert.That(rejectedCallbacks, Is.Zero);
        }

        [Test]
        public void ReleaseCleanup_CapacityEvictionDoesNotReplayCleanupOnDestroy()
        {
            NeoObjectPool pool = CreatePool(CreateSource("EvictedSource"), 0, 1);
            GameObject first = Rent(pool);
            GameObject evicted = Rent(pool);
            PooledObjectInfo info = evicted.GetComponent<PooledObjectInfo>();
            int cleanups = 0;
            bool sawReturnedState = false;
            Assert.That(info.TryRegisterReleaseCleanup(pool, info.SpawnGeneration, target =>
            {
                cleanups++;
                sawReturnedState = info.IsInPool;
            }), Is.True);

            pool.ReturnObject(first);
            pool.ReturnObject(evicted);
            pool.Clear();

            Assert.That(evicted == null, Is.True);
            Assert.That(cleanups, Is.EqualTo(1));
            Assert.That(sawReturnedState, Is.True);
            Assert.That(pool.CountInactive, Is.Zero);
        }

        private GameObject CreateSource(string name)
        {
            GameObject source = new(name);
            source.SetActive(false);
            _objects.Add(source);
            return source;
        }

        private Material CreateMaterial(string name)
        {
            Shader shader = Shader.Find("Hidden/InternalErrorShader");
            Assert.That(shader, Is.Not.Null, "A built-in shader is needed for observable material ownership.");
            Material material = new(shader) { name = name };
            _materials.Add(material);
            return material;
        }

        private NeoObjectPool CreatePool(GameObject source, int initialSize, int maxSize = 100,
            Transform storageRoot = null)
        {
            NeoObjectPool pool = new(source, initialSize, true, maxSize, storageRoot);
            _pools.Add(pool);
            return pool;
        }

        private GameObject Rent(NeoObjectPool pool)
        {
            GameObject rental = pool.GetObject(Vector3.zero, Quaternion.identity);
            _objects.Add(rental);
            return rental;
        }
    }
}
