# ToggleObject

`ToggleObject` holds reactive boolean state and emits action callbacks from `Set` / `Toggle`. Useful
for C# or Inspector wiring such as switching panels, lights, or doors. File:
`Assets/Neoxider/Scripts/Tools/InteractableObject/ToggleObject.cs`, namespace: `Neo.Tools`.

## Typical use

1. Add `ToggleObject` to a scene object.
2. Wire a button's `onClick` to `Toggle()`.
3. Subscribe `ON` and `OFF` events to the desired actions (e.g. `GameObject.SetActive`).

## Main field

| Field | Description |
|-------|-------------|
| `Value` | `ReactivePropertyBool` holding the current state (`true` = ON). |
| `ValueBool` | Read-only current boolean state, also available to reflection/NeoCondition. |

## Main API

| API | Description |
|-----|-------------|
| `Toggle()` | Inverts the current value. |
| `Set(bool value)` | Sets the value and emits action events, including when the value was already equal. |

Both methods are also available as Inspector buttons in Play Mode. They are disabled outside Play Mode,
preventing `ON`, `OFF`, and change events from firing while editing the scene. The legacy `toggleDebug`
field likewise performs its toggle only during Play Mode.

## Events

- `ON` — emitted by every `Set(true)` call.
- `OFF` — emitted by every `Set(false)` call.
- `Value.OnChanged` — reactive event carrying the new value when it actually changes.
- `OnChangeFlip(bool)` — emitted by every `Set` call, carrying the inverse of its argument.

`Toggle()` delegates to `Set(!ValueBool)`. `Set` updates `Value` first, then invokes `OnChangeFlip`,
then `ON` or `OFF`. Repeating `Set(true)` repeats the latter action events but does not repeat the
reactive change notification. This preserves the component's existing action semantics.

`Set` bounds reactive/action notifications with shared-query invalidation before and after dispatch,
including an exception. See [Shared queries](./SharedQueries.md) when a toggle listener changes world
geometry. Direct external `Value.Value` writes do not use this component's dispatch boundary; the
application should invalidate shared queries if those subscribers change physics inside the epoch.

Writing `Value.Value` directly uses only the reactive property's notifications; it does not invoke
the component's `OnChangeFlip`, `ON`, or `OFF`. Use `Set` when these scene callbacks are needed.
There is no component-level `OnChange` event; subscribe to the reactive property instead.

```csharp
ToggleObject toggle = GetComponent<ToggleObject>();
toggle.Set(true);
bool isOn = toggle.ValueBool;
toggle.Toggle();
```

## Example

Button click → `ToggleObject.Toggle()`; `ON` → PanelA.SetActive(true), PanelB.SetActive(false); `OFF` → PanelA.SetActive(false), PanelB.SetActive(true).

## See also

- [README](./README.md)
- [InteractiveObject](./InteractiveObject.md)
