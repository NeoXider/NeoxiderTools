# InventoryPickupBridge

**Purpose:** A bridge utility attached to the player (or a child trigger). Forwards physics events (triggers, `PhysicsEvents3D`) to the `Collect()` method of a `PickableItem`. Great for No-Code setups.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Pickable Item** | Reference to the target `PickableItem`. If null, uses the component on the same object. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Collect()` | Calls `PickableItem.Collect()` (manual pickup). |
| `void CollectFromCollider(Collider collider3D)` | Pickup passing a 3D collider as the collector. |
| `void CollectFromCollider2D(Collider2D collider2D)` | Pickup passing a 2D collider as the collector. |
| `void CollectFromGameObject(GameObject collector)` | Pickup passing a GameObject as the collector. |

## Examples

### No-Code Example (Inspector)
On the player, create a child object with `SphereCollider (Is Trigger)`. Add `PhysicsEvents3D` and `InventoryPickupBridge`. In the `OnTriggerEnter` event of `PhysicsEvents3D`, call `InventoryPickupBridge.CollectFromCollider()`. Now any `PickableItem` entering the trigger will be picked up automatically.

## See Also
- [PickableItem](PickableItem.md)
- ← [Tools/Inventory](README.md)
