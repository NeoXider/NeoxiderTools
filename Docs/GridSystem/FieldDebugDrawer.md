# FieldDebugDrawer

**What it is:** an optional Edit/Play Mode Gizmos component (`Scripts/GridSystem/FieldDebugDrawer.cs`, namespace `Neo.GridSystem`) that draws a `FieldGenerator` grid, cell states, coordinates and a debug path in the Scene view. It has no runtime gameplay effect. `FieldGenerator` also draws its own Gizmos through `GizmoSettings`; use this component when you want separate colors or a second debug layer.

**How to use:**
1. Add `FieldDebugDrawer` beside a `FieldGenerator` (or enable `DebugDrawer` in `GridGameBuilder`).
2. Keep `FieldGenerator.DebugEnabled` on and adjust colors and text settings.
3. Fill `DebugPath` (for example from a `GridPathfinder` result) and enable `DrawPath` to inspect a route.

---

## Fields

| Field | Description |
|-------|-------------|
| `GridColor` | Grid line color. |
| `WalkableCellColor`, `BlockedCellColor`, `DisabledCellColor`, `OccupiedCellColor` | Cell fill colors by state (disabled, then occupied, then walkable/blocked). |
| `DrawCoordinates`, `TextColor`, `TextFontSize` | Coordinate labels (Editor only). |
| `DrawPath`, `PathColor`, `DebugPath` | Polyline through the listed cell positions. |
| `CoordinatesColor` | Reserved; labels use `TextColor`. |

Coordinate label strings are built once per generated field and the label style is reused, so large boards do not allocate on every Scene repaint.

## See also

- [FieldGenerator](FieldGenerator.md)
- [GridPathfinder](GridPathfinder.md)
- [GridSystem module](README.md)
