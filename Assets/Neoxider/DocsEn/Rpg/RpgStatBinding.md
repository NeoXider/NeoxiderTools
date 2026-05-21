# RpgStatBinding

**What it is:** binds one `RpgCharacter` stat to a UnityEvent.

Use it for UI and NoCode when you need to display or react to `Strength`, `Defense`, `FireResist`, or a custom stat.

## Fields

| Field | Purpose |
|-------|---------|
| `_character` | Source `RpgCharacter`; if empty, searches parents |
| `_statId` | Preset or custom ID |
| `_onValue` | Event with the current stat value |

## Example

`_statId = Strength`, `_onValue -> SetText.SetFloat`.

For conditions use `RpgConditionAdapter.StatAtLeast` / `StatBelow`.
