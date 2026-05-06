# InventoryItemCountText

**Purpose:** A UI component that automatically displays the quantity of *one specific item* in the inventory (e.g., a coin counter). It subscribes to inventory events and updates the text whenever changes occur.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Inventory** | Reference to the inventory. If null, auto-finds the default `InventoryComponent`. |
| **Target Text** | The `TMP_Text` component where the result will be written. |
| **Item Id** | The unique ID of the item we are tracking. |
| **Format** | String formatting (default `{0}`). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetItemId(int itemId)` | Change the tracked item at runtime and immediately refresh the text. |
| `void Refresh()` | Force recalculate and update the text. |

## Examples

### No-Code Example (Inspector)
Place a text object on the Canvas for coins. Add `InventoryItemCountText`. In the `Item Id` field, enter your coin's ID from the `InventoryDatabase` (e.g., `10`). In the `Format` field, write `$ {0}`. Now, whenever you pick up a coin, this text will automatically show something like `$ 15`.

## See Also
- [InventoryTotalCountText](InventoryTotalCountText.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](../README.md)
