# GridPlacementService

**What it is:** `Neo.GridSystem.GridPlacementService` — a rule-driven placement layer over the `FieldGenerator` placement API, plain C# (no MonoBehaviour). Gameplay services reuse one request shape instead of growing new `FieldGenerator` overloads.

**GridPlacementRequest:** `Anchor` + `Entries` (`GridPlacementEntry` footprint; `GridPlacementRequest.Single(...)` for one cell), the standard rules `RequireEnabled` / `RequireWalkable` / `RequireUnoccupied`, an optional `CellPredicate` (`Func<FieldCell, bool>`), the `OverwritePolicy` (`Reject` keeps occupied cells intact, `Overwrite` replaces their content), and `Notify` (raise `OnCellStateChanged` per written cell).

**Usage:**

```csharp
GridPlacementService service = new GridPlacementService(fieldGenerator);
GridPlacementRequest request = GridPlacementRequest.Single(new Vector3Int(2, 3, 0), contentId: 7);
request.CellPredicate = cell => cell.Type != BLOCKED_TYPE;

if (service.CanPlace(request, out string reason))
{
    GridPlacementResult result = service.Place(request); // atomic: all cells or none
}
```

Validation covers bounds, enabled/walkable/occupied state, the predicate, and the overwrite policy; failures return a readable `FailureReason` and never partially write cells. A successful `Place` writes `ContentId` and occupancy exactly like `FieldGenerator.PlaceContentFootprint`.

Every target coordinate must be unique, including requests that allow overwriting occupied cells. Duplicate, null, overflowing or out-of-bounds entries reject the whole request without writes or notifications. All three `Anchor + Offset` components use widened arithmetic before conversion and lookup; even `int.MinValue + int.MinValue` cannot wrap onto cell zero. Negative offsets remain valid when the resolved cells fit the generated field. An anchor outside the board may still be used if every resolved target is valid.

Keep `CellPredicate` free of board mutations. Keep the request and its entries stable during `CanPlace` / `Place`; predicates and entry enumeration must not regenerate the board. This API does not provide a transaction over arbitrary callback mutations.

When `Notify` is true, notifications retain footprint order and run after the complete footprint has committed. A listener on the first cell can read the final content and occupancy of all placed cells. `Notify = false` suppresses the notifications while preserving the placement result.

For scene workflows, keep input and view references on a MonoBehaviour, construct this service with that scene's `FieldGenerator`, and render the cells returned in `GridPlacementResult`. No ScriptableObject needs a scene object reference.

**See also:** [FieldGenerator](./FieldGenerator.md), [GridPlacementEntry](./GridPlacementEntry.md), [GridPlacementResult](./GridPlacementResult.md).
