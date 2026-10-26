# UniversalRotator

## Overview
`UniversalRotator` is a flexible rotation component for both 2D and 3D.
It can aim at a `Transform`, a world point/direction, or mouse position (plane or physics raycast),
optionally with axis limits and selectable update loop.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Move/UniversalRotator.cs`

## Common setups
- **2D turret**: `Rotation Mode = Mode2D`, `Use Mouse World = true`, optional angle limits
- **3D guard**: `Rotation Mode = Mode3D`, set target transform, adjust `worldUp`

## Public methods (selected)
- `void SetTarget(Transform newTarget)`
- `void ClearTarget()`
- `void RotateTo(Vector3 worldPoint, bool instant = false)`
- `void RotateToDirection(Vector3 worldDirection, bool instant = false)`
- `void RotateBy(float deltaDegrees)`
- `void RotateBy(Vector3 eulerDelta)`

`RotateTo` and `RotateToDirection` aim in world space in both modes. A rotated parent does not add its
rotation to the requested 2D aim. Limits remain local to the current parent; runtime reparenting uses
that new parent for world/local conversion. `limitsRelativeToInitial` still refers to the local pose
captured in `Awake`.

For scene/Inspector use, choose a rotation mode and update loop, assign `target` or enable mouse mode,
and configure `limitRange`. Mouse mode reads through `MouseInputCompat` for legacy/new input support.

---

## See also
- [`Move`](./README.md)
