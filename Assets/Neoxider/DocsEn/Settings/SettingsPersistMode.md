# SettingsPersistMode

**Purpose:** Enum defining how a settings change is written to `SaveProvider`.

## Values

| Value | Description |
|-------|-------------|
| `Immediate` | Save immediately (according to group rules). |
| `Deferred` | Delay save until debounce elapses (for sliders). |
| `SkipUntilFlush` | Don't save until manual `FlushPendingSettingsSave()`. |

## See Also
- [GameSettingsComponent](GameSettingsComponent.md)
- ← [Settings](README.md)
