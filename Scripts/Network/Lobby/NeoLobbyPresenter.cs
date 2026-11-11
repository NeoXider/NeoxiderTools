#if MIRROR
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Network
{
    /// <summary>Local lobby UI adapter; its methods and output events can be wired persistently in a scene.</summary>
    [NeoDoc("Network/Lobby.md")]
    [AddComponentMenu("Neoxider/Network/Neo Lobby Presenter")]
    public sealed class NeoLobbyPresenter : MonoBehaviour
    {
        [SerializeField] private NeoLobbyManager _manager;
        [SerializeField] private string _address = "localhost";
        [SerializeField] private UnityEvent<string> _onStatusChanged = new();
        [SerializeField] private UnityEvent<int> _onPlayerCountChanged = new();
        [SerializeField] private UnityEvent<string> _onPlayerCountTextChanged = new();
        [SerializeField] private UnityEvent<bool> _onReadyChanged = new();

        private double _nextRefresh;
        private string _lastStatus;
        private int _lastCount = -1;
        private bool? _lastReady;
        private ulong _publicationRevision;
        private NetworkIdentity _cachedLocalIdentity;
        private NeoLobbyPlayer _cachedLocalPlayer;

        /// <summary>Lobby manager to drive; resolved from <c>NetworkManager.singleton</c> when empty.</summary>
        public NeoLobbyManager Manager
        {
            get => _manager;
            set => _manager = value;
        }

        /// <summary>Address used by <see cref="JoinLobby"/>.</summary>
        public string Address => _address;
        /// <summary>Status text (Connecting..., Connected, All players ready, ...), raised on change.</summary>
        public UnityEvent<string> OnStatusChanged => _onStatusChanged;
        /// <summary>Room player count, raised on change.</summary>
        public UnityEvent<int> OnPlayerCountChanged => _onPlayerCountChanged;
        /// <summary>Room player count as <c>Players: n</c>, raised on change.</summary>
        public UnityEvent<string> OnPlayerCountTextChanged => _onPlayerCountTextChanged;
        /// <summary>Local player ready state, raised on change.</summary>
        public UnityEvent<bool> OnReadyChanged => _onReadyChanged;

        /// <summary>Sets the join address; empty or whitespace falls back to localhost.</summary>
        public void SetAddress(string address)
        {
            _address = string.IsNullOrWhiteSpace(address) ? "localhost" : address.Trim();
        }

        /// <summary>Starts hosting a lobby.</summary>
        public void HostLobby()
        {
            if (ResolveManager())
            {
                _manager.HostLobby();
                RefreshView();
            }
        }

        /// <summary>Joins the lobby at <see cref="Address"/>.</summary>
        public void JoinLobby()
        {
            if (ResolveManager())
            {
                _manager.JoinLobby(string.IsNullOrWhiteSpace(_address) ? "localhost" : _address);
                RefreshView();
            }
        }

        /// <summary>Leaves the lobby or stops hosting; also cancels a pending connect.</summary>
        public void LeaveLobby()
        {
            if (ResolveManager())
            {
                _manager.LeaveLobby();
                RefreshView();
            }
        }

        /// <summary>Toggles the ready state of the current local lobby player.</summary>
        public void ToggleReady()
        {
            NeoLobbyPlayer player = LocalPlayer();
            if (player != null)
            {
                player.ToggleReady();
            }
        }

        /// <summary>Sets the ready state of the current local lobby player.</summary>
        public void SetReady(bool ready)
        {
            NeoLobbyPlayer player = LocalPlayer();
            if (player != null)
            {
                player.SetReady(ready);
            }
        }

        private void OnEnable()
        {
            _lastStatus = null;
            _lastCount = -1;
            _lastReady = null;
            RefreshView();
        }

        private void Update()
        {
            double now = Time.unscaledTimeAsDouble;
            if (now >= _nextRefresh)
            {
                _nextRefresh = now + 0.1d;
                RefreshView();
            }
        }

        /// <summary>Publishes changed outputs now instead of waiting for the 0.1 s poll.</summary>
        public void RefreshView()
        {
            if (_manager == null)
            {
                _manager = NetworkManager.singleton as NeoLobbyManager;
            }

            int count = _manager != null ? _manager.PlayerCount : 0;
            NeoLobbyPlayer player = LocalPlayer();
            bool ready = player != null && player.IsReady;
            string status = NetworkClient.isConnecting ? "Connecting..."
                : _manager != null && NetworkServer.active && _manager.AllReady ? "All players ready"
                : NetworkClient.isConnected ? "Connected"
                : NetworkServer.active ? "Server running" : "Disconnected";

            bool countChanged = count != _lastCount;
            bool readyChanged = ready != _lastReady;
            bool statusChanged = status != _lastStatus;
            if (!countChanged && !readyChanged && !statusChanged)
            {
                return;
            }

            // WHY: only a changed snapshot supersedes an in-flight publication; unchanged reentry is harmless.
            _lastCount = count;
            _lastReady = ready;
            _lastStatus = status;
            ulong revision = ++_publicationRevision;
            if (countChanged)
            {
                _onPlayerCountChanged?.Invoke(count);
                if (revision != _publicationRevision)
                {
                    return;
                }

                _onPlayerCountTextChanged?.Invoke($"Players: {count}");
                if (revision != _publicationRevision)
                {
                    return;
                }
            }

            if (readyChanged)
            {
                _onReadyChanged?.Invoke(ready);
                if (revision != _publicationRevision)
                {
                    return;
                }
            }

            if (statusChanged) _onStatusChanged?.Invoke(status);
        }

        private bool ResolveManager()
        {
            if (_manager == null)
            {
                _manager = NetworkManager.singleton as NeoLobbyManager;
            }

            if (_manager != null)
            {
                return true;
            }

            NetworkDiagnostics.LogWarning("[NeoLobbyPresenter] Assign a NeoLobbyManager before using lobby controls.", this);
            return false;
        }

        // WHY: resolved per poll (10 Hz) and per click; the component lookup reruns only when the local player changes.
        private NeoLobbyPlayer LocalPlayer()
        {
            NetworkIdentity identity = NetworkClient.localPlayer;
            if (identity == null)
            {
                _cachedLocalIdentity = null;
                _cachedLocalPlayer = null;
                return null;
            }

            if (!ReferenceEquals(identity, _cachedLocalIdentity))
            {
                _cachedLocalIdentity = identity;
                _cachedLocalPlayer = identity.GetComponent<NeoLobbyPlayer>();
            }

            return _cachedLocalPlayer;
        }
    }
}
#endif
