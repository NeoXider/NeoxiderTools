# ItemCollectionInfo

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

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
