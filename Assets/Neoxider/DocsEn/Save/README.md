# Save module

The `Save` module combines provider-based persistence, component-level scene saves, and global project-wide save data.

## Main workflows

- `SaveProvider` for simple key/value persistence with switchable backends.
- `SaveableBehaviour` + `[SaveField]` + `SaveManager` for component state on scene objects.
- `GlobalSave` for data that should not belong to a specific scene object.

## Quick start

```csharp
SaveProvider.SetInt("score", 100);
SaveProvider.Save();
int score = SaveProvider.GetInt("score", 0);
```

## More docs

- Russian docs: [`../../Docs/Save/README.md`](../../Docs/Save/README.md)
- Time module: [`../Tools/Time/README.md`](../Tools/Time/README.md)
- Shop module: [`../Shop/README.md`](../Shop/README.md)
