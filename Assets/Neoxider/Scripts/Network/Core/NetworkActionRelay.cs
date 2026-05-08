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
    public class NetworkActionRelay :
#if MIRROR
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
        [Header("Channels")]
        [Tooltip("Define one or more action channels, each with its own scope and events.")]
        [SerializeField] private NetworkActionChannel[] _channels = new NetworkActionChannel[1];

#if MIRROR
        private float _lastCmdTime;
        private const float CmdRateLimit = 0.05f;
#endif

        /// <summary>Number of configured channels.</summary>
        public int ChannelCount => _channels?.Length ?? 0;

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
            if (NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsClientOnly)
                {
                    CmdTriggerVoid(idx);
                    return;
                }

                if (NeoNetworkState.IsServer)
                {
                    var ch = _channels[idx];
                    if (ch.scope != NetworkActionScope.OthersOnly)
                        ch.onTriggered?.Invoke();
                    RpcTriggerVoid(idx);
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
            if (NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsClientOnly)
                {
                    CmdTriggerFloat(idx, value);
                    return;
                }

                if (NeoNetworkState.IsServer)
                {
                    var ch = _channels[idx];
                    if (ch.scope != NetworkActionScope.OthersOnly)
                        ch.onTriggeredFloat?.Invoke(value);
                    RpcTriggerFloat(idx, value);
                    return;
                }
            }
#endif
            _channels[idx].onTriggeredFloat?.Invoke(value);
        }

        private void DispatchString(int idx, string value)
        {
#if MIRROR
            if (NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsClientOnly)
                {
                    CmdTriggerString(idx, value);
                    return;
                }

                if (NeoNetworkState.IsServer)
                {
                    var ch = _channels[idx];
                    if (ch.scope != NetworkActionScope.OthersOnly)
                        ch.onTriggeredString?.Invoke(value);
                    RpcTriggerString(idx, value);
                    return;
                }
            }
#endif
            _channels[idx].onTriggeredString?.Invoke(value);
        }

        // ────────────────────── Mirror Cmd / Rpc ──────────────────────

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdTriggerVoid(int idx, NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            if (!ValidateIndex(idx)) return;

            var ch = _channels[idx];
            // Server-side execution
            if (ch.scope != NetworkActionScope.OthersOnly)
                ch.onTriggered?.Invoke();
            // Broadcast to clients
            if (ch.scope != NetworkActionScope.ServerOnly)
                RpcTriggerVoid(idx);
        }

        [Command(requiresAuthority = false)]
        private void CmdTriggerFloat(int idx, float value, NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            if (!ValidateIndex(idx)) return;

            var ch = _channels[idx];
            if (ch.scope != NetworkActionScope.OthersOnly)
                ch.onTriggeredFloat?.Invoke(value);
            if (ch.scope != NetworkActionScope.ServerOnly)
                RpcTriggerFloat(idx, value);
        }

        [Command(requiresAuthority = false)]
        private void CmdTriggerString(int idx, string value, NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;
            if (!ValidateIndex(idx)) return;

            var ch = _channels[idx];
            if (ch.scope != NetworkActionScope.OthersOnly)
                ch.onTriggeredString?.Invoke(value);
            if (ch.scope != NetworkActionScope.ServerOnly)
                RpcTriggerString(idx, value);
        }

        [ClientRpc]
        private void RpcTriggerVoid(int idx)
        {
            if (isServerOnly || !ValidateIndex(idx)) return;
            _channels[idx].onTriggered?.Invoke();
        }

        [ClientRpc]
        private void RpcTriggerFloat(int idx, float value)
        {
            if (isServerOnly || !ValidateIndex(idx)) return;
            _channels[idx].onTriggeredFloat?.Invoke(value);
        }

        [ClientRpc]
        private void RpcTriggerString(int idx, string value)
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
