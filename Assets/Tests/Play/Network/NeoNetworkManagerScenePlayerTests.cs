using System.Collections;
using System.Reflection;
using Mirror;
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class NeoNetworkManagerScenePlayerTests
    {
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;
        private GameObject _scenePlayerTemplate;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _managerObj = new GameObject("NetworkManagerScenePlayerTest");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();
            Transport.active = transport;

            _scenePlayerTemplate = new GameObject("ScenePlayerTemplate");
            _scenePlayerTemplate.AddComponent<NetworkIdentity>().sceneId = 12345;
            _scenePlayerTemplate.AddComponent<NeoNetworkPlayer>();

            var stats = new GameObject("Stats");
            stats.transform.SetParent(_scenePlayerTemplate.transform);
            stats.AddComponent<Counter>();

            var weapon = new GameObject("Weapon");
            weapon.transform.SetParent(_scenePlayerTemplate.transform);
            weapon.AddComponent<WeaponMarker>();
            weapon.SetActive(false);

            _scenePlayerTemplate.AddComponent<BoxCollider>();

            _networkManager.UseScenePlayerTemplate = true;
            _networkManager.ScenePlayerTemplate = _scenePlayerTemplate;
            _networkManager.ScenePlayerTemplateSpawnId = "neo-tests-scene-player-template";
            _networkManager.DisableScenePlayerTemplate = true;
            _networkManager.playerPrefab = null;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_networkManager != null)
                _networkManager.StopNetwork();

            yield return null;

            if (_managerObj != null)
                Object.DestroyImmediate(_managerObj);

            if (_scenePlayerTemplate != null)
                Object.DestroyImmediate(_scenePlayerTemplate);

            NetworkClient.ClearSpawners();
        }

        [UnityTest]
        public IEnumerator ScenePlayerTemplate_StartHost_DisablesTemplateAndSpawnsPlayerCopy()
        {
            _networkManager.StartHost();

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
                yield return null;

            yield return null;

            GameObject spawnedPlayer = NetworkClient.localPlayer.gameObject;

            Assert.IsFalse(_scenePlayerTemplate.activeSelf, "Scene template should stay disabled at runtime.");
            Assert.AreNotSame(_scenePlayerTemplate, spawnedPlayer, "Network player should be a spawned copy, not the scene template.");
            Assert.IsTrue(spawnedPlayer.activeSelf, "Spawned player copy should be active.");
            Assert.AreEqual(0UL, spawnedPlayer.GetComponent<NetworkIdentity>().sceneId, "Spawned copy must not keep the template sceneId.");
            Assert.AreEqual(12345UL, _scenePlayerTemplate.GetComponent<NetworkIdentity>().sceneId,
                "Scene template should keep its sceneId after cloning.");
        }

        [UnityTest]
        public IEnumerator SceneObjectAssignedAsPlayerPrefab_AutoSwitchesToSceneTemplateMode()
        {
            _networkManager.UseScenePlayerTemplate = false;
            _networkManager.ScenePlayerTemplate = null;
            _networkManager.playerPrefab = _scenePlayerTemplate;

            _networkManager.StartHost();

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
                yield return null;

            yield return null;

            Assert.IsTrue(_networkManager.UseScenePlayerTemplate,
                "Scene object assigned to Player Prefab should automatically enable scene template mode.");
            Assert.AreEqual(_scenePlayerTemplate, _networkManager.ScenePlayerTemplate,
                "Scene object from Player Prefab should become the Scene Player Template.");
            Assert.IsNull(_networkManager.playerPrefab,
                "Player Prefab should be cleared before Mirror registers prefabs.");
            Assert.IsFalse(_scenePlayerTemplate.activeSelf,
                "Scene template should be disabled so only spawned copies remain active.");
        }

        [UnityTest]
        public IEnumerator ScenePlayerTemplate_PlayerNoCodeComponents_AreClonedPerSpawnedPlayer()
        {
            _networkManager.StartHost();

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
                yield return null;

            yield return null;

            GameObject spawnedPlayer = NetworkClient.localPlayer.gameObject;
            Counter templateCounter = _scenePlayerTemplate.GetComponentInChildren<Counter>(true);
            Counter spawnedCounter = spawnedPlayer.GetComponentInChildren<Counter>(true);

            Assert.IsNotNull(templateCounter, "Scene template should contain the configured NoCode counter.");
            Assert.IsNotNull(spawnedCounter, "Spawned player should receive a cloned NoCode counter.");
            Assert.AreNotSame(templateCounter, spawnedCounter, "Spawned player must not reuse the template counter instance.");

            spawnedCounter.Set(10);
            yield return null;

            Assert.AreEqual(10, spawnedCounter.ValueInt, "Changing the spawned player's counter should affect that player clone.");
            Assert.AreEqual(0, templateCounter.ValueInt, "Changing the spawned player must not mutate the disabled scene template.");
        }

        [UnityTest]
        public IEnumerator ScenePlayerTemplate_TriggerPickup_EnablesWeaponOnlyForEnteredPlayerClone()
        {
            _networkManager.StartHost();

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
                yield return null;

            yield return null;

            GameObject firstPlayer = NetworkClient.localPlayer.gameObject;
            GameObject secondPlayer = CreateScenePlayerCopyForTest();
            var pickup = new GameObject("WeaponPickup").AddComponent<WeaponPickupTrigger>();

            WeaponMarker firstWeapon = firstPlayer.GetComponentInChildren<WeaponMarker>(true);
            WeaponMarker secondWeapon = secondPlayer.GetComponentInChildren<WeaponMarker>(true);
            WeaponMarker templateWeapon = _scenePlayerTemplate.GetComponentInChildren<WeaponMarker>(true);

            Assert.IsFalse(firstWeapon.gameObject.activeSelf, "First player starts without weapon equipped.");
            Assert.IsFalse(secondWeapon.gameObject.activeSelf, "Second player starts without weapon equipped.");
            Assert.IsFalse(templateWeapon.gameObject.activeSelf, "Scene template weapon stays disabled.");

            pickup.Pickup(firstPlayer.GetComponent<Collider>());
            yield return null;

            Assert.IsTrue(firstWeapon.gameObject.activeSelf, "Pickup should enable weapon on the entered first player.");
            Assert.IsFalse(secondWeapon.gameObject.activeSelf, "First pickup must not enable weapon on the second player.");
            Assert.IsFalse(templateWeapon.gameObject.activeSelf, "Pickup must not mutate the disabled scene template.");

            pickup.Pickup(secondPlayer.GetComponent<Collider>());
            yield return null;

            Assert.IsTrue(firstWeapon.gameObject.activeSelf, "First player should keep the picked weapon.");
            Assert.IsTrue(secondWeapon.gameObject.activeSelf, "Second player should enable its own cloned weapon after entering.");
            Assert.IsFalse(templateWeapon.gameObject.activeSelf, "Scene template weapon should remain a clean template.");

            Object.DestroyImmediate(pickup.gameObject);
            Object.DestroyImmediate(secondPlayer);
        }

        private GameObject CreateScenePlayerCopyForTest()
        {
            MethodInfo createMethod = typeof(NeoNetworkManager).GetMethod("TryCreateScenePlayer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(createMethod, "NeoNetworkManager.TryCreateScenePlayer should exist.");

            object[] args = { null, null };
            bool created = (bool)createMethod.Invoke(_networkManager, args);
            Assert.IsTrue(created, "Scene player template should create runtime player copies.");
            return (GameObject)args[1];
        }

        private sealed class WeaponMarker : MonoBehaviour
        {
        }

        private sealed class WeaponPickupTrigger : MonoBehaviour
        {
            public void Pickup(Collider enteredCollider)
            {
                if (enteredCollider == null)
                    return;

                WeaponMarker weapon = enteredCollider.GetComponentInChildren<WeaponMarker>(true);
                if (weapon != null)
                    weapon.gameObject.SetActive(true);
            }
        }
    }
}
