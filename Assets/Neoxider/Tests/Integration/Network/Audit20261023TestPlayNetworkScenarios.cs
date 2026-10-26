#if MIRROR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mirror;
using Neo.Network;
using Neo.Network.Realtime;
using Neo.Reactive;
using Neo.Save;
using UnityEngine;

namespace Neo.Tests.Network.Integration
{
    public sealed partial class Audit20261023TestPlayNetworkPeerDriver
    {
        private MatrixState _expectedState;
        private double _lastMoneyFence = double.NegativeInfinity;
        private const MatrixChecks StateChecks = MatrixChecks.Counter | MatrixChecks.Selector
            | MatrixChecks.Money | MatrixChecks.Rpg | MatrixChecks.Property;

        private IEnumerator RunServer()
        {
            yield return StartServerWorld();
            _artifacts.Write("server-listening", "Dedicated Telepathy server; seeded world is ready for client A.");
            yield return Until(() => _peers.ContainsKey("client-a"), "client A Authenticated/Ready/PlayerSpawned");
            yield return ObservePeers("rpg-final-initialization", StateChecks, onlyPeer: "client-a");

            // Mutate after A's initial receipt; B must receive the new state in its spawn payload.
            _world.Character.Damage(47f);
            Require(_world.Character.Spend(Audit20261023TestPlayNetworkWorld.ResourceId, 3f), "Server resource spend failed.");
            _expectedState.hp = 73f;
            _expectedState.resource = 7f;
            yield return ObservePeers("rpg-live-snapshot", MatrixChecks.Rpg, onlyPeer: "client-a");
            _artifacts.Write("launch-client-b", "A observed final initialization; admit the independent late client now.");
            yield return Until(() => _peers.ContainsKey("client-b"), "independent late client B");
            yield return ObservePeers("late-join-initial", StateChecks);

            yield return CounterCase();
            yield return RelayCases();
            yield return SelectorCases();
            yield return MoneyCases();
            yield return MoneyPrecisionCases();
            yield return CounterFiniteIngressCases();
            yield return HandlerCase("handler-replaced-older-disposed", 101, "lease-first");
            yield return HandlerCase("handler-current-disposed", 103, "lease-opposite");
            yield return PollingCase();
            yield return PhysicsCase();

            yield return Reconnect(false);
            // AllClients context actions are transient: A retains its child state, while B's
            // re-created replicas start inactive and receive no implicit replay.
            yield return ObservePeers("late-join-reconnect", StateChecks | MatrixChecks.Reactive | MatrixChecks.Context,
                serverBonusA: false, aBonusA: true, bBonusA: false);
            yield return HandlerCase("handler-client-reconnect", 105, "lease-current");
            yield return Reconnect(true);
            yield return ObservePeers("server-restart-snapshot", StateChecks);
            yield return HandlerCase("handler-server-restart", 107, "lease-current");

            BeginStep();
            foreach (string peer in PeerIds()) Send(peer, "finish", new MatrixInstruction { caseId = "matrix-complete" });
            yield return Responses();
            BeginStep();
            foreach (string peer in PeerIds()) Send(peer, "exit", new MatrixInstruction { caseId = "matrix-complete" });
            yield return Until(() => _peers.Count == 0, "both owned clients stopped");
            yield return Finish(true);
        }

        private IEnumerator StartServerWorld()
        {
            _manager.StartAsServer();
            yield return Until(() => NetworkServer.active, "dedicated ServerStarted");
            Require(!NetworkClient.active && NetworkServer.localConnection == null, "Host substitution is forbidden.");
            _world = Audit20261023TestPlayNetworkWorld.Create(this);
            NetworkServer.Spawn(_world.gameObject, Audit20261023TestPlayNetworkWorld.WorldAssetId);
            yield return Until(() => _world != null && _world.StartCompleted, "server world Start completed");
            _world.Counter.Set(10);
            _world.Selector.Set(0);
            _world.Selector.SetExcludedIndices(new[] { 0, 1, 3 });
            _world.Selector.SetRandom(false); // Exactly one eligible item; retain item 0.
            _world.Selector.SetExcludedIndices(new[] { 1 });
            _world.Money.SetMoney(200f);
            Require(_world.Money.ClientMutationAuthority == NetworkAuthorityMode.ServerOnly,
                "Fresh Money does not default to ServerOnly.");
            _expectedState = new MatrixState
            {
                counter = 10, selector = 2, selectorFill = false,
                activeIndices = new[] { 0, 2 }, excludedIndices = new[] { 1 }, money = 200f,
                hp = 120f, maxHp = 120f, resource = 10f, property = 3f,
                reactiveExists = true, reactive = 0f
            };
            CheckServer("seeded-world", StateChecks);
        }

        private IEnumerator CounterCase()
        {
            const string caseId = "counter-concurrent-relative-add";
            yield return ResetEvents(caseId);
            NetworkIdentity identity = _world.netIdentity;
            Dictionary<int, NetworkConnectionToClient> observers =
                new Dictionary<int, NetworkConnectionToClient>(identity.observers);
            // Hold only this test identity's outbound replication. Both independent clients must
            // invoke Add(1) from 10 even when their OS scheduling/network latency differs.
            identity.observers.Clear();
            foreach (NetworkConnectionToClient connection in observers.Values) connection.observing.Remove(identity);
            try
            {
                _artifacts.Write("controlled-stale-replicas", "World observers temporarily held; actual client Commands remain on Telepathy.",
                    caseId: caseId, state: _world.Capture(_playerA, _playerB));
                yield return ExecutePeers(caseId, "counter-add", 1, "counter-add", 1);
                Require(Response("client-a").counterAtInvocation == 10
                    && Response("client-b").counterAtInvocation == 10, "Counter callers did not both start from 10.");
                _expectedState.counter = 12;
                CheckServer(caseId, MatrixChecks.Counter, Events("counter", 11f, 12f));
            }
            finally
            {
                foreach (KeyValuePair<int, NetworkConnectionToClient> pair in observers)
                {
                    identity.observers[pair.Key] = pair.Value;
                    pair.Value.observing.Add(identity);
                }
            }
            // Set(12) is correctly a no-op. Two explicit trusted publications rejoin the replicas;
            // the server's 12 and two accepted deltas above are asserted before these writes.
            yield return ResetEvents(caseId);
            _world.Counter.Set(13);
            _world.Counter.Set(12);
            yield return ObservePeers(caseId, MatrixChecks.Counter, Events("counter", 13f, 12f),
                Events("counter", 13f, 12f), Events("counter", 13f, 12f));
        }

        private IEnumerator RelayCases()
        {
            yield return ResetEvents("owner-admission");
            yield return ExecutePeers("owner-admission", "relay:owner", 11, "relay:owner", 22);
            yield return ObservePeers("owner-admission", MatrixChecks.None, Events("owner", 11f),
                Events("owner", 11f), Events("owner", 11f));

            _world.Relay("unknown").AuthorityMode = (NetworkAuthorityMode)999;
            yield return ResetEvents("unknown-authority-denied");
            yield return ExecutePeers("unknown-authority-denied", "relay:unknown", 31);
            yield return ObservePeers("unknown-authority-denied", MatrixChecks.None, Events("unknown"),
                Events("unknown"), Events("unknown"));
            // A separate component is the positive transport control. Unknown-policy rejection
            // does not promise preservation of that same sender's admission budget.
            yield return ExecutePeers("known-authority-positive-control", "relay:known", 32);
            yield return ObservePeers("known-authority-positive-control", MatrixChecks.None, Events("known", 32f),
                Events("known", 32f), Events("known", 32f));

            _world.Relay("local").isNetworked = false; // Clients retain true: a genuinely queued ingress request.
            double localStart = Time.unscaledTimeAsDouble;
            yield return ResetEvents("local-only-ingress-denied");
            yield return ExecutePeers("local-only-ingress-denied", "relay:local", 41);
            yield return ObservePeers("local-only-ingress-denied", MatrixChecks.None, Events("local"),
                Events("local"), Events("local"));
            _world.Relay("local").isNetworked = true;
            Require(Time.unscaledTimeAsDouble - localStart < 1000d, "Local-only budget observation window expired.");
            yield return ExecutePeers("local-only-budget-intact", "relay:local", 42);
            yield return ObservePeers("local-only-budget-intact", MatrixChecks.None, Events("local", 42f),
                Events("local", 42f), Events("local", 42f));

            yield return ResetEvents("per-peer-rate-limit");
            yield return ExecutePeers("per-peer-rate-limit", "relay-twice:rate", 51, "relay:rate", 52);
            MatrixEvents[] rate = Events("rate", 51f, 52f);
            rate[0].unordered = true;
            yield return ObservePeers("per-peer-rate-limit", MatrixChecks.None, rate, Events("rate"), Events("rate"));
            yield return ResetEvents("scope-all-clients");
            yield return ExecutePeers("scope-all-clients", "relay:all", 71);
            yield return ObservePeers("scope-all-clients", MatrixChecks.None, Events("all", 71f),
                Events("all", 71f), Events("all", 71f));
            yield return ResetEvents("scope-server-only");
            yield return ExecutePeers("scope-server-only", "relay:server", 72);
            yield return ObservePeers("scope-server-only", MatrixChecks.None, Events("server", 72f),
                Events("server"), Events("server"));
            yield return ResetEvents("scope-others-only");
            yield return ExecutePeers("scope-others-only", "relay:others", 73);
            yield return ObservePeers("scope-others-only", MatrixChecks.None, Events("others"),
                Events("others"), Events("others", 73f));
        }

        private IEnumerator SelectorCases()
        {
            _world.Selector.AuthorityMode = NetworkAuthorityMode.OwnerOnly;
            double deniedAt = Time.unscaledTimeAsDouble;
            yield return ResetEvents("selector-nonowner-denied");
            yield return ExecutePeers("selector-nonowner-denied", "none", 0, "selector-set", 1);
            CheckServer("selector-nonowner-denied", MatrixChecks.Selector, Events("selector"));
            // Set() optimistically changes B's local index; rejection has no public rollback acknowledgement.
            yield return ObservePeers("selector-nonowner-denied", MatrixChecks.Selector, Events("selector"),
                Events("selector"), null, bChecks: MatrixChecks.None);
            _world.Selector.SetExcludedIndices(new[] { 0, 1, 2 });
            _expectedState.excludedIndices = new[] { 0, 1, 2 };
            yield return ObservePeers("selector-eligible-snapshot", MatrixChecks.Selector, bChecks: MatrixChecks.None);
            yield return ResetEvents("selector-owner-additive");
            Require(Time.unscaledTimeAsDouble - deniedAt < 1000d, "Selector rejection/admission budget window expired.");
            yield return ExecutePeers("selector-owner-additive", "selector-random", 0);
            _expectedState.selector = 3;
            _expectedState.activeIndices = new[] { 0, 2, 3 };
            yield return ObservePeers("selector-owner-additive", MatrixChecks.Selector, Events("selector", 3f),
                Events("selector", 3f), Events("selector", 3f));
            _world.Selector.SetExcludedIndices(new[] { 1 });
            _expectedState.excludedIndices = new[] { 1 };
            yield return ObservePeers("selector-complete-snapshot", MatrixChecks.Selector);
        }

        private IEnumerator MoneyCases()
        {
            yield return ResetEvents("money-server-only-default");
            yield return ExecutePeers("money-server-only-default", "money-add", 99, "money-add", 99);
            yield return ObservePeers("money-server-only-default", MatrixChecks.Money, Events("money"),
                Events("money"), Events("money"));
            _world.Money.ClientMutationAuthority = (NetworkAuthorityMode)999;
            yield return ResetEvents("money-unknown-policy");
            yield return ExecutePeers("money-unknown-policy", "money-add", 99);
            yield return ObservePeers("money-unknown-policy", MatrixChecks.Money, Events("money"),
                Events("money"), Events("money"));
            _world.Money.ClientMutationAuthority = NetworkAuthorityMode.None;
            yield return ResetEvents("money-malformed-budget-intact");
            yield return MoneyBudgetBoundary();
            yield return ExecutePeers("money-malformed-budget-intact", "money-malformed-then-add", 1);
            _lastMoneyFence = Time.unscaledTimeAsDouble;
            _expectedState.money = 201f;
            yield return ObservePeers("money-malformed-budget-intact", MatrixChecks.Money, Events("money", 201f),
                Events("money", 201f), Events("money", 201f));
            yield return MoneyBudgetBoundary();
            yield return ResetEvents("money-cooperative-a");
            yield return ExecutePeers("money-cooperative-a", "money-add", 3);
            _lastMoneyFence = Time.unscaledTimeAsDouble;
            _expectedState.money = 204f;
            yield return ObservePeers("money-cooperative-a", MatrixChecks.Money, Events("money", 204f),
                Events("money", 204f), Events("money", 204f));
            yield return ResetEvents("money-cooperative-b");
            yield return ExecutePeers("money-cooperative-b", "none", 0, "money-add", 4);
            _lastMoneyFence = Time.unscaledTimeAsDouble;
            _expectedState.money = 208f;
            yield return ObservePeers("money-cooperative-b", MatrixChecks.Money, Events("money", 208f),
                Events("money", 208f), Events("money", 208f));
            yield return MoneyBudgetBoundary();
            _world.Money.ClientMutationAuthority = NetworkAuthorityMode.OwnerOnly;
            yield return ResetEvents("money-owner-only");
            yield return ExecutePeers("money-owner-only", "money-add", 5, "money-add", 7);
            _expectedState.money = 213f;
            yield return ObservePeers("money-owner-only", MatrixChecks.Money, Events("money", 213f),
                Events("money", 213f), Events("money", 213f));
        }

        private IEnumerator MoneyBudgetBoundary()
        {
            // This measures the documented 50ms admission window, not guessed delivery completion.
            yield return Until(() => Time.unscaledTimeAsDouble - _lastMoneyFence >= 0.051d,
                "Money next eligible unscaled budget boundary");
        }

        private IEnumerator PollingCase()
        {
            const string caseId = "polling-paused-null-retry";
            yield return ResetEvents(caseId);
            Time.timeScale = 0f;
            try
            {
                int before = _world.SyncTarget.ReadCount;
                int frame = Time.frameCount;
                double start = Time.realtimeSinceStartupAsDouble;
                yield return Until(() => Time.realtimeSinceStartupAsDouble - start >= 1d,
                    "one realtime second unchanged-property observation window");
                double elapsed = Time.realtimeSinceStartupAsDouble - start;
                int reads = _world.SyncTarget.ReadCount - before;
                Require(reads >= 1 && reads <= Math.Ceiling(elapsed / 0.2d) + 2,
                    "Unchanged property is not polled on its unscaled interval: " + reads);
                Require(_world.SyncTarget.Reactive == null, "Null reactive retry precondition was lost.");
                _artifacts.Write("polling-window", "seconds=" + elapsed.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                    + ";frames=" + (Time.frameCount - frame) + ";reads=" + reads + ";interval=0.2;timeScale=0",
                    caseId: caseId, state: _world.Capture(_playerA, _playerB));
                _world.SyncTarget.RawValue = 9f;
                _world.SyncTarget.Reactive = new ReactivePropertyFloat(15f);
                _expectedState.property = 9f;
                _expectedState.reactive = 15f;
                yield return ObservePeers(caseId, MatrixChecks.Property | MatrixChecks.Reactive);
            }
            finally { Time.timeScale = 1f; }
        }

        private IEnumerator PhysicsCase()
        {
            const string caseId = "physics-context-target";
            yield return ResetEvents(caseId);
            NetworkIdentity player = _peers["client-a"].Connection.identity;
            Require(player != null && player.netId != 0 && player.netId == _playerA && player.netId != _playerB,
                "Physics actor does not match client A's actual player identity.");
            Rigidbody body = player.GetComponent<Rigidbody>();
            Require(body != null && player.GetComponent<Collider>().enabled, "Server collider/rigidbody is absent.");
            body.position = _world.TriggerPosition;
            yield return Until(() => _world.HasPhysicsEvent(player.netId), "actual engine trigger carrying client A's netId");
            MatrixEvents[] events = Events("physics", _playerA);
            // AllClients applies on both current clients, not on a dedicated server.
            yield return ObservePeers(caseId, MatrixChecks.Context, events, events, events,
                serverBonusA: false, aBonusA: true, bBonusA: true);
        }

        private IEnumerator HandlerCase(string caseId, int value, string action)
        {
            yield return ResetEvents(caseId);
            if (action == "lease-first")
            {
                NetServerHandlers old = ServerLease(true);
                _serverLease = ServerLease(false);
                old.Dispose();
            }
            else if (action == "lease-opposite")
            {
                NetServerHandlers old = _serverLease;
                NetServerHandlers current = ServerLease(true);
                current.Dispose(); // Disposing the current generation must not resurrect old.
                _serverLease = ServerLease(false);
                old.Dispose(); // This stale generation must not remove the new one.
            }
            yield return ExecutePeers(caseId, action, value, action, value);
            MatrixEvents[] server = Events("handler-server", value, value);
            yield return ObservePeers(caseId, MatrixChecks.None, server,
                Events("handler-client", value), Events("handler-client", value));
        }

        private NetServerHandlers ServerLease(bool stale)
        {
            NetServerHandlers set = new NetServerHandlers();
            set.Add<Audit20261023TestPlayNetworkLeaseProbe>((connection, probe) =>
            {
                Require(!stale, "A replaced server handler was invoked.");
                Require(probe.Step == _step && connection.isReady && connection.identity != null,
                    "Handler probe preceded the real readiness/player barrier.");
                string peer = PeerFor(connection);
                Require(probe.Text == LeaseText(peer, probe.Step, probe.Value), "Server handler received altered Unicode payload.");
                _world.Record("handler-server", probe.Value);
                _world.LeasePayload = probe.Text;
                _artifacts.Write("handler-receiver", probe.Text, step: probe.Step, fromPeer: peer,
                    connectionId: connection.connectionId, epoch: _peers[peer].Epoch);
                connection.Send(new Audit20261023TestPlayNetworkLeaseReply
                    { Step = probe.Step, Value = probe.Value, Text = probe.Text });
            });
            set.RegisterNow();
            return set;
        }

        private NetClientHandlers ClientLease(bool stale)
        {
            NetClientHandlers set = new NetClientHandlers();
            set.Add<Audit20261023TestPlayNetworkLeaseReply>(reply =>
            {
                Require(!stale, "A replaced client handler was invoked.");
                Require(reply.Step == _step && reply.Text == LeaseText(_options.PeerId, reply.Step, reply.Value),
                    "Client handler received altered/stale payload.");
                Require(!_leaseReplies.ContainsKey(reply.Step), "Duplicate handler reply.");
                _leaseReplies.Add(reply.Step, reply);
                _world.Record("handler-client", reply.Value);
                _world.LeasePayload = reply.Text;
                _artifacts.Write("handler-receiver", reply.Text, step: reply.Step, epoch: _epoch);
            });
            set.RegisterNow();
            return set;
        }

        private string LeaseText(string peer, int step, int value) =>
            "Привет|日本語|é|🙂;" + _options.RunId + ";" + peer + ";" + step + ";" + value;

        private string PeerFor(NetworkConnectionToClient connection)
        {
            foreach (KeyValuePair<string, Peer> pair in _peers)
                if (ReferenceEquals(pair.Value.Connection, connection)) return pair.Key;
            throw new InvalidOperationException("Lease message came from an unadmitted connection.");
        }

        private IEnumerator Reconnect(bool restartServer)
        {
            string caseId = restartServer ? "server-restart" : "client-b-reconnect";
            BeginStep();
            string relative = Path.Combine("server", caseId + "-" + _step + ".json");
            int parkStep = _step;
            Send("client-b", "park", new MatrixInstruction { caseId = caseId, rendezvous = relative });
            if (restartServer)
                Send("client-a", "park", new MatrixInstruction { caseId = caseId, rendezvous = relative });
            yield return Until(() => restartServer ? _peers.Count == 0 : !_peers.ContainsKey("client-b"),
                "parked peers disconnected");
            if (restartServer)
            {
                _manager.StopNetwork();
                yield return Until(() => !NetworkServer.active && _world == null, "owned dedicated server stopped");
                _playerA = 0;
                _playerB = 0;
                _epoch++;
                yield return StartServerWorld();
            }
            string path = _options.UnderRoot(relative);
            string temporary = path + ".tmp";
            using (StreamWriter writer = new StreamWriter(new FileStream(temporary, FileMode.CreateNew,
                FileAccess.Write, FileShare.None), new System.Text.UTF8Encoding(false)))
                writer.Write(JsonUtility.ToJson(new MatrixRendezvous
                    { runId = _options.RunId, snapshotSha = _options.SnapshotSha, step = parkStep }));
            File.Move(temporary, path);
            yield return Until(() => _peers.Count == 2, "same independent client processes reconnected");
            _artifacts.Write("reconnected", "Rendezvous and Authenticated/Ready/PlayerSpawned barriers satisfied.",
                caseId: caseId, epoch: _epoch);
        }

        private IEnumerator Execute(MatrixInstruction instruction, int step)
        {
            Require(_world != null && _world.StartCompleted, "Action preceded initialized spawned world.");
            Require(ReferenceEquals(SaveProvider.CurrentProvider, _saveProvider), "Scenario replaced its isolated save provider.");
            MatrixReply response = new MatrixReply { detail = instruction.caseId };
            string action = instruction.action ?? "none";
            if (action == "counter-add")
            {
                response.counterAtInvocation = _world.Counter.ValueInt;
                _world.Counter.Add(instruction.value);
            }
            else if (action.StartsWith("relay:", StringComparison.Ordinal))
                _world.Relay(action.Substring(6)).TriggerFloat(instruction.value);
            else if (action.StartsWith("relay-twice:", StringComparison.Ordinal))
            {
                _world.Relay(action.Substring(12)).TriggerFloat(instruction.value);
                _world.Relay(action.Substring(12)).TriggerFloat(instruction.value);
            }
            else if (action == "selector-set") _world.Selector.Set(instruction.value);
            else if (action == "selector-random") _world.Selector.SetRandom(false);
            else if (action == "money-add") _world.Money.Add(instruction.value);
            else if (action.StartsWith("precision-", StringComparison.Ordinal))
                yield return ExecutePrecision(instruction, step, response);
            else if (action.StartsWith("counter-input-", StringComparison.Ordinal))
                yield return ExecuteCounterInput(instruction);
            else if (action == "money-malformed-then-add")
            {
                SendMoneyWire(7, 100f);
                SendMoneyWire(255, 100f);
                SendMoneyWire(0, float.NaN);
                SendMoneyWire(0, -1f);
                SendMoneyWire(1, -1f);
                SendMoneyWire(1, 100000f);
                _world.Money.Add(instruction.value);
            }
            else if (action.StartsWith("lease-", StringComparison.Ordinal))
            {
                if (action == "lease-first")
                {
                    NetClientHandlers old = ClientLease(true);
                    _clientLease = ClientLease(false);
                    old.Dispose();
                }
                else if (action == "lease-opposite")
                {
                    NetClientHandlers old = _clientLease;
                    NetClientHandlers current = ClientLease(true);
                    current.Dispose();
                    Require(!NetworkClient.UnregisterHandler<Audit20261023TestPlayNetworkLeaseReply>(),
                        "Disposing the newest client lease implicitly restored an old handler.");
                    _artifacts.Write("handler-empty", "Current lease disposed; no previous handler restored.", step: step, epoch: _epoch);
                    _clientLease = ClientLease(false);
                    old.Dispose();
                }
                else Require(action == "lease-current" && _clientLease != null, "Live lease is absent after restart.");
                NetworkClient.Send(new Audit20261023TestPlayNetworkLeaseProbe
                    { Step = step, Value = instruction.value, Text = LeaseText(_options.PeerId, step, instruction.value) });
                yield return Until(() => _leaseReplies.ContainsKey(step), "actual replacement-handler response");
                _leaseReplies.Remove(step);
            }
            else Require(action == "none", "Unknown scenario action: " + action);
            // Default Mirror Commands and this signal share Channels.Reliable. Receiving this ACK
            // is a server-side ingress fence, not a claim that component RPCs reached a client.
            response.state = _world.Capture(_playerA, _playerB);
            Reply("ack", step, response);
        }

        private void SendMoneyWire(byte operation, float amount)
        {
            // Adversarial payload traverses the real transport and Weaver dispatch; no private
            // method invocation. Signature matches installed Mirror's Cecil FullName convention.
            const string signature = "System.Void Neo.Shop.Money::CmdMoneyOp(Neo.Shop.Money/MoneyOp,System.Single,Mirror.NetworkConnectionToClient)";
            using (NetworkWriterPooled writer = NetworkWriterPool.Get())
            {
                writer.WriteByte(operation);
                writer.WriteFloat(amount);
                NetworkClient.Send(new CommandMessage
                {
                    netId = _world.Money.netId,
                    componentIndex = _world.Money.ComponentIndex,
                    functionHash = signature.GetStableHashCode16(),
                    payload = writer.ToArraySegment()
                });
            }
        }

        private void BeginStep()
        {
            Require(ReferenceEquals(SaveProvider.CurrentProvider, _saveProvider), "Server save provider isolation was lost.");
            _step++;
            _responses.Clear();
        }

        private string[] PeerIds() => _peers.ContainsKey("client-b")
            ? new[] { "client-a", "client-b" } : new[] { "client-a" };

        private IEnumerator ResetEvents(string caseId)
        {
            BeginStep();
            _world.ClearEvents();
            foreach (string peer in PeerIds()) Send(peer, "reset", new MatrixInstruction { caseId = caseId });
            yield return Responses();
        }

        private IEnumerator ExecutePeers(string caseId, string actionA, int valueA,
            string actionB = "none", int valueB = 0)
        {
            BeginStep();
            Send("client-a", "execute", new MatrixInstruction { caseId = caseId, action = actionA, value = valueA });
            Send("client-b", "execute", new MatrixInstruction { caseId = caseId, action = actionB, value = valueB });
            yield return Responses();
        }

        private MatrixReply Response(string peer) => _responses[_step + ":" + peer];

        private IEnumerator Responses(string onlyPeer = null)
        {
            yield return Until(() => onlyPeer != null ? _responses.ContainsKey(_step + ":" + onlyPeer)
                : _responses.Count == _peers.Count, "same-channel control/receiver acknowledgements");
        }

        private MatrixExpected Expected(MatrixChecks checks, MatrixEvents[] events, bool owned = false,
            bool contextBonusA = false)
        {
            MatrixState wanted = JsonUtility.FromJson<MatrixState>(JsonUtility.ToJson(_expectedState));
            wanted.isOwned = owned;
            // Context child state is role-specific, unlike the shared replicated snapshot.
            wanted.bonusA = contextBonusA;
            wanted.bonusB = false;
            return new MatrixExpected { checks = checks, state = wanted, events = events ?? Array.Empty<MatrixEvents>() };
        }

        private void CheckServer(string caseId, MatrixChecks checks, MatrixEvents[] events = null,
            bool contextBonusA = false)
        {
            MatrixState actual = _world.Capture(_playerA, _playerB);
            string mismatch = Expected(checks, events, contextBonusA: contextBonusA).Mismatch(actual);
            Require(mismatch == null, caseId + ": server " + mismatch);
            _artifacts.Write("server-receiver", "Authoritative state/event payloads checked", step: _step,
                caseId: caseId, state: actual, epoch: _epoch);
        }

        private IEnumerator ObservePeers(string caseId, MatrixChecks checks, MatrixEvents[] serverEvents = null,
            MatrixEvents[] aEvents = null, MatrixEvents[] bEvents = null,
            string onlyPeer = null, MatrixChecks? bChecks = null,
            bool serverBonusA = false, bool aBonusA = false, bool bBonusA = false)
        {
            CheckServer(caseId, checks, serverEvents, serverBonusA);
            BeginStep();
            if (onlyPeer == null || onlyPeer == "client-a")
                Send("client-a", "observe", new MatrixInstruction
                {
                    caseId = caseId, expected = Expected(checks | MatrixChecks.Ownership, aEvents, true, aBonusA)
                });
            if (onlyPeer == null || onlyPeer == "client-b")
                Send("client-b", "observe", new MatrixInstruction
                {
                    caseId = caseId, expected = Expected((bChecks ?? checks) | MatrixChecks.Ownership, bEvents,
                        contextBonusA: bBonusA)
                });
            yield return Responses(onlyPeer);
            // The server independently checks the actual received snapshots as well as client assertions.
            foreach (string peer in onlyPeer == null ? PeerIds() : new[] { onlyPeer })
            {
                MatrixExpected expected = Expected((peer == "client-b" ? bChecks ?? checks : checks)
                    | MatrixChecks.Ownership, peer == "client-a" ? aEvents : bEvents, peer == "client-a",
                    peer == "client-a" ? aBonusA : bBonusA);
                Require(expected.Mismatch(Response(peer).state) == null, "Server received an incorrect client observation.");
            }
            _artifacts.Write("case-pass", "Server and all addressed independent receivers satisfied the case.",
                step: _step, caseId: caseId, epoch: _epoch);
        }

        private static MatrixEvents[] Events(string name, params float[] values) =>
            new[] { new MatrixEvents { name = name, values = values } };
    }
}
#endif
