# PhysicsEvents2D

`PhysicsEvents2D` forwards Unity 2D physics messages (`OnTriggerEnter2D`, `OnCollisionEnter2D`, etc.) into
UnityEvents and typed C# events. Supports layer/tag filters and optional Mirror replication. File:
`Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents2D.cs`, namespace: `Neo.Tools`.

## Typical use

1. Add `PhysicsEvents2D` to a GameObject with a `Collider2D`.
2. For trigger events: enable `Is Trigger` on the collider.
3. Ensure at least one participant has a `Rigidbody2D`, and that body types/layers permit the interaction.
4. Enable `filterByLayer` / `filterByTag` and configure the corresponding filter values as needed.
5. Subscribe to the desired events in the Inspector.

## Main fields

| Field | Description |
|-------|-------------|
| `interactable` | Enables or disables all event handling. |
| `filterByLayer` / `layers` | Layer filtering is enabled by default; only selected layers pass. |
| `filterByTag` / `requiredTag` | Tag filtering is off by default. When enabled, a non-empty tag must match; use a defined Unity tag. |
| `isNetworked` | Replicates callbacks in an active network session; otherwise callbacks still run locally. |

## Trigger events

- `onTriggerEnter` (`UnityEvent<Collider2D>`)
- `onTriggerStay` (`UnityEvent<Collider2D>`)
- `onTriggerExit` (`UnityEvent<Collider2D>`)

## Collision events

- `onCollisionEnter` (`UnityEvent<Collision2D>`)
- `onCollisionStay` (`UnityEvent<Collision2D>`)
- `onCollisionExit` (`UnityEvent<Collision2D>`)

## C# subscriptions

Use `TriggerEnterOccurred`, `TriggerStayOccurred`, `TriggerExitOccurred` (`Action<Collider2D>`) and
`CollisionEnterOccurred`, `CollisionStayOccurred`, `CollisionExitOccurred` (`Action<Collision2D>`).
They receive the same payload as their UnityEvent counterpart, immediately after that UnityEvent:

```csharp
relay.TriggerEnterOccurred += HandleTriggerEnter;
// Remove the same handler when its subscriber is disabled or disposed:
relay.TriggerEnterOccurred -= HandleTriggerEnter;
```

The Inspector UnityEvents and C# subscriptions share `interactable`, tag, and layer filtering.
These are raw accepted physics callbacks: changing filters during an overlap does not synthesize a
matching enter/exit pair. `interactable` is the explicit callback gate; Unity can send physics messages
to disabled behaviours.

Dispatch invalidates the [shared interaction-query epoch](./SharedQueries.md) before and after
listeners, including an exception. A listener that changes a blocker cannot leave the next shared
interaction query using the pre-callback geometry. Event order and payload/filter rules are retained.

## Network behavior

When Mirror is unavailable or no network session is active, callbacks run locally even if `isNetworked`
is true. In an active session, the server emits the full callback and broadcasts to clients; a host
does not dispatch the same callback a second time through its RPC. Pure-client physics callbacks
are suppressed in favor of the server's messages.

Remote trigger callbacks resolve a `Collider2D` from the transmitted object's `NetworkIdentity`; the
payload can be `null` when that object is unavailable or has no collider on the identity GameObject.
Remote collision callbacks always pass `null`: a native `Collision2D` cannot be reconstructed from an
RPC. Listeners must handle that case. Compound/child colliders are not reconstructed by these RPCs.

## See also

- [README](./README.md)
- [InteractiveObject](./InteractiveObject.md)
