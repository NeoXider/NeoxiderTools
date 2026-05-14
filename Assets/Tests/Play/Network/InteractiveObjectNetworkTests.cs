using System.Collections;
using Mirror;
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class InteractiveObjectNetworkTests
    {
        private GameObject _managerObj;
        private NeoNetworkManager _networkManager;

        private GameObject _objInteractive;
        private InteractiveObject _interactiveObject;

        private bool _eventFired;

        private GameObject _cameraObj;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _eventFired = false;

            _cameraObj = new GameObject("MainCamera");
            _cameraObj.tag = "MainCamera";
            _cameraObj.AddComponent<Camera>();

            // Setup NetworkManager
            _managerObj = new GameObject("NetworkManager");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();

            GameObject dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 99999);
            _networkManager.playerPrefab = dummyPlayer;

            Transport.active = transport;
            yield return null;

            // Setup Interactive Object
            _objInteractive = new GameObject("InteractiveObject");
            
            // Add NetworkBehaviour FIRST so NetworkIdentity.Awake() wires the netIdentity property
            _interactiveObject = _objInteractive.AddComponent<InteractiveObject>();
            if (_interactiveObject.onInteractDown == null) _interactiveObject.onInteractDown = new UnityEngine.Events.UnityEvent();

            NetworkIdentity identity = _objInteractive.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(identity, 12345);

            NetworkClient.RegisterPrefab(_objInteractive);

            // Start Host
            _networkManager.StartHost();
            while (!NetworkServer.active || !NetworkClient.isConnected) yield return null;
            
            // Spawn Singleton on server
            NetworkServer.Spawn(_objInteractive);
            if (!NetworkClient.ready) NetworkClient.Ready();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_networkManager != null) _networkManager.StopHost();
            yield return null;

            if (_managerObj != null) Object.DestroyImmediate(_managerObj);
            if (_objInteractive != null) Object.DestroyImmediate(_objInteractive);
            if (_cameraObj != null) Object.DestroyImmediate(_cameraObj);
        }

        [UnityTest]
        public IEnumerator InteractiveObject_NetworkedHostInteract_FiresOnce()
        {
            _interactiveObject.IsNetworked = true;
            _interactiveObject.AuthorityMode = NetworkAuthorityMode.None;

            int eventCount = 0;
            _interactiveObject.onInteractDown.AddListener(() =>
            {
                _eventFired = true;
                eventCount++;
            });

            var triggerMethod = typeof(InteractiveObject).GetMethod("TriggerInteractDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            triggerMethod.Invoke(_interactiveObject, null);

            yield return new WaitForSeconds(0.1f);
            
            Assert.IsTrue(_eventFired, "Host should execute the networked interaction.");
            Assert.AreEqual(1, eventCount, "Host should not receive duplicate local + RPC interaction events.");
        }

        [UnityTest]
        public IEnumerator InteractiveObject_OfflineFallback_FiresLocally()
        {
            _interactiveObject.IsNetworked = false;

            int eventCount = 0;
            _interactiveObject.onInteractDown.AddListener(() => eventCount++);

            var triggerMethod = typeof(InteractiveObject).GetMethod("TriggerInteractDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            triggerMethod.Invoke(_interactiveObject, null);

            yield return null;

            Assert.AreEqual(1, eventCount, "Offline/non-networked interaction should fire locally.");
        }

        [UnityTest]
        public IEnumerator InteractiveObject_DefaultAuthorityNone_AllowsSceneObject()
        {
            _interactiveObject.IsNetworked = true;
            _interactiveObject.AuthorityMode = NetworkAuthorityMode.None;

            int eventCount = 0;
            _interactiveObject.onInteractDown.AddListener(() => eventCount++);

            var triggerMethod = typeof(InteractiveObject).GetMethod("TriggerInteractDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            triggerMethod.Invoke(_interactiveObject, null);

            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(1, eventCount, "Default no-authority mode should work on scene objects.");
        }

        [UnityTest]
        public IEnumerator NetworkAuthorityMode_Helper_FiltersRemoteSender()
        {
            var remoteSender = new NetworkConnectionToClient(42, "remote");

            Assert.IsTrue(NeoNetworkState.IsAuthorized(_objInteractive, remoteSender, NetworkAuthorityMode.None));
            Assert.IsFalse(NeoNetworkState.IsAuthorized(_objInteractive, remoteSender, NetworkAuthorityMode.ServerOnly));
            Assert.IsFalse(NeoNetworkState.IsAuthorized(_objInteractive, remoteSender, NetworkAuthorityMode.OwnerOnly));

            yield return null;
        }
    }
}
