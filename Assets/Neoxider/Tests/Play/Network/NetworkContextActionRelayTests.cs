#if MIRROR
using System.Collections;
using System.Reflection;
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class NetworkContextActionRelayTests
    {
        private NetworkTestSession _session;
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;
        private GameObject _scenePlayerTemplate;
        private GameObject _relayHost;
        private NetworkContextActionRelay _relay;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            _session = new NetworkTestSession();
            _networkManager =
                _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager("NetworkContextActionRelayTest", out _managerObj));

            _scenePlayerTemplate = _session.Own(new GameObject("ScenePlayerTemplate"));
            _scenePlayerTemplate.AddComponent<NetworkIdentity>().sceneId = 54321;
            _scenePlayerTemplate.AddComponent<NeoNetworkPlayer>();
            _scenePlayerTemplate.AddComponent<BoxCollider>();

            GameObject sphere = _session.Own(new GameObject("Sphere"));
            sphere.transform.SetParent(_scenePlayerTemplate.transform);
            sphere.SetActive(false);

            _networkManager.UseScenePlayerTemplate = true;
            _networkManager.ScenePlayerTemplate = _scenePlayerTemplate;
            _networkManager.ScenePlayerTemplateSpawnId = "neo-tests-context-relay";
            _networkManager.DisableScenePlayerTemplate = true;
            _networkManager.playerPrefab = null;

            _relayHost = _session.Own(new GameObject("ContextRelayHost"));
            _relayHost.SetActive(false);
            _relayHost.transform.SetParent(_managerObj.transform);
            _relayHost.AddComponent<NetworkIdentity>();
            _relay = _relayHost.AddComponent<NetworkContextActionRelay>();
            _relay.isNetworked = true;
            _relay.ContextSource = NetworkContextSourceMode.EventArgument;
            _relay.RootMode = NetworkContextRootMode.NetworkIdentityInParents;
            _relay.TargetMode = NetworkContextTargetMode.ChildByName;
            _relay.TargetChildName = "Sphere";
            _relay.Action = NetworkContextActionType.SetActive;
            _relay.ActionBoolValue = true;
            _relay.Scope = NetworkActionScope.AllClients;
            _relay.AuthorityMode = NetworkAuthorityMode.None;
            // WHY: The host test deliberately triggers with a non-local player's collider; the new local-only
            // de-dup filter would skip it. Disable for this synthetic scenario.
            _relay.TriggerOnlyForLocalContext = false;
            _relayHost.SetActive(true);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_session != null)
            {
                yield return _session.Cleanup();
                _session = null;
            }
        }

        [UnityTest]
        public IEnumerator Host_TriggerWithCollider_EnablesSphereOnlyOnMatchingPlayerClone()
        {
            _networkManager.StartHost();

            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);

            yield return null;

            NetworkServer.Spawn(_relayHost);
            yield return NetworkTestHelper.WaitUntil(
                () => _relayHost.GetComponent<NetworkIdentity>().netId != 0,
                description: "context relay spawn");

            yield return null;

            GameObject firstPlayer = NetworkClient.localPlayer.gameObject;
            GameObject secondPlayer = CreateScenePlayerCopyForTest();
            NetworkIdentity secondIdentity = secondPlayer.GetComponent<NetworkIdentity>();
            Assert.IsNotNull(secondIdentity, "Scene player copy must have a root NetworkIdentity.");
            NetworkServer.Spawn(secondPlayer);
            yield return NetworkTestHelper.WaitForSpawn(secondIdentity);

            Assert.AreNotEqual(0u, secondIdentity.netId,
                "Second player must be server-spawned so relay messages carry a valid context netId.");

            Transform firstSphere = firstPlayer.transform.Find("Sphere");
            Transform secondSphere = secondPlayer.transform.Find("Sphere");
            Transform templateSphere = _scenePlayerTemplate.transform.Find("Sphere");

            Assert.IsNotNull(firstSphere);
            Assert.IsNotNull(secondSphere);
            Assert.IsFalse(firstSphere.gameObject.activeSelf);
            Assert.IsFalse(secondSphere.gameObject.activeSelf);
            Assert.IsFalse(templateSphere.gameObject.activeSelf);

            Collider firstCollider = firstPlayer.GetComponent<Collider>();
            Assert.IsNotNull(firstCollider);

            _relay.Trigger(firstCollider);
            yield return NetworkTestHelper.ReliableBarrier("first context relay receive barrier");

            Assert.IsTrue(firstSphere.gameObject.activeSelf,
                "Pickup should enable Sphere on the entered player clone.");
            Assert.IsFalse(secondSphere.gameObject.activeSelf, "Other player Sphere must stay inactive.");
            Assert.IsFalse(templateSphere.gameObject.activeSelf, "Scene template Sphere must not be toggled.");

            Collider secondCollider = secondPlayer.GetComponent<Collider>();
            _relay.Trigger(secondCollider);
            yield return NetworkTestHelper.ReliableBarrier("second context relay receive barrier");

            Assert.IsTrue(firstSphere.gameObject.activeSelf);
            Assert.IsTrue(secondSphere.gameObject.activeSelf);
            Assert.IsFalse(templateSphere.gameObject.activeSelf);

            if (NetworkServer.active && secondIdentity.netId != 0)
            {
                NetworkServer.Destroy(secondPlayer);
            }
            else
            {
                Object.DestroyImmediate(secondPlayer);
            }
        }

        [UnityTest]
        public IEnumerator Host_TriggerLocalPlayer_EnablesSphereOnLocalRuntimePlayer()
        {
            _networkManager.StartHost();

            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);

            yield return null;

            NetworkServer.Spawn(_relayHost);
            yield return NetworkTestHelper.WaitUntil(
                () => _relayHost.GetComponent<NetworkIdentity>().netId != 0,
                description: "context relay spawn");

            yield return null;

            _relay.ContextSource = NetworkContextSourceMode.LocalPlayer;
            _relay.RootMode = NetworkContextRootMode.NetworkIdentityInParents;

            GameObject local = NetworkClient.localPlayer.gameObject;
            Transform sphere = local.transform.Find("Sphere");
            Assert.IsFalse(sphere.gameObject.activeSelf);

            _relay.Trigger();
            yield return NetworkTestHelper.ReliableBarrier("local context relay receive barrier");

            Assert.IsTrue(sphere.gameObject.activeSelf);
        }

        private GameObject CreateScenePlayerCopyForTest()
        {
            MethodInfo createMethod = typeof(NeoNetworkManager).GetMethod("TryCreateScenePlayer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(createMethod);

            object[] args = { null, null };
            bool created = (bool)createMethod.Invoke(_networkManager, args);
            _session.Own((GameObject)args[1]);
            Assert.IsTrue(created);
            return (GameObject)args[1];
        }
    }
}
#endif
