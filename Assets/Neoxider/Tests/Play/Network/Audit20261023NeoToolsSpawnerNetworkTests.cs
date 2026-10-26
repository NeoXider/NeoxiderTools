#if MIRROR
using System.Collections;
using System.Reflection;
using Mirror;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    [TestFixture]
    public class Audit20261023NeoToolsSpawnerNetworkTests
    {
        private TestNetworkManager _manager;
        private GameObject _managerObject;
        private GameObject _playerPrefab;
        private GameObject _spawnPrefab;
        private GameObject _spawnerObject;
        private Spawner _spawner;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_spawner != null)
            {
                _spawner.StopSpawn();
            }

            if (_manager != null)
            {
                _manager.StopHost();
            }

            yield return null;
            SpawnUtility.ClearFallbackPools();
            NetworkClient.ClearSpawners();
            GameObject[] objects = { _spawnerObject, _spawnPrefab, _playerPrefab, _managerObject };
            foreach (GameObject instance in objects)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }

            yield return null;
            Time.timeScale = _previousTimeScale;
        }

        [UnityTest]
        public IEnumerator HostWave_FiresEachEventAndTracksEachObjectOnce()
        {
            yield return PrepareHostSpawner();
            int waves = 0;
            int objects = 0;
            int waveObjects = 0;
            _spawner.OnWaveStarted.AddListener(wave => waves++);
            _spawner.OnObjectSpawned.AddListener(instance => objects++);
            _spawner.OnWaveObjectSpawned.AddListener((instance, wave) => waveObjects++);

            _spawner.StartSpawn();
            yield return new WaitForSeconds(0.1f);
            yield return null;

            Assert.That(waves, Is.EqualTo(1));
            Assert.That(objects, Is.EqualTo(1));
            Assert.That(waveObjects, Is.EqualTo(1));
            Assert.That(_spawner.SpawnedObjects.Count, Is.EqualTo(1));
            NetworkIdentity spawnedIdentity = _spawner.SpawnedObjects[0].GetComponent<NetworkIdentity>();
            yield return NetworkTestHelper.WaitForSpawn(spawnedIdentity);
            Assert.That(NetworkServer.spawned.ContainsKey(spawnedIdentity.netId), Is.True);
            Assert.That(NetworkServer.spawned[spawnedIdentity.netId], Is.SameAs(spawnedIdentity));
            Assert.That(NetworkClient.spawned[spawnedIdentity.netId], Is.SameAs(spawnedIdentity));
        }

        [UnityTest]
        public IEnumerator PooledNetworkRental_ReturnUnregistersItAndReuseRegistersANewSpawn()
        {
            yield return PrepareHostSpawner();
            GameObject first = _spawner.SpawnObject(_spawnPrefab, Vector3.zero, Quaternion.identity, null);
            NetworkIdentity identity = first.GetComponent<NetworkIdentity>();
            uint firstNetId = identity.netId;
            Assert.That(firstNetId, Is.Not.Zero);
            yield return null;
            yield return NetworkTestHelper.WaitForSpawn(identity);
            Assert.That(NetworkServer.spawned.ContainsKey(firstNetId), Is.True);
            Assert.That(NetworkClient.spawned.ContainsKey(firstNetId), Is.True);
            Assert.That(NetworkServer.spawned[firstNetId], Is.SameAs(identity));
            Assert.That(NetworkClient.spawned[firstNetId], Is.SameAs(identity));
            int releases = 0;
            bool releaseSawRegistration = false;
            first.GetComponent<Audit20261023NeoToolsSpawnerPoolProbe>().Released = released =>
            {
                releases++;
                releaseSawRegistration = identity.isServer && NetworkServer.spawned.ContainsKey(firstNetId);
            };
            PooledObjectInfo info = first.GetComponent<PooledObjectInfo>();
            int cleanups = 0;
            bool cleanupSawUnregistered = false;
            Assert.That(info.TryRegisterReleaseCleanup(info.OwnerPool, info.SpawnGeneration, target =>
            {
                cleanups++;
                cleanupSawUnregistered = info.IsInPool && !NetworkServer.spawned.ContainsKey(firstNetId)
                                                        && !NetworkClient.spawned.ContainsKey(firstNetId);
            }), Is.True);

            SpawnUtility.Despawn(first);

            Assert.That(releases, Is.EqualTo(1));
            Assert.That(releaseSawRegistration, Is.True, "Release callbacks retain their existing network context.");
            Assert.That(cleanups, Is.EqualTo(1));
            Assert.That(cleanupSawUnregistered, Is.True, "Generation cleanup follows the existing network release callbacks.");
            Assert.That(NetworkServer.spawned.ContainsKey(firstNetId), Is.False);
            Assert.That(NetworkClient.spawned.ContainsKey(firstNetId), Is.False);
            Assert.That(identity.netId, Is.Zero);
            Assert.That(first.activeSelf, Is.False);
            Assert.That(first.GetComponent<PooledObjectInfo>().IsInPool, Is.True);

            GameObject second = _spawner.SpawnObject(_spawnPrefab, Vector3.one, Quaternion.identity, null);
            Assert.That(second, Is.SameAs(first));
            Assert.That(identity.netId, Is.Not.Zero);
            Assert.That(identity.netId, Is.Not.EqualTo(firstNetId));
            Assert.That(NetworkServer.spawned[identity.netId], Is.SameAs(identity));
            uint secondNetId = identity.netId;
            yield return null;
            yield return NetworkTestHelper.WaitForSpawn(identity);
            Assert.That(identity.netId, Is.EqualTo(secondNetId));
            Assert.That(NetworkServer.spawned[secondNetId], Is.SameAs(identity));
            Assert.That(NetworkClient.spawned[secondNetId], Is.SameAs(identity));
            Assert.That(NetworkServer.spawned.ContainsKey(firstNetId), Is.False);
            Assert.That(NetworkClient.spawned.ContainsKey(firstNetId), Is.False);
            Assert.That(cleanups, Is.EqualTo(1), "Reusing the instance must not replay the old generation's cleanup.");
        }

        [UnityTest]
        public IEnumerator RetiredServerRental_LateReturnUnspawnsAndCleansBeforeDestruction()
        {
            yield return PrepareHostSpawner();
            GameObject rental = _spawner.SpawnObject(_spawnPrefab, Vector3.zero, Quaternion.identity, null);
            PooledObjectInfo info = rental.GetComponent<PooledObjectInfo>();
            NeoObjectPool pool = info.OwnerPool;
            NetworkIdentity identity = rental.GetComponent<NetworkIdentity>();
            uint netId = identity.netId;
            Assert.That(netId, Is.Not.Zero);
            yield return NetworkTestHelper.WaitForSpawn(identity);
            Assert.That(NetworkServer.spawned[netId], Is.SameAs(identity));
            Assert.That(NetworkClient.spawned[netId], Is.SameAs(identity));
            int releases = 0;
            bool releaseSawRegistration = false;
            rental.GetComponent<Audit20261023NeoToolsSpawnerPoolProbe>().Released = released =>
            {
                releases++;
                releaseSawRegistration = identity.isServer && NetworkServer.spawned.ContainsKey(netId)
                                                           && NetworkClient.spawned.ContainsKey(netId);
            };
            int cleanups = 0;
            bool cleanupSawUnregistered = false;
            Assert.That(info.TryRegisterReleaseCleanup(pool, info.SpawnGeneration, target =>
            {
                cleanups++;
                cleanupSawUnregistered = info.IsInPool && !NetworkServer.spawned.ContainsKey(netId)
                                                      && !NetworkClient.spawned.ContainsKey(netId);
            }), Is.True);
            yield return null;

            Assert.That(SpawnUtility.RetirePool(_spawnPrefab), Is.True);
            Assert.That(rental.activeSelf, Is.True);
            Assert.That(NetworkServer.spawned.ContainsKey(netId), Is.True);
            Assert.That(NetworkClient.spawned.ContainsKey(netId), Is.True);
            Assert.That(releases, Is.Zero);
            Assert.That(cleanups, Is.Zero);
            Assert.That(pool.GetObject(Vector3.one, Quaternion.identity), Is.Null);
            SpawnUtility.Despawn(rental);

            Assert.That(releases, Is.EqualTo(1));
            Assert.That(releaseSawRegistration, Is.True, "Late-return release retains both host registrations.");
            Assert.That(NetworkServer.spawned.ContainsKey(netId), Is.False);
            Assert.That(NetworkClient.spawned.ContainsKey(netId), Is.False);
            Assert.That(cleanups, Is.EqualTo(1));
            Assert.That(cleanupSawUnregistered, Is.True, "Late-return cleanup follows network unregistration.");
            Assert.That(identity.netId, Is.Zero);
            Assert.That(info.IsInPool, Is.True);
            Assert.That(rental.activeSelf, Is.False);
            Assert.That(pool.CountInactive, Is.Zero);
            SpawnUtility.Despawn(rental);
            yield return null;
            Assert.That(rental == null, Is.True);
            Assert.That(releases, Is.EqualTo(1));
            Assert.That(cleanups, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);
        }

        private IEnumerator PrepareHostSpawner()
        {
            _manager = NetworkTestHelper.CreateTestNetworkManager("AuditSpawnerNetworkManager", out _managerObject);
            _playerPrefab = new GameObject("AuditSpawnerNetworkPlayer", typeof(NetworkIdentity));
            NetworkTestHelper.SetAssetId(_playerPrefab.GetComponent<NetworkIdentity>(), 42731000);
            _manager.playerPrefab = _playerPrefab;
            yield return null;
            _manager.StartHost();
            yield return NetworkTestHelper.WaitForHost();
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            _spawnPrefab = new GameObject("AuditSpawnerNetworkPrefab", typeof(NetworkIdentity));
            NetworkTestHelper.SetAssetId(_spawnPrefab.GetComponent<NetworkIdentity>(), 42731001);
            _spawnPrefab.SetActive(false);
            _spawnPrefab.AddComponent<Audit20261023NeoToolsSpawnerPoolProbe>();
            NetworkClient.RegisterPrefab(_spawnPrefab);
            // NetworkIdentity.Awake caches its behaviours once: compose and configure before activation.
            _spawnerObject = new GameObject("AuditSpawnerNetwork");
            _spawnerObject.SetActive(false);
            NetworkIdentity spawnerIdentity = _spawnerObject.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(spawnerIdentity, 42731002);
            _spawner = _spawnerObject.AddComponent<Spawner>();
            _spawner.isNetworked = true;
            _spawner.prefabs = new[] { _spawnPrefab };
            _spawner.spawnMode = SpawnMode.Waves;
            _spawner.maxWaves = 1;
            _spawner.minSpawnDelay = _spawner.maxSpawnDelay = 10f;
            FieldInfo waveCount = typeof(Spawner).GetField("_baseWaveCount",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(waveCount, Is.Not.Null);
            waveCount.SetValue(_spawner, 1);
            _spawner.OnWaveStarted = new UnityEvent<int>();
            _spawner.OnObjectSpawned = new UnityEvent<GameObject>();
            _spawner.OnWaveObjectSpawned = new UnityEvent<GameObject, int>();
            _spawnerObject.SetActive(true);
            Assert.That(_spawner.netIdentity, Is.SameAs(spawnerIdentity), "Native Awake must cache the configured identity.");
            Assert.That(spawnerIdentity.NetworkBehaviours, Does.Contain(_spawner));
            Assert.That(spawnerIdentity.NetworkBehaviours[_spawner.ComponentIndex], Is.SameAs(_spawner));
            NetworkServer.Spawn(_spawnerObject);
            yield return null;
            yield return NetworkTestHelper.WaitForSpawn(spawnerIdentity);
            Assert.That(_spawner.netIdentity, Is.SameAs(spawnerIdentity));
            Assert.That(_spawner.netId, Is.Not.Zero);
            Assert.That(NetworkServer.spawned[_spawner.netId], Is.SameAs(spawnerIdentity));
            Assert.That(NetworkClient.spawned[_spawner.netId], Is.SameAs(spawnerIdentity));
        }
    }
}
#endif
