# GameSettings

**What it is:** static class `Neo.Settings.GameSettings` — current settings values, **`Set…`** mutations with **`SettingsPersistMode`**, **`SaveProvider`** load/save. File: `Scripts/Settings/GameSettings.cs`.

**How to use:**

1. Ensure **`GameSettingsComponent`** is in the scene (it calls **`Attach`** in `Awake`).
2. Read properties (`MouseSensitivity`, `GraphicsPreset`, …).
3. Call **`Set…`** from code; use **`Deferred`** for sliders and **`FlushPendingSettingsSave`** when closing the UI.

See the Russian doc for tables and examples: [../../Docs/Settings/GameSettings.md](../../Docs/Settings/GameSettings.md).
