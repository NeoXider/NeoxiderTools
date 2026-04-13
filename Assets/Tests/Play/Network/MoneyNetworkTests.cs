using System.Collections;
using Mirror;
using Neo.Network;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class MoneyNetworkTests
    {
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;

        private GameObject _moneyObj;
        private Money _money;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _managerObj = new GameObject("NetworkManagerMoneyTest");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();

            GameObject dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            typeof(NetworkIdentity).GetProperty("assetId").SetValue(dummyId, (uint)99997);
            _networkManager.playerPrefab = dummyPlayer;

            Transport.active = transport;
            yield return null;

            _networkManager.StartHost();
            while (!NetworkServer.active || !NetworkClient.isConnected) yield return null;
            if (!NetworkClient.ready) NetworkClient.Ready();
            yield return null;

            _moneyObj = new GameObject("MoneySingleton");
            _money = _moneyObj.AddComponent<Money>();
            
            var id = _moneyObj.AddComponent<NetworkIdentity>();
            typeof(NetworkIdentity).GetProperty("assetId").SetValue(id, (uint)10005);
            NetworkClient.RegisterPrefab(_moneyObj);
            NetworkServer.Spawn(_moneyObj);

            yield return null;
            
            // Clean state
            _money.SetMoney(0);
            _money.SetLevelMoney(0);
            yield return new WaitForSeconds(0.1f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_networkManager != null) _networkManager.StopHost();
            yield return null;
            if (_managerObj != null) Object.DestroyImmediate(_managerObj);
            if (_moneyObj != null) Object.DestroyImmediate(_moneyObj);
            
            // Delete save
            PlayerPrefs.DeleteKey("Money");
            PlayerPrefs.DeleteKey("MoneyAllMoney");
        }

        [UnityTest]
        public IEnumerator Money_Shared_AddMoneyUpdatesGlobally()
        {
            _money.isShared = true;
            
            _money.Add(150);

            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(150, _money.money);
            Assert.AreEqual(150, _money.LastChangeMoneyValue);
        }

        [UnityTest]
        public IEnumerator Money_Shared_SpendMoneyUpdatesGlobally()
        {
            _money.isShared = true;
            _money.SetMoney(200);
            yield return new WaitForSeconds(0.1f);

            bool success = _money.Spend(50);
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(success);
            Assert.AreEqual(150, _money.money);
        }

        [UnityTest]
        public IEnumerator Money_Personal_ChangesRemainLocal()
        {
            _money.isShared = false;
            
            _money.Add(100);
            yield return new WaitForSeconds(0.1f);
            
            Assert.AreEqual(100, _money.money);
        }
    }
}
