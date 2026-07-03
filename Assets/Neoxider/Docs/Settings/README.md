# Settings module

**What it is:** runtime assembly **Neo.Settings**: static **`GameSettings`**, **`GameSettingsComponent`** singleton (persistence via **Neo.Save.SaveProvider**, `QualitySettings` presets), and UI binder **`SettingsView`**. Scripts live under `Assets/Neoxider/Scripts/Settings/`.

**Contents:**

| Doc | Topic |
|-----|--------|
| [GameSettings.md](./GameSettings.md) | Static API |
| [GameSettingsComponent.md](./GameSettingsComponent.md) | Scene service |
| [SettingsView.md](./SettingsView.md) | UI binding |
| [GraphicsPreset.md](./GraphicsPreset.md) | Named quality tiers |

**How to use:**

1. Add **Game Settings Service** (`GameObject → Neoxider/Settings/Game Settings Service`).
2. Read from **`GameSettings`**; write via **`Set…`** or **`SettingsView`**.
3. Keep audio volumes in **`AMSettings`**.

**Requirements:** this module uses **`QualitySettings`** and does not require URP. If your project needs URP-specific settings, add `com.unity.render-pipelines.universal` to the project separately.
