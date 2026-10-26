# ShopCategorySelector

**What it is:** `Neo.Shop.ShopCategorySelector` — a NoCode category pill with prev/next arrows: cycles a serialized category list and calls `ShopListView.SetCategory(id)`. Complements `ShopCategoryButton` (one button per category) for shops where categories are browsed sequentially.

**Usage:** place on the pill, assign the `ShopListView`, prev/next buttons, icon `Image` and name `TMP_Text`; fill the category list (`id` = `ShopItemData.Category`, empty id = all items). API: `Next()`, `Prev()`, `Select(id)`, `CurrentCategoryId`.

An empty/null list reports an empty `CurrentCategoryId`; navigation is inert. A null entry also reports an empty id and does not change the list filter or label. Navigation continues to wrap through the configured entries.
