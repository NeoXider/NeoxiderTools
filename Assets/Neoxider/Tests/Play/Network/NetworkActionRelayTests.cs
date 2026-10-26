#if MIRROR
using System.Collections;
using System.Reflection;
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class NetworkActionRelayTests
    {
        private NetworkTestSession _session;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            _session = new NetworkTestSession();
            TestNetworkManager manager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager(
                "NetworkManagerRelayTest", out GameObject managerObject));
            GameObject playerPrefab = _session.Own(new GameObject("DummyPlayer"));
            NetworkTestHelper.SetAssetId(playerPrefab.AddComponent<NetworkIdentity>(), 88001);
            manager.playerPrefab = playerPrefab;
            manager.StartHost();
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

        private NetworkActionRelay CreateRelay(uint assetId, params NetworkActionChannel[] channels)
        {
            GameObject relayObject = _session.Own(new GameObject("Relay"));
            relayObject.SetActive(false);
            NetworkIdentity identity = relayObject.AddComponent<NetworkIdentity>();
            NetworkActionRelay relay = relayObject.AddComponent<NetworkActionRelay>();
            relay.isNetworked = true;
            FieldInfo channelsField = typeof(NetworkActionRelay).GetField("_channels",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(channelsField, Is.Not.Null);
            channelsField.SetValue(relay, channels);
            NetworkTestHelper.SetAssetId(identity, assetId);
            relayObject.SetActive(true);
            NetworkClient.RegisterPrefab(relayObject);
            NetworkServer.Spawn(relayObject);
            return relay;
        }

        private static NetworkActionChannel Channel(string name, NetworkActionScope scope = NetworkActionScope.AllClients)
        {
            return new NetworkActionChannel
            {
                channelName = name, scope = scope, onTriggered = new UnityEvent(),
                onTriggeredFloat = new UnityEvent<float>(), onTriggeredString = new UnityEvent<string>()
            };
        }

        [UnityTest]
        public IEnumerator Trigger_Void_FiresOnAllClients()
        {
            NetworkActionChannel channel = Channel("test");
            NetworkActionRelay relay = CreateRelay(88010, channel);
            yield return NetworkTestHelper.WaitForSpawn(relay.GetComponent<NetworkIdentity>());
            int count = 0;
            channel.onTriggered.AddListener(() => count++);
            relay.Trigger(0);
            yield return NetworkTestHelper.ReliableBarrier("void relay receive barrier");
            Assert.That(count, Is.EqualTo(1), "Host loopback must not duplicate the event.");
        }

        [UnityTest]
        public IEnumerator TriggerFloat_FiresWithPayload()
        {
            NetworkActionChannel channel = Channel("hp");
            NetworkActionRelay relay = CreateRelay(88011, channel);
            yield return NetworkTestHelper.WaitForSpawn(relay.GetComponent<NetworkIdentity>());
            float received = -1f;
            int count = 0;
            channel.onTriggeredFloat.AddListener(value => { received = value; count++; });
            relay.TriggerFloat(42.5f);
            yield return NetworkTestHelper.ReliableBarrier("float relay receive barrier");
            Assert.That(received, Is.EqualTo(42.5f));
            Assert.That(count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TriggerString_FiresWithPayload()
        {
            NetworkActionChannel channel = Channel("chat");
            NetworkActionRelay relay = CreateRelay(88012, channel);
            yield return NetworkTestHelper.WaitForSpawn(relay.GetComponent<NetworkIdentity>());
            string received = null;
            int count = 0;
            channel.onTriggeredString.AddListener(value => { received = value; count++; });
            relay.TriggerString("hello world");
            yield return NetworkTestHelper.ReliableBarrier("string relay receive barrier");
            Assert.That(received, Is.EqualTo("hello world"));
            Assert.That(count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ServerOnly_Scope_FiresOnServer()
        {
            NetworkActionChannel channel = Channel("serverCmd", NetworkActionScope.ServerOnly);
            NetworkActionRelay relay = CreateRelay(88013, channel);
            NetworkIdentity identity = relay.GetComponent<NetworkIdentity>();
            yield return NetworkTestHelper.WaitForSpawn(identity);
            int count = 0;
            channel.onTriggered.AddListener(() => count++);
            using NetworkTestHelper.RpcObservation envelopes = new NetworkTestHelper.RpcObservation(identity);
            relay.Trigger(0);
            yield return NetworkTestHelper.ReliableBarrier("server-only relay barrier");
            Assert.That(count, Is.EqualTo(1));
            Assert.That(envelopes.Sent, Is.Zero, "ServerOnly must not send an RPC back to the host.");
        }

        [UnityTest]
        public IEnumerator OthersOnly_Scope_DoesNotEchoToHostSender()
        {
            NetworkActionChannel channel = Channel("others", NetworkActionScope.OthersOnly);
            NetworkActionRelay relay = CreateRelay(88016, channel);
            yield return NetworkTestHelper.WaitForSpawn(relay.GetComponent<NetworkIdentity>());
            int count = 0;
            channel.onTriggered.AddListener(() => count++);
            relay.Trigger(0);
            yield return NetworkTestHelper.ReliableBarrier("others-only rejection barrier");
            Assert.That(count, Is.Zero);
            channel.scope = NetworkActionScope.AllClients;
            relay.Trigger(0);
            yield return NetworkTestHelper.ReliableBarrier("same-channel positive relay control");
            Assert.That(count, Is.EqualTo(1), "An uninitialized channel cannot satisfy the negative case.");
        }

        [UnityTest]
        public IEnumerator TriggerByName_FindsCorrectChannel()
        {
            NetworkActionChannel alpha = Channel("alpha");
            NetworkActionChannel beta = Channel("beta");
            NetworkActionRelay relay = CreateRelay(88014, alpha, beta);
            yield return NetworkTestHelper.WaitForSpawn(relay.GetComponent<NetworkIdentity>());
            int alphaCount = 0;
            int betaCount = 0;
            alpha.onTriggered.AddListener(() => alphaCount++);
            beta.onTriggered.AddListener(() => betaCount++);
            relay.TriggerByName("beta");
            yield return NetworkTestHelper.ReliableBarrier("named relay receive barrier");
            Assert.That(alphaCount, Is.Zero);
            Assert.That(betaCount, Is.EqualTo(1));
            relay.TriggerByName("alpha");
            yield return NetworkTestHelper.ReliableBarrier("alpha positive control");
            Assert.That(alphaCount, Is.EqualTo(1));
            Assert.That(betaCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Trigger_InvalidIndex_DoesNotThrow()
        {
            NetworkActionChannel channel = Channel("valid");
            NetworkActionRelay relay = CreateRelay(88015, channel);
            yield return NetworkTestHelper.WaitForSpawn(relay.GetComponent<NetworkIdentity>());
            int count = 0;
            float receivedFloat = -1f;
            string receivedString = "unchanged";
            channel.onTriggered.AddListener(() => count++);
            channel.onTriggeredFloat.AddListener(value => receivedFloat = value);
            channel.onTriggeredString.AddListener(value => receivedString = value);
            Assert.DoesNotThrow(() => relay.Trigger(999));
            relay.Trigger(-1);
            relay.TriggerFloatAt(999, 99f);
            relay.TriggerStringAt(-1, "invalid");
            relay.TriggerByName("missing");
            yield return NetworkTestHelper.ReliableBarrier("invalid channel rejection barrier");
            Assert.That(count, Is.Zero);
            Assert.That(receivedFloat, Is.EqualTo(-1f));
            Assert.That(receivedString, Is.EqualTo("unchanged"));

            relay.Trigger(0);
            relay.TriggerFloatAt(0, 12.5f);
            relay.TriggerStringAt(0, "valid");
            yield return NetworkTestHelper.ReliableBarrier("valid channel positive controls");
            Assert.That(count, Is.EqualTo(1));
            Assert.That(receivedFloat, Is.EqualTo(12.5f));
            Assert.That(receivedString, Is.EqualTo("valid"));
        }
    }
}
#endif
