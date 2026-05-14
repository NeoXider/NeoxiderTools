using UnityEngine;
using UnityEngine.Events;

#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
    /// <summary>
    /// Scope of a network action channel — controls who receives the action.
    /// </summary>
    public enum NetworkActionScope
    {
        /// <summary>Server executes + RPC to all clients.</summary>
        AllClients = 0,

        /// <summary>Only the server executes the action (no RPC).</summary>
        ServerOnly = 1,

        /// <summary>RPC to all clients except the sender.</summary>
        OthersOnly = 2
    }

    /// <summary>
    /// A named channel that carries a networked action with optional payloads.
    /// </summary>
    [System.Serializable]
    public class NetworkActionChannel
    {
        [Tooltip("Human-readable name for this channel (for TriggerByName).")]
        public string channelName;

        [Tooltip("Who receives this action.")]
        public NetworkActionScope scope = NetworkActionScope.AllClients;

        [Space]
        [Tooltip("Fired when the action is triggered (no payload).")]
        public UnityEvent onTriggered = new();

        [Tooltip("Fired when the action is triggered with a float payload.")]
        public UnityEvent<float> onTriggeredFloat = new();

        [Tooltip("Fired when the action is triggered with a string payload.")]
        public UnityEvent<string> onTriggeredString = new();
    }

    /// <summary>
    ///     Multi-channel NoCode network action relay.
    ///     Allows broadcasting any UnityEvent across the network by wiring a
    ///     local trigger (Button OnClick, PhysicsEvent, etc.) to <see cref="Trigger(int)"/>.
    ///     <para>Each channel can have a different <see cref="NetworkActionScope"/>
    ///     and carries typed payloads (void, float, string).</para>
    ///     <para>Without Mirror this component fires events locally (offline fallback).</para>
    /// </summary>
    [NeoDoc("Network/NetworkActionRelay.md")]
    [AddComponentMenu("Neoxider/Network/Network Action Relay")]
    public class NetworkActionRelay : NeoNetworkComponent
    {
        [Header("Channels")]
        [Tooltip("Define one or more action channels, each with its own scope and events.")]
        [SerializeField] private NetworkActionChannel[] _channels = new NetworkActionChannel[1];

        [Header("Authority")]
        [Tooltip("Who may trigger relay channels over the network. Default None lets NoCode scene objects work without ownership.")]
        [SerializeField] private NetworkAuthorityMode _authorityMode = NetworkAuthorityMode.None;

#if MIRROR
        private float _lastCmdTime;
        private const float CmdRateLimit = 0.05f;
#endif

        /// <summary>Number of configured channels.</summary>
        public int ChannelCount => _channels?.Length ?? 0;

        /// <summary>Manual NoCode authority policy. Defaults to None.</summary>
        public NetworkAuthorityMode AuthorityMode
        {
            get => _authorityMode;
            set => _authorityMode = value;
        }

        // ────────────────────── Public API (NoCode wiring) ──────────────────────

        /// <summary>Trigger channel at index (no payload). Wire from UnityEvent / Button OnClick.</summary>
        public void Trigger(int channelIndex)
        {
            if (!ValidateIndex(channelIndex)) return;
            DispatchVoid(channelIndex);
        }

        /// <summary>Trigger channel 0 (no payload). Convenience for single-channel use.</summary>
        public void Trigger() => Trigger(0);

        /// <summary>Trigger channel at index with a float payload.</summary>
        public void TriggerFloat(float value) => TriggerFloatAt(0, value);

        /// <summary>Trigger channel at index with a float payload.</summary>
        public void TriggerFloatAt(int channelIndex, float value)
        {
            if (!ValidateIndex(channelIndex)) return;
            DispatchFloat(channelIndex, value);
        }

        /// <summary>Trigger channel at index with a string payload.</summary>
        public void TriggerString(string value) => TriggerStringAt(0, value);

        /// <summary>Trigger channel at index with a string payload.</summary>
        public void TriggerStringAt(int channelIndex, string value)
        {
            if (!ValidateIndex(channelIndex)) return;
            DispatchString(channelIndex, value);
        }

        /// <summary>Trigger channel by name (no payload).</summary>
        public void TriggerByName(string channelName)
        {
            int idx = FindChannelIndex(channelName);
            if (idx >= 0) DispatchVoid(idx);
        }

        // ────────────────────── Dispatch Logic ──────────────────────

        private void DispatchVoid(int idx)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsClientOnly)
                {
                    CmdTriggerVoid(idx);
                    return;
                }

                if (NeoNetworkState.IsServer)
                {
                    DispatchVoidOnServer(idx, NetworkServer.localConnection);
                    return;
                }
            }
#endif
            // Offline fallback
            _channels[idx].onTriggered?.Invoke();
        }

        private void DispatchFloat(int idx, float value)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsClientOnly)
                {
                    CmdTriggerFloat(idx, value);
                    return;
                }

                if (NeoNetworkState.IsServer)
                {
                    DispatchFloatOnServer(idx, value, NetworkServer.localConnection);
                    return;
                }
            }
#endif
            _channels[idx].onTriggeredFloat?.Invoke(value);
        }

        private void DispatchString(int idx, string value)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsClientOnly)
                {
                    CmdTriggerString(idx, value);
                    return;
                }

                if (NeoNetworkState.IsServer)
                {
                    DispatchStringOnServer(idx, value, NetworkServer.localConnection);
                    return;
                }
            }
#endif
            _channels[idx].onTriggeredString?.Invoke(value);
        }

        // ────────────────────── Mirror Cmd / Rpc ──────────────────────

#if MIRROR
        private bool AuthorizedSender(NetworkConnectionToClient sender) =>
            NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode);

        private void DispatchVoidOnServer(int idx, NetworkConnectionToClient sender)
        {
            var ch = _channels[idx];
            if (ch.scope == NetworkActionScope.ServerOnly)
            {
                ch.onTriggered?.Invoke();
                return;
            }

            bool skipHostLocalRpc = sender == NetworkServer.localConnection && NeoNetworkState.IsClient;
            if (ch.scope == NetworkActionScope.AllClients && (!NeoNetworkState.IsClient || skipHostLocalRpc))
            {
                ch.onTriggered?.Invoke();
            }

            if (ch.scope == NetworkActionScope.OthersOnly)
            {
                TargetTriggerVoidForOthers(sender, idx, ShouldSkipHostSender(sender));
                return;
            }

            RpcTriggerVoid(idx, skipHostLocalRpc);
        }

        private void DispatchFloatOnServer(int idx, float value, NetworkConnectionToClient sender)
        {
            var ch = _channels[idx];
            if (ch.scope == NetworkActionScope.ServerOnly)
            {
                ch.onTriggeredFloat?.Invoke(value);
                return;
            }

            bool skipHostLocalRpc = sender == NetworkServer.localConnection && NeoNetworkState.IsClient;
            if (ch.scope == NetworkActionScope.AllClients && (!NeoNetworkState.IsClient || skipHostLocalRpc))
            {
                ch.onTriggeredFloat?.Invoke(value);
            }

            if (ch.scope == NetworkActionScope.OthersOnly)
            {
                TargetTriggerFloatForOthers(sender, idx, value, ShouldSkipHostSender(sender));
                return;
            }

            RpcTriggerFloat(idx, value, skipHostLocalRpc);
        }

        private void DispatchStringOnServer(int idx, string value, NetworkConnectionToClient sender)
        {
            var ch = _channels[idx];
            if (ch.scope == NetworkActionScope.ServerOnly)
            {
                ch.onTriggeredString?.Invoke(value);
                return;
            }

            bool skipHostLocalRpc = sender == NetworkServer.localConnection && NeoNetworkState.IsClient;
            if (ch.scope == NetworkActionScope.AllClients && (!NeoNetworkState.IsClient || skipHostLocalRpc))
            {
                ch.onTriggeredString?.Invoke(value);
            }

            if (ch.scope == NetworkActionScope.OthersOnly)
            {
                TargetTriggerStringForOthers(sender, idx, value, ShouldSkipHostSender(sender));
                return;
            }

            RpcTriggerString(idx, value, skipHostLocalRpc);
        }

        private void TargetTriggerVoidForOthers(NetworkConnectionToClient sender, int idx, bool skipHostSender)
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (IsSenderConnection(connection, sender, skipHostSender))
                {
                    continue;
                }

                TargetTriggerVoid(connection, idx);
            }
        }

        private void TargetTriggerFloatForOthers(NetworkConnectionToClient sender, int idx, float value, bool skipHostSender)
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (IsSenderConnection(connection, sender, skipHostSender))
                {
                    continue;
                }

                TargetTriggerFloat(connection, idx, value);
            }
        }

        private void TargetTriggerStringForOthers(NetworkConnectionToClient sender, int idx, string value, bool skipHostSender)
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (IsSenderConnection(connection, sender, skipHostSender))
                {
                    continue;
                }

                TargetTriggerString(connection, idx, value);
            }
        }

        private static bool ShouldSkipHostSender(NetworkConnectionToClient sender)
        {
            return NeoNetworkState.IsHost && (sender == null || sender == NetworkServer.localConnection);
        }

        private static bool IsSenderConnection(NetworkConnectionToClient connection, NetworkConnectionToClient sender, bool skipHostSender)
        {
            if (connection == null)
            {
                return true;
            }

            if (sender != null && connection.connectionId == sender.connectionId)
            {
                return true;
            }

            return skipHostSender &&
                   (connection == NetworkServer.localConnection ||
                    connection.connectionId == NetworkConnection.LocalConnectionId);
        }

        [Command(requiresAuthority = false)]
        private void CmdTriggerVoid(int idx, NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            if (!ValidateIndex(idx)) return;
            if (!AuthorizedSender(sender)) return;

            DispatchVoidOnServer(idx, sender);
        }

        [Command(requiresAuthority = false)]
        private void CmdTriggerFloat(int idx, float value, NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            if (!ValidateIndex(idx)) return;
            if (!AuthorizedSender(sender)) return;

            DispatchFloatOnServer(idx, value, sender);
        }

        [Command(requiresAuthority = false)]
        private void CmdTriggerString(int idx, string value, NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            if (!ValidateIndex(idx)) return;
            if (!AuthorizedSender(sender)) return;

            DispatchStringOnServer(idx, value, sender);
        }

        [ClientRpc]
        private void RpcTriggerVoid(int idx, bool skipHostLocal)
        {
            if (isServerOnly || !ValidateIndex(idx)) return;
            if (skipHostLocal && NeoNetworkState.IsHost) return;
            _channels[idx].onTriggered?.Invoke();
        }

        [ClientRpc]
        private void RpcTriggerFloat(int idx, float value, bool skipHostLocal)
        {
            if (isServerOnly || !ValidateIndex(idx)) return;
            if (skipHostLocal && NeoNetworkState.IsHost) return;
            _channels[idx].onTriggeredFloat?.Invoke(value);
        }

        [ClientRpc]
        private void RpcTriggerString(int idx, string value, bool skipHostLocal)
        {
            if (isServerOnly || !ValidateIndex(idx)) return;
            if (skipHostLocal && NeoNetworkState.IsHost) return;
            _channels[idx].onTriggeredString?.Invoke(value);
        }

        [TargetRpc]
        private void TargetTriggerVoid(NetworkConnectionToClient target, int idx)
        {
            if (isServerOnly || !ValidateIndex(idx)) return;
            _channels[idx].onTriggered?.Invoke();
        }

        [TargetRpc]
        private void TargetTriggerFloat(NetworkConnectionToClient target, int idx, float value)
        {
            if (isServerOnly || !ValidateIndex(idx)) return;
            _channels[idx].onTriggeredFloat?.Invoke(value);
        }

        [TargetRpc]
        private void TargetTriggerString(NetworkConnectionToClient target, int idx, string value)
        {
            if (isServerOnly || !ValidateIndex(idx)) return;
            _channels[idx].onTriggeredString?.Invoke(value);
        }
#endif

        // ────────────────────── Helpers ──────────────────────

        private bool ValidateIndex(int idx)
        {
            if (_channels == null || idx < 0 || idx >= _channels.Length)
            {
                Debug.LogWarning($"[NetworkActionRelay] Invalid channel index {idx} on '{name}'.", this);
                return false;
            }
            return true;
        }

        private int FindChannelIndex(string channelName)
        {
            if (_channels == null || string.IsNullOrEmpty(channelName)) return -1;
            for (int i = 0; i < _channels.Length; i++)
            {
                if (string.Equals(_channels[i]?.channelName, channelName, System.StringComparison.Ordinal))
                    return i;
            }
            Debug.LogWarning($"[NetworkActionRelay] Channel '{channelName}' not found on '{name}'.", this);
            return -1;
        }
    }
}
