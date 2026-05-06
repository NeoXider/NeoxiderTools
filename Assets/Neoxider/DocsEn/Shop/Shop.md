# Shop

**Purpose:** The main controller for the in-game shop. It handles the generation of shop items (UI) based on `ShopItemData`, processes purchases (interacting with the player's balance), saves purchased items, and manages the currently selected (equipped) item.

## Setup

1. Add the component via `Add Component > Neoxider > Shop > Shop` to an object in the scene.
2. In the `_shopItemDatas` field, assign a list of items (`ScriptableObject`).
3. Assign the `_prefab` (a prefab with a `ShopItem` component) and `_container` (usually a Layout Group where buttons will spawn).
4. If you have a preview window, assign the `_shopItemPreview`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_prices` | Default prices array (used if `ShopItemData` is not provided). |
| `_shopItemDatas` | Array of item data (ScriptableObject `ShopItemData`), from which icons, descriptions, and base prices are taken. |
| `_shopItemPreview` | Reference to a UI preview component (same `ShopItem` class) that will display the selected item before purchase. |
| `_shopItems` | (Auto-populated) Array of UI items already present in the scene. |
| `_useSetItem` | If `true`, the shop tracks the "Equipped/Selected" item, highlighting it among others. |
| `_autoSubscribe` | If `true`, the shop automatically subscribes to the `buttonBuy` of all spawned `ShopItem`s. |
| `_activateSavedEquipped` | Automatically select the saved equipped item when the scene loads. |
| `_keySave` | Base key for saving purchase progress in `SaveProvider`. |
| `_keySaveEquipped` | Key for saving the ID of the equipped item. |
| `_changePreviewOnPurchaseFailed` | Change the preview to the item if a purchase attempt fails due to insufficient funds. |
| `_container` | The `Transform` parent where item prefabs will be instantiated. |
| `_prefab` | The item prefab (must have a `ShopItem` component). |
| `moneySpendSource` | Source of funds to deduct. The object must implement the `IMoneySpend` interface. If left empty, the global `Money.I` is used. |

## Events
- `OnSelect` — Triggered when an item is selected (equipped).
- `OnPurchased` — Triggered upon a successful purchase.
- `OnPurchaseFailed` — Triggered when a purchase fails (e.g., not enough money).
- `OnLoad` — Triggered after the shop data finishes loading.

## See Also

- [Module Root](../README.md)
