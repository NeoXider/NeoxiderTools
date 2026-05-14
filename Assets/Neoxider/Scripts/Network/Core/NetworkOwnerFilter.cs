using UnityEngine;
using UnityEngine.Events;

namespace Neo.Network
{
    /// <summary>
    /// Filter mode: determines which network role is allowed to pass.
    /// </summary>
    public enum OwnerFilterMode
    {
        /// <summary>Only the local player (isLocalPlayer / isOwned).</summary>
        LocalPlayerOnly = 0,

        /// <summary>Only the server (or host acting as server).</summary>
        ServerOnly = 1,

        /// <summary>Everyone (no filtering — useful as a no-op placeholder).</summary>
        Everyone = 2
    }

    /// <summary>
    ///     NoCode network role filter.
    ///     Place between a trigger (Button, PhysicsEvent, etc.) and an action to
    ///     gate execution by network role.
    ///     <para>Wire the trigger to <see cref="Filter"/>. If the current role matches
    ///     <see cref="_mode"/>, <see cref="onAllowed"/> fires; otherwise <see cref="onDenied"/> fires.</para>
    ///     <para>Without Mirror, <see cref="Filter"/> always fires <see cref="onAllowed"/>.</para>
    /// </summary>
    [NeoDoc("Network/NetworkOwnerFilter.md")]
    [AddComponentMenu("Neoxider/Network/Network Owner Filter")]
    public class NetworkOwnerFilter : NeoNetworkComponent
    {
        [Header("Filter")]
        [Tooltip("Which network role is allowed to pass.")]
        [SerializeField] private OwnerFilterMode _mode = OwnerFilterMode.LocalPlayerOnly;

        [Header("Events")]
        [Tooltip("Fired when the current role matches the filter.")]
        public UnityEvent onAllowed = new();

        [Tooltip("Fired when the current role does NOT match the filter.")]
        public UnityEvent onDenied = new();

        /// <summary>
        /// Evaluate the filter and invoke <see cref="onAllowed"/> or <see cref="onDenied"/>.
        /// Wire this to any trigger (Button OnClick, PhysicsEvent, InteractiveObject, etc.).
        /// </summary>
        [Button]
        public void Filter()
        {
            if (IsAllowed())
                onAllowed?.Invoke();
            else
                onDenied?.Invoke();
        }

        /// <summary>
        /// Returns true if the current runtime role passes the configured filter.
        /// </summary>
        public bool IsAllowed()
        {
#if MIRROR
            switch (_mode)
            {
                case OwnerFilterMode.LocalPlayerOnly:
                    return NeoNetworkState.HasAuthority(gameObject);
                case OwnerFilterMode.ServerOnly:
                    return NeoNetworkState.IsServer;
                case OwnerFilterMode.Everyone:
                    return true;
                default:
                    return true;
            }
#else
            return true;
#endif
        }
    }
}
