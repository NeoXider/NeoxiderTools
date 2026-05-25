# NeoLobbyManager

**What it is:** a NoCode wrapper around Mirror `NetworkRoomManager` for lobby and ready-flow scenes.

**Where:** `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyManager.cs`, menu `Neoxider/Network/Neo Lobby Manager`.

---

## Purpose

Use `NeoLobbyManager` when a multiplayer scene needs a waiting room before the gameplay scene starts. It exposes lobby lifecycle callbacks as `UnityEvent`s so UI and NoCode components can react without custom glue scripts.

## Main Events

| Event | When it fires |
|------|---------------|
| `OnPlayerJoinedRoom` | A client enters the room on the server. |
| `OnPlayerLeftRoom` | A room player disconnects or leaves. |
| `OnAllPlayersReady` | The ready check passes and the game can start. |
| `OnGameSceneLoaded` | The configured gameplay scene is reached. |
| `OnReturnedToLobby` | A client returns from game to lobby. |
| `OnPlayerCountChanged` | Room slot count changes. |

## API

| Method | Use |
|------|-----|
| `HostLobby()` | Starts host mode. Wire to a host button. |
| `JoinLobby(string address)` | Sets `networkAddress` and starts a client. |
| `LeaveLobby()` | Stops host, client, or server depending on current mode. |
| `CanStartGame()` | Returns true when enough players are present and ready. |

## Setup

1. Add `NeoLobbyManager` to the network root object.
2. Assign Mirror room/game scenes and room player prefab.
3. Set **Min Players To Start**.
4. Wire UI buttons to `HostLobby`, `JoinLobby`, and `LeaveLobby`.
5. Use `NeoLobbyPlayer` on the room player prefab for ready toggles.

## See Also

- [NeoLobbyPlayer](NeoLobbyPlayer.md)
- [NeoNetworkDiscovery](NeoNetworkDiscovery.md)
- [Multiplayer Guide](Multiplayer_Guide.md)
