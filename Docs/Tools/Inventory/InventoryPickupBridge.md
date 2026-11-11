# InventoryPickupBridge

**What it is:** NoCode adapter that forwards UnityEvent calls (trigger events, buttons) to one `PickableItem`, by default the one on the same object. `Scripts/Tools/Inventory/Runtime/InventoryPickupBridge.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add `InventoryPickupBridge` to the pickup object that has the `PickableItem` (or assign **Pickable Item**).
2. Wire the pickup's own trigger/interaction event to `CollectFromCollider` / `CollectFromCollider2D` / `CollectFromGameObject`, passing the collector.
3. From C#, call the `PickableItem` methods directly; the bridge adds nothing beyond forwarding.

---

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

A destroyed or missing `PickableItem` is ignored (Unity null check), so a late event cannot run collection on a dead pickup.

## Examples

### No-Code Example (Inspector)
On a coin prefab with `PickableItem` (auto trigger collection disabled) add a trigger collider, `PhysicsEvents3D` and `InventoryPickupBridge`. In `PhysicsEvents3D.OnTriggerEnter`, call `InventoryPickupBridge.CollectFromCollider` with the dynamic collider argument. The collider that entered is the collector, so its `InventoryComponent` (or a parent's) receives the coin.

## See Also
- [PickableItem](PickableItem.md)
- ← [Tools/Inventory](README.md)
