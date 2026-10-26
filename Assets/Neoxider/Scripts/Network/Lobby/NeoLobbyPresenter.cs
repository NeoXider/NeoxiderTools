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

        public NeoLobbyManager Manager
        {
            get => _manager;
            set => _manager = value;
        }

        public string Address => _address;
        public UnityEvent<string> OnStatusChanged => _onStatusChanged;
        public UnityEvent<int> OnPlayerCountChanged => _onPlayerCountChanged;
        public UnityEvent<string> OnPlayerCountTextChanged => _onPlayerCountTextChanged;
        public UnityEvent<bool> OnReadyChanged => _onReadyChanged;

        public void SetAddress(string address)
        {
            _address = string.IsNullOrWhiteSpace(address) ? "localhost" : address.Trim();
        }

        public void HostLobby()
        {
            if (ResolveManager())
            {
                _manager.HostLobby();
                RefreshView();
            }
        }

        public void JoinLobby()
        {
            if (ResolveManager())
            {
                _manager.JoinLobby(string.IsNullOrWhiteSpace(_address) ? "localhost" : _address);
                RefreshView();
            }
        }

        public void LeaveLobby()
        {
            if (ResolveManager())
            {
                _manager.LeaveLobby();
                RefreshView();
            }
        }

        public void ToggleReady()
        {
            NeoLobbyPlayer player = LocalPlayer();
            if (player != null)
            {
                player.ToggleReady();
            }
        }

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

            // Only a changed snapshot supersedes an in-flight publication; unchanged reentry is harmless.
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

        private static NeoLobbyPlayer LocalPlayer()
        {
            return NetworkClient.localPlayer != null
                ? NetworkClient.localPlayer.GetComponent<NeoLobbyPlayer>() : null;
        }
    }
}
#endif
