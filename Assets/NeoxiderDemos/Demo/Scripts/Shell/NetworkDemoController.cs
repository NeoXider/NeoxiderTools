using System.Globalization;
using Neo.Network;
using Neo.Rpg.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if MIRROR
using Mirror;
#endif

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Network</b> demo scene (NetworkDemo.unity). The scene saves
    ///     the whole UI, the network objects and every persistent listener in the scene: Host / Server / Stop call
    ///     <see cref="NeoNetworkManager" /> directly, the relay buttons call <see cref="NetworkActionRelay" /> directly,
    ///     and the relay channels call this component and the shared boss on every peer. This component builds no UI:
    ///     it holds the serialized references, mirrors the session, LAN, telemetry and boss state into the labels (only
    ///     when a value changes) and implements the C# paths <see cref="JoinAddressFromField" /> (StartAsClient with an
    ///     address), <see cref="SendChatFromField" /> (TriggerStringByName) and <see cref="RequestBossHit" />
    ///     (TryGetChannelIndex once, then TriggerFloatAt).
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Network Demo")]
    public sealed class NetworkDemoController : MonoBehaviour
    {
        /// <summary>Relay channel 0 (string payload, AllClients); index 0 is also the TriggerString shortcut.</summary>
        public const string ChatChannel = "Chat";

        /// <summary>Relay channel 1 (no payload, AllClients).</summary>
        public const string PingChannel = "Ping";

        /// <summary>Relay channel 2 (no payload, OthersOnly).</summary>
        public const string WaveChannel = "Wave";

        /// <summary>Relay channel 3 (float payload, ServerOnly): the server damages the shared boss.</summary>
        public const string HitBossChannel = "Hit boss";

        /// <summary>Relay channel 4 (no payload, ServerOnly): the server restores the shared boss.</summary>
        public const string ReviveBossChannel = "Revive boss";

        private const float StatsInterval = 0.5f;

        [Header("Network objects")]
        [Tooltip("Scene NeoNetworkManager (Host / Join / Server / Stop).")]
        [SerializeField] private NeoNetworkManager _manager;

        [Tooltip("Scene relay with the Chat, Ping, Wave, Hit boss and Revive boss channels.")]
        [SerializeField] private NetworkActionRelay _relay;

        [Tooltip("Shared RpgCharacter replicated by RpgCharacterNetworkAdapter.")]
        [SerializeField] private RpgCharacter _boss;

        [Tooltip("Telemetry source of the Ping and Traffic rows (its IMGUI overlay stays off).")]
        [SerializeField] private NeoNetworkTelemetry _telemetry;

#if MIRROR
        [Tooltip("LAN discovery behind Find games, Join found and Quick Play.")]
        [SerializeField] private NeoNetworkDiscovery _discovery;
#endif

        [Header("Session")]
        [SerializeField] private TMP_Text _roleValue;
        [SerializeField] private TMP_Text _addressValue;
        [SerializeField] private TMP_Text _connectionsValue;
        [SerializeField] private TMP_Text _pingValue;
        [SerializeField] private TMP_Text _trafficValue;
        [SerializeField] private TMP_InputField _addressField;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _serverButton;
        [SerializeField] private Button _stopButton;

        [Header("LAN")]
        [SerializeField] private TMP_Text _lanValue;
        [Tooltip("Buttons that only make sense while offline (searching stops a running advertisement).")]
        [SerializeField] private Button[] _lanButtons = new Button[0];

        [Header("Relay")]
        [SerializeField] private TMP_Text _pingsValue;
        [SerializeField] private TMP_Text _lastMessageValue;
        [SerializeField] private TMP_InputField _chatField;

        [Header("Shared boss")]
        [SerializeField] private TMP_Text _bossValue;
        [SerializeField] private Image _bossFill;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private SessionRole _shownRole = SessionRole.Unknown;
        private int _shownConnections = int.MinValue;
        private string _shownHost;
        private int _shownPort = -1;
        private int _shownLanState = -1;
        private bool _bossShown;
        private bool _shownBossAwake;
        private bool _shownBossDead;
        private float _shownBossHp;
        private float _shownBossMax;
        private float _nextStatsAt;
        private int _pings;
        private int _hitChannel = -1;
        private bool _lanSearching;
        private bool _warnedUnbuilt;
        private string _lastChatMessage = string.Empty;

        private enum SessionRole
        {
            Unknown = -1,
            Offline = 0,
            Connecting = 1,
            Client = 2,
            Host = 3,
            Server = 4,
            Unavailable = 5
        }

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _log != null && _roleValue != null && _manager != null && _relay != null &&
                                    _boss != null && _pingsValue != null && _bossValue != null;

        /// <summary>'Ping' relay events this peer received.</summary>
        public int PingsReceived => _pings;

        /// <summary>Last 'Chat' relay payload this peer received.</summary>
        public string LastChatMessage => _lastChatMessage;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

#if MIRROR
            if (_manager == null)
            {
                _manager = NetworkManager.singleton as NeoNetworkManager;
            }

            if (_addressField != null && _manager != null)
            {
                _addressField.SetTextWithoutNotify(_manager.networkAddress);
            }

            Log("Offline · relay buttons fire locally · press Host to start a session");
#else
            Log("Mirror is not installed · the relay and the boss run locally (offline fallback)");
#endif
            SetText(_pingsValue, "0");
            SetText(_lastMessageValue, "-");
        }

        private void Update()
        {
            SessionRole role = CurrentRole();
            if (role != _shownRole)
            {
                _shownRole = role;
                ApplyRole(role);
            }

            RefreshConnections(role);
            RefreshAddress();
            RefreshLan(role);
            RefreshBoss();

            float now = Time.unscaledTime;
            if (now >= _nextStatsAt)
            {
                _nextStatsAt = now + StatsInterval;
                RefreshStats(role);
            }
        }

        /// <summary>Address field (On End Edit) target: the address Join and StartAsClient() connect to.</summary>
        public void SetServerAddress(string address)
        {
            string trimmed = address == null ? string.Empty : address.Trim();
#if MIRROR
            if (_manager == null)
            {
                return;
            }

            if (trimmed.Length == 0)
            {
                Log("Empty address ignored · keeping " + _manager.networkAddress);
                if (_addressField != null)
                {
                    _addressField.SetTextWithoutNotify(_manager.networkAddress);
                }

                return;
            }

            if (_manager.networkAddress == trimmed)
            {
                return;
            }

            _manager.networkAddress = trimmed;
            Log("NeoNetworkManager.networkAddress = \"" + trimmed + "\"");
#else
            if (trimmed.Length > 0)
            {
                Log("Address " + trimmed + " kept · Mirror is not installed");
            }
#endif
        }

        /// <summary>Join button target (C# path): <see cref="NeoNetworkManager.StartAsClient(string)" />.</summary>
        public void JoinAddressFromField()
        {
            if (_manager == null)
            {
                Log("No NeoNetworkManager assigned");
                return;
            }

            string address = _addressField != null ? _addressField.text.Trim() : string.Empty;
#if MIRROR
            if (address.Length == 0)
            {
                address = _manager.networkAddress;
            }
#endif
            Log("NeoNetworkManager.StartAsClient(\"" + address + "\") · C# API");
            _manager.StartAsClient(address);
        }

        /// <summary>Join found button target: connects to the first LAN game NeoNetworkDiscovery reported.</summary>
        public void JoinFirstLanServer()
        {
#if MIRROR
            if (_discovery == null)
            {
                Log("No NeoNetworkDiscovery assigned");
                return;
            }

            if (_discovery.ServerCount == 0)
            {
                Log("No LAN game found yet · Host on the other instance, then press Find games");
                return;
            }

            Log("NeoNetworkDiscovery.ConnectToFirstServer() · C# API");
            _discovery.ConnectToFirstServer();
#else
            Log("LAN discovery needs Mirror");
#endif
        }

        /// <summary>NeoNetworkDiscovery.OnDiscoveryStarted target.</summary>
        public void OnLanSearchStarted()
        {
            _lanSearching = true;
            _shownLanState = -1;
            Log("NeoNetworkDiscovery · searching the LAN");
        }

        /// <summary>NeoNetworkDiscovery.OnServerFound target (argument: the server address).</summary>
        public void OnLanServerFound(string address)
        {
            Log("LAN · found a game at " + address);
        }

        /// <summary>NeoNetworkDiscovery.OnQuickPlayResolved target (true: this instance became the host).</summary>
        public void OnQuickPlayResolved(bool hosted)
        {
            _lanSearching = false;
            _shownLanState = -1;
            Log(hosted ? "Quick Play · no LAN game found, this instance hosts" : "Quick Play · joining a LAN host");
        }

        /// <summary>'Ping' channel target (AllClients): runs on every peer.</summary>
        public void OnPingReceived()
        {
            _pings++;
            SetText(_pingsValue, _pings.ToString(CultureInfo.InvariantCulture));
            Log(NeoNetworkState.IsNetworkActive
                ? "Relay 'Ping' arrived on this peer (AllClients)"
                : "Relay 'Ping' fired locally (offline fallback)");
        }

        /// <summary>'Chat' channel target (AllClients, string payload).</summary>
        public void OnChatReceived(string message)
        {
            _lastChatMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
            string shown = _lastChatMessage.Length == 0 ? "(empty)" : _lastChatMessage;
            SetText(_lastMessageValue, shown);
            Log("Relay 'Chat' · " + shown);
        }

        /// <summary>'Wave' channel target (OthersOnly): the peer that waved does not receive it.</summary>
        public void OnWaveReceived()
        {
            Log(NeoNetworkState.IsNetworkActive
                ? "Relay 'Wave' (OthersOnly) · another peer waved at you"
                : "Relay 'Wave' fired locally (the offline fallback ignores the scope)");
        }

        /// <summary>'Hit boss' channel target (ServerOnly): logs the hit the server just applied.</summary>
        public void OnBossHitOnServer(float amount)
        {
            Log("Server · 'Hit boss' (ServerOnly) -> RpgCharacterNetworkAdapter.NetDamage(" +
                amount.ToString("0", CultureInfo.InvariantCulture) + ")");
        }

        /// <summary>'Revive boss' channel target (ServerOnly).</summary>
        public void OnBossRevivedOnServer()
        {
            Log(_boss != null && !_boss.isActiveAndEnabled
                ? "Relay 'Revive boss' fired locally · the Shared Boss sleeps until a session starts"
                : "Server · 'Revive boss' (ServerOnly) -> RpgCharacter.Restore()");
        }

        /// <summary>Send button target (C# path): <see cref="NetworkActionRelay.TriggerStringByName" />.</summary>
        public void SendChatFromField()
        {
            if (_relay == null)
            {
                Log("No NetworkActionRelay assigned");
                return;
            }

            string text = _chatField != null ? _chatField.text : string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                Log("Type a chat message first");
                return;
            }

            Log("NetworkActionRelay.TriggerStringByName(\"" + ChatChannel + "\", text) · C# API");
            _relay.TriggerStringByName(ChatChannel, text.Trim());
            ClearChatField();
        }

        /// <summary>Clears the chat field (second listener of its On Submit, after the relay call).</summary>
        public void ClearChatField()
        {
            if (_chatField != null)
            {
                _chatField.SetTextWithoutNotify(string.Empty);
            }
        }

        /// <summary>
        ///     Hit buttons target (C# path): resolves the 'Hit boss' channel once with
        ///     <see cref="NetworkActionRelay.TryGetChannelIndex" /> and triggers it by index. The channel is ServerOnly,
        ///     so only the server applies the damage and the adapter replicates the new HP to every peer.
        /// </summary>
        public void RequestBossHit(float amount)
        {
            if (_relay == null || _boss == null)
            {
                Log("Shared Boss or its relay is not assigned");
                return;
            }

            if (!_boss.isActiveAndEnabled)
            {
                Log("Shared Boss sleeps until a session starts (Mirror disables scene network objects) · press Host");
                return;
            }

            if (_hitChannel < 0 && !_relay.TryGetChannelIndex(HitBossChannel, out _hitChannel))
            {
                Log("The relay has no '" + HitBossChannel + "' channel");
                return;
            }

            Log("relay.TriggerFloatAt(" + _hitChannel.ToString(CultureInfo.InvariantCulture) + ", " +
                amount.ToString("0", CultureInfo.InvariantCulture) + ") · index from TryGetChannelIndex");
            _relay.TriggerFloatAt(_hitChannel, amount);
        }

#if MIRROR
        /// <summary>NeoNetworkManager server event: a connection joined (the host's own client included).</summary>
        public void OnServerClientConnected(NetworkConnectionToClient connection)
        {
            Log("Server · connection #" + ConnectionId(connection) + Origin(connection) + " connected");
        }

        /// <summary>NeoNetworkManager server event: a connection left.</summary>
        public void OnServerClientDisconnected(NetworkConnectionToClient connection)
        {
            Log("Server · connection #" + ConnectionId(connection) + Origin(connection) + " disconnected");
        }

        private static string ConnectionId(NetworkConnectionToClient connection)
        {
            return connection != null ? connection.connectionId.ToString(CultureInfo.InvariantCulture) : "?";
        }

        private static string Origin(NetworkConnectionToClient connection)
        {
            return NeoNetworkState.IsLocalHostConnection(connection) ? " (this host's own client)" : string.Empty;
        }
#endif

        private static SessionRole CurrentRole()
        {
#if MIRROR
            if (NetworkServer.active)
            {
                return NetworkClient.active ? SessionRole.Host : SessionRole.Server;
            }

            if (NetworkClient.isConnected)
            {
                return SessionRole.Client;
            }

            return NetworkClient.active ? SessionRole.Connecting : SessionRole.Offline;
#else
            return SessionRole.Unavailable;
#endif
        }

        private void ApplyRole(SessionRole role)
        {
            SetText(_roleValue, RoleText(role));
            bool offline = role == SessionRole.Offline;
            SetInteractable(_hostButton, offline);
            SetInteractable(_joinButton, offline);
            SetInteractable(_serverButton, offline);
            SetInteractable(_stopButton, role != SessionRole.Offline && role != SessionRole.Unavailable);
            if (_lanButtons != null)
            {
                for (int i = 0; i < _lanButtons.Length; i++)
                {
                    SetInteractable(_lanButtons[i], offline);
                }
            }

            if (!offline)
            {
                // WHY: NeoNetworkDiscovery stops searching once a session runs; the label must not keep "searching".
                _lanSearching = false;
                _shownLanState = -1;
            }
        }

        private static string RoleText(SessionRole role)
        {
            switch (role)
            {
                case SessionRole.Connecting:
                    return "Client · connecting…";
                case SessionRole.Client:
                    return "Client · connected";
                case SessionRole.Host:
                    return "Host · server + client";
                case SessionRole.Server:
                    return "Dedicated server";
                case SessionRole.Unavailable:
                    return "Mirror not installed";
                default:
                    return "Offline";
            }
        }

        private void RefreshConnections(SessionRole role)
        {
#if MIRROR
            int count = NetworkServer.active ? NetworkServer.connections.Count : -1;
#else
            int count = -1;
#endif
            if (count == _shownConnections)
            {
                return;
            }

            _shownConnections = count;
            if (count < 0)
            {
                SetText(_connectionsValue, "-");
                return;
            }

            string text = count.ToString(CultureInfo.InvariantCulture);
            SetText(_connectionsValue, role == SessionRole.Host ? text + " · incl. this host" : text);
        }

        private void RefreshAddress()
        {
#if MIRROR
            string host = _manager != null ? _manager.networkAddress : null;
            int port = _manager != null && _manager.transport is PortTransport portTransport ? portTransport.Port : 0;
#else
            string host = null;
            int port = 0;
#endif
            if (port == _shownPort && string.Equals(host, _shownHost))
            {
                return;
            }

            _shownHost = host;
            _shownPort = port;
            if (string.IsNullOrEmpty(host))
            {
                SetText(_addressValue, "-");
                return;
            }

            SetText(_addressValue, port > 0 ? host + ":" + port.ToString(CultureInfo.InvariantCulture) : host);
        }

        private void RefreshLan(SessionRole role)
        {
#if MIRROR
            if (_discovery == null || _lanValue == null)
            {
                return;
            }

            bool advertising = _discovery.IsAdvertising;
            bool searching = _lanSearching && role == SessionRole.Offline;
            int servers = _discovery.ServerCount;
            int state = (advertising ? 1 : 0) | (searching ? 2 : 0) | (servers << 2);
            if (state == _shownLanState)
            {
                return;
            }

            _shownLanState = state;
            string found = servers.ToString(CultureInfo.InvariantCulture) + " found";
            SetText(_lanValue, advertising ? "advertising this host" : searching ? "searching · " + found :
                servers > 0 ? found : "idle");
#endif
        }

        private void RefreshBoss()
        {
            if (_boss == null)
            {
                return;
            }

            bool awake = _boss.isActiveAndEnabled;
            float hp = awake ? _boss.HpValue : 0f;
            float max = awake ? _boss.MaxHpValue : 0f;
            bool dead = awake && _boss.IsDead;
            if (_bossShown && awake == _shownBossAwake && dead == _shownBossDead && Mathf.Approximately(hp, _shownBossHp) &&
                Mathf.Approximately(max, _shownBossMax))
            {
                return;
            }

            _bossShown = true;
            _shownBossAwake = awake;
            _shownBossDead = dead;
            _shownBossHp = hp;
            _shownBossMax = max;
            if (!awake)
            {
                SetText(_bossValue, "asleep · start a session");
            }
            else if (dead)
            {
                SetText(_bossValue, "defeated · press Revive");
            }
            else
            {
                SetText(_bossValue, Mathf.CeilToInt(hp).ToString(CultureInfo.InvariantCulture) + " / " +
                                    Mathf.CeilToInt(max).ToString(CultureInfo.InvariantCulture) + " HP");
            }

            if (_bossFill != null)
            {
                RectTransform fill = _bossFill.rectTransform;
                fill.anchorMax = new Vector2(awake && max > 0f ? Mathf.Clamp01(hp / max) : 0f, fill.anchorMax.y);
            }
        }

        private void RefreshStats(SessionRole role)
        {
#if MIRROR
            bool running = role == SessionRole.Client || role == SessionRole.Host || role == SessionRole.Server;
            if (_telemetry == null || !running)
            {
                SetText(_pingValue, "-");
                SetText(_trafficValue, "-");
                return;
            }

            if (role == SessionRole.Client)
            {
                SetText(_pingValue, Mathf.RoundToInt(_telemetry.RttMs).ToString(CultureInfo.InvariantCulture) + " ms");
            }
            else
            {
                int remote = NetworkServer.connections.Count - (role == SessionRole.Host ? 1 : 0);
                SetText(_pingValue, remote > 0
                    ? "worst client " + Mathf.RoundToInt(_telemetry.WorstClientRttMs).ToString(CultureInfo.InvariantCulture) + " ms"
                    : "no remote clients");
            }

            SetText(_trafficValue, "in " + Rate(_telemetry.BytesInPerSecond) + " · out " + Rate(_telemetry.BytesOutPerSecond));
#else
            SetText(_pingValue, "-");
            SetText(_trafficValue, "-");
#endif
        }

        private static string Rate(float bytesPerSecond)
        {
            return bytesPerSecond < 1024f
                ? Mathf.RoundToInt(bytesPerSecond).ToString(CultureInfo.InvariantCulture) + " B/s"
                : (bytesPerSecond / 1024f).ToString("0.0", CultureInfo.InvariantCulture) + " KB/s";
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null && selectable.interactable != interactable)
            {
                selectable.interactable = interactable;
            }
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[NetworkDemoController] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
