#if UNITY_EDITOR
using System.Collections;
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class MultSceneNetworkTests
    {
        private const string MultScenePath = "Assets/Scenes/Mult.unity";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            NeoNetworkManager manager = Object.FindFirstObjectByType<NeoNetworkManager>();
            if (manager != null)
            {
                manager.StopNetwork();
            }

            yield return null;
            NetworkClient.ClearSpawners();
        }

        [UnityTest]
        public IEnumerator MultScene_StartHost_UsesSceneTemplateAndLeavesOnlyRuntimePlayerActive()
        {
            Scene scene = EditorSceneManager.LoadSceneInPlayMode(MultScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));

            while (!scene.isLoaded)
            {
                yield return null;
            }

            yield return null;

            NeoNetworkManager manager = Object.FindFirstObjectByType<NeoNetworkManager>();
            Assert.IsNotNull(manager, "Mult scene should contain a NeoNetworkManager.");

            GameObject sceneTemplate = manager.ScenePlayerTemplate != null
                ? manager.ScenePlayerTemplate
                : GameObject.Find("First Person Controller");

            Assert.IsNotNull(sceneTemplate, "Mult scene should contain a scene-authored First Person Controller template.");
            Assert.IsTrue(manager.UseScenePlayerTemplate || manager.playerPrefab == sceneTemplate,
                "Mult scene should use Scene Player Template mode or auto-migrate a scene object assigned as Player Prefab.");

            manager.StartHost();

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
            {
                yield return null;
            }

            yield return null;

            GameObject localPlayer = NetworkClient.localPlayer.gameObject;
            Assert.AreNotSame(sceneTemplate, localPlayer, "Host player should be a runtime copy, not the scene template.");
            Assert.IsFalse(sceneTemplate.activeSelf, "Scene-authored player template should be disabled after host start.");

            NeoNetworkPlayer[] players = Object.FindObjectsByType<NeoNetworkPlayer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int activePlayers = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].gameObject.activeInHierarchy)
                {
                    activePlayers++;
                }
            }

            Assert.AreEqual(1, activePlayers, "Host should leave only one active runtime player in Mult scene.");
            Assert.IsTrue(localPlayer.activeInHierarchy, "NetworkClient.localPlayer should be the active runtime player.");
            Assert.AreEqual(0UL, localPlayer.GetComponent<NetworkIdentity>().sceneId,
                "Runtime player copy must not keep the template sceneId.");
        }
    }
}
#endif
