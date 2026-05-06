# InventoryDropper

**Purpose:** A component for dropping items from the inventory into the game world. Spawns the item's WorldDropPrefab with physics, colliders, and auto-configured `PickableItem` for re-pickup. Supports keyboard input, throw impulse, and random spawn offset.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Inventory** | Item source. Auto-finds if empty. |
| **Drop Point** | Spawn position Transform. Defaults to this object. |
| **Drop Key** | Key for dropping (default `G`). |
| **Drop Selected On Key** | Drop the item selected in `InventoryComponent.SelectedItemId`. |
| **Fallback Drop Prefab** | Default prefab if the item has no `WorldDropPrefab`. |
| **Throw Direction** | Throw direction (local space). |
| **Throw Impulse** | Force applied on throw. |
| **Random Radius** | Random offset radius around the drop point. |
| **Add Rigidbody 3D / 2D** | Automatically add a `Rigidbody` to the spawned item. |
| **Configure Pickable Item** | Automatically configure `PickableItem` on the dropped object. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `int DropSelected(int amount = 1)` | Drop the currently selected item. |
| `int DropById(int itemId, int amount = 1)` | Drop an item by its ID. |
| `int DropSlot(int slotIndex, int amount)` | Drop from a specific physical slot. |
| `int DropFirst(int amount = 1)` | Drop the first available item. |
| `int DropLast(int amount = 1)` | Drop the last available item. |
| `void SetDropEnabled(bool enabled)` | Enable/disable dropping capability. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnItemDropped` | `int itemId, int amount, GameObject dropped` | Item successfully dropped into the world. |
| `OnDropFailed` | `int itemId, int amount` | Drop attempt failed. |

## Examples

### No-Code Example (Inspector)
Attach `InventoryDropper` to the player. Create an empty child in front of the player as the `Drop Point`. Enable `Drop Selected On Key = true`, set `Drop Key = G`. Now pressing `G` throws the selected item forward, and it can be picked up again.

### Code Example
```csharp
[SerializeField] private InventoryDropper _dropper;

public void DropCurrentWeapon()
{
    int dropped = _dropper.DropSelected();
    Debug.Log($"Dropped: {dropped} items.");
}
```

## See Also
- [InventoryHand](InventoryHand.md)
- [PickableItem](PickableItem.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](README.md)
