# Save module

The `Save` module combines provider-based persistence, component-level scene saves, and global project-wide save data.

Demo: `Assets/Neoxider/Samples~/Demo/Scenes/Save/SaveDemo.unity`, with controller `Demo/Scripts/Shell/SaveDemoController.cs`. The release layout uses `Samples~`; active sample development uses `Assets/Neoxider/Samples`. Package Manager imports the sample into `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/`.

## Main workflows

- `SaveProvider` for simple key/value persistence with switchable backends.
- `SaveableBehaviour` + `[SaveField]` + `SaveManager` for component state on scene objects.
- `GlobalSave` for data that should not belong to a specific scene object.

## Quick start

```csharp
using Neo.Save;

SaveProvider.SetInt("score", 100);
SaveProvider.Save();
int score = SaveProvider.GetInt("score", 0);
```

For component state, add one `SaveManager` and derive components from `SaveableBehaviour`, marking fields with `[SaveField("unique-key")]`. Private fields in base classes are supported. Use `ISaveIdentityProvider` when scene placement is not a suitable persistent identity; identities must stay unique per component type and stable while registered.

`SaveManager.Save(component)` stages all marked fields. `SaveManager.Save()` stages fields that allow automatic saving and keeps previously saved manual fields. Call `SaveProvider.Save()` to persist staged changes, or `SaveManager.I.FlushNow()` to stage registered components and flush together.

## Scene / Inspector setup

1. Create a **Neoxider → Save → Save Provider Settings** asset. Select PlayerPrefs or File and configure optional file encryption.
2. Put the asset at `Resources/SaveProviderSettings.asset` for lazy initialization, or assign it to `SaveProviderSettingsComponent`. When using the component, arrange its initialization before `SaveManager` or other save consumers.
3. Add `SaveManager` to the initial scene. `SaveableBehaviour` handles registration and stages automatic fields before disabling. The manager restores initial and newly loaded scene objects, including objects registered during `OnEnable`.
4. After creating runtime objects and assigning their stable identities, call `SaveManager.Load(component)` explicitly. `Register` alone caches fields.

Direct `FileSaveProvider` instances need explicit `Save()` calls. The static facade also flushes on focus loss and application quit. Failed file-slot flushes retain the original slot and pending values; use [`TryChangeSlot`](./FileSaveProvider.md) when the caller needs a success result.

Invalid enabled file encryption is rejected before opening storage. [`SaveProviderSettings.TryCreateProvider`](./Settings/SaveProviderSettings.md) returns the error without activating a provider; legacy `CreateProvider()` throws `InvalidOperationException`. Lazy Resources initialization can be retried after correcting the asset, and an assigned scene-settings failure retains the previous provider. This is an intentional 11.0 change from silently falling back to plaintext.

## More docs

- [`SaveProvider`](./SaveProvider.md)
- [`FileSaveProvider`](./FileSaveProvider.md)
- [`SaveProviderSettings`](./Settings/SaveProviderSettings.md)
- [`SaveProviderSettingsComponent`](./SaveProviderSettingsComponent.md)
- [`SaveManager`](./SaveManager.md)
- [`SaveableBehaviour`](./SaveableBehaviour.md)
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
- [`SaveIdentityUtility`](./SaveIdentityUtility.md)
- Time module: [`../Tools/Time/README.md`](../Tools/Time/README.md)
- Shop module: [`../Shop/README.md`](../Shop/README.md)
