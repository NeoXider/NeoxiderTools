# KeyboardMover

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/MovementToolkit/KeyboardMover.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Move`) that moves from keyboard/gamepad axes or typed `IMover` commands.

**How to use:**
1. Add **Neoxider > Tools > KeyboardMover** and choose `movementPlane`, `speed` and `inputBackend`.
2. Optionally add a `Rigidbody2D` (XY plane) for physics movement.
3. Connect `OnMoveStart` / `OnMoveStop`, or drive it from code through `IMover`.

---

## Setup

- Add `KeyboardMover`, choose `movementPlane` (XY, XZ or YZ), `speed`, and `inputBackend`.
- `AxisRaw` preserves the input magnitude. `AxisNormalized` normalizes nonzero input.
- With a `Rigidbody2D`, use XY: motion is applied in `FixedUpdate`. Without it, the Transform moves in `Update`.
- Connect `OnMoveStart` / `OnMoveStop` to scene effects. They fire on movement state transitions.

## C# API

`MoveDelta(Vector2)` applies a displacement in the selected plane, including small nonzero deltas.
`MoveToPoint(Vector2)` targets coordinates in that plane and preserves the perpendicular coordinate.
Both are immediate Transform commands; `IsMoving` and transition events describe automatic movement.

For example, on XZ, `MoveToPoint(new Vector2(6f, 8f))` sets X/Z to 6/8 and keeps Y.
These commands do not apply automatic speed or multiply their displacement by delta time.

Automatic input prefers the New Input System when available. If legacy input is disabled, automatic
fallback stays on the optional new-input path rather than calling unavailable legacy axes.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `movementPlane` | `XY`, `XZ` or `YZ`. |
| `axisMode` | `AxisRaw` keeps the input magnitude; `AxisNormalized` normalizes nonzero input. |
| `speed` | Units per second. |
| `Speed` / `SetSpeed(float)` | Runtime speed for code and NoCode, for example `AbilityUnitBehaviour.OnMoveSpeedChanged -> SetSpeed` so haste and slows move the player. |
| `inputBackend` | `AutoPreferNew`, `NewInputSystem` or `LegacyInputManager`. |
| `horizontalAxis` / `verticalAxis` | Legacy Input Manager axis names. |
| `_logInputFallbackWarnings` | One-time warning when falling back to legacy input. |
| `OnMoveStart` / `OnMoveStop` | Automatic movement started or stopped. |

## See Also

- [IMover](./IMover.md)
- [MovementToolkit](./README.md)
