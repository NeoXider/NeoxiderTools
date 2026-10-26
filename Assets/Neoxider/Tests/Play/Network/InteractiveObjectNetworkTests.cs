using System.Collections;
#if MIRROR
using Mirror;
#endif
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class InteractiveObjectNetworkTests
    {
#if MIRROR
        private NetworkTestSession _session;
        private GameObject _managerObj;
        private GameObject _playerPrefab;
        private NeoNetworkManager _networkManager;

        private GameObject _objInteractive;
        private InteractiveObject _interactiveObject;

        private bool _eventFired;

        private GameObject _cameraObj;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            if (TestContext.CurrentContext.Test.MethodName ==
                nameof(InteractiveObject_OfflineFallback_FiresLocally))
            {
                yield break;
            }

            _session = new NetworkTestSession();
            _eventFired = false;

            _cameraObj = _session.Own(new GameObject("MainCamera"));
            _cameraObj.tag = "MainCamera";
            _cameraObj.AddComponent<Camera>();

            _networkManager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager("NetworkManager", out _managerObj));

            _playerPrefab = _session.Own(new GameObject("DummyPlayer"));
            NetworkIdentity dummyId = _playerPrefab.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 99999);
            _networkManager.playerPrefab = _playerPrefab;

            yield return null;

            _objInteractive = _session.Own(new GameObject("InteractiveObject"));
            _objInteractive.SetActive(false);

            // Configure all behaviours before the inactive identity awakens.
            _interactiveObject = _objInteractive.AddComponent<InteractiveObject>();
            if (_interactiveObject.onInteractDown == null)
            {
                _interactiveObject.onInteractDown = new UnityEngine.Events.UnityEvent();
            }
            if (_interactiveObject.onClick == null)
            {
                _interactiveObject.onClick = new UnityEngine.Events.UnityEvent();
            }

            NetworkIdentity identity = _objInteractive.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(identity, 12345);

            _objInteractive.SetActive(true);
            NetworkClient.RegisterPrefab(_objInteractive);

            _networkManager.StartHost();
            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);

            NetworkServer.Spawn(_objInteractive);
            yield return NetworkTestHelper.WaitForSpawn(identity);
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            yield return null;
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
        public IEnumerator InteractiveObject_NetworkedHostPublicApi_FiresEachEventOnce()
        {
            _interactiveObject.IsNetworked = true;
            _interactiveObject.AuthorityMode = NetworkAuthorityMode.None;

            int interactDownCount = 0;
            int clickCount = 0;
            _interactiveObject.onInteractDown.AddListener(() =>
            {
                _eventFired = true;
                interactDownCount++;
            });
            _interactiveObject.onClick.AddListener(() => clickCount++);

            _interactiveObject.InteractDown();
            _interactiveObject.Click();

            yield return NetworkTestHelper.WaitUntil(() => interactDownCount > 0 && clickCount > 0,
                description: "interactive host events");
            yield return NetworkTestHelper.ReliableBarrier("interactive host events receive barrier");

            Assert.IsTrue(_eventFired, "Host should execute the networked interaction.");
            Assert.AreEqual(1, interactDownCount,
                "Host should not receive duplicate local + RPC interaction events.");
            Assert.AreEqual(1, clickCount, "Host should not receive duplicate local + RPC click events.");
        }

#endif

        [UnityTest]
        public IEnumerator InteractiveObject_OfflineFallback_FiresLocally()
        {
#if MIRROR
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False,
                "This local interaction positive must not take over a borrowed session.");
#endif
            GameObject interactiveObject = new GameObject("OfflineInteractiveObject");
            try
            {
                interactiveObject.SetActive(false);
#if MIRROR
                interactiveObject.AddComponent<NetworkIdentity>();
#endif
                InteractiveObject interactive = interactiveObject.AddComponent<InteractiveObject>();
                interactive.IsNetworked = false;
                interactive.onInteractDown = new UnityEngine.Events.UnityEvent();
                interactiveObject.SetActive(true);
                int eventCount = 0;
                interactive.onInteractDown.AddListener(() => eventCount++);
                interactive.InteractDown();
                Assert.That(eventCount, Is.EqualTo(1), "A non-networked public interaction fires locally once.");
                yield break;
            }
            finally
            {
                Object.DestroyImmediate(interactiveObject);
            }
        }

#if MIRROR
        [UnityTest]
        public IEnumerator InteractiveObject_DefaultAuthorityNone_AllowsSceneObject()
        {
            _interactiveObject.IsNetworked = true;
            _interactiveObject.AuthorityMode = NetworkAuthorityMode.None;

            int eventCount = 0;
            _interactiveObject.onInteractDown.AddListener(() => eventCount++);

            _interactiveObject.InteractDown();

            yield return NetworkTestHelper.WaitUntil(() => eventCount > 0,
                description: "interactive no-authority event");
            yield return NetworkTestHelper.ReliableBarrier("interactive no-authority event receive barrier");

            Assert.AreEqual(1, eventCount, "Default no-authority mode should work on scene objects.");
        }

        [UnityTest]
        public IEnumerator NetworkAuthorityMode_Helper_FiltersRemoteSender()
        {
            NetworkConnectionToClient remoteSender = new NetworkConnectionToClient(42, "remote");

            Assert.IsTrue(NeoNetworkState.IsAuthorized(_objInteractive, remoteSender, NetworkAuthorityMode.None));
            Assert.IsFalse(NeoNetworkState.IsAuthorized(_objInteractive, remoteSender,
                NetworkAuthorityMode.ServerOnly));
            Assert.IsFalse(NeoNetworkState.IsAuthorized(_objInteractive, remoteSender, NetworkAuthorityMode.OwnerOnly));

            yield return null;
        }
#endif
    }
}
