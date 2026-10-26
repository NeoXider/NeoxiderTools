# Settings module

**What it is:** runtime assembly **Neo.Settings**: static **`GameSettings`**, **`GameSettingsComponent`** singleton (persistence via **Neo.Save.SaveProvider**, `QualitySettings` presets), and UI binder **`SettingsView`**. Scripts live under `Assets/Neoxider/Scripts/Settings/`.

Demo: `Samples~/Demo/Scenes/Settings/SettingsDemo.unity` — runtime-built UI via `NeoDemoShell`, controller `Samples~/Demo/Scripts/Shell/SettingsDemoController.cs` (paths relative to the package). The release layout uses `Samples~`; active sample development can use `Samples` instead. Package Manager imports samples into `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/`.

**Contents:**

| Doc | Topic |
|-----|--------|
| [GameSettings.md](./GameSettings.md) | Static API |
| [GameSettingsComponent.md](./GameSettingsComponent.md) | Scene service |
| [SettingsView.md](./SettingsView.md) | UI binding |
| [GraphicsPreset.md](./GraphicsPreset.md) | Named quality tiers |
| [SettingsPersistMode.md](./SettingsPersistMode.md) | Immediate, debounced and explicit saving |

**How to use:**

1. Add **Game Settings Service** (`GameObject → Neoxider/Settings/Game Settings Service`).
2. Read from **`GameSettings`**; write via **`Set…`** or **`SettingsView`**.
3. Keep audio volumes in **`AMSettings`**.

**C# example:** reference `Neo.Settings` from your gameplay assembly and use `using Neo.Settings;`.

```csharp
float sensitivity = GameSettings.MouseSensitivity;
GameSettings.SetMouseSensitivity(3.5f, SettingsPersistMode.Deferred);
GameSettings.SetFullScreen(false);
GameSettings.FlushPendingSettingsSave();
```

The scene service supplies configuration and loads values during initialization. `GameSettings.OnSettingsChanged` lets code react to applied settings; unsubscribe when the consumer is disabled or disposed. Enabled `SettingsView` panels synchronize automatically after API and UI changes. Their callbacks are released when disabled.

Quality dropdown and named preset changes preserve the separate VSync selection. The requested VSync is reapplied after the quality profile and before panels are notified; platforms determine whether VSync affects presentation.

Persistence flags control **writes**, while loading still reads existing saved values. `SaveState` writes every enabled group into the current `SaveProvider`; backend durability is controlled by that provider. See [GameSettings.md](./GameSettings.md) for input validation, display mode consistency and save boundaries.

**Requirements:** this module uses **`QualitySettings`** and does not require URP. If your project needs URP-specific settings, add `com.unity.render-pipelines.universal` to the project separately.
