# Network

Mirror-based multiplayer helpers for NeoxiderTools. The module keeps package gameplay code usable in offline projects while enabling network synchronization when Mirror is installed.

## Main entry points

| Component / file | Docs |
|------------------|------|
| `Scripts/Network/Core/NeoNetworkComponent.cs` | [NeoNetworkComponent](./NeoNetworkComponent.md) |
| `Scripts/Network/Core/NeoNetworkManager.cs` | [NeoNetworkManager](./NeoNetworkManager.md) |
| `Scripts/Network/Core/NetworkActionRelay.cs` | [NetworkActionRelay](./NetworkActionRelay.md) |
| `Scripts/Network/Core/NetworkContextActionRelay.cs` | [NetworkContextActionRelay](./NetworkContextActionRelay.md) |
| `Scripts/Network/Core/NetworkOwnerFilter.cs` | [NetworkOwnerFilter](./NetworkOwnerFilter.md) |
| `Scripts/Network/Core/NetworkPropertySync.cs` | [NetworkPropertySync](./NetworkPropertySync.md) |
| `Scripts/Network/Core/NetworkSingleton.cs` | [NetworkSingleton](./NetworkSingleton.md) |
| `Scripts/Network/Player/NeoNetworkPlayer.cs` | [NeoNetworkPlayer](./NeoNetworkPlayer.md) |
| `Scripts/Network/Spawner/NeoNetworkSpawner.cs` | [NeoNetworkSpawner](./NeoNetworkSpawner.md) |
| `Scripts/Network/Lobby/*.cs` | [Lobby](./Lobby.md), [NeoNetworkDiscovery](./NeoNetworkDiscovery.md), [NeoLobbyManager](./NeoLobbyManager.md), [NeoLobbyPlayer](./NeoLobbyPlayer.md) |

## Guides

| Page | Purpose |
|------|---------|
| [Multiplayer Guide](./Multiplayer_Guide.md) | Setup flow for Mirror, `NeoNetworkManager`, scene-player templates, and common no-code sync patterns. |
| [NoCode Network Spec](./NoCode_Network_Spec.md) | Rules for building networked no-code components. |

## Diagnostics

Runtime `Log` and `Warning` output in package code goes through `NetworkDiagnostics` and is disabled by default. Enable `NetworkDiagnostics.RuntimeLogsEnabled` or `NetworkDiagnostics.RuntimeWarningsEnabled` only while debugging; component-level verbose toggles (`Debug Lifecycle Log`, `Verbose Logging`) still print explicitly requested diagnostics.

## Usage notes

- Install Mirror only when the project needs multiplayer.
- Keep offline gameplay paths working; network components should bridge state, not own the game rule.
- Prefer explicit ownership checks for player actions.
- Use `NetworkPropertySync` for simple state, and dedicated networked components for durable or security-sensitive game state.

## Related docs

- [Rpg](../Rpg/README.md)
- [StateMachine](../StateMachine/README.md)
- [Russian Network docs](../../Docs/Network/README.md)
