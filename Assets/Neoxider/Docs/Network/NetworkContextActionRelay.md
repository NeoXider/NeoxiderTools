# NetworkContextActionRelay

**What it is:** a `NeoNetworkComponent` that turns ordinary `UnityEvent`s into **context-aware** network actions. Instead of hard-wiring an inspector reference to a single template `GameObject`, the relay resolves the **same networked entity on every client** by `netId` and applies an action to a child target (e.g. `Sphere`).

**Where:** `Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs`, menu `Neoxider/Network/Network Context Action Relay`.

**Inspector:** dedicated (`Assets/Neoxider/Editor/Network/NetworkContextActionRelayEditor.cs`) — inherits `CustomEditorBase` (same look as `NeoCondition`), draws sectioned Context / Target / Action / Networking / Diagnostics / Editor Helpers / Events with reflection-driven dropdowns for component and method selection (shared helpers in `ComponentBindingInspectorShared`).

## When to use

- **Pickup** on the scene: the trigger disappears for everyone on touch (`ContextSource = Self`, `Action = SetActive(false)`).
- **Buff on the entering player**: the trigger enables a child object **on the player whose collider entered** (`ContextSource = Event Argument`, `Target Mode = Child By Name`).
- Multiple relays on one trigger: you can attach two or more `NetworkContextActionRelay`s on a single `NetworkIdentity` — pickup-self + bonus-on-player. Messages are disambiguated by `relayComponentIndex` (Mirror's `NetworkBehaviour.ComponentIndex`).
- UI `Button`: `Button.onClick → Trigger()` with `Local Player` as the context source.
- `InteractiveObject`, `NeoCondition`, any zero-parameter `UnityEvent`: `→ Trigger()` or `→ TriggerLocalPlayer()`.

## Pickup pattern (shared object disappears for all)

1. Add `NetworkContextActionRelay` to a scene object with `NetworkIdentity` (the trigger cube).
2. **Context Source:** `Self`, **Root Mode:** `Source Object`, **Target Mode:** `Root`.
3. **Action:** `Set Active`, **Bool Value:** `false`.
4. **Scope:** `All Clients`.
5. Wire `PhysicsEvents3D.onTriggerEnter → Trigger(Collider)`.

## Buff pattern (Sphere on the entering player)

1. A second `NetworkContextActionRelay` (on the same object if you want it together with pickup).
2. **Context Source:** `Event Argument`, **Root Mode:** `Network Identity In Parents`.
3. **Target Mode:** `Child By Name`, **Target Name:** `Sphere`.
4. **Action:** `Set Active`, **Bool Value:** `true`.
5. **Scope:** `All Clients`, **Trigger Only For Local Context:** `true` (recommended — filter by entering collider, suppresses duplicate dispatches from other clients' physics).

> **⚠ Heads-up:** if the target is a child of `First Person Camera` (or any object inside `NeoNetworkPlayer._localOnlyObjects`), it will be invisible on remote players because the parent is disabled (`activeInHierarchy = false`). Move the target into a part of the hierarchy that stays active on remote players.

## Fields

| Field | Description |
|---|---|
| **Is Networked** | Enables network dispatch. When `false` the action applies locally only. |
| **Context Source** | Where the context comes from: `Self`, `Local Player`, `Owner`, `Event Argument`, `Explicit Object`. |
| **Root Mode** | How to climb to a root: source object, `NetworkIdentity` in parents, `NeoNetworkPlayer` in parents. |
| **Explicit Context** | The fixed object for `Explicit Object`. |
| **Target Mode** | Where to find the target: root, child by name, child by `Transform.Find` path, child by component type. |
| **Target Name / Path / Component Type** | Target-finding parameters. The custom inspector exposes a component-type dropdown driven by the editor Preview Target. |
| **Include Inactive** | Include inactive children during search. |
| **Action** | `Invoke Events Only`, `Set Active`, `Send Message`, `Invoke Component Method`. |
| **Bool / Float / String Value** | Action arguments. The inspector only shows the field relevant to the chosen `Action` / `Method Argument Mode`. |
| **Send Message Name** | Method name for `Send Message`. |
| **Method Component Type / Method Name / Method Argument Mode** | For `Invoke Component Method` — component, method, and argument kind. Reflection-driven dropdowns in the inspector. |
| **Scope** | `AllClients` / `ServerOnly` / `OthersOnly`. |
| **Authority Mode** | `None` / `OwnerOnly` / `ServerOnly` — sender validation for `Command`. |
| **Trigger Only For Local Context** | (default `true`) Input-side filter: every client runs physics on every replicated collider, so without this filter `N` clients dispatch the same trigger. When on, only the client that owns the entering collider dispatches; remotes wait for the server broadcast. |
| **Verbose Logging** | Trace the whole hop chain: `Trigger → Send → OnServer → Broadcast → OnClient → Apply`. |
| **Editor Preview Target** | Editor-only reference for inspector dropdowns; never read at runtime. |

## Events (UnityEvent bridge)

- **On Network Triggered** — networked parameterless `UnityEvent` after server validation.
- **On Context Resolved (GameObject)** — the resolved context root (e.g. the entering player).
- **On Target Resolved (GameObject)** — the resolved target.

For **per-player** effects use `On Target Resolved` or the built-in **Action** — don't drag the template `Sphere` from the inspector again.

## How the network path works

1. The client calls `Trigger(...)`. `relayComponentIndex` is taken from `NetworkBehaviour.ComponentIndex`, which is identical on every peer thanks to Mirror's deterministic order.
2. A pure client sends `NetworkContextActionMessage(relayNetId, relayComponentIndex, contextNetId)` to the server.
3. The server applies the action locally (host) **and** sends the message to every other client (skipping the host's local connection to avoid a double-apply).
4. Each client resolves the exact relay via `NetworkIdentity.NetworkBehaviours[componentIndex]` and applies the action to its local copy of `contextNetId`.

Why direct `NetworkConnection.Send` instead of `[ClientRpc]`: the RPC path is observer-driven (AOI / interest management), so on scene `NetworkIdentity`s with an empty observer list mid-spawn the action silently never reached remote clients. Direct send guarantees delivery to every connected peer.

## Limitations

- A "stays on forever" state after pickup is **not auto-synced for late joiners**. For durable state use `SyncVar` / `NetworkPropertySync` / a dedicated networked state holder.
- The action target must live in a part of the hierarchy that remains active on remote players (not under `_localOnlyObjects`).

## See also

- [NetworkActionRelay](NetworkActionRelay.md)
- [NoCode_Network_Spec](NoCode_Network_Spec.md)
- [Multiplayer_Guide](Multiplayer_Guide.md)
