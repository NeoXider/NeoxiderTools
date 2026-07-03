# NetworkActionRelay

**What it is:** a `NetworkBehaviour` / `MonoBehaviour` component that broadcasts any action over the network via a UnityEvent. Supports multiple channels with typed payloads (void/float/string) and a selectable scope. Path: `Scripts/Network/Core/NetworkActionRelay.cs`, namespace `Neo.Network`.

**How to use:**
1. Add `NetworkActionRelay` to a GameObject with a `NetworkIdentity`.
2. Configure channels in the Inspector (name, scope, events).
3. Wire a trigger (button, PhysicsEvent, InteractiveObject) to the `Trigger()` method.

## Key differences from NetworkEventDispatcher

| Capability | NetworkEventDispatcher | NetworkActionRelay |
|-------------|----------------------|-------------------|
| Number of channels | 1 | Any number |
| Typed payloads | ❌ | float, string |
| Scope selection | ❌ | AllClients, ServerOnly, OthersOnly |
| Rate-limiting | ❌ | ✅ (50ms) |
| Lookup by name | ❌ | ✅ TriggerByName() |

## Fields

### Channel (per channel)

| Field | Description |
|------|----------|
| **Channel Name** | Channel name (for lookup via `TriggerByName()`). |
| **Scope** | Who receives the action: `AllClients` (everyone), `ServerOnly` (server only), `OthersOnly` (everyone except the sender). |
| **On Triggered** | UnityEvent with no parameters. |
| **On Triggered Float** | UnityEvent with a `float` parameter. |
| **On Triggered String** | UnityEvent with a `string` parameter. |

## API

| Method | Description |
|-------|----------|
| **Trigger()** | Fires channel 0 (no data). Convenient for buttons. |
| **Trigger(int index)** | Fires the channel at the given index. |
| **TriggerFloat(float value)** | Fires channel 0 with a float value. |
| **TriggerFloatAt(int index, float value)** | Fires the channel at the given index with a float. |
| **TriggerString(string value)** | Fires channel 0 with a string. |
| **TriggerStringAt(int index, string value)** | Fires the channel at the given index with a string. |
| **TriggerByName(string name)** | Finds a channel by name and fires it. |

## Examples

### Opening a door (No-Code)
1. On the door: `NetworkActionRelay` with channel `"open"`, scope = `AllClients`.
2. On the lever: `InteractiveObject.OnInteract()` → `NetworkActionRelay.Trigger()`.
3. In the channel: `onTriggered` → `Animator.SetBool("isOpen", true)`.
Result: any player pulls the lever → everyone sees the door animation.

### Picking up an item (Server Only)
1. On the item: `NetworkActionRelay` with channel `"pickup"`, scope = `ServerOnly`.
2. Trigger: `PhysicsEvents3D.OnTriggerEnter` → `NetworkActionRelay.Trigger()`.
3. In the channel: `onTriggered` → `InventoryComponent.AddItem()` + `Destroy(gameObject)`.
Result: the server adds the item and removes the object. Clients see the removal via Mirror.

### Chat (Float / String)
1. `InputField.OnSubmit` → `NetworkActionRelay.TriggerString(text)`.
2. Channel scope = `AllClients`, `onTriggeredString` → `ChatUI.AddMessage()`.

## Without Mirror (Offline)
If Mirror is not installed, every action runs locally. The component behaves like a plain MonoBehaviour.

## See also
- [NetworkContextActionRelay](NetworkContextActionRelay.md) — contextual actions on a networked player (trigger/UI with no template reference)
- [NetworkOwnerFilter](NetworkOwnerFilter.md) — role filter before an action
- [NetworkEventDispatcher (legacy)](./NetworkContextActionRelay.md) — legacy single-channel version
- [NoCode Network Spec](NoCode_Network_Spec.md) — conventions

## Authority and scope notes

`Authority Mode` controls who may trigger relay channels over the network:

| Mode | Behavior |
|------|----------|
| `None` | Default NoCode mode. Works on non-owned scene objects. |
| `OwnerOnly` | Only the owning client can send a remote command; host/server is allowed. |
| `ServerOnly` | Remote clients cannot trigger the relay; server/host can. |

Mirror commands still use `requiresAuthority = false`; the relay validates `sender` manually so scene objects do not need ownership setup.

Scope behavior:
- `AllClients`: sends the channel event to every client; dedicated server also invokes locally.
- `ServerOnly`: invokes only on the server/host and does not RPC.
- `OthersOnly`: sends `TargetRpc` to every client except the sender; host-local is excluded when the host triggered it.
