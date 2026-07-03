# ShopListView

**Purpose:** optional dynamic UI view layer for `Shop`.

Use one `Shop` for purchase logic, save data, currency, ownership, and inventory events. Add one or more `ShopListView` components when you need separate category views, tabs, filtered lists, or a fully spawned storefront.

## Typical Setup

1. Put `Shop` in the scene and assign all `ShopItemData` assets.
2. Disable `Auto Spawn Items` on `Shop` if the shop should not create its own full list.
3. Add `ShopListView` to a UI root.
4. Assign the same `Shop`, an `Item Prefab`, and an `Items Root`.
5. Call `ShowCategory(string)` from category buttons, or `ShowAll()` for the full list.

## Key Fields (Inspector)

| Field | Purpose |
|-------|---------|
| `Shop` | Source shop controller. Auto-resolved from parent/scene when empty. |
| `Category` | Current category filter. |
| `Show All When Category Empty` | Empty category means all items instead of only uncategorized items. |
| `Include Owned` / `Include Unowned` | Basic ownership filters. |
| `Hide Owned Single Purchase Items` | Removes already owned one-time items from the list. |
| `Item Prefab` | `ShopItem` prefab used to spawn missing views. |
| `Views` | Optional pre-authored views reused before spawning more. |
| `Button Action` | What `ShopItem.buttonBuy` does: buy, preview, or select. |

## NoCode API

- `ShowAll()`
- `ShowCategory(string category)`
- `SetCategory(string category)`
- `SetIncludeOwned(bool)`
- `SetIncludeUnowned(bool)`
- `SetButtonAction(ShopListButtonAction)`
- `SetItemPrefab(ShopItem)`
- `SetItemsRoot(Transform)`
- `SetShowAllWhenCategoryEmpty(bool)`
- `SetHideOwnedSinglePurchaseItems(bool)`
- `Refresh()`

For most projects, use one `Shop` and multiple `ShopListView` instances instead of one `Shop` per category.

## Category Buttons

If UnityEvent string parameters are inconvenient, add `ShopCategoryButton` to a UI `Button`.

1. Assign `Target View`.
2. Set `Category` or enable `Show All`.
3. Leave `Auto Bind Button` enabled.

At runtime the button calls `ShopListView.ShowCategory(...)` or `ShopListView.ShowAll()`.
