# Multiplayer Guide

Complete multiplayer guide is not yet fully mirrored to EN.

- [Russian guide](../../Docs/Network/Multiplayer_Guide.md)

## Lobby on Neo.Pages (recipe)

1. **Lobby Page** (`UIPage` + PageId `PageLobby`): player list = one-row prefab + `VerticalLayoutGroup`;
   spawn a row per `NeoLobbyManager.OnPlayerJoinedRoom`, remove on `OnPlayerLeftRoom`
   (or rebuild on `OnPlayerCountChanged`).
2. **Ready button** → `NeoLobbyPlayer.ToggleReady()` on the local player; bind the row highlight
   to `OnReadyChanged`.
3. **Start**: `NeoLobbyManager.OnAllPlayersReady` → enable the host's Start button →
   host calls the room-manager scene change; `OnGameSceneLoaded` → `PM.I.ChangePageByName("PageGame")`.
4. **Names**: add `NetworkPlayerName` to the player object and bind `OnNameChanged` to the row's TMP label.
5. **Quick play**: menu button → `NeoNetworkDiscovery.QuickPlay()`; `OnQuickPlayResolved` →
   open `PageLobby`.
