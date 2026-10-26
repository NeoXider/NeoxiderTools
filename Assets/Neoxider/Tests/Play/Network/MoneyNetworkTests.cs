using System.Collections;
#if MIRROR
using Mirror;
#endif
using Neo.Network;
using Neo.Shop;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class MoneyNetworkTests
    {
#if MIRROR
        private NetworkTestSession _session;
        private GameObject _managerObj;
        private GameObject _playerPrefab;
        private TestNetworkManager _networkManager;
#endif

        private GameObject _moneyObj;
        private Money _money;
        private ISaveProvider _previousProvider;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _moneyObj = null;
            _previousProvider = null;
#if MIRROR
            _session = null;
            _session = new NetworkTestSession();
#endif
            Assert.That(Object.FindObjectsByType<Money>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length, Is.Zero, "A borrowed Money singleton must remain untouched.");
            _previousProvider = SaveProvider.CurrentProvider;
            SaveProvider.SetProvider(new ShopPurchasePlayModeTests.MemorySaveProvider());
#if MIRROR
            _networkManager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager("NetworkManagerMoneyTest", out _managerObj));

            _playerPrefab = _session.Own(new GameObject("DummyPlayer"));
            NetworkIdentity dummyId = _playerPrefab.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 99997);
            _networkManager.playerPrefab = _playerPrefab;

            yield return null;

            _networkManager.StartHost();
            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);

            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            yield return null;

#endif
            _moneyObj = new GameObject("MoneySingleton");
#if MIRROR
            _session.Own(_moneyObj);
#endif
            _moneyObj.SetActive(false);
#if MIRROR
            NetworkIdentity id = _moneyObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10005);
#endif
            _money = _moneyObj.AddComponent<Money>();
#if MIRROR
            _money.isNetworked = true;
#else
            _money.isNetworked = false;
#endif
            _moneyObj.SetActive(true);
#if MIRROR
            NetworkClient.RegisterPrefab(_moneyObj);
            NetworkServer.Spawn(_moneyObj);
            yield return NetworkTestHelper.WaitForSpawn(id);
#else
            yield return null; // Let the real component startup finish before establishing the baseline.
#endif

            // Clean state — local only to avoid RPC before full spawn
            _money.isNetworked = false;
            _money.SetMoney(0);
            _money.SetLevelMoney(0);
#if MIRROR
            _money.isNetworked = true;
            yield return NetworkTestHelper.WaitUntil(() => _money.money == 0,
                description: "money clean baseline");
            yield return NetworkTestHelper.ReliableBarrier("money clean baseline receive barrier");
#else
            Assert.That(_money.money, Is.Zero);
#endif
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
#if MIRROR
                if (_session != null)
                {
                    yield return _session.Cleanup();
                    _session = null;
                }
#endif
                yield break;
            }
            finally
            {
                if (_moneyObj != null)
                {
                    Object.DestroyImmediate(_moneyObj);
                }

                if (_previousProvider != null)
                {
                    SaveProvider.SetProvider(_previousProvider);
                    _previousProvider = null;
                }
            }
        }

#if MIRROR
        [UnityTest]
        public IEnumerator Money_Shared_AddMoneyUpdatesGlobally()
        {
            _money.isNetworked = true;

            _money.Add(150);

            yield return NetworkTestHelper.WaitUntil(() => _money.money == 150 && _money.LastChangeMoneyValue == 150,
                description: "money add receipt");
            yield return NetworkTestHelper.ReliableBarrier("money add receipt receive barrier");

            Assert.AreEqual(150, _money.money);
            Assert.AreEqual(150, _money.LastChangeMoneyValue);
        }

        [UnityTest]
        public IEnumerator Money_Shared_SpendMoneyUpdatesGlobally()
        {
            _money.isNetworked = true;
            _money.SetMoney(200);
            yield return NetworkTestHelper.WaitUntil(() => _money.money == 200,
                description: "money initial balance");
            yield return NetworkTestHelper.ReliableBarrier("money initial balance receive barrier");

            bool success = _money.Spend(50);
            yield return NetworkTestHelper.WaitUntil(() => _money.money == 150,
                description: "money spend receipt");
            yield return NetworkTestHelper.ReliableBarrier("money spend receipt receive barrier");

            Assert.IsTrue(success);
            Assert.AreEqual(150, _money.money);
        }

#endif
        [UnityTest]
        public IEnumerator Money_Personal_ChangesRemainLocal()
        {
            _money.isNetworked = false;

            _money.Add(100);
#if MIRROR
            yield return NetworkTestHelper.WaitUntil(() => _money.money == 100,
                description: "personal money receipt");
            yield return NetworkTestHelper.ReliableBarrier("personal money receipt receive barrier");
#else
            yield return null;
#endif

            Assert.AreEqual(100, _money.money);
        }
    }
}
