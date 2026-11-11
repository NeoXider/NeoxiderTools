# NeoNetworkDiscovery

**What it is:** a `MonoBehaviour` LAN discovery helper around Mirror's `NetworkDiscovery` for lobby-oriented
projects (`Scripts/Network/Lobby/NeoNetworkDiscovery.cs`, namespace `Neo.Network`, Mirror only). It advertises a
running server, keeps a list of discovered servers and offers one-button QuickPlay.

**How to use:**
1. Add `NeoNetworkDiscovery` next to the network manager; its required Mirror `NetworkDiscovery` is added with it.
2. Keep **Auto Advertise On Host** on to answer LAN searches whenever a server or host session starts.
3. Keep **Auto Discover On Client** on to search while no session runs, and bind `OnServerFound` /
   `OnServerListUpdated` to a server list.
4. Wire a menu button to `QuickPlay()` or `ConnectToFirstServer()`.

---

## Usage Notes

- Keep discovery as an optional scene/network layer; gameplay rules must not depend on it.
- Route gameplay state through explicit network components after a connection is established.
- For the broader lobby flow, see [Lobby](./Lobby.md).

## C# API and scene workflow

Configure the active `NetworkManager` and transport before starting discovery.

| API | Scene / UnityEvent use |
|-----|-----------------------|
| `StartAdvertising()` | Host advertises its transport URI. Runs automatically after a session starts with **Auto Advertise On Host**; a repeated call while advertising does nothing. |
| `StartDiscovery()`, `StopDiscovery()` | Start or stop searching. Mirror shares one socket set, so both also stop advertising. |
| `ConnectToFirstServer()` | Join the first discovered server using its complete URI, preserving scheme and port. |
| `ConnectToServer(string address)` | Join a manually supplied host name/IP using the transport's configured port. |
| `QuickPlay()` | Search, join the first response when enabled, or host after the configured timeout. |
| `CancelQuickPlay()` | Cancel automatic join/host resolution; call `StopDiscovery()` as well to stop searching. |
| `IsAdvertising`, `IsSearching`, `ServerCount`, `DiscoveredServers` | Current state for custom views (for example a "Searching..." label bound to `IsSearching`). |
| `AutoAdvertiseOnHost`, `AutoDiscoverOnClient` | Runtime switches for the two inspector toggles. |

Wire a menu button to `QuickPlay` or `ConnectToFirstServer`; bind `OnServerFound` / `OnServerListUpdated`
to a server-list view and `OnQuickPlayResolved(bool becameHost)` to navigation. The automatic join path
also preserves the discovered URI. With a `NeoNetworkManager`, discovered joins call its URI overload, and
QuickPlay hosting and `ConnectToServer` go through `StartAsHost` / `StartAsClient(address)`, so scene-player
template preparation remains active. From C#, the same methods are callable directly.

```csharp
NeoNetworkDiscovery discovery = manager.GetComponent<NeoNetworkDiscovery>();
discovery.OnServerFound.AddListener(host => serverList.Add(host));
discovery.StartDiscovery();
// later, from a list entry:
discovery.ConnectToFirstServer();
```

## Lifecycle

- **Advertising** starts once per server or host session: in `Start()` when a server is already running, or on
  the first `Update` after a session starts, with any `NetworkManager` (including `NeoLobbyManager`). A deliberate
  `StopDiscovery()` while hosting stays in effect. Advertising stops automatically once the server stops, so
  clients never find a dead address.
- **Advertising failures** do not throw: when the discovery port is already taken (two hosts on one machine) or
  the platform has no UDP broadcast (WebGL), `StartAdvertising()` logs one warning and `IsAdvertising` stays false.
  `IsAdvertising` also turns false when the `NetworkDiscovery` component is disabled or destroyed, because Mirror
  closes its sockets then. On a headless build Mirror's own `NetworkDiscovery.Start()` already advertises before
  any session runs; `IsAdvertising` reports it only once a server session starts.
- **Searching** refreshes every **Refresh Interval** seconds only while no session runs. When any session starts
  (a host, a server, a client that is still connecting, whichever component started it) the component stops
  Mirror's repeating discovery request once, unless this machine is advertising, and keeps the shown server list.
  Without that, Mirror would keep broadcasting every few seconds until a client connection completes, and on a
  server for the whole session.
- **Unsupported platforms** (no UDP broadcast, such as WebGL): `StartDiscovery()` and the automatic search do
  nothing (a gated warning) instead of throwing Mirror's `PlatformNotSupportedException` from `Start()`, and
  `IsSearching` stays false. `QuickPlay()` then only waits for its host timeout.
- **`IsSearching`** is true after `StartDiscovery()`, `QuickPlay()` or an automatic refresh, and turns false on
  `StopDiscovery()`, when advertising starts (Mirror closes the search socket first), when any session starts,
  and when the `NetworkDiscovery` component is disabled or destroyed.
- QuickPlay timeout and discovery refresh use unscaled double timestamps. They keep progressing when
  gameplay timeScale is zero; their configured durations remain seconds of real networking time.

## See Also

- [NeoLobbyManager](./NeoLobbyManager.md)
- [NeoLobbyPlayer](./NeoLobbyPlayer.md)
- [Network README](./README.md)
