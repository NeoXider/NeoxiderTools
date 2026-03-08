# ToggleObject

`ToggleObject` is a simple boolean state holder that raises `UnityEvent` callbacks when its value changes. Useful for no-code logic such as switching panels, toggling lights, or opening/closing doors. File: `Assets/Neoxider/Scripts/Tools/InteractableObject/ToggleObject.cs`, namespace: `Neo.Tools`.

## Typical use

1. Add `ToggleObject` to a scene object.
2. Wire a button's `onClick` to `Toggle()`.
3. Subscribe `ON` and `OFF` events to the desired actions (e.g. `GameObject.SetActive`).

## Main field

| Field | Description |
|-------|-------------|
| `value` | Current state (`true` = ON, `false` = OFF). |

## Main API

| API | Description |
|-----|-------------|
| `Toggle()` | Inverts the current value. |
| `Set(bool value)` | Sets the value explicitly. |

## Events

- `ON` — raised when state becomes `true`.
- `OFF` — raised when state becomes `false`.
- `OnChange(bool)` — raised on any change; passes the new value.
- `OnChangeFlip(bool)` — raised on any change; passes the inverted value.

## Example

Button click → `ToggleObject.Toggle()`; `ON` → PanelA.SetActive(true), PanelB.SetActive(false); `OFF` → PanelA.SetActive(false), PanelB.SetActive(true).

## See also

- [README](./README.md)
- [InteractiveObject](./InteractiveObject.md)
- [Russian docs](../../../Docs/Tools/InteractableObject/ToggleObject.md)
