# InventoryTotalCountText

**What it is:** UI `MonoBehaviour` that writes an inventory total to a `TMP_Text`: all units, distinct ids, or the count of the inventory's `SelectedItemId`. `Scripts/Tools/Inventory/UI/InventoryTotalCountText.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add it to a TextMeshPro label (or assign **Target Text**) and assign **Inventory** (or rely on the default inventory).
2. Choose **Mode** and a **Format** such as `Items: {0}`.
3. It refreshes on `OnInventoryChanged`, and in `Selected` mode also on `OnSelectedItemChanged`; call `Refresh()` after changing other inputs.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Inventory** | Reference to the inventory. If null, auto-finds the default `InventoryComponent`. |
| **Target Text** | The `TMP_Text` component where the result will be written. |
| **Mode** | What exactly to count: `Total` (sum of all stacks), `Unique` (number of different item types), `Selected` (count of `InventoryComponent.SelectedItemId`, which `InventoryHand` keeps in sync). |
| **Format** | String formatting (default `{0}`). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetMode(InventoryCountViewMode mode)` | Change the counting mode at runtime. |
| `void SetInventory(InventoryComponent inventory)` | Rebind to another inventory (subscribes only while enabled) and refresh. |
| `void Refresh()` | Recalculate and write the text. The write is skipped when the label already shows the same value and format, so frequent inventory events do not re-format unchanged numbers. |

## Examples

### No-Code Example (Inspector)
Place a text object on the screen called "Backpack Fullness". Attach `InventoryTotalCountText`. Set `Mode = Unique` and `Format` to `Slots taken: {0}`. Now this text will show exactly how many different item slots are occupied.

## See Also
- [InventoryItemCountText](InventoryItemCountText.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](README.md)
