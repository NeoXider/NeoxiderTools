#if MIRROR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Mirror;
using Neo.Network;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using TestTransport = global::Neo.Editor.Tests.Audit20261023FixtureNeoNetworkManagerHardeningUnsupportedTransport;
using NetworkDiagnostics = Neo.Network.NetworkDiagnostics;
using ManagerProbe = global::Neo.Editor.Tests.Audit20261023FixtureNetworkContextActionIngressManagerProbe;
using CooldownRelay = global::Neo.Editor.Tests.Audit20261023FixtureNetworkContextActionIngressCooldownRelay;

namespace Neo.Editor.Tests
{
    /// <summary>Ingress regressions without a listening transport; only spawned objects are eligible targets.</summary>
    [TestFixture]
    public sealed class NetworkContextActionIngressTests
    {




        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly List<GameObject> _objects = new();
        private readonly Dictionary<uint, NetworkIdentity> _savedServerObjects = new();
        private readonly Dictionary<uint, NetworkIdentity> _savedClientObjects = new();
        private readonly Dictionary<int, NetworkConnectionToClient> _savedConnections = new();
        private IDictionary _savedServerHandlers;
        private IDictionary _savedClientHandlers;
        private object _savedClientState;
        private bool _savedServerActive;
        private bool _savedLogs;
        private bool _savedWarnings;
        private int _applied;
        private readonly List<NetworkContextActionRelay> _savedRelays = new();
        private IDictionary _savedBuckets;
        private bool _savedRegistrationLogs;
        private static HashSet<NetworkContextActionRelay> EnabledRelays =>
            (HashSet<NetworkContextActionRelay>)typeof(NetworkContextActionRelay).GetField("s_enabledRelays", Static).GetValue(null);

        private static IDictionary ServerHandlers => (IDictionary)typeof(NetworkServer).GetField("handlers", Static).GetValue(null);
        private static IDictionary ClientHandlers => (IDictionary)typeof(NetworkClient).GetField("handlers", Static).GetValue(null);
        private static IDictionary Buckets => (IDictionary)typeof(NetworkContextActionRelay).GetField("s_serverIngressBuckets", Static).GetValue(null);
        private static ushort MessageId => NetworkMessageId<NetworkContextActionMessage>.Id;

        [SetUp]
        public void SetUp()
        {
            _savedServerActive = NetworkServer.active;
            _savedClientState = typeof(NetworkClient).GetField("connectState", Static).GetValue(null);
            _savedRelays.Clear();
            _savedRelays.AddRange(EnabledRelays);
            _savedBuckets = new Hashtable(Buckets);
            _savedRegistrationLogs = (bool)typeof(NetworkContextActionRelay).GetField("s_verboseRegistrationLogging", Static).GetValue(null);
            _savedLogs = NetworkDiagnostics.RuntimeLogsEnabled;
            _savedWarnings = NetworkDiagnostics.RuntimeWarningsEnabled;
            _savedServerHandlers = new Hashtable(ServerHandlers);
            _savedClientHandlers = new Hashtable(ClientHandlers);
            Copy(NetworkServer.spawned, _savedServerObjects);
            Copy(NetworkClient.spawned, _savedClientObjects);
            Copy(NetworkServer.connections, _savedConnections);
            ServerHandlers.Clear();
            ClientHandlers.Clear();
            NetworkServer.spawned.Clear();
            NetworkClient.spawned.Clear();
            NetworkServer.connections.Clear();
            SetServerActive(true);
            SetClientActive(false);
            NetworkDiagnostics.RuntimeLogsEnabled = false;
            NetworkDiagnostics.RuntimeWarningsEnabled = false;
            CallStatic("ResetStaticDiagnosticsState");
            _applied = 0;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
            {
                if (go != null)
                {
                    foreach (NetworkContextActionRelay relay in go.GetComponents<NetworkContextActionRelay>())
                    {
                        CallLifecycle(relay, "OnDisable");
                    }
                    Object.DestroyImmediate(go);
                }
            }
            _objects.Clear();
            CallStatic("ResetStaticDiagnosticsState");
            foreach (NetworkContextActionRelay relay in _savedRelays) EnabledRelays.Add(relay);
            RestoreHandlers(Buckets, _savedBuckets);
            typeof(NetworkContextActionRelay).GetField("s_verboseRegistrationLogging", Static).SetValue(null, _savedRegistrationLogs);
            Restore(NetworkServer.spawned, _savedServerObjects);
            Restore(NetworkClient.spawned, _savedClientObjects);
            Restore(NetworkServer.connections, _savedConnections);
            RestoreHandlers(ServerHandlers, _savedServerHandlers);
            RestoreHandlers(ClientHandlers, _savedClientHandlers);
            SetServerActive(_savedServerActive);
            typeof(NetworkClient).GetField("connectState", Static).SetValue(null, _savedClientState);
            NetworkDiagnostics.RuntimeLogsEnabled = _savedLogs;
            NetworkDiagnostics.RuntimeWarningsEnabled = _savedWarnings;
        }

        [Test]
        public void Registration_WithZeroRelays_DoesNotOpenServerOrClientIngress()
        {
            SetClientActive(true);
            NetworkContextActionRelay.RegisterMirrorHandlers();
            Assert.That(ServerHandlers.Contains(MessageId), Is.False);
            Assert.That(ClientHandlers.Contains(MessageId), Is.False);
        }

        [Test]
        public void ManagerServerAndClientStartup_WithZeroRelays_CannotOpenIngress()
        {
            GameObject go = new GameObject("Zero-relay manager");
            _objects.Add(go);
            ManagerProbe manager = go.AddComponent<ManagerProbe>();
            manager.ActivateSceneObjectsOnStart = false;
            SetClientActive(true);
            manager.OnStartServer();
            manager.OnStartClient();
            Assert.That(ServerHandlers.Contains(MessageId), Is.False);
            Assert.That(ClientHandlers.Contains(MessageId), Is.False);
        }

        [Test]
        public void RelayLifecycle_IsIdempotent_AndLastDisableClosesIngress()
        {
            SetClientActive(true);
            NetworkContextActionRelay relay = NewRelay(10);
            CallLifecycle(relay, "Awake");
            CallLifecycle(relay, "OnEnable");
            NetworkContextActionRelay.RegisterMirrorHandlers();
            Assert.That(ServerHandlers.Contains(MessageId), Is.True);
            Assert.That(ClientHandlers.Contains(MessageId), Is.True);
            CallLifecycle(relay, "OnDisable");
            CallLifecycle(relay, "OnDisable");
            Assert.That(ServerHandlers.Contains(MessageId), Is.False);
            Assert.That(ClientHandlers.Contains(MessageId), Is.False);
            CallLifecycle(relay, "OnEnable");
            Assert.That(ServerHandlers.Contains(MessageId), Is.True);
        }

        [Test]
        public void DisablingOneOfTwoRelays_PreservesHandler_UntilLastDisable()
        {
            NetworkContextActionRelay first = NewRelay(10);
            NetworkContextActionRelay second = NewRelay(11);
            CallLifecycle(first, "OnDisable");
            Assert.That(ServerHandlers.Contains(MessageId), Is.True);
            CallLifecycle(second, "OnDisable");
            Assert.That(ServerHandlers.Contains(MessageId), Is.False);
        }

        [Test]
        public void ActiveRelay_ReRegistersAfterMirrorClearsHandlerDictionary()
        {
            NewRelay(10);
            ServerHandlers.Clear();
            NetworkContextActionRelay.RegisterMirrorHandlers();
            Assert.That(ServerHandlers.Contains(MessageId), Is.True);
        }

        [TestCase("unready")]
        [TestCase("playerless")]
        [TestCase("foreignPlayer")]
        [TestCase("departed")]
        [TestCase("unauthenticated")]
        public void UnattributableSender_IsRejectedBeforeIngressBucketAllocation(string condition)
        {
            NetworkContextActionRelay relay = NewRelay(10);
            NetworkConnectionToClient peer = NewPeer(20, 100);
            if (condition == "unready") peer.isReady = false;
            if (condition == "playerless") SetPlayer(peer, null);
            if (condition == "foreignPlayer") SetOwner(peer.identity, new NetworkConnectionToClient(21));
            if (condition == "departed") NetworkServer.connections.Remove(peer.connectionId);
            if (condition == "unauthenticated") peer.isAuthenticated = false;
            Receive(peer, Message(relay, 100));
            Assert.That(_applied, Is.Zero);
            Assert.That(Buckets.Count, Is.Zero, "unattributable traffic must not create retained state");
        }

        [Test]
        public void NullSender_IsRejectedWithoutThrowingOrRetainedState()
        {
            NetworkContextActionRelay relay = NewRelay(10);
            Assert.DoesNotThrow(() => Receive(null, Message(relay, 100)));
            Assert.That(Buckets.Count, Is.Zero);
        }

        [Test]
        public void SceneOnlyOrClientOnlyIdentity_IsNeverResolvedOnServer()
        {
            NetworkConnectionToClient peer = NewPeer(20, 100);
            NetworkContextActionRelay relay = NewRelay(10);
            NetworkIdentity sceneOnly = NewIdentity(200);
            NetworkClient.spawned[200] = sceneOnly;
            Receive(peer, Message(relay, 200));
            Assert.That(_applied, Is.Zero);
            NetworkClient.spawned.Clear();
            Receive(peer, Message(relay, 200));
            Assert.That(_applied, Is.Zero);
        }

        [Test]
        public void OutOfRangeComponentIndex_DoesNotFallBackToFirstRelay()
        {
            NetworkConnectionToClient peer = NewPeer(20, 100);
            NetworkContextActionRelay relay = NewRelay(10);
            NetworkContextActionMessage message = Message(relay, 100);
            message.relayComponentIndex = byte.MaxValue;
            Receive(peer, message);
            Assert.That(_applied, Is.Zero);
        }

        [Test]
        public void DisabledRelay_IsRejectedWhileAnotherRelayKeepsIngressOpen()
        {
            NetworkConnectionToClient peer = NewPeer(20, 100);
            NetworkContextActionRelay relay = NewRelay(10);
            NewRelay(11);
            relay.enabled = false;
            CallLifecycle(relay, "OnDisable");
            Receive(peer, Message(relay, 100));
            Assert.That(_applied, Is.Zero);
        }

        [Test]
        public void ForeignOwnedContext_IsRejected()
        {
            NetworkConnectionToClient peer = NewPeer(20, 100);
            NetworkConnectionToClient other = NewPeer(21, 101);
            NetworkContextActionRelay relay = NewRelay(10);
            Receive(peer, Message(relay, other.identity.netId));
            Assert.That(_applied, Is.Zero);
        }

        [Test]
        public void OwnerOnlyRelay_RemainsProtected_WhenSenderOwnsContext()
        {
            NetworkConnectionToClient peer = NewPeer(20, 100);
            NetworkConnectionToClient other = NewPeer(21, 101);
            NetworkContextActionRelay relay = NewRelay(10);
            relay.AuthorityMode = NetworkAuthorityMode.OwnerOnly;
            SetOwner(relay.GetComponent<NetworkIdentity>(), other);
            Receive(peer, Message(relay, peer.identity.netId));
            Assert.That(_applied, Is.Zero, "context ownership must not replace relay ownership checks");
        }

        [Test]
        public void Bucket_BoundsBurst_Refills_AndDoesNotStarveAnotherPeer()
        {
            NetworkContextActionRelay relay = NewRelay(10);
            NetworkConnectionToClient peer = NewPeer(20, 100);
            NetworkConnectionToClient other = NewPeer(21, 101);
            for (int i = 0; i < 20; i++) Receive(peer, Message(relay, 100));
            Assert.That(_applied, Is.EqualTo(5));
            Receive(other, Message(relay, 101));
            Assert.That(_applied, Is.EqualTo(6));
            object bucket = Buckets[peer];
            bucket.GetType().GetField("LastRefillTime", Instance).SetValue(bucket, Time.unscaledTimeAsDouble - 1d);
            Buckets[peer] = bucket;
            Receive(peer, Message(relay, 100));
            Assert.That(_applied, Is.EqualTo(7));
        }

        [Test]
        public void DisconnectAndConnectionIdReuse_RemoveOldBudget_AndGiveNewPeerFreshBurst()
        {
            NetworkContextActionRelay relay = NewRelay(10);
            NetworkConnectionToClient old = NewPeer(20, 100);
            for (int i = 0; i < 5; i++) Receive(old, Message(relay, 100));
            NetworkServer.connections.Remove(old.connectionId);
            CallStatic("ForgetServerIngressPeer", old);
            Assert.That(Buckets.Count, Is.Zero);
            NetworkConnectionToClient replacement = NewPeer(20, 101);
            Receive(replacement, Message(relay, 101));
            Assert.That(_applied, Is.EqualTo(6));
            Assert.That(Buckets.Contains(old), Is.False);
        }

        [Test]
        public void ServerRestart_ClearsExhaustedIngressBudget()
        {
            NetworkContextActionRelay relay = NewRelay(10);
            NetworkConnectionToClient peer = NewPeer(20, 100);
            for (int i = 0; i < 5; i++) Receive(peer, Message(relay, 100));
            relay.OnStopServer();
            Assert.That(Buckets.Count, Is.Zero);
            relay.OnStartServer();
            Receive(peer, Message(relay, 100));
            Assert.That(_applied, Is.EqualTo(6));
        }

        [Test]
        public void RejectedForeignOwner_DoesNotConsumeAuthoredRelayCooldown()
        {
            NetworkConnectionToClient owner = NewPeer(20, 100);
            NetworkConnectionToClient foreign = NewPeer(21, 101);
            NetworkContextActionRelay relay = NewRelay(10, true);
            relay.AuthorityMode = NetworkAuthorityMode.OwnerOnly;
            SetOwner(relay.GetComponent<NetworkIdentity>(), owner);
            Receive(foreign, Message(relay, 101));
            Receive(owner, Message(relay, 100));
            Assert.That(_applied, Is.EqualTo(1));
        }

        [Test]
        public void ExistingActiveRelay_ReTracksAfterSubsystemStaticReset()
        {
            NetworkContextActionRelay relay = NewRelay(10);
            CallStatic("ResetStaticDiagnosticsState");
            ServerHandlers.Clear();
            relay.OnStartServer();
            Assert.That(ServerHandlers.Contains(MessageId), Is.True);
        }

        [Test]
        public void NullSender_WithHostConnection_IsNotTreatedAsHostOrThrown()
        {
            PropertyInfo local = typeof(NetworkServer).GetProperty("localConnection", Static);
            object saved = local.GetValue(null);
            try
            {
                local.SetValue(null, new LocalConnectionToClient());
                object result = null;
                Assert.DoesNotThrow(() => result = CallStatic("IsHostLocalConnection", new object[] { null }));
                Assert.That(result, Is.EqualTo(false));
            }
            finally
            {
                local.SetValue(null, saved);
            }
        }

        [Test]
        public void ServerListenerException_IsLogged_AndDoesNotReachMirrorsDisconnectPath()
        {
            NetworkConnectionToClient peer = NewPeer(20, 100);
            NetworkContextActionRelay relay = NewRelay(10);
            relay.OnNetworkTriggered.AddListener(() => throw new InvalidOperationException("server relay listener bug"));
            LogAssert.Expect(LogType.Exception, new Regex("server relay listener bug"));

            Assert.DoesNotThrow(() => Receive(peer, Message(relay, 100)),
                "an exception escaping the handler makes Mirror disconnect the innocent sender");
            Assert.That(_applied, Is.EqualTo(1), "listeners before the failing one still ran");
        }

        [Test]
        public void ClientListenerException_IsLogged_AndDoesNotReachMirrorsDisconnectPath()
        {
            SetClientActive(true);
            NetworkContextActionRelay relay = NewRelay(10);
            NetworkIdentity context = NewIdentity(100);
            NetworkClient.spawned[10] = relay.GetComponent<NetworkIdentity>();
            NetworkClient.spawned[100] = context;
            relay.OnNetworkTriggered.AddListener(() => throw new InvalidOperationException("client relay listener bug"));
            LogAssert.Expect(LogType.Exception, new Regex("client relay listener bug"));

            Assert.DoesNotThrow(() => CallStatic("OnClientMessage", Message(relay, 100)));
            Assert.That(_applied, Is.EqualTo(1));
        }

        [TestCase(NetworkActionScope.AllClients)]
        [TestCase(NetworkActionScope.OthersOnly)]
        public void Broadcast_ReachesTheRelayObserversOnly(NetworkActionScope scope)
        {
            Transport savedTransport = UseCountingTransport(out Func<int> broadcasts, out Action stopCounting);
            try
            {
                NetworkConnectionToClient sender = NewPeer(20, 100);
                NetworkConnectionToClient other = NewPeer(21, 101);
                NewPeer(24, 102);
                NetworkServer.connections[22] = new NetworkConnectionToClient(22)
                    { isReady = false, isAuthenticated = true };
                NetworkConnectionToClient spectator = new NetworkConnectionToClient(23)
                    { isReady = true, isAuthenticated = true };
                NetworkServer.connections[23] = spectator;
                NetworkContextActionRelay relay = NewRelay(10);
                relay.Scope = scope;
                Observe(relay, sender, other, spectator);

                Receive(sender, Message(relay, 100));

                // WHY: peer 24 is spawned but does not observe the relay (hidden by interest management), so it has
                // no relay and possibly no handler: a message would disconnect it. The unready peer 22 is still
                // loading. The playerless spectator observes the relay's object, so it holds the relay and is served.
                Assert.That(broadcasts(), Is.EqualTo(scope == NetworkActionScope.AllClients ? 3 : 2));
            }
            finally
            {
                stopCounting();
                Transport.active = savedTransport;
            }
        }

        [Test]
        public void HostListenerException_DoesNotSkipTheRemoteObservers()
        {
            PropertyInfo local = typeof(NetworkServer).GetProperty("localConnection", Static);
            object savedLocal = local.GetValue(null);
            Transport savedTransport = UseCountingTransport(out Func<int> broadcasts, out Action stopCounting);
            try
            {
                SetClientActive(true);
                LocalConnectionToClient host = new LocalConnectionToClient();
                local.SetValue(null, host);
                NetworkConnectionToClient sender = NewPeer(20, 100);
                NetworkConnectionToClient other = NewPeer(21, 101);
                NetworkContextActionRelay relay = NewRelay(10);
                relay.Scope = NetworkActionScope.AllClients;
                Observe(relay, host, sender, other);
                relay.OnNetworkTriggered.AddListener(() => throw new InvalidOperationException("host relay listener bug"));
                LogAssert.Expect(LogType.Exception, new Regex("host relay listener bug"));

                Receive(sender, Message(relay, 100));

                Assert.That(broadcasts(), Is.EqualTo(2),
                    "remote observers are served before the host applies, so a host listener bug cannot desync them");
                Assert.That(_applied, Is.EqualTo(1), "the host still applied the action locally");
            }
            finally
            {
                stopCounting();
                Transport.active = savedTransport;
                local.SetValue(null, savedLocal);
            }
        }

        [Test]
        public void ResolveType_RetriesAMissOnlyAfterAnotherAssemblyLoads()
        {
            const string Unknown = "Neo.Network.Tests.TypeLoadedLater";
            IDictionary misses = (IDictionary)typeof(NetworkContextActionRelay)
                .GetField("s_unresolvedTypes", Static).GetValue(null);

            Assert.That(NetworkContextActionRelay.ResolveType(Unknown), Is.Null);
            object firstScan = misses[Unknown];
            Assert.That(NetworkContextActionRelay.ResolveType(Unknown), Is.Null);
            Assert.That(misses[Unknown], Is.EqualTo(firstScan), "without a new assembly the cached miss is reused");

            CallStatic("OnAssemblyLoaded", null, null);

            Assert.That(NetworkContextActionRelay.ResolveType(Unknown), Is.Null);
            Assert.That(misses[Unknown], Is.Not.EqualTo(firstScan),
                "a DLL loaded at runtime (a mod or hot update) may define the type, so the miss is scanned again");
        }

        [Test]
        public void ResolveType_RepeatsWithoutRescanningOrAllocating()
        {
            string known = typeof(NetworkContextActionRelay).FullName;
            const string Unknown = "Neo.Network.Tests.TypeThatDoesNotExist";
            Assert.That(NetworkContextActionRelay.ResolveType(known), Is.SameAs(typeof(NetworkContextActionRelay)));
            Assert.That(NetworkContextActionRelay.ResolveType(Unknown), Is.Null);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            Type knownAgain = NetworkContextActionRelay.ResolveType(known);
            Type unknownAgain = NetworkContextActionRelay.ResolveType(Unknown);
            long allocated = allocationCounter.End();

            Assert.That(knownAgain, Is.SameAs(typeof(NetworkContextActionRelay)));
            Assert.That(unknownAgain, Is.Null);
            Assert.That(allocated, Is.Zero, "a repeated lookup must not walk AppDomain assemblies again");
        }

        private NetworkIdentity NewIdentity(uint id)
        {
            GameObject go = new GameObject("Ingress " + id, typeof(NetworkIdentity));
            _objects.Add(go);
            NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
            typeof(NetworkIdentity).GetProperty("netId", Instance).SetValue(identity, id);
            return identity;
        }

        private NetworkContextActionRelay NewRelay(uint id, bool cooldown = false)
        {
            NetworkIdentity identity = NewIdentity(id);
            NetworkContextActionRelay relay = cooldown
                ? identity.gameObject.AddComponent<CooldownRelay>()
                : identity.gameObject.AddComponent<NetworkContextActionRelay>();
            relay.isNetworked = true;
            relay.Scope = NetworkActionScope.ServerOnly;
            relay.OnNetworkTriggered.AddListener(() => _applied++);
            typeof(NetworkIdentity).GetMethod("InitializeNetworkBehaviours", Instance).Invoke(identity, null);
            NetworkServer.spawned[id] = identity;
            CallLifecycle(relay, "OnEnable");
            return relay;
        }

        private NetworkConnectionToClient NewPeer(int connectionId, uint playerId)
        {
            NetworkConnectionToClient peer = new NetworkConnectionToClient(connectionId)
                { isReady = true, isAuthenticated = true };
            NetworkIdentity identity = NewIdentity(playerId);
            SetOwner(identity, peer);
            SetPlayer(peer, identity);
            NetworkServer.spawned[playerId] = identity;
            NetworkServer.connections[connectionId] = peer;
            return peer;
        }

        private Transport UseCountingTransport(out Func<int> sentRelayMessages, out Action stopCounting)
        {
            GameObject transportObject = new GameObject("Ingress test transport");
            _objects.Add(transportObject);
            Transport saved = Transport.active;
            Transport.active = transportObject.AddComponent<TestTransport>();
            int count = 0;
            Action<Mirror.NetworkDiagnostics.MessageInfo> countRelayMessages = info =>
            {
                if (info.message is NetworkContextActionMessage)
                {
                    count += info.count;
                }
            };
            Mirror.NetworkDiagnostics.OutMessageEvent += countRelayMessages;
            sentRelayMessages = () => count;
            stopCounting = () => Mirror.NetworkDiagnostics.OutMessageEvent -= countRelayMessages;
            return saved;
        }

        private static void Observe(NetworkContextActionRelay relay, params NetworkConnectionToClient[] connections)
        {
            NetworkIdentity identity = relay.GetComponent<NetworkIdentity>();
            foreach (NetworkConnectionToClient connection in connections)
            {
                identity.observers[connection.connectionId] = connection;
            }
        }

        private static NetworkContextActionMessage Message(NetworkContextActionRelay relay, uint contextId) =>
            new NetworkContextActionMessage { relayNetId = relay.GetComponent<NetworkIdentity>().netId,
                relayComponentIndex = (byte)relay.ComponentIndex, contextNetId = contextId };

        private static void SetOwner(NetworkIdentity identity, NetworkConnectionToClient owner) =>
            typeof(NetworkIdentity).GetProperty("connectionToClient", Instance).SetValue(identity, owner);

        private static void SetPlayer(NetworkConnectionToClient peer, NetworkIdentity identity) =>
            typeof(NetworkConnection).GetProperty("identity", Instance).SetValue(peer, identity);

        private static void SetServerActive(bool active) =>
            typeof(NetworkServer).GetProperty("active", Static).SetValue(null, active);

        private static void Receive(NetworkConnectionToClient peer, NetworkContextActionMessage message) =>
            CallStatic("OnServerMessage", peer, message);

        private static object CallStatic(string name, params object[] arguments) =>
            typeof(NetworkContextActionRelay).GetMethod(name, Static).Invoke(null, arguments);

        private static void CallLifecycle(NetworkContextActionRelay relay, string name) =>
            typeof(NetworkContextActionRelay).GetMethod(name, Instance).Invoke(relay, null);

        private static void SetClientActive(bool active)
        {
            FieldInfo field = typeof(NetworkClient).GetField("connectState", Static);
            field.SetValue(null, Enum.Parse(field.FieldType, active ? "Connected" : "None"));
        }

        private static void Copy<TKey, TValue>(Dictionary<TKey, TValue> from, Dictionary<TKey, TValue> to)
        {
            to.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in from) to.Add(pair.Key, pair.Value);
        }

        private static void Restore<TKey, TValue>(Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> saved)
        {
            target.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in saved) target.Add(pair.Key, pair.Value);
        }

        private static void RestoreHandlers(IDictionary target, IDictionary saved)
        {
            target.Clear();
            foreach (DictionaryEntry entry in saved) target.Add(entry.Key, entry.Value);
        }
    }
}
#endif
