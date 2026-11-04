# LayoutExtensions

**What it is:** extension methods that place a collection of `Transform`s with the [LayoutUtils](LayoutUtils.md)
generators (line, grid, circle, 3D grid/circle, sphere surface, spiral, sine wave). File:
`Scripts/Extensions/LayoutExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call the arrangement on the collection: `cards.ArrangeInLine(origin, Vector3.right, 1.2f)`.

---

## Methods

`ArrangeInLine`, `ArrangeInGrid`, `ArrangeInCircle`, `ArrangeInGrid3D`, `ArrangeInCircle3D`,
`ArrangeOnSphereSurface`, `ArrangeInSpiral`, `ArrangeOnSineWave` on `IEnumerable<Transform>`; plus
`pivot.ArrangeInCircle(elements, radius, …)` around a pivot and `element.ArrangeInCircle(center, radius, index, …)`
for a single transform.

Collection arrangement methods materialize the source once, then use that same ordered list for both count and
placement. One-shot enumerables are supported. Null entries retain their layout slots and are skipped during
placement; a null collection is a no-op. Positions are world positions.

```csharp
[SerializeField] private Transform[] _seats;

private void Start()
{
    _seats.ArrangeInCircle(transform.position, 4f);
}
```

## See Also
- [LayoutUtils](LayoutUtils.md)
- ← [Extensions](README.md)
