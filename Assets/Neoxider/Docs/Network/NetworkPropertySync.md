# NetworkPropertySync

**What it is:** a `NetworkBehaviour` / `MonoBehaviour` component that automatically syncs any field or property of any component via reflection. Supports Float, Int, Bool, String, Vector3. Path: `Scripts/Network/Core/NetworkPropertySync.cs`, namespace `Neo.Network`.

**How to use:**
1. Add `NetworkPropertySync` to a GameObject with a `NetworkIdentity`.
2. Drag the component whose field should sync into **Target Component**.
3. Type the field or property name into **Field Name** (case-sensitive).
4. Pick a **Value Type** (Float / Int / Bool / String / Vector3).
5. Pick a **Direction** (ServerToClients or OwnerToServer).
6. Configure **Sync Interval** (default 0.1s) and **Threshold**.

---

## Fields

| Field | Type | Description |
|------|-----|----------|
| `_targetComponent` | `Component` | The component whose field will be synced |
| `_fieldName` | `string` | Name of the field or property (public or private) |
| `_valueType` | `SyncValueType` | Data type: `Float`, `Int`, `Bool`, `String`, `Vector3` |
| `_direction` | `SyncPropertyDirection` | `ServerToClients` — server writes, clients read. `OwnerToServer` — owner writes, server distributes. |
| `_syncInterval` | `float` | Change-check interval (seconds, default 0.1) |
| `_threshold` | `float` | Minimum change required to sync (Float/Int/Vector3, default 0.01) |

## Events

| Event | Type | Description |
|---------|-----|----------|
| `onValueChanged` | `UnityEvent` | Fired when the synced value changes on this client |

## Late-Join

The component uses one `[SyncVar]` per data type. New clients automatically receive the current value via `OnStartClient`.

## Examples

### Syncing HP
```
GameObject: Enemy
├── HealthComponent (_currentHp : float)
├── NetworkPropertySync
│   ├── Target: HealthComponent
│   ├── Field: _currentHp
│   ├── Type: Float
│   ├── Direction: ServerToClients
│   └── Interval: 0.1s
└── NetworkIdentity
```

### Syncing a StateMachine
```
GameObject: GameState
├── StateMachine (currentStateIndex : int)
├── NetworkPropertySync
│   ├── Target: StateMachine
│   ├── Field: currentStateIndex
│   ├── Type: Int
│   └── Direction: ServerToClients
└── NetworkIdentity
```

### Syncing a player name
```
GameObject: Player
├── PlayerProfile (DisplayName : string)
├── NetworkPropertySync
│   ├── Target: PlayerProfile
│   ├── Field: DisplayName
│   ├── Type: String
│   └── Direction: OwnerToServer
└── NetworkIdentity
```

## See also
- [NetworkActionRelay](NetworkActionRelay.md) — action (event) synchronization
- [NeoNetworkComponent](NeoNetworkComponent.md) — base class
- [NoCode Network Spec](NoCode_Network_Spec.md) — Rule 10

## Interval constraints (9.6.2)

`Sync Interval` now has a **0.1 s minimum** (`[Min]`): an interval below the server-side rate limit
(0.05 s) caused silent Cmd drops — the owner considered the value sent while all clients stayed stuck
on the stale value until the next change.

`OwnerToServer` caveat: the SyncVar hook overwrites the owner's local value with the server value;
continuously changing local values may rubber-band.

Enable **Skip Hook On Owner** when the owner should keep its newer local value instead of applying
that echo. Accepted owner updates also write the configured target and raise `onValueChanged` on a
dedicated server; they do not rely on a client-only SyncVar hook to run gameplay logic. Commands are
accepted only with **Is Networked** enabled, **Direction = OwnerToServer**, and the matching **Value Type**.
Mirror's ownership validation still applies; validate game-specific ranges in an authoritative component.

For C# gameplay with explicit typed configuration, a custom `NetworkBehaviour` can keep the value in
a `[SyncVar]`, apply accepted values to its gameplay API on the server, and update clients in the hook.
Use `NetworkReactivePropertyBridge.SetFromNetwork` if that target is a reactive property. The Inspector
component above is the reflection-based scene adapter; its serialized target and field name remain unchanged.

## Binding and cadence in 11.0.0

The selected payload type must match the member's declared type (`float`, `int`, `bool`, `string`,
`Vector3`). A field must be writable; a property must have both getter and setter and take no index
arguments. Unsupported/mismatched members, readonly fields and missing accessors are rejected before
incoming commands mutate the SyncVar or target. Invalid configuration is cached with one gated diagnostic.
Earlier implicit numeric/coercion paths were not safe bidirectional bindings: choose the matching type
explicitly or use a typed gameplay adapter. Existing serialized names and valid bindings stay compatible;
no prefab or scene is rewritten automatically.

Eligible capture attempts use an unscaled double clock. Every eligible poll advances the check timestamp,
even when the value is unchanged or the binding is unavailable. Dirty values alone are sent; the adapter
does not reflect/box an unchanged target every frame after the interval expires. Gameplay ownership and
direction gates remain in force while timeScale is zero.
