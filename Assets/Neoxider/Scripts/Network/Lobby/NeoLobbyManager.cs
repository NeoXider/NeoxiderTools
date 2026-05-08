#if MIRROR
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Network
{
    /// <summary>
    ///     NoCode lobby manager wrapping Mirror's <see cref="NetworkRoomManager"/>.
    ///     Provides UnityEvents for all lobby lifecycle hooks.
    ///     <para>Replace your NeoNetworkManager with this when you need a lobby/ready flow.</para>
    /// </summary>
    [NeoDoc("Network/NeoLobbyManager.md")]
    [AddComponentMenu("Neoxider/Network/Neo Lobby Manager")]
    public class NeoLobbyManager : NetworkRoomManager
    {
        [Header("Lobby Settings")]
        [Tooltip("Minimum players required before the game can start.")]
        [SerializeField] private int _minPlayersToStart = 1;

        [Header("Lobby Events")]
        [Tooltip("Fired when a new player enters the room.")]
        public UnityEvent<NetworkConnectionToClient> OnPlayerJoinedRoom = new();

        [Tooltip("Fired when a player leaves the room.")]
        public UnityEvent<NetworkConnectionToClient> OnPlayerLeftRoom = new();

        [Tooltip("Fired when all players are ready and the game is about to start.")]
        public UnityEvent OnAllPlayersReady = new();

        [Tooltip("Fired when the game scene has loaded for all players.")]
        public UnityEvent OnGameSceneLoaded = new();

        [Tooltip("Fired when returning from game to lobby.")]
        public UnityEvent OnReturnedToLobby = new();

        [Tooltip("Fired on the server when a client connects. String = player display name.")]
        public UnityEvent<string> OnPlayerConnectedInfo = new();

        [Tooltip("Current number of players in the room.")]
        public UnityEvent<int> OnPlayerCountChanged = new();

        /// <summary>Current player count in room.</summary>
        public int PlayerCount => roomSlots.Count;

        /// <summary>Are all players ready?</summary>
        public bool AllReady => AreAllPlayersReady();

        // ────────────────────── Mirror Overrides ──────────────────────

        public override void OnRoomServerPlayersReady()
        {
            if (roomSlots.Count < _minPlayersToStart) return;

            OnAllPlayersReady?.Invoke();
            base.OnRoomServerPlayersReady();
        }

        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn,
            GameObject roomPlayer, GameObject gamePlayer)
        {
            return true;
        }

        public override void OnRoomServerSceneChanged(string sceneName)
        {
            base.OnRoomServerSceneChanged(sceneName);
            if (sceneName == GameplayScene)
            {
                OnGameSceneLoaded?.Invoke();
            }
        }

        public override void OnRoomServerConnect(NetworkConnectionToClient conn)
        {
            base.OnRoomServerConnect(conn);
            OnPlayerJoinedRoom?.Invoke(conn);
            OnPlayerCountChanged?.Invoke(roomSlots.Count);
            OnPlayerConnectedInfo?.Invoke($"Player_{conn.connectionId}");
        }

        public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnRoomServerDisconnect(conn);
            OnPlayerLeftRoom?.Invoke(conn);
            OnPlayerCountChanged?.Invoke(roomSlots.Count);
        }

        public override void OnRoomClientEnter()
        {
            base.OnRoomClientEnter();
        }

        public override void OnRoomClientExit()
        {
            base.OnRoomClientExit();
            OnReturnedToLobby?.Invoke();
        }

        // ────────────────────── Public API ──────────────────────

        /// <summary>Start as host (server + client). Wire to a button OnClick.</summary>
        [Button]
        public void HostLobby()
        {
            StartHost();
        }

        /// <summary>Join a lobby by address. Wire to a button OnClick.</summary>
        public void JoinLobby(string address)
        {
            networkAddress = address;
            StartClient();
        }

        /// <summary>Leave the lobby / stop hosting.</summary>
        [Button]
        public void LeaveLobby()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                StopHost();
            else if (NetworkClient.isConnected)
                StopClient();
            else if (NetworkServer.active)
                StopServer();
        }

        /// <summary>Check if enough players and all are ready.</summary>
        public bool CanStartGame()
        {
            return roomSlots.Count >= _minPlayersToStart && AreAllPlayersReady();
        }

        private bool AreAllPlayersReady()
        {
            if (roomSlots.Count == 0) return false;
            foreach (var slot in roomSlots)
            {
                if (slot == null) continue;
                if (!slot.readyToBegin) return false;
            }
            return true;
        }
    }
}
#endif
