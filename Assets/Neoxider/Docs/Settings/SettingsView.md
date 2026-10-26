# SettingsView

**What it is:** Binds **UnityEngine.UI** controls to **`GameSettings`**. Optional **`ISettingsLocalization`** for labels. File: `Scripts/Settings/SettingsView.cs`.

**How to use:** Add the component, wire references, assign optional **Resolution Block Root** for limited platforms.

## Scene workflow

1. Add and configure `GameSettingsComponent` before enabling the panel.
2. Assign any subset of the sensitivity `Slider`, preset/quality/resolution/FPS `Dropdown` controls, fullscreen/VSync `Toggle` controls and group reset `Button` controls. These are `UnityEngine.UI` controls.
3. Set the sensitivity slider's limits to match the intended range within `0.01..50`; its own range can otherwise hide valid runtime values.
4. Optionally assign the resolution block root. The view hides it on WebGL and the listed console platforms; engine/platform restrictions still apply to resolution changes.

The resolution dropdown begins with Auto. Named presets use localized label keys, quality options use `QualitySettings.names`, and FPS options come from the service. A null or empty FPS list leaves that dropdown empty without preventing other controls from refreshing.

## C# API and lifecycle

`BindAll()` wires the assigned controls idempotently. `RefreshFromSettings()` rebuilds options and synchronizes values without notifying UI listeners; call it after changing the localization provider or authoring configuration at runtime.

Enabled panels listen for settings changes and reloads, so API mutations, quality/preset changes and changes from another panel update their displayed values. Ordinary mutations refresh values without recreating dropdown options. Programmatic refresh uses the controls' `Set…WithoutNotify` methods.

Disabling the view releases its own control callbacks and settings subscriptions, then flushes settings. Other listeners on those controls are preserved. Re-enabling restores bindings and current values. References are expected to stay stable while the view is enabled; disable it before replacing assigned controls.

## Commit modes

- `DebouncedLive`: sensitivity changes use `Deferred`; dropdowns and toggles use `Immediate`. Reset buttons call `ResetGroup`.
- `ApplyButton`: currently a reserved enum value. It uses immediate sensitivity saving and does **not** provide staged values, an Apply action or Cancel/rollback. Use the live workflow until a transaction API is available.

Call `GameSettings.FlushPendingSettingsSave()` when a custom menu closes if it does not disable this view. `SkipUntilFlush` controls scheduling, not isolation from other saves; see [SettingsPersistMode](./SettingsPersistMode.md).
