# GraphicsPresetLevelMapping

**What it is:** A serializable struct mapping a named graphics preset (`GraphicsPreset`) to a Unity `QualitySettings` level index.

**How to use:** Edit the `_presetToQuality` array of `GameSettingsComponent`; each entry maps one preset to a `QualitySettings` index.

## Fields

| Field | Description |
|-------|-------------|
| `Preset` | Graphics preset (enum). |
| `QualityLevelIndex` | Quality level index in `QualitySettings` (≥ 0). |

## See Also
- [GameSettingsComponent](GameSettingsComponent.md) — ← [Settings](README.md)
