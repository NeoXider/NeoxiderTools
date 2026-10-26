# GameSettingsComponent

**What it is:** `Singleton<MonoBehaviour>` — inspector config: SaveProvider key prefix, preset→quality mapping, resolution/FPS lists, debounced save, reset defaults. File: `Scripts/Settings/GameSettingsComponent.cs`.

**How to use:** Create via **Neoxider/Settings/Game Settings Service**, configure mappings and persist flags.

## Scene configuration

- Set a distinct save key prefix when multiple projects or profiles share a provider. The default is `Neo.Settings.`; suffixes are defined by `GameSettingsSaveKeys`.
- Map named graphics presets to project quality indices. Several presets may map to one index; reverse lookup selects the first matching mapping. Unmapped indices resolve to `Custom`.
- Leave custom resolutions empty to use `Screen.resolutions`, or provide explicit positive width/height pairs. Entries are deduplicated and sorted by descending width, then height; custom invalid entries are skipped.
- Configure FPS dropdown presets, normally beginning with `-1`. A null or empty array produces no FPS options; the C# framerate API remains usable.
- Configure defaults for reset and missing saved keys. Runtime load/reset validate sensitivity, quality/resolution indices, enum values, display mode pairs and framerate lower bounds.
- Persist flags control writes for Input, Graphics, Display and Performance. Previously saved values are still read when a flag is off.

Initialization attaches this service and loads settings. The inherited singleton can optionally persist across scene loads. Explicit serialized scene references are sufficient; callers do not need to search for the service to read `GameSettings`.

## Public C# and UnityEvent entry points

| Method | Behavior |
|--------|----------|
| `ReloadFromDisk()` | Calls `GameSettings.LoadState()` against the current provider; backend disk reload is separate. |
| `SaveNow()` | Writes enabled groups with `GameSettings.SaveState()`. |
| `SetMouseSensitivityForMenu(float)` | Applies sensitivity and requests debounced persistence. |
| `SetMouseSensitivityForMenuImmediate(float)` | Applies sensitivity and writes enabled groups immediately. |
| `FlushDeferredSaveCoroutine()` | Cancels the callback only. Use `GameSettings.FlushPendingSettingsSave()` to also write values. |

These typed methods can be called directly or connected to UI UnityEvents. `SettingsView` supplies the common bindings automatically.

## Deferred-save lifecycle

The debounce delay uses real time, so pausing gameplay through `Time.timeScale` does not delay the save. A later deferred request replaces the scheduled callback. Disabling the owning service flushes its pending save before the callback is lost, including when its GameObject is destroyed. A deferred request made while the attached service is inactive writes synchronously because it cannot schedule a coroutine. Application pause also flushes settings.

A former service's delayed callback must not save a replacement context. Explicitly changing contexts is not a profile migration: flush before replacing a service if its current state must be retained. Closing a `SettingsView` flushes enabled groups even if that panel did not start the pending change.

Provider writes and durable disk persistence are separate; see [GameSettings](./GameSettings.md).
