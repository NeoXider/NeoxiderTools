#if MIRROR
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Network
{
    /// <summary>
    ///     NoCode lobby player wrapping Mirror's <see cref="NetworkRoomPlayer"/>.
    ///     Provides UnityEvents and inspector-friendly ready/unready controls.
    ///     <para>Assign as Room Player Prefab in <see cref="NeoLobbyManager"/>.</para>
    /// </summary>
    [NeoDoc("Network/NeoLobbyPlayer.md")]
    [CreateFromMenu("Neoxider/Network/Neo Lobby Player")]
    [AddComponentMenu("Neoxider/Network/Neo Lobby Player")]
    public class NeoLobbyPlayer : NetworkRoomPlayer
    {
        [Header("Player Events")] [Tooltip("Fired when this player's ready state changes. Bool = isReady.")]
        public UnityEvent<bool> OnReadyChanged = new();

        [Tooltip("Fired when this player becomes the local player.")]
        public UnityEvent OnBecameLocalPlayer = new();

        [Tooltip("Fired when the game scene is loaded for this player.")]
        public UnityEvent OnGameSceneReady = new();

        /// <summary>Is this the local player?</summary>
        public bool IsLocal => isLocalPlayer;

        /// <summary>Is this player ready?</summary>
        public bool IsReady => readyToBegin;

        /// <summary>
        ///     Session-unique player id, identical on every peer: the room player's <c>netId</c> as an int for UI
        ///     and NoCode bindings. A client never knows the server-side connection ids of other players.
        /// </summary>
        public int PlayerId => (int)netId;

        /// <summary>Old name of <see cref="PlayerId" />; despite the name it never was a connection id.</summary>
        [System.Obsolete("ConnectionId returns the room player's netId, not a connection id. Use PlayerId.")]
        public int ConnectionId => PlayerId;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            OnBecameLocalPlayer?.Invoke();
        }

        public override void ReadyStateChanged(bool oldReady, bool newReady)
        {
            base.ReadyStateChanged(oldReady, newReady);
            OnReadyChanged?.Invoke(newReady);
        }

        public override void OnClientEnterRoom()
        {
            base.OnClientEnterRoom();
        }

        public override void OnClientExitRoom()
        {
            base.OnClientExitRoom();
        }

        /// <summary>Toggle ready state. Wire to a button OnClick.</summary>
        [Button]
        public void ToggleReady()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            CmdChangeReadyState(!readyToBegin);
        }

        /// <summary>Set ready state explicitly. Wire to VisualToggle or similar.</summary>
        public void SetReady(bool ready)
        {
            if (!isLocalPlayer)
            {
                return;
            }

            CmdChangeReadyState(ready);
        }
    }
}
#endif
