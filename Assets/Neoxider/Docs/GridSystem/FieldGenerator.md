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

- `Size` - backing 3D cell array size.
- `GridType` - `Rectangular`, `Hexagonal`, `Custom`.
- `MovementRule` - neighbor direction set.
- `ShapeMask` - ScriptableObject shape mask.
- `DisabledCells` / `ForcedEnabledCells` - shape overrides.
- `BlockedCells` / `ForcedWalkableCells` - walkability overrides.
- `PassabilityMode` - how pathfinding treats occupied cells.
- `Origin2D`, `OriginDepth`, `OriginOffset` - logical-to-Unity Grid anchoring.

## Main API

- `GenerateField(config)` - create/recreate cells.
- `GetCell(...)` - get a cell.
- `GetAllCells(includeDisabled)` - enumerate cells.
- `SetWalkable(...)`, `SetEnabled(...)`, `SetOccupied(...)`, `SetContentId(...)` - mutate state.
- `GetNeighbors(...)` - get neighbors from `MovementRule` or override directions.
- `FindPathDetailed(...)` - pathfinding with failure reason diagnostics.
- `GetCellWorldCenter(...)`, `GetCellCornerWorld(...)`, `GetCellFromWorld(...)` - conversion helpers.
- `TryGetCellPositionFromWorld(...)`, `TrySnapWorldToCellCenter(...)`, `SnapWorldToCellCenter(...)` - origin-aware helpers for drag/drop, cursor previews, and snap-to-cell placement.
- `CanPlaceContentFootprint(...)`, `PlaceContentFootprint(...)` - reusable API for multi-cell placement such as shapes, items, dice pairs, and inventory blocks.
- `GridSlotAllocator` - optional helper for ordered one-cell slot allocation on top of this placement API.

## Placement API Example

```csharp
Vector3Int anchor;
if (!field.TryGetCellPositionFromWorld(pointerWorldPosition, out anchor))
    return;

var entries = new[]
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
