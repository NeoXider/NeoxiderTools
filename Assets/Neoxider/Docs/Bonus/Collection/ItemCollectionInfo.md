# ItemCollectionInfo

**What it is:** a details panel for one collection item (`MonoBehaviour`, `Scripts/Bonus/Collection/ItemCollectionInfo.cs`): name, description (with a default text) and icon.

**How to use:**
1. Add it to a details panel and assign the name/description texts and the icon image.
2. Wire `CollectionVisualManager.OnSetItem(int)` to `SetItemId(int)`, or call `SetData(itemData)` from code.

---

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_collection` | Collection. |
| `_imageItem` | Image Item. |
| `_setNativeSize` | Set Native Size. |
| `_textDefaultValue` | Text Default Value. |
| `_textDescription` | Text Description. |
| `_textName` | Text Name. |

## Runtime API

`SetData(ItemCollectionData)` updates the optional name, description, and icon outputs. An item without
an icon clears the previous item's icon; **Set Native Size** applies only when an icon exists.
`SetItemId(int)` resolves through the explicitly assigned collection, falling back to `Collection.I`.
Both methods can be called directly or bound to UnityEvents.

## See Also

- [Module Root](../README.md)
