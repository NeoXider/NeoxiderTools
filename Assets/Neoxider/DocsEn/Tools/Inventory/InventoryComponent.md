# InventoryComponent

**Purpose:** The main inventory manager. Handles item storage, limits (max stacks, slot capacity), and automatic save/load. Supports both physical slots (`SlotGrid`) and a general item list (`Aggregated`).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Database** | Reference to `InventoryDatabase` for item info and max stack rules. |
| **Storage Mode** | Storage type: `Aggregated` (general list) or `SlotGrid` (fixed slot grid). |
| **Slot Count** | Number of slots (only used if `SlotGrid` is selected). |
| **Save Key** | Unique key for `SaveProvider` persistence. |
| **Auto Load** / **Auto Save** | Automatically load data on start and save on every inventory change. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `int TotalItemCount { get; }` | Returns the total sum of all items in the inventory. |
| `int GetCount(int itemId)` | Returns the current amount of a specific item by ID. |
| `int AddItemByIdAmount(int itemId, int amount)` | Adds items to the inventory (if there's space). Returns the actual amount added. |
| `bool TryConsume(int itemId, int amount)` | Attempts to spend the specified amount. Returns `true` if successful. |
| `void Clear()` | Removes all items from the inventory. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnInventoryChanged` | *(none)* | Fired on any inventory modification (add, remove, move). Ideal for UI updates. |
| `OnItemAdded` | `int itemId, int amount` | Fired when an item is successfully added. |
| `OnItemRemoved` | `int itemId, int amount` | Fired when an item is successfully removed/spent. |
| `OnCapacityRejected` | `int itemId, int amount` | Fired when an item cannot be added due to inventory limits. |

## Examples

### No-Code Example (Inspector)
You can bind a UI text update or a sound effect to the `OnInventoryChanged` event directly in the Inspector to refresh the interface whenever the player picks something up.

### Code Example
```csharp
// Check if we have 5 coins, and if so, spend them
if (InventoryComponent.I.GetCount(10) >= 5) 
{
    InventoryComponent.I.TryConsume(10, 5);
    Debug.Log("Purchase successful!");
}
```

## See Also
- [PickableItem](PickableItem.md)
- [InventoryDropper](InventoryDropper.md)
- [InventoryHand](InventoryHand.md)
- ← [Tools/Inventory](../README.md)
