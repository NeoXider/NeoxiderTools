using System.Collections;
#if MIRROR
using Mirror;
#endif
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
#if MIRROR
        private NetworkTestSession _session;
        private GameObject _managerObj;
        private GameObject _playerPrefab;
        private TestNetworkManager _networkManager;

        private GameObject _testContext;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            if (TestContext.CurrentContext.Test.MethodName ==
                nameof(NetworkReactivePropertyBridge_SetFromNetwork_UpdatesAndNotifies))
            {
                yield break;
            }

            _session = new NetworkTestSession();
            _networkManager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager("NetworkManagerToolsTest", out _managerObj));

            _playerPrefab = _session.Own(new GameObject("DummyPlayer"));
            NetworkIdentity dummyId = _playerPrefab.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 99998);
            _networkManager.playerPrefab = _playerPrefab;

            yield return null;

            _testContext = _session.Own(new GameObject("TestContext"));

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
        public IEnumerator Counter_CmdSetValue_UpdatesGlobally()
        {
            GameObject counterObj = _session.Own(new GameObject("Counter"));
            counterObj.SetActive(false);
            Counter counter = counterObj.AddComponent<Counter>();
            counter.isNetworked = true;

            NetworkIdentity id = counterObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10001);
            counterObj.SetActive(true);
            NetworkClient.RegisterPrefab(counterObj);
            NetworkServer.Spawn(counterObj);

            yield return NetworkTestHelper.WaitForSpawn(id);

            int invokedValue = -1;
            counter.OnValueChangedInt.AddListener(v => invokedValue = v);

            counter.Set(42);

            yield return NetworkTestHelper.WaitUntil(() => invokedValue == 42 && counter.ValueInt == 42,
                description: "counter value and event");
            yield return NetworkTestHelper.ReliableBarrier("counter value and event receive barrier");

            Assert.AreEqual(42, counter.ValueInt);
            Assert.AreEqual(42, invokedValue);

            Object.DestroyImmediate(counterObj);
        }

        [UnityTest]
        public IEnumerator Selector_CmdSyncState_UpdatesGlobally()
        {
            GameObject selectorObj = _session.Own(new GameObject("Selector"));
            selectorObj.SetActive(false);
            Selector selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;

            NetworkIdentity id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10002);
            selectorObj.SetActive(true);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return NetworkTestHelper.WaitForSpawn(id);

            selector.Count = 5;

            int invokedValue = -1;
            selector.OnSelectionChanged.AddListener(v => invokedValue = v);

            selector.Set(3);

            yield return NetworkTestHelper.WaitUntil(() => invokedValue == 3 && selector.Value == 3,
                description: "selector value and event");
            yield return NetworkTestHelper.ReliableBarrier("selector value and event receive barrier");

            Assert.AreEqual(3, selector.Value);
            Assert.AreEqual(3, invokedValue);

            Object.DestroyImmediate(selectorObj);
        }

        [UnityTest]
        public IEnumerator Selector_NetworkedHostSet_FiresSelectionChangedOnce()
        {
            GameObject selectorObj = _session.Own(new GameObject("SelectorHostOnce"));
            selectorObj.SetActive(false);
            Selector selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;

            NetworkIdentity id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10006);
            selectorObj.SetActive(true);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return NetworkTestHelper.WaitForSpawn(id);

            selector.Count = 5;

            int fireCount = 0;
            int invokedValue = -1;
            selector.OnSelectionChanged.AddListener(v =>
            {
                fireCount++;
                invokedValue = v;
            });

            selector.Set(2);

            yield return NetworkTestHelper.WaitUntil(() => fireCount > 0 && invokedValue == 2,
                description: "selector once-only event");
            yield return NetworkTestHelper.ReliableBarrier("selector once-only event receive barrier");

            Assert.AreEqual(2, selector.Value);
            Assert.AreEqual(2, invokedValue);
            Assert.AreEqual(1, fireCount, "Host should not receive a duplicate ClientRpc echo.");

            Object.DestroyImmediate(selectorObj);
        }

        [UnityTest]
        public IEnumerator Selector_NetworkedFillMode_ActivatesPrefix()
        {
            GameObject selectorObj = _session.Own(new GameObject("SelectorFillNetworked"));
            selectorObj.SetActive(false);
            GameObject first = _session.Own(new GameObject("First"));
            GameObject second = _session.Own(new GameObject("Second"));
            GameObject third = _session.Own(new GameObject("Third"));
            first.transform.SetParent(selectorObj.transform);
            second.transform.SetParent(selectorObj.transform);
            third.transform.SetParent(selectorObj.transform);

            Selector selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;
            selector.startOnAwake = false;

            NetworkIdentity id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10007);
            selectorObj.SetActive(true);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return NetworkTestHelper.WaitForSpawn(id);

            selector.FillMode = true;
            selector.Set(1);

            yield return NetworkTestHelper.WaitUntil(() => first.activeSelf && second.activeSelf && !third.activeSelf,
                description: "selector active prefix");
            yield return NetworkTestHelper.ReliableBarrier("selector active prefix receive barrier");

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
            GameObject selectorObj = _session.Own(new GameObject("SelectorExcludeNetworked"));
            selectorObj.SetActive(false);
            Selector selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;

            NetworkIdentity id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10008);
            selectorObj.SetActive(true);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return NetworkTestHelper.WaitForSpawn(id);

            selector.Count = 3;
            selector.ExcludeIndex(1);

            yield return NetworkTestHelper.WaitUntil(() => selector.IsExcluded(1),
                description: "selector exclusion");
            yield return NetworkTestHelper.ReliableBarrier("selector exclusion receive barrier");

            Assert.IsTrue(selector.IsExcluded(1));
            Assert.AreEqual(1, selector.ExcludedCount);

            selector.IncludeIndex(1);

            yield return NetworkTestHelper.WaitUntil(() => !selector.IsExcluded(1),
                description: "selector inclusion");
            yield return NetworkTestHelper.ReliableBarrier("selector inclusion receive barrier");

            Assert.IsFalse(selector.IsExcluded(1));
            Assert.AreEqual(0, selector.ExcludedCount);

            Object.DestroyImmediate(selectorObj);
        }

        [UnityTest]
        public IEnumerator NeoCondition_CmdCheckResult_FiresGlobalEvents()
        {
            GameObject condObj = _session.Own(new GameObject("Condition"));
            condObj.SetActive(false);
            NeoCondition condition = condObj.AddComponent<NeoCondition>();
            condition.isNetworked = true;
            typeof(NeoCondition)
                .GetField("_onlyOnChange",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(condition, false);

            NetworkIdentity id = condObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10003);
            condObj.SetActive(true);
            NetworkClient.RegisterPrefab(condObj);
            NetworkServer.Spawn(condObj);

            yield return NetworkTestHelper.WaitForSpawn(id);

            bool onTrueFired = false;
            condition.OnTrue.AddListener(() => onTrueFired = true);

            condition.Check(); // WHY: base evaluation with 0 entries is true

            yield return NetworkTestHelper.WaitUntil(() => onTrueFired,
                description: "condition true event");
            yield return NetworkTestHelper.ReliableBarrier("condition true event receive barrier");

            Assert.IsTrue(onTrueFired);

            Object.DestroyImmediate(condObj);
        }

        [UnityTest]
        public IEnumerator RandomRange_CmdGenerate_UpdatesGlobally()
        {
            GameObject randObj = _session.Own(new GameObject("RandomRange"));
            randObj.SetActive(false);
            RandomRange randomRange = randObj.AddComponent<RandomRange>();
            randomRange.SetMin(1);
            randomRange.SetMax(100);
            randomRange.isNetworked = true;

            NetworkIdentity id = randObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10004);
            randObj.SetActive(true);
            NetworkClient.RegisterPrefab(randObj);
            NetworkServer.Spawn(randObj);

            yield return NetworkTestHelper.WaitForSpawn(id);

            int invokedValue = -1;
            randomRange.OnGeneratedInt.AddListener(v => invokedValue = v);

            randomRange.Generate();

            yield return NetworkTestHelper.WaitUntil(() => invokedValue >= 1,
                description: "random range payload");
            yield return NetworkTestHelper.ReliableBarrier("random range payload receive barrier");

            Assert.IsTrue(randomRange.ValueInt >= 1 && randomRange.ValueInt <= 100);
            Assert.AreEqual(randomRange.ValueInt, invokedValue);

            Object.DestroyImmediate(randObj);
        }

#endif

        [Test]
        public void NetworkReactivePropertyBridge_SetFromNetwork_UpdatesAndNotifies()
        {
            ReactivePropertyFloat property = new ReactivePropertyFloat(1f);
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

#if MIRROR
        [UnityTest]
        public IEnumerator Selector_CmdSetRandom_UpdatesGlobally()
        {
            GameObject selectorObj = _session.Own(new GameObject("Selector"));
            selectorObj.SetActive(false);
            Selector selector = selectorObj.AddComponent<Selector>();
            selector.isNetworked = true;

            NetworkIdentity id = selectorObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 10005);
            selectorObj.SetActive(true);
            NetworkClient.RegisterPrefab(selectorObj);
            NetworkServer.Spawn(selectorObj);

            yield return NetworkTestHelper.WaitForSpawn(id);

            selector.Count = 5;
            typeof(Selector)
                .GetField("_useRandomSelection",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(selector, true);

            int invokedValue = -1;
            selector.OnSelectionChanged.AddListener(v => invokedValue = v);

            selector.SetRandom(true);

            yield return NetworkTestHelper.WaitUntil(() => invokedValue >= 0,
                description: "selector random event");
            yield return NetworkTestHelper.ReliableBarrier("selector random event receive barrier");

            Assert.IsTrue(selector.Value >= 0 && selector.Value < 5);
            Assert.AreEqual(selector.Value, invokedValue);

            Object.DestroyImmediate(selectorObj);
        }
#endif
    }
}
