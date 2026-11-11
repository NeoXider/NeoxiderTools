
# Save System (Save)

**What it is:** an overview of the Save module: a switchable key/value backend (`SaveProvider`, PlayerPrefs or JSON file), component-field persistence (`SaveManager`, `[SaveField]`) and one global data object (`GlobalSave`). Scripts live in `Scripts/Save/`; class pages are listed in [Save/README](Save/README.md).

**How to use:**
1. For plain values call the static `SaveProvider` (`SetInt`, `SetLong`, `SetString`, ...) and `SaveProvider.Save()` at checkpoints.
2. For scene component state add one `SaveManager`, derive components from `SaveableBehaviour` and mark fields with `[SaveField("key")]`.
3. For project-wide data use `GlobalSave.data`.
4. To store data in a file instead of PlayerPrefs, create a **Save Provider Settings** asset (see [SaveProviderSettings](Save/Settings/SaveProviderSettings.md)).

---

## 1. Key/value backend (SaveProvider)

`SaveProvider` is a static, PlayerPrefs-like facade over the active `ISaveProvider`:

- `PlayerPrefsSaveProvider` (default) writes through Unity `PlayerPrefs`.
- `FileSaveProvider` keeps typed values in a JSON file (optionally AES-encrypted) with an atomic commit and a rotating backup.

Supported value types are `int`, `long`, `float`, `string` and `bool`, plus `int[]` / `float[]` through [SaveProviderExtensions](Save/SaveProviderExtensions.md). Numbers are stored culture-invariantly, so saves survive a device locale change.

```csharp
using Neo.Save;

SaveProvider.SetLong("wallet.cents", 12_345_678_901L);
SaveProvider.SetBool("tutorial.done", true);
SaveProvider.Save(); // flush to disk at a checkpoint

long cents = SaveProvider.GetLong("wallet.cents");
```

`Set*` calls only stage values in file-backed providers. `SaveProvider` also flushes on focus loss and on quit; an unchanged file provider skips the disk write.

## 2. Component fields (SaveManager)

1. Add **one** `SaveManager` to the first scene. It is a scene singleton: it does not create itself on first access.
2. Implement `ISaveableComponent` on the component, or derive it from `SaveableBehaviour`, which registers on enable and stages its automatic fields on disable.
3. Mark fields with `[SaveField("UniqueKey")]`. Optional arguments: `autoSaveOnQuit` (default `true`) and `autoLoadOnAwake` (default `true`).
4. Apply loaded values in `OnDataLoaded()`.

`SaveManager` loads registered components on startup and on scene load, and flushes them on pause, focus loss and quit. Public calls:

- `SaveManager.Save()` stages all registered components; `SaveManager.Save(component)` stages one.
- `SaveManager.Load(component)` restores one; `SaveManager.I.FlushNow()` stages everything and writes to disk.

Details: [SaveManager](Save/SaveManager.md), [SaveableBehaviour](Save/SaveableBehaviour.md), [ISaveIdentityProvider](Save/ISaveIdentityProvider.md).

## 3. Global data (GlobalSave)

`GlobalSave` is a static holder of one `GlobalData` object stored as JSON under the `SavesData` key. Reading `GlobalSave.data` loads it lazily; assigning `GlobalSave.data = value` stages it; `GlobalSave.SaveProgress()` stages the current object. Add the fields you need to `GlobalData`. See [GlobalSave](Save/GlobalSave.md).

## See also

- [Save module index](Save/README.md)
- [FileSaveProvider](Save/FileSaveProvider.md) — durability, recovery and encryption
