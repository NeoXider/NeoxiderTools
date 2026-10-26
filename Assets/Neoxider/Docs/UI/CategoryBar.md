# CategoryBar

**What it is:** `Neo.UI.CategoryBar` — a reusable horizontal/tab category bar that owns selection state and a configurable selected visual. Generic by design: it reports selection through id/index events and has no Shop dependency (pair it with `ShopListViewCategoryBar` for shops). `Neo.UI.CategoryBarItem` is the per-entry view (Button + optional TMP label, icon `Image`, and a per-item selected visual).

**Usage:** author entries in the Inspector (`Id`, `DisplayName`, `Icon`, `Disabled`) or provide them at runtime with `SetCategories(entries, initialIndex)`. Item views are either authored children with `CategoryBarItem` (matched by index; auto-collected when the list is empty) or spawned from an optional `Item Prefab` under `Items Root`. The shared `Selection Marker` RectTransform is re-parented onto the selected item with an anchored offset — authored graphics are never resized or repositioned; per-item frames go into `CategoryBarItem.Selected Visual`.

**API:** `Select(int)`, `Select(string id)`, `Next()`, `Prev()` (skip disabled entries; wrap is configurable), `SetEntryDisabled(index, bool)`, `CurrentIndex`, `CurrentCategoryId`, `Initialize()` (idempotent; runs on Start). Events: `OnCategorySelected(int)`, `OnCategoryIdSelected(string)` — raised once per actual change.

```csharp
bar.SetCategories(new[]
{
    new CategoryBar.Entry { Id = "all", DisplayName = "All" },
    new CategoryBar.Entry { Id = "equipment", DisplayName = "Equipment" }
}, initialIndex: 1);
bar.Initialize(); // optional before Start; selects equipment
bar.Select("all");
```

`SetCategories` accepts calls before `Start`/`Initialize` and remembers an explicit initial index; disabled entries are skipped when initialization runs. It snapshots the input before replacing the list, so passing `bar.Categories` (including an enumeration derived from it) is supported. A negative initial index preserves an existing selected id after initialization when possible.

For Inspector/UnityEvent workflows, configure the category list and item views, then call `Next`, `Prev`, or `Select(int)` from buttons. Authored item views beyond the current category count remain visible but become non-interactable. Rebuilding restores a view's interactability when it has an enabled entry again.

The bar removes only its own button listeners when rebuilt or destroyed. Authored views and consumer listeners remain intact. Spawned views belong to the bar and are removed when the component is destroyed, including views placed under an external `Items Root`; the shared marker is detached before spawned views are removed.

**See also:** [ShopListViewCategoryBar](../Shop/ShopListViewCategoryBar.md), [ShopCategoryButton](../Shop/ShopCategoryButton.md), [ShopCategorySelector](../Shop/ShopCategorySelector.md).
