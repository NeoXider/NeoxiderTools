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
        public override bool Available() => true;
        public override int GetMaxPacketSize(int channelId = 0) => 1200;
        public override bool ServerActive() => true;
        public override void ServerStart() {}
        public override void ServerSend(int connectionId, System.ArraySegment<byte> segment, int channelId = 0) {}
        public override void ServerDisconnect(int connectionId) {}
        public override string ServerGetClientAddress(int connectionId) => "127.0.0.1";
        public override void ServerStop() {}
        public override void ClientConnect(string address) {}
        public override void ClientConnect(System.Uri uri) {}
        public override void ClientSend(System.ArraySegment<byte> segment, int channelId = 0) {}
        public override void ClientDisconnect() {}
        public override bool ClientConnected() => true;
        public override void ClientEarlyUpdate() {}
        public override void ServerEarlyUpdate() {}
        public override void ClientLateUpdate() {}
        public override void ServerLateUpdate() {}
        public override System.Uri ServerUri() => new System.Uri("tcp4://127.0.0.1");
        public override void Shutdown() {}
    }

    public class TestNetworkManager : NeoNetworkManager
    {
        public override void OnValidate() { } // Suppress base Unity warnings during programmatic instantiation
    }

    public class TestNetworkSingleton : NetworkSingleton<TestNetworkSingleton>
    {
        [SyncVar]
        public int testValue;

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
            // Setup NetworkManager
            _managerObj = new GameObject("NetworkManager");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();

            GameObject dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 99999);
            _networkManager.playerPrefab = dummyPlayer;

            Transport.active = transport;
            
            // Wait a frame
            yield return null;

            // Setup Singleton Prefab
            _singletonObj = new GameObject("TestSingleton");
            _singleton = _singletonObj.AddComponent<TestNetworkSingleton>();
            
            NetworkIdentity identity = _singletonObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(identity, 54321);

            // Register prefab
            NetworkClient.RegisterPrefab(_singletonObj);

            // Start Host
            _networkManager.StartHost();
            while (!NetworkServer.active || !NetworkClient.isConnected) yield return null;
            
            // Spawn Singleton on server
            NetworkServer.Spawn(_singletonObj);
            if (!NetworkClient.ready) NetworkClient.Ready();
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

            if (_managerObj != null) Object.DestroyImmediate(_managerObj);
            if (_singletonObj != null) Object.DestroyImmediate(_singletonObj);

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
            // Act
            TestNetworkSingleton.I.CmdSetTestValue(42);
            
            // Wait for Mirror sync
            yield return new WaitForSeconds(0.1f);
            
            // Assert
            Assert.AreEqual(42, TestNetworkSingleton.I.testValue);
        }
    }
}
