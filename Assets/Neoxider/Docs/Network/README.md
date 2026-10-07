# Network

Mirror-based multiplayer helpers for NeoxiderTools. The module keeps package gameplay code usable in offline projects while enabling network synchronization when Mirror is installed.

## Main entry points

| Component / file | Docs |
|------------------|------|
| `Scripts/Network/Core/NeoNetworkComponent.cs` | [NeoNetworkComponent](./NeoNetworkComponent.md) |
| `Scripts/Network/Core/NeoNetworkManager.cs` | [NeoNetworkManager](./NeoNetworkManager.md) |
| `Scripts/Network/Core/NetworkActionRelay.cs` | [NetworkActionRelay](./NetworkActionRelay.md) |
| `Scripts/Network/Core/NetworkContextActionRelay.cs` | [NetworkContextActionRelay](./NetworkContextActionRelay.md) |
| `Scripts/Network/Core/NetworkEventDispatcher.cs` | [NetworkEventDispatcher](../Tools/Network/NetworkEventDispatcher.md) |
| `Scripts/Network/Core/NetworkOwnerFilter.cs` | [NetworkOwnerFilter](./NetworkOwnerFilter.md) |
| `Scripts/Network/Core/NetworkPropertySync.cs` | [NetworkPropertySync](./NetworkPropertySync.md) |
| `Scripts/Network/Core/NetworkReactiveSync.cs` | [NetworkReactiveSync](./NetworkReactiveSync.md) |
| `Scripts/Network/Core/NetworkSingleton.cs` | [NetworkSingleton](./NetworkSingleton.md) |
| `Scripts/Network/Player/NeoNetworkPlayer.cs` | [NeoNetworkPlayer](./NeoNetworkPlayer.md) |
| `Scripts/Network/Player/NetworkPlayerName.cs` | [NetworkPlayerName](./NetworkPlayerName.md) |
| `Scripts/Network/Spawner/NeoNetworkSpawner.cs` | [NeoNetworkSpawner](./NeoNetworkSpawner.md) |
| `Scripts/Network/Lobby/*.cs` | [Lobby](./Lobby.md), [NeoNetworkDiscovery](./NeoNetworkDiscovery.md), [NeoLobbyManager](./NeoLobbyManager.md), [NeoLobbyPlayer](./NeoLobbyPlayer.md) |
| `Scripts/Network/Bootstrap/*.cs` | [NeoNetworkBootstrap](./NeoNetworkBootstrap.md): command line (`-host`, `-server`, `-client`, ...), dedicated server, headless runtime |
| `Scripts/Network/Realtime/*.cs` | [Realtime toolkit](./Realtime/README.md): snapshot timeline and buffer, prediction, quantisation, framing and fragmentation, handlers, event channel |
| `Scripts/Network/Telemetry/*.cs` | [NeoNetworkTelemetry](./NeoNetworkTelemetry.md): RTT, bytes per second, per-kind traffic, overlay |

## Guides

| Page | Purpose |
|------|---------|
| [Multiplayer Guide](./Multiplayer_Guide.md) | Setup flow for Mirror, `NeoNetworkManager`, scene-player templates, and common no-code sync patterns. |
| [Realtime IO Guide](./Realtime_IO_Guide.md) | Architecture of a server-authoritative realtime game: handshake, snapshots, interpolation, prediction, events, telemetry, bootstrap. |
| [NoCode Network Spec](./NoCode_Network_Spec.md) | Rules for building networked no-code components. |

## Diagnostics

Runtime `Log` and `Warning` output in package code goes through `NetworkDiagnostics` and is disabled by default. Enable `NetworkDiagnostics.RuntimeLogsEnabled` or `NetworkDiagnostics.RuntimeWarningsEnabled` only while debugging; component-level verbose toggles (`Debug Lifecycle Log`, `Verbose Logging`) still print explicitly requested diagnostics.

## Realtime games (.io, arenas, shooters)

Everything a server-authoritative realtime game needs beyond per-property sync lives in `Neo.Network.Realtime` plus the bootstrap and telemetry components: a guaranteed client handshake with per-connection server events, scene objects woken on every peer, batched and quantised world snapshots with datagram fragmentation, an adaptive interpolation clock, client prediction with input acknowledgement, a reliable typed event channel, handler registries that survive reconnects, RTT and bandwidth telemetry, and a command-line bootstrap for dedicated servers (WebGL included). Start with the [Realtime IO Guide](./Realtime_IO_Guide.md).

## Start / stop cheat sheet

| You want | Call |
|----------|------|
| Host | `NeoNetworkManager.StartAsHost()` |
| Client | `StartAsClient()`, `StartAsClient(address)`, `StartAsClient(address, port)` |
| Dedicated server | `StartAsServer()` (or `-server` with [`NeoNetworkBootstrap`](./NeoNetworkBootstrap.md)) |
| Stop | `StopNetwork()` |

Details: [NeoNetworkManager](./NeoNetworkManager.md#start--stop-cheat-sheet).

## Usage notes

- Install Mirror only when the project needs multiplayer.
- Keep offline gameplay paths working; network components should bridge state, not own the game rule.
- Prefer explicit ownership checks for player actions.
- Use `NetworkPropertySync` for simple state, and dedicated networked components for durable or security-sensitive game state.

## Related docs

- [Rpg](../Rpg/README.md)
- [StateMachine](../StateMachine/README.md)
