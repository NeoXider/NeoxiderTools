# NeoLobbyPlayer

**Purpose:** lobby player component for Mirror lobby flows.

`NeoLobbyPlayer` lives under `Scripts/Network/Lobby/NeoLobbyPlayer.cs`. It represents player-facing lobby state before the runtime game scene takes over.

## Usage Notes

- Keep lobby state separate from durable gameplay state.
- Use typed network components for runtime gameplay synchronization.
- Pair it with [NeoLobbyManager](./NeoLobbyManager.md) for lobby orchestration.

## See Also

- [Lobby](./Lobby.md)
- [NeoNetworkDiscovery](./NeoNetworkDiscovery.md)
- [Network README](./README.md)
