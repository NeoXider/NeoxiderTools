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
| `bool Collect()` | Attempt a manual pickup (no collector validation). |
| `bool CollectFromGameObject(GameObject collector)` | Pickup with a specific collector (for tag/inventory checks). |
| `void Activate()` | Fires `OnActivate` (for in-hand item use). |
| `void Configure(...)` | Set up the item from code (ItemData, fallbackId, amount, target). |
| `int ResolvedItemId { get; }` | Returns the effective item ID. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnCollectStarted` | *(none)* | Collection begins (before adding to inventory). |
| `OnCollected` | `int itemId, int amount` | Item successfully added to inventory. |
| `OnCollectFailed` | *(none)* | Collection failed (no inventory, filter miss, etc.). |
| `OnAfterCollectDespawn` | *(none)* | Fired just before the object is destroyed/deactivated. |
| `OnActivate` | *(none)* | Item activated (for in-hand use via `InventoryHand`). |

## Examples

### No-Code Example (Inspector)
Create a coin prefab. Add `SphereCollider` → `Is Trigger = true`. Attach `PickableItem`. Set `Item Id = 10`, `Amount = 1`, `Collect On Trigger 3D = true`, `Destroy After Collect = true`. When the player enters the trigger, the coin is collected and destroyed.

### Code Example
```csharp
[SerializeField] private PickableItem _keyPickup;

public void ForcePickupKey()
{
    bool success = _keyPickup.Collect();
    if (success) Debug.Log("Key picked up!");
}
```

## See Also
- [InventoryPickupBridge](InventoryPickupBridge.md)
- [InventoryComponent](InventoryComponent.md)
- [InventoryDropper](InventoryDropper.md)
- ← [Tools/Inventory](README.md)
