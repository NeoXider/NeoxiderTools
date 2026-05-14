using System.Collections;
using Mirror;
using Neo.Condition;
using Neo.Network;
using Neo.Reactive;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class NetworkToolsTests
    {
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;

        private GameObject _testContext;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _managerObj = new GameObject("NetworkManagerToolsTest");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();

            GameObject dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 99998);
            _networkManager.playerPrefab = dummyPlayer;

            Transport.active = transport;
            yield return null;

            _testContext = new GameObject("TestContext");

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
            if (_testContext != null) Object.DestroyImmediate(_testContext);
        }

        [UnityTest]
        public IEnumerator Counter_CmdSetValue_UpdatesGlobally()
        {
            var counterObj = new GameObject("Counter");
            var counter = counterObj.AddComponent<Counter>();
            counter.isNetworked = true;
            
            var id = counterObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10001);
            NetworkClient.RegisterPrefab(counterObj);
            NetworkServer.Spawn(counterObj);

            yield return null;

            int invokedValue = -1;
            counter.OnValueChangedInt.AddListener(v => invokedValue = v);

            counter.Set(42);

            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(42, counter.ValueInt);
            Assert.AreEqual(42, invokedValue);

            Object.DestroyImmediate(counterObj);
        }

        [UnityTest]
        public IEnumerator Selector_CmdSyncState_UpdatesGlobally()
        {
            var selectorObj = new GameObject("Selector");
            var selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;

            var id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10002);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return null;
            
            selector.Count = 5;

            int invokedValue = -1;
            selector.OnSelectionChanged.AddListener(v => invokedValue = v);

            selector.Set(3);

            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(3, selector.Value);
            Assert.AreEqual(3, invokedValue);

            Object.DestroyImmediate(selectorObj);
        }

        [UnityTest]
        public IEnumerator Selector_NetworkedHostSet_FiresSelectionChangedOnce()
        {
            var selectorObj = new GameObject("SelectorHostOnce");
            var selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;

            var id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10006);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return null;

            selector.Count = 5;

            int fireCount = 0;
            int invokedValue = -1;
            selector.OnSelectionChanged.AddListener(v =>
            {
                fireCount++;
                invokedValue = v;
            });

            selector.Set(2);

            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(2, selector.Value);
            Assert.AreEqual(2, invokedValue);
            Assert.AreEqual(1, fireCount, "Host should not receive a duplicate ClientRpc echo.");

            Object.DestroyImmediate(selectorObj);
        }

        [UnityTest]
        public IEnumerator Selector_NetworkedFillMode_ActivatesPrefix()
        {
            var selectorObj = new GameObject("SelectorFillNetworked");
            var first = new GameObject("First");
            var second = new GameObject("Second");
            var third = new GameObject("Third");
            first.transform.SetParent(selectorObj.transform);
            second.transform.SetParent(selectorObj.transform);
            third.transform.SetParent(selectorObj.transform);

            var selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;
            selector.startOnAwake = false;

            var id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10007);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return null;

            selector.FillMode = true;
            selector.Set(1);

            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(selector.FillMode);
            Assert.IsTrue(first.activeSelf);
            Assert.IsTrue(second.activeSelf);
            Assert.IsFalse(third.activeSelf);
            Assert.AreEqual(2, selector.CountActive);

            Object.DestroyImmediate(selectorObj);
        }

        [UnityTest]
        public IEnumerator Selector_NetworkedExcludeInclude_UpdatesPool()
        {
            var selectorObj = new GameObject("SelectorExcludeNetworked");
            var selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;

            var id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10008);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return null;

            selector.Count = 3;
            selector.ExcludeIndex(1);

            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(selector.IsExcluded(1));
            Assert.AreEqual(1, selector.ExcludedCount);

            selector.IncludeIndex(1);

            yield return new WaitForSeconds(0.1f);

            Assert.IsFalse(selector.IsExcluded(1));
            Assert.AreEqual(0, selector.ExcludedCount);

            Object.DestroyImmediate(selectorObj);
        }

        [UnityTest]
        public IEnumerator NeoCondition_CmdCheckResult_FiresGlobalEvents()
        {
            var condObj = new GameObject("Condition");
            var condition = condObj.AddComponent<NeoCondition>();
            condition.isNetworked = true;
            typeof(NeoCondition).GetField("_onlyOnChange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(condition, false);

            var id = condObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10003);
            NetworkClient.RegisterPrefab(condObj);
            NetworkServer.Spawn(condObj);

            yield return null;

            bool onTrueFired = false;
            condition.OnTrue.AddListener(() => onTrueFired = true);

            condition.Check(); // Base evaluation with 0 entries is true

            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(onTrueFired);

            Object.DestroyImmediate(condObj);
        }

        [UnityTest]
        public IEnumerator RandomRange_CmdGenerate_UpdatesGlobally()
        {
            var randObj = new GameObject("RandomRange");
            var randomRange = randObj.AddComponent<RandomRange>();
            randomRange.isNetworked = true;
            randomRange.SetMin(1);
            randomRange.SetMax(100);

            var id = randObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10004);
            NetworkClient.RegisterPrefab(randObj);
            NetworkServer.Spawn(randObj);

            yield return null;

            int invokedValue = -1;
            randomRange.OnGeneratedInt.AddListener(v => invokedValue = v);

            randomRange.Generate();

            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(randomRange.ValueInt >= 1 && randomRange.ValueInt <= 100);
            Assert.AreEqual(randomRange.ValueInt, invokedValue);

            Object.DestroyImmediate(randObj);
        }

        [Test]
        public void NetworkReactivePropertyBridge_SetFromNetwork_UpdatesAndNotifies()
        {
            var property = new ReactivePropertyFloat(1f);
            float observedValue = -1f;
            int notifyCount = 0;
            property.AddListener(value =>
            {
                observedValue = value;
                notifyCount++;
            });

            NetworkReactivePropertyBridge.SetFromNetwork(property, 7.5f);

            Assert.AreEqual(7.5f, property.CurrentValue, 0.001f);
            Assert.AreEqual(7.5f, observedValue, 0.001f);
            Assert.AreEqual(1, notifyCount);
        }

        [UnityTest]
        public IEnumerator Selector_CmdSetRandom_UpdatesGlobally()
        {
            var selectorObj = new GameObject("Selector");
            var selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;

            var id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10005);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return null;
            
            selector.Count = 5;
            typeof(Selector).GetField("_useRandomSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(selector, true);

            int invokedValue = -1;
            selector.OnSelectionChanged.AddListener(v => invokedValue = v);

            selector.SetRandom(true);

            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(selector.Value >= 0 && selector.Value < 5);
            Assert.AreEqual(selector.Value, invokedValue);

            Object.DestroyImmediate(selectorObj);
        }
    }
}
