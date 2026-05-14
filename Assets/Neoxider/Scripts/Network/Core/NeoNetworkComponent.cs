using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
    /// <summary>
    ///     Abstract base class for NoCode networked components.
    ///     Provides shared infrastructure so subclasses don't duplicate boilerplate:
    ///     <list type="bullet">
    ///         <item><see cref="isNetworked"/> toggle (Правило 1)</item>
    ///         <item>Command rate-limiting (<see cref="RateLimitCheck"/>)</item>
    ///         <item>Late-join template (<see cref="ApplyNetworkState"/>)</item>
    ///         <item>Dispatch helpers (<see cref="DispatchToNetwork"/>)</item>
    ///     </list>
    ///     <para>Without Mirror, this is a plain MonoBehaviour.</para>
    /// </summary>
    [NeoDoc("Network/NeoNetworkComponent.md")]
    public abstract class NeoNetworkComponent :
#if MIRROR
        NetworkBehaviour
#else
        MonoBehaviour
#endif
    {
        [Header("Networking")]
        [Tooltip("If true, state changes are replicated across the network. If false, component works locally.")]
        public bool isNetworked = false;

#if MIRROR
        // NegativeInfinity: first RateLimitCheck must not treat t=0 as "within 0.05s of last=0".
        private float _lastCmdTime = float.NegativeInfinity;

        /// <summary>Minimum interval between Commands (seconds). Override to customize per-component.</summary>
        protected virtual float NetworkRateLimit => 0.05f;

        /// <summary>
        ///     Returns <c>true</c> if the command should be rejected (too frequent).
        ///     Call at the start of every <c>[Command]</c> method.
        /// </summary>
        protected bool RateLimitCheck()
        {
            if (Time.time - _lastCmdTime < NetworkRateLimit) return true;
            _lastCmdTime = Time.time;
            return false;
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
                ApplyNetworkState();
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
        // Offline stubs so subclasses compile without #if MIRROR everywhere.
        protected bool RateLimitCheck() => false;
        protected virtual void ApplyNetworkState() { }
        protected bool ShouldDispatchToServer() => false;
        protected bool ShouldBroadcastRpc() => false;
#endif
    }
}
