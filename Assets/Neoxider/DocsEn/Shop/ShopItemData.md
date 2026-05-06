# ShopItemData

**Purpose:** A `ScriptableObject` for storing shop item information. It allows you to configure item properties (price, icon, name) directly in the Inspector without altering the code.

## Setup

1. Create an item data object via the context menu: `Right Click > Create > Neoxider > Shop > Shop Item Data`.
2. Configure the fields.
3. Add the created object to the `_shopItemDatas` array in the `Shop` controller.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_isSinglePurchase` | Can this item be bought only once? (e.g., a unique skin or level unlock). |
| `_nameItem` | The item's display name in the shop. |
| `_description` | The item's description text. |
| `_price` | The initial price of the item. |
| `_sprite` | The main item image (e.g., for the preview window). |
| `_icon` | The item icon (e.g., for a small list slot). |

## See Also

- [Shop](Shop.md) - Main controller.
- [ShopItem](ShopItem.md) - UI representation of the item.
- [Module Root](../README.md)
