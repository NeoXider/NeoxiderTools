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

## Implementations in this module
- `KeyboardMover`
- `MouseMover2D`
- `MouseMover3D`

---

## See also
- [`MovementToolkit`](./README.md)
