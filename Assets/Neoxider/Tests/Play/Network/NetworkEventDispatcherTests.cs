using System.Collections;
using Mirror;
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class NetworkEventDispatcherTests
    {
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _networkManager =
                NetworkTestHelper.CreateTestNetworkManager("NetworkManagerEventDispatcherTest", out _managerObj);

            var dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 88101);
            _networkManager.playerPrefab = dummyPlayer;

            yield return null;

            _networkManager.StartHost();
            while (!NetworkServer.active || !NetworkClient.isConnected)
            {
                yield return null;
            }

            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_networkManager != null)
            {
                _networkManager.StopHost();
            }

            yield return null;
            if (_managerObj != null)
            {
                Object.DestroyImmediate(_managerObj);
            }
        }

        [UnityTest]
        public IEnumerator DispatchGlobalEvent_Host_FiresOnce()
        {
            var dispatcherObj = new GameObject("NetworkEventDispatcher");
            NetworkEventDispatcher dispatcher = dispatcherObj.AddComponent<NetworkEventDispatcher>();

            NetworkIdentity id = dispatcherObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 88110);
            NetworkClient.RegisterPrefab(dispatcherObj);
            NetworkServer.Spawn(dispatcherObj);
            yield return null;

            int firedCount = 0;
            dispatcher.onNetworkEvent.AddListener(() => firedCount++);

            dispatcher.DispatchGlobalEvent();

            yield return new WaitForSeconds(0.15f);

            Assert.AreEqual(1, firedCount, "Host dispatch should fire once, without local + RPC duplicates.");
            Object.DestroyImmediate(dispatcherObj);
        }

        [UnityTest]
        public IEnumerator DispatchGlobalEvent_DefaultAuthorityNone_AllowsSceneObject()
        {
            var dispatcherObj = new GameObject("NetworkEventDispatcherNone");
            NetworkEventDispatcher dispatcher = dispatcherObj.AddComponent<NetworkEventDispatcher>();
            dispatcher.AuthorityMode = NetworkAuthorityMode.None;

            NetworkIdentity id = dispatcherObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 88111);
            NetworkClient.RegisterPrefab(dispatcherObj);
            NetworkServer.Spawn(dispatcherObj);
            yield return null;

            int firedCount = 0;
            dispatcher.onNetworkEvent.AddListener(() => firedCount++);

            dispatcher.DispatchGlobalEvent();

            yield return new WaitForSeconds(0.15f);

            Assert.AreEqual(1, firedCount, "Default no-authority mode should work on scene objects.");
            Object.DestroyImmediate(dispatcherObj);
        }

        [UnityTest]
        public IEnumerator DispatchGlobalEvent_OfflineFallback_FiresLocally()
        {
            _networkManager.StopHost();
            yield return null;

            var dispatcherObj = new GameObject("NetworkEventDispatcherOffline");
            NetworkEventDispatcher dispatcher = dispatcherObj.AddComponent<NetworkEventDispatcher>();

            int firedCount = 0;
            dispatcher.onNetworkEvent.AddListener(() => firedCount++);

            dispatcher.DispatchGlobalEvent();

            yield return null;

            Assert.AreEqual(1, firedCount, "Without an active network session, dispatcher should fire locally.");
            Object.DestroyImmediate(dispatcherObj);
        }
    }
}
