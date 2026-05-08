using System.Collections;
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class NetworkOwnerFilterTests
    {
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _managerObj = new GameObject("NetworkManagerFilterTest");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();

            GameObject dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 89001);
            _networkManager.playerPrefab = dummyPlayer;

            Transport.active = transport;
            yield return null;

            _networkManager.StartHost();
            while (!NetworkServer.active || !NetworkClient.isConnected) yield return null;
            if (!NetworkClient.ready) NetworkClient.Ready();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_networkManager != null) _networkManager.StopHost();
            yield return null;
            if (_managerObj != null) Object.DestroyImmediate(_managerObj);
        }

        [UnityTest]
        public IEnumerator ServerOnly_AllowsOnHost()
        {
            var filterObj = new GameObject("FilterServerOnly");
            var filter = filterObj.AddComponent<NetworkOwnerFilter>();

            var modeField = typeof(NetworkOwnerFilter).GetField("_mode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            modeField.SetValue(filter, OwnerFilterMode.ServerOnly);

            var id = filterObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 89010);
            NetworkClient.RegisterPrefab(filterObj);
            NetworkServer.Spawn(filterObj);
            yield return null;

            bool allowed = false, denied = false;
            filter.onAllowed.AddListener(() => allowed = true);
            filter.onDenied.AddListener(() => denied = true);

            filter.Filter();

            Assert.IsTrue(allowed, "Host is also server — should be allowed.");
            Assert.IsFalse(denied, "Should NOT fire denied on host for ServerOnly.");

            Object.DestroyImmediate(filterObj);
        }

        [UnityTest]
        public IEnumerator Everyone_AlwaysAllows()
        {
            var filterObj = new GameObject("FilterEveryone");
            var filter = filterObj.AddComponent<NetworkOwnerFilter>();

            var modeField = typeof(NetworkOwnerFilter).GetField("_mode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            modeField.SetValue(filter, OwnerFilterMode.Everyone);

            var id = filterObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 89011);
            NetworkClient.RegisterPrefab(filterObj);
            NetworkServer.Spawn(filterObj);
            yield return null;

            bool allowed = false;
            filter.onAllowed.AddListener(() => allowed = true);

            filter.Filter();

            Assert.IsTrue(allowed, "Everyone mode should always allow.");

            Object.DestroyImmediate(filterObj);
        }

        [UnityTest]
        public IEnumerator IsAllowed_ReturnsCorrectBool()
        {
            var filterObj = new GameObject("FilterBool");
            var filter = filterObj.AddComponent<NetworkOwnerFilter>();

            var modeField = typeof(NetworkOwnerFilter).GetField("_mode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            modeField.SetValue(filter, OwnerFilterMode.ServerOnly);

            var id = filterObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 89012);
            NetworkClient.RegisterPrefab(filterObj);
            NetworkServer.Spawn(filterObj);
            yield return null;

            // Host is server, so IsAllowed should be true
            Assert.IsTrue(filter.IsAllowed());

            Object.DestroyImmediate(filterObj);
        }
    }
}
