# ShopListViewCategoryBar

**What it is:** `Neo.Shop.ShopListViewCategoryBar` — the optional adapter between a generic `Neo.UI.CategoryBar` and a `ShopListView`: whenever the bar selection changes, the list view switches to the selected category id (empty id shows all items). Keeps `CategoryBar` itself free of any Shop dependency.

**Usage:** put it next to the `CategoryBar` (both references auto-resolve: bar from the same object, list view from parents) and make the bar entry ids match `ShopItemData.Category`. Subscribes on enable, unsubscribes on disable, and applies the current bar selection immediately when enabled.

**Auto categories (9.12.0):** enable `Build Categories From Shop` to fill the bar from the Shop catalog on enable — one entry per distinct `ShopItemData.Category` (first-seen order), optionally preceded by a show-all entry (`Include All Entry`, `All Entry Name`, empty id). Call `BuildCategoriesFromShop()` again after `Shop.SetItems` to refresh.

**See also:** [CategoryBar](../UI/CategoryBar.md), [ShopListView](ShopListView.md).
