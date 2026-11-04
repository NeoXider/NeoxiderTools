# InventoryItemCountText

**What it is:** UI `MonoBehaviour` that writes the count of one item id to a `TMP_Text` (e.g., a coin counter) and refreshes on inventory changes. `Scripts/Tools/Inventory/UI/InventoryItemCountText.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add it to a TextMeshPro label (or assign **Target Text**) and assign **Inventory** (or rely on the default inventory).
2. Set **Item Id** and a **Format** such as `$ {0}`.
3. Change the tracked item at runtime with `SetItemId`.

---

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
| `void SetInventory(InventoryComponent inventory)` | Rebind to another inventory (null shows 0) and refresh. |
| `void Refresh()` | Recalculate and write the text; skipped when the label already shows the same value and format. |

## Examples

### No-Code Example (Inspector)
Place a text object on the Canvas for coins. Add `InventoryItemCountText`. In the `Item Id` field, enter your coin's ID from the `InventoryDatabase` (e.g., `10`). In the `Format` field, write `$ {0}`. Now, whenever you pick up a coin, this text will automatically show something like `$ 15`.

## See Also
- [InventoryTotalCountText](InventoryTotalCountText.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](README.md)
