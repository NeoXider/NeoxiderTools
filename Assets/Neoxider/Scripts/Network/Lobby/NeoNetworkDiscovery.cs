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
    [AddComponentMenu("Neoxider/Network/Neo Network Discovery")]
    [RequireComponent(typeof(NetworkDiscovery))]
    public class NeoNetworkDiscovery : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Automatically start advertising when hosting.")]
        [SerializeField] private bool _autoAdvertiseOnHost = true;

        [Tooltip("Automatically start discovery when not hosting.")]
        [SerializeField] private bool _autoDiscoverOnClient = true;

        [Tooltip("How often to refresh the server list (seconds).")]
        [SerializeField] private float _refreshInterval = 2f;

        [Header("Events")]
        [Tooltip("Fired when a new server is found on the LAN. String = server address.")]
        public UnityEvent<string> OnServerFound = new();

        [Tooltip("Fired when the server list is updated. Int = total server count.")]
        public UnityEvent<int> OnServerListUpdated = new();

        [Tooltip("Fired when advertising starts (this machine is hosting).")]
        public UnityEvent OnAdvertisingStarted = new();

        [Tooltip("Fired when discovery starts (this machine is searching).")]
        public UnityEvent OnDiscoveryStarted = new();

        private NetworkDiscovery _discovery;
        private readonly Dictionary<long, ServerResponse> _servers = new();
        private float _lastRefreshTime;

        /// <summary>Currently discovered servers.</summary>
        public IReadOnlyDictionary<long, ServerResponse> DiscoveredServers => _servers;

        /// <summary>Number of discovered servers.</summary>
        public int ServerCount => _servers.Count;

        private void Awake()
        {
            _discovery = GetComponent<NetworkDiscovery>();
            if (_discovery == null)
            {
                Debug.LogError("[NeoNetworkDiscovery] Missing NetworkDiscovery component.", this);
                return;
            }
            _discovery.OnServerFound.AddListener(OnDiscoveredServer);
        }

        private void OnDestroy()
        {
            if (_discovery != null)
                _discovery.OnServerFound.RemoveListener(OnDiscoveredServer);
        }

        private void Start()
        {
            if (_autoAdvertiseOnHost && NetworkServer.active)
            {
                StartAdvertising();
            }
            else if (_autoDiscoverOnClient && !NetworkServer.active)
            {
                StartDiscovery();
            }
        }

        private void Update()
        {
            // Auto-refresh discovery periodically
            if (_discovery != null && !NetworkServer.active && _autoDiscoverOnClient)
            {
                if (Time.time - _lastRefreshTime > _refreshInterval)
                {
                    _lastRefreshTime = Time.time;
                    _servers.Clear();
                    _discovery.StartDiscovery();
                }
            }
        }

        // ────────────────────── Public API ──────────────────────

        /// <summary>Start advertising this server on LAN. Call after StartHost().</summary>
        [Button]
        public void StartAdvertising()
        {
            if (_discovery == null) return;
            _discovery.AdvertiseServer();
            OnAdvertisingStarted?.Invoke();
        }

        /// <summary>Start searching for servers on LAN.</summary>
        [Button]
        public void StartDiscovery()
        {
            if (_discovery == null) return;
            _servers.Clear();
            _discovery.StartDiscovery();
            OnDiscoveryStarted?.Invoke();
        }

        /// <summary>Stop discovery.</summary>
        [Button]
        public void StopDiscovery()
        {
            if (_discovery == null) return;
            _discovery.StopDiscovery();
        }

        /// <summary>Connect to a discovered server by its address.</summary>
        public void ConnectToServer(string address)
        {
            StopDiscovery();
            NetworkManager.singleton.networkAddress = address;
            NetworkManager.singleton.StartClient();
        }

        /// <summary>Connect to the first discovered server (convenience for quick-join).</summary>
        [Button]
        public void ConnectToFirstServer()
        {
            foreach (var kvp in _servers)
            {
                var uri = kvp.Value.uri;
                StopDiscovery();
                NetworkManager.singleton.networkAddress = uri.Host;
                NetworkManager.singleton.StartClient();
                return;
            }
            Debug.LogWarning("[NeoNetworkDiscovery] No servers found to connect to.");
        }

        // ────────────────────── Internal ──────────────────────

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
        }
    }
}
#endif
