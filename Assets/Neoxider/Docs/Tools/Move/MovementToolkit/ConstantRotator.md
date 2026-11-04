# ConstantRotator

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/MovementToolkit/ConstantRotator.cs`, namespace `Neo.Tools`) that rotates its object at a constant angular speed through the Transform, a `Rigidbody` or a `Rigidbody2D`.

**How to use:**
1. Add **Neoxider > Tools > ConstantRotator**.
2. Pick an `axisSource` (the default `None` does not rotate), `spaceLocal` and `degreesPerSecond`.
3. Change the speed at runtime with `SetDegreesPerSecond(float)`.

---

## Setup

- Add `ConstantRotator`, select `mode`, then choose `axisSource` and `spaceLocal`.
- `AxisSource.None` does not rotate. `Custom` uses `customAxis`.
- Transform motion runs in `Update`; Rigidbody motion runs in `FixedUpdate`.
- Rigidbody2D uses the axis's world Z sign. An axis lying in XY does not rotate a 2D body.
  For ordinary 2D spin, choose `LocalForward3D` or a custom Z axis.

## C# API

Call `SetDegreesPerSecond(float)` to change speed. With `useDeltaTime = true` it is degrees per second;
with `false` it is degrees per update. Negative speeds reverse rotation.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `mode` | `Transform`, `Rigidbody` or `Rigidbody2D`; a missing body falls back to the Transform. |
| `spaceLocal` | Axis in local space (true) or world space. |
| `useDeltaTime` | Degrees per second (true) or per update. |
| `axisSource` / `customAxis` | `None`, `LocalForward3D`, `Up2D`, `Right2D` or `Custom`. |
| `degreesPerSecond` | Angular speed; negative values reverse. |

## See Also

- [ConstantMover](./ConstantMover.md)
- [MovementToolkit](./README.md)
