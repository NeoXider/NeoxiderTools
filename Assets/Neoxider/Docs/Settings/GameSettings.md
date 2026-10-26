# GameSettings

**What it is:** static class `Neo.Settings.GameSettings` — current settings values, **`Set…`** mutations with **`SettingsPersistMode`**, **`SaveProvider`** load/save. File: `Scripts/Settings/GameSettings.cs`.

**How to use:**

1. Ensure **`GameSettingsComponent`** is in the scene (it calls **`Attach`** in `Awake`).
2. Read properties (`MouseSensitivity`, `GraphicsPreset`, …).
3. Call **`Set…`** from code; use **`Deferred`** for sliders and **`FlushPendingSettingsSave`** when closing the UI.

## Runtime API

Read the current state through `MouseSensitivity`, `GraphicsPreset`, `QualityLevelIndex`, `FullScreen`, `FullScreenModeValue`, `ResolutionAuto`, `ResolutionIndex`, `FramerateCap` and `VSync`. Each `Set…` method applies its value immediately; its `SettingsPersistMode` controls when the current state is written to `SaveProvider`.

```csharp
using Neo.Settings;

GameSettings.SetMouseSensitivity(4f, SettingsPersistMode.Deferred);
GameSettings.SetGraphicsPreset(GraphicsPreset.Medium);
GameSettings.SetVSync(true);
GameSettings.FlushPendingSettingsSave();
```

`OnSettingsChanged` fires after a mutation is applied and before its persistence request. `LoadState` applies the loaded state, raises `OnAfterSettingsLoaded`, then raises `OnSettingsChanged`. Consumers should release subscriptions with their own lifecycle. Invalid enum setter arguments are ignored without notifying or saving.

`Attach(component)` selects the scene configuration; normal consumers let the service initialize it. `Detach(component)` only releases the matching context. Graphics preset/quality changes, reset, resolution lookup and persistence require a context. Setters such as mouse sensitivity and VSync can still update runtime values before one is attached.

## Validation and display behavior

- Sensitivity is clamped to `0.01..50`. A NaN setter argument preserves the current value. A saved NaN uses the configured default; a NaN default uses `2`. Infinite values clamp to the corresponding bound. Group reset uses the same bounds.
- Quality and resolution indices clamp to their available lists. `TryGetRuntimeResolution` returns `false` and zero dimensions for an invalid index or missing context.
- `SetQualityLevel` and named `SetGraphicsPreset` changes reapply the requested `VSync` after Unity applies the quality profile, before `OnSettingsChanged` and saving. The separate performance setting takes precedence over the profile's VSync value; changing to `Custom` alone keeps the current quality level.
- Invalid saved graphics presets use the configured default, or `High` if that default is also invalid. Invalid fullscreen modes use a valid default, or `FullScreenWindow` when fullscreen is requested.
- `FullScreen` and `FullScreenModeValue` describe the same requested display state. Turning fullscreen off selects `Windowed`; turning it on from `Windowed` selects `FullScreenWindow`. A subsequent manual resolution change keeps that mode. Load and display reset normalize inconsistent legacy/default pairs; the next save writes the normalized values using the existing keys.
- Framerate values below `-1` normalize to `-1` in setters, load and reset. Configure `-1` for unlimited or positive FPS values for explicit caps. VSync uses `vSyncCount` values `0` and `1`; platform support determines the actual result.

`SetResolutionIndex` selects a manual resolution and disables Auto. Selecting Auto stops further indexed resolution applications; it does not restore a previous desktop resolution. Resolution indices refer to the service's current sorted resolution list.

## Saving and reset

`LoadState` reads the **current provider**; it does not call `SaveProvider.Load()` to reload that backend from disk. `SaveState` writes every group whose persist flag is enabled; it does not call `SaveProvider.Save()`. Call the provider's load/save operations separately when an explicit backend reload or durable flush is needed.

`FlushPendingSettingsSave` cancels the pending debounce callback and writes enabled groups immediately. `SkipUntilFlush` only skips scheduling for that mutation: another immediate save, a panel closing, or a pending deferred save can still capture its current value. It is not an isolated transaction.

`ResetGroup(SettingsGroup.Input/Graphics/Display/Performance)` restores validated service defaults for that group, applies the engine state, notifies subscribers, and writes enabled groups. Persist flags suppress writes, not reads of older saved settings.

## Scene workflow

Configure defaults, persist flags and lists on `GameSettingsComponent`. Bind optional UI controls through `SettingsView`; changes from code also update enabled panels without invoking their UI callbacks. See [GameSettingsComponent](./GameSettingsComponent.md) for debounce lifecycle and [SettingsView](./SettingsView.md) for the reserved Apply mode.
