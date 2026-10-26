#if MIRROR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mirror;
using Neo.Network;
using Neo.Network.Realtime;
using Neo.Save;
using UnityEngine;
using UnityEngine.Scripting;

namespace Neo.Tests.Network.Integration
{
    public sealed class Audit20261023TestPlayNetworkManager : NeoNetworkManager
    {
        internal Audit20261023TestPlayNetworkPeerDriver Driver;
        public override void OnValidate() { }

        public override void OnServerAddPlayer(NetworkConnectionToClient connection)
        {
            GameObject player = Audit20261023TestPlayNetworkWorld.CreatePlayer(Driver,
                new Vector3(-20f - connection.connectionId * 4f, 0f, 0f));
            if (!NetworkServer.AddPlayerForConnection(connection, player,
                Audit20261023TestPlayNetworkWorld.PlayerAssetId))
                Driver.Fault("The real remote connection did not receive its player.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Driver.RegisterSpawnFactories();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            base.OnServerDisconnect(connection);
            Driver.PeerDisconnected(connection);
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Driver.ClientDisconnected();
        }
    }

    [Serializable]
    internal sealed class MatrixRendezvous
    {
        public string runId;
        public string snapshotSha;
        public int step;
    }

    [Preserve]
    public sealed partial class Audit20261023TestPlayNetworkPeerDriver : MonoBehaviour
    {
        private sealed class Peer
        {
            public NetworkConnectionToClient Connection;
            public int ProcessId;
            public int Epoch;
        }

        private MatrixOptions _options;
        private MatrixArtifacts _artifacts;
        private MatrixEnvironment _environment;
        private FileSaveProvider _saveProvider;
        private Audit20261023TestPlayNetworkManager _manager;
        private Audit20261023TestPlayNetworkWorld _world;
        private NetServerHandlers _serverSignals;
        private NetClientHandlers _clientSignals;
        private NetServerHandlers _serverLease;
        private NetClientHandlers _clientLease;
        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        private readonly List<string> _cleanupLogErrors = new List<string>();
        private readonly Dictionary<string, Peer> _peers = new Dictionary<string, Peer>();
        private readonly Dictionary<string, int> _knownPids = new Dictionary<string, int>();
        private readonly Dictionary<string, MatrixReply> _responses = new Dictionary<string, MatrixReply>();
        private readonly Dictionary<int, Audit20261023TestPlayNetworkLeaseReply> _leaseReplies =
            new Dictionary<int, Audit20261023TestPlayNetworkLeaseReply>();
        private string _fault;
        private bool _finishing;
        private bool _exitRequested;
        private bool _parkRequested;
        private bool _expectedDisconnect;
        private bool _ownsNetwork;
        private string _rendezvous;
        private int _parkStep;
        private int _epoch;
        private int _step;
        private int _lastClientStep;
        private double _overallDeadline;
        private float _savedTimeScale;
        private int _savedFrameRate;
        private int _savedVSync;
        private bool _savedBackground;
        private uint _playerA;
        private uint _playerB;

        internal bool IsServer => _options.IsServer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (!MatrixOptions.Requested(arguments)) return;
            try
            {
                MatrixOptions options = MatrixOptions.Parse(arguments);
                options.VerifyPlayerIdentity();
                GameObject root = new GameObject("Audit20261023NetworkMatrix");
                DontDestroyOnLoad(root);
                Audit20261023TestPlayNetworkPeerDriver driver = root.AddComponent<Audit20261023TestPlayNetworkPeerDriver>();
                driver.Initialize(options);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError("Network matrix bootstrap refused: " + exception);
                Application.Quit(2);
            }
        }

        private void Initialize(MatrixOptions options)
        {
            _options = options;
            _artifacts = new MatrixArtifacts(options);
            _savedTimeScale = Time.timeScale;
            _savedFrameRate = Application.targetFrameRate;
            _savedVSync = QualitySettings.vSyncCount;
            _savedBackground = Application.runInBackground;
            Application.logMessageReceived += OnLog;
            _overallDeadline = Time.realtimeSinceStartupAsDouble + options.OverallTimeout;
            _environment = new MatrixEnvironment
            {
                company = Application.companyName,
                product = Application.productName,
                persistentDataPath = Application.persistentDataPath,
                playerPrefsNamespace = @"HKEY_CURRENT_USER\Software\" + Application.companyName + "\\" + Application.productName,
                saveRoot = options.SaveRoot,
                saveFile = Path.Combine(options.SaveRoot, "matrix-save.json"),
                saveProvider = typeof(FileSaveProvider).FullName,
                platform = Application.platform.ToString(),
                unityVersion = Application.unityVersion,
                applicationVersion = Application.version,
                mirrorAssemblyVersion = typeof(NetworkIdentity).Assembly.GetName().Version?.ToString(),
                transport = typeof(TelepathyTransport).FullName,
                serverPort = options.Port,
                processId = Process.GetCurrentProcess().Id
            };
            // This record precedes the first provider construction/load and every save-mutating scenario.
            _artifacts.Write("environment", "Identity verified; scenario stores will use the declared task-owned file root.",
                environment: _environment);
            _saveProvider = new FileSaveProvider("matrix-save.json",
                new FileSaveProviderOptions { PersistenceRoot = options.SaveRoot });
            SaveProvider.SetProvider(_saveProvider);
            string sentinel = "matrix-owned-" + options.RunId;
            SaveProvider.SetString(sentinel, options.PeerId);
            SaveProvider.Save();
            FileSaveProvider reload = new FileSaveProvider("matrix-save.json",
                new FileSaveProviderOptions { PersistenceRoot = options.SaveRoot });
            Require(reload.GetString(sentinel) == options.PeerId, "Task-owned file provider sentinel did not round-trip.");
            SaveProvider.DeleteKey(sentinel);
            SaveProvider.Save();
            _artifacts.Write("store-isolated", "Sentinel round-trip used FileSaveProvider.PersistenceRoot; no PlayerPrefs API is used.",
                environment: _environment);
        }

        private IEnumerator Start()
        {
            Require(!NetworkServer.active && !NetworkClient.active && NetworkManager.singleton == null,
                "A foreign network session/manager exists; no takeover is allowed.");
            Require(UnityEngine.Object.FindObjectsByType<NetworkIdentity>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Length == 0, "Use an empty isolated bootstrap scene.");
            Require(Transport.active == null, "A foreign active transport exists.");
            _ownsNetwork = true;
            Time.timeScale = 1f;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            GameObject managerObject = Own(new GameObject("MatrixManager"));
            managerObject.SetActive(false);
            TelepathyTransport transport = managerObject.AddComponent<TelepathyTransport>();
            transport.Port = _options.Port;
            transport.serverMaxMessageSize = 64 * 1024;
            transport.clientMaxMessageSize = 64 * 1024;
            _manager = managerObject.AddComponent<Audit20261023TestPlayNetworkManager>();
            _manager.Driver = this;
            _manager.transport = transport;
            _manager.autoCreatePlayer = false;
            _manager.HandshakeMode = NeoHandshakeMode.Always;
            _manager.maxConnections = 2;
            _manager.networkAddress = "127.0.0.1";
            Transport.active = transport;
            managerObject.SetActive(true);
            if (IsServer)
            {
                _serverSignals = new NetServerHandlers();
                _serverSignals.Add<Audit20261023TestPlayNetworkSignal>(OnServerSignal);
                _serverSignals.RegisterNow();
                yield return RunServer();
            }
            else
            {
                _clientSignals = new NetClientHandlers();
                _clientSignals.Add<Audit20261023TestPlayNetworkSignal>(OnClientSignal);
                _clientSignals.RegisterNow();
                yield return RunClient();
            }
        }

        private void Update()
        {
            if (_options == null || _finishing) return;
            if (_fault == null && Time.realtimeSinceStartupAsDouble >= _overallDeadline)
                Fault("Overall realtime deadline expired.");
            if (_fault != null)
            {
                StopAllCoroutines();
                StartCoroutine(Finish(false));
            }
        }

        internal T Own<T>(T value) where T : UnityEngine.Object
        {
            _owned.Add(value);
            return value;
        }

        internal void Fault(string message)
        {
            if (_fault != null) return;
            _fault = message;
            _artifacts?.Write("failure", message, false, step: _step, epoch: _epoch);
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (_finishing)
            {
                if (_cleanupLogErrors.Count < 64) _cleanupLogErrors.Add(message + "\n" + stack);
            }
            else Fault(message + "\n" + stack);
        }

        internal void RegisterSpawnFactories()
        {
            NetworkClient.RegisterSpawnHandler(Audit20261023TestPlayNetworkWorld.WorldAssetId,
                (SpawnMessage message) =>
                {
                    Require(_world == null, "Duplicate world spawn.");
                    _world = Audit20261023TestPlayNetworkWorld.Create(this);
                    _world.transform.position = message.position;
                    return _world.gameObject;
                }, (GameObject value) => UnityEngine.Object.Destroy(value));
            NetworkClient.RegisterSpawnHandler(Audit20261023TestPlayNetworkWorld.PlayerAssetId,
                (SpawnMessage message) => Audit20261023TestPlayNetworkWorld.CreatePlayer(this, message.position),
                (GameObject value) => UnityEngine.Object.Destroy(value));
            NetworkClient.RegisterSpawnHandler(PrecisionWalletAssetId,
                (SpawnMessage message) => CreatePrecisionWallet(),
                (GameObject value) => UnityEngine.Object.Destroy(value));
            RegisterCounterInputFactories();
        }

        internal void PeerDisconnected(NetworkConnectionToClient connection)
        {
            string removed = null;
            foreach (KeyValuePair<string, Peer> pair in _peers)
                if (ReferenceEquals(pair.Value.Connection, connection)) removed = pair.Key;
            if (removed != null)
            {
                _peers.Remove(removed);
                _artifacts.Write("disconnected", fromPeer: removed, connectionId: connection.connectionId);
            }
        }

        internal void ClientDisconnected()
        {
            _artifacts?.Write("disconnected", epoch: _epoch);
            if (!_expectedDisconnect && !_finishing) Fault("Unexpected production-transport disconnect.");
        }

        private void OnServerSignal(NetworkConnectionToClient connection, Audit20261023TestPlayNetworkSignal message)
        {
            Require(message.RunId == _options.RunId && message.SnapshotSha == _options.SnapshotSha,
                "Foreign run/snapshot on the task-owned port.");
            Require(message.PeerId == "client-a" || message.PeerId == "client-b", "Unknown peer ID.");
            if (message.Kind == "hello")
            {
                Require(connection.isAuthenticated && connection.isReady && connection.identity != null,
                    "Hello preceded Authenticated/Ready/PlayerSpawned.");
                Require(!_peers.ContainsKey(message.PeerId), "A duplicate independent client claimed the same peer ID.");
                MatrixReply hello = JsonUtility.FromJson<MatrixReply>(message.Json);
                Require(hello != null && hello.environment != null, "Missing peer persistence/identity probe.");
                MatrixEnvironment environment = hello.environment;
                Require(environment.company == _options.Company && environment.product.Contains(message.PeerId)
                    && environment.serverPort == _options.Port && environment.transport == typeof(TelepathyTransport).FullName
                    && MatrixOptions.SamePath(environment.saveRoot,
                        _options.UnderRoot(Path.Combine(message.PeerId, "save"))), "Peer environment differs from the declared run.");
                Require(environment.processId != _environment.processId, "A client is sharing the server OS process.");
                foreach (KeyValuePair<string, int> pair in _knownPids)
                    if (pair.Key != message.PeerId) Require(pair.Value != environment.processId, "Both clients share an OS process.");
                if (_knownPids.TryGetValue(message.PeerId, out int originalPid))
                    Require(originalPid == environment.processId && message.Epoch > 0, "Unexpected peer replacement.");
                _knownPids[message.PeerId] = environment.processId;
                _peers.Add(message.PeerId, new Peer
                    { Connection = connection, ProcessId = environment.processId, Epoch = message.Epoch });
                if (message.PeerId == "client-a")
                {
                    _playerA = connection.identity.netId;
                    Require(_world != null, "Server world is absent.");
                    Require(_world.netIdentity.AssignClientAuthority(connection), "World ownership was not assigned to A.");
                }
                else _playerB = connection.identity.netId;
                _artifacts.Write("peer-ready", fromPeer: message.PeerId, connectionId: connection.connectionId,
                    environment: environment, epoch: message.Epoch);
                return;
            }
            Require(_peers.TryGetValue(message.PeerId, out Peer peer)
                && ReferenceEquals(peer.Connection, connection) && peer.Epoch == message.Epoch,
                "Reply does not belong to the admitted peer/epoch.");
            if (message.Kind == "failed")
            {
                Fault("Remote " + message.PeerId + ": " + message.Json);
                return;
            }
            Require(message.Step == _step, "Reply belongs to a different phase.");
            Require(message.Kind == "ack" || message.Kind == "observed", "Unknown peer signal kind.");
            string key = message.Step + ":" + message.PeerId;
            Require(!_responses.ContainsKey(key), "Duplicate phase receiver/acknowledgement.");
            MatrixReply reply = JsonUtility.FromJson<MatrixReply>(message.Json);
            Require(reply != null, "Missing receiver payload.");
            _responses.Add(key, reply);
            if (reply.spendRequests != null && reply.spendRequests.Length != 0)
                _artifacts.Write("remote-request-status", "Client return status; not a server spend acknowledgement.",
                    step: message.Step, caseId: reply.detail, state: reply.state, fromPeer: message.PeerId,
                    connectionId: connection.connectionId, epoch: message.Epoch, spendRequests: reply.spendRequests);
            _artifacts.Write(message.Kind == "observed" ? "remote-receiver" : "remote-ack", reply.detail,
                step: message.Step, caseId: message.Kind == "observed" ? reply.detail : null,
                state: reply.state, fromPeer: message.PeerId,
                connectionId: connection.connectionId, epoch: message.Epoch);
        }

        private void OnClientSignal(Audit20261023TestPlayNetworkSignal message)
        {
            Require(message.RunId == _options.RunId && message.SnapshotSha == _options.SnapshotSha
                && message.PeerId == _options.PeerId && message.Epoch == _epoch, "Control correlation/epoch mismatch.");
            Require(message.Step > _lastClientStep, "Duplicate/out-of-order control message.");
            _lastClientStep = message.Step;
            _step = message.Step;
            MatrixInstruction instruction = JsonUtility.FromJson<MatrixInstruction>(message.Json);
            Require(instruction != null, "Missing phase instruction.");
            _playerA = instruction.playerA;
            _playerB = instruction.playerB;
            if (message.Kind == "reset")
            {
                Require(_world != null && _world.StartCompleted, "Reset preceded world initialization.");
                _world.ClearEvents();
                Reply("ack", message.Step, new MatrixReply { detail = "event baseline reset" });
            }
            else if (message.Kind == "execute") StartCoroutine(Execute(instruction, message.Step));
            else if (message.Kind == "observe") StartCoroutine(Observe(instruction, message.Step));
            else if (message.Kind == "park")
            {
                _expectedDisconnect = true;
                _parkRequested = true;
                _parkStep = message.Step;
                _rendezvous = _options.UnderRoot(instruction.rendezvous);
                _artifacts.Write("park-requested", step: message.Step, caseId: instruction.caseId, epoch: _epoch);
            }
            else if (message.Kind == "finish")
                Reply("ack", message.Step, new MatrixReply { detail = "all receiver checks completed" });
            else if (message.Kind == "exit")
            {
                _expectedDisconnect = true;
                _exitRequested = true;
            }
            else throw new InvalidOperationException("Unknown matrix control: " + message.Kind);
        }

        private IEnumerator RunClient()
        {
            while (!_exitRequested)
            {
                _expectedDisconnect = false;
                _manager.StartAsClient("127.0.0.1", _options.Port);
                yield return Until(() => NetworkClient.isConnected, "Connected");
                _artifacts.Write("barrier", "Connected", epoch: _epoch);
                yield return Until(() => NetworkClient.connection != null && NetworkClient.connection.isAuthenticated,
                    "Authenticated");
                _artifacts.Write("barrier", "Authenticated", epoch: _epoch);
                yield return Until(() => NetworkClient.ready, "Ready");
                _artifacts.Write("barrier", "Ready", epoch: _epoch);
                yield return Until(() => NetworkClient.localPlayer != null, "PlayerSpawned");
                _artifacts.Write("barrier", "PlayerSpawned", epoch: _epoch);
                Require(!NetworkServer.active && !(NetworkClient.connection is LocalConnectionToServer),
                    "A real pure client is required.");
                Reply("hello", 0, new MatrixReply { environment = _environment });
                yield return Until(() => _parkRequested || _exitRequested, "server phases", _options.OverallTimeout);
                if (_parkRequested)
                {
                    _manager.StopNetwork();
                    yield return Until(() => !NetworkClient.active && _world == null, "owned client stop before reconnect");
                    yield return Until(RendezvousReady, "server reconnect rendezvous");
                    _parkRequested = false;
                    _epoch++;
                }
            }
            yield return Finish(true);
        }

        private bool RendezvousReady()
        {
            if (!File.Exists(_rendezvous)) return false;
            MatrixRendezvous marker = JsonUtility.FromJson<MatrixRendezvous>(File.ReadAllText(_rendezvous));
            return marker != null && marker.runId == _options.RunId && marker.snapshotSha == _options.SnapshotSha
                && marker.step == _parkStep;
        }

        private IEnumerator Observe(MatrixInstruction instruction, int step)
        {
            MatrixState state = null;
            string mismatch = "world not spawned";
            yield return Until(() =>
            {
                if (_world == null) return false;
                state = _world.Capture(_playerA, _playerB);
                mismatch = instruction.expected.Mismatch(state);
                return mismatch == null;
            }, instruction.caseId + ": receiver state/events");
            Require(mismatch == null, mismatch);
            _artifacts.Write("receiver", "Observed after reliable control barrier", step: step,
                caseId: instruction.caseId, state: state, epoch: _epoch);
            Reply("observed", step, new MatrixReply { state = state, detail = instruction.caseId });
        }

        private void Reply(string kind, int step, MatrixReply reply)
        {
            NetworkClient.Send(new Audit20261023TestPlayNetworkSignal
            {
                RunId = _options.RunId, SnapshotSha = _options.SnapshotSha, PeerId = _options.PeerId,
                Kind = kind, Epoch = _epoch, Step = step, Json = JsonUtility.ToJson(reply)
            });
        }

        private void Send(string peerId, string kind, MatrixInstruction instruction)
        {
            Peer peer = _peers[peerId];
            instruction.playerA = _playerA;
            instruction.playerB = _playerB;
            peer.Connection.Send(new Audit20261023TestPlayNetworkSignal
            {
                RunId = _options.RunId, SnapshotSha = _options.SnapshotSha, PeerId = peerId,
                Kind = kind, Epoch = peer.Epoch, Step = _step, Json = JsonUtility.ToJson(instruction)
            });
        }

        private IEnumerator Until(Func<bool> predicate, string phase, double timeout = -1)
        {
            double end = Time.realtimeSinceStartupAsDouble + (timeout < 0 ? _options.PhaseTimeout : timeout);
            while (!predicate())
            {
                if (_fault != null) throw new InvalidOperationException(_fault);
                if (Time.realtimeSinceStartupAsDouble >= end)
                    throw new TimeoutException(phase + ": server=" + NetworkServer.active + ", client="
                        + NetworkClient.active + ", connected=" + NetworkClient.isConnected + ", ready=" + NetworkClient.ready);
                yield return null;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private IEnumerator Finish(bool requestedSuccess)
        {
            if (_finishing) yield break;
            _finishing = true;
            _expectedDisconnect = true;
            List<string> cleanup = new List<string>();
            try
            {
                _serverLease?.Dispose();
                _clientLease?.Dispose();
                _serverSignals?.Dispose();
                _clientSignals?.Dispose();
                if (_ownsNetwork)
                {
                    if (_manager != null) _manager.StopNetwork();
                    NetworkServer.Shutdown();
                    NetworkClient.Shutdown();
                    NetworkClient.ClearSpawners();
                    Transport.active = null;
                }
            }
            catch (Exception exception) { cleanup.Add(exception.ToString()); }
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) UnityEngine.Object.Destroy(_owned[i]);
            yield return null;
            try
            {
                foreach (UnityEngine.Object value in _owned)
                    if (value != null) cleanup.Add("Owned Unity object survived destruction: " + value.name);
                if (_ownsNetwork && (NetworkServer.active || NetworkClient.active))
                    cleanup.Add("Owned transport is still active.");
                SaveProvider.Save();
            }
            catch (Exception exception) { cleanup.Add(exception.ToString()); }
            Time.timeScale = _savedTimeScale;
            Application.targetFrameRate = _savedFrameRate;
            QualitySettings.vSyncCount = _savedVSync;
            Application.runInBackground = _savedBackground;
            Application.logMessageReceived -= OnLog;
            cleanup.AddRange(_cleanupLogErrors);
            bool success = requestedSuccess && _fault == null && cleanup.Count == 0;
            _artifacts.Write("cleanup", cleanup.Count == 0 ? "Owned objects/transport released; task-owned persistence retained." : string.Join("\n", cleanup),
                cleanup.Count == 0, epoch: _epoch);
            _artifacts.Write("result", _fault ?? (success ? "all assigned matrix cases received and checked" : "cleanup failed"),
                success, epoch: _epoch);
            _artifacts.Dispose();
            _artifacts = null;
            Application.Quit(success ? 0 : 2);
        }
    }
}
#endif
