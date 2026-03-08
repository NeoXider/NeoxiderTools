# VisualToggle

`VisualToggle` is a reusable two-state visual switch component. It can change sprites, colors, TMP text, and `GameObject` active state between start/end variants, and it can optionally sync with a Unity `Toggle`. File: `Assets/Neoxider/Scripts/UI/View/VisualToggle.cs`, namespace: `Neo.UI`.

## Typical use

1. Add `VisualToggle` to a UI object.
2. Configure start/end visuals for images, TMP text, and object groups.
3. Optionally place a `Toggle` on the same object for auto-sync.
4. Change state through `IsActive`, `SetActive(...)`, `SetInactive()`, or `Toggle()`.

## Main features

- Two-state visual switching
- Multiple image, text, and object targets
- Optional automatic synchronization with `Toggle`
- `UnityEvent` callbacks for `On`, `Off`, and `OnValueChanged`
- Inspector-friendly testing methods

## Main property

| Property | Description |
|----------|-------------|
| `IsActive` | Current state. `true` means active/end state, `false` means inactive/start state. |

## Main API

| API | Description |
|-----|-------------|
| `Toggle()` | Inverts the current state. |
| `SetActive()` | Forces the active/end state. |
| `SetInactive()` | Forces the inactive/start state. |
| `SetActive(bool isActive, bool invokeToggleEvent = false)` | Sets a specific state and can optionally forward the change into a linked `Toggle`. |
| `UpdateVisuals()` | Reapplies visuals for the current state. |

## Events

- `On`
- `Off`
- `OnValueChanged(bool)`

## What it can drive

- `Image` sprite variants
- `Image` color variants
- `TMP` color and optional text variants
- `GameObject` active groups for start/end layouts

## Notes

- The component works both with and without a linked `Toggle`.
- `setOnAwake` can be used to emit the current state events during startup.
- This component is useful for sound buttons, selection markers, enabled/disabled UI states, and simple settings toggles.

## See also

- [README](./README.md)
- [UI](./UI.md)
- [Russian UI docs](../../Docs/UI/README.md)
