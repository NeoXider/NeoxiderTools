# Lobby and LAN Discovery

**What it is:** three NoCode components for building a lobby, ready-checks, and automatic LAN server discovery. Wrappers around built-in Mirror components (`NetworkRoomManager`, `NetworkRoomPlayer`, `NetworkDiscovery`). Path: `Scripts/Network/Lobby/`, namespace `Neo.Network`.

**How to use:**
1. Add `NeoNetworkDiscovery` + `NetworkDiscovery` to a manager object — for LAN discovery.
2. Replace `NeoNetworkManager` with `NeoLobbyManager` — if you need a lobby with ready-checks.
3. Build a prefab with `NeoLobbyPlayer` + `NetworkIdentity` — as the Room Player Prefab.
4. Wire the UI via UnityEvents (Host/Join/Ready buttons).

---

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
