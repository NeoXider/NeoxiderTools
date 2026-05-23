using System;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class SpawnerTests
    {
        private GameObject _managerObj;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _managerObj = new GameObject("PoolManager");
            _managerObj.AddComponent<PoolManager>();

            // Wait for Awake to register the Singleton pattern

            _prefab = new GameObject("TestPrefab");
            _prefab.SetActive(false); // Prefabs are typically inactive objects in tests
            // Adding a distinct component so we can verify the spawned object
            _prefab.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerObj != null)
            {
                UnityEngine.Object.DestroyImmediate(_managerObj);
            }

            if (_prefab != null)
            {
                UnityEngine.Object.DestroyImmediate(_prefab);
            }

            // Clean up the singleton instance just in case
            FieldInfo field =
                typeof(Singleton<PoolManager>).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(null, null);
            }
        }

        [Test]
        public void PoolManager_Get_ReturnsInstantiatedPrefab()
        {
            GameObject instance = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);

            Assert.IsNotNull(instance, "Get should return an instantiated object");
            Assert.AreNotEqual(_prefab, instance, "Should not return the prefab itself");
            Assert.IsTrue(instance.activeSelf, "Spawned instance should be active");
            Assert.IsNotNull(instance.GetComponent<BoxCollider>(), "Spawned instance should have prefab components");
            Assert.IsNotNull(instance.GetComponent<PooledObjectInfo>(), "PoolManager should attach PooledObjectInfo");
        }

        [Test]
        public void PoolManager_Release_ReturnsObjectToPool_AndSetsInactive()
        {
            GameObject instance = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);

            Assert.IsTrue(instance.activeSelf);

            PoolManager.Release(instance);

            Assert.IsFalse(instance.activeSelf, "Released instance should be deactivated by the pool");

            PooledObjectInfo pooledInfo = instance.GetComponent<PooledObjectInfo>();
            Assert.IsNotNull(pooledInfo);
        }

        [Test]
        public void PoolManager_Get_ReusesReleasedInstances()
        {
            GameObject instance1 = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);
            PoolManager.Release(instance1);

            GameObject instance2 = PoolManager.Get(_prefab, Vector3.zero, Quaternion.identity);

            // Since we released instance1, instance2 should be exactly the same object being re-used
            Assert.AreEqual(instance1, instance2, "Pool should re-use the previously released instance");
        }
    }
}
