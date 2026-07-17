using System.Collections;
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class DummyTransport : Transport
    {
        public override bool Available()
        {
            return true;
        }

        public override int GetMaxPacketSize(int channelId = 0)
        {
            return 1200;
        }

        public override bool ServerActive()
        {
            return true;
        }

        public override void ServerStart() { }
        public override void ServerSend(int connectionId, System.ArraySegment<byte> segment, int channelId = 0) { }
        public override void ServerDisconnect(int connectionId) { }

        public override string ServerGetClientAddress(int connectionId)
        {
            return "127.0.0.1";
        }

        public override void ServerStop() { }
        public override void ClientConnect(string address) { }
        public override void ClientConnect(System.Uri uri) { }
        public override void ClientSend(System.ArraySegment<byte> segment, int channelId = 0) { }
        public override void ClientDisconnect() { }

        public override bool ClientConnected()
        {
            return true;
        }

        public override void ClientEarlyUpdate() { }
        public override void ServerEarlyUpdate() { }
        public override void ClientLateUpdate() { }
        public override void ServerLateUpdate() { }

        public override System.Uri ServerUri()
        {
            return new System.Uri("tcp4://127.0.0.1");
        }

        public override void Shutdown() { }
    }

    public class TestNetworkManager : NeoNetworkManager
    {
        public override void OnValidate() { } // WHY: Suppress base Unity warnings during programmatic instantiation
    }

    public class TestNetworkSingleton : NetworkSingleton<TestNetworkSingleton>
    {
        [SyncVar] public int testValue;

        [Command(requiresAuthority = false)]
        public void CmdSetTestValue(int v)
        {
            testValue = v;
        }

        protected override bool DontDestroyOnLoadEnabled => false;
    }

    public class NetworkSingletonTests
    {
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;

        private GameObject _singletonObj;
        private TestNetworkSingleton _singleton;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _networkManager = NetworkTestHelper.CreateTestNetworkManager("NetworkManager", out _managerObj);

            var dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 99999);
            _networkManager.playerPrefab = dummyPlayer;

            yield return null;

            _singletonObj = new GameObject("TestSingleton");
            _singleton = _singletonObj.AddComponent<TestNetworkSingleton>();

            NetworkIdentity identity = _singletonObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(identity, 54321);

            NetworkClient.RegisterPrefab(_singletonObj);

            _networkManager.StartHost();
            while (!NetworkServer.active || !NetworkClient.isConnected)
            {
                yield return null;
            }

            NetworkServer.Spawn(_singletonObj);
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
            if (_networkManager != null)
            {
                _networkManager.StopHost();
            }

            yield return null;

            if (_managerObj != null)
            {
                Object.DestroyImmediate(_managerObj);
            }

            if (_singletonObj != null)
            {
                Object.DestroyImmediate(_singletonObj);
            }

            TestNetworkSingleton.DestroyInstance();
        }

        [UnityTest]
        public IEnumerator IsInitialized_WhenSpawned_ReturnsTrue()
        {
            Assert.IsTrue(TestNetworkSingleton.IsInitialized);
            Assert.IsNotNull(TestNetworkSingleton.I);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ServerAuthority_InHostMode_ReturnsTrue()
        {
            Assert.IsTrue(_singleton.HasServerAuthority);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CmdSetTestValue_UpdatesSyncVar()
        {
            TestNetworkSingleton.I.CmdSetTestValue(42);

            // WHY: Wait for Mirror sync
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(42, TestNetworkSingleton.I.testValue);
        }
    }
}
