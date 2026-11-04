# KeyInputCompat

**What it is:** a static C# helper (`Scripts/Tools/Input/KeyInputCompat.cs`, namespace `Neo.Tools`, assembly
`Neo.Tools.Input`) that polls keyboard input through the legacy Input Manager or the optional Input System
fallback, so the same call works with either **Active Input Handling** setting.

**How to use:** reference `Neo.Tools.Input`, then call `KeyInputCompat.GetKeyDown(KeyCode)`, `GetKey` or `GetKeyUp`
where you would call `Input.GetKey*`. For Inspector wiring without code use
[MultiKeyEventTrigger](MultiKeyEventTrigger.md).

---

## C# API

```csharp
using Neo.Tools;
using UnityEngine;

bool began = KeyInputCompat.GetKeyDown(KeyCode.Space);
bool held = KeyInputCompat.GetKey(KeyCode.Space);
bool ended = KeyInputCompat.GetKeyUp(KeyCode.Space);
```

These are frame-based reads; call them from `Update`. There is no `IsPressed` method. Add a reference to `Neo.Tools.Input` when using a custom asmdef.

The shared `InputBackendCompat` selection prefers legacy in legacy-only/Both configurations and skips it in New-only configurations. One rejected legacy read selects the optional Input System adapter for every compatible key/mouse/touch reader, avoiding a selection exception on each later call. Missing keyboards or unsupported keys return `false`. Both backends poll without allocating once a key has been read; see [Polling model and cost](README.md#polling-model-and-cost).

Subsystem registration resets selection. `InputBackendCompat.ResetBackendSelection()` explicitly invalidates a remembered failure after deliberate backend reconfiguration; do not call it per frame or for ordinary device reconnection. The fallback reads the current device, so connection changes do not require resetting the backend choice.

The fallback maps letters A–Z, number-row Alpha0–Alpha9, Space, Return, KeypadEnter, Escape, Tab, Backspace, left/right Shift, Control and Alt, arrows, Insert/Delete/Home/End/PageUp/PageDown, Numlock, CapsLock and F1–F12. Return and KeypadEnter use separate controls (`enterKey` and `numpadEnterKey`). Other keypad, punctuation, mouse and joystick `KeyCode` values are not mapped by this keyboard fallback. Mappings identify physical keyboard controls; use Input Actions or text input for rebinding/localized text.

`OptionalInputSystemAdapter.TryReadKeyState` accepts `isPressed`, `wasPressedThisFrame` or `wasReleasedThisFrame`. Its return value reports whether the requested control/state exists; the `out bool` reports its value. An available unpressed key therefore returns `true` with `false` output.

For WASD/arrow movement prefer `MoveInputCompat.ReadMove()` over four key reads: it uses the same backend selection and also reads the legacy axes or the gamepad left stick (see the [module entry](README.md#runtime-api)).

## Inspector workflow

Use [MultiKeyEventTrigger](MultiKeyEventTrigger.md) to wire individual key-down events without writing polling code. The helper itself is a static C# API and is not a component.

## See Also

- ← [Tools/Input](README.md)
