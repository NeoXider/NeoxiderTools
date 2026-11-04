# Shapes

**What it is:** two small `[Serializable]` structs, `Circle` (2D center + radius) and `Sphere` (3D center +
radius), used as inputs for random point helpers. As fields of a MonoBehaviour or ScriptableObject they serialize
and show in the Inspector. File: `Scripts/Extensions/Shapes.cs`, namespace `Neo.Extensions`.

**How to use:** construct the shape and call a [RandomShapeExtensions](RandomShapeExtensions.md) method on it.

---

| Type | Fields | Constructor |
|------|--------|-------------|
| `Circle` | `Vector2 center`, `float radius` | `new Circle(center, radius)` |
| `Sphere` | `Vector3 center`, `float radius` | `new Sphere(center, radius)` |

```csharp
Vector2 drop = new Circle(chest.position, 1.5f).RandomPointInCircle();
```

## See Also
- [RandomShapeExtensions](RandomShapeExtensions.md)
- ← [Extensions](README.md)
