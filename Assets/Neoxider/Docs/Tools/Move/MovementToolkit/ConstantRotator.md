# ConstantRotator

Constant angular motion using a Transform, Rigidbody or Rigidbody2D.

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
| `AxisSource` | Axis Source. |
| `RotationMode` | Rotation Mode. |
| `axisSource` | Axis Source. |
| `customAxis` | Custom Axis. |
| `degreesPerSecond` | Degrees Per Second. |
| `mode` | Mode. |
| `spaceLocal` | Space Local. |
| `useDeltaTime` | Use Delta Time. |

## See Also

- [Module Root](../README.md)
