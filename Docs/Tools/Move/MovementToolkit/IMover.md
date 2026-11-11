# IMover

**What it is:** the movement command interface (`Scripts/Tools/Move/MovementToolkit/IMover.cs`, namespace `Neo.Tools`) implemented by `KeyboardMover`, `MouseMover2D` and `MouseMover3D`. Other systems can move an object through it without knowing the concrete mover.

**How to use:**
1. Get the mover as `IMover` (`GetComponent<KeyboardMover>()` or any implementation).
2. Call `MoveDelta(Vector2)` for a displacement or `MoveToPoint(Vector2)` for a target in the mover's plane.
3. Read `IsMoving` for the automatic, input-driven movement state.

---

## Members
- `bool IsMoving { get; }`
- `void MoveDelta(Vector2 delta)`
- `void MoveToPoint(Vector2 worldTarget)`

Commands use world units in the component's configured plane: XY, XZ or YZ. For example, `(4, 6)`
means world X = 4 and Z = 6 on XZ. `MoveToPoint` preserves the perpendicular coordinate; `MoveDelta`
is a displacement per call, independent of the configured automatic movement speed.

`KeyboardMover` and `MouseMover2D` expose immediate Transform commands separately from automatic
input-driven motion. `MouseMover3D` commands use a Rigidbody when present. Their `IsMoving` and
start/stop events describe automatic movement. See each component for body support.

## Implementations in this module
- `KeyboardMover`
- `MouseMover2D`
- `MouseMover3D`

---

## See also
- [`MovementToolkit`](./README.md)
