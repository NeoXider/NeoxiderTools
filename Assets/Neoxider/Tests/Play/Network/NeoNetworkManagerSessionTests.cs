#if MIRROR
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Neo.Network;
using Neo.Network.Realtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public struct SessionProbeMessage : NetworkMessage
    {
        public int Value;
    }

    public struct SessionEventMessage : NetworkMessage
    {
        public int Id;
    }

    /// <summary>
    ///     A real host loopback (dummy transport) exercising what <see cref="NeoNetworkManager"/> guarantees on top of
    ///     Mirror: the handshake, per-connection server events, the local-player event, scene-object activation and a
    ///     "host, stop, host again" restart that keeps message handlers alive.
    /// </summary>
    public class NeoNetworkManagerSessionTests
    {
        private NetworkTestSession _session;
        private GameObject _managerObject;
        private TestNetworkManager _manager;
        private GameObject _playerPrefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _session = null;
            _session = new NetworkTestSession();
            _manager = _session.OwnManager(NetworkTestHelper.CreateTestNetworkManager("SessionManager", out _managerObject));

            _playerPrefab = _session.Own(new GameObject("SessionPlayerPrefab"));
            NetworkIdentity identity = _playerPrefab.AddComponent<NetworkIdentity>();
            NetworkTestHelper.SetAssetId(identity, 86001);
            _manager.playerPrefab = _playerPrefab;
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

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds = 5f)
        {
            return NetworkTestHelper.WaitUntil(condition, timeoutSeconds);
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private GameObject NewSceneObject(string name, ulong sceneId, bool active)
        {
            GameObject go = _session.Own(new GameObject(name));
            go.AddComponent<NetworkIdentity>().sceneId = sceneId;
            go.SetActive(active);
            return go;
        }

        // ---- handshake and events ---------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Host_RaisesServerEventsInOrder_AndTheLocalPlayerEvent()
        {
            List<string> order = new List<string>();
            NetworkConnectionToClient playerReadyFor = null;
            int localSpawned = 0;
            int unityEventSpawned = 0;
            _manager.ServerClientConnected += _ => order.Add("connected");
            _manager.ServerClientReady += _ => order.Add("ready");
            _manager.ServerPlayerReady += conn =>
            {
                order.Add("playerReady");
                playerReadyFor = conn;
            };
            _manager.LocalPlayerSpawned += () => localSpawned++;
            _manager.OnLocalPlayerSpawnedEvent.AddListener(() => unityEventSpawned++);

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.localPlayer != null && order.Contains("playerReady"));
            yield return Frames(3);

            Assert.That(order, Is.EqualTo(new[] { "connected", "ready", "playerReady" }));
            Assert.That(playerReadyFor, Is.SameAs(NetworkServer.localConnection));
            Assert.That(_manager.IsConnectionSpawned(NetworkServer.localConnection), Is.True);
            Assert.That(NeoNetworkState.IsLocalHostConnection(playerReadyFor), Is.True);
            Assert.That(localSpawned, Is.EqualTo(1));
            Assert.That(unityEventSpawned, Is.EqualTo(1));
            Assert.That(_manager.IsLocalPlayerSpawned, Is.True);
        }

        [UnityTest]
        public IEnumerator UnityEvents_MirrorTheCSharpEvents()
        {
            int connected = 0;
            int ready = 0;
            int playerReady = 0;
            _manager.OnServerClientConnectedEvent.AddListener(_ => connected++);
            _manager.OnServerClientReadyEvent.AddListener(_ => ready++);
            _manager.OnServerPlayerReadyEvent.AddListener(_ => playerReady++);

            _manager.StartAsHost();
            yield return WaitUntil(() => playerReady > 0);
            yield return Frames(2);

            Assert.That(connected, Is.EqualTo(1));
            Assert.That(ready, Is.EqualTo(1));
            Assert.That(playerReady, Is.EqualTo(1), "fires once per connection");
        }

        [UnityTest]
        public IEnumerator AThrowingCSharpListener_DoesNotBreakTheOtherListenersOrTheSession()
        {
            int good = 0;
            _manager.ServerClientConnected += _ => throw new InvalidOperationException("listener bug");
            _manager.OnServerClientConnectedEvent.AddListener(_ => good++);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("listener bug"));

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.localPlayer != null);

            Assert.That(good, Is.EqualTo(1));
            Assert.That(NetworkServer.active, Is.True);
        }

        [UnityTest]
        public IEnumerator HandshakeMode_Auto_DoesNotAddAPlayerWhenAutoCreateIsOff()
        {
            _manager.autoCreatePlayer = false;
            _manager.HandshakeMode = NeoHandshakeMode.Auto;

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.ready);
            yield return WaitUntil(() => NetworkServer.localConnection != null && NetworkServer.localConnection.isReady);
            yield return NetworkTestHelper.ReliableBarrier("auto handshake playerless barrier");

            Assert.That(NetworkClient.ready, Is.True, "Ready is always sent");
            Assert.That(NetworkClient.localPlayer, Is.Null, "Auto follows Mirror's Auto Create Player flag");
            Assert.That(NetworkClient.AddPlayer(), Is.True, "Explicit add is the positive control for this connection.");
            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);
        }

        [UnityTest]
        public IEnumerator HandshakeMode_Always_AddsAPlayerEvenWithAutoCreateOff()
        {
            _manager.autoCreatePlayer = false;
            _manager.HandshakeMode = NeoHandshakeMode.Always;

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.localPlayer != null);

            Assert.That(NetworkClient.localPlayer, Is.Not.Null);
            Assert.That(_manager.IsConnectionSpawned(NetworkServer.localConnection), Is.True);
        }

        [UnityTest]
        public IEnumerator HandshakeMode_Manual_LeavesEverythingToMirror()
        {
            _manager.autoCreatePlayer = false;
            _manager.HandshakeMode = NeoHandshakeMode.Manual;

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.ready);
            yield return WaitUntil(() => NetworkServer.localConnection != null && NetworkServer.localConnection.isReady);
            yield return NetworkTestHelper.ReliableBarrier("manual handshake playerless barrier");

            Assert.That(NetworkClient.localPlayer, Is.Null);
            Assert.That(NetworkClient.AddPlayer(), Is.True);
            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);
        }

        [UnityTest]
        public IEnumerator AutoCreatePlayer_SendsExactlyOneAddPlayer()
        {
            int added = 0;
            _manager.ServerPlayerReady += _ => added++;

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.localPlayer != null);
            yield return Frames(15);

            Assert.That(added, Is.EqualTo(1));
            int players = 0;
            foreach (NetworkIdentity spawned in NetworkServer.spawned.Values)
            {
                if (spawned.connectionToClient != null)
                {
                    players++;
                }
            }

            Assert.That(players, Is.EqualTo(1), "a guaranteed handshake must not double-add the player");
        }

        [UnityTest]
        public IEnumerator PlayerReady_AlsoFiresForAPlayerAddedByGameCode()
        {
            _manager.autoCreatePlayer = false;
            _manager.HandshakeMode = NeoHandshakeMode.Manual;
            int playerReady = 0;
            _manager.ServerPlayerReady += _ => playerReady++;

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.ready && NetworkServer.localConnection != null
                                                               && NetworkServer.localConnection.isReady);
            Assert.That(playerReady, Is.EqualTo(0), "ready but playerless is not 'player ready'");

            GameObject customPlayer = _session.Own(new GameObject("CustomPlayer"));
            NetworkTestHelper.SetAssetId(customPlayer.AddComponent<NetworkIdentity>(), 86002);
            NetworkClient.RegisterPrefab(customPlayer);
            NetworkServer.AddPlayerForConnection(NetworkServer.localConnection, customPlayer);

            yield return WaitUntil(() => playerReady > 0);
            Assert.That(playerReady, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StopThenHostAgain_RaisesTheEventsAgain()
        {
            int playerReady = 0;
            int localSpawned = 0;
            _manager.ServerPlayerReady += _ => playerReady++;
            _manager.LocalPlayerSpawned += () => localSpawned++;

            _manager.StartAsHost();
            yield return WaitUntil(() => playerReady == 1 && localSpawned == 1);
            Assert.That(playerReady, Is.EqualTo(1));

            _manager.StopNetwork();
            yield return WaitUntil(() => !NetworkServer.active && !NetworkClient.active);
            yield return Frames(3);

            _manager.StartAsHost();
            yield return WaitUntil(() => playerReady == 2 && localSpawned == 2);

            Assert.That(playerReady, Is.EqualTo(2), "a second session reports its connection too");
            Assert.That(localSpawned, Is.EqualTo(2));
            Assert.That(_manager.IsConnectionSpawned(NetworkServer.localConnection), Is.True);
        }

        // ---- scene objects ---------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator StartHost_LeavesEverySceneObjectActive()
        {
            GameObject door = NewSceneObject("Door", 810, false);

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.localPlayer != null);

            Assert.That(door.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator ScenePlayerTemplate_StaysDisabled_WhileOtherSceneObjectsWake()
        {
            GameObject template = NewSceneObject("PlayerTemplate", 812, false);
            template.SetActive(true);
            GameObject other = NewSceneObject("Other", 813, false);
            _manager.UseScenePlayerTemplate = true;
            _manager.ScenePlayerTemplate = template;
            _manager.ScenePlayerTemplateSpawnId = "neo-tests-session-template";
            _manager.DisableScenePlayerTemplate = true;
            _manager.playerPrefab = null;

            _manager.StartAsHost();
            yield return WaitUntil(() => NetworkClient.localPlayer != null);

            Assert.That(template.activeSelf, Is.False, "only spawned copies of the template may be active");
            Assert.That(other.activeSelf, Is.True);
            Assert.That(NetworkClient.localPlayer.gameObject, Is.Not.SameAs(template));
        }

        // ---- message handlers across a restart ----------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ServerAndClientHandlers_SurviveAHostRestart()
        {
            int serverGot = 0;
            int clientGot = 0;
            int lastServerValue = 0;
            using NetServerHandlers serverHandlers = new NetServerHandlers();
            using NetClientHandlers clientHandlers = new NetClientHandlers();
            serverHandlers.Add<SessionProbeMessage>((conn, message) =>
            {
                serverGot++;
                lastServerValue = message.Value;
            });
            clientHandlers.Add<SessionProbeMessage>(message => clientGot++);
            serverHandlers.RegisterNow();
            clientHandlers.RegisterNow();

            for (int round = 1; round <= 2; round++)
            {
                _manager.StartAsHost();
                yield return WaitUntil(() => _manager.IsConnectionSpawned(NetworkServer.localConnection));
                yield return Frames(2);

                NetworkClient.Send(new SessionProbeMessage { Value = round * 11 });
                NetReadyBroadcast.ToReadyClients(new SessionProbeMessage { Value = round }, Channels.Reliable, true);
                yield return WaitUntil(() => serverGot == round && clientGot == round);

                Assert.That(serverGot, Is.EqualTo(round), $"server handler after start #{round}");
                Assert.That(clientGot, Is.EqualTo(round), $"client handler after start #{round}");
                Assert.That(lastServerValue, Is.EqualTo(round * 11));

                _manager.StopNetwork();
                yield return WaitUntil(() => !NetworkServer.active && !NetworkClient.active);
                yield return Frames(3);
            }
        }

        [UnityTest]
        public IEnumerator EventChannel_NeverDeliversToTheHostItself()
        {
            using NetClientHandlers handlers = new NetClientHandlers();
            NetEventChannel<SessionEventMessage> channel = new NetEventChannel<SessionEventMessage>(handlers);
            List<int> received = new List<int>();
            channel.Received += message => received.Add(message.Id);
            handlers.RegisterNow();

            _manager.StartAsHost();
            yield return WaitUntil(() => _manager.IsConnectionSpawned(NetworkServer.localConnection));

            yield return NetworkTestHelper.WaitForHost(requireLocalPlayer: true);
            Assert.That(NetworkServer.localConnection.isReady, Is.True);
            Assert.That(NetworkClient.ready, Is.True);
            Assert.That(NeoNetworkState.IsLocalHostConnection(NetworkServer.localConnection), Is.True);

            List<int> sentPayloads = new List<int>();
            List<int> admittedPayloads = new List<int>();
            List<int> sentChannels = new List<int>();
            List<int> admittedChannels = new List<int>();
            int sentEnvelopes = 0;
            int admittedEnvelopes = 0;
            int admittedBytes = 0;
            Action<Mirror.NetworkDiagnostics.MessageInfo> onSent = info =>
            {
                if (info.message is SessionEventMessage message)
                {
                    sentPayloads.Add(message.Id);
                    sentChannels.Add(info.channel);
                    sentEnvelopes += info.count;
                }
            };
            Action<Mirror.NetworkDiagnostics.MessageInfo> onAdmitted = info =>
            {
                if (info.message is SessionEventMessage message)
                {
                    admittedPayloads.Add(message.Id);
                    admittedChannels.Add(info.channel);
                    admittedEnvelopes += info.count;
                    admittedBytes += info.bytes;
                }
            };
            Mirror.NetworkDiagnostics.OutMessageEvent += onSent;
            Mirror.NetworkDiagnostics.InMessageEvent += onAdmitted;
            try
            {
                int sent = channel.Publish(new SessionEventMessage { Id = 1 });
                bool targeted = channel.PublishTo(NetworkServer.localConnection, new SessionEventMessage());
                // Exercise the actual installed channel handler through serialized host ingress.
                // It must defensively drop this message too; diagnostics only observe admission.
                NetworkServer.localConnection.Send(new SessionEventMessage { Id = 99 }, Channels.Reliable);
                yield return NetworkTestHelper.WaitUntil(() => admittedPayloads.Count > 0,
                    description: "event-channel serialized host admission control");
                yield return NetworkTestHelper.ReliableBarrier("excluded event-channel host admission barrier");

                Assert.That(sent, Is.EqualTo(0), "the loopback connection is skipped: the authority already acted locally");
                Assert.That(targeted, Is.False);
                Assert.That(channel.PublishedCount, Is.EqualTo(1));
                Assert.That(sentPayloads, Is.EqualTo(new[] { 99 }), "Publish and PublishTo must send no host envelope.");
                Assert.That(sentEnvelopes, Is.EqualTo(1));
                Assert.That(admittedPayloads, Is.EqualTo(new[] { 99 }), "The genuine serialized control must be decoded intact.");
                Assert.That(admittedEnvelopes, Is.EqualTo(1));
                Assert.That(admittedBytes, Is.GreaterThan(0));
                Assert.That(sentChannels, Is.EqualTo(new[] { Channels.Reliable }));
                Assert.That(admittedChannels, Is.EqualTo(new[] { Channels.Reliable }));
                Assert.That(NetworkServer.active && NetworkClient.isConnected, Is.True);
                Assert.That(_manager.IsConnectionSpawned(NetworkServer.localConnection), Is.True);
                Assert.That(NetworkClient.localPlayer != null, Is.True);
                Assert.That(received, Is.Empty, "The channel drops every host-side message, including direct ingress.");
                Assert.That(channel.ReceivedCount, Is.Zero);
            }
            finally
            {
                Mirror.NetworkDiagnostics.OutMessageEvent -= onSent;
                Mirror.NetworkDiagnostics.InMessageEvent -= onAdmitted;
            }
        }

        [UnityTest]
        public IEnumerator FragmentedFrame_ReachesTheHostClientIntact()
        {
            using NetClientHandlers handlers = new NetClientHandlers();
            NetFrameReceiver receiver = new NetFrameReceiver(handlers, 16 * 1024);
            byte[] received = null;
            receiver.Subscribe(3, frame =>
            {
                received = new byte[frame.Count];
                Buffer.BlockCopy(frame.Array, frame.Offset, received, 0, frame.Count);
            });
            handlers.RegisterNow();

            byte[] frame4500 = new byte[4500];
            for (int i = 0; i < frame4500.Length; i++)
            {
                frame4500[i] = (byte)(i * 13 + 5);
            }

            _manager.StartAsHost();
            yield return WaitUntil(() => _manager.IsConnectionSpawned(NetworkServer.localConnection));

            NetFrameSender sender = new NetFrameSender(3, Channels.Reliable, 1000);
            int connections = sender.SendToReady(new ArraySegment<byte>(frame4500), includeLocalHost: true);
            yield return WaitUntil(() => received != null);

            Assert.That(connections, Is.EqualTo(1));
            Assert.That(sender.FragmentsSent, Is.EqualTo(5));
            Assert.That(received, Is.EqualTo(frame4500));
            Assert.That(receiver.FramesDelivered, Is.EqualTo(1));
            Assert.That(receiver.GetAssembler(3).CompletedFrames, Is.EqualTo(1));
            receiver.Detach();
        }
    }
}
#endif
