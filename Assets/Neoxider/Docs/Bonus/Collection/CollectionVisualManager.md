# CollectionVisualManager

Displays the ordered catalog from `Collection.I` through an Inspector-assigned array of `ItemCollection`
views. It refreshes on load/unlock/remove and waits for a collection created after the view.

Assign **Items** in catalog order, including each optional button. `Visual()` / `RefreshAllItems()`
refresh the full view; `RefreshItem(id)` / `UpdateItemVisibility(id)` refresh one slot. `GetItem(id)`
returns the view or null for an invalid index.

`SetItem(int)` and `SetItem(ItemCollection)` emit `OnSetItem(int)` only for a valid assigned view.
With **Enable Set Item** enabled, that item must be unlocked in the collection. With it disabled,
locked items can be selected for previews. Missing collections and invalid/unregistered views do not
pass the unlock gate. Wire `OnSetItem` to `ItemCollectionInfo.SetItemId` for an Inspector-only details panel,
or subscribe with `manager.OnSetItem.AddListener(id => ShowDetails(id))` in C#.

## See Also

- [Module Root](../README.md)
