# IMover

## Overview
`IMover` is a simple interface used by movement components in `Tools/Move/MovementToolkit`.
It allows other systems to control movement without knowing the concrete implementation.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/IMover.cs`

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
