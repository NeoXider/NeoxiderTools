# DebugGizmos

**What it is:** a static helper (not a component) that draws common debug gizmos: bounds outlines, an average
position marker and lines to targets. File: `Scripts/Extensions/DebugGizmos.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Call the methods from your component's `OnDrawGizmos` or `OnDrawGizmosSelected`.
2. Pass `changeColor: false` to keep the current `Gizmos.color`, or a `color` to draw in a specific one.

---

## Methods

| Method | Description |
|--------|-------------|
| `DrawBounds(Bounds, bool changeColor = true, Color? color = null)` | Wire cube of the bounds. |
| `DrawAveragePosition(Vector3 center, bool changeColor = true, Color? color = null, float radius = 0.5f)` | Wire sphere at a position (for example from `GetAveragePosition`). |
| `DrawLineToClosest(Vector3 from, GameObject closest, bool changeColor = true, Color? color = null)` | Line to the closest object (for example from `FindClosest`); a null target draws nothing. |
| `DrawConnections(Vector3 from, IEnumerable<GameObject> targets, bool changeColor = true, Color? color = null)` | Lines to every non-null target. |

With `changeColor` true the default color is red and the previous `Gizmos.color` is restored afterwards.

```csharp
private void OnDrawGizmosSelected()
{
    DebugGizmos.DrawBounds(_targets.GetCombinedBounds(), color: Color.cyan);
    DebugGizmos.DrawLineToClosest(transform.position, _targets.FindClosest(transform.position));
}
```

Gizmos are editor-only drawing; these calls have no effect in a player build.

## See Also
- [GameObjectArrayExtensions](GameObjectArrayExtensions.md)
- ← [Extensions](README.md)
