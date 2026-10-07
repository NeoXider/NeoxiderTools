# Neo Network Manager

**What it is:** a `NetworkManager` wrapper (`Scripts/Network/Core/NeoNetworkManager.cs`, namespace `Neo.Network`) that owns the whole Mirror session of a game: it starts and stops host / client / dedicated server, guarantees the client handshake, reports per-connection server events and the local player spawn, wakes scene `NetworkIdentity` objects on every peer, and keeps the Scene Player Template flow for no-code players. Without Mirror installed the component compiles to an empty stub and the game runs solo.

**How to use:**
1. Add `NeoNetworkManager` and a Transport (Telepathy, KCP, SimpleWeb, ...) to a scene object.
2. Assign a **Player Prefab** (or enable **Use Scene Player Template**).
3. Start a session from a UI button (`UnityEvent` -> `StartAsHost()` / `StartAsClient()`) or from code (see the cheat sheet below).
4. Subscribe to the events you need (`OnLocalPlayerSpawnedEvent`, `OnServerPlayerReadyEvent`, ...).
5. For a game with a command line (`-host`, `-server`, `-client`), add [`NeoNetworkBootstrap`](./NeoNetworkBootstrap.md) next to it instead of writing your own start-up code.

---

## Start / stop cheat sheet

The real surface is `StartAsHost()`, `StartAsClient()`, `StartAsServer()` and `StopNetwork()`. Mirror's own `StartHost()` / `StartClient()` / `StartServer()` are hidden by the manager (they additionally prepare the scene player template), so they still work, but the `StartAs...` methods are the ones to wire to buttons.

| You want | Call | Notes |
|----------|------|-------|
| Host (server + local client) | `manager.StartAsHost()` | The host's own loopback connection is a normal connection with `conn is LocalConnectionToClient`. |
| Client | `manager.StartAsClient()` | Connects to `networkAddress`. |
| Client to an address | `manager.StartAsClient("10.0.0.5")` | Sets `networkAddress` first. |
| Client to an address and port | `manager.StartAsClient("10.0.0.5", 7778)` | Sets the port of any `PortTransport` (Telepathy, KCP, SimpleWeb, Multiplex). |
| Dedicated server (no local player) | `manager.StartAsServer()` | Normally started by [`NeoNetworkBootstrap`](./NeoNetworkBootstrap.md) from `-server`. |
| Stop whatever is running | `manager.StopNetwork()` | Safe when nothing runs. Picks `StopHost` / `StopServer` / `StopClient`. |
| Ask what runs | `manager.IsServer`, `IsClient`, `IsHost` or `NeoNetworkState.IsServer` ... | The static `NeoNetworkState` works without `#if MIRROR`. |

```csharp
using Neo.Network;
using UnityEngine;

public class MatchMenu : MonoBehaviour
{
    [SerializeField] private NeoNetworkManager _network;

    public void Host() => _network.StartAsHost();
    public void Join(string address) => _network.StartAsClient(address);
    public void Leave() => _network.StopNetwork();
}
```

A session can be restarted in one process (`StartAsHost()`, `StopNetwork()`, `StartAsHost()`): events fire again, scene objects are woken again, and handlers kept in [`NetClientHandlers` / `NetServerHandlers`](./Realtime/NetMessaging.md) re-register themselves.

---

## The client handshake

A Mirror server only sends spawned objects to **ready** connections, and only bursts the spawn list once the connection **owns a player object**. A client that skips `Ready` or `AddPlayer` connects "successfully" and then silently receives nothing; a broadcast that reaches a ready but playerless connection arrives before its handlers exist and Mirror disconnects the client for an unknown message id.

`NeoNetworkManager` completes the client side for you (`Handshake Mode`):

| Mode | Behavior |
|------|----------|
| **Auto** (default) | `Ready` is always sent. `AddPlayer` is sent when Mirror's *Auto Create Player* is on or a scene player template is used, exactly once per connection, also after a server scene change. This is Mirror's own behavior made reliable (no double `AddPlayer`). |
| **Always** | `Ready` and `AddPlayer` are always sent, even with *Auto Create Player* off. Use it when the game builds its player in an overridden `OnServerAddPlayer` (no Player Prefab) but still needs the connection to own an object so broadcasts reach it. |
| **Manual** | Nothing is added to Mirror's stock behavior. Pick it when the game drives the handshake by hand (character select, lobby). |

If `AddPlayer` arrives and there is neither a Player Prefab nor a Scene Player Template, the manager logs a gated warning instead of throwing from inside Mirror's message loop; an overridden `OnServerAddPlayer` is unaffected.

To check whether a message sent to a connection will actually be dispatched, use `manager.IsConnectionSpawned(conn)` or the static `NeoNetworkState.IsConnectionSpawned(conn)` (`conn.isReady && conn.identity != null`). [`NetReadyBroadcast`](./Realtime/NetMessaging.md) sends only to such connections.

---

## Events

All events exist twice: a `UnityEvent` for the inspector / no-code wiring and a C# `event` for code.

### Client side

| UnityEvent | C# event | When |
|------------|----------|------|
| `OnClientConnectedEvent` | | The client connected (after the handshake calls were made). |
| `OnClientDisconnectedEvent` | | The client disconnected. |
| `OnLocalPlayerSpawnedEvent` | `LocalPlayerSpawned` | The client's own player object exists (`NetworkClient.localPlayer`), and again after every respawn. Raised at the end of the frame in which the spawn message was processed. |

`manager.IsLocalPlayerSpawned` reads the same state.

### Server side (per connection)

| UnityEvent | C# event | When |
|------------|----------|------|
| `OnServerStartedEvent` | | The server started listening. |
| `OnServerStoppedEvent` | | The server shut down. |
| `OnServerClientConnectedEvent(conn)` | `ServerClientConnected` | A client connected (authenticated, when an authenticator is used). The host's loopback connection is included. |
| `OnServerClientReadyEvent(conn)` | `ServerClientReady` | The client sent `Ready`: it now receives spawned objects. |
| `OnServerPlayerReadyEvent(conn)` | `ServerPlayerReady` | The connection is ready **and** owns a player (`IsConnectionSpawned` became true). Fires once per connection, however the player was created (`OnServerAddPlayer`, `NetworkServer.AddPlayerForConnection` from game code). **This is the moment to welcome the client and start broadcasting to it.** |
| `OnServerClientDisconnectedEvent(conn)` | `ServerClientDisconnected` | A client disconnected. Raised **before** Mirror destroys its player, so `conn.identity` is still readable for cleanup. |

A throwing C# listener is logged and does not break Mirror's message loop or the other listeners.

```csharp
manager.ServerPlayerReady += conn =>
{
    int id = roster.Admit(conn);                 // allocate an id, a name, a seat ...
    NetReadyBroadcast.SendTo(conn, new Welcome { YourId = id }, Channels.Reliable);
};
manager.ServerClientDisconnected += conn => roster.Release(conn);
```

Static session events for infrastructure code: `NeoNetworkManager.ClientSessionStarted` (raised from `OnStartClient`) and `NeoNetworkManager.ServerSessionStarted` (raised from `OnStartServer`). Mirror clears every message handler when a session shuts down, and these events are how [`NetClientHandlers` / `NetServerHandlers`](./Realtime/NetMessaging.md) put theirs back before any message can arrive.

---

## Scene objects on every peer

Mirror's scene post-process **disables every scene object that carries a `NetworkIdentity`**, and only the server wakes them (`NetworkServer.SpawnObjects`). A client keeps them off until the spawn message for each one arrives, so a scene object that registers a message handler in `Awake` (a coordinator, a snapshot receiver) has no handler when the first message lands, and Mirror disconnects the client.

With **Activate Scene Objects On Start** (on by default) the manager calls `NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects()` when a client, host or server session starts, and again for every scene loaded while the session runs. Objects created at runtime (`sceneId == 0`) are never touched, and the Scene Player Template stays disabled. Activating early is safe: Mirror's client-side lookup does not care whether a scene object is active, and on the server it is exactly what `SpawnObjects` does.

Turn it off only when a scene deliberately keeps networked objects disabled for its own reasons.

---

## Fields

| Field | Description |
|------|----------|
| **Network Address** | Address the client connects to (Mirror field, `localhost` by default). |
| **Player Prefab** | Player prefab spawned on connect. Must have a `NetworkIdentity`. |
| **Auto Create Player** | Mirror's flag: add the player automatically on connect. |
| **Registered Spawnable Prefabs** | Every object the server can spawn over the network. |
| **Handshake Mode** | `Auto` / `Always` / `Manual`, see above. |
| **Activate Scene Objects On Start** | Wake scene `NetworkIdentity` objects when a session starts. |
| **Use Scene Player Template**, **Scene Player Template**, **Disable Scene Player Template** | The no-code player flow, below. |
| **Debug Lifecycle Log**, **Enable Runtime Network Logs / Warnings** | Gated diagnostics (`NetworkDiagnostics`). |

## Scene Player Template for NoCode

For NoCode projects, the player can live directly in the scene: cameras, UI, UnityEvents, and bindings already wired. Enable **Use Scene Player Template** on `NeoNetworkManager` for this.

| Field | Description |
|------|----------|
| **Use Scene Player Template** | Uses a scene object as the player template instead of a regular Mirror `Player Prefab`. |
| **Scene Player Template** | The scene object with a `NetworkIdentity` and player components. Every NoCode reference is wired on it. |
| **Disable Scene Player Template** | Disables the original template at runtime so only network copies stay active. On by default. |

How it works:

1. The scene player stays a template only.
2. `NeoNetworkManager` disables the template when the network starts.
3. When a player connects, the server temporarily clears the template's `sceneId` before cloning, creates a runtime copy without a `sceneId`, assigns a stable runtime `assetId`, and calls `NetworkServer.AddPlayerForConnection`.
4. Clients register a Mirror spawn handler with the same stable id and build their own copy from the local scene template.

Every client/build must have the same scene with the same `NeoNetworkManager` and the same `Scene Player Template` assigned. A regular `Player Prefab` still works fine when the player is a pure prefab asset with no scene-level NoCode references.

## Methods and properties

| Member | Description |
|--------|-------------|
| `StartAsHost()`, `StartAsClient()`, `StartAsClient(string address)`, `StartAsClient(string address, ushort port)`, `StartAsServer()`, `StopNetwork()` | Start / stop, see the cheat sheet. All return `void`. |
| `bool ApplyPort(ushort port)` | Sets the port of a `PortTransport`. Returns `true` when applied; `0` is ignored. |
| `bool IsServer`, `IsClient`, `IsHost` | What runs right now (`NetworkServer.active` / `NetworkClient.active`). |
| `bool IsLocalPlayerSpawned` | The client owns a spawned player object. |
| `bool IsConnectionSpawned(NetworkConnectionToClient conn)` | Ready and owns a player. |
| `NeoHandshakeMode HandshakeMode` | Runtime access to the handshake mode. |
| `bool ActivateSceneObjectsOnStart` | Runtime access to the scene-object switch. |
| `bool UseScenePlayerTemplate`, `GameObject ScenePlayerTemplate`, `string ScenePlayerTemplateSpawnId`, `bool DisableScenePlayerTemplate` | Scene player template configuration. |

## See also
- [Multiplayer Guide](./Multiplayer_Guide.md)
- [NeoNetworkBootstrap](./NeoNetworkBootstrap.md) - command line, dedicated server, headless runtime
- [Realtime IO Guide](./Realtime_IO_Guide.md) - snapshots, interpolation, prediction, events
- [NetworkSingleton](./NetworkSingleton.md)
- [Official Mirror documentation](https://mirror-networking.gitbook.io/docs)
