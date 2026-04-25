# InventorySlotView

**Purpose:** A UI component representing a single physical cell within an `InventorySlotGridView`. It contains the component that draws the item itself (`InventoryItemView`), alongside logic for selection highlighting and click handling.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Item View** | Reference to the `InventoryItemView` (icon and count) housed within this slot. |
| **Selection Highlight** | A GameObject (e.g., a border or glow) that turns on when the player clicks the slot for transfer. |
| **Empty Root** | A GameObject (e.g., a translucent background) that is visible only when the slot is empty. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Bind(...)` | Called automatically by `InventorySlotGridView` to pass item data and slot state. |
| `void OnPointerClick(...)` | `IPointerClickHandler` implementation. Forwards the slot index to the parent Grid for transfer logic. |

## Examples

### No-Code Example (Inspector)
Create a UI slot prefab (a square background). Inside, place a child `ItemPresenter` (with `InventoryItemView`), a child `Highlight` (yellow border), and a child `EmptyBg` (gray background). Map these references in the `InventorySlotView` fields. When empty, only `EmptyBg` shows; when clicked, `Highlight` turns on.

## See Also
- [InventorySlotGridView](InventorySlotGridView.md)
- [InventoryItemView](InventoryItemView.md)
- ← [Tools/Inventory](../README.md)
