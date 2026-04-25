# ISaveableComponent

**Purpose:** Interface for saveable components: a marker for `SaveManager` and a callback `OnDataLoaded()`. `SaveableBehaviour` already implements this interface. Namespace: `Neo.Save`.

## API

| Method | Description |
|--------|-------------|
| `void OnDataLoaded()` | Called by `SaveManager` after all `[SaveField]`-marked fields have been loaded and restored. Use it to refresh UI or recalculate derived data. |

## See Also
- [SaveField](SaveField.md)
- [SaveableBehaviour](SaveableBehaviour.md)
- [SaveManager](SaveManager.md)
- ← [Save](README.md)
