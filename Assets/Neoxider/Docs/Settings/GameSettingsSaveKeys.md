# GameSettingsSaveKeys

**What it is:** A static class containing save key constants for `GameSettings`. Suffixes are appended to `GameSettingsComponent.SaveKeyPrefix`.

**How to use:** Combine with the service prefix to read a stored value directly, e.g. `SaveProvider.GetFloat("Neo.Settings." + GameSettingsSaveKeys.MouseSensitivity)`.

## Constants

| Key | Description |
|-----|-------------|
| `MouseSensitivity` | Mouse sensitivity. |
| `GraphicsPreset` | Graphics preset. |
| `QualityLevel` | Quality level. |
| `FullScreen` | Fullscreen toggle. |
| `FullScreenMode` | Fullscreen mode. |
| `ResolutionAuto` | Auto resolution. |
| `ResolutionIndex` | Resolution index. |
| `FramerateCap` | Framerate cap. |
| `VSync` | Vertical sync. |

## See Also
- [GameSettingsComponent](GameSettingsComponent.md)
- ← [Settings](README.md)
