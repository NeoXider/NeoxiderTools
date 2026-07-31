# NeoLobbyManager

**Purpose:** Mirror lobby manager wrapper for NeoxiderTools network samples and projects.

`NeoLobbyManager` lives under `Scripts/Network/Lobby/NeoLobbyManager.cs`. It belongs to the network/lobby layer and should coordinate connection flow, lobby player setup, and scene transitions without owning offline gameplay rules.

## Usage Notes

- Use the component only when Mirror multiplayer is installed and enabled.
- Keep reusable gameplay systems usable without the lobby manager.
- Pair it with [NeoLobbyPlayer](./NeoLobbyPlayer.md) for lobby player state.

## See Also

- [Lobby](./Lobby.md)
- [NeoNetworkDiscovery](./NeoNetworkDiscovery.md)
- [Network README](./README.md)
