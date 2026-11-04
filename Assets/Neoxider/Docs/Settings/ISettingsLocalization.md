# ISettingsLocalization

**What it is:** Interface for localizing strings in the settings UI. Implemented outside the `Neo.Settings` module.

**How to use:** Implement `Get(key)` and pass the instance to `GameSettings.SetLocalizationProvider(localization)` before opening a `SettingsView`.

## API

| Method | Description |
|--------|-------------|
| `string Get(string key)` | Returns a localized display string for a settings label key, or the key itself if unknown. |

## See Also
- [GameSettingsComponent](GameSettingsComponent.md)
- ← [Settings](README.md)
