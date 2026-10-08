#if MIRROR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Mirror;
using Neo.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class NeoNetworkManagerHardeningTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly List<GameObject> _objects = new();

        // Avoid singleton/transport configuration in these transportless EditMode tests.
        private sealed class ManagerProbe : NeoNetworkManager
        {
            public override void Awake() { }
        }

        private sealed class UnsupportedTransport : Transport
        {
            public int clientSendQueueLimit = 100;
            public override bool Available() => true;
            public override bool ClientConnected() => false;
            public override void ClientConnect(string address) { }
            public override void ClientSend(ArraySegment<byte> segment, int channelId) { }
            public override void ClientDisconnect() { }
            public override Uri ServerUri() => new Uri("unsupported://localhost");
            public override bool ServerActive() => false;
            public override void ServerStart() { }
            public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId) { }
            public override void ServerDisconnect(int connectionId) { }
            public override string ServerGetClientAddress(int connectionId) => "test";
            public override void ServerStop() { }
            public override int GetMaxPacketSize(int channelId = 0) => 1024;
            public override void Shutdown() { }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        [Test]
        public void ThrowingUnityReadyEvent_DoesNotEscapeMirrorCallback()
        {
            ManagerProbe manager = NewManager();
            NetworkConnectionToClient peer = new NetworkConnectionToClient(72);
            int called = 0;
            manager.ServerClientReady += _ => called++;
            manager.OnServerClientReadyEvent.AddListener(_ => throw new InvalidOperationException("ready listener failed"));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: ready listener failed"));
            Assert.DoesNotThrow(() => manager.OnServerReady(peer));
            Assert.That(peer.isReady, Is.True);
            Assert.That(called, Is.EqualTo(1));
        }

        [Test]
        public void ThrowingUnityDisconnectEvent_DoesNotEscape_AndPendingTrackingIsRemoved()
        {
            ManagerProbe manager = NewManager();
            NetworkConnectionToClient peer = new NetworkConnectionToClient(73);
            List<NetworkConnectionToClient> pending =
                (List<NetworkConnectionToClient>)typeof(NeoNetworkManager).GetField("_pendingPlayerConnections", Instance).GetValue(manager);
            HashSet<int> reported =
                (HashSet<int>)typeof(NeoNetworkManager).GetField("_playerReadyReported", Instance).GetValue(manager);
            GameObject player = new GameObject("Disconnect player", typeof(NetworkIdentity));
            _objects.Add(player);
            NetworkIdentity identity = player.GetComponent<NetworkIdentity>();
            identity.sceneId = 0;
            typeof(NetworkIdentity).GetMethod("InitializeNetworkBehaviours", Instance).Invoke(identity, null);
            typeof(NetworkIdentity).GetProperty("connectionToClient", Instance).SetValue(identity, peer);
            typeof(NetworkConnection).GetProperty("identity", Instance).SetValue(peer, identity);
            pending.Add(peer);
            reported.Add(peer.connectionId);
            manager.OnServerClientDisconnectedEvent.AddListener(_ => throw new InvalidOperationException("disconnect listener failed"));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: disconnect listener failed"));
            bool serverWasActive = NetworkServer.active;
            try
            {
                typeof(NetworkServer).GetProperty("active", Static).SetValue(null, true);
                Assert.DoesNotThrow(() => manager.OnServerDisconnect(peer));
                Assert.That(player == null, Is.True, "Mirror must destroy the owned player after the throwing event");
            }
            finally
            {
                typeof(NetworkServer).GetProperty("active", Static).SetValue(null, serverWasActive);
            }
            Assert.That(pending.Contains(peer), Is.False);
            Assert.That(reported.Contains(peer.connectionId), Is.False);
            Assert.That(peer.identity, Is.Null, "Mirror base cleanup must continue after a listener throws");
            Assert.That(peer.owned.Count, Is.Zero, "the real owned player must be released by Mirror cleanup");
        }

        [Test]
        public void ThrowingCSharpListener_DoesNotPreventUnityEventDelivery()
        {
            NetworkConnectionToClient peer = new NetworkConnectionToClient(74);
            int called = 0;
            UnityEvent<NetworkConnectionToClient> unityEvent = new();
            unityEvent.AddListener(_ => called++);
            Action<NetworkConnectionToClient> listener = _ => throw new InvalidOperationException("CSharp listener failed");
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: CSharp listener failed"));
            MethodInfo raise = typeof(NeoNetworkManager).GetMethod("RaiseServerEvent", Static);
            Assert.DoesNotThrow(() => raise.Invoke(null, new object[] { listener, unityEvent, peer }));
            Assert.That(called, Is.EqualTo(1));
        }

        [TestCase(0, false)]
        [TestCase(-16, false)]
        [TestCase(16, false)]
        [TestCase(16, true)]
        public void TelepathyQueueLimit_IsOptIn_AndOnlyLowersEachExistingLimit(int cap, bool unbounded)
        {
            ManagerProbe manager = NewManager();
            Transport transport = NewTelepathy();
            manager.transport = transport;
            string[] names = { "clientSendQueueLimit", "clientReceiveQueueLimit", "serverSendQueueLimitPerConnection", "serverReceiveQueueLimitPerConnection" };
            int[] initial = unbounded ? new[] { 0, -1, 64, 4 } : new[] { 8, 32, 64, 4 };
            int clientMessageSize = (int)transport.GetType().GetField("clientMaxMessageSize", Instance).GetValue(transport);
            int serverMessageSize = (int)transport.GetType().GetField("serverMaxMessageSize", Instance).GetValue(transport);
            for (int i = 0; i < names.Length; i++) transport.GetType().GetField(names[i], Instance).SetValue(transport, initial[i]);
            manager.TelepathyQueueLimit = cap;
            manager.ApplyTransportQueueLimits();
            manager.ApplyTransportQueueLimits();
            Assert.That(manager.TelepathyQueueLimit, Is.EqualTo(Math.Max(0, cap)));
            Assert.That((int)transport.GetType().GetField("clientMaxMessageSize", Instance).GetValue(transport), Is.EqualTo(clientMessageSize));
            Assert.That((int)transport.GetType().GetField("serverMaxMessageSize", Instance).GetValue(transport), Is.EqualTo(serverMessageSize));
            for (int i = 0; i < names.Length; i++)
            {
                int expected = cap <= 0 ? initial[i] : initial[i] <= 0 ? cap : Math.Min(cap, initial[i]);
                Assert.That((int)transport.GetType().GetField(names[i], Instance).GetValue(transport), Is.EqualTo(expected), names[i]);
            }
        }

        [Test]
        public void QueueLimits_WithNoTransport_IsSafe()
        {
            ManagerProbe manager = NewManager();
            manager.TelepathyQueueLimit = 16;
            Assert.DoesNotThrow(() => manager.ApplyTransportQueueLimits());
        }

        [Test]
        public void QueueLimits_WithUnsupportedTransport_DoNotChangeCoincidentallyNamedFields()
        {
            ManagerProbe manager = NewManager();
            GameObject go = new GameObject("Unsupported transport");
            _objects.Add(go);
            UnsupportedTransport transport = go.AddComponent<UnsupportedTransport>();
            manager.transport = transport;
            manager.TelepathyQueueLimit = 16;
            Assert.DoesNotThrow(() => manager.ApplyTransportQueueLimits());
            Assert.That(transport.clientSendQueueLimit, Is.EqualTo(100));
        }

        private ManagerProbe NewManager()
        {
            GameObject go = new GameObject("Manager hardening tests");
            _objects.Add(go);
            return go.AddComponent<ManagerProbe>();
        }

        private Transport NewTelepathy()
        {
            Type type = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType("Mirror.TelepathyTransport");
                if (type != null) break;
            }
            Assert.That(type, Is.Not.Null, "Mirror TelepathyTransport must be available for queue configuration tests");
            GameObject go = new GameObject("Telepathy queue tests");
            _objects.Add(go);
            return (Transport)go.AddComponent(type);
        }
    }
}
#endif
