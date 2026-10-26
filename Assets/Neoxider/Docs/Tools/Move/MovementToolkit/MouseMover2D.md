# MouseMover2D

Moves from pointer deltas, held/clicked targets or a direction dragged from the initial click.

## Setup

- Add `MouseMover2D`, then choose `mode`, `axes`, `mouseButton` and sensitivity/speed.
- Use `Rigidbody2D` on XY only. For physics on other planes, use `MouseMover3D` with a 3D Rigidbody.
- Create/tag the camera as `MainCamera` before activation. Perspective and orthographic pointer targets
  are projected onto the selected plane through the mover. A parallel ray leaves the target at the
  current plane position. With no camera, the legacy screen-coordinate fallback remains.
- Pointer position/buttons use `MouseInputCompat`, including projects with only the New Input System enabled.

## C# API and movement events

`MoveDelta(Vector2)` directly translates in the configured plane. `MoveToPoint(Vector2)` directly sets
that plane's coordinates while preserving the normal coordinate. Both are immediate Transform
commands; automatic pointer movement separately uses `FixedUpdate` and an optional `Rigidbody2D`.

On XZ, `(a, b)` means X/Z; on YZ it means Y/Z. X uses the first component, Y/Z the second.
Repeated point commands are idempotent. `offset` is also expressed in plane coordinates.

`IsMoving`, `OnMoveStart` and `OnMoveStop` describe automatic movement, not direct commands.
Small nonzero steps are applied; `arrivalThreshold` controls stopping in point modes.
In `DeltaRaw`, `pxToWorld` converts accumulated pointer pixels to world displacement;
`DeltaNormalized` instead uses `speed` in units per second.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `AxisMask` | Axis Mask. |
| `IsMoving` | Is Moving. |
| `MoveMode` | Move Mode. |
| `OnMoveStart` | On Move Start. |
| `OnMoveStop` | On Move Stop. |
| `arrivalThreshold` | Arrival Threshold. |
| `axes` | Axes. |
| `invertDeltaX` | Invert Delta X. |
| `invertDeltaY` | Invert Delta Y. |
| `mode` | Mode. |
| `mouseButton` | Mouse Button. |
| `offset` | Offset. |
| `pxToWorld` | Px To World. |

## See Also

- [Module Root](../README.md)
