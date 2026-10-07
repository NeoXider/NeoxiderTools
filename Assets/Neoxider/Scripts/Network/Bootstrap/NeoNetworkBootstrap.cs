using System;
using System.Text;
#if MIRROR
using Mirror;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Neo.Network
{
    /// <summary>
    ///     Decides how this process starts and nothing else: reads the command line (or the page URL on WebGL),
    ///     resolves a <see cref="NeoStartupIntent"/>, prepares the transport and calls exactly one of
    ///     <see cref="NeoNetworkManager.StartAsServer"/> / <see cref="NeoNetworkManager.StartAsHost"/> /
    ///     <see cref="NeoNetworkManager.StartAsClient()"/>.
    ///     <para>
    ///         With no networking switches it does nothing, so single player is unaffected. With
    ///         <c>-batchmode -nographics -server</c> it becomes a dedicated server: it suppresses cameras, audio
    ///         listeners and UI, caps the frame rate, turns vSync off and logs a status line every few seconds.
    ///     </para>
    ///     <para>
    ///         A dedicated server can only be started from the command line, never from a button (it would blank the
    ///         player's own screen). Every public entry point is idempotent.
    ///     </para>
    /// </summary>
    [NeoDoc("Network/NeoNetworkBootstrap.md")]
    [CreateFromMenu("Neoxider/Network/NeoNetworkBootstrap")]
    [AddComponentMenu("Neoxider/Network/" + nameof(NeoNetworkBootstrap))]
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public sealed class NeoNetworkBootstrap : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("The Neo network manager to drive. Optional: found in the loaded scenes when empty.")]
        [SerializeField]
        private NeoNetworkManager _manager;

        [Header("Startup")]
        [Tooltip("Start networking automatically from the command line in Start(). Turn off to start from UI only.")]
        [SerializeField]
        private bool _startFromCommandLine = true;

        [Tooltip("WebGL has no command line: read -client / -address / -port / -name from the page URL query " +
                 "(?client&address=host&port=7778&name=Bob). Only a client may be started this way.")]
        [SerializeField]
        private bool _readUrlQueryOnWebGl = true;

        [Tooltip("Address a client connects to when -address is not given.")] [SerializeField]
        private string _defaultAddress = NeoStartupIntent.DefaultAddress;

        [Tooltip("Port used when -port is not given. 0 keeps the transport's own port.")] [SerializeField] [Min(0)]
        private int _defaultPort;

        [Tooltip("Capacity used when -maxplayers is not given. 0 keeps Mirror's Max Connections.")]
        [SerializeField]
        [Min(0)]
        private int _defaultMaxPlayers;

        [Tooltip("Extra game-specific command-line switches that take a value (for example -botfill). " +
                 "Read them with Intent.GetCustomInt / TryGetCustom.")]
        [SerializeField]
        private string[] _customValueSwitches = Array.Empty<string>();

        [Tooltip("Extra game-specific command-line flags without a value (for example -autoplay). Read with Intent.HasCustom.")]
        [SerializeField]
        private string[] _customFlagSwitches = Array.Empty<string>();

        [Header("Dedicated Server")]
        [Tooltip("On a dedicated server disable every Camera, AudioListener, Canvas and UIDocument so nothing renders or plays.")]
        [SerializeField]
        private bool _suppressPresentationOnDedicatedServer = true;

        [Tooltip("Extra components (local HUD, camera followers, effects) to disable on a dedicated server.")]
        [SerializeField]
        private Behaviour[] _dedicatedServerSuppressedBehaviours = Array.Empty<Behaviour>();

        [Header("Headless Runtime")]
        [Tooltip("Frame cap of a headless process (batch mode or dedicated server). A batch-mode server otherwise " +
                 "spins a core at thousands of frames per second. 0 leaves Mirror's own cap (its send rate).")]
        [SerializeField]
        [Min(0)]
        private int _headlessTargetFrameRate = 60;

        [Tooltip("Turn vSync off in a headless process so the frame cap is the only limiter.")] [SerializeField]
        private bool _disableVSyncWhenHeadless = true;

        [Tooltip("Keep simulating when the window loses focus, for any networked peer. An unfocused client or host " +
                 "otherwise stops and looks like a dead peer to the server.")]
        [SerializeField]
        private bool _runInBackgroundWhenNetworked = true;

        [Header("Status Log")]
        [Tooltip("Seconds between one-line '[Server] status:' log lines from an authority peer. 0 turns it off.")]
        [SerializeField]
        [Min(0f)]
        private float _statusLogIntervalSeconds = 10f;

        [Tooltip("Seconds before the first status line once the server is up.")] [SerializeField] [Min(0f)]
        private float _firstStatusDelaySeconds = 3f;

        [Tooltip("Log the status line only in batch mode or on a dedicated server (a desktop host has a screen for that).")]
        [SerializeField]
        private bool _statusLogOnlyWhenHeadless = true;

#if MIRROR
        [Header("WebGL")]
        [Tooltip("Transport used instead of the manager's own in WebGL builds, normally a SimpleWebTransport " +
                 "(WebSocket). Browsers cannot open raw TCP/UDP sockets. A server that must accept browsers AND " +
                 "desktop clients puts both transports in a MultiplexTransport.")]
        [SerializeField]
        private Transport _webGlTransport;
#endif

        [Header("Events")] [SerializeField] private UnityEvent _onNetworkStarted = new();

        private NeoStartupMode _commandLineMode;
        private float _nextStatusAt;
        private bool _sceneSuppressionSubscribed;
        private readonly StringBuilder _status = new(192);

        /// <summary>
        ///     True once a dedicated-server peer has been resolved. Presenters and effects read it to skip work
        ///     instead of being destroyed; useful for components created after Awake.
        /// </summary>
        public static bool LocalPresentationSuppressed { get; private set; }

        /// <summary>True from Awake until OnDestroy.</summary>
        public static bool HasInstance { get; private set; }

        /// <summary>The live bootstrap, or <see langword="null"/> before Awake.</summary>
        public static NeoNetworkBootstrap Active { get; private set; }

        /// <summary>
        ///     True when the bootstrap applied a headless frame-rate cap. <see cref="NeoNetworkManager"/> then leaves
        ///     <c>Application.targetFrameRate</c> alone instead of resetting it to Mirror's send rate.
        /// </summary>
        public static bool OwnsFrameRate { get; private set; }

        /// <summary>The resolved (and possibly UI-overridden) startup decision.</summary>
        public NeoStartupIntent Intent { get; private set; }

        /// <summary>The role the current <see cref="Intent"/> asks for.</summary>
        public NeoStartupMode StartupMode => Intent.Mode;

        /// <summary>True once this component started a Mirror session.</summary>
        public bool HasStartedNetwork { get; private set; }

        /// <summary>Raised after a session was started by this component.</summary>
        public UnityEvent OnNetworkStartedEvent => _onNetworkStarted;

        /// <summary>C# event: raised once the command line was resolved in Awake.</summary>
        public event Action<NeoStartupIntent> IntentResolved;

        /// <summary>
        ///     Optional provider appended to the periodic status line, for game metrics
        ///     (<c>"humans=3 bots=4 mobs=120"</c>). Return <see langword="null"/> to add nothing.
        /// </summary>
        public Func<string> StatusExtraProvider { get; set; }

        /// <summary>The Neo manager this bootstrap drives; resolved lazily.</summary>
        public NeoNetworkManager Manager
        {
            get
            {
                if (_manager != null)
                {
                    return _manager;
                }

#if MIRROR
                _manager = NetworkManager.singleton as NeoNetworkManager;
#endif
                if (_manager == null)
                {
                    _manager = FindFirstObjectByType<NeoNetworkManager>(FindObjectsInactive.Include);
                }

                return _manager;
            }
        }

        /// <summary>
        ///     Resolves the startup decision from raw arguments. Static and Unity-free, so the whole decision table is
        ///     unit-testable: <c>NeoNetworkBootstrap.Resolve(new[] { "-host", "-port", "7778" })</c>.
        /// </summary>
        public static NeoStartupIntent Resolve(string[] args)
        {
            return NeoStartupCommandLine.Parse(args);
        }

        /// <summary>Resolves against explicit defaults and extra switches.</summary>
        public static NeoStartupIntent Resolve(
            string[] args,
            NeoStartupDefaults defaults,
            string[] customValueSwitches = null,
            string[] customFlagSwitches = null)
        {
            return NeoStartupCommandLine.Parse(args, defaults, customValueSwitches, customFlagSwitches);
        }

        private void Awake()
        {
            if (Active != null && Active != this)
            {
                NetworkDiagnostics.LogError(
                    $"[NeoNetworkBootstrap] A second bootstrap was found on '{name}'. The first one keeps ownership; remove this copy.",
                    this);
                enabled = false;
                return;
            }

            Active = this;
            HasInstance = true;

            Intent = ResolveFromEnvironment();
            _commandLineMode = Intent.Mode;
            LocalPresentationSuppressed = Intent.SuppressesLocalPresentation;
            ApplyHeadlessRuntime(Intent);
            ApplyWebGlTransport();

            NetworkDiagnostics.Log($"[NeoNetworkBootstrap] Resolved {Intent}", this);
            IntentResolved?.Invoke(Intent);
        }

        private void Start()
        {
            if (_startFromCommandLine)
            {
                StartNetwork(Intent);
            }
        }

        private void OnDestroy()
        {
            if (_sceneSuppressionSubscribed)
            {
                _sceneSuppressionSubscribed = false;
                SceneManager.sceneLoaded -= OnSceneLoadedSuppress;
            }

            if (Active != this)
            {
                return;
            }

            Active = null;
            HasInstance = false;
            LocalPresentationSuppressed = false;
            OwnsFrameRate = false;
        }

        private void Update()
        {
            if (_statusLogIntervalSeconds <= 0f || !HasStartedNetwork)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now < _nextStatusAt)
            {
                return;
            }

            _nextStatusAt = now + _statusLogIntervalSeconds;
            if (!ShouldLogStatus())
            {
                return;
            }

            NetworkDiagnostics.Log(BuildStatusLine(), this, true);
        }

        /// <summary>
        ///     Starts (or, for solo, deliberately does not start) the given role. Idempotent and safe from any
        ///     caller: a UI button, a test and the <c>Start()</c> path can all call it.
        /// </summary>
        /// <param name="intent">What to start. A dedicated server is refused unless the command line asked for it.</param>
        /// <returns><see langword="true"/> when a session was started by this call.</returns>
        public bool StartNetwork(NeoStartupIntent intent)
        {
            if (intent.Mode == NeoStartupMode.DedicatedServer && _commandLineMode != NeoStartupMode.DedicatedServer)
            {
                NetworkDiagnostics.LogError(
                    "[NeoNetworkBootstrap] A dedicated server can only be started from the command line (-server); " +
                    "it cannot be started from the game.", this);
                return false;
            }

            // WHY: checked before anything is overwritten, so a stray second request cannot flip the running
            // session's presentation flag or intent.
            if (HasStartedNetwork)
            {
                NetworkDiagnostics.Log(
                    $"[NeoNetworkBootstrap] StartNetwork('{intent.Mode}') ignored: a session is already running.", this);
                return false;
            }

            Intent = intent;
            LocalPresentationSuppressed = intent.SuppressesLocalPresentation;

            if (!intent.IsNetworked)
            {
                NetworkDiagnostics.Log("[NeoNetworkBootstrap] Solo: no Mirror session started.", this);
                return false;
            }

            NeoNetworkManager manager = Manager;
            if (manager == null)
            {
                NetworkDiagnostics.LogError(
                    $"[NeoNetworkBootstrap] No NeoNetworkManager in the loaded scenes, so {intent.Mode} cannot start.", this);
                return false;
            }

            ApplyHeadlessRuntime(intent);
            ApplyConnectionSettings(manager, intent);
            ApplyDedicatedServerSuppression(intent);

            HasStartedNetwork = true;
            _nextStatusAt = Time.realtimeSinceStartup + Mathf.Max(0f, _firstStatusDelaySeconds);

            switch (intent.Mode)
            {
                case NeoStartupMode.DedicatedServer:
                    manager.StartAsServer();
                    break;
                case NeoStartupMode.Host:
                    manager.StartAsHost();
                    break;
                default:
                    manager.StartAsClient();
                    break;
            }

            NetworkDiagnostics.Log($"[NeoNetworkBootstrap] Started {intent.Mode}.", this, true);
            _onNetworkStarted?.Invoke();
            return true;
        }

        /// <summary>
        ///     Starts the given role from game code or a UI button, reusing the resolved port, capacity and name.
        ///     A client uses <see cref="NeoStartupIntent.Address"/>; a host listens locally.
        /// </summary>
        /// <param name="mode">Host or client. Solo is not something to start; a dedicated server is command-line only.</param>
        public bool StartAs(NeoStartupMode mode)
        {
            if (mode == NeoStartupMode.Solo)
            {
                NetworkDiagnostics.LogError("[NeoNetworkBootstrap] Solo is the absence of a network role, not something to start.",
                    this);
                return false;
            }

            return StartNetwork(Intent.WithMode(mode));
        }

        /// <summary>UI-friendly: start as host (server + local client).</summary>
        public void StartAsHost()
        {
            StartAs(NeoStartupMode.Host);
        }

        /// <summary>UI-friendly: start as a client of <see cref="NeoStartupIntent.Address"/>.</summary>
        public void StartAsClient()
        {
            StartAs(NeoStartupMode.Client);
        }

        /// <summary>UI-friendly: start as a client of <paramref name="address"/>.</summary>
        /// <param name="address">Host name or IP of the server; empty keeps the resolved address.</param>
        public void StartAsClient(string address)
        {
            NeoStartupIntent intent = string.IsNullOrWhiteSpace(address) ? Intent : Intent.WithAddress(address.Trim());
            StartNetwork(intent.WithMode(NeoStartupMode.Client));
        }

        /// <summary>Stops the running role. Idempotent; safe when nothing is running.</summary>
        public void StopNetwork()
        {
            if (!HasStartedNetwork)
            {
                return;
            }

            HasStartedNetwork = false;
            NeoNetworkManager manager = Manager;
            if (manager != null)
            {
                manager.StopNetwork();
            }
        }

        /// <summary>
        ///     Builds the one-line status string: role, connection counts, frame rate, uptime and the optional
        ///     <see cref="StatusExtraProvider"/> suffix. Public so a game can log or display it elsewhere.
        /// </summary>
        public string BuildStatusLine()
        {
            _status.Length = 0;
            _status.Append("[Server] status: mode=").Append(Intent.Mode);
#if MIRROR
            int connections = 0;
            int ready = 0;
            int spawned = 0;
            if (NetworkServer.active)
            {
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                {
                    connections++;
                    if (connection.isReady)
                    {
                        ready++;
                    }

                    if (NeoNetworkState.IsConnectionSpawned(connection))
                    {
                        spawned++;
                    }
                }
            }

            _status.Append(" connections=").Append(connections)
                .Append(" ready=").Append(ready)
                .Append(" spawned=").Append(spawned);
#endif
            _status.Append(" fps=").Append(Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)))
                .Append(" uptime=").Append(Mathf.RoundToInt(Time.realtimeSinceStartup)).Append('s');

            string extra = null;
            Func<string> provider = StatusExtraProvider;
            if (provider != null)
            {
                try
                {
                    extra = provider();
                }
                catch (Exception exception)
                {
                    NetworkDiagnostics.LogException(exception, this);
                }
            }

            if (!string.IsNullOrEmpty(extra))
            {
                _status.Append(' ').Append(extra);
            }

            return _status.ToString();
        }

        private bool ShouldLogStatus()
        {
            if (!Intent.IsAuthorityPeer)
            {
                return false;
            }

            return !_statusLogOnlyWhenHeadless || Application.isBatchMode || Intent.SuppressesLocalPresentation;
        }

        private NeoStartupIntent ResolveFromEnvironment()
        {
            NeoStartupDefaults defaults = new NeoStartupDefaults(_defaultAddress, _defaultPort, _defaultMaxPlayers);
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return ResolveFromUrl(defaults);
            }

            return NeoStartupCommandLine.Parse(ReadProcessArguments(), defaults, _customValueSwitches, _customFlagSwitches);
        }

        // A browser tab can only be a client: it cannot listen on a socket, and a "?server" in a link must never
        // turn a visitor's tab into a dedicated server.
        private NeoStartupIntent ResolveFromUrl(NeoStartupDefaults defaults)
        {
            if (!_readUrlQueryOnWebGl)
            {
                return NeoStartupIntent.Solo(defaults);
            }

            NeoStartupIntent fromUrl = NeoStartupCommandLine.Parse(
                NeoStartupCommandLine.FromUrlQuery(Application.absoluteURL), defaults, _customValueSwitches,
                _customFlagSwitches);
            return fromUrl.Mode == NeoStartupMode.Client ? fromUrl : fromUrl.WithMode(NeoStartupMode.Solo);
        }

        private static string[] ReadProcessArguments()
        {
            try
            {
                return Environment.GetCommandLineArgs();
            }
            catch (Exception exception)
            {
                NetworkDiagnostics.LogWarning(
                    $"[NeoNetworkBootstrap] Command line unavailable ({exception.GetType().Name}); defaulting to solo.");
                return Array.Empty<string>();
            }
        }

        // A headless or networked peer must keep running at a steady, bounded rate and keep running when its
        // window loses focus. Without a frame cap a batch-mode server spins a whole core; without run-in-background
        // an unfocused client or host stops simulating, which a server cannot tell from a dead peer.
        private void ApplyHeadlessRuntime(NeoStartupIntent intent)
        {
            bool headless = intent.Headless || Application.isBatchMode || intent.SuppressesLocalPresentation;
            if (headless)
            {
                if (_disableVSyncWhenHeadless)
                {
                    QualitySettings.vSyncCount = 0;
                }

                if (_headlessTargetFrameRate > 0)
                {
                    Application.targetFrameRate = _headlessTargetFrameRate;
                    OwnsFrameRate = true;
                }
            }

            if (intent.IsNetworked && _runInBackgroundWhenNetworked)
            {
                Application.runInBackground = true;
            }
        }

        private void ApplyWebGlTransport()
        {
#if MIRROR
            if (Application.platform != RuntimePlatform.WebGLPlayer || _webGlTransport == null)
            {
                return;
            }

            NeoNetworkManager manager = Manager;
            if (manager == null)
            {
                return;
            }

            manager.transport = _webGlTransport;
            Transport.active = _webGlTransport;
            NetworkDiagnostics.Log($"[NeoNetworkBootstrap] WebGL build: using {_webGlTransport.GetType().Name}.", this);
#endif
        }

        private void ApplyConnectionSettings(NeoNetworkManager manager, NeoStartupIntent intent)
        {
#if MIRROR
            if (intent.Mode == NeoStartupMode.Client && !string.IsNullOrEmpty(intent.Address))
            {
                manager.networkAddress = intent.Address;
            }

            if (intent.HasPort)
            {
                manager.ApplyPort((ushort)intent.Port);
            }

            if (intent.HasMaxPlayers && intent.Mode != NeoStartupMode.Client)
            {
                manager.maxConnections = intent.MaxPlayers;
            }
#endif
        }

        // Server-only peers must not render, listen to audio or show a local HUD. Presenting components read
        // LocalPresentationSuppressed; the explicit list plus the sweep cover anything that predates that flag.
        private void ApplyDedicatedServerSuppression(NeoStartupIntent intent)
        {
            if (!intent.SuppressesLocalPresentation)
            {
                return;
            }

            for (int i = 0; i < _dedicatedServerSuppressedBehaviours.Length; i++)
            {
                Behaviour behaviour = _dedicatedServerSuppressedBehaviours[i];
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }

            if (!_suppressPresentationOnDedicatedServer)
            {
                return;
            }

            SuppressPresentationObjects();
            if (!_sceneSuppressionSubscribed)
            {
                _sceneSuppressionSubscribed = true;
                SceneManager.sceneLoaded += OnSceneLoadedSuppress;
            }
        }

        private void OnSceneLoadedSuppress(Scene scene, LoadSceneMode mode)
        {
            if (LocalPresentationSuppressed && _suppressPresentationOnDedicatedServer)
            {
                SuppressPresentationObjects();
            }
        }

        private static void SuppressPresentationObjects()
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].enabled = false;
            }

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].enabled = false;
            }

            // WHY: looked up by name so Neo.Network keeps no hard dependency on the UI Toolkit module, which a
            // project may have switched off in Package Manager.
            Type documentType = Type.GetType("UnityEngine.UIElements.UIDocument, UnityEngine.UIElementsModule");
            if (documentType == null)
            {
                return;
            }

            UnityEngine.Object[] documents = FindObjectsByType(documentType, FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] is Behaviour document)
                {
                    document.enabled = false;
                }
            }
        }
    }
}
