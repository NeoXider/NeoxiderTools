# InventoryTotalCountText

**Purpose:** A UI component that displays cumulative inventory statistics (rather than the count of one specific item). For example: the total number of all items, the number of unique item types, or the number of selected items. Automatically subscribes to inventory events.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Inventory** | Reference to the inventory. If null, auto-finds the default `InventoryComponent`. |
| **Target Text** | The `TMP_Text` component where the result will be written. |
| **Mode** | What exactly to count: `Total` (sum of all stacks), `Unique` (number of different item types), `Selected` (number of selected items, if a selector is used). |
| **Format** | String formatting (default `{0}`). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetMode(InventoryCountViewMode mode)` | Change the counting mode at runtime. |
| `void Refresh()` | Force recalculate the inventory and update the text. |

## Examples

### No-Code Example (Inspector)
Place a text object on the screen called "Backpack Fullness". Attach `InventoryTotalCountText`. Set `Mode = Unique` and `Format` to `Slots taken: {0}`. Now this text will show exactly how many different item slots are occupied.

## See Also
- [InventoryItemCountText](InventoryItemCountText.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](../README.md)
