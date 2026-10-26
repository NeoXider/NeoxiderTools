# ShopItem

**Purpose:** The visual representation of a shop item (`ShopItemData`) in the User Interface. It binds data (image, text) to UI elements and the purchase button.

## Setup

1. Create a UI prefab for a shop item slot.
2. Add the component `Add Component > Neoxider > Shop > ShopItem`.
3. Assign references to the text and image fields within your prefab.
4. Assign this prefab to the `_prefab` field of the `Shop` controller.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_id` | Deprecated integer view id. Use `BoundItemId` for stable identity. |
| `_textName` | `TMP_Text` for displaying the item's name (`_nameItem`). |
| `_textDescription` | `TMP_Text` for displaying the description. |
| `_textPrice` | `TMP_Text` for displaying the price (if not using `ButtonPrice`). |
| `_imageItem` | UI `Image` for displaying the main item sprite. |
| `_imageIco` | UI `Image` for displaying the item icon. |
| `_spriteRendererItem` | (Optional) `SpriteRenderer` for the sprite (if the shop is in the 2D world instead of UI). |
| `_spriteRendererIcon` | (Optional) `SpriteRenderer` for the icon. |
| `buttonPrice` | Reference to an advanced `ButtonPrice` component that automatically toggles Buy/Select states. |
| `buttonBuy` | Reference to a standard `Button` (which the `Shop` subscribes to). Auto-filled on OnValidate. |
| `OnSelectItem` | Event triggered when this item is selected (highlighted) by the player. |
| `OnDeselectItem` | Event triggered when the highlight is removed. |
| `OnViewChanged` | Event after binding, clearing, or selection visuals; dependent views refresh here. |

## Runtime Binding

Call `Visual(ShopItemData, int)` or `Visual(ShopBundleData, int)` to bind a reusable cell. `BoundItemId`, `BoundItemData`, and `BoundBundleData` describe its current binding. `Clear()` removes the data and selection. `OnViewChanged` lets code or Inspector listeners react after the visual data is applied; `ShopPurchaseButtonView` uses it automatically when a category change reuses a row.

## See Also

- [Shop](Shop.md) - Main shop controller.
- [ButtonPrice](ButtonPrice.md) - Advanced button component.
- [Module Root](README.md)
