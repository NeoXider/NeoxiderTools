# FieldGenerator

**Purpose:** GridSystem core. This component generates and stores the runtime grid, shape, coordinates, and cell state. Gameplay layers work on top of `FieldGenerator`.

## Cell State

`FieldCell` contains:

- `Position` - logical coordinates.
- `IsEnabled` - cell belongs to the active board shape.
- `IsWalkable` - cell can be used by pathfinding/gameplay rules.
- `IsOccupied` - cell is occupied by a runtime object.
- `ContentId` - gameplay content: Match3 tile, X/O mark, 2048 value, item id.
- `Type` - custom cell/tile type.
- `Flags` - optional markers.
- `UserData` - optional custom C# payload.

## Config

- `Size` - dimensions requested for the next `GenerateField()` call. A zero depth becomes one layer.
- `GridType` - `Rectangular`, `Hexagonal`, `Custom`.
- `MovementRule` - neighbor direction set.
- `ShapeMask` - ScriptableObject shape mask.
- `DisabledCells` / `ForcedEnabledCells` - shape overrides.
- `BlockedCells` / `ForcedWalkableCells` - walkability overrides.
- `PassabilityMode` - how pathfinding treats occupied cells.
- `Origin2D`, `OriginDepth`, `OriginOffset` - logical-to-Unity Grid anchoring.

## Main API

- `GenerateField(config)` - create/recreate cells.
- `GeneratedSize` - current array dimensions, or `(0, 0, 0)` when no cells exist. Pending `Config.Size` edits do not resize the current board or shift its origin.
- `GetCell(...)` - get a cell.
- `GetAllCells(includeDisabled)` - enumerate cells.
- `SetWalkable(...)`, `SetEnabled(...)`, `SetOccupied(...)`, `SetContentId(...)` - mutate state.
- `GetNeighbors(...)` - get neighbors from `MovementRule` or override directions.
- `FindPathDetailed(...)` - pathfinding with failure reason diagnostics.
- `GetCellWorldCenter(...)`, `GetCellCornerWorld(...)`, `GetCellFromWorld(...)` - conversion helpers.
- `TryGetCellPositionFromWorld(...)`, `TrySnapWorldToCellCenter(...)`, `SnapWorldToCellCenter(...)` - origin-aware helpers for drag/drop, cursor previews, and snap-to-cell placement.
- `CanPlaceContentFootprint(...)`, `PlaceContentFootprint(...)` - reusable API for multi-cell placement such as shapes, items, dice pairs, and inventory blocks.
- `GridSlotAllocator` - optional helper for ordered one-cell slot allocation on top of this placement API.

`GetCell`, `InBounds`, `Cells2D` and `GetAllCells` use the generated array. An enumeration already in progress keeps its original array if the field is regenerated between enumeration steps. New lookups and enumerations see the new board. `GenerateField()` restores a default configuration if `Config` has been set to null.

For pathfinding, `WalkableOnly` ignores occupancy; `WalkableEnabledAndUnoccupied` rejects occupied cells, including endpoints. `GridPathRequest.IgnoreOccupied` can override that check. Enabled/walkable requirements and `CustomPassabilityPredicate` still apply. Cell-reference `FindPath` returns an empty list when either endpoint is null; it never substitutes the origin. Predicates should only inspect state and should not regenerate or mutate the field during validation.

## Placement API Example

```csharp
Vector3Int anchor;
if (!field.TryGetCellPositionFromWorld(pointerWorldPosition, out anchor))
    return;

GridPlacementEntry[] entries =
{
    new GridPlacementEntry(Vector3Int.zero, firstValue),
    new GridPlacementEntry(Vector3Int.right, secondValue)
};

if (!field.CanPlaceContentFootprint(anchor, entries))
    return;

GridPlacementResult result = field.PlaceContentFootprint(anchor, entries);

foreach (Vector3Int position in result.Positions)
{
    FieldCell cell = field.GetCell(position);
    // Spawn or update your view for cell.ContentId.
}
```

## Events

- `OnFieldGenerated`
- `OnCellChanged`
- `OnCellStateChanged`

Both footprint APIs validate all entries before writing and reject duplicate target positions. Each anchor-plus-offset component is added in `long` and checked before conversion to `Vector3Int` and board lookup. Overflow or an out-of-bounds result rejects the entire footprint without writes or events, even when earlier entries are valid. Negative offsets are allowed when their resolved cells are in bounds. Their per-cell notifications run after all content and occupancy values have been written. `notify: false` suppresses those notifications.

In the scene workflow, configure size/shape/origin on `FieldGenerator`, then use **Regenerate Field**. Runtime code that edits `Config.Size` must call `GenerateField()` explicitly. Regeneration replaces `FieldCell` instances; existing views or object bindings must refresh their references from `OnFieldGenerated`.

Coordinate labels retain their configured alignment, font size, color and text. With cell and coordinate drawing enabled, the generator creates one label style per draw invocation and reuses it for all cells. Large-grid repaint timing and appearance still need Editor verification.

## Architectural Role

`FieldGenerator` should not own game-specific rules. Match3, TicTacToe, SlidingMerge, inventory grids, and custom games attach focused services that read/write cell state.

Grid input and view layers should also reuse `FieldGenerator` conversion helpers instead of duplicating grid math in demos. For example, a drag/drop view can apply its own pointer offset, then call `SnapWorldToCellCenter` for a snapped preview and `TryGetCellPositionFromWorld` for final placement. Rule layers can use `PlaceContentFootprint` to write multi-cell pieces into `FieldCell.ContentId` without duplicating bounds/occupied checks.

## See Also

- [GridGameBuilder](GridGameBuilder.md)
- [GridPlacementEntry](GridPlacementEntry.md)
- [GridPlacementResult](GridPlacementResult.md)
- [GridSlotAllocator](GridSlotAllocator.md)
- [GridShapeMask](GridShapeMask.md)
- [FieldDebugDrawer](FieldDebugDrawer.md)
- [SlidingMerge](SlidingMerge/SlidingMergeBoardService.md)
