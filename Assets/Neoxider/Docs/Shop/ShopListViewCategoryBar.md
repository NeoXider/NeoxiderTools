# ShopListViewCategoryBar

**What it is:** `Neo.Shop.ShopListViewCategoryBar` — the optional adapter between a generic `Neo.UI.CategoryBar` and a `ShopListView`: whenever the bar selection changes, the list view switches to the selected category id (empty id shows all items). Keeps `CategoryBar` itself free of any Shop dependency.

**Usage:** put it next to the `CategoryBar` (both references auto-resolve: bar from the same object, list view from parents) and make the bar entry ids match `ShopItemData.Category`. Subscribes on enable, unsubscribes on disable, and applies the current bar selection immediately when enabled.

**See also:** [CategoryBar](../UI/CategoryBar.md), [ShopListView](ShopListView.md).
