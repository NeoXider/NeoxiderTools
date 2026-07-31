# NoCode Multiplayer Guidelines (Spec)

This document describes the fundamental rules and conventions for NeoxiderTools network components, to keep "NoCode"-style automatic synchronization consistent.

## Rule 1: A single boolean toggle — `isNetworked`

All interactive components (Condition, Counter, Selector, PhysicsEvents, etc.) are **fully local and synchronous by default** (`isNetworked = false`, or an unchecked box).
This way, a developer who wants to use a component without multiplayer just adds it and everything works without stray warnings (`OnValidate` accounts for this).

When a developer checks **`isNetworked = true`**, the component starts automatically replicating its state and events through the server. Don't use enums (like `ExecutionMode`) — it must be a plain boolean toggle.

## Rule 2: UnityEvents fire on every client via RPC

When `isNetworked` is on, any public component event (`UnityEvent`) must **fire on every client simultaneously** (via Rpc / ClientRpc), not only on the client that triggered it:
- The server receives the action signal.
- The server broadcasts a ClientRpc.
- Each client calls `UnityEvent.Invoke()` locally.

## Rule 3: "Single server" principle for spawning objects

Components that generate entities (like `Spawner`) must be **duplication-safe** when `isNetworked` is on. Spawning and content generation is trusted **exclusively to the Server**; clients delegate spawn rights to it.

## Rule 4: Inheritance and the preprocessor

Any network script gains multiplayer functionality through the `#if MIRROR` directive.
If the Mirror preprocessor symbol is present:
- The component inherits from `NetworkBehaviour` (instead of `MonoBehaviour`).
- Cmd/Rpc functions are implemented.
If the preprocessor symbol is absent, the component stays a plain `MonoBehaviour`.

## Rule 5: Structure and test organization

- When public variables (attributes) change, make sure they still render correctly in **Custom Editors**.
- All Unit and PlayMode **network tests live in their own directory** (`Network`); everything else is grouped under its parent module (Tools, Core, Rpg, etc.).

## Rule 6: Custom Editor for NetworkBehaviour

Mirror has its own built-in `NetworkBehaviourInspector`. To keep it from overriding the NeoxiderTools fallback inspector's nice UI, you must register explicit overrides.
When a component starts inheriting from `NetworkBehaviour` (under `#if MIRROR`), always register its exact type in `NeoCustomEditor.cs`:
```csharp
#if MIRROR
[CustomEditor(typeof(YourNetworkComponent), true)]
[CanEditMultipleObjects]
public class YourNetworkComponentNeoEditor : NeoCustomEditor { }
#endif
```

## Rule 7: Separating global vs. local (per-player) state

Avoid building generic `NetworkSingleton` components for resources meant to be used individually by each player.
- **Global resources (shared treasury) / global variables:** use `Money` (which is a `NetworkSingleton`). With `isNetworked = true`, this singleton syncs the value for everyone as a single shared pool.
- **Individual resources (personal wallet) / per-player variables:** use `Counter` with `isNetworked = true` and attach it **directly to the scene player template (`Scene Player Template`)**. If you use a plain Mirror workflow with no scene-level NoCode references, the same principle applies to the `Player Prefab`. Since every client spawns its own unique player clone with a `NetworkIdentity`, each also gets its own independent networked `Counter`.

To modify these values from external triggers without a direct reference (letting the system find the right wallet on its own), use the wrapper component **`ModifyCounterByKey`**. It can look up both `Money` (the global treasury) and `Counter` by their unique `SaveKey` string.

For actions on **a specific player's child objects** (a weapon, a `Sphere`, a visual), when the event comes from a collider/UI and you can't store a direct reference to an object inside the scene `Scene Player Template`, use **`NetworkContextActionRelay`** (see `NetworkContextActionRelay.md`): it finds that player's runtime copy on each client by `netId` and applies the action to the resolved target.

## Rule 8: Server-side validation

Every `[Command]` must include basic server-side protection:
- **Rate-limiting** — reject commands arriving faster than `CmdRateLimit` (50ms by default).
- **Logic validation** — e.g. `CmdSpend` checks `CanSpend(amount)` on the server, never trusting the client.
- **A `sender` parameter** — every Cmd receives `NetworkConnectionToClient sender = null` so the sender can be identified if needed.

```csharp
[Command(requiresAuthority = false)]
private void CmdSetValue(float newValue, NetworkConnectionToClient sender = null)
{
    if (Time.time - _lastCmdTime < CmdRateLimit) return;
    _lastCmdTime = Time.time;
    // ... apply
}
```

### Authority mode for NoCode scene objects
NoCode multiplayer components should use `NetworkAuthorityMode` instead of raw Mirror ownership checks:

| Mode | Behavior |
|------|----------|
| `None` | Default. Any client/server trigger is accepted; works on non-owned scene objects. |
| `OwnerOnly` | Remote client commands are accepted only when `sender == NetworkIdentity.connectionToClient`. Host/server is allowed. |
| `ServerOnly` | Only server/host-originated actions are accepted. Remote client commands are rejected. |

Commands should remain `[Command(requiresAuthority = false)]`; validation is done manually through `NeoNetworkState.IsAuthorized(gameObject, sender, authorityMode)`.

## Rule 9: Late-join synchronization

Components with `isNetworked = true` must use `[SyncVar]` to hold the authoritative value on the server. When a new client connects, the value is delivered automatically by Mirror, and `OnStartClient()` applies it to local state:

```csharp
[SyncVar] private float _syncValue;

public override void OnStartClient()
{
    base.OnStartClient();
    if (isNetworked && !isServer) ApplyValueLocally(_syncValue);
}
```

### ReactiveProperty late-join
Reactive variables work with the same rule when the authoritative value is stored in a `[SyncVar]`.
The `ReactiveProperty*` object itself is not a Mirror SyncVar. Keep a primitive SyncVar (`float`, `int`, `bool`) and apply it into the reactive variable:

```csharp
[SyncVar] private float _syncValue;
public ReactivePropertyFloat Value = new();

private void ApplyValueLocally(float value)
{
    NetworkReactivePropertyBridge.SetFromNetwork(Value, value);
}

public override void OnStartClient()
{
    base.OnStartClient();
    if (isNetworked && !isServer) ApplyValueLocally(_syncValue);
}
```

In the editor, replicated UnityEvents and replicated reactive values are marked when `isNetworked` is enabled. The marker means the field is driven by Cmd/Rpc or SyncVar late-join logic, not just a local UnityEvent.

## Rule 10: Universal NoCode network components

To quickly add multiplayer to **any** mechanic with no code, use:

| Component | Purpose | Menu |
|-----------|-----------|------|
| **NetworkPropertySync** | Auto-syncs any field/property via reflection (Float/Int/Bool/String/Vector3) | `Neoxider/Network/Network Property Sync` |
| **NetworkActionRelay** | Multi-channel UnityEvent broadcast (void/float/string) with a selectable scope (AllClients, ServerOnly, OthersOnly) | `Neoxider/Network/Network Action Relay` |
| **NetworkContextActionRelay** | Contextual network actions: `Trigger()` / `Trigger(Collider)` + finds a target inside the networked player by name/path/component (no template reference needed) | `Neoxider/Network/Network Context Action Relay` |
| **NetworkOwnerFilter** | Filters by role (LocalPlayer, Server, Everyone) before invoking an action | `Neoxider/Network/Network Owner Filter` |
| **NeoNetworkDiscovery** | LAN server discovery (wraps Mirror NetworkDiscovery) | `Neoxider/Network/Neo Network Discovery` |
| **NeoLobbyManager** | Lobby with ready-checks (wraps Mirror NetworkRoomManager) | `Neoxider/Network/Neo Lobby Manager` |
| **NeoLobbyPlayer** | A lobby player with a ready button | `Neoxider/Network/Neo Lobby Player` |
| **NetworkEventDispatcher** | Simple single-UnityEvent broadcast (legacy, kept for compatibility) | `Neoxider/Tools/Network/Network Event Dispatcher` |

## Rule 11: Inherit from NeoNetworkComponent

Every new non-singleton networked NoCode component must inherit from `NeoNetworkComponent`, not directly from `NetworkBehaviour`. The base class provides:
- `isNetworked` — boolean toggle (Rule 1)
- `RateLimitCheck()` — spam protection (Rule 8)
- `ApplyNetworkState()` — late-join template (Rule 9)
- `ShouldDispatchToServer()` / `ShouldBroadcastRpc()` — dispatch-pattern helpers

Singleton managers should use `NetworkSingleton<T>` with equivalent utilities.

## Rule 12: The scene player as a NoCode template

NoCode projects often configure the player right in the scene: cameras, UI, UnityEvents, bindings, and manager/child references are already wired in the Inspector. Mirror's prefab-only flow isn't always convenient for that.

For this scenario, use `NeoNetworkManager`:

- enable `Use Scene Player Template`;
- assign the scene player object to `Scene Player Template`;
- leave `Disable Scene Player Template` enabled;
- the player object must have a `NetworkIdentity`.

Behavior:

1. The scene object is a template only.
2. When the network starts, the template is disabled.
3. The server creates an active copy per connection and calls `NetworkServer.AddPlayerForConnection`.
4. Clients create their own copies via a Mirror spawn handler with the same stable runtime id.

Requirement: the server and every client must have the same scene with the same `Scene Player Template` assigned. If the player doesn't depend on scene-level NoCode references, use a regular Mirror `Player Prefab`.

## Implementation note: shared inheritance

Do not duplicate class-level `#if MIRROR` inheritance blocks in every NoCode component.
Non-singleton networked NoCode components should inherit from `NeoNetworkComponent`.
Singleton managers should inherit from `NetworkSingleton<T>`.
Component scripts may still wrap Mirror-only fields and Cmd/Rpc methods with `#if MIRROR`.
