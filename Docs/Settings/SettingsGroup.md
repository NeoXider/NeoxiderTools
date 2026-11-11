# SettingsGroup

**What it is:** Enum for logical settings groups, used for persistence and reset management.

**How to use:** Pass it to `GameSettings.ResetGroup(group)`; the matching persist flags live on `GameSettingsComponent`.

## Values

| Value | Description |
|-------|-------------|
| `Graphics` | Graphics settings (quality, presets). |
| `Input` | Input settings (mouse sensitivity). |
| `Display` | Display settings (resolution, fullscreen). |
| `Performance` | Performance settings (FPS, VSync). |

## See Also
- [GameSettingsComponent](GameSettingsComponent.md)
- ← [Settings](README.md)
