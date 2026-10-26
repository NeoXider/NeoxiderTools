using System.Collections;
using System.Reflection;
#if MIRROR
using Mirror;
#endif
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class NetworkOwnerFilterTests
    {
#if MIRROR
        private NetworkTestSession _session;
        private GameObject _managerObj;
        private GameObject _playerPrefab;
        private TestNetworkManager _networkManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            _session = new NetworkTestSession();
            _networkManager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager("NetworkManagerFilterTest", out _managerObj));

            _playerPrefab = _session.Own(new GameObject("DummyPlayer"));
            NetworkIdentity dummyId = _playerPrefab.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 89001);
            _networkManager.playerPrefab = _playerPrefab;

            yield return null;

            _networkManager.StartHost();
            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);

            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

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
        public IEnumerator ServerOnly_AllowsOnHost()
        {
            GameObject filterObj = _session.Own(new GameObject("FilterServerOnly"));
            filterObj.SetActive(false);
            NetworkOwnerFilter filter = filterObj.AddComponent<NetworkOwnerFilter>();

            FieldInfo modeField = typeof(NetworkOwnerFilter).GetField("_mode",
                BindingFlags.NonPublic | BindingFlags.Instance);
            modeField.SetValue(filter, OwnerFilterMode.ServerOnly);

            NetworkIdentity id = filterObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 89010);
            filterObj.SetActive(true);
            NetworkClient.RegisterPrefab(filterObj);
            NetworkServer.Spawn(filterObj);
            yield return NetworkTestHelper.WaitForSpawn(id);

            bool allowed = false, denied = false;
            filter.onAllowed.AddListener(() => allowed = true);
            filter.onDenied.AddListener(() => denied = true);

            filter.Filter();

            Assert.IsTrue(allowed, "Host is also server — should be allowed.");
            Assert.IsFalse(denied, "Should NOT fire denied on host for ServerOnly.");

            Object.DestroyImmediate(filterObj);
        }

#endif
        [UnityTest]
        public IEnumerator Everyone_AlwaysAllows()
        {
            GameObject filterObj = new GameObject("FilterEveryone");
            try
            {
#if MIRROR
                _session.Own(filterObj);
#endif
                filterObj.SetActive(false);
                NetworkOwnerFilter filter = filterObj.AddComponent<NetworkOwnerFilter>();
                FieldInfo modeField = typeof(NetworkOwnerFilter).GetField("_mode",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                modeField.SetValue(filter, OwnerFilterMode.Everyone);
#if MIRROR
                NetworkIdentity id = filterObj.AddComponent<NetworkIdentity>();
                NetworkTestHelper.SetAssetId(id, 89011);
#endif
                filterObj.SetActive(true);
#if MIRROR
                NetworkClient.RegisterPrefab(filterObj);
                NetworkServer.Spawn(filterObj);
                yield return NetworkTestHelper.WaitForSpawn(id);
#endif
                bool allowed = false;
                int allowedCount = 0;
                int deniedCount = 0;
                filter.onAllowed.AddListener(() => { allowed = true; allowedCount++; });
                filter.onDenied.AddListener(() => deniedCount++);
                filter.Filter();
                Assert.IsTrue(allowed, "Everyone mode should always allow.");
                Assert.That(allowedCount, Is.EqualTo(1));
                Assert.That(deniedCount, Is.Zero);
                yield break;
            }
            finally
            {
                Object.DestroyImmediate(filterObj);
            }
        }

#if MIRROR
        [UnityTest]
        public IEnumerator IsAllowed_ReturnsCorrectBool()
        {
            GameObject filterObj = _session.Own(new GameObject("FilterBool"));
            filterObj.SetActive(false);
            NetworkOwnerFilter filter = filterObj.AddComponent<NetworkOwnerFilter>();

            FieldInfo modeField = typeof(NetworkOwnerFilter).GetField("_mode",
                BindingFlags.NonPublic | BindingFlags.Instance);
            modeField.SetValue(filter, OwnerFilterMode.ServerOnly);

            NetworkIdentity id = filterObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 89012);
            filterObj.SetActive(true);
            NetworkClient.RegisterPrefab(filterObj);
            NetworkServer.Spawn(filterObj);
            yield return NetworkTestHelper.WaitForSpawn(id);

            // WHY: Host is server, so IsAllowed should be true
            Assert.IsTrue(filter.IsAllowed());

            Object.DestroyImmediate(filterObj);
        }
#endif
    }
}
