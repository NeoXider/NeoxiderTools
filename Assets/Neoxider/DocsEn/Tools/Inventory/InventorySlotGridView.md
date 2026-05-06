# InventorySlotGridView

**Purpose:** A UI component for displaying a physical grid of slots (`SlotGrid` mode in `InventoryComponent`). It automatically spawns an `InventorySlotView` for each slot (including empty ones) and manages their updates.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Inventory** | Reference to the inventory (must be in `Slot Grid` mode). |
| **Auto Find Inventory** | Automatically find an `InventoryComponent` in the scene on start if null. |
| **Slot Prefab** | Prefab of an empty slot (with `InventorySlotView`) to be cloned. |
| **Slots Root** | The container (e.g., with a `GridLayoutGroup`) where spawned slots will be parented. |
| **Manual Slots** | List of pre-placed slots (if you prefer not to spawn prefabs dynamically). |
| **Enable Click Transfer** | Enables move logic: first click selects a slot, second click (on another slot) transfers the item. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetInventory(InventoryComponent inventory)` | Bind the grid to a different inventory at runtime and refresh the UI. |
| `void Refresh()` | Force a redraw of all slots (called automatically on inventory changes). |
| `void HandleSlotClick(int slotIndex)` | Handles a click on a specific slot (called from `InventorySlotView`). |

## Examples

### No-Code Example (Inspector)
Create a UI Panel, add a `GridLayoutGroup` and `InventorySlotGridView`. Drag your slot prefab into the `Slot Prefab` field. On start, the grid will automatically fill with empty slots matching the `Slot Capacity` of your `InventoryComponent`.

### Code Example
```csharp
[SerializeField] private InventorySlotGridView _playerGrid;
[SerializeField] private InventorySlotGridView _chestGrid;

public void OpenChest(InventoryComponent chestInventory)
{
    // Switch the chest UI grid to the new chest's inventory
    _chestGrid.SetInventory(chestInventory);
    
    // Thanks to Enable Click Transfer, the player can click to move items between grids
}
```

## See Also
- [InventorySlotView](InventorySlotView.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](../README.md)
