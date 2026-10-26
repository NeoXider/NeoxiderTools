#if MIRROR
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Neo.Network;
using Neo.Network.Realtime;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public struct Audit20261023TestPlayNetworkProbeMessage : NetworkMessage
    {
        public int Sequence;
        public string Text;
    }

    /// <summary>
    ///     Serialized host-loopback contracts. DummyTransport opens no sockets; these tests do not
    ///     represent a remote client or exercise the transport's send/receive implementation.
    /// </summary>
    public sealed class Audit20261023TestPlayNetworkHostContractsTests
    {
        private readonly List<GameObject> _ownedObjects = new List<GameObject>();
        private GameObject _managerObject;
        private TestNetworkManager _manager;
        private Transport _previousTransport;
        private bool _ownsSession;
        private bool _savedRunInBackground;
        private int _savedTargetFrameRate;
        private int _savedVSyncCount;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _ownsSession = false;
            _manager = null;
            _managerObject = null;
            _ownedObjects.Clear();
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False,
                "This fixture needs an idle Mirror session; it must not stop another owner's session.");
            Assert.That(NetworkManager.singleton == null, Is.True,
                "Run network fixtures in an empty scene without an existing NetworkManager.");
            NetworkIdentity[] existingIdentities = UnityEngine.Object.FindObjectsByType<NetworkIdentity>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(existingIdentities, Is.Empty,
                "Host startup would claim existing scene identities; run this fixture in an empty test scene.");

            _previousTransport = Transport.active;
            _savedRunInBackground = Application.runInBackground;
            _savedTargetFrameRate = Application.targetFrameRate;
            _savedVSyncCount = QualitySettings.vSyncCount;
            _ownsSession = true;

            _manager = NetworkTestHelper.CreateTestNetworkManager(
                "Audit20261023TestPlayNetworkManager", out _managerObject);
            _manager.autoCreatePlayer = false;
            _manager.HandshakeMode = NeoHandshakeMode.Manual;
            _manager.StartAsHost();

            yield return NetworkTestHelper.WaitUntil(() => NetworkServer.active && NetworkClient.isConnected
                && NetworkClient.ready && NetworkServer.localConnection != null
                && NetworkServer.localConnection.isReady, description: "host ready handshake");
            Assert.That(NetworkClient.connection, Is.InstanceOf<LocalConnectionToServer>());
            Assert.That(NetworkServer.localConnection.identity, Is.Null,
                "The ready-without-player contract needs a genuinely playerless connection.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (!_ownsSession)
            {
                yield break;
            }

            try
            {
                if (_manager != null)
                {
                    _manager.StopNetwork();
                }

                NetworkServer.Shutdown();
                NetworkClient.Shutdown();
                NetworkClient.ClearSpawners();
            }
            finally
            {
                for (int i = _ownedObjects.Count - 1; i >= 0; i--)
                {
                    if (_ownedObjects[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_ownedObjects[i]);
                    }
                }

                _ownedObjects.Clear();
                if (_managerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(_managerObject);
                }

                NetworkManager.ResetStatics();
                Transport.active = _previousTransport;
                Application.runInBackground = _savedRunInBackground;
                Application.targetFrameRate = _savedTargetFrameRate;
                QualitySettings.vSyncCount = _savedVSyncCount;
                _ownsSession = false;
            }

            yield return null;
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False,
                "The fixture must release its session before the next test.");
        }

        [UnityTest]
        public IEnumerator ReadyWithoutPlayer_BroadcastSkipsUntilPlayerIsAttached()
        {
            List<int> sequences = new List<int>();
            List<string> payloads = new List<string>();
            using NetClientHandlers handlers = new NetClientHandlers();
            handlers.Add<Audit20261023TestPlayNetworkProbeMessage>(message =>
            {
                sequences.Add(message.Sequence);
                payloads.Add(message.Text);
            });
            handlers.RegisterNow();
            NetworkConnectionToClient connection = NetworkServer.localConnection;

            Assert.That(NetReadyBroadcast.CountReady(), Is.EqualTo(0));
            Assert.That(NetReadyBroadcast.SendTo(connection,
                new Audit20261023TestPlayNetworkProbeMessage { Sequence = -1 }), Is.False);
            Assert.That(NetReadyBroadcast.ToReadyClients(
                new Audit20261023TestPlayNetworkProbeMessage { Sequence = -2 }), Is.EqualTo(0));

            // A same-channel control proves the client processed the queue after the rejection checks.
            connection.Send(new Audit20261023TestPlayNetworkProbeMessage
                { Sequence = 0, Text = "ready, no player" });
            yield return NetworkTestHelper.WaitUntil(() => sequences.Contains(0), description: "playerless client control message");
            Assert.That(sequences, Is.EqualTo(new[] { 0 }));

            GameObject player = AttachPlayer();
            yield return NetworkTestHelper.WaitUntil(() => NetworkClient.localPlayer != null
                && NetworkClient.localPlayer.gameObject == player, description: "manual player attachment");
            Assert.That(NetReadyBroadcast.CanReceive(connection), Is.True);
            Assert.That(NetReadyBroadcast.CountReady(), Is.EqualTo(1));

            Assert.That(NetReadyBroadcast.ToReadyClients(new Audit20261023TestPlayNetworkProbeMessage
                { Sequence = 1, Text = "Игрок | spawned" }), Is.EqualTo(1));
            Assert.That(NetReadyBroadcast.SendTo(connection, new Audit20261023TestPlayNetworkProbeMessage
                { Sequence = 2, Text = "targeted" }), Is.True);
            yield return NetworkTestHelper.WaitUntil(() => sequences.Contains(2), description: "broadcast and targeted payloads");

            Assert.That(sequences, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(payloads, Is.EqualTo(new[] { "ready, no player", "Игрок | spawned", "targeted" }));
        }

        [UnityTest]
        public IEnumerator LiveHandlers_RoundTripPayloadAndExcludeTheHostFromBroadcast()
        {
            using NetServerHandlers serverHandlers = new NetServerHandlers();
            using NetClientHandlers clientHandlers = new NetClientHandlers();
            serverHandlers.RegisterNow();
            clientHandlers.RegisterNow();
            GameObject player = AttachPlayer();
            yield return NetworkTestHelper.WaitUntil(() => NetworkClient.localPlayer != null
                && NetworkClient.localPlayer.gameObject == player, description: "manual player attachment");

            List<NetworkConnectionToClient> senders = new List<NetworkConnectionToClient>();
            List<string> serverPayloads = new List<string>();
            List<int> clientSequences = new List<int>();
            List<string> clientPayloads = new List<string>();
            serverHandlers.Add<Audit20261023TestPlayNetworkProbeMessage>((connection, message) =>
            {
                senders.Add(connection);
                serverPayloads.Add(message.Text);
                connection.Send(new Audit20261023TestPlayNetworkProbeMessage
                    { Sequence = message.Sequence + 1, Text = message.Text + "|ack" });
            });
            clientHandlers.Add<Audit20261023TestPlayNetworkProbeMessage>(message =>
            {
                clientSequences.Add(message.Sequence);
                clientPayloads.Add(message.Text);
            });

            Assert.That(NetReadyBroadcast.CountReady(includeLocalHost: false), Is.EqualTo(0));
            Assert.That(NetReadyBroadcast.ToReadyClients(new Audit20261023TestPlayNetworkProbeMessage
                { Sequence = -1, Text = "must not echo" }, includeLocalHost: false), Is.EqualTo(0));

            const string text = "Команда; player=1 | payload";
            NetworkClient.Send(new Audit20261023TestPlayNetworkProbeMessage { Sequence = 40, Text = text });
            yield return NetworkTestHelper.WaitUntil(() => clientSequences.Contains(41), description: "serialized request and acknowledgement");

            Assert.That(senders, Is.EqualTo(new[] { NetworkServer.localConnection }));
            Assert.That(serverPayloads, Is.EqualTo(new[] { text }));
            Assert.That(clientSequences, Is.EqualTo(new[] { 41 }),
                "The excluded broadcast must not arrive before the positive-control acknowledgement.");
            Assert.That(clientPayloads, Is.EqualTo(new[] { text + "|ack" }));
        }

        [UnityTest]
        public IEnumerator NetworkedDispatcher_SendsAndReceivesEachRpcWithoutHostEventDuplicates()
        {
            GameObject player = AttachPlayer();
            yield return NetworkTestHelper.WaitUntil(() => NetworkClient.localPlayer != null
                && NetworkClient.localPlayer.gameObject == player, description: "manual player attachment");

            GameObject dispatcherObject = new GameObject("Audit20261023TestPlayNetworkDispatcher");
            _ownedObjects.Add(dispatcherObject);
            dispatcherObject.SetActive(false);
            NetworkIdentity identity = dispatcherObject.AddComponent<NetworkIdentity>();
            NetworkEventDispatcher dispatcher = dispatcherObject.AddComponent<NetworkEventDispatcher>();
            dispatcher.isNetworked = true;
            identity.sceneId = 0;
            dispatcherObject.SetActive(true);
            NetworkClient.RegisterPrefab(dispatcherObject, 88432);
            NetworkServer.Spawn(dispatcherObject);
            yield return NetworkTestHelper.WaitUntil(() => identity.netId != 0 && identity.isClient
                && identity.observers.ContainsKey(NetworkServer.localConnection.connectionId), description: "dispatcher spawn and observer");

            int voidEvents = 0;
            List<int> intEvents = new List<int>();
            List<float> floatEvents = new List<float>();
            List<string> stringEvents = new List<string>();
            dispatcher.onNetworkEvent.AddListener(() => voidEvents++);
            dispatcher.onNetworkIntEvent.AddListener(value => intEvents.Add(value));
            dispatcher.onNetworkFloatEvent.AddListener(value => floatEvents.Add(value));
            dispatcher.onNetworkStringEvent.AddListener(value => stringEvents.Add(value));

            int outgoingRpcs = 0;
            int incomingRpcs = 0;
            Action<Mirror.NetworkDiagnostics.MessageInfo> sent = info =>
            {
                if (info.message is RpcMessage rpc && rpc.netId == identity.netId)
                {
                    outgoingRpcs += info.count;
                }
            };
            Action<Mirror.NetworkDiagnostics.MessageInfo> received = info =>
            {
                if (info.message is RpcMessage rpc && rpc.netId == identity.netId)
                {
                    incomingRpcs += info.count;
                }
            };

            bool barrierReceived = false;
            using NetClientHandlers handlers = new NetClientHandlers();
            handlers.Add<Audit20261023TestPlayNetworkProbeMessage>(message => barrierReceived = message.Sequence == 99);
            handlers.RegisterNow();
            Mirror.NetworkDiagnostics.OutMessageEvent += sent;
            Mirror.NetworkDiagnostics.InMessageEvent += received;
            try
            {
                dispatcher.DispatchGlobalEvent();
                dispatcher.DispatchGlobalInt(-17);
                dispatcher.DispatchGlobalFloat(42.5f);
                dispatcher.DispatchGlobalString("Событие | payload");
                Assert.That(NetReadyBroadcast.SendTo(NetworkServer.localConnection,
                    new Audit20261023TestPlayNetworkProbeMessage { Sequence = 99 }), Is.True);
                yield return NetworkTestHelper.WaitUntil(() => barrierReceived, description: "RPC queue barrier");

                Assert.That(outgoingRpcs, Is.EqualTo(4), "Local fallback cannot satisfy the networked contract.");
                Assert.That(incomingRpcs, Is.EqualTo(4), "The loopback must deserialize the RPCs before the barrier.");
                Assert.That(voidEvents, Is.EqualTo(1));
                Assert.That(intEvents, Is.EqualTo(new[] { -17 }));
                Assert.That(floatEvents, Is.EqualTo(new[] { 42.5f }));
                Assert.That(stringEvents, Is.EqualTo(new[] { "Событие | payload" }));
            }
            finally
            {
                Mirror.NetworkDiagnostics.OutMessageEvent -= sent;
                Mirror.NetworkDiagnostics.InMessageEvent -= received;
            }
        }

        private GameObject AttachPlayer()
        {
            GameObject player = new GameObject("Audit20261023TestPlayNetworkPlayer");
            _ownedObjects.Add(player);
            NetworkIdentity identity = player.AddComponent<NetworkIdentity>();
            identity.sceneId = 0;
            NetworkClient.RegisterPrefab(player, 88431);
            Assert.That(NetworkServer.AddPlayerForConnection(NetworkServer.localConnection, player), Is.True);
            return player;
        }

    }
}
#endif
