# PhysicsEvents2D

`PhysicsEvents2D` forwards Unity 2D physics messages (`OnTriggerEnter2D`, `OnCollisionEnter2D`, etc.) into `UnityEvent` callbacks. Supports filters by `LayerMask` and `requiredTag`. File: `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents2D.cs`, namespace: `Neo.Tools`.

## Typical use

1. Add `PhysicsEvents2D` to a GameObject with a `Collider2D`.
2. For trigger events: enable `Is Trigger` on the collider.
3. For collision events: add a `Rigidbody2D`.
4. Optionally set `layers` and `requiredTag` filters.
5. Subscribe to the desired events in the Inspector.

## Main fields

| Field | Description |
|-------|-------------|
| `interactable` | Enables or disables all event handling. |
| `layers` | Layer filter; events fire only for objects on selected layers. |
| `requiredTag` | Tag filter; if set, events fire only for objects with this tag. |

## Trigger events

- `onTriggerEnter` (`UnityEvent<Collider2D>`)
- `onTriggerStay` (`UnityEvent<Collider2D>`)
- `onTriggerExit` (`UnityEvent<Collider2D>`)

## Collision events

- `onCollisionEnter` (`UnityEvent<Collision2D>`)
- `onCollisionStay` (`UnityEvent<Collision2D>`)
- `onCollisionExit` (`UnityEvent<Collision2D>`)

## See also

- [README](./README.md)
- [InteractiveObject](./InteractiveObject.md)
- [Russian docs](../../../Docs/Tools/InteractableObject/PhysicsEvents2D.md)
