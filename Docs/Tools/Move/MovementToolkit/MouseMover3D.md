# MouseMover3D

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/MovementToolkit/MouseMover3D.cs`, namespace `Neo.Tools`) for pointer-driven motion in XY, XZ, YZ or along one axis, optionally through a 3D `Rigidbody`.

**How to use:**
1. Add **Neoxider > Tools > MouseMover3D**, choose `mode` and `plane`, and tune `speed` / `pxToWorld`.
2. Tag the scene camera `MainCamera`; set `groundMask` for click-to-point on terrain.
3. Connect `OnMoveStart` / `OnMoveStop`, or drive it from code through `IMover`.

---

## Setup

- Add `MouseMover3D`, choose `mode` and `plane`, and tune `speed` / `pxToWorld`.
- The camera is `Camera.main`, resolved in `Awake` and again whenever the cached camera has been destroyed
  (scene reload, camera swap). Point modes first raycast `groundMask` when configured, then fall back to a
  plane through the object.
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
| `mode` | `DeltaNormalized`, `DeltaRaw`, `MoveToPointHold`, `ClickToPoint` or `Direction`. |
| `plane` | Plane or single axis: `XZ`, `XY`, `YZ`, `X`, `Y`, `Z`. |
| `speed` | Units per second for normalized and point modes. |
| `pxToWorld` | Pixel-to-world factor for delta modes. |
| `groundMask` | Optional raycast layers for point modes; empty uses a plane through the object. |
| `mouseButton` / `deltaOnlyWhenButtonHeld` | Button index and whether delta modes need it held. |
| `invertDeltaX` / `invertDeltaY` | Invert pointer deltas. |
| `arrivalThreshold` | Stop distance for point modes. |
| `OnMoveStart` / `OnMoveStop` | Automatic movement started or stopped. |

## See Also

- [MouseMover2D](./MouseMover2D.md)
- [MovementToolkit](./README.md)
