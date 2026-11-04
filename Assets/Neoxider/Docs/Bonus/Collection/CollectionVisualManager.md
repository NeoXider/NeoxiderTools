# CollectionVisualManager

**What it is:** a scene view manager (`Singleton<CollectionVisualManager>`, `Scripts/Bonus/Collection/CollectionVisualManager.cs`) that displays the ordered catalog of a [Collection](Collection.md) — the assigned one, otherwise `Collection.I` — through an Inspector-assigned array of [ItemCollection](ItemCollection.md) views.

**How to use:**
1. Add it, assign **Items** in catalog order (each with its optional button).
2. Optionally assign **Collection** to display a specific (for example non-global) collection; leave it empty to use `Collection.I`.
3. Wire `OnSetItem(int)` to a details view such as `ItemCollectionInfo.SetItemId`.
4. For a second panel (for example stickers and trophies), add another manager with **Set Instance On Awake** off
   and its own **Collection**. Only one manager can be the singleton: a second one with the flag on destroys its
   own GameObject in `Awake` (a warning is logged when warnings or **Debug Log Warnings** are on).

---

It refreshes on load/unlock/remove and waits for a collection created after the view. The wait resumes when
the manager's object is reactivated, so a manager hidden before its collection appeared still subscribes. On
destroy it unsubscribes from the exact collection instance it subscribed to. Item buttons are wired in `Awake`
for every manager, registered as the singleton or not.

`TargetCollection` (get/set) returns the collection the manager displays: the subscribed one, otherwise the
assigned **Collection**, otherwise `Collection.I` when one is registered (null while none is). Assigning it at
runtime moves the subscription to the new collection and redraws the views; assigning null returns to the
global fallback, also after the assigned collection was destroyed. Changing **Collection** in the Inspector
during Play Mode is applied the same way on the next editor update: redrawing inside `OnValidate` would run the
views' `SetActive` events where Unity rejects them. The getter never scans the scene.

```csharp
CollectionVisualManager trophiesPanel = trophiesPanelObject.GetComponent<CollectionVisualManager>();
trophiesPanel.TargetCollection = trophyCollection; // a Collection with Set Instance On Awake off
trophiesPanel.OnSetItem.AddListener(id => ShowDetails(id));
```

Assign **Items** in catalog order, including each optional button. From code, `SetItems(ItemCollection[])`
does the same: it unwires the buttons of the previous views, wires the new ones once the manager has run its
`Awake`, and redraws them when a collection is already bound (null clears the views). `Visual()` /
`RefreshAllItems()` refresh the full view; `RefreshItem(id)` / `UpdateItemVisibility(id)` refresh one slot.
`GetItem(id)` returns the view or null for an invalid index.

Each view is told which collection the manager displays it for. `ItemCollection.Unlock()` on a view without
its own **Collection** therefore unlocks into that collection, so an unlock button on a non-global panel
writes to the panel's collection, not to `Collection.I`. A view's own **Collection** still wins.

```csharp
trophiesPanel.SetItems(trophyViews);
trophiesPanel.TargetCollection = trophyCollection;
trophyViews[0].Unlock();
```

`SetItem(int)` and `SetItem(ItemCollection)` emit `OnSetItem(int)` only for a valid assigned view.
With **Enable Set Item** enabled, that item must be unlocked in the collection. With it disabled,
locked items can be selected for previews. Missing collections and invalid/unregistered views do not
pass the unlock gate. The gate reads `TargetCollection`; only when that is null does it resolve `Collection.I`,
as earlier versions did. That lookup searches the scene, can initialize an inactive global Collection, and
creates one when `Singleton<Collection>.CreateInstance` is on. Wire `OnSetItem` to `ItemCollectionInfo.SetItemId`
for an Inspector-only details panel, or subscribe with `manager.OnSetItem.AddListener(id => ShowDetails(id))` in C#.

## See Also

- [Module Root](../README.md)
