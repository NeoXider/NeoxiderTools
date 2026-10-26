# KeyboardMover

Moves from keyboard/gamepad input or typed `IMover` commands. Namespace `Neo.Tools`, assembly `Neo.Tools.Move`.

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
| `AxisMode` | Axis Mode. |
| `IsMoving` | Is Moving. |
| `MovementPlane` | Movement Plane. |
| `OnMoveStart` | On Move Start. |
| `OnMoveStop` | On Move Stop. |
| `axisMode` | Axis Mode. |
| `horizontalAxis` | Horizontal Axis. |
| `inputBackend` | Input Backend. |
| `movementPlane` | Movement Plane. |
| `verticalAxis` | Vertical Axis. |

## See Also

- [Module Root](../README.md)
