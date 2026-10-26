# InventoryItemView

**Purpose:** A foundational UI component responsible for drawing a single item stack. It takes data (`InventoryItemData`) and updates the `Image` icon, name, and count text (`TMP_Text`). Used inside slots and standard lists.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Item Id** | Default Item ID (used if this component is bound statically to a specific item in `InventoryView`). |
| **Icon Image** | `Image` component for the item's icon. Taken from `ItemData` or auto-generated from the prefab. |
| **Name Text** | `TMP_Text` to display the item's display name. |
| **Count Text** | `TMP_Text` to display the current stack count. |
| **Root** | The root GameObject, which is disabled (`SetActive(false)`) if the count hits zero or the slot is cleared. |
| **Count Format** | Format string for the count text (default `{0}`). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Bind(InventoryItemData itemData, int itemId, int count)` | Fills the UI with the provided data. Called automatically by lists or slots. |
| `void Clear()` | Clears and hides (`SetActive(false)`) the visual representation of the item. |
| `int BoundItemId { get; }` | Returns the Item ID currently being displayed. |

When **Root** is assigned, `Clear` hides that root and `Bind` shows it again. Without **Root**, these
methods hide/show the component's own GameObject, so an empty row can be reused after items return.

`Bind` uses the explicitly authored `InventoryItemData.Icon` first. If it is absent, the view asks
`WorldDropPrefab.GetPreviewSprite()` for a presentation fallback. This lookup does not assign the
preview to the item asset. The Extensions preview cache owns generated sprites; the view borrows them
and must not destroy them. After `PrefabPreviewExtensions.ClearPreviewSpriteCache()`, refresh/rebind
the view to resolve a replacement. Explicit icon assets and prefab-owned sprites are borrowed and are
not destroyed by cache clearing. See [InventoryItemData](Data/InventoryItemData.md).

## Examples

### No-Code Example (Inspector)
Attach this script to an "Item Row" prefab. Assign references for the icon and count text. In the `Count Format` field, you can type `x{0}` — then a count of 5 will be displayed as `x5` in the game.

## See Also
- [InventorySlotView](InventorySlotView.md)
- [InventoryView](InventoryView.md)
- ← [Tools/Inventory](README.md)
