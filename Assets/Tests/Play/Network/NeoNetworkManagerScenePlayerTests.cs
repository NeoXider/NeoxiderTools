using System.Collections;
using Mirror;
using Neo.Network;
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
            _scenePlayerTemplate.AddComponent<NetworkIdentity>();
            _scenePlayerTemplate.AddComponent<NeoNetworkPlayer>();

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
        }
    }
}
