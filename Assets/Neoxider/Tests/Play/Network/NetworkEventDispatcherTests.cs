using System.Collections;
using System.Collections.Generic;
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
    public class NetworkEventDispatcherTests
    {
#if MIRROR
        private NetworkTestSession _session;
        private TestNetworkManager _networkManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            if (TestContext.CurrentContext.Test.MethodName ==
                nameof(DispatchGlobalEvent_OfflineFallback_FiresLocally))
            {
                yield break;
            }

            _session = new NetworkTestSession();
            _networkManager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager(
                "NetworkManagerEventDispatcherTest", out GameObject managerObject));
            GameObject playerPrefab = _session.Own(new GameObject("DummyPlayer"));
            NetworkTestHelper.SetAssetId(playerPrefab.AddComponent<NetworkIdentity>(), 88101);
            _networkManager.playerPrefab = playerPrefab;
            _networkManager.StartHost();
            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);
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
        public IEnumerator DispatchGlobalEvent_Host_FiresOnce()
        {
            yield return VerifyNetworkDispatch("NetworkEventDispatcher", 88110, false);
        }

        [UnityTest]
        public IEnumerator DispatchGlobalEvent_DefaultAuthorityNone_AllowsSceneObject()
        {
            yield return VerifyNetworkDispatch("NetworkEventDispatcherNone", 88111, true);
        }

        private IEnumerator VerifyNetworkDispatch(string name, uint assetId, bool setAuthorityExplicitly)
        {
            GameObject dispatcherObject = _session.Own(new GameObject(name));
            dispatcherObject.SetActive(false);
            NetworkIdentity identity = dispatcherObject.AddComponent<NetworkIdentity>();
            NetworkEventDispatcher dispatcher = dispatcherObject.AddComponent<NetworkEventDispatcher>();
            dispatcher.isNetworked = true;
            if (setAuthorityExplicitly)
            {
                dispatcher.AuthorityMode = NetworkAuthorityMode.None;
            }

            NetworkTestHelper.SetAssetId(identity, assetId);
            dispatcherObject.SetActive(true);
            NetworkClient.RegisterPrefab(dispatcherObject);
            NetworkServer.Spawn(dispatcherObject);
            yield return NetworkTestHelper.WaitForSpawn(identity);

            int firedCount = 0;
            List<int> integers = new List<int>();
            List<float> floats = new List<float>();
            List<string> strings = new List<string>();
            dispatcher.onNetworkEvent.AddListener(() => firedCount++);
            dispatcher.onNetworkIntEvent.AddListener(value => integers.Add(value));
            dispatcher.onNetworkFloatEvent.AddListener(value => floats.Add(value));
            dispatcher.onNetworkStringEvent.AddListener(value => strings.Add(value));

            using NetworkTestHelper.RpcObservation envelopes = new NetworkTestHelper.RpcObservation(identity);
            dispatcher.DispatchGlobalEvent();
            dispatcher.DispatchGlobalInt(-17);
            dispatcher.DispatchGlobalFloat(42.5f);
            dispatcher.DispatchGlobalString("Событие | payload");
            yield return NetworkTestHelper.ReliableBarrier("dispatcher four-RPC receive barrier");

            Assert.That(envelopes.Sent, Is.EqualTo(4), "Local fallback must not satisfy a network test.");
            Assert.That(envelopes.Received, Is.EqualTo(4), "All four RPC envelopes must be deserialized.");
            Assert.That(firedCount, Is.EqualTo(1), "Host dispatch fires once without a duplicate RPC echo.");
            Assert.That(integers, Is.EqualTo(new[] { -17 }));
            Assert.That(floats, Is.EqualTo(new[] { 42.5f }));
            Assert.That(strings, Is.EqualTo(new[] { "Событие | payload" }));
        }

#endif

        [UnityTest]
        public IEnumerator DispatchGlobalEvent_OfflineFallback_FiresLocally()
        {
#if MIRROR
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False,
                "The offline positive needs no live session and must never stop a borrowed one.");
#endif
            GameObject dispatcherObject = new GameObject("NetworkEventDispatcherOffline");
            try
            {
                dispatcherObject.SetActive(false);
#if MIRROR
                dispatcherObject.AddComponent<NetworkIdentity>();
#endif
                NetworkEventDispatcher dispatcher = dispatcherObject.AddComponent<NetworkEventDispatcher>();
                dispatcher.isNetworked = true;
                dispatcherObject.SetActive(true);
                int firedCount = 0;
                List<int> integers = new List<int>();
                List<float> floats = new List<float>();
                List<string> strings = new List<string>();
                dispatcher.onNetworkEvent.AddListener(() => firedCount++);
                dispatcher.onNetworkIntEvent.AddListener(value => integers.Add(value));
                dispatcher.onNetworkFloatEvent.AddListener(value => floats.Add(value));
                dispatcher.onNetworkStringEvent.AddListener(value => strings.Add(value));
                dispatcher.DispatchGlobalEvent();
                dispatcher.DispatchGlobalInt(-17);
                dispatcher.DispatchGlobalFloat(42.5f);
                dispatcher.DispatchGlobalString("Offline | payload");
                Assert.That(firedCount, Is.EqualTo(1), "An inactive session uses the synchronous local fallback.");
                Assert.That(integers, Is.EqualTo(new[] { -17 }));
                Assert.That(floats, Is.EqualTo(new[] { 42.5f }));
                Assert.That(strings, Is.EqualTo(new[] { "Offline | payload" }));
                yield break;
            }
            finally
            {
                Object.DestroyImmediate(dispatcherObject);
            }
        }
    }
}
