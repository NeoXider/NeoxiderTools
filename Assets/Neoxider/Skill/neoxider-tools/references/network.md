# Neo.Network — multiplayer (Mirror-optional)

Source: `Assets/Neoxider/Scripts/Network/` (namespace `Neo.Network`; `NetworkEventDispatcher` is `Neo.Tools`).
Networking is **Mirror-optional** — most of it is behind `#if MIRROR`. The package compiles and runs fine
without Mirror (everything falls back to a single-player "solo" mode). Verify signatures against source.

## Gating: the `MIRROR` define
`Neo.Network.asmdef` declares a version-define: `MIRROR` is set automatically when
`com.mirrornetworking.mirror` is installed. With Mirror, networked classes compile their full
`NetworkBehaviour` implementation; without it they fall back to `MonoBehaviour` stubs, and some types
(`NeoLobbyManager`, `NeoLobbyPlayer`, `NeoNetworkDiscovery`, `NeoMirrorSceneReactivator`, the
`NetworkContextActionMessage` struct) **don't exist at all** — code referencing them must itself be inside
`#if MIRROR`. Install Mirror via Package Manager (OpenUPM/Git URL); LTS 86+ expected.

**Solo-mode behavior (no Mirror):** `NetworkSingleton<T>` is a `MonoBehaviour`; `NeoNetworkState.IsServer/
IsClient/IsHost`, `CanMutateState`, `HasServerAuthority` all return `true`; `IsNetworkActive` is `false`;
`NeoNetworkSpawner.Spawn` does plain `Instantiate`.

## Core types
- **`NetworkSingleton<T>`** — networked singleton base. Same access as `Singleton<T>`: `I`/`Instance`,
  `HasInstance`, `TryGetInstance`. With Mirror it extends `NetworkBehaviour` so subclasses get `[SyncVar]`,
  `[Command]`, `[ClientRpc]`, `isServer`, `isOwned`. `HasServerAuthority` = `NetworkServer.active`. Static
  state auto-resets on domain reload.
- **`NeoNetworkManager`** — wraps Mirror `NetworkManager`. Always-compiled `UnityEvent`s:
  `OnServerStartedEvent`, `OnServerStoppedEvent`, `OnClientConnectedEvent`, `OnClientDisconnectedEvent`;
  state `IsServer/IsClient/IsHost`; control `StartAsHost()/StartAsClient()/StartAsServer()/StopNetwork()`
  (solo: log + no-op). Calls `NetworkContextActionRelay.RegisterMirrorHandlers()` on start.
- **`NeoNetworkState`** (static) — branch online/offline cleanly WITHOUT `#if MIRROR`: `IsServer`,
  `IsClient`, `IsClientOnly`, `IsHost`, `IsNetworkActive`, `CanMutateState`, `HasAuthority(go)`. Use
  `CanMutateState` to guard state changes (server-or-solo).
- **`NeoNetworkSpawner`** (static) — `Spawn(prefab,pos,rot,parent=null)`, `Despawn(instance)`, `CanSpawn`.
  With Mirror: server-authoritative `NetworkServer.Spawn`/`Destroy`; **returns null and destroys the
  instance** if called on a client-only peer for a `NetworkIdentity` (prevents client ghosts) — guard with
  `CanSpawn`.
- **`NetworkReactivePropertyBridge`** (static) — connect a Mirror `[SyncVar(hook)]` to a `Neo.Reactive`
  property: `SetFromNetwork(ReactivePropertyFloat|Int|Bool, value)` (does `SetValueWithoutNotify` +
  `ForceNotify`). There is **no** standalone `NetworkReactiveProperty` class — you pair a `[SyncVar]` field
  with a normal `ReactiveProperty*` via this bridge.
- **`NeoNetworkComponent`** — abstract base for the relays below; `bool isNetworked`; implements
  `INeoOptionalNetworked`; protected `RateLimitCheck()`, `ApplyNetworkState()`.
- **`NeoNetworkPlayer`** — player base: `IsLocalPlayer`, `HasAuthority`, events `OnLocalPlayerStarted`/
  `OnRemotePlayerStarted`; auto local/remote object visibility + remote AudioListener disable.
- **`Money`** (`Neo.Shop`, `NetworkSingleton<Money>`) — currency; `isNetworked` toggle; reactive
  `CurrentMoney/LevelMoney/AllMoney`; syncs `_syncCurrentMoney` `[SyncVar]` when networked.

### Lobby (Mirror-only)
- **`NeoLobbyManager`** (extends `NetworkRoomManager`) — `HostLobby()`, `JoinLobby(address)`,
  `LeaveLobby()`, `CanStartGame()`; `PlayerCount`, `AllReady`; events `OnPlayerJoinedRoom`,
  `OnAllPlayersReady`, `OnGameSceneLoaded`, `OnPlayerCountChanged`; `_minPlayersToStart`.
- **`NeoLobbyPlayer`** (extends `NetworkRoomPlayer`) — `ToggleReady()`, `SetReady(bool)`; `IsLocal`,
  `IsReady`; events `OnReadyChanged(bool)`, `OnBecameLocalPlayer`.
- Flow: `HostLobby()` → clients `JoinLobby(addr)` → each `SetReady(true)` → `OnAllPlayersReady` when all
  ready & `PlayerCount>=_minPlayersToStart` → Mirror loads game scene → `OnGameSceneLoaded`.

### Scene-NetworkIdentity reactivation
- **`INeoOptionalNetworked { bool IsNetworked { get; } }`** (always compiled). Implemented by
  `NeoNetworkComponent`, `Money`, player controllers.
- **`NeoMirrorSceneReactivator`** (Mirror-only, static) — runtime safety net that re-enables scene
  `NetworkIdentity` objects in offline scenes (editor post-processor `NeoMirrorScenePostProcess` bakes the
  fix at build). **See Gotchas — this is the #1 Mirror pitfall.**

### Relay components — **[no-code], inspector-primary**
These exist mainly to wire networked actions in the Inspector; for code-first networking write your own
`NetworkBehaviour` with `[Command]`/`[ClientRpc]` instead. (Callable from code, but not the idiomatic path.)
- **`NetworkActionRelay`** — multi-channel broadcast: `Trigger([channel])`, `TriggerFloat/String[...]`,
  `TriggerByName(name)`; `NetworkActionScope{AllClients,ServerOnly,OthersOnly}`.
- **`NetworkContextActionRelay`** — context-aware (resolves a `NetworkIdentity` like "the entering player");
  `Trigger(...)`, `TriggerLocalPlayer()`, `TriggerOnlyForLocalContext` (dedups physics events). Uses a
  custom `NetworkMessage`; needs `RegisterMirrorHandlers()` (auto with `NeoNetworkManager`).
- **`NetworkEventDispatcher`** (`Neo.Tools`) — `DispatchGlobalEvent()` → server fires + RPCs all clients.

## Code-first patterns
### Networked singleton with a synced value
```csharp
using Neo.Network; using Neo.Reactive;
#if MIRROR
using Mirror;
#endif

public class PlayerHealth : NetworkSingleton<PlayerHealth>
{
    public ReactivePropertyFloat HpState = new(100f);   // UI binds to this everywhere
#if MIRROR
    [SyncVar(hook = nameof(OnHpChanged))] private float _syncHp = 100f;
    private void OnHpChanged(float _, float v) => NetworkReactivePropertyBridge.SetFromNetwork(HpState, v);

    [Server] public void ServerSetHp(float v)
    {
        _syncHp = v;                       // → SyncVar hook → reactive on all clients
        HpState.SetValueWithoutNotify(v); HpState.ForceNotify();   // update server locally
    }
#else
    public void ServerSetHp(float v) { HpState.Value = v; }        // solo fallback
#endif
}
// anywhere: PlayerHealth.I.HpState.AddListener(v => bar.SetValue(v));
```

### Server-authoritative spawning + state guards (no `#if` in gameplay code)
```csharp
using Neo.Network;

if (NeoNetworkSpawner.CanSpawn)                       // server or solo
    NeoNetworkSpawner.Spawn(enemyPrefab, pos, Quaternion.identity);

if (NeoNetworkState.CanMutateState) ApplyWorldChange(); // server or solo only
if (NeoNetworkState.HasAuthority(player)) DoLocalInput(player);
```

### Scene object that should stay enabled offline (the INeoOptionalNetworked pattern)
```csharp
using Neo.Network;
#if MIRROR
using Mirror;
[RequireComponent(typeof(NetworkIdentity))]
#endif
public class ScenePowerUp : MonoBehaviour, INeoOptionalNetworked
{
    [SerializeField] private bool _isNetworked = false;
    public bool IsNetworked => _isNetworked;          // false → reactivator re-enables it offline
}
```

## Gotchas
- **Scene NetworkIdentity force-disable (THE pitfall):** Mirror's `NetworkScenePostProcess` disables every
  scene `NetworkIdentity` GameObject so the server owns their lifecycle. In an offline scene they'd stay
  disabled forever. NeoxiderTools fixes this in two layers (editor post-processor + runtime
  `NeoMirrorSceneReactivator`), but **any custom scene object with a `NetworkIdentity` that must run offline
  must implement `INeoOptionalNetworked` and return `IsNetworked = false`**, or Mirror leaves it disabled.
- Gate mutations with `NeoNetworkState.CanMutateState` (or Mirror `[Server]`); use `IsNetworkActive` to
  branch online/offline without `#if MIRROR`.
- `NeoNetworkSpawner.Spawn` returns `null` (and destroys) on a client-only peer — guard with `CanSpawn`.
- Custom `NetworkManager` (not `NeoNetworkManager`)? Call
  `NetworkContextActionRelay.RegisterMirrorHandlers()` in your start hooks or context relays silently fail.
- `NetworkDiagnostics.RuntimeLogsEnabled/RuntimeWarningsEnabled` default `false` — enable to trace.

## New in 9.7.0

- `NetworkReactiveSync` — NoCode replication of `ReactivePropertyFloat/Int/Bool` (e.g. `Money.CurrentMoney`)
  from the inspector: target component + field name + direction. Inert without Mirror. Use instead of
  hand-written SyncVar bridges for wallets/score/HP.
- `NetworkPlayerName` — replicated nickname: `SetLocalName(v)` on the local player, `OnNameChanged(string)`
  → TMP label; trimmed + length-capped server-side, rate-limited.
- `NeoNetworkDiscovery.QuickPlay()` — one-button LAN: auto-join first found server or host after
  `Host If None Found After` seconds; `OnQuickPlayResolved(bool becameHost)`.
- `NetworkEventDispatcher` — payload variants `DispatchGlobalInt/Float/String` + matching UnityEvents;
  all commands rate-limited per connection.
- `NetworkPropertySync` — `Skip Hook On Owner` prevents rubber-band in OwnerToServer mode;
  `Sync Interval` has a 0.1s floor.
