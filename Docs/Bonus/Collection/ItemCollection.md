# ItemCollection

**What it is:** a view for one collection slot (`MonoBehaviour`, `Scripts/Bonus/Collection/ItemCollection.cs`): icon, optional details panel, unlocked state events and an `Unlock()` method for UnityEvent bindings.

**How to use:**
1. Add it to each slot view and assign the button, icon image and optional [ItemCollectionInfo](ItemCollectionInfo.md); assign **Collection** or leave it empty to use the collection the owning
   [CollectionVisualManager](CollectionVisualManager.md) displays, otherwise `Collection.I`.
2. Register the views, in catalog order, in [CollectionVisualManager](CollectionVisualManager.md); it calls `SetItemId`, `SetData` and `SetEnabled` and passes its displayed collection, which `Unlock()`
   uses when **Collection** is empty. From code, register them with `CollectionVisualManager.SetItems`.
3. Wire `OnActive` / `OnDeactivated` / `OnChangeEnabled(bool)` to locked/unlocked visuals.

---

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Button` | Button. |
| `IsEnabled` | Is Enabled. |
| `ItemId` | Item Id. |
| `ItemImage` | Item Image. |
| `ItemInfo` | Item Info. |
| `OnActive` | On Active. |
| `OnChangeEnabled` | On Change Enabled. |
| `OnDeactivated` | On Deactivated. |
| `_button` | Button. |
| `_collection` | Collection. |
| `_itemImage` | Item Image. |
| `_itemInfo` | Item Info. |

## See Also

- [Module Root](../README.md)