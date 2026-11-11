# NeoLobbyManager

**What it is:** a lobby manager (`Scripts/Network/Lobby/NeoLobbyManager.cs`, namespace `Neo.Network`, Mirror only)
derived from Mirror's `NetworkRoomManager`. It runs the room/ready flow, switches to the gameplay scene once
everyone is ready and exposes the lobby lifecycle as UnityEvents. It coordinates connection flow, lobby player
setup and scene transitions without owning offline gameplay rules.

**How to use:**
1. Use it instead of `NeoNetworkManager` on the manager object when the game needs a lobby with ready-checks.
2. Assign a **Room Player Prefab** with [NeoLobbyPlayer](./NeoLobbyPlayer.md) and a gameplay **Player Prefab**,
   plus the Room and Gameplay scenes; set **Min Players To Start**.
3. Wire Host / Join / Leave buttons to `HostLobby()`, `JoinLobby(address)` and `LeaveLobby()` (or use
   `NeoLobbyPresenter`, see [Lobby](./Lobby.md#persistent-lobby-ui)).

---

## Usage Notes

- Use the component only when Mirror multiplayer is installed and enabled.
- Keep reusable gameplay systems usable without the lobby manager.
- Pair it with [NeoLobbyPlayer](./NeoLobbyPlayer.md) for lobby player state and
  [NeoNetworkDiscovery](./NeoNetworkDiscovery.md) for LAN search; discovery advertises automatically once the
  lobby host starts.

From C#, call `HostLobby()`, `JoinLobby(address)` and `LeaveLobby()`; the same methods can be wired to
menu buttons. `LeaveLobby` also cancels a client that is still connecting, so a Cancel button works
before a transport connection completes. Read `PlayerCount`, `AllReady` and `CanStartGame()` for lobby
UI, and bind `OnPlayerJoinedRoom`, `OnPlayerLeftRoom` and `OnPlayerCountChanged` to refresh the view.
`OnAllPlayersReady` fires on the server once at least **Min Players To Start** players are ready, right before
the gameplay scene loads; `OnGameSceneLoaded` follows when the server reaches it.

## See Also

- [Lobby](./Lobby.md)
- [NeoNetworkDiscovery](./NeoNetworkDiscovery.md)
- [Network README](./README.md)
