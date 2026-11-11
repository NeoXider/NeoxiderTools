# UniversalRotator

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/UniversalRotator.cs`, namespace `Neo.Tools`) that rotates its object toward a `Transform`, a world point/direction or the mouse position (plane or physics raycast) in 2D or 3D, with optional axis limits and a selectable update loop.

**How to use:**
1. Add **Neoxider > Tools > UniversalRotator** and choose `Rotation Mode` (`Mode3D` / `Mode2D`) and `updateMode`.
2. Assign `target`, or enable `useMouseWorld` (with `targetCamera`, default `Camera.main`).
3. Optionally set `limitRange` (any range spanning 360 degrees or more, such as the default `[0, 360]`, disables limits) and call `RotateTo`, `RotateToDirection` or `RotateBy` from code.

---

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

## Limits

`limitRange` (degrees, each end within [-360, 360]) limits the local angle on the working axis: Z in `Mode2D`,
`limitedAxis3D` in `Mode3D`, measured from the `Awake` pose when `limitsRelativeToInitial` is on, otherwise from 0.

- A range spanning 360 degrees or more (`[0, 360]`, `[-180, 180]`, `[-360, 360]`, `[-90, 270]` ...) covers every
  angle and disables limits. Before this was fixed, ranges such as `[-360, 360]` normalized both ends to the same
  angle and pinned every aim to it.
- A smaller range is a sector running from the first value to the second in increasing angle; it may cross 180
  (for example `[170, 190]`, or the same sector written `[170, -170]`), and an aim outside it snaps to the nearer
  edge. A second value below the first wraps through 180, so `[90, -90]` is the rear half.
- These are the same sector rules as the [Follow](Follow.md) rotation limits, so one authored range means the
  same thing on both components.

---

## See also
- [`Move`](./README.md)
