# ShopCategoryButton

**Purpose:** NoCode helper for shop category tabs.

Put it on a UI `Button`, assign a `ShopListView`, and enter the category string from `ShopItemData.Category`. When clicked, it switches the target view to that category.

## Key Fields (Inspector)

| Field | Purpose |
|-------|---------|
| `Target View` | `ShopListView` that should change category. Auto-resolved from parent/scene when empty. |
| `Category` | Category label to show. Must match `ShopItemData.Category` exactly. |
| `Show All` | Ignore `Category` and show the full shop list. |
| `Auto Bind Button` | Automatically subscribes to the local `Button.onClick`. |
| `Button` | Optional explicit `Button`; auto-filled from the same GameObject. |

## API

- `Apply()`
- `SetShowAll(bool)`
- `Category`
- `TargetView`

Use this when you want category buttons fully configured in the Inspector without wiring string parameters manually.
