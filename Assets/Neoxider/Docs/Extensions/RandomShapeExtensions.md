# RandomShapeExtensions

**What it is:** extension methods that return random points inside or on the surface of bounds, circles, spheres
and colliders. File: `Scripts/Extensions/RandomShapeExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call the method on the shape: `spawnArea.bounds.RandomPointInBounds()`.

---

## Methods

| Method | Description |
|--------|-------------|
| `Vector3 RandomPointInBounds(this Bounds)` | Uniform point inside the box. |
| `Vector3 RandomPointOnBounds(this Bounds)` | Point on the box surface. |
| `Vector2 RandomPointInCircle(this Circle)` / `RandomPointOnCircle(this Circle)` | Inside / on the circumference of a [`Circle`](Shapes.md). |
| `Vector3 RandomPointInSphere(this Sphere)` / `RandomPointOnSphere(this Sphere)` | Inside / on the surface of a [`Sphere`](Shapes.md). |
| `Vector3 RandomPointInBounds(this Collider)` | Inside the collider's world AABB (not its exact shape). |
| `Vector2 RandomPointInBounds(this Collider2D)` | Inside the 2D collider's world AABB. |

All methods use `UnityEngine.Random`. Collider overloads sample the axis-aligned bounds, so rotated or concave
colliders can produce points outside the physical shape; verify with `Collider.ClosestPoint` when that matters.

## See Also
- [Shapes](Shapes.md)
- [RandomExtensions](RandomExtensions.md)
- ← [Extensions](README.md)
