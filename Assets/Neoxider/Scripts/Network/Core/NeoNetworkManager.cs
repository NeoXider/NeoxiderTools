#if MIRROR
using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Network
{
    /// <summary>
    ///     Neoxider wrapper around Mirror's <c>NetworkManager</c>.
    ///     Provides Unity events for connection lifecycle and integrates with
    ///     Neo systems (scene flow, spawner, etc.).
    ///     <para>
    ///         On top of Mirror it guarantees the client handshake (<c>Ready</c>, then <c>AddPlayer</c>, see
    ///         <see cref="NeoHandshakeMode"/>), wakes scene <c>NetworkIdentity</c> objects on every peer when a session
    ///         starts, and reports per-connection server events (connect, ready, player ready, disconnect) and the
    ///         local player spawn, so game code never has to poll Mirror's connection table.
    ///     </para>
    ///     <para>Start / stop: <see cref="StartAsHost"/>, <see cref="StartAsClient()"/>, <see cref="StartAsServer"/>,
    ///     <see cref="StopNetwork"/>.</para>
    ///     <para>Without Mirror installed this component does nothing — it compiles
    ///     as an empty <see cref="MonoBehaviour"/> stub.</para>
    /// </summary>
    [NeoDoc("Network/NeoNetworkManager.md")]
    [CreateFromMenu("Neoxider/Network/NeoNetworkManager")]
    [AddComponentMenu("Neoxider/Network/" + nameof(NeoNetworkManager))]
    public class NeoNetworkManager :
#if MIRROR
        NetworkManager
#else
        MonoBehaviour
#endif
    {
        [Header("Neo Events")]
        [Tooltip("Triggered when the server is started locally (Host or Dedicated server).")]
        [SerializeField]
        private UnityEvent _onServerStarted = new();

        [Tooltip("Triggered when the server is stopped.")] [SerializeField]
        private UnityEvent _onServerStopped = new();

        [Tooltip("Triggered when the client successfully connects to the server.")] [SerializeField]
        private UnityEvent _onClientConnected = new();

        [Tooltip("Triggered when the client disconnects from the server.")] [SerializeField]
        private UnityEvent _onClientDisconnected = new();

        [Tooltip("Triggered on the client when its own player object has spawned (and again after every respawn). " +
                 "Raised at the end of the frame in which the spawn message was processed.")]
        [SerializeField]
        private UnityEvent _onLocalPlayerSpawned = new();

        [Header("Diagnostics")] [SerializeField]
        private bool _debugLifecycleLog;

        [Tooltip("Enable gated runtime info logs from Neo network components (NetworkDiagnostics).")]
        [SerializeField]
        private bool _enableRuntimeNetworkLogs;

        [Tooltip("Enable gated runtime warnings from Neo network components (NetworkDiagnostics).")]
        [SerializeField]
        private bool _enableRuntimeNetworkWarnings;

#if MIRROR
        [Header("Handshake")]
        [Tooltip("Auto: Ready always, AddPlayer when Auto Create Player or a scene template is used (once per connection, " +
                 "also after scene changes). Always: Ready + AddPlayer even with Auto Create Player off (custom OnServerAddPlayer). " +
                 "Manual: Mirror's stock behavior only.")]
        [SerializeField]
        private NeoHandshakeMode _handshakeMode = NeoHandshakeMode.Auto;

        [Header("Transport Queue Limits")]
        [Tooltip("Optional cap for the root Telepathy transport's send/receive queues. 0 keeps authored limits; " +
                 "256 is recommended for a small reliable message stream. Applies before session start.")]
        [SerializeField] [Min(0)] private int _telepathyQueueLimit;

        [Header("Scene Objects")]
        [Tooltip("When a client, host or server session starts, activate every scene object that carries a NetworkIdentity. " +
                 "Mirror's scene post-process disables them and only the server wakes them, so a client's scene objects " +
                 "(and the message handlers they register in Awake) would otherwise not exist when the first message arrives.")]
        [SerializeField]
        private bool _activateSceneObjectsOnStart = true;

        [Header("Server Connection Events")]
        [Tooltip("Server: a client connected (the host's own loopback connection included). Argument: the connection.")]
        [SerializeField]
        private UnityEvent<NetworkConnectionToClient> _onServerClientConnected = new();

        [Tooltip("Server: a client sent Ready, so the server now sends it spawned objects.")]
        [SerializeField]
        private UnityEvent<NetworkConnectionToClient> _onServerClientReady = new();

        [Tooltip("Server: a connection is ready AND owns a player object, the moment broadcasts can safely reach it. " +
                 "Fires once per connection.")]
        [SerializeField]
        private UnityEvent<NetworkConnectionToClient> _onServerPlayerReady = new();

        [Tooltip("Server: a client disconnected. Raised before Mirror destroys its player, so connection.identity is still readable.")]
        [SerializeField]
        private UnityEvent<NetworkConnectionToClient> _onServerClientDisconnected = new();

        [Header("Scene Player Template")]
        [Tooltip("Use a player object configured in the scene as the NoCode template instead of a prefab asset.")]
        [SerializeField]
        private bool _useScenePlayerTemplate;

        [Tooltip("Disabled scene object that contains NetworkIdentity and all NoCode references for the player.")]
        [SerializeField]
        private GameObject _scenePlayerTemplate;

        [Tooltip("Disable the scene template at runtime so only spawned network copies are active.")] [SerializeField]
        private bool _disableScenePlayerTemplate = true;

        [SerializeField] [HideInInspector] private string _scenePlayerTemplateSpawnId;

        private bool _addPlayerRequested;
        private bool _sceneActivationSubscribed;
        private NetworkIdentity _reportedLocalPlayer;
        private readonly List<NetworkConnectionToClient> _pendingPlayerConnections = new();
        private readonly HashSet<int> _playerReadyReported = new();
        private uint _scenePlayerTemplateAssetId;
        private uint _registeredScenePlayerTemplateAssetId;
        private bool _scenePlayerTemplateSpawnHandlerRegistered;
        private bool _hasSpawnedFieldMissingLogged;
        private static readonly FieldInfo NetworkIdentityHasSpawnedField =
            typeof(NetworkIdentity).GetField("hasSpawned", BindingFlags.NonPublic | BindingFlags.Instance);
#endif

        /// <summary>Raised on the server when it starts listening.</summary>
        public UnityEvent OnServerStartedEvent => _onServerStarted;

        /// <summary>Raised on the server when it shuts down.</summary>
        public UnityEvent OnServerStoppedEvent => _onServerStopped;

        /// <summary>Raised on the client when it connects to a server.</summary>
        public UnityEvent OnClientConnectedEvent => _onClientConnected;

        /// <summary>Raised on the client when it disconnects from the server.</summary>
        public UnityEvent OnClientDisconnectedEvent => _onClientDisconnected;

        /// <summary>
        ///     Raised on the client when its own player object has spawned, and again after every respawn.
        ///     Fires at the end of the frame in which the spawn message was processed.
        /// </summary>
        public UnityEvent OnLocalPlayerSpawnedEvent => _onLocalPlayerSpawned;

        // WHY: NoCode debugging - inspector checkboxes flip the global gated-log flags at startup.
        private void ApplyDiagnosticsToggles()
        {
            if (_enableRuntimeNetworkLogs)
            {
                NetworkDiagnostics.RuntimeLogsEnabled = true;
            }

            if (_enableRuntimeNetworkWarnings)
            {
                NetworkDiagnostics.RuntimeWarningsEnabled = true;
            }
        }

#if MIRROR
        /// <summary>
        ///     Whether this instance is running as a server (host or dedicated).
        /// </summary>
        public bool IsServer => NetworkServer.active;

        /// <summary>
        ///     Whether this instance is running as a client.
        /// </summary>
        public bool IsClient => NetworkClient.active;

        /// <summary>
        ///     Whether this instance is a host (server + client).
        /// </summary>
        public bool IsHost => NetworkServer.active && NetworkClient.active;

        /// <summary>
        ///     Use a scene-authored player object as a template for spawned player copies.
        /// </summary>
        public bool UseScenePlayerTemplate
        {
            get => _useScenePlayerTemplate;
            set => _useScenePlayerTemplate = value;
        }

        /// <summary>
        ///     Scene object used as the player template when <see cref="UseScenePlayerTemplate"/> is enabled.
        /// </summary>
        public GameObject ScenePlayerTemplate
        {
            get => _scenePlayerTemplate;
            set => _scenePlayerTemplate = value;
        }

        /// <summary>
        ///     Stable id used by Mirror spawn handlers for scene-authored player templates.
        /// </summary>
        public string ScenePlayerTemplateSpawnId
        {
            get => _scenePlayerTemplateSpawnId;
            set
            {
                _scenePlayerTemplateSpawnId = value;
                _scenePlayerTemplateAssetId = 0;
            }
        }

        /// <summary>
        ///     Disable the original scene template at runtime.
        /// </summary>
        public bool DisableScenePlayerTemplate
        {
            get => _disableScenePlayerTemplate;
            set => _disableScenePlayerTemplate = value;
        }

        /// <summary>
        ///     How the client handshake (Ready, then AddPlayer) is completed after connect. See <see cref="NeoHandshakeMode"/>.
        /// </summary>
        public NeoHandshakeMode HandshakeMode
        {
            get => _handshakeMode;
            set => _handshakeMode = value;
        }

        /// <summary>Optional Telepathy queue cap; zero preserves authored transport limits.</summary>
        public int TelepathyQueueLimit
        {
            get => _telepathyQueueLimit;
            set => _telepathyQueueLimit = Mathf.Max(0, value);
        }

        /// <summary>
        ///     Activate every scene <see cref="NetworkIdentity"/> object when a session starts (see
        ///     <see cref="NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects(System.Predicate{UnityEngine.GameObject})"/>).
        /// </summary>
        public bool ActivateSceneObjectsOnStart
        {
            get => _activateSceneObjectsOnStart;
            set => _activateSceneObjectsOnStart = value;
        }

        /// <summary>Server: a client connected (host loopback included). Argument: the connection.</summary>
        public UnityEvent<NetworkConnectionToClient> OnServerClientConnectedEvent => _onServerClientConnected;

        /// <summary>Server: a client sent Ready.</summary>
        public UnityEvent<NetworkConnectionToClient> OnServerClientReadyEvent => _onServerClientReady;

        /// <summary>
        ///     Server: a connection is ready and owns a player object (<see cref="IsConnectionSpawned"/> became true).
        ///     Fires once per connection; this is the moment a handler-carrying broadcast can safely reach it.
        /// </summary>
        public UnityEvent<NetworkConnectionToClient> OnServerPlayerReadyEvent => _onServerPlayerReady;

        /// <summary>Server: a client disconnected. Raised before Mirror destroys its player object.</summary>
        public UnityEvent<NetworkConnectionToClient> OnServerClientDisconnectedEvent => _onServerClientDisconnected;

        /// <summary>C# twin of <see cref="OnLocalPlayerSpawnedEvent"/>.</summary>
        public event Action LocalPlayerSpawned;

        /// <summary>
        ///     Static: a client session is starting (client or host), raised from <c>OnStartClient</c>, before any message
        ///     can arrive. Mirror clears every message handler when a session shuts down; handler registries
        ///     (<see cref="Realtime.NetClientHandlers"/>) re-register from this event.
        /// </summary>
        public static event Action ClientSessionStarted;

        /// <summary>
        ///     Static: a server session is starting (server or host), raised from <c>OnStartServer</c> right after
        ///     <c>NetworkServer.Listen</c>. See <see cref="ClientSessionStarted"/>.
        /// </summary>
        public static event Action ServerSessionStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionEvents()
        {
            ClientSessionStarted = null;
            ServerSessionStarted = null;
        }

        /// <summary>C# twin of <see cref="OnServerClientConnectedEvent"/>.</summary>
        public event Action<NetworkConnectionToClient> ServerClientConnected;

        /// <summary>C# twin of <see cref="OnServerClientReadyEvent"/>.</summary>
        public event Action<NetworkConnectionToClient> ServerClientReady;

        /// <summary>C# twin of <see cref="OnServerPlayerReadyEvent"/>.</summary>
        public event Action<NetworkConnectionToClient> ServerPlayerReady;

        /// <summary>C# twin of <see cref="OnServerClientDisconnectedEvent"/>.</summary>
        public event Action<NetworkConnectionToClient> ServerClientDisconnected;

        /// <summary>
        ///     True while this client owns a spawned player object (<c>NetworkClient.localPlayer</c> is set).
        /// </summary>
        public bool IsLocalPlayerSpawned => NetworkClient.active && NetworkClient.localPlayer != null;

        /// <summary>
        ///     Server: true when a message sent to <paramref name="connection"/> now will be dispatched by the peer
        ///     (<c>isReady &amp;&amp; identity != null</c>). Same as <see cref="NeoNetworkState.IsConnectionSpawned"/>.
        /// </summary>
        public bool IsConnectionSpawned(NetworkConnectionToClient connection)
        {
            return NeoNetworkState.IsConnectionSpawned(connection);
        }

        public override void Reset()
        {
            base.Reset();
            EnsureScenePlayerTemplateSpawnId();
        }

        public override void OnValidate()
        {
            ApplyScenePlayerTemplateMode();
            base.OnValidate();
            EnsureScenePlayerTemplateSpawnId();

            if (_scenePlayerTemplate != null && !_scenePlayerTemplate.TryGetComponent(out NetworkIdentity _))
            {
                NetworkDiagnostics.LogError("[NeoNetworkManager] Scene Player Template must have a NetworkIdentity.",
                    this);
            }
        }

        public override void Awake()
        {
            ApplyDiagnosticsToggles();
            PrepareScenePlayerTemplate();
            base.Awake();

            if (singleton == this)
            {
                ApplyTransportQueueLimits();
            }

            // A duplicate manager is destroyed by Mirror in base.Awake; only the live singleton may listen.
            if (singleton == this && !_sceneActivationSubscribed)
            {
                _sceneActivationSubscribed = true;
                SceneManager.sceneLoaded += OnSceneLoadedActivateNetworked;
            }
        }

        public override void OnDestroy()
        {
            if (_sceneActivationSubscribed)
            {
                _sceneActivationSubscribed = false;
                SceneManager.sceneLoaded -= OnSceneLoadedActivateNetworked;
            }

            base.OnDestroy();
        }

        public override void LateUpdate()
        {
            base.LateUpdate();
            PollLocalPlayer();
            PollPendingPlayerConnections();
        }

        /// <summary>
        ///     Mirror resets <c>Application.targetFrameRate</c> to its send rate on a headless start. When a
        ///     <see cref="NeoNetworkBootstrap"/> has applied its own headless cap, that cap wins.
        /// </summary>
        public override void ConfigureHeadlessFrameRate()
        {
            if (NeoNetworkBootstrap.OwnsFrameRate)
            {
                return;
            }

            base.ConfigureHeadlessFrameRate();
        }

        public override void Start()
        {
            PrepareScenePlayerTemplate();
            ApplyTransportQueueLimits();
            base.Start();
        }

        public new void StartHost()
        {
            PrepareScenePlayerTemplate(true);
            RegisterScenePlayerTemplateSpawnHandler();
            ApplyTransportQueueLimits();
            base.StartHost();
            DisableScenePlayerTemplateInstance();
        }

        public new void StartServer()
        {
            PrepareScenePlayerTemplate(true);
            ApplyTransportQueueLimits();
            base.StartServer();
            DisableScenePlayerTemplateInstance();
        }

        public new void StartClient()
        {
            PrepareScenePlayerTemplate(true);
            RegisterScenePlayerTemplateSpawnHandler();
            ApplyTransportQueueLimits();
            base.StartClient();
        }

        public new void StartClient(System.Uri uri)
        {
            PrepareScenePlayerTemplate(true);
            RegisterScenePlayerTemplateSpawnHandler();
            ApplyTransportQueueLimits();
            base.StartClient(uri);
        }

        /// <summary>
        ///     Caps the root Telepathy transport's queues before it starts. Call after replacing a transport
        ///     or before starting through a Mirror-typed manager reference. Positive authored limits are
        ///     never increased; invalid nonpositive limits become the configured cap. Multiplex is not traversed.
        /// </summary>
        public void ApplyTransportQueueLimits()
        {
            if (_telepathyQueueLimit <= 0 || transport == null)
            {
                return;
            }

            Type transportType = transport.GetType();
            Type candidate = transportType;
            while (candidate != null && candidate.FullName != "Mirror.TelepathyTransport")
            {
                candidate = candidate.BaseType;
            }

            if (candidate == null)
            {
                return;
            }

            CapTransportQueueField(transportType, "serverSendQueueLimitPerConnection");
            CapTransportQueueField(transportType, "serverReceiveQueueLimitPerConnection");
            CapTransportQueueField(transportType, "clientSendQueueLimit");
            CapTransportQueueField(transportType, "clientReceiveQueueLimit");
        }

        private void CapTransportQueueField(Type transportType, string fieldName)
        {
            FieldInfo field = transportType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null || field.FieldType != typeof(int))
            {
                return;
            }

            int current = (int)field.GetValue(transport);
            field.SetValue(transport, current > 0 ? Math.Min(current, _telepathyQueueLimit) : _telepathyQueueLimit);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            PrepareScenePlayerTemplate(true);
            DisableScenePlayerTemplateInstance();
            ResetServerTracking();
            ActivateSceneObjects();
            NetworkContextActionRelay.RegisterMirrorHandlers();
            RaiseSafe(ServerSessionStarted);
            _onServerStarted?.Invoke();
            LogLifecycle("Server started.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _addPlayerRequested = false;
            _reportedLocalPlayer = null;
            NetworkContextActionRelay.RegisterMirrorHandlers();
            RegisterScenePlayerTemplateSpawnHandler();
            PrepareScenePlayerTemplate(true);
            if (!NetworkServer.active)
            {
                DisableScenePlayerTemplateInstance();
            }

            ActivateSceneObjects();
            RaiseSafe(ClientSessionStarted);
        }

        public override void OnStartHost()
        {
            base.OnStartHost();
            NetworkContextActionRelay.RegisterMirrorHandlers();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ResetServerTracking();
            _onServerStopped?.Invoke();
            LogLifecycle("Server stopped.");
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _addPlayerRequested = false;
            _reportedLocalPlayer = null;
            UnregisterScenePlayerTemplateSpawnHandler();
        }

        // ---- server-side per-connection events ---------------------------------------------------

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            TrackPendingPlayerConnection(conn);
            LogLifecycle($"Server: client connected (connId={ConnectionIdOf(conn)}).");
            RaiseServerEvent(ServerClientConnected, _onServerClientConnected, conn);
        }

        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
            LogLifecycle($"Server: client ready (connId={ConnectionIdOf(conn)}).");
            RaiseServerEvent(ServerClientReady, _onServerClientReady, conn);
            TryReportPlayerReady(conn);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (TryCreateScenePlayer(conn, out GameObject player))
            {
                NetworkServer.AddPlayerForConnection(conn, player, ScenePlayerTemplateAssetId);
            }
            else if (playerPrefab != null)
            {
                base.OnServerAddPlayer(conn);
            }
            else
            {
                // WHY: Mirror's default implementation instantiates playerPrefab and throws on null. A game that
                // builds its own player (HandshakeMode.Always, no prefab) overrides this method and never gets here.
                NetworkDiagnostics.LogWarning(
                    "[NeoNetworkManager] AddPlayer received but there is no Player Prefab or Scene Player Template; " +
                    "override OnServerAddPlayer to create the player object.", this);
            }

            TryReportPlayerReady(conn);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            ForgetPlayerConnection(conn);
            LogLifecycle($"Server: client disconnected (connId={ConnectionIdOf(conn)}).");
            // WHY: raised before base.OnServerDisconnect so conn.identity is still readable for cleanup.
            RaiseServerEvent(ServerClientDisconnected, _onServerClientDisconnected, conn);
            base.OnServerDisconnect(conn);
        }

        // ---- client handshake -----------------------------------------------------------------------

        public override void OnClientConnect()
        {
            // Mirror's own path: Ready always, AddPlayer when autoCreatePlayer, but only when no scene change is pending.
            bool mirrorHandshakeRuns = !clientLoadedScene;
            if (mirrorHandshakeRuns && autoCreatePlayer)
            {
                _addPlayerRequested = true;
            }

            base.OnClientConnect();
            TryAddSceneTemplatePlayer();
            if (mirrorHandshakeRuns)
            {
                EnsureClientHandshake();
            }

            _onClientConnected?.Invoke();
            LogLifecycle("Client connected.");
        }

        public override void OnClientSceneChanged()
        {
            // base: Ready, and AddPlayer when autoCreatePlayer and the local player is gone.
            if (autoCreatePlayer && NetworkClient.localPlayer == null)
            {
                _addPlayerRequested = true;
            }

            base.OnClientSceneChanged();
            EnsureClientHandshake();
        }

        public override void OnClientNotReady()
        {
            base.OnClientNotReady();
            _addPlayerRequested = false;
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            _addPlayerRequested = false;
            _reportedLocalPlayer = null;
            _onClientDisconnected?.Invoke();
            LogLifecycle("Client disconnected.");
        }

        private void LogLifecycle(string message)
        {
            if (_debugLifecycleLog)
            {
                NetworkDiagnostics.Log($"[NeoNetworkManager] {message}", this, true);
            }
        }

        /// <summary>
        ///     Convenience method: start as Host (server + client).
        /// </summary>
        public void StartAsHost()
        {
            ((NeoNetworkManager)this).StartHost();
        }

        /// <summary>
        ///     Convenience method: start as Client only. Connects to the current <c>networkAddress</c>.
        /// </summary>
        public void StartAsClient()
        {
            ((NeoNetworkManager)this).StartClient();
        }

        /// <summary>
        ///     Convenience method: set <c>networkAddress</c> to <paramref name="address"/> and start as Client only.
        /// </summary>
        /// <param name="address">Host name or IP address of the server.</param>
        public void StartAsClient(string address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                networkAddress = address.Trim();
            }

            ((NeoNetworkManager)this).StartClient();
        }

        /// <summary>
        ///     Convenience method: set the address and the port of a <see cref="PortTransport"/> (Telepathy, KCP,
        ///     SimpleWeb, Multiplex) and start as Client only.
        /// </summary>
        /// <param name="address">Host name or IP address of the server.</param>
        /// <param name="port">Server port. 0 leaves the transport's port untouched.</param>
        public void StartAsClient(string address, ushort port)
        {
            ApplyPort(port);
            StartAsClient(address);
        }

        /// <summary>
        ///     Convenience method: start as Server only (headless/dedicated).
        /// </summary>
        public void StartAsServer()
        {
            ((NeoNetworkManager)this).StartServer();
        }

        /// <summary>
        ///     Stop whatever role is currently running.
        /// </summary>
        public void StopNetwork()
        {
            if (IsHost)
            {
                StopHost();
            }
            else if (IsServer)
            {
                StopServer();
            }
            else if (IsClient)
            {
                StopClient();
            }
        }

        /// <summary>
        ///     Sets the listen/connect port on the active transport when it implements <see cref="PortTransport"/>.
        /// </summary>
        /// <param name="port">Port to apply; 0 is ignored.</param>
        /// <returns><see langword="true"/> when the port was applied.</returns>
        public bool ApplyPort(ushort port)
        {
            if (port == 0 || transport == null)
            {
                return false;
            }

            if (transport is PortTransport portTransport)
            {
                portTransport.Port = port;
                return true;
            }

            NetworkDiagnostics.LogWarning(
                $"[NeoNetworkManager] Transport {transport.GetType().Name} has no settable port; ignoring port {port}.",
                this);
            return false;
        }

        // ---- handshake / events / activation internals -------------------------------------------

        private void EnsureClientHandshake()
        {
            if (_handshakeMode == NeoHandshakeMode.Manual || !NetworkClient.active || NetworkClient.connection == null)
            {
                return;
            }

            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            bool wantsPlayer = _handshakeMode == NeoHandshakeMode.Always
                               || (_handshakeMode == NeoHandshakeMode.Auto && (autoCreatePlayer || _useScenePlayerTemplate));
            if (!wantsPlayer || _addPlayerRequested || !NetworkClient.ready)
            {
                return;
            }

            if (NetworkClient.localPlayer != null || NetworkClient.connection.identity != null)
            {
                return;
            }

            _addPlayerRequested = true;
            NetworkClient.AddPlayer();
        }

        private void PollLocalPlayer()
        {
            NetworkIdentity current = NetworkClient.active ? NetworkClient.localPlayer : null;
            if (current == null)
            {
                _reportedLocalPlayer = null;
                return;
            }

            if (ReferenceEquals(current, _reportedLocalPlayer))
            {
                return;
            }

            _reportedLocalPlayer = current;
            LogLifecycle("Local player spawned.");
            RaiseSafe(LocalPlayerSpawned);
            _onLocalPlayerSpawned?.Invoke();
        }

        private void TrackPendingPlayerConnection(NetworkConnectionToClient conn)
        {
            if (conn != null && !_playerReadyReported.Contains(conn.connectionId)
                             && !_pendingPlayerConnections.Contains(conn))
            {
                _pendingPlayerConnections.Add(conn);
            }
        }

        private void TryReportPlayerReady(NetworkConnectionToClient conn)
        {
            if (conn == null || !NeoNetworkState.IsConnectionSpawned(conn)
                             || !_playerReadyReported.Add(conn.connectionId))
            {
                return;
            }

            _pendingPlayerConnections.Remove(conn);
            LogLifecycle($"Server: player ready (connId={conn.connectionId}).");
            RaiseServerEvent(ServerPlayerReady, _onServerPlayerReady, conn);
        }

        // WHY: a game can add the player through NetworkServer.AddPlayerForConnection from its own code (lobby,
        // character select), bypassing OnServerAddPlayer. A cheap sweep over the few not-yet-spawned connections
        // makes "player ready" independent of how the player was created.
        private void PollPendingPlayerConnections()
        {
            if (!NetworkServer.active || _pendingPlayerConnections.Count == 0)
            {
                return;
            }

            for (int i = _pendingPlayerConnections.Count - 1; i >= 0; i--)
            {
                if (i >= _pendingPlayerConnections.Count)
                {
                    continue;
                }

                TryReportPlayerReady(_pendingPlayerConnections[i]);
            }
        }

        private void ForgetPlayerConnection(NetworkConnectionToClient conn)
        {
            if (conn == null)
            {
                return;
            }

            _pendingPlayerConnections.Remove(conn);
            _playerReadyReported.Remove(conn.connectionId);
            NetworkContextActionRelay.ForgetServerIngressPeer(conn);
        }

        private void ResetServerTracking()
        {
            _pendingPlayerConnections.Clear();
            _playerReadyReported.Clear();
            NetworkContextActionRelay.ResetServerIngressState();
        }

        private static int ConnectionIdOf(NetworkConnectionToClient conn)
        {
            return conn != null ? conn.connectionId : -1;
        }

        private static void RaiseSafe(Action handler)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler();
            }
            catch (Exception exception)
            {
                NetworkDiagnostics.LogException(exception);
            }
        }

        // Listener failures must not interrupt handshake continuation or disconnect cleanup.
        private static void RaiseServerEvent(Action<NetworkConnectionToClient> handler,
            UnityEvent<NetworkConnectionToClient> unityEvent, NetworkConnectionToClient conn)
        {
            if (handler != null)
            {
                try
                {
                    handler(conn);
                }
                catch (Exception exception)
                {
                    NetworkDiagnostics.LogException(exception);
                }
            }

            try
            {
                unityEvent?.Invoke(conn);
            }
            catch (Exception exception)
            {
                NetworkDiagnostics.LogException(exception);
            }
        }

        private void ActivateSceneObjects()
        {
            if (!_activateSceneObjectsOnStart)
            {
                return;
            }

            int activated = NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects(IsScenePlayerTemplateObject);
            if (activated > 0)
            {
                LogLifecycle($"Activated {activated} scene network object(s).");
            }
        }

        private void OnSceneLoadedActivateNetworked(Scene scene, LoadSceneMode mode)
        {
            if (_activateSceneObjectsOnStart && (NetworkServer.active || NetworkClient.active))
            {
                NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects(scene, IsScenePlayerTemplateObject);
            }
        }

        // The disabled scene template (and its children) must stay off: only spawned copies may be active.
        private bool IsScenePlayerTemplateObject(GameObject candidate)
        {
            return _useScenePlayerTemplate
                   && _disableScenePlayerTemplate
                   && _scenePlayerTemplate != null
                   && (candidate == _scenePlayerTemplate || candidate.transform.IsChildOf(_scenePlayerTemplate.transform));
        }

        private uint ScenePlayerTemplateAssetId
        {
            get
            {
                if (_scenePlayerTemplateAssetId == 0)
                {
                    _scenePlayerTemplateAssetId = CalculateStableAssetId(GetEffectiveScenePlayerTemplateSpawnId());
                }

                return _scenePlayerTemplateAssetId;
            }
        }

        private void PrepareScenePlayerTemplate(bool allowDisableTemplate = false)
        {
            NormalizeScenePlayerTemplateMode();

            if (!_useScenePlayerTemplate)
            {
                return;
            }

            ApplyScenePlayerTemplateMode();

            if (!allowDisableTemplate || _scenePlayerTemplate == null || !_disableScenePlayerTemplate)
            {
                return;
            }

            DisableScenePlayerTemplateInstance();
        }

        private void DisableScenePlayerTemplateInstance()
        {
            if (!_useScenePlayerTemplate || !_disableScenePlayerTemplate || _scenePlayerTemplate == null)
            {
                return;
            }

            if (_scenePlayerTemplate.activeSelf)
            {
                _scenePlayerTemplate.SetActive(false);
            }
        }

        private void NormalizeScenePlayerTemplateMode()
        {
            if (_useScenePlayerTemplate)
            {
                return;
            }

            if (playerPrefab == null)
            {
                return;
            }

            if (!playerPrefab.TryGetComponent(out NetworkIdentity identity) || identity.sceneId == 0)
            {
                return;
            }

            _useScenePlayerTemplate = true;
            _scenePlayerTemplate = playerPrefab;
            NetworkDiagnostics.LogWarning(
                "[NeoNetworkManager] Player Prefab references a scene object. Switching to Scene Player Template mode automatically.",
                this);
        }

        private void ApplyScenePlayerTemplateMode()
        {
            if (!_useScenePlayerTemplate)
            {
                return;
            }

            autoCreatePlayer = false;
            playerPrefab = null;
        }

        private void TryAddSceneTemplatePlayer()
        {
            if (!_useScenePlayerTemplate || NetworkClient.localPlayer != null || _addPlayerRequested)
            {
                return;
            }

            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }

            _addPlayerRequested = true;
            NetworkClient.AddPlayer();
        }

        private bool TryCreateScenePlayer(NetworkConnectionToClient conn, out GameObject player)
        {
            player = null;

            if (!_useScenePlayerTemplate)
            {
                return false;
            }

            if (!IsScenePlayerTemplateValid())
            {
                return false;
            }

            Transform startPosition = GetStartPosition();
            Vector3 position = startPosition != null ? startPosition.position : _scenePlayerTemplate.transform.position;
            Quaternion rotation =
                startPosition != null ? startPosition.rotation : _scenePlayerTemplate.transform.rotation;

            player = InstantiateScenePlayerTemplate(position, rotation);
            player.name = conn != null
                ? $"{_scenePlayerTemplate.name} (Player {conn.connectionId})"
                : $"{_scenePlayerTemplate.name} (Player)";
            player.SetActive(true);
            return true;
        }

        private void RegisterScenePlayerTemplateSpawnHandler()
        {
            if (!_useScenePlayerTemplate || !IsScenePlayerTemplateValid())
            {
                return;
            }

            uint assetId = ScenePlayerTemplateAssetId;
            if (_scenePlayerTemplateSpawnHandlerRegistered && _registeredScenePlayerTemplateAssetId == assetId)
            {
                return;
            }

            UnregisterScenePlayerTemplateSpawnHandler();
            NetworkClient.RegisterSpawnHandler(assetId, SpawnScenePlayerTemplate, UnspawnScenePlayerTemplate);
            _registeredScenePlayerTemplateAssetId = assetId;
            _scenePlayerTemplateSpawnHandlerRegistered = true;
        }

        private void UnregisterScenePlayerTemplateSpawnHandler()
        {
            if (!_scenePlayerTemplateSpawnHandlerRegistered)
            {
                return;
            }

            NetworkClient.UnregisterSpawnHandler(_registeredScenePlayerTemplateAssetId);
            _registeredScenePlayerTemplateAssetId = 0;
            _scenePlayerTemplateSpawnHandlerRegistered = false;
        }

        private GameObject SpawnScenePlayerTemplate(SpawnMessage message)
        {
            GameObject player = InstantiateScenePlayerTemplate(message.position, message.rotation);
            player.transform.localScale = message.scale;
            player.name = $"{_scenePlayerTemplate.name} (Remote Player)";
            player.SetActive(true);
            return player;
        }

        private GameObject InstantiateScenePlayerTemplate(Vector3 position, Quaternion rotation)
        {
            if (NetworkIdentityHasSpawnedField == null && !_hasSpawnedFieldMissingLogged)
            {
                _hasSpawnedFieldMissingLogged = true;
                NetworkDiagnostics.LogWarning(
                    "[NeoNetworkManager] Mirror's private NetworkIdentity.hasSpawned field was not found " +
                    "(Mirror version changed?). Scene Player Template copies may fail to spawn correctly.",
                    this, true);
            }

            NetworkIdentity[] templateIdentities = _scenePlayerTemplate.GetComponentsInChildren<NetworkIdentity>(true);
            ulong[] originalSceneIds = new ulong[templateIdentities.Length];
            bool[] originalHasSpawned = new bool[templateIdentities.Length];

            for (int i = 0; i < templateIdentities.Length; i++)
            {
                originalSceneIds[i] = templateIdentities[i].sceneId;
                originalHasSpawned[i] = GetHasSpawned(templateIdentities[i]);
                templateIdentities[i].sceneId = 0;
                SetHasSpawned(templateIdentities[i], false);
            }

            GameObject player;
            try
            {
                player = Instantiate(_scenePlayerTemplate, position, rotation);
            }
            finally
            {
                for (int i = 0; i < templateIdentities.Length; i++)
                {
                    templateIdentities[i].sceneId = originalSceneIds[i];
                    SetHasSpawned(templateIdentities[i], originalHasSpawned[i]);
                }
            }

            ClearSceneIds(player);
            return player;
        }

        private static bool GetHasSpawned(NetworkIdentity identity)
        {
            return NetworkIdentityHasSpawnedField != null &&
                   (bool)NetworkIdentityHasSpawnedField.GetValue(identity);
        }

        private static void SetHasSpawned(NetworkIdentity identity, bool value)
        {
            NetworkIdentityHasSpawnedField?.SetValue(identity, value);
        }

        private static void ClearSceneIds(GameObject target)
        {
            NetworkIdentity[] identities = target.GetComponentsInChildren<NetworkIdentity>(true);
            for (int i = 0; i < identities.Length; i++)
            {
                identities[i].sceneId = 0;
            }
        }

        private static void UnspawnScenePlayerTemplate(GameObject spawned)
        {
            Destroy(spawned);
        }

        private bool IsScenePlayerTemplateValid()
        {
            if (_scenePlayerTemplate == null)
            {
                NetworkDiagnostics.LogError(
                    "[NeoNetworkManager] Scene Player Template is enabled, but no template object is assigned.", this);
                return false;
            }

            if (!_scenePlayerTemplate.TryGetComponent(out NetworkIdentity _))
            {
                NetworkDiagnostics.LogError("[NeoNetworkManager] Scene Player Template must have a NetworkIdentity.",
                    this);
                return false;
            }

            return true;
        }

        private string GetEffectiveScenePlayerTemplateSpawnId()
        {
            if (!string.IsNullOrWhiteSpace(_scenePlayerTemplateSpawnId))
            {
                return _scenePlayerTemplateSpawnId;
            }

            string scenePath = gameObject.scene.IsValid() ? gameObject.scene.path : string.Empty;
            string templateName = _scenePlayerTemplate != null ? _scenePlayerTemplate.name : "ScenePlayerTemplate";
            return $"{scenePath}/{name}/{templateName}";
        }

        private void EnsureScenePlayerTemplateSpawnId()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(_scenePlayerTemplateSpawnId))
            {
                _scenePlayerTemplateSpawnId = System.Guid.NewGuid().ToString("N");
            }
#endif
            _scenePlayerTemplateAssetId = 0;
        }

        private static uint CalculateStableAssetId(string value)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;

            uint hash = offset;
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= prime;
            }

            return hash == 0 ? 1 : hash;
        }
#else
        // WHY: Solo-mode stubs so user code compiles without Mirror.
        public bool IsServer => true;
        public bool IsClient => true;
        public bool IsHost => true;

        private void Awake()
        {
            ApplyDiagnosticsToggles();
        }

        public void StartAsHost() => NetworkDiagnostics.LogWarning("[NeoNetworkManager] Mirror is not installed. Running in solo mode.");
        public void StartAsClient() => NetworkDiagnostics.LogWarning("[NeoNetworkManager] Mirror is not installed. Running in solo mode.");
        public void StartAsClient(string address) => StartAsClient();
        public void StartAsServer() => NetworkDiagnostics.LogWarning("[NeoNetworkManager] Mirror is not installed. Running in solo mode.");
        public void StopNetwork() { }
#endif
    }
}
