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
│   └── Interval: 0.05s
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
