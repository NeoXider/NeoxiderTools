# PickableItem

**Purpose:** A component for world items (coins, potions, weapons) that the player can pick up. Reacts to triggers (2D/3D), filters by tag, validates the collector, and auto-destroys/deactivates after collection.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Item Data** | Reference to `InventoryItemData` (priority source for the item ID). |
| **Item Id** | Fallback item ID if `Item Data` is not assigned. |
| **Amount** | Number of items added on pickup. |
| **Target Inventory** | Target inventory. If empty and `Auto Find` is on, resolved automatically. |
| **Collect On Trigger 3D / 2D** | Auto-collect when entering a 3D/2D trigger zone. |
| **Required Collector Tag** | Tag filter (empty = no filter). E.g., `Player`. |
| **Require Collector Inventory** | Require an `InventoryComponent` on the collecting object. |
| **Collect Only Once** | Picked up only once (prevents duplicate collection). |
| **Destroy After Collect** | Destroy the `GameObject` after successful pickup. |
| **Deactivate After Collect** | Deactivate the object if `Destroy` is off. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `bool Collect()` | Attempt pickup without a collector; requires collector-inventory validation disabled and no collector tag filter. |
| `bool CollectFromGameObject(GameObject collector)` | Pickup with a specific collector (for tag/inventory checks). |
| `void Activate()` | Fires `OnActivate` (for in-hand item use). |
| `void Configure(...)` | Set up the item from code (ItemData, fallbackId, amount, target). |
| `void ConfigureInstance(InventoryItemData itemData, InventoryItemInstance instance, InventoryComponent targetInventory = null)` | Clone and restore a dropped instance, preserving its id/version/payload. Null `itemData` is allowed. Throws for a null `instance` or a non-null `itemData` whose item id differs from `instance.ItemId`. |
| `int ResolvedItemId { get; }` | Returns the effective item ID. |
| `int Amount { get; }` | Quantity remaining in the world; zero after complete collection. |
| `InventoryItemInstance StoredInstance { get; }` | Clone of the remaining dropped instance; null for a simple or fully collected pickup. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnCollectStarted` | *(none)* | Collection begins (before adding to inventory). |
| `OnCollected` | `int itemId, int amount` | Item successfully added to inventory. |
| `OnCollectFailed` | *(none)* | Collection failed (no inventory, filter miss, etc.). |
| `OnAfterCollectDespawn` | *(none)* | Fired just before the object is destroyed/deactivated. |
| `OnActivate` | *(none)* | Item activated (for in-hand use via `InventoryHand`). |

## Capacity, callbacks, and reuse

Pickup returns `true` when at least one unit was accepted. If only part fits, `Amount` becomes the
remaining quantity; the world object and its colliders remain available for another attempt. The
collect-once latch, collider disabling, destruction/deactivation, and `OnAfterCollectDespawn` apply only
when the remaining quantity is fully collected. `OnCollected` reports the actual units added each time.
Stateful pickups stop trying additional instances after the first rejected addition.

Before each inventory notification, accepted units have already been subtracted from `Amount`.
Complete collection sets it to zero and closes the collect-once latch before callbacks. Reentrant
listeners may consume those inventory units; this does not undo the accepted pickup or replenish its
world quantity. If an inventory observer, `OnSaved`, or state capture throws after a partial grant,
only the actual remainder stays collectable. Already accepted units are never retried.

`OnCollected` is attempted for any positive grant even after an inventory observer fault. For a complete
grant, collider/despawn cleanup is attempted and destruction/deactivation runs even if `OnCollected`
or `OnAfterCollectDespawn` throws. The first fault is rethrown afterward. Later listeners within an
interrupted UnityEvent are not replayed. `OnCollectStarted` runs before any grant; an exception there
prevents collection.

`InventoryDropper` uses `ConfigureInstance` to retain instance identity and opaque JSON entries when an
item leaves and re-enters inventory. State components present on the world object refresh their own
entries on capture; payload entries without a matching world component remain in the stored template.
The `StoredInstance` snapshot does not transfer ownership.

Collection callbacks cannot recursively collect the same source while a request is in progress.
`Configure(...)` re-arms a previously collected source for reuse. A pool still owns object activation
and resetting colliders it wants enabled. On complete collection, collider disabling includes children,
including inactive children, for both 3D and 2D physics.

For an explicit repeatable grant source (`Collect Only Once`, `Destroy After Collect`, and `Deactivate
After Collect` all off), a fault-free full grant replenishes its configured amount. A fault after that
grant leaves `Amount == 0`; call `Configure` or `ConfigureInstance` explicitly to re-arm it. This 11.0
behavior prevents an exception from turning a completed grant into another automatic grant.

В 11.0 `Amount` отражает остаток до событий и равен нулю после полной выдачи. Ошибка обработчика не
возвращает уже принятые единицы в мир; очистка полного pickup выполняется до повторного выброса ошибки.

With the default **Require Collector Inventory** setting, use `CollectFromGameObject` or the collider
overloads with a collector that has an inventory on itself or its parents. `InventoryPickupBridge`
provides void methods for Inspector event wiring; it applies the same validation rules.

## Examples

### No-Code Example (Inspector)
Create a coin prefab. Add `SphereCollider` → `Is Trigger = true`. Attach `PickableItem`. Set `Item Id = 10`, `Amount = 1`, `Collect On Trigger 3D = true`, `Destroy After Collect = true`. When the player enters the trigger, the coin is collected and destroyed.

### Code Example
```csharp
[SerializeField] private PickableItem _keyPickup;
[SerializeField] private InventoryComponent _collectorInventory;

public void ForcePickupKey()
{
    bool success = _keyPickup.CollectFromGameObject(_collectorInventory.gameObject);
    if (success) Debug.Log("Key picked up!");
}
```

## See Also
- [InventoryPickupBridge](InventoryPickupBridge.md)
- [InventoryComponent](InventoryComponent.md)
- [InventoryDropper](InventoryDropper.md)
- ← [Tools/Inventory](README.md)
