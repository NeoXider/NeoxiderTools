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
            NeoNetworkManager[] managers = Object.FindObjectsByType<NeoNetworkManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null)
                {
                    managers[i].StopNetwork();
                }
            }

            yield return null;

            NetworkManager.ResetStatics();
            NetworkClient.ClearSpawners();

            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null)
                {
                    Object.DestroyImmediate(managers[i].gameObject);
                }
            }

            yield return null;
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
            NetworkTestHelper.UseDummyTransport(manager);

            GameObject sceneTemplate = manager.ScenePlayerTemplate != null
                ? manager.ScenePlayerTemplate
                : GameObject.Find("First Person Controller");

            Assert.IsNotNull(sceneTemplate,
                "Mult scene should contain a scene-authored First Person Controller template.");
            Assert.IsTrue(manager.UseScenePlayerTemplate || manager.playerPrefab == sceneTemplate,
                "Mult scene should use Scene Player Template mode or auto-migrate a scene object assigned as Player Prefab.");

            manager.StartHost();

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
            {
                yield return null;
            }

            yield return null;

            GameObject localPlayer = NetworkClient.localPlayer.gameObject;
            Assert.AreNotSame(sceneTemplate, localPlayer,
                "Host player should be a runtime copy, not the scene template.");
            Assert.IsFalse(sceneTemplate.activeSelf,
                "Scene-authored player template should be disabled after host start.");

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
            Assert.IsTrue(localPlayer.activeInHierarchy,
                "NetworkClient.localPlayer should be the active runtime player.");
            Assert.AreEqual(0UL, localPlayer.GetComponent<NetworkIdentity>().sceneId,
                "Runtime player copy must not keep the template sceneId.");
        }

        [UnityTest]
        public IEnumerator MultScene_NetworkContextActionRelay_PickupDisablesCubeAndEnablesPlayerSphere()
        {
            // Mult.unity wires Trigger Cube (1) with TWO NetworkContextActionRelays:
            //   1. Self → Root → SetActive(false) — the cube itself disappears (visible to all clients).
            //   2. EventArgument → NetworkIdentityInParents → ChildByName("Sphere") → SetActive(true) —
            //      the entering player's Sphere child lights up (visible to all clients).
            // Both fire from PhysicsEvents3D.onTriggerEnter.
            Scene scene = EditorSceneManager.LoadSceneInPlayMode(MultScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));

            while (!scene.isLoaded)
            {
                yield return null;
            }

            yield return null;

            NetworkContextActionRelay[] relays = FindAllNetworkContextActionRelaysInLoadedScenes();
            Assert.AreEqual(2, relays.Length,
                "Mult.unity should now contain two NetworkContextActionRelays on Trigger Cube (1): pickup-self + bonus-on-player.");

            NetworkContextActionRelay pickupRelay = null;
            NetworkContextActionRelay bonusRelay = null;
            for (int i = 0; i < relays.Length; i++)
            {
                if (relays[i].ContextSource == NetworkContextSourceMode.Self)
                {
                    pickupRelay = relays[i];
                }
                else if (relays[i].ContextSource == NetworkContextSourceMode.EventArgument)
                {
                    bonusRelay = relays[i];
                }
            }

            Assert.IsNotNull(pickupRelay, "One relay must be configured as Pickup (ContextSource=Self).");
            Assert.IsNotNull(bonusRelay, "One relay must be configured as Bonus (ContextSource=EventArgument).");

            NeoNetworkManager manager = Object.FindFirstObjectByType<NeoNetworkManager>();
            Assert.IsNotNull(manager);
            NetworkTestHelper.UseDummyTransport(manager);

            manager.StartHost();

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
            {
                yield return null;
            }

            yield return null;

            Assert.IsTrue(pickupRelay.gameObject.activeSelf,
                "Pickup trigger cube must be active once the server has spawned scene objects.");

            GameObject localPlayer = NetworkClient.localPlayer.gameObject;
            Transform sphere = FindDeepChild(localPlayer.transform, "Sphere");
            Assert.IsNotNull(sphere, "Runtime player should contain a child named Sphere.");
            Assert.IsFalse(sphere.gameObject.activeSelf, "Sphere should start disabled on the runtime player.");

            Collider playerCollider = localPlayer.GetComponentInChildren<Collider>(true);
            Assert.IsNotNull(playerCollider, "Runtime player should have at least one Collider for trigger tests.");

            pickupRelay.Trigger(playerCollider);
            bonusRelay.Trigger(playerCollider);
            yield return null;

            Assert.IsFalse(pickupRelay.gameObject.activeSelf,
                "Pickup relay should disable its own GameObject when the local player triggers it.");
            Assert.IsTrue(sphere.gameObject.activeSelf,
                "Bonus relay should enable the Sphere child of the entering player.");
        }

        private static NetworkContextActionRelay[] FindAllNetworkContextActionRelaysInLoadedScenes()
        {
            return Object.FindObjectsByType<NetworkContextActionRelay>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static NetworkContextActionRelay FindNetworkContextActionRelayInLoadedScenes()
        {
            NetworkContextActionRelay[] relays = Object.FindObjectsByType<NetworkContextActionRelay>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < relays.Length; i++)
            {
                if (relays[i] != null)
                {
                    return relays[i];
                }
            }

            const string triggerName = "Trigger Cube (1)";
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene candidate = SceneManager.GetSceneAt(s);
                if (!candidate.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = candidate.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    Transform found = FindDeepChild(roots[r].transform, triggerName);
                    if (found != null)
                    {
                        NetworkContextActionRelay onFound = found.GetComponent<NetworkContextActionRelay>();
                        if (onFound != null)
                        {
                            return onFound;
                        }
                    }
                }
            }

            return null;
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
#endif
