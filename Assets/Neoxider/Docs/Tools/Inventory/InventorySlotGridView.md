# InventorySlotGridView

**What it is:** UI `MonoBehaviour` that shows the physical slots of a `SlotGrid` inventory, spawning one [InventorySlotView](InventorySlotView.md) per slot (including empty ones), with optional click-to-transfer between grids. `Scripts/Tools/Inventory/UI/InventorySlotGridView.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add it to a container with a `GridLayoutGroup` and assign **Inventory** (Storage Mode = Slot Grid).
2. Assign a **Slot Prefab** with `InventorySlotView` (or fill **Manual Slots**).
3. Keep **Enable Click Transfer** on to move items by clicking a source slot and then a target slot.

---

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

Rebinding a selected grid cancels its pending transfer selection. A later click cannot transfer from
the newly bound inventory using the previous inventory's selection. Disabled grids do not subscribe
to inventory mutations during `SetInventory`; enabling or calling `Refresh` updates their visuals.

Successful moves, swaps, and payload replacements refresh the grid through `OnInventoryChanged`, even
when total item counts are unchanged. A refresh binds each slot from the live slot state instead of a deep
copy, so redraws do not clone instance payloads. The component destroys its prefab-spawned slots when destroyed,
including slots under an external Slots Root. Manual Slots and unrelated root children are preserved.

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
- ← [Tools/Inventory](README.md)
