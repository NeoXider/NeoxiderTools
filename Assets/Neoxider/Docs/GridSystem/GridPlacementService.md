# GridPlacementService

**What it is:** `Neo.GridSystem.GridPlacementService` — a rule-driven placement layer over the `FieldGenerator` placement API, plain C# (no MonoBehaviour). Gameplay services reuse one request shape instead of growing new `FieldGenerator` overloads.

**GridPlacementRequest:** `Anchor` + `Entries` (`GridPlacementEntry` footprint; `GridPlacementRequest.Single(...)` for one cell), the standard rules `RequireEnabled` / `RequireWalkable` / `RequireUnoccupied`, an optional `CellPredicate` (`Func<FieldCell, bool>`), the `OverwritePolicy` (`Reject` keeps occupied cells intact, `Overwrite` replaces their content), and `Notify` (raise `OnCellStateChanged` per written cell).

**Usage:**

```csharp
var service = new GridPlacementService(fieldGenerator);
var request = GridPlacementRequest.Single(new Vector3Int(2, 3, 0), contentId: 7);
request.CellPredicate = cell => cell.Type != BLOCKED_TYPE;

if (service.CanPlace(request, out string reason))
{
    GridPlacementResult result = service.Place(request); // atomic: all cells or none
}
```

Validation covers bounds, enabled/walkable/occupied state, the predicate, and the overwrite policy; failures return a readable `FailureReason` and never partially write cells. A successful `Place` writes `ContentId` and occupancy exactly like `FieldGenerator.PlaceContentFootprint`.

**See also:** [FieldGenerator](./FieldGenerator.md), [GridPlacementEntry](./GridPlacementEntry.md), [GridPlacementResult](./GridPlacementResult.md).
