# Multiplayer Integration (Neo.Network)

The **Neo.Network** module is a seamless network wrapper built on top of **Mirror Networking**.
The library supplies optional network adapters around reusable gameplay systems. Keep game rules
callable from C# and choose explicit authority and synchronization for the state that needs networking.

The library is kept to a minimal set of settings. If Mirror isn't installed in the project, the whole library compiles down to plain MonoBehaviour components.

---

## 1. How it works (with no extra setup)

You never need to write `#if MIRROR` in your own logic. The library's architecture solves synchronization through abstract components:

| NeoxiderTools component | With Mirror installed | Without Mirror (solo game) |
|-------------------------|------------------------|--------------------------------------|
| `NeoNetworkComponent` | Base class: isNetworked, rate-limiting, late-join template | Plain `MonoBehaviour` |
| `NetworkSingleton<T>` | Inherits `NetworkBehaviour`, supports `[SyncVar]` | Inherits `MonoBehaviour`, works as a regular script |
| `NetworkReactivePropertyBridge` | Applies values from a `[SyncVar]` hook or server update to a reactive property | Applies values and notifies local listeners |
| `NeoNetworkManager` | Wraps NetworkManager: start/stop, guaranteed handshake, per-connection events | A regular script (inactive) |
| `NeoNetworkBootstrap` | Command-line start-up (`-host`, `-server`, `-client`), headless dedicated server | Parses intent; network start returns false and raises no started event |
| `Neo.Network.Realtime` | Snapshots, interpolation, prediction, events for realtime games | Pure parts work, Mirror parts absent |
| `NetworkPropertySync` | Syncs any field via reflection (Float/Int/Bool/String/Vector3) | No-op |
| `NetworkActionRelay` | Multi-channel network UnityEvent broadcast (void/float/string) | A plain local event call |
| `NetworkContextActionRelay` | Contextual actions: `Trigger(Collider)` / `Trigger()` + target inside the networked player (no template reference) | Local resolve, no networking |
| `NetworkOwnerFilter` | Filters by role (LocalPlayer/Server/Everyone) | Always passes (solo = allowed) |
| `NeoNetworkDiscovery` | LAN server discovery (wraps Mirror NetworkDiscovery) | N/A (requires Mirror) |
| `NeoLobbyManager` | Lobby + ready-checks (wraps Mirror NetworkRoomManager) | N/A (requires Mirror) |
| `NeoLobbyPlayer` | A lobby player with NoCode readiness | N/A (requires Mirror) |
| `NeoNetworkState` | Static IsServer/IsClient/IsHost/CanMutateState checks | Returns safe defaults |

### Falling back to single-player (resilience)
If you're building a solo game, just remove the Mirror package. Every bit of your code that uses `NetworkSingleton<T>` automatically becomes a `MonoBehaviour`.

---

## 2. Setting up a Host and Clients (default flow)

Building a multiplayer lobby is simple — it works right out of the box.
The **Host** acts as both server and client (it plays the game and processes other players' logic at the same time).

### Scene setup
1. Create an empty scene object and add the `NeoNetworkManager` component.
2. Add the `Telepathy Transport` component (Mirror's default transport).
3. For a NoCode project, keep the player right in the scene: add a `NetworkIdentity` to it, enable **Use Scene Player Template** on `NeoNetworkManager`, and assign that object as **Scene Player Template**. Leave **Player Prefab** empty.

> [!NOTE]
> Use a regular Mirror **Player Prefab** only if the player doesn't depend on scene-level NoCode references. For an Inspector/UnityEvent workflow, the recommended path is the scene player template.

### Connecting
Server startup can be triggered from C# code or purely No-Code (a UI button → UnityEvent):

#### Host (game creator)
Just call the start method:
```csharp
using Mirror;
using Neo.Network;

NeoNetworkManager manager = (NeoNetworkManager)NetworkManager.singleton;
manager.StartAsHost();
```
*This automatically makes the player a Host. Its client connects locally to its own server.*

#### Client (joining)
```csharp
manager.StartAsClient("127.0.0.1");        // or the host's LAN IP
manager.StartAsClient("10.0.0.5", 7778);   // address and port of a PortTransport
```

#### Start / stop cheat sheet

| You want | Call |
|----------|------|
| Host (server + local client) | `StartAsHost()` |
| Client | `StartAsClient()`, `StartAsClient(string address)`, `StartAsClient(string address, ushort port)` |
| Dedicated server | `StartAsServer()` |
| Stop whatever is running | `StopNetwork()` |

> [!NOTE]
> `StartHost()` / `StartClient()` / `StartServer()` / `StopHost()` / `StopClient()` are Mirror's own methods. They still work, but the `StartAs...` and `StopNetwork()` methods are the documented surface. `NeoNetworkManager` has no `Singleton` property; Mirror's static is the lower-case `NetworkManager.singleton`.

> [!TIP]
> `NeoNetworkManager` exposes ready-made public methods `StartAsHost()`, `StartAsClient()`, `StartAsServer()` and `StopNetwork()` that you can wire **directly to `OnClick()` buttons** in a Unity Canvas without a single line of code!

After connecting, the manager completes the handshake (`Ready`, then `AddPlayer`), reports server-side per-connection events (`OnServerClientConnectedEvent`, `OnServerPlayerReadyEvent`, `OnServerClientDisconnectedEvent`) and the client's own player spawn (`OnLocalPlayerSpawnedEvent`), and wakes scene `NetworkIdentity` objects on every peer. See [NeoNetworkManager](./NeoNetworkManager.md).

For LAN menus, wire `NeoNetworkDiscovery.QuickPlay()` or `ConnectToFirstServer()` to a button.
Discovered joins retain the advertised URI's scheme and port. `NeoLobbyManager.LeaveLobby()` also
cancels a connection attempt before it connects. See [discovery](./NeoNetworkDiscovery.md) and
[lobby control](./NeoLobbyManager.md).

#### Dedicated server and command line

For a game with a dedicated server (`Game.exe -batchmode -nographics -server -port 7777`, `Game.exe -client -address host`) add [`NeoNetworkBootstrap`](./NeoNetworkBootstrap.md). For realtime games (arenas, .io, shooters) read the [Realtime IO Guide](./Realtime_IO_Guide.md).

---

## 3. Genres this architecture supports

NeoxiderTools' architecture is designed for Server-Authoritative flows (the server trusts only itself). You can easily build the following genres:

### 1. Co-op RPGs / survival games (Valheim, Diablo)
* **How it's implemented:**
  Keep combat and progression in their runtime APIs, validate player requests on the server, and
  replicate accepted state through explicit adapters, SyncVars or snapshots.
* **Why it fits:** Local rules remain reusable; authoritative validation belongs to the game's network adapter.

### 2. Session-based arena shooters (Quake, CS:GO-style modes)
* **How it's implemented:**
  The server checks pickups and spawns projectiles with `NeoNetworkSpawner`. Choose a transform
  synchronizer or a snapshot stream for movement and dedicated fields/events for weapon state.
* **Why it fits:** Spawning, snapshot interpolation and typed events can be composed independently.

### 3. Party games (Among Us, Fall Guys)
* **How it's implemented:**
  A lever calls a validated server action. A state holder stores whether the door is open, and
  a relay or typed event supplies transient presentation feedback.
* **Why it fits:** Existing quest/state-machine APIs stay independent of the optional network wrapper;
  durable state can also initialize late joiners.

---

## 4. Tests and reliability

NeoxiderTools ships EditMode contract tests and PlayMode integration tests using an in-memory
`DummyTransport`, including host/player lifecycle and dedicated-server replication regressions.
Run the relevant tests after changing network code; their presence alone is not a validation result.

---

## 5. NoCode multiplayer for any mechanic

With the `NetworkActionRelay`, **`NetworkContextActionRelay`**, and `NetworkOwnerFilter` components you can build multiplayer **without a single line of code**:

### Example: personal pickup (a child object on the joining player)
1. On the trigger: `PhysicsEvents3D` (isNetworked=true), `OnTriggerEnter → NetworkContextActionRelay.Trigger(Collider)` (dynamic argument).
2. `NetworkContextActionRelay`: Context = **Event Argument**, Root = **Network Identity In Parents**, Target = **Child By Name** `Sphere`, Action = **Set Active** true, Scope = **All Clients**.
3. Result: `Sphere` is enabled **on the specific player** whose collider entered the trigger, not on the scene template object.

### Example: doors / levers
1. On the lever: `InteractiveObject` (isNetworked=true), `OnInteract → NetworkActionRelay.Trigger()`
2. `NetworkActionRelay` → Channel "open", scope=AllClients → `onTriggered → Animator.SetBool("isOpen", true)`
3. Result: any player pulls the lever → everyone sees the door animation.

### Example: server-side item pickup
1. `PhysicsEvents3D.OnTriggerEnter → NetworkOwnerFilter.Filter()` (ServerOnly)
2. `onAllowed → InventoryComponent.AddItem()` + `Destroy(gameObject)`
3. Result: only the server processes the item, no duplicates.

### Example: global score
1. `Counter (isNetworked=true)` in the scene — a shared variable for everyone.
2. `PhysicsEvents3D.OnTriggerEnter → Counter.Add(1)` — a Cmd to the server → Rpc to everyone.
3. A late-joining client sees the current value via `[SyncVar]`.

> [!TIP]
> SyncVar-backed state supports late joiners; transient action relays do not replay earlier events.
> `OwnerToServer` property/reactive sync applies accepted values to dedicated-server targets and
> checks its configured networking option, direction and payload type. Game-specific limits still
> belong in an authoritative gameplay API. From C#, apply accepted values directly and use
> `NetworkReactivePropertyBridge.SetFromNetwork` for reactive notifications. See
> [NetworkPropertySync](./NetworkPropertySync.md), [NetworkReactiveSync](./NetworkReactiveSync.md)
> and [NoCode Network Spec](NoCode_Network_Spec.md).

## See also
- [NeoNetworkManager](NeoNetworkManager.md)
- [NeoNetworkBootstrap](NeoNetworkBootstrap.md)
- [Realtime IO Guide](Realtime_IO_Guide.md)
- [NeoNetworkTelemetry](NeoNetworkTelemetry.md)
- [NetworkSingleton](NetworkSingleton.md)
- [NetworkActionRelay](NetworkActionRelay.md)
- [NetworkContextActionRelay](NetworkContextActionRelay.md)
- [NetworkOwnerFilter](NetworkOwnerFilter.md)
- [NoCode Network Spec](NoCode_Network_Spec.md)

## Lobby on Neo.Pages (recipe)

For a persistent plain-UI lobby, use [NeoLobbyPresenter](Lobby.md#persistent-lobby-ui): wire address,
Host/Join/Ready/Leave methods and changed view outputs through serialized UnityEvents. Room and
game player references must be correctly typed prefab assets. The same presenter methods are callable
from C#; no page-navigation package is required for that path.

11.0.0 also validates sync member/accessor types before caching, retries a temporarily null reactive
instance, and uses unscaled read/command/discovery intervals. Unknown authority integers reject even
trusted contexts; use one of the three named [contract policies](Contracts.md). Existing serialized
fields and named policies remain intact; choose matching sync types rather than implicit coercions.

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
