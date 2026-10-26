# MouseMover3D

Pointer-driven motion in XY, XZ, YZ or along one axis, optionally using a 3D Rigidbody.

## Setup

- Add `MouseMover3D`, choose `mode` and `plane`, and tune `speed` / `pxToWorld`.
- Create/tag `Camera.main` before activation. Point modes first raycast `groundMask` when configured,
  then fall back to a plane through the object.
- Input is sampled in `Update`; automatic steps are applied in `FixedUpdate`, via `Rigidbody.MovePosition`
  when a body exists. Pointer position/buttons use `MouseInputCompat` for legacy/new input.
- Connect `OnMoveStart` / `OnMoveStop` to scene effects. Small nonzero steps still move and count as motion.

## C# API

`MoveDelta(Vector2)` maps a displacement onto `plane`. `MoveToPoint(Vector2)` requests that plane's
target coordinates while preserving the normal coordinate. On XZ, `(a, b)` means world X/Z; on YZ it
means Y/Z. X uses the first component, Y/Z the second. Commands use `Rigidbody.MovePosition` when a
body exists, or the Transform otherwise.

`IsMoving` and movement events describe the automatic pointer loop. `arrivalThreshold` applies to point
modes; it does not discard valid small steps in delta modes.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `AxisPlane` | Axis Plane. |
| `IsMoving` | Is Moving. |
| `MoveMode` | Move Mode. |
| `OnMoveStart` | On Move Start. |
| `OnMoveStop` | On Move Stop. |
| `arrivalThreshold` | Arrival Threshold. |
| `groundMask` | Ground Mask. |
| `invertDeltaX` | Invert Delta X. |
| `invertDeltaY` | Invert Delta Y. |
| `mode` | Mode. |
| `mouseButton` | Mouse Button. |
| `plane` | Plane. |
| `pxToWorld` | Px To World. |

## See Also

- [Module Root](../README.md)
