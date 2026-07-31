using System.Collections;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the audit fix for Spawner: Unity kills the spawn coroutine on disable, so the
    ///     lifecycle hook must clear isSpawning - otherwise StartSpawn is blocked by its own guard
    ///     for the rest of the session.
    /// </summary>
    [TestFixture]
    public class AuditFixesSpawnerTests
    {
        private GameObject _spawnerGo;
        private GameObject _prefab;
        private Spawner _spawner;

        [SetUp]
        public void SetUp()
        {
#if MIRROR
            _spawnerGo = new GameObject("AuditFixesSpawner", typeof(NetworkIdentity));
#else
            _spawnerGo = new GameObject("AuditFixesSpawner");
#endif
            _spawner = _spawnerGo.AddComponent<Spawner>();

            _prefab = new GameObject("AuditFixesSpawnPrefab");
            _prefab.SetActive(false);

            _spawner.prefabs = new[] { _prefab };
            _spawner.minSpawnDelay = 1f;
            _spawner.maxSpawnDelay = 1f;

            // WHY: plain Instantiate keeps this fixture independent of PoolManager singleton state.
            SetPrivateField(_spawner, "_useObjectPool", false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_spawner != null)
            {
                foreach (GameObject spawned in _spawner.SpawnedObjects)
                {
                    if (spawned != null)
                    {
                        Object.DestroyImmediate(spawned);
                    }
                }
            }

            if (_spawnerGo != null)
            {
                Object.DestroyImmediate(_spawnerGo);
            }

            if (_prefab != null)
            {
                Object.DestroyImmediate(_prefab);
            }
        }

        [Test]
        public void OnDisable_ResetsIsSpawning_SoStartSpawnIsNotBlockedForever()
        {
            // WHY: MoveNext runs the loop up to its first wait, exactly like a coroutine that is then
            // killed by a disable before its flag-clearing tail can run.
            IEnumerator spawnLoop = _spawner.SpawnObjects();
            Assert.IsTrue(spawnLoop.MoveNext(), "the spawn loop must reach its first wait");
            Assert.IsTrue(_spawner.isSpawning, "the running loop marks the spawner as spawning");

            InvokeLifecycle(_spawner, "OnDisable");

            Assert.IsFalse(_spawner.isSpawning,
                "OnDisable must clear the guard that the killed coroutine can no longer clear");
        }

        [Test]
        public void OnDisable_WithoutRunningSpawn_IsSafe()
        {
            Assert.DoesNotThrow(() => InvokeLifecycle(_spawner, "OnDisable"));
            Assert.IsFalse(_spawner.isSpawning);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"{target.GetType().Name} must declare {methodName}");
            method.Invoke(target, null);
        }
    }
}
