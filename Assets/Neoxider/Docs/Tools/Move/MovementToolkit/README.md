# Tools / Move / MovementToolkit

Small movement and rotation components in namespace `Neo.Tools`, assembly `Neo.Tools.Move`.

## Choose a component

- [KeyboardMover](./KeyboardMover.md): keyboard/gamepad motion and typed `IMover` commands.
- [MouseMover2D](./MouseMover2D.md): pointer deltas or point targets, with optional `Rigidbody2D` on XY.
- [MouseMover3D](./MouseMover3D.md): pointer motion with optional `Rigidbody` and ground raycasts.
- [ConstantMover](./ConstantMover.md): constant motion along an authored direction.
- [ConstantRotator](./ConstantRotator.md): constant rotation around an authored axis.
- [IMover](./IMover.md): shared command contract.

## Plane coordinates and timing

Configure the plane in the Inspector before issuing commands. A `Vector2(a, b)` maps to XY `(a, b)`,
XZ `(a, b)` as world X/Z, or YZ `(a, b)` as world Y/Z. Point commands preserve the coordinate outside
that plane. Single-axis mouse modes use X from `a`, and Y/Z from `b`.

`MoveDelta` applies world units per call; do not multiply a position delta by time again. Automatic
movement uses speed in units per second and its configured update loop. Small nonzero movement steps
are applied even below `0.01` units; an arrival threshold belongs to point-following modes only.

Use `Rigidbody2D` on XY. For XZ/YZ motion use a Transform or `MouseMover3D` with a 3D Rigidbody.
Mouse components and the camera rotation helpers reuse `MouseInputCompat` for legacy/new input
compatibility. The two mouse movers resolve `Camera.main` in `Awake`; create/tag the camera before
activating them.

## Code and Inspector workflows

```csharp
using Neo.Tools;
using UnityEngine;

// With KeyboardMover configured for XZ in the Inspector:
IMover mover = GetComponent<KeyboardMover>();
mover.MoveDelta(new Vector2(0.1f, 0.2f)); // world X += 0.1, Z += 0.2
mover.MoveToPoint(new Vector2(4f, 6f));  // world X/Z target, preserve world Y
```

For pointer-driven scenes, select a mouse mover's `Move Mode`, plane, sensitivity/speed and mouse
button. Connect `OnMoveStart` / `OnMoveStop` to scene effects. These automatic movement events describe
applied steps; direct `IMover` commands do not emit them or update `IsMoving`.
