# NeoLobbyPlayer

**What it is:** a lobby player (`Scripts/Network/Lobby/NeoLobbyPlayer.cs`, namespace `Neo.Network`, Mirror only)
derived from Mirror's `NetworkRoomPlayer`, with NoCode ready controls and UnityEvents. It represents
player-facing lobby state before the gameplay scene takes over.

**How to use:**
1. Create a prefab with `NetworkIdentity` and `NeoLobbyPlayer` and assign it as the **Room Player Prefab** of
   [NeoLobbyManager](./NeoLobbyManager.md).
2. Wire a Ready button to `ToggleReady()` or a toggle to `SetReady(bool)`; both act only on the local player.
3. Bind `OnReadyChanged(bool)` to the player card and `OnBecameLocalPlayer` to local-only UI.

---

## API

| Member | Description |
|--------|-------------|
| `ToggleReady()`, `SetReady(bool)` | Sends Mirror's ready command; ignored on copies that are not the local player. |
| `IsLocal`, `IsReady` | Local player flag and the replicated ready state. |
| `PlayerId` | Session-unique player id, identical on every peer: the room player's `netId` as an `int` for UI and NoCode bindings. Clients never learn the server-side connection ids of other players. |
| `ConnectionId` | Obsolete (compile warning) since 12.0.0: the old name of `PlayerId`, returning the same value. It never was a connection id; replace it with `PlayerId`. |
| `OnReadyChanged`, `OnBecameLocalPlayer`, `OnGameSceneReady` | UnityEvents for the lobby view. |

## Usage Notes

- Keep lobby state separate from durable gameplay state.
- Use typed network components for runtime gameplay synchronization.
- `NeoLobbyPresenter` resolves the current local `NeoLobbyPlayer` at click time; never wire a Ready button to a
  prefab or scene copy of this component.

## See Also

- [Lobby](./Lobby.md)
- [NeoNetworkDiscovery](./NeoNetworkDiscovery.md)
- [Network README](./README.md)
