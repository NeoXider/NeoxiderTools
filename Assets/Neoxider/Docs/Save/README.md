# Save module

The `Save` module combines provider-based persistence, component-level scene saves, and global project-wide save data.

Demo: `Samples/Demo/Scenes/Save/SaveDemo.unity` — runtime-built UI via `NeoDemoShell`, controller `Samples/Demo/Scripts/Shell/SaveDemoController.cs`.

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

- [`SaveProvider`](./SaveProvider.md)
- [`SaveManager`](./SaveManager.md)
- [`SaveableBehaviour`](./SaveableBehaviour.md)
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
- [`SaveIdentityUtility`](./SaveIdentityUtility.md)
- Time module: [`../Tools/Time/README.md`](../Tools/Time/README.md)
- Shop module: [`../Shop/README.md`](../Shop/README.md)
