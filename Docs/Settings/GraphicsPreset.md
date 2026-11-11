# GraphicsPreset

**What it is:** `Neo.Settings.GraphicsPreset` enum — named graphics quality tiers `Minimal`, `Low`, `Medium`,
`High`, `Maximum` and `Custom`. File: `Scripts/Settings/GraphicsPreset.cs`.

**How to use:** call `GameSettings.SetGraphicsPreset(GraphicsPreset.Medium)` (or bind a dropdown through
[SettingsView](SettingsView.md)); read the current tier from `GameSettings.GraphicsPreset`. Each named tier maps to a
`QualitySettings` level through [GraphicsPresetLevelMapping](GraphicsPresetLevelMapping.md) entries on
[GameSettingsComponent](GameSettingsComponent.md); `Custom` keeps the current quality level.

---

| Value | Int |
|-------|-----|
| `Minimal` | 0 |
| `Low` | 1 |
| `Medium` | 2 |
| `High` | 3 (built-in default) |
| `Maximum` | 4 |
| `Custom` | 5 |

The value is saved as an integer; an unknown stored value falls back to the service's default preset.

See also: [GameSettings](GameSettings.md).
