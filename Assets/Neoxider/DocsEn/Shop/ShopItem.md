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
| `_id` | The item's ID (assigned automatically by the `Shop` controller). |
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

## See Also

- [Shop](Shop.md) - Main shop controller.
- [ButtonPrice](ButtonPrice.md) - Advanced button component.
- [Module Root](../README.md)
