# VisualToggle

`VisualToggle` is a reusable two-state visual switch component. It can change sprites, colors, TMP text, and `GameObject` active state between start/end variants, and it can optionally sync with a Unity `Toggle`. File: `Assets/Neoxider/Scripts/UI/View/VisualToggle.cs`, namespace: `Neo.UI`.

## Typical use

1. Add `VisualToggle` to a UI object.
2. Configure start/end visuals for images, TMP text, and object groups.
3. Optionally place a `Toggle` on the same object for auto-sync.
4. Change state through `Set(bool)`, `SetActive(...)`, `SetInactive()`, or `Toggle()`. Read it through `IsActive` or `ValueBool`.

## Main features

- Two-state visual switching
- Multiple image, text, and object targets
- Optional automatic synchronization with `Toggle`
- `UnityEvent` callbacks for `On`, `Off`, and `Value.OnChanged`
- Inspector-friendly testing methods

## Main property

| Property | Description |
|----------|-------------|
| `IsActive` | Current state. `true` means active/end state, `false` means inactive/start state. |
| `ValueBool` | Current reactive value, suitable for value bindings. |

## Main API

| API | Description |
|-----|-------------|
| `Toggle()` | Inverts the current state. |
| `Set(bool value)` | Sets the state from a C# call or a bool UnityEvent. |
| `SetActive()` | Forces the active/end state. |
| `SetInactive()` | Forces the inactive/start state. |
| `SetActive(bool isActive, bool invokeToggleEvent = false)` | Sets a specific state and can optionally forward the change into a linked `Toggle`. |
| `UpdateVisuals()` | Reapplies visuals for the current state. |

## Events

- `On`
- `Off`
- `Value.OnChanged(bool)` (Inspector event); use `Value.Subscribe(...)` for code subscriptions.

```csharp
visualToggle.Set(true);
System.IDisposable subscription = visualToggle.Value.Subscribe(
    active => indicator.SetActive(active), invokeImmediately: true);
// Dispose the subscription when the consumer stops observing the view.
```

## What it can drive

- `Image` sprite variants
- `Image` color variants
- `TMP` color and optional text variants
- `GameObject` active groups for start/end layouts

## Notes

- The component works both with and without a linked `Toggle`.
- The same-object `Toggle` is resolved in `Awake` when no explicit reference is assigned.
- `IsActive`, `ValueBool`, and visuals are initialized together in `Awake`, even when `_setOnAwake` is off. Initialization is silent; `_setOnAwake` controls `On`/`Off` emission in `Start`.
- On state changes, the reactive value is updated before `On`/`Off` callbacks, so those callbacks can read the new state.
- A linked `ToggleGroup` can reject turning off its last selected toggle. The visual and reactive state follow the actual `Toggle.isOn` value after such a request.
- This component is useful for sound buttons, selection markers, enabled/disabled UI states, and simple settings toggles.

## See also

- [README](./README.md)
- [UI](./UI.md)
- [UI docs](./README.md)
