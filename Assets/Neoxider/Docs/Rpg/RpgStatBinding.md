# RpgStatBinding

**What it is:** binds one `RpgCharacter` stat to a UnityEvent.

Callbacks belong to one exact character/stat subscription. Disable, rebind and runtime replacement
invalidate callbacks already captured in the former reactive notification; old states are detached by
their cached source and exact delegate.
The cached character must remain live under Unity's null semantics before its reactive source getter is
used, including when an earlier listener destroys the source while a separate view remains enabled.

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

In C#, assign `Character` and `StatId`, then subscribe to `OnValue`. The component emits an initial
value and subsequent changes while enabled. It reconnects after `RebuildRuntime()` / `ApplyTemplate`,
including a character that initializes after this view, and detaches listeners from replaced stat objects.
