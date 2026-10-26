# Lobby and LAN Discovery

**What it is:** lobby/ready/discovery wrappers around Mirror's `NetworkRoomManager`, `NetworkRoomPlayer`,
`NetworkDiscovery`, plus the local `NeoLobbyPresenter` UI adapter. Path: `Scripts/Network/Lobby/`, namespace `Neo.Network`.

**How to use:**
1. Add `NeoNetworkDiscovery` + `NetworkDiscovery` to a manager object — for LAN discovery.
2. Replace `NeoNetworkManager` with `NeoLobbyManager` — if you need a lobby with ready-checks.
3. Build a prefab with `NeoLobbyPlayer` + `NetworkIdentity` — as the Room Player Prefab.
4. Wire the UI via UnityEvents (Host/Join/Ready buttons).

---

## Persistent lobby UI

Add `NeoLobbyPresenter` and assign its **Manager** (`NeoLobbyManager`). Persist these Inspector
connections in the scene (editor tooling must create persistent UnityEvent listeners, not captured
`AddListener` callbacks):

| Input / output | Connection |
|---|---|
| Address input `onValueChanged(string)` | `presenter.SetAddress(string)` |
| Host / Join buttons | `presenter.HostLobby()` / `presenter.JoinLobby()` |
| Ready / Leave buttons | `presenter.ToggleReady()` / `presenter.LeaveLobby()` |
| Ready toggle | `presenter.SetReady(bool)` |
| `OnStatusChanged(string)` | Status label's text setter |
| `OnPlayerCountTextChanged(string)` | Player-count label's text setter (`Players: n`) |
| `OnPlayerCountChanged(int)`, `OnReadyChanged(bool)` | Typed custom views |

The presenter resolves the current local `NeoLobbyPlayer` at click time; never capture a scene room
player or a prefab reference for Ready. It refreshes changed outputs at a 0.1-second unscaled cadence;
`RefreshView()` is also public. C# uses the same API: assign `presenter.Manager = manager`, call
`presenter.SetAddress(address)`, then `presenter.JoinLobby()` and `presenter.ToggleReady()`. There is no
dependency on a specific UI package in the runtime presenter.

Changed outputs publish in count → count text → ready → status order. If a listener reenters a lobby
method or `RefreshView()` and commits a changed snapshot, the remaining output stages of the older
snapshot stop. For example, `JoinLobby()` from a count listener can publish `Connecting...` without
an obsolete `Disconnected` status following it. An unchanged nested refresh preserves all remaining
ordinary stages. The fence applies between events; each UnityEvent retains its usual listener behavior.

The manager's **Room Player Prefab** must be a saved prefab asset with `NeoLobbyPlayer` and
`NetworkIdentity`. Its **Player Prefab** is a separate saved gameplay prefab with `NetworkIdentity`
(and `NeoNetworkPlayer` when desired); it is not the scene room-player object. Assign the Room and
Gameplay scene paths. The editor generator owner creates/persists this authoring layout and checks
save cancellation before replacing loaded scenes. Existing assets are not automatically rewritten.
Save/reopen button wiring and two-peer Ready/scene transition still require actual later acceptance.

## NeoNetworkDiscovery

**What it is:** a `MonoBehaviour` wrapper around Mirror's `NetworkDiscovery`. Automatically finds LAN servers and exposes UnityEvents. Path: `Scripts/Network/Lobby/NeoNetworkDiscovery.cs`.

### Fields

| Field | Type | Description |
|------|-----|----------|
| `_autoAdvertiseOnHost` | `bool` | Automatically start advertising when hosting (default true) |
| `_autoDiscoverOnClient` | `bool` | Automatically search for servers (default true) |
| `_refreshInterval` | `float` | Server-list refresh interval (seconds) |

### Methods

| Method | Returns | Description |
|-------|---------|----------|
| `StartAdvertising()` | `void` | Start advertising the server on the LAN. Call after `StartHost()`. |
| `StartDiscovery()` | `void` | Start searching for servers. |
| `StopDiscovery()` | `void` | Stop searching. |
| `ConnectToServer(string)` | `void` | Connect to a server at the given address. |
| `ConnectToFirstServer()` | `void` | Connect to the first server found. |

### Events

| Event | Type | Description |
|---------|-----|----------|
| `OnServerFound` | `UnityEvent<string>` | A new server was found (string = IP address) |
| `OnServerListUpdated` | `UnityEvent<int>` | The server list was updated (int = count) |
| `OnAdvertisingStarted` | `UnityEvent` | The host started advertising itself |
| `OnDiscoveryStarted` | `UnityEvent` | The client started searching |

### Properties

| Property | Type | Description |
|----------|-----|----------|
| `ServerCount` | `int` | Number of servers found |
| `DiscoveredServers` | `IReadOnlyDictionary` | Dictionary of discovered servers |

---

## NeoLobbyManager

**What it is:** a `NetworkRoomManager` subclass. Manages a lobby with ready-checks and the transition between the Room scene and the Game scene. Path: `Scripts/Network/Lobby/NeoLobbyManager.cs`.

### Fields

| Field | Type | Description |
|------|-----|----------|
| `_minPlayersToStart` | `int` | Minimum players required to start (default 1) |

### Methods

| Method | Returns | Description |
|-------|---------|----------|
| `HostLobby()` | `void` | Create a lobby (Host). Wire to a button. |
| `JoinLobby(string)` | `void` | Join a lobby at the given address. |
| `LeaveLobby()` | `void` | Leave the lobby / stop hosting. |
| `CanStartGame()` | `bool` | Returns `true` if there are enough players and all are ready. |

### Events

| Event | Type | Description |
|---------|-----|----------|
| `OnPlayerJoinedRoom` | `UnityEvent<NetworkConnectionToClient>` | A player joined the room |
| `OnPlayerLeftRoom` | `UnityEvent<NetworkConnectionToClient>` | A player left |
| `OnAllPlayersReady` | `UnityEvent` | Every player is ready, the game is starting |
| `OnGameSceneLoaded` | `UnityEvent` | The gameplay scene finished loading |
| `OnReturnedToLobby` | `UnityEvent` | Returned to the lobby |
| `OnPlayerCountChanged` | `UnityEvent<int>` | The player count changed |
| `OnPlayerConnectedInfo` | `UnityEvent<string>` | Info about a newly connected player |

### Properties

| Property | Type | Description |
|----------|-----|----------|
| `PlayerCount` | `int` | Current player count |
| `AllReady` | `bool` | Whether everyone is ready |

---

## NeoLobbyPlayer

**What it is:** a `NetworkRoomPlayer` subclass. A lobby player with NoCode readiness and UnityEvents. Path: `Scripts/Network/Lobby/NeoLobbyPlayer.cs`.

### Methods

| Method | Returns | Description |
|-------|---------|----------|
| `ToggleReady()` | `void` | Toggle readiness. Wire to a button. |
| `SetReady(bool)` | `void` | Explicitly set readiness. |

### Events

| Event | Type | Description |
|---------|-----|----------|
| `OnReadyChanged` | `UnityEvent<bool>` | Readiness state changed |
| `OnBecameLocalPlayer` | `UnityEvent` | This object became the local player |
| `OnGameSceneReady` | `UnityEvent` | The gameplay scene is ready |

### Properties

| Property | Type | Description |
|----------|-----|----------|
| `IsLocal` | `bool` | Is this the local player? |
| `IsReady` | `bool` | Is this player ready? |
| `ConnectionId` | `int` | Connection ID |

---

## Example: LAN Party Game

```
Scene: MainMenu
├── NeoNetworkDiscovery
│   ├── OnServerFound → ServerList.AddEntry()
│   └── Auto Discover On Client = true
├── Button "Host" → NeoLobbyManager.HostLobby()
└── Button "Join" → NeoNetworkDiscovery.ConnectToFirstServer()

Scene: Lobby (Room Scene)
├── NeoLobbyManager (Gameplay Scene = "Game", Min Players = 2)
├── For each player: NeoLobbyPlayer prefab
│   ├── Button "Ready" → ToggleReady()
│   └── OnReadyChanged → UpdatePlayerCard()
└── NeoLobbyManager.OnAllPlayersReady → show "Starting..."

Scene: Game (Gameplay Scene)
└── Standard gameplay with NeoNetworkPlayer
```

## Quick start: demo scene

Use the Editor generator to automatically build a fully configured demo scene:

**Menu:** `Neoxider → Network → Create Lobby Demo Scene`

The generator creates:
- `NeoLobbyManager` + `KcpTransport` + `NetworkDiscovery` + `NeoNetworkDiscovery`
- A Room Player prefab (`NeoLobbyPlayer` + `NetworkIdentity`)
- A UI panel with Host / Join / Ready / Leave buttons
- Automatic event wiring (player count, status, discovery)
- An EventSystem

After generating:
1. Save the scene (a dialog appears automatically)
2. In the `NeoLobbyManager` Inspector, set **Gameplay Scene** (the scene shown after the lobby)
3. Add both scenes to Build Settings
4. Run — the Host button creates a lobby, Join connects to one

---

## See also
- [Multiplayer Guide](Multiplayer_Guide.md) — main guide
- [NeoNetworkManager](NeoNetworkManager.md) — base manager (no lobby)
- [NoCode Network Spec](NoCode_Network_Spec.md) — Rule 10
