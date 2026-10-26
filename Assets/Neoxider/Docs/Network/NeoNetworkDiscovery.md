# NeoNetworkDiscovery

**Purpose:** Mirror LAN discovery helper for lobby-oriented projects.

`NeoNetworkDiscovery` lives under `Scripts/Network/Lobby/NeoNetworkDiscovery.cs`. Use it when a local network lobby needs host discovery without making game rules depend on networking.

## Usage Notes

- Keep discovery as an optional scene/network layer.
- Route gameplay state through explicit network components after a connection is established.
- For the broader lobby flow, see [Lobby](./Lobby.md).

## C# API and scene workflow

Add `NeoNetworkDiscovery` to a scene object; its required Mirror `NetworkDiscovery` component supplies
LAN responses. Configure the active `NetworkManager` and transport before starting discovery.

| API | Scene / UnityEvent use |
|-----|-----------------------|
| `StartAdvertising()` | Host advertises its transport URI. |
| `StartDiscovery()`, `StopDiscovery()` | Start or stop searching. |
| `ConnectToFirstServer()` | Join the first discovered server using its complete URI, preserving scheme and port. |
| `ConnectToServer(string address)` | Join a manually supplied host name/IP using the transport's configured port. |
| `QuickPlay()` | Search, join the first response when enabled, or host after the configured timeout. |
| `CancelQuickPlay()` | Cancel automatic join/host resolution; call `StopDiscovery()` as well to stop searching. |

Wire a menu button to `QuickPlay` or `ConnectToFirstServer`; bind `OnServerFound` / `OnServerListUpdated`
to a server-list view and `OnQuickPlayResolved(bool becameHost)` to navigation. The automatic join path
also preserves the discovered URI. With a `NeoNetworkManager`, discovered joins call its URI overload
so scene-player template preparation remains active. From C#, the same methods are callable directly;
`DiscoveredServers` exposes the current responses and `ServerCount` their count.

QuickPlay timeout and discovery refresh use unscaled double timestamps. They keep progressing when
gameplay timeScale is zero; their configured durations remain seconds of real networking time.

## See Also

- [NeoLobbyManager](./NeoLobbyManager.md)
- [NeoLobbyPlayer](./NeoLobbyPlayer.md)
- [Network README](./README.md)
