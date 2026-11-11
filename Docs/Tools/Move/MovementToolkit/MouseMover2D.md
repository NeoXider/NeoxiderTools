# MouseMover2D

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/MovementToolkit/MouseMover2D.cs`, namespace `Neo.Tools`) that moves from pointer deltas, held/clicked targets or a direction dragged from the initial click, in a selected plane.

**How to use:**
1. Add **Neoxider > Tools > MouseMover2D**, then choose `mode`, `axes`, `mouseButton` and sensitivity/speed.
2. Tag the scene camera `MainCamera`; optionally add a `Rigidbody2D` (XY only).
3. Connect `OnMoveStart` / `OnMoveStop`, or drive it from code through `IMover`.

---

## Setup

- Add `MouseMover2D`, then choose `mode`, `axes`, `mouseButton` and sensitivity/speed.
- Use `Rigidbody2D` on XY only. For physics on other planes, use `MouseMover3D` with a 3D Rigidbody.
- The camera is `Camera.main`, resolved in `Awake` and again whenever the cached camera has been destroyed
  (scene reload, camera swap). Perspective and orthographic pointer targets are projected onto the selected
  plane through the mover. A parallel ray leaves the target at the
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
| `mode` | `DeltaNormalized`, `DeltaRaw`, `MoveToPointHold`, `ClickToPoint` or `Direction`. |
| `axes` | Plane or single axis: `XY`, `XZ`, `YZ`, `X`, `Y`, `Z`. |
| `speed` | Units per second for normalized and point modes. |
| `pxToWorld` | Pixel-to-world factor for `DeltaRaw`. |
| `mouseButton` / `deltaOnlyWhenButtonHeld` | Button index and whether delta modes need it held. |
| `invertDeltaX` / `invertDeltaY` | Invert pointer deltas. |
| `arrivalThreshold` | Stop distance for point modes. |
| `offset` | Plane-space offset added to point targets. |
| `OnMoveStart` / `OnMoveStop` | Automatic movement started or stopped. |

## See Also

- [MouseMover3D](./MouseMover3D.md)
- [MovementToolkit](./README.md)
