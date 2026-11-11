# GridPathfinder

**What it is:** a static, plain C# breadth-first pathfinding service over a `FieldGenerator` (`Scripts/GridSystem/GridPathfinder.cs`, namespace `Neo.GridSystem`). Every move costs one step, so a found path has the fewest moves for the request's movement directions and passability rules. It also answers reachability queries such as tactics movement ranges and flood fills.

**How to use:**
1. Generate a field and configure passability (`IsEnabled`, `IsWalkable`, `IsOccupied`, `Config.PassabilityMode`).
2. Fill a `GridPathRequest` with `Start`, `End` and optional ignore flags, predicate and direction override.
3. Call `GridPathfinder.FindPath` (or `FieldGenerator.FindPathDetailed`) for a result object, or `TryFindPath` with your own list for allocation-free per-frame queries.
4. Call `CollectReachableCells` for "where can this unit move in N steps".

---

## Methods

| Method | Description |
|--------|-------------|
| `GridPathResult FindPath(FieldGenerator, GridPathRequest)` | Returns the path (start and end inclusive) and a `NoPathReason`. The path list is never null. |
| `bool TryFindPath(FieldGenerator, GridPathRequest, List<FieldCell> path, out NoPathReason reason)` | Clears and fills a caller-owned list. A warm call allocates nothing. Throws `ArgumentNullException` for a null list. |
| `int CollectReachableCells(FieldGenerator, GridPathRequest, int maxSteps, List<FieldCell> results, List<int> stepCounts = null)` | Every cell reachable from `request.Start` within `maxSteps` moves, in breadth-first order. `maxSteps < 0` is unlimited, `0` returns only the origin. `request.End` is ignored. |

`FieldGenerator.FindPath(...)`, `FindPathDetailed(...)` remain the component facade over the same service.

## Request fields

| Field | Description |
|-------|-------------|
| `Start`, `End` | Logical cell coordinates. |
| `IgnoreOccupied`, `IgnoreDisabled`, `IgnoreWalkability` | Relax the standard checks of `FieldGenerator.IsCellPassable`. |
| `CustomPassabilityPredicate` | Extra per-cell rule (terrain, team, doors). It runs after the standard checks and at most once per cell per query, plus the endpoint pre-checks. |
| `Directions` | Neighbor offsets for this query; the field `MovementRule` when null. Zero offsets are ignored. |

## Rules

- Both endpoints must exist and be passable; the result reports `InvalidStartOrEnd`, `StartNotPassable`, `EndNotPassable` or `NoPathFound`.
- `WalkableOnly` passability ignores occupancy; `WalkableEnabledAndUnoccupied` rejects occupied cells, including endpoints, unless `IgnoreOccupied` is set.
- Reachability includes the origin without checking its passability, because a unit usually occupies the cell it moves from. Every other result is passable.
- Predicates should only read state. They may run their own nested path query; it uses separate buffers.

## Performance

The search runs over flat index buffers sized to the largest board searched on the calling thread and reused between calls. Cost is `O(cells x directions)` per query with no per-cell allocations. Keep one `List<FieldCell>` per caller and use `TryFindPath` / `CollectReachableCells` in per-frame code (hover previews, AI ticks).

## Examples

```csharp
using System.Collections.Generic;
using Neo.GridSystem;
using UnityEngine;

private readonly List<FieldCell> _path = new();
private readonly List<FieldCell> _range = new();

bool CanWalk(FieldGenerator field, Vector3Int from, Vector3Int to)
{
    GridPathRequest request = new GridPathRequest { Start = from, End = to };
    return GridPathfinder.TryFindPath(field, request, _path, out NoPathReason reason);
}

void ShowMoveRange(FieldGenerator field, Vector3Int unitCell, int moves)
{
    GridPathRequest request = new GridPathRequest
    {
        Start = unitCell,
        CustomPassabilityPredicate = cell => cell.Type != 2 // 2 = water in this game
    };

    GridPathfinder.CollectReachableCells(field, request, moves, _range);
    // Highlight every cell in _range.
}
```

## Scene / NoCode workflow

Pathfinding has no component of its own. Configure `FieldGenerator.Config.MovementPreset` (or `MovementRule` from code), `PassabilityMode`, `BlockedCells` and the shape in the Inspector; then call the API from a controller. `FieldDebugDrawer` and `FieldGizmoSettings.DebugPath` draw a path for inspection.

## See also

- [FieldGenerator](FieldGenerator.md)
- [GridSystem module](README.md)
