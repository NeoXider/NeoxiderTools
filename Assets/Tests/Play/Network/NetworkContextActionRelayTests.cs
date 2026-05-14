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
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;
        private GameObject _scenePlayerTemplate;
        private GameObject _relayHost;
        private NetworkContextActionRelay _relay;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _managerObj = new GameObject("NetworkContextActionRelayTest");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();
            Transport.active = transport;

            _scenePlayerTemplate = new GameObject("ScenePlayerTemplate");
            _scenePlayerTemplate.AddComponent<NetworkIdentity>().sceneId = 54321;
            _scenePlayerTemplate.AddComponent<NeoNetworkPlayer>();
            _scenePlayerTemplate.AddComponent<BoxCollider>();

            var sphere = new GameObject("Sphere");
            sphere.transform.SetParent(_scenePlayerTemplate.transform);
            sphere.SetActive(false);

            _networkManager.UseScenePlayerTemplate = true;
            _networkManager.ScenePlayerTemplate = _scenePlayerTemplate;
            _networkManager.ScenePlayerTemplateSpawnId = "neo-tests-context-relay";
            _networkManager.DisableScenePlayerTemplate = true;
            _networkManager.playerPrefab = null;

            _relayHost = new GameObject("ContextRelayHost");
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

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_networkManager != null)
            {
                _networkManager.StopNetwork();
            }

            yield return null;

            NetworkManager.ResetStatics();
            NetworkClient.ClearSpawners();

            if (_relayHost != null)
            {
                Object.DestroyImmediate(_relayHost);
            }

            if (_managerObj != null)
            {
                Object.DestroyImmediate(_managerObj);
            }

            if (_scenePlayerTemplate != null)
            {
                Object.DestroyImmediate(_scenePlayerTemplate);
            }
        }

        [UnityTest]
        public IEnumerator Host_TriggerWithCollider_EnablesSphereOnlyOnMatchingPlayerClone()
        {
            _networkManager.StartHost();

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
            {
                yield return null;
            }

            yield return null;

            NetworkServer.Spawn(_relayHost);
            while (_relayHost.GetComponent<NetworkIdentity>().netId == 0)
            {
                yield return null;
            }

            yield return null;

            GameObject firstPlayer = NetworkClient.localPlayer.gameObject;
            GameObject secondPlayer = CreateScenePlayerCopyForTest();
            NetworkIdentity secondIdentity = secondPlayer.GetComponent<NetworkIdentity>();
            Assert.IsNotNull(secondIdentity, "Scene player copy must have a root NetworkIdentity.");
            NetworkServer.Spawn(secondPlayer);
            for (int safety = 0; safety < 120 && secondIdentity.netId == 0; safety++)
            {
                yield return null;
            }

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
            yield return null;

            Assert.IsTrue(firstSphere.gameObject.activeSelf, "Pickup should enable Sphere on the entered player clone.");
            Assert.IsFalse(secondSphere.gameObject.activeSelf, "Other player Sphere must stay inactive.");
            Assert.IsFalse(templateSphere.gameObject.activeSelf, "Scene template Sphere must not be toggled.");

            Collider secondCollider = secondPlayer.GetComponent<Collider>();
            _relay.Trigger(secondCollider);
            yield return null;

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

            while (!NetworkServer.active || !NetworkClient.isConnected || NetworkClient.localPlayer == null)
            {
                yield return null;
            }

            yield return null;

            NetworkServer.Spawn(_relayHost);
            while (_relayHost.GetComponent<NetworkIdentity>().netId == 0)
            {
                yield return null;
            }

            yield return null;

            _relay.ContextSource = NetworkContextSourceMode.LocalPlayer;
            _relay.RootMode = NetworkContextRootMode.NetworkIdentityInParents;

            GameObject local = NetworkClient.localPlayer.gameObject;
            Transform sphere = local.transform.Find("Sphere");
            Assert.IsFalse(sphere.gameObject.activeSelf);

            _relay.Trigger();
            yield return null;

            Assert.IsTrue(sphere.gameObject.activeSelf);
        }

        private GameObject CreateScenePlayerCopyForTest()
        {
            MethodInfo createMethod = typeof(NeoNetworkManager).GetMethod("TryCreateScenePlayer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(createMethod);

            object[] args = { null, null };
            bool created = (bool)createMethod.Invoke(_networkManager, args);
            Assert.IsTrue(created);
            return (GameObject)args[1];
        }
    }
}
