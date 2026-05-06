# TransformExtensions

**Purpose:** A comprehensive set of extension methods for `UnityEngine.Transform` — position, rotation, scale manipulation, 2D look-at, closest transform, children utilities.

## API (Key Methods)

### Position
| Method | Description |
|--------|-------------|
| `SetPosition(this Transform, ...)` | Set world position (all or individual axes). |
| `AddPosition(this Transform, ...)` | Add to world position. |
| `SetLocalPosition(this Transform, ...)` | Set local position. |
| `AddLocalPosition(this Transform, ...)` | Add to local position. |

### Rotation
| Method | Description |
|--------|-------------|
| `SetRotation(this Transform, ...)` | Set world rotation (Quaternion or Euler). |
| `SetLocalRotation(this Transform, ...)` | Set local rotation. |

### Scale
| Method | Description |
|--------|-------------|
| `SetScale(this Transform, ...)` | Set local scale. |
| `AddScale(this Transform, ...)` | Add to local scale. |

### Utilities
| Method | Description |
|--------|-------------|
| `LookAt2D(this Transform, ...)` | Rotate towards target on XY plane. |
| `GetClosest(this Transform, ...)` | Find closest Transform from collection. |
| `GetChildTransforms(this Transform)` | Get all first-level child Transforms. |
| `ResetTransform(this Transform)` | Reset world position, rotation, scale. |
| `ResetLocalTransform(this Transform)` | Reset local position, rotation, scale. |
| `CopyFrom(this Transform, Transform)` | Copy all Transform params from source. |
| `DestroyChildren(this Transform)` | Destroy all children. |

## See Also
- ← [Extensions](README.md)
