# InventorySlotView

**What it is:** UI `MonoBehaviour` for one physical cell of an `InventorySlotGridView`: item presenter, selection highlight, empty-state root and click forwarding. `Scripts/Tools/Inventory/UI/InventorySlotView.cs`, namespace `Neo.Tools`.

**How to use:**
1. Build a slot prefab with this component, a child [InventoryItemView](InventoryItemView.md) and optional highlight/empty objects.
2. Assign the prefab to `InventorySlotGridView.Slot Prefab`; the grid calls `Bind` on refresh.
3. Clicks (via the EventSystem) are forwarded to the owning grid; a destroyed grid is ignored.

---

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
- ← [Tools/Inventory](README.md)
