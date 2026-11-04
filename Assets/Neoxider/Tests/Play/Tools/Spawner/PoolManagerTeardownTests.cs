using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     A destroyed <see cref="PoolManager" /> retires its pools: its inactive instances are destroyed, and a rental
    ///     still out is destroyed on its later return instead of being parked inactive where it stands. A rental
    ///     without a parent lives in the active scene, not in the manager's DontDestroyOnLoad scene.
    /// </summary>
    [TestFixture]
    public class PoolManagerTeardownTests
    {
        private static readonly FieldInfo SingletonInstanceField =
            typeof(Singleton<PoolManager>).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo SingletonSearchFailedField =
            typeof(Singleton<PoolManager>).GetField("_searchFailed", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly List<GameObject> _objects = new();
        private GameObject _prefab;
        private object _previousManager;
        private object _previousSearchFailed;
        private bool _previousCreateInstance;

        [SetUp]
        public void SetUp()
        {
            Assert.That(SingletonInstanceField, Is.Not.Null, "Singleton<PoolManager>._instance expected");
            Assert.That(SingletonSearchFailedField, Is.Not.Null, "Singleton<PoolManager>._searchFailed expected");
            _previousManager = SingletonInstanceField.GetValue(null);
            _previousSearchFailed = SingletonSearchFailedField.GetValue(null);
            _previousCreateInstance = PoolManager.CreateInstance;
            // WHY: A manager leaked by an earlier test would make the new one destroy itself as a duplicate.
            SingletonInstanceField.SetValue(null, null);
            SingletonSearchFailedField.SetValue(null, true);
            PoolManager.CreateInstance = false;

            _prefab = CreateObject("PoolManagerTeardownPrefab");
            _prefab.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
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
                _objects.Clear();
                PoolManager.CreateInstance = _previousCreateInstance;
                SingletonInstanceField.SetValue(null, _previousManager);
                SingletonSearchFailedField.SetValue(null, _previousSearchFailed);
            }
        }

        [UnityTest]
        public IEnumerator DestroyedManager_RetiresItsPools_AndDestroysALateReturn([Values] bool immediate)
        {
            GameObject managerObject = CreateObject("TeardownPoolManager");
            managerObject.SetActive(false);
            PoolManager manager = managerObject.AddComponent<PoolManager>();
            managerObject.SetActive(true);
            Assert.That(PoolManager.TryGetInstance(out PoolManager registered) && registered == manager, Is.True);

            Transform externalOwner = CreateObject("ExternalRentalOwner").transform;
            GameObject rental = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity, externalOwner);
            GameObject parked = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);
            _objects.Add(rental);
            PoolManager.Release(parked);
            PooledObjectInfo info = rental.GetComponent<PooledObjectInfo>();
            NeoObjectPool pool = info.OwnerPool;
            Assert.That(parked.transform.IsChildOf(managerObject.transform), Is.True,
                "positive control: the parked instance is stored under the manager");
            int cleanups = 0;
            Assert.That(info.TryRegisterReleaseCleanup(pool, info.SpawnGeneration, _ => cleanups++), Is.True);

            if (immediate)
            {
                Object.DestroyImmediate(managerObject);
            }
            else
            {
                Object.Destroy(managerObject);
            }

            yield return null;

            Assert.That(pool.IsRetired, Is.True, "The destroyed manager's pools must be retired.");
            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(parked == null, Is.True, "Stored instances go down with the manager's storage roots.");
            Assert.That(rental != null && rental.activeSelf, Is.True, "A rental still out survives the teardown.");
            Assert.That(rental.transform.parent, Is.SameAs(externalOwner));
            Assert.That(cleanups, Is.Zero);
            Assert.That(pool.GetObject(Vector3.zero, Quaternion.identity), Is.Null);

            SpawnUtility.Despawn(rental);
            SpawnUtility.Despawn(rental);
            yield return null;

            Assert.That(rental == null, Is.True, "A late return to a retired pool destroys the rental.");
            Assert.That(cleanups, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DuplicateManager_DestroyedOnAwake_LeavesThePrimaryPoolsWorking()
        {
            GameObject primaryObject = CreateObject("PrimaryPoolManager");
            primaryObject.SetActive(false);
            PoolManager primary = primaryObject.AddComponent<PoolManager>();
            primaryObject.SetActive(true);
            GameObject rental = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);
            NeoObjectPool pool = rental.GetComponent<PooledObjectInfo>().OwnerPool;

            GameObject duplicateObject = CreateObject("DuplicatePoolManager");
            duplicateObject.SetActive(false);
            duplicateObject.AddComponent<PoolManager>();
            duplicateObject.SetActive(true);
            yield return null;

            Assert.That(duplicateObject == null, Is.True, "positive control: the duplicate destroyed itself");
            Assert.That(PoolManager.TryGetInstance(out PoolManager registered) && registered == primary, Is.True);
            Assert.That(pool.IsRetired, Is.False, "Only the destroyed manager's own pools retire.");
            PoolManager.Release(rental);
            Assert.That(rental != null && !rental.activeSelf, Is.True);
            Assert.That(PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity), Is.SameAs(rental));
            _objects.Add(rental);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DestroyedManagerComponent_DestroysTheStoredInstances()
        {
            GameObject managerObject = CreateObject("ComponentOnlyPoolManager");
            managerObject.SetActive(false);
            PoolManager manager = managerObject.AddComponent<PoolManager>();
            managerObject.SetActive(true);
            GameObject parked = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);
            _objects.Add(parked);
            NeoObjectPool pool = parked.GetComponent<PooledObjectInfo>().OwnerPool;
            PoolManager.Release(parked);
            Assert.That(parked.transform.IsChildOf(managerObject.transform), Is.True,
                "positive control: the parked instance is stored under the manager");

            Object.Destroy(manager);
            yield return null;
            yield return null;

            Assert.That(managerObject != null, Is.True, "positive control: only the component was destroyed");
            Assert.That(pool.IsRetired, Is.True);
            Assert.That(parked == null, Is.True,
                "Stored instances must not stay behind under storage roots that outlive the component.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NullParentRental_LandsInTheActiveScene_NotInDontDestroyOnLoad()
        {
            GameObject managerObject = CreateObject("ScenePoolManager");
            managerObject.SetActive(false);
            managerObject.AddComponent<PoolManager>();
            managerObject.SetActive(true);
            yield return null;
            Scene activeScene = SceneManager.GetActiveScene();
            Assert.That(managerObject.scene == activeScene, Is.False,
                "positive control: the manager and its storage live in DontDestroyOnLoad");

            Vector3 position = new(1f, 2f, 3f);
            GameObject rental = PoolManager.Get(_prefab, position, Quaternion.identity);
            _objects.Add(rental);
            Assert.That(rental.transform.parent, Is.Null);
            Assert.That(rental.scene == activeScene, Is.True,
                "A rental without a parent unloads with the active scene.");
            Assert.That(Vector3.Distance(rental.transform.position, position), Is.LessThan(1e-4f));

            PoolManager.Release(rental);
            Assert.That(rental.transform.IsChildOf(managerObject.transform), Is.True,
                "A return moves the instance back to the persistent storage.");
            GameObject reused = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);
            Assert.That(reused, Is.SameAs(rental));
            Assert.That(reused.scene == activeScene, Is.True, "A reused rental leaves DontDestroyOnLoad as well.");

            Transform owner = CreateObject("SceneRentalOwner").transform;
            PoolManager.Release(reused);
            GameObject parented = PoolManager.Get(_prefab, position, Quaternion.identity, owner);
            Assert.That(parented, Is.SameAs(rental));
            Assert.That(parented.transform.parent, Is.SameAs(owner));
            Assert.That(parented.scene == owner.gameObject.scene, Is.True);
            Assert.That(Vector3.Distance(parented.transform.position, position), Is.LessThan(1e-4f));
            LogAssert.NoUnexpectedReceived();
        }

        private GameObject CreateObject(string name)
        {
            GameObject created = new(name);
            _objects.Add(created);
            return created;
        }
    }
}
