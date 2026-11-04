using UnityEngine;
#if MIRROR
using System.Collections.Generic;
using Mirror;
#endif

namespace Neo.Network
{
    /// <summary>
    ///     Abstract base class for NoCode networked components.
    ///     Provides shared infrastructure so subclasses don't duplicate boilerplate:
    ///     <list type="bullet">
    ///         <item><see cref="isNetworked"/> toggle (Rule 1)</item>
    ///         <item>Command rate-limiting (<see cref="RateLimitCheck"/>)</item>
    ///         <item>Late-join template (<see cref="ApplyNetworkState"/>)</item>
    ///         <item>Dispatch helpers (<see cref="DispatchToNetwork"/>)</item>
    ///     </list>
    ///     <para>Without Mirror, this is a plain MonoBehaviour.</para>
    /// </summary>
    [NeoDoc("Network/NeoNetworkComponent.md")]
    public abstract class NeoNetworkComponent :
#if MIRROR
        NetworkBehaviour,
#else
        MonoBehaviour,
#endif
        INeoOptionalNetworked
    {
        [Header("Networking")]
        [Tooltip("If true, state changes are replicated across the network. If false, component works locally.")]
        public bool isNetworked = false;

        /// <inheritdoc />
        bool INeoOptionalNetworked.IsNetworked => isNetworked;

#if MIRROR
        // WHY: NegativeInfinity - first RateLimitCheck must not treat t=0 as "within 0.05s of last=0".
        private double _lastCmdTime = double.NegativeInfinity;

        /// <summary>Minimum interval between Commands (seconds). Override to customize per-component.</summary>
        protected virtual float NetworkRateLimit => 0.05f;

        /// <summary>
        ///     Returns <c>true</c> if the command should be rejected (too frequent).
        ///     Call at the start of every <c>[Command]</c> method.
        /// </summary>
        protected bool RateLimitCheck()
        {
            double now = Time.unscaledTimeAsDouble;
            if (now - _lastCmdTime < NetworkRateLimit)
            {
                return true;
            }

            _lastCmdTime = now;
            return false;
        }

        /// <summary>
        ///     Size at which <see cref="RateLimitCheck(NetworkConnectionToClient)"/> drops expired per-connection
        ///     entries before adding a new peer, so a long-running server does not keep one entry per historical
        ///     connection id.
        /// </summary>
        internal const int PerConnectionPruneThreshold = 32;

        private static readonly List<int> s_expiredConnectionIds = new();
        private Dictionary<int, double> _lastCmdTimePerConnection;

        /// <summary>
        ///     Per-connection variant for commands declared with <c>requiresAuthority = false</c> on
        ///     shared scene objects: one spamming client cannot silently starve legitimate commands
        ///     from other clients. Falls back to the instance-wide check for host/local calls.
        /// </summary>
        protected bool RateLimitCheck(NetworkConnectionToClient sender)
        {
            if (sender == null || sender == NetworkServer.localConnection)
            {
                return RateLimitCheck();
            }

            double now = Time.unscaledTimeAsDouble;
            float limit = NetworkRateLimit;
            _lastCmdTimePerConnection ??= new Dictionary<int, double>();
            if (_lastCmdTimePerConnection.TryGetValue(sender.connectionId, out double last))
            {
                if (now - last < limit)
                {
                    return true;
                }
            }
            else if (_lastCmdTimePerConnection.Count >= PerConnectionPruneThreshold)
            {
                PruneExpiredConnections(now, limit);
            }

            _lastCmdTimePerConnection[sender.connectionId] = now;
            return false;
        }

        // WHY: an entry older than the limit rejects nothing, so removing it is indistinguishable from keeping it.
        private void PruneExpiredConnections(double now, float limit)
        {
            s_expiredConnectionIds.Clear();
            foreach (KeyValuePair<int, double> entry in _lastCmdTimePerConnection)
            {
                if (now - entry.Value >= limit)
                {
                    s_expiredConnectionIds.Add(entry.Key);
                }
            }

            for (int i = 0; i < s_expiredConnectionIds.Count; i++)
            {
                _lastCmdTimePerConnection.Remove(s_expiredConnectionIds[i]);
            }

            s_expiredConnectionIds.Clear();
        }

        /// <summary>
        ///     Override to apply server-authoritative SyncVar state when a late-joining client connects.
        ///     Called automatically from <see cref="OnStartClient"/> for non-server clients.
        /// </summary>
        protected virtual void ApplyNetworkState() { }

        /// <summary>
        ///     Late-join hook: applies synced state on newly connected clients.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isNetworked && !isServer)
            {
                ApplyNetworkState();
            }
        }

        /// <summary>
        ///     Helper: returns <c>true</c> if this call should be dispatched to the server via Command
        ///     (i.e. we are a pure client, not the server). Caller should <c>return</c> after sending Cmd.
        /// </summary>
        protected bool ShouldDispatchToServer()
        {
            return isNetworked && NeoNetworkState.IsClient && !NeoNetworkState.IsServer;
        }

        /// <summary>
        ///     Helper: returns <c>true</c> if the server should broadcast an RPC after applying locally.
        /// </summary>
        protected bool ShouldBroadcastRpc()
        {
            return isNetworked && NeoNetworkState.IsServer;
        }
#else
        // WHY: Offline stubs so subclasses compile without #if MIRROR everywhere.
        protected bool RateLimitCheck() => false;
        protected virtual void ApplyNetworkState() { }
        protected bool ShouldDispatchToServer() => false;
        protected bool ShouldBroadcastRpc() => false;
#endif
    }
}
