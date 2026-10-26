# SettingsPersistMode

**Purpose:** Enum defining how a settings change is written to `SaveProvider`.

## Values

| Value | Description |
|-------|-------------|
| `Immediate` | Write every enabled group to the current `SaveProvider` immediately. |
| `Deferred` | Replace the debounce callback (for sliders). An inactive attached service writes immediately; disabling the owner flushes a pending save. |
| `SkipUntilFlush` | Apply the value without requesting a save. Explicit flush or another save can still write it. |

All modes apply runtime values immediately. They do not stage a separate snapshot. `GameSettings.FlushPendingSettingsSave()` cancels the callback and writes enabled groups; provider durability is separate from these writes. A `SettingsView` closing or application pause can also trigger a flush.

## See Also
- [GameSettingsComponent](GameSettingsComponent.md)
- ← [Settings](README.md)
