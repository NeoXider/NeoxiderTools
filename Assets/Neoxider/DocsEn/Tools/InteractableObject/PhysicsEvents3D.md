# PhysicsEvents3D

`PhysicsEvents3D` forwards Unity 3D physics messages (`OnTriggerEnter`, `OnCollisionEnter`, etc.) into `UnityEvent` callbacks. Supports filters by `LayerMask` and `requiredTag`. File: `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents3D.cs`, namespace: `Neo.Tools`.

## Typical use

1. Add `PhysicsEvents3D` to a GameObject with a `Collider` (e.g. `BoxCollider`, `SphereCollider`).
2. For trigger events: enable `Is Trigger` on the collider.
3. For collision events: add a `Rigidbody`.
4. Optionally set `layers` and `requiredTag` filters.
5. Subscribe to the desired events in the Inspector.

## Main fields

| Field | Description |
|-------|-------------|
| `interactable` | Enables or disables all event handling. |
| `layers` | Layer filter; events fire only for objects on selected layers. |
| `requiredTag` | Tag filter; if set, events fire only for objects with this tag. |

## Trigger events

- `onTriggerEnter` (`UnityEvent<Collider>`)
- `onTriggerStay` (`UnityEvent<Collider>`)
- `onTriggerExit` (`UnityEvent<Collider>`)

## Collision events

- `onCollisionEnter` (`UnityEvent<Collision>`)
- `onCollisionStay` (`UnityEvent<Collision>`)
- `onCollisionExit` (`UnityEvent<Collision>`)

## See also

- [README](./README.md)
- [InteractiveObject](./InteractiveObject.md)
- [Russian docs](../../../Docs/Tools/InteractableObject/PhysicsEvents3D.md)
