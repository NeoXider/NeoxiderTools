#if MIRROR
using Mirror;
using Mirror.Discovery;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Network
{
    /// <summary>
    ///     NoCode LAN discovery wrapper around Mirror's <see cref="NetworkDiscovery"/>.
    ///     Automatically handles server advertising and client server-list via UnityEvents.
    ///     <para>Usage: add to your NetworkManager object alongside <see cref="NetworkDiscovery"/>.</para>
    /// </summary>
    [NeoDoc("Network/NeoNetworkDiscovery.md")]
    [CreateFromMenu("Neoxider/Network/Neo Network Discovery")]
    [AddComponentMenu("Neoxider/Network/Neo Network Discovery")]
    [RequireComponent(typeof(NetworkDiscovery))]
    public class NeoNetworkDiscovery : MonoBehaviour
    {
        [Header("Settings")] [Tooltip("Automatically start advertising when hosting.")] [SerializeField]
        private bool _autoAdvertiseOnHost = true;

        [Tooltip("Automatically start discovery when not hosting.")] [SerializeField]
        private bool _autoDiscoverOnClient = true;

        [Tooltip("How often to refresh the server list (seconds).")] [SerializeField]
        private float _refreshInterval = 2f;

        [Header("Quick Play")]
        [Tooltip("During QuickPlay(): connect to the first server found automatically.")]
        [SerializeField]
        private bool _autoJoinFirstFound = true;

        [Tooltip("During QuickPlay(): host when no server was found within this many seconds. 0 = never host.")]
        [SerializeField] [Min(0f)]
        private float _hostIfNoneFoundAfter = 3f;

        [Header("Events")] [Tooltip("Fired when a new server is found on the LAN. String = server address.")]
        public UnityEvent<string> OnServerFound = new();

        [Tooltip("Fired when the server list is updated. Int = total server count.")]
        public UnityEvent<int> OnServerListUpdated = new();

        [Tooltip("Fired when advertising starts (this machine is hosting).")]
        public UnityEvent OnAdvertisingStarted = new();

        [Tooltip("Fired when discovery starts (this machine is searching).")]
        public UnityEvent OnDiscoveryStarted = new();

        [Tooltip("Fired when QuickPlay() resolves. True = this machine became the host, false = it joined a server.")]
        public UnityEvent<bool> OnQuickPlayResolved = new();

        private NetworkDiscovery _discovery;
        private readonly Dictionary<long, ServerResponse> _servers = new();
        private double _lastRefreshTime;
        private bool _quickPlayActive;
        private double _quickPlayStartedAt;
        private bool _advertising;
        private bool _searching;
        private bool _wasServerActive;
        private bool _wasNetworkActive;

        /// <summary>Currently discovered servers.</summary>
        public IReadOnlyDictionary<long, ServerResponse> DiscoveredServers => _servers;

        /// <summary>Number of discovered servers.</summary>
        public int ServerCount => _servers.Count;

        /// <summary>
        ///     True while this machine answers LAN discovery requests for its running server through this component.
        ///     It turns false when the server stops, when advertising fails (the port is taken, or the platform has
        ///     no UDP broadcast) and when the <see cref="NetworkDiscovery"/> component is disabled. Mirror's own
        ///     headless start-up advertising is not reported until a server session starts.
        /// </summary>
        public bool IsAdvertising => _advertising;

        /// <summary>
        ///     True while this machine is searching the LAN for servers through this component: after
        ///     <see cref="StartDiscovery"/>, <see cref="QuickPlay"/> or an automatic refresh. It turns false on
        ///     <see cref="StopDiscovery"/>, when advertising starts, when a session starts (Mirror stops the search
        ///     requests then), and when the <see cref="NetworkDiscovery"/> component is disabled.
        /// </summary>
        public bool IsSearching => _searching;

        /// <summary>Start advertising automatically when a server or host session starts.</summary>
        public bool AutoAdvertiseOnHost
        {
            get => _autoAdvertiseOnHost;
            set => _autoAdvertiseOnHost = value;
        }

        /// <summary>Keep searching for servers while no session is running.</summary>
        public bool AutoDiscoverOnClient
        {
            get => _autoDiscoverOnClient;
            set => _autoDiscoverOnClient = value;
        }

        private void Awake()
        {
            _discovery = GetComponent<NetworkDiscovery>();
            if (_discovery == null)
            {
                NetworkDiagnostics.LogError("[NeoNetworkDiscovery] Missing NetworkDiscovery component.", this);
                return;
            }

            _discovery.OnServerFound.AddListener(OnDiscoveredServer);
        }

        private void OnDestroy()
        {
            if (_discovery != null)
            {
                _discovery.OnServerFound.RemoveListener(OnDiscoveredServer);
            }
        }

        private void Start()
        {
            _wasServerActive = NetworkServer.active;
            _wasNetworkActive = NeoNetworkState.IsNetworkActive;
            if (_autoAdvertiseOnHost && NetworkServer.active)
            {
                StartAdvertising();
            }
            else if (_autoDiscoverOnClient && !NeoNetworkState.IsNetworkActive)
            {
                StartDiscovery();
            }
        }

        private void Update()
        {
            double now = Time.unscaledTimeAsDouble;
            if (_quickPlayActive && _hostIfNoneFoundAfter > 0f
                                 && now - _quickPlayStartedAt >= _hostIfNoneFoundAfter)
            {
                ResolveQuickPlayAsHost();
                return;
            }

            // WHY: Mirror's OnDisable/OnDestroy close the discovery sockets without telling this wrapper.
            if ((_advertising || _searching) && (_discovery == null || !_discovery.isActiveAndEnabled))
            {
                _advertising = false;
                _searching = false;
            }

            // WHY: edge-triggered so it works with any NetworkManager (NeoLobbyManager is a NetworkRoomManager)
            // and a deliberate StopDiscovery() while hosting is not undone on the next frame.
            bool serverActive = NetworkServer.active;
            if (serverActive != _wasServerActive)
            {
                _wasServerActive = serverActive;
                if (serverActive && _autoAdvertiseOnHost)
                {
                    StartAdvertising();
                }
            }

            // WHY: an advertised server that stopped would keep answering LAN requests with a dead address.
            if (_advertising && !serverActive)
            {
                StopDiscovery();
            }

            // WHY: Mirror's StartDiscovery keeps broadcasting a request every few seconds until StopDiscovery, and
            // stops by itself only once a client is connected. A session started any other way (a lobby join, a
            // bootstrap, a dedicated server with advertising off, a client still connecting) would keep searching.
            bool networkActive = NeoNetworkState.IsNetworkActive;
            if (networkActive != _wasNetworkActive)
            {
                _wasNetworkActive = networkActive;
                if (networkActive && !_advertising && _discovery != null)
                {
                    _discovery.StopDiscovery();
                }

                if (networkActive)
                {
                    _searching = false;
                }
            }

            // WHY: refresh only while idle. A connected client re-broadcasting every interval wasted the LAN and
            // wiped the server list the UI was showing.
            if (_discovery != null && _autoDiscoverOnClient && !networkActive
                && NetworkDiscovery.SupportedOnThisPlatform)
            {
                if (now - _lastRefreshTime > _refreshInterval)
                {
                    _lastRefreshTime = now;
                    _servers.Clear();
                    _discovery.StartDiscovery();
                    _searching = true;
                }
            }
        }

        /// <summary>
        ///     One-button LAN quick play: searches for servers; joins the first one found
        ///     (when auto-join is on) or becomes the host after the configured timeout.
        /// </summary>
        [Button]
        public void QuickPlay()
        {
            if (NeoNetworkState.IsNetworkActive)
            {
                return;
            }

            _quickPlayActive = true;
            _quickPlayStartedAt = Time.unscaledTimeAsDouble;
            StartDiscovery();
        }

        /// <summary>Cancels a pending QuickPlay() without joining or hosting.</summary>
        public void CancelQuickPlay()
        {
            _quickPlayActive = false;
        }

        private void ResolveQuickPlayAsHost()
        {
            _quickPlayActive = false;
            StopDiscovery();
            NetworkManager manager = NetworkManager.singleton;
            if (manager == null)
            {
                NetworkDiagnostics.LogWarning("[NeoNetworkDiscovery] QuickPlay cannot host: no NetworkManager.", this);
                return;
            }

            // WHY: StartHost is hidden (new) on NeoNetworkManager; calling it through a NetworkManager reference
            // skipped the scene player template and transport preparation.
            if (manager is NeoNetworkManager neoManager)
            {
                neoManager.StartAsHost();
            }
            else
            {
                manager.StartHost();
            }

            if (_autoAdvertiseOnHost && NetworkServer.active)
            {
                // WHY: advertise right away; marking the start as seen keeps Update from trying a second time.
                _wasServerActive = true;
                StartAdvertising();
            }

            OnQuickPlayResolved?.Invoke(true);
        }

        /// <summary>
        ///     Start advertising this server on LAN. Happens automatically when a session starts while
        ///     <see cref="AutoAdvertiseOnHost"/> is on; a repeated call while advertising does nothing.
        ///     A failure (the discovery port is taken by another server on this machine, or the platform has no
        ///     UDP broadcast) is logged as a warning and leaves <see cref="IsAdvertising"/> false.
        /// </summary>
        [Button]
        public void StartAdvertising()
        {
            if (_discovery == null || _advertising)
            {
                return;
            }

            // WHY: Mirror's AdvertiseServer stops a running search before it opens the server socket.
            _searching = false;
            try
            {
                _discovery.AdvertiseServer();
            }
            catch (Exception exception)
            {
                // WHY: forced, not gated: it replaces Mirror's unhandled exception and fires at most once per
                // server start (the auto path is edge-triggered) or per explicit call.
                NetworkDiagnostics.LogWarning(
                    $"[NeoNetworkDiscovery] LAN advertising could not start: {exception.Message}", this, true);
                return;
            }

            _advertising = true;
            OnAdvertisingStarted?.Invoke();
        }

        /// <summary>
        ///     Start searching for servers on LAN. Stops advertising (Mirror uses one discovery socket set). Does
        ///     nothing on a platform without UDP broadcast (WebGL), where <see cref="IsSearching"/> stays false.
        /// </summary>
        [Button]
        public void StartDiscovery()
        {
            if (_discovery == null)
            {
                return;
            }

            // WHY: Mirror throws PlatformNotSupportedException here; Start() calls this with Auto Discover On Client
            // on, so every WebGL scene load threw from Start.
            if (!NetworkDiscovery.SupportedOnThisPlatform)
            {
                NetworkDiagnostics.LogWarning("[NeoNetworkDiscovery] LAN discovery is not supported on this platform.",
                    this);
                return;
            }

            _servers.Clear();
            _advertising = false;
            _searching = false;
            _discovery.StartDiscovery();
            _searching = true;
            OnDiscoveryStarted?.Invoke();
        }

        /// <summary>Stop searching and advertising.</summary>
        [Button]
        public void StopDiscovery()
        {
            _advertising = false;
            _searching = false;
            if (_discovery == null)
            {
                return;
            }

            _discovery.StopDiscovery();
        }

        /// <summary>Connect to a discovered server by its address.</summary>
        public void ConnectToServer(string address)
        {
            StopDiscovery();
            NetworkManager manager = NetworkManager.singleton;
            if (manager == null)
            {
                NetworkDiagnostics.LogWarning("[NeoNetworkDiscovery] Cannot connect: no NetworkManager.", this);
                return;
            }

            if (manager is NeoNetworkManager neoManager)
            {
                neoManager.StartAsClient(address);
                return;
            }

            manager.networkAddress = address;
            manager.StartClient();
        }

        /// <summary>Connect to the first discovered server (convenience for quick-join).</summary>
        [Button]
        public void ConnectToFirstServer()
        {
            foreach (KeyValuePair<long, ServerResponse> kvp in _servers)
            {
                Uri uri = kvp.Value.uri;
                ConnectToDiscoveredServer(uri);
                return;
            }

            NetworkDiagnostics.LogWarning("[NeoNetworkDiscovery] No servers found to connect to.", this);
        }

        private void OnDiscoveredServer(ServerResponse info)
        {
            long serverId = info.serverId;
            bool isNew = !_servers.ContainsKey(serverId);
            _servers[serverId] = info;

            if (isNew)
            {
                OnServerFound?.Invoke(info.uri.Host);
            }

            OnServerListUpdated?.Invoke(_servers.Count);

            if (_quickPlayActive && _autoJoinFirstFound)
            {
                _quickPlayActive = false;
                ConnectToDiscoveredServer(info.uri);
                OnQuickPlayResolved?.Invoke(false);
            }
        }

        private void ConnectToDiscoveredServer(Uri uri)
        {
            StopDiscovery();
            // WHY: the URI overload keeps the advertised scheme and port; the NeoNetworkManager one also keeps
            // Neo's scene-template preparation.
            NetworkManager singleton = NetworkManager.singleton;
            if (singleton == null)
            {
                NetworkDiagnostics.LogWarning("[NeoNetworkDiscovery] Cannot connect: no NetworkManager.", this);
                return;
            }

            if (singleton is NeoNetworkManager manager)
            {
                manager.StartClient(uri);
            }
            else
            {
                singleton.StartClient(uri);
            }
        }
    }
}
#endif
