# Multiplayer Integration (Neo.Network)

The **Neo.Network** module is a seamless network wrapper built on top of **Mirror Networking**.
The library's core philosophy: **your game works automatically as both single-player and multiplayer, with no code changes.**

The library is kept to a minimal set of settings. If Mirror isn't installed in the project, the whole library compiles down to plain MonoBehaviour components.

---

## 1. How it works (with no extra setup)

You never need to write `#if MIRROR` in your own logic. The library's architecture solves synchronization through abstract components:

| NeoxiderTools component | With Mirror installed | Without Mirror (solo game) |
|-------------------------|------------------------|--------------------------------------|
| `NeoNetworkComponent` | Base class: isNetworked, rate-limiting, late-join template | Plain `MonoBehaviour` |
| `NetworkSingleton<T>` | Inherits `NetworkBehaviour`, supports `[SyncVar]` | Inherits `MonoBehaviour`, works as a regular script |
| `NetworkReactiveProperty` | Syncs data from `[SyncVar]` into No-Code events | A plain UnityEvent-backed property |
| `NeoNetworkManager` | Wraps NetworkManager | A regular script (inactive) |
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
NeoNetworkManager.Singleton.StartHost();
```
*This automatically makes the player a Host. Its client connects locally to its own server.*

#### Client (joining)
```csharp
NeoNetworkManager.Singleton.networkAddress = "127.0.0.1"; // Or the host's LAN IP
NeoNetworkManager.Singleton.StartClient();
```

> [!TIP]
> `NeoNetworkManager` exposes ready-made public methods `StartHost()`, `StartClient()`, `StopHost()` that you can wire **directly to `OnClick()` buttons** in a Unity Canvas without a single line of code!

---

## 3. Genres this architecture supports

NeoxiderTools' architecture is designed for Server-Authoritative flows (the server trusts only itself). You can easily build the following genres:

### 1. Co-op RPGs / survival games (Valheim, Diablo)
* **How it's implemented:**
  Uses `RpgCharacter` (network-adapted). Hits and damage go through the server API `Damage()` / `DamageType()`. Resource, level, buff, and status state is broadcast to clients via snapshot sync and reactive properties.
* **Why it fits:** Anti-cheat by design. A client can't grant itself infinite health locally, since the math runs on the Host.

### 2. Session-based arena shooters (Quake, CS:GO-style modes)
* **How it's implemented:**
  The `InventoryManager` inventory system runs through `NetworkSingleton`. Players pick up weapons; the server checks item availability and spawns projectiles.
* **Why it fits:** Automatic Transform and weapon-state sync with no hand-written RPC calls (via `NeoNetworkSpawner`).

### 3. Party games (Among Us, Fall Guys)
* **How it's implemented:**
  Uses the `DialogueManager` and `ConditionManager` systems. A player pulls a lever in the scene, triggering a `Command` to the Server. The server flips a global condition in `ConditionManager`, and every client sees the door open.
* **Why it fits:** The whole quest/state-machine stack already runs on the `Singleton<T>` abstraction, which became network-aware.

---

## 4. Tests and reliability

NeoxiderTools ships with integration `PlayMode` tests. Spinning up a local host, spawning players, and verifying `HasServerAuthority` are all checked automatically using an in-memory transport (`DummyTransport`), keeping multiplayer stable even through aggressive refactors.

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
> Every network component has **server-side validation** (rate-limiting, `CanSpend` checks, sender) and **Late-Join sync** via SyncVar. See [NoCode Network Spec](NoCode_Network_Spec.md), Rules 8–10.

## See also
- [NeoNetworkManager](NeoNetworkManager.md)
- [NetworkSingleton](NetworkSingleton.md)
- [NetworkActionRelay](NetworkActionRelay.md)
- [NetworkContextActionRelay](NetworkContextActionRelay.md)
- [NetworkOwnerFilter](NetworkOwnerFilter.md)
- [NoCode Network Spec](NoCode_Network_Spec.md)

## Lobby on Neo.Pages (recipe)

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
