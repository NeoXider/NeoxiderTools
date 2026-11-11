# ISaveableComponent

**What it is:** Interface for saveable components: a marker for `SaveManager` and a callback `OnDataLoaded()`. `SaveableBehaviour` already implements this interface. Namespace: `Neo.Save`.

**How to use:** Implement it on a component (or derive from `SaveableBehaviour`), mark fields with `[SaveField]` and apply loaded values in `OnDataLoaded()`.

## API

| Method | Description |
|--------|-------------|
| `void OnDataLoaded()` | Called by `SaveManager` after all `[SaveField]`-marked fields have been loaded and restored. Use it to refresh UI or recalculate derived data. |

An exception thrown by `OnDataLoaded()` is logged through `NeoDiagnostics.LogException` with the component as context, and `SaveManager` continues with the next component of the same bulk, scene or single load. The fields of the throwing component are already restored when the callback runs.

## See Also
- [SaveField](SaveField.md)
- [SaveableBehaviour](SaveableBehaviour.md)
- [SaveManager](SaveManager.md)
- ← [Save](README.md)
