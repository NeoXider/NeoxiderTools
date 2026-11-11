# LayoutUtils

**What it is:** static generators of layout positions (line, grid, circle, 3D grid/circle, sphere surface, spiral,
sine wave). Each returns an `IEnumerable<Vector3>` of world positions. File: `Scripts/Extensions/LayoutUtils.cs`,
namespace `Neo.Extensions`.

**How to use:**
1. Call a generator with the number of points and the shape parameters.
2. Iterate the result once, or use [LayoutExtensions](LayoutExtensions.md) to place transforms directly.

---

## Methods

| Method | Shape |
|--------|-------|
| `GetLine(count, origin, direction, spacing)` | Evenly spaced along a direction. |
| `GetGrid(count, origin, columns, spacingX, spacingY)` | Rows of `columns` on the XY plane. |
| `GetCircle(count, center, radius, rotationOffset = 0)` | Circle on the XY plane. |
| `GetGrid3D(count, origin, columns, right, up, spacingX, spacingY)` | Grid on a custom plane spanned by `right`/`up`. |
| `GetCircle3D(count, center, normal, radius, rotationOffset = 0)` | Circle on the plane with the given normal. |
| `GetSphereSurface(count, center, radius)` | Simple spherical mapping (not perfectly uniform). |
| `GetSpiral(count, center, radiusStep, angleStep)` | 2D spiral. |
| `GetSineWave(count, origin, direction, amplitude, frequency, spacing)` | Points along a sine wave. |

```csharp
foreach (Vector3 slot in LayoutUtils.GetCircle(8, transform.position, 3f))
{
    Instantiate(_coinPrefab, slot, Quaternion.identity);
}
```

## See Also
- [LayoutExtensions](LayoutExtensions.md)
- ← [Extensions](README.md)
