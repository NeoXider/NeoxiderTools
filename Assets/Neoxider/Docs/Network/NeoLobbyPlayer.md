# NeoLobbyPlayer

**What it is:** a NoCode room-player component built on Mirror `NetworkRoomPlayer`.

**Where:** `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyPlayer.cs`, menu `Neoxider/Network/Neo Lobby Player`.

---

## Purpose

`NeoLobbyPlayer` belongs on the room player prefab used by `NeoLobbyManager`. It exposes ready state and local-player lifecycle events to UI and NoCode flows.

## Events

| Event | When it fires |
|------|---------------|
| `OnReadyChanged` | The Mirror room ready state changes. |
| `OnBecameLocalPlayer` | This room player becomes the local player. |
| `OnGameSceneReady` | Reserved for game-scene ready wiring. |

## API

| Method | Use |
|------|-----|
| `ToggleReady()` | Toggles ready state for the local room player. |
| `SetReady(bool ready)` | Sets ready state explicitly, useful for toggles. |

## Setup

1. Add `NeoLobbyPlayer` to the room player prefab.
2. Assign that prefab in `NeoLobbyManager`.
3. Wire a UI button to `ToggleReady()` or a toggle to `SetReady(bool)`.
4. Use `OnReadyChanged` to update ready indicators.

## See Also

- [NeoLobbyManager](NeoLobbyManager.md)
- [Multiplayer Guide](Multiplayer_Guide.md)
