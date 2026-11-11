# CategoryBar

**What it is:** `Neo.UI.CategoryBar` — a reusable horizontal/tab category bar that owns selection state and a configurable selected visual. Generic by design: it reports selection through id/index events and has no Shop dependency (pair it with `ShopListViewCategoryBar` for shops). `Neo.UI.CategoryBarItem` is the per-entry view (Button + optional TMP label, icon `Image`, and a per-item selected visual).

**File:** `Scripts/UI/CategoryBar/CategoryBar.cs`, `Scripts/UI/CategoryBar/CategoryBarItem.cs`.

**How to use:** author entries in the Inspector (`Id`, `DisplayName`, `Icon`, `Disabled`) or provide them at runtime with `SetCategories(entries, initialIndex)`. Item views are either authored children with `CategoryBarItem` (matched by index; auto-collected when the list is empty) or spawned from an optional `Item Prefab` under `Items Root`. The shared `Selection Marker` RectTransform is re-parented onto the selected item with an anchored offset — authored graphics are never resized or repositioned; per-item frames go into `CategoryBarItem.Selected Visual`.

**API:** `Select(int)`, `Select(string id)`, `Next()`, `Prev()` (skip disabled entries; wrap is configurable), `SetEntryDisabled(index, bool)` (and the one-argument UnityEvent targets `DisableEntry(int)` / `EnableEntry(int)`), `CurrentIndex`, `CurrentCategoryId`, `IsInitialized`, `Initialize()` (idempotent; runs on Start). Events: the C# event `SelectionChanged(int index, string id)` and the UnityEvents `OnCategorySelected(int)`, `OnCategoryIdSelected(string)` — raised once per actual change, C# event first.

---

```csharp
bar.SetCategories(new[]
{
    new CategoryBar.Entry { Id = "all", DisplayName = "All" },
    new CategoryBar.Entry { Id = "equipment", DisplayName = "Equipment" }
}, initialIndex: 1);
bar.Initialize(); // optional before Start; selects equipment
bar.SelectionChanged += (index, id) => Debug.Log($"Category {index}: {id}");
bar.Select("all");
```

`Select`, `Next` and `Prev` also work before `Start` (for example from another component's `Awake`): the
entry is selected right away, the events are raised once for it, and Start keeps it instead of applying
`Start Index`. Before `Start`, `Next`/`Prev` step from the entry `Start Index` would select, so `Next` on a bar
starting at entry 0 selects entry 1. A disabled or unknown entry is rejected. These calls neither build item
views (Start or `Initialize()` does that) nor change the serialized `Start Index`, so the `Next`/`Prev`
inspector buttons stay a selection preview in Edit Mode and never spawn `Item Prefab` clones into the scene.

`SetCategories` accepts calls before `Start`/`Initialize` and remembers an explicit initial index; disabled entries are skipped when initialization runs. It snapshots the input before replacing the list, so passing `bar.Categories` (including an enumeration derived from it) is supported. A negative initial index preserves an existing selected id after initialization when possible. After initialization `SetCategories` re-announces the resulting selection, if any (`SelectionChanged` and both UnityEvents fire even when the same id stays selected), so listeners such as a filtered list re-apply it to the new entries. Before initialization it resets the selection to the initial index (or Start Index).

For Inspector/UnityEvent workflows, configure the category list and item views, then call `Next`, `Prev`, or `Select(int)` from buttons. Authored item views beyond the current category count remain visible but become non-interactable. Rebuilding restores a view's interactability when it has an enabled entry again.

The bar removes only its own button listeners when rebuilt or destroyed. Authored views and consumer listeners remain intact. Spawned views belong to the bar and are removed when the component is destroyed, including views placed under an external `Items Root`; the shared marker is detached before spawned views are removed. Removing only the component from an active bar moves the marker back under the bar (inactive); destroying the bar's whole GameObject (or unloading its scene) destroys a marker that sits on a spawned view together with that view, without re-parenting it onto the dying bar. Unity cannot tell an inactive bar from one being destroyed, so removing the component while the bar's GameObject is inactive also treats the marker as part of the dying hierarchy; to keep it, move the marker out (or select an authored item) first.

**See also:** [ShopListViewCategoryBar](../Shop/ShopListViewCategoryBar.md), [ShopCategoryButton](../Shop/ShopCategoryButton.md), [ShopCategorySelector](../Shop/ShopCategorySelector.md).
