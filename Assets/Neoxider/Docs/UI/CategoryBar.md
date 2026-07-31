# CategoryBar

**What it is:** `Neo.UI.CategoryBar` — a reusable horizontal/tab category bar that owns selection state and a configurable selected visual. Generic by design: it reports selection through id/index events and has no Shop dependency (pair it with `ShopListViewCategoryBar` for shops). `Neo.UI.CategoryBarItem` is the per-entry view (Button + optional TMP label, icon `Image`, and a per-item selected visual).

**Usage:** author entries in the Inspector (`Id`, `DisplayName`, `Icon`, `Disabled`) or provide them at runtime with `SetCategories(entries, initialIndex)`. Item views are either authored children with `CategoryBarItem` (matched by index; auto-collected when the list is empty) or spawned from an optional `Item Prefab` under `Items Root`. The shared `Selection Marker` RectTransform is re-parented onto the selected item with an anchored offset — authored graphics are never resized or repositioned; per-item frames go into `CategoryBarItem.Selected Visual`.

**API:** `Select(int)`, `Select(string id)`, `Next()`, `Prev()` (skip disabled entries; wrap is configurable), `SetEntryDisabled(index, bool)`, `CurrentIndex`, `CurrentCategoryId`, `Initialize()` (idempotent; runs on Start). Events: `OnCategorySelected(int)`, `OnCategoryIdSelected(string)` — raised once per actual change.

**See also:** [ShopListViewCategoryBar](../Shop/ShopListViewCategoryBar.md), [ShopCategoryButton](../Shop/ShopCategoryButton.md), [ShopCategorySelector](../Shop/ShopCategorySelector.md).
