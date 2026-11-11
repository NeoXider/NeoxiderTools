# NeoCharacterInput

**What it is:** A `MonoBehaviour` deriving from CMF's `CharacterInput` that feeds movement and jump input to any CMF controller. Replaces CMF's `CharacterKeyboardInput` with the Neoxider input stack: New Input System or legacy Input Manager (auto-detected), plus injection hooks for on-screen joysticks, AI and network drivers. `Scripts/Tools/Move/CharacterController/NeoCharacterInput.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add it to the same GameObject as `AdvancedWalkerController` — CMF resolves its `CharacterInput` from its own GameObject in `Awake`, so it must not sit on a child.
2. Leave `Input Backend` on `Auto Prefer New` unless you need to force one API.
3. For mobile, drive it from your joystick with `SetMoveInput(Vector2?)` / `SetJumpInput(bool)` / `SetRunInput(bool)`.
4. Gate it from menus and cutscenes with `SetMovementEnabled(bool)` / `SetJumpEnabled(bool)` (both are UnityEvent-friendly).

---

## Fields

### Input

| Field | Type | Purpose |
|-------|------|---------|
| `Input Backend` | `NeoInputBackend` | `AutoPreferNew` (default), `NewInputSystem` or `LegacyInputManager`. See [decision rule](#backend-decision-rule). |
| `Horizontal Axis` / `Vertical Axis` | `string` | Legacy Input Manager axes. Default `Horizontal` / `Vertical`. |
| `Jump Button` | `string` | Legacy Input Manager button. Default `Jump`. |
| `Run Key` | `KeyCode` | Legacy sprint key. Default `LeftShift`. |

On the New Input System path the bindings are fixed: WASD/arrows + left stick for movement, Space + gamepad South for jump, Shift + left stick click for sprint.

### Gating

| Field | Type | Purpose |
|-------|------|---------|
| `Movement Enabled` | `bool` | Process movement input. Off returns zero movement and zero sprint. |
| `Jump Enabled` | `bool` | Process jump input. |
| `Can Run` | `bool` | Allow sprint. Read by [NeoCharacterSprint](./NeoCharacterSprint.md); has no effect on its own. |

### Diagnostics

| Field | Type | Purpose |
|-------|------|---------|
| `Log Input Fallback Warnings` | `bool` | Warn once when the configured backend is unavailable and the component falls back. Off by default. |

---

## Properties

| Property | Type | Meaning |
|----------|------|---------|
| `MovementEnabled` | `bool` | Whether movement input is processed. |
| `JumpEnabled` | `bool` | Whether jump input is processed. |
| `IsRunHeld` | `bool` | Whether sprint is currently held. False when `Can Run` or `Movement Enabled` is off. While external input is active this reflects `SetRunInput`, not the device. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetHorizontalMovementInput()` | `float` | Strafe axis, -1..1. Called by CMF. |
| `GetVerticalMovementInput()` | `float` | Forward axis, -1..1. Called by CMF. |
| `IsJumpKeyPressed()` | `bool` | Jump **held** state, not a one-frame edge. Called by CMF. |
| `SetMovementEnabled(bool)` | `void` | Enables/disables movement input. UnityEvent-friendly. |
| `SetJumpEnabled(bool)` | `void` | Enables/disables jump input. UnityEvent-friendly. |
| `SetMoveInput(Vector2?)` | `void` | Injects movement (x = strafe, y = forward), clamped to magnitude 1. `null` reverts to the device. |
| `SetJumpInput(bool)` | `void` | Injects the jump button state. |
| `SetRunInput(bool)` | `void` | Injects sprint state. Only read while `SetMoveInput` is active. |

---

## Jump is held, not pressed

`IsJumpKeyPressed()` returns whether the button is **down right now**. CMF derives the press and release edges itself in `AdvancedWalkerController.HandleJumpKeyInput`, and uses the release edge to cut a jump short — that is how variable jump height works.

For `SetJumpInput` this means a one-frame `true` followed by `false` always produces a minimum-height jump. Hold it for as long as the player holds the on-screen button:

```csharp
// On-screen jump button
public void OnJumpButtonDown() => _input.SetJumpInput(true);
public void OnJumpButtonUp()   => _input.SetJumpInput(false);
```

## Backend decision rule

`NeoInputBackendResolver.ShouldUseNewInput(backend, newInputAvailable, legacyAvailable)`:

| Backend | New Input available | Legacy available | Uses |
|---------|---------------------|------------------|------|
| `LegacyInputManager` | any | any | Legacy — an explicit choice is never overridden |
| `AutoPreferNew` / `NewInputSystem` | yes | any | New Input System |
| `AutoPreferNew` / `NewInputSystem` | no | yes | Legacy |
| `AutoPreferNew` / `NewInputSystem` | no | no | New Input System (legacy would throw) |

With *Active Input Handling = Input System Package (New)* every legacy `Input` call throws, so the last row is the only survivable path. Every legacy read is additionally wrapped in a `try/catch` that falls back at runtime.

## Example

```csharp
using Neo.Tools;
using UnityEngine;

public class MobileControls : MonoBehaviour
{
    [SerializeField] private NeoCharacterInput _input;
    [SerializeField] private Joystick _joystick;

    private void Update()
    {
        _input.SetMoveInput(new Vector2(_joystick.Horizontal, _joystick.Vertical));
    }

    private void OnDisable()
    {
        _input.SetMoveInput(null); // back to keyboard/gamepad
    }
}
```

## See also

- [CharacterController overview](./README.md)
- [NeoCharacterSprint](./NeoCharacterSprint.md)
- [NeoCameraInput](./NeoCameraInput.md)
