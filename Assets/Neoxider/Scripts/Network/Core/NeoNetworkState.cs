using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Network
{
    /// <summary>
    /// Static helper to check network state globally safely.
    /// Falls back to true when Mirror is uninstalled (Solo mode).
    /// </summary>
    public static class NeoNetworkState
    {
        /// <summary>
        /// True if running as a Server or Host. In Solo mode (no Mirror), always true.
        /// </summary>
        public static bool IsServer
        {
            get
            {
#if MIRROR
                return NetworkServer.active;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// True if running as a Client. In Solo mode (no Mirror), always true.
        /// </summary>
        public static bool IsClient
        {
            get
            {
#if MIRROR
                return NetworkClient.active;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// Checks if the local player has authority over the given GameObject.
        /// Useful for LocalPlayerOnly execution filtering.
        /// In Solo mode (no Mirror), always true.
        /// </summary>
        public static bool HasAuthority(GameObject obj)
        {
#if MIRROR
            if (obj != null && obj.TryGetComponent(out NetworkIdentity identity))
            {
                return identity.isLocalPlayer || identity.isOwned;
            }
            return false; // In multiplayer, if it lacks NetworkIdentity, NO ONE has authority to trigger LocalPlayer events from it.
#else
            return true;
#endif
        }
    }
}
