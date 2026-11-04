# TransformExtensions

**What it is:** extension methods for `UnityEngine.Transform`: per-axis position/rotation/scale changes, 2D
look-at, nearest transform, child utilities and resets. File: `Scripts/Extensions/TransformExtensions.cs`,
namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call the method on a transform: `transform.SetPosition(y: 0f)`, `transform.LookAt2D(target.position)`.

Most methods are null-safe: a null transform makes them a no-op (or return null/empty).

---

## Methods

### Position, rotation, scale
| Method | Description |
|--------|-------------|
| `SetPosition` / `AddPosition` | World position: a full vector and/or individual axes (`x:`, `y:`, `z:`). |
| `SetLocalPosition` / `AddLocalPosition` | Local position, same parameters. |
| `SetRotation` / `AddRotation` | World rotation from a `Quaternion`, Euler angles or individual axes. |
| `SetLocalRotation` / `AddLocalRotation` | Local rotation, same parameters. |
| `SetScale` / `AddScale` | Local scale. |

### Utilities
| Method | Description |
|--------|-------------|
| `LookAt2D(Vector3 target, float offset = 0f)` | Rotates around Z so the transform's up axis faces the target on the XY plane. A target on the same XY position (or a null transform) leaves the rotation unchanged. |
| `IEnumerator SmoothLookAtRoutine(Vector3 target, float speed)` | Coroutine body that slerps toward the target; run with `StartCoroutine`. |
| `Transform GetClosest(IEnumerable<Transform> others)` | Nearest other transform by squared distance, skipping null/destroyed entries and the transform itself. Arrays and lists are scanned without allocating. Null when nothing qualifies or the transform is null. |
| `Transform[] GetChildTransforms()` | Direct children. |
| `ResetTransform()` / `ResetLocalTransform()` | Zero position, identity rotation, unit scale (world / local). |
| `CopyFrom(Transform source)` | Copies world position, rotation and local scale. |
| `DestroyChildren()` | Destroys every child (`DestroyImmediate` outside Play Mode). |

```csharp
transform.SetPosition(y: groundHeight);
transform.LookAt2D(enemy.position, offset: 0f);
Transform nearest = transform.GetClosest(_waypoints);
```

## See Also
- [GameObjectArrayExtensions](GameObjectArrayExtensions.md)
- ← [Extensions](README.md)
