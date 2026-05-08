using System.Collections;
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
        private GameObject _managerObj;
        private TestNetworkManager _networkManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _managerObj = new GameObject("NetworkManagerRelayTest");
            Transport transport = _managerObj.AddComponent<DummyTransport>();
            _networkManager = _managerObj.AddComponent<TestNetworkManager>();

            GameObject dummyPlayer = new GameObject("DummyPlayer");
            NetworkIdentity dummyId = dummyPlayer.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(dummyId, 88001);
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

        // ────────────── Void Trigger ──────────────

        [UnityTest]
        public IEnumerator Trigger_Void_FiresOnAllClients()
        {
            var relayObj = new GameObject("Relay");
            var relay = relayObj.AddComponent<NetworkActionRelay>();

            // Configure channel via reflection (serialized field)
            var channelsField = typeof(NetworkActionRelay).GetField("_channels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = new NetworkActionChannel
            {
                channelName = "test",
                scope = NetworkActionScope.AllClients,
                onTriggered = new UnityEvent()
            };
            channelsField.SetValue(relay, new[] { channel });

            var id = relayObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 88010);
            NetworkClient.RegisterPrefab(relayObj);
            NetworkServer.Spawn(relayObj);
            yield return null;

            bool fired = false;
            channel.onTriggered.AddListener(() => fired = true);

            relay.Trigger(0);

            yield return new WaitForSeconds(0.15f);

            Assert.IsTrue(fired, "Void trigger should fire onTriggered event.");
            Object.DestroyImmediate(relayObj);
        }

        // ────────────── Float Trigger ──────────────

        [UnityTest]
        public IEnumerator TriggerFloat_FiresWithPayload()
        {
            var relayObj = new GameObject("RelayFloat");
            var relay = relayObj.AddComponent<NetworkActionRelay>();

            var channelsField = typeof(NetworkActionRelay).GetField("_channels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = new NetworkActionChannel
            {
                channelName = "hp",
                scope = NetworkActionScope.AllClients,
                onTriggeredFloat = new UnityEvent<float>()
            };
            channelsField.SetValue(relay, new[] { channel });

            var id = relayObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 88011);
            NetworkClient.RegisterPrefab(relayObj);
            NetworkServer.Spawn(relayObj);
            yield return null;

            float received = -1f;
            channel.onTriggeredFloat.AddListener(v => received = v);

            relay.TriggerFloat(42.5f);

            yield return new WaitForSeconds(0.15f);

            Assert.AreEqual(42.5f, received, 0.01f, "Float trigger should pass payload.");
            Object.DestroyImmediate(relayObj);
        }

        // ────────────── String Trigger ──────────────

        [UnityTest]
        public IEnumerator TriggerString_FiresWithPayload()
        {
            var relayObj = new GameObject("RelayString");
            var relay = relayObj.AddComponent<NetworkActionRelay>();

            var channelsField = typeof(NetworkActionRelay).GetField("_channels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = new NetworkActionChannel
            {
                channelName = "chat",
                scope = NetworkActionScope.AllClients,
                onTriggeredString = new UnityEvent<string>()
            };
            channelsField.SetValue(relay, new[] { channel });

            var id = relayObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 88012);
            NetworkClient.RegisterPrefab(relayObj);
            NetworkServer.Spawn(relayObj);
            yield return null;

            string received = null;
            channel.onTriggeredString.AddListener(v => received = v);

            relay.TriggerString("hello world");

            yield return new WaitForSeconds(0.15f);

            Assert.AreEqual("hello world", received, "String trigger should pass payload.");
            Object.DestroyImmediate(relayObj);
        }

        // ────────────── ServerOnly Scope ──────────────

        [UnityTest]
        public IEnumerator ServerOnly_Scope_FiresOnServer()
        {
            var relayObj = new GameObject("RelayServerOnly");
            var relay = relayObj.AddComponent<NetworkActionRelay>();

            var channelsField = typeof(NetworkActionRelay).GetField("_channels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channel = new NetworkActionChannel
            {
                channelName = "serverCmd",
                scope = NetworkActionScope.ServerOnly,
                onTriggered = new UnityEvent()
            };
            channelsField.SetValue(relay, new[] { channel });

            var id = relayObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 88013);
            NetworkClient.RegisterPrefab(relayObj);
            NetworkServer.Spawn(relayObj);
            yield return null;

            bool fired = false;
            channel.onTriggered.AddListener(() => fired = true);

            relay.Trigger(0);

            yield return new WaitForSeconds(0.15f);

            // In Host mode, server is local — event should fire
            Assert.IsTrue(fired, "ServerOnly scope should still fire on Host (which is also the server).");
            Object.DestroyImmediate(relayObj);
        }

        // ────────────── TriggerByName ──────────────

        [UnityTest]
        public IEnumerator TriggerByName_FindsCorrectChannel()
        {
            var relayObj = new GameObject("RelayByName");
            var relay = relayObj.AddComponent<NetworkActionRelay>();

            var channelsField = typeof(NetworkActionRelay).GetField("_channels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ch0 = new NetworkActionChannel { channelName = "alpha", scope = NetworkActionScope.AllClients, onTriggered = new UnityEvent() };
            var ch1 = new NetworkActionChannel { channelName = "beta", scope = NetworkActionScope.AllClients, onTriggered = new UnityEvent() };
            channelsField.SetValue(relay, new[] { ch0, ch1 });

            var id = relayObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 88014);
            NetworkClient.RegisterPrefab(relayObj);
            NetworkServer.Spawn(relayObj);
            yield return null;

            bool alphaFired = false, betaFired = false;
            ch0.onTriggered.AddListener(() => alphaFired = true);
            ch1.onTriggered.AddListener(() => betaFired = true);

            relay.TriggerByName("beta");

            yield return new WaitForSeconds(0.15f);

            Assert.IsFalse(alphaFired, "Alpha channel should NOT fire.");
            Assert.IsTrue(betaFired, "Beta channel should fire.");
            Object.DestroyImmediate(relayObj);
        }

        // ────────────── Invalid Index ──────────────

        [UnityTest]
        public IEnumerator Trigger_InvalidIndex_DoesNotThrow()
        {
            var relayObj = new GameObject("RelayInvalid");
            var relay = relayObj.AddComponent<NetworkActionRelay>();

            var id = relayObj.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(id, 88015);
            NetworkClient.RegisterPrefab(relayObj);
            NetworkServer.Spawn(relayObj);
            yield return null;

            Assert.DoesNotThrow(() => relay.Trigger(999));

            Object.DestroyImmediate(relayObj);
        }
    }
}
