# FieldGenerator

**What it is:** the GridSystem core MonoBehaviour (`Scripts/GridSystem/FieldGenerator.cs`, namespace `Neo.GridSystem`). It generates and stores the runtime cells, board shape, coordinates and cell state, and converts between logical cells and world space through the required Unity `Grid`. Gameplay layers (Match3, TicTacToe, SlidingMerge, Dice, custom rules) read and write its cells; it owns no game rules itself.

**How to use:**
1. Add `FieldGenerator` to a GameObject (a `Grid` is added automatically) or use `GridGameBuilder`.
2. Configure `Config` in the Inspector: `Size`, `GridType`, `MovementPreset`, `ShapeMask`, cell overrides, `PassabilityMode`, origin.
3. Press **Regenerate Field** in Edit Mode, or call `GenerateField()` from code. `Awake` generates the field when none exists.
4. Read and write cells through `GetCell`, the `Set*` helpers or the placement API, and subscribe views to `OnFieldGenerated` / `OnCellStateChanged`.

---

## Cell state

`FieldCell` contains:

- `Position` - logical coordinates.
- `IsEnabled` - cell belongs to the active board shape.
- `IsWalkable` - cell can be used by pathfinding/gameplay rules.
- `IsOccupied` - cell is occupied by a runtime object or blocker.
- `ContentId` - gameplay content: Match3 tile, X/O mark, 2048 value, item id. Newly generated cells use `-1`.
- `Type` - custom cell/tile type (terrain, cost class).
- `Flags` - optional markers (`SpawnPoint`, `Goal`, two reserved bits).
- `UserData` - optional custom C# payload.

## Config

| Field | Description |
|-------|-------------|
| `Size` | Dimensions for the next `GenerateField()` call. A zero depth becomes one layer. |
| `GridType` | `Rectangular`, `Hexagonal` (axial disc inside the size) or `Custom` (only `ShapeMask` cells). |
| `MovementPreset` | Inspector-selectable neighbor rule (`FourDirections2D`, `EightDirections2D`, `DiagonalDirections2D`, `HexDirectionsX/Y`, `SixDirections3D`, `EighteenDirections3D`, `TwentySixDirections3D`). Applied on generation. `UseMovementRule` (default) keeps the rule assigned from code. |
| `MovementRule` | Neighbor directions used by `GetNeighbors`, pathfinding and merges. Code-assigned; 4-way 2D by default. |
| `ShapeMask` | Optional [GridShapeMask](GridShapeMask.md) asset. |
| `DisabledCells` / `ForcedEnabledCells` | Shape overrides. |
| `BlockedCells` / `ForcedWalkableCells` | Walkability overrides. |
| `PassabilityMode` | `WalkableOnly` ignores occupancy in pathfinding; `WalkableEnabledAndUnoccupied` rejects occupied cells. |
| `Origin2D`, `OriginDepth`, `OriginOffset` | Logical-to-Unity-Grid anchoring. |

## Methods

| Method | Description |
|--------|-------------|
| `GenerateField(config = null)` | Create or recreate cells (replaces every `FieldCell` instance) and raise `OnFieldGenerated`. |
| `ApplyShapeMask()` | Reapply shape mask and overrides to existing cells. |
| `GeneratedSize` | Current cell dimensions, `(0,0,0)` before generation. Pending `Config.Size` edits do not resize the board. |
| `GetCell(...)`, `InBounds(...)` | Lookup by `Vector3Int`, `Vector2Int` or `x, y`. |
| `GetAllCells(includeDisabled = true)` | Enumerate cells. |
| `Cells2D` | Copy of layer `z = 0`. Allocates a new array on every access; read it once. |
| `SetCell`, `SetWalkable`, `SetEnabled`, `SetOccupied`, `SetContentId` | Mutate state and raise `OnCellStateChanged`. |
| `IsCellPassable(cell, ignoreOccupied, ignoreDisabled, ignoreWalkability)` | Passability used by pathfinding. |
| `GetNeighbors(cell, directions = null)` | New list of existing neighbor cells. |
| `CollectNeighbors(cell, results, directions = null)` | Allocation-free variant: clears and fills a caller-owned list in the same direction order as `GetNeighbors`, returns the count. |
| `FindPath(...)`, `FindPathDetailed(...)` | Facade over [GridPathfinder](GridPathfinder.md). |
| `GetCellWorldCenter`, `GetCellCornerWorld`, `GetCellFromWorld` | Conversion helpers. |
| `TryGetCellPositionFromWorld`, `TrySnapWorldToCellCenter`, `SnapWorldToCellCenter`, `GetNearestCellFromWorld` | Origin-aware picking for drag/drop, cursor previews and snapping. |
| `CanPlaceContentFootprint(...)`, `PlaceContentFootprint(...)` | Multi-cell placement of shapes, items, dice pairs and inventory blocks. |

`GetCell`, `InBounds`, `Cells2D` and `GetAllCells` use the generated array. An enumeration already in progress keeps its original array if the field is regenerated between enumeration steps. `GenerateField()` restores a default configuration if `Config` has been set to null.

## Events

| Event | When |
|-------|------|
| `OnFieldGenerated` | After `GenerateField`, **Apply Shape && Overrides** and **Clear Manual Overrides**. Views must refresh their cell references here. |
| `OnCellChanged` | Legacy `SetCell` (type + walkability). |
| `OnCellStateChanged` | Per-cell changes made through the generator `Set*` helpers and `PlaceContentFootprint`, and by `GridPlacementService`, `GridSlotAllocator`, `GridMergeResolver`, `DiceBoard` / `DiceBoardService`, `SlidingMergeResolver` / `SlidingMergeBoardService` and `TicTacToeBoardService`. `Match3BoardService` writes `ContentId` directly and publishes only board-level events (`OnBoardChanged`, `OnResolvePhase`, `OnMatchesResolved`, `OnBoardShuffled`); refresh Match3 views from those. |

## Placement

Both footprint APIs validate all entries before writing and reject duplicate target positions. Each anchor-plus-offset component is added in `long` and checked before conversion and board lookup; overflow or an out-of-bounds result rejects the whole footprint without writes or events. Negative offsets are allowed when their resolved cells are in bounds. Per-cell notifications run after every content and occupancy value has been written; `notify: false` suppresses them. A listener may validate or place again while those notifications run.

```csharp
if (!field.TryGetCellPositionFromWorld(pointerWorldPosition, out Vector3Int anchor))
{
    return;
}

GridPlacementEntry[] entries =
{
    new GridPlacementEntry(Vector3Int.zero, firstValue),
    new GridPlacementEntry(Vector3Int.right, secondValue)
};

if (field.CanPlaceContentFootprint(anchor, entries))
{
    GridPlacementResult result = field.PlaceContentFootprint(anchor, entries);
    // result.Positions lists the written cells.
}
```

## Performance notes

- Cell lookups use cached generated dimensions.
- `CanPlaceContentFootprint` with an array or list of entries, `CollectNeighbors` and `GridPathfinder.TryFindPath` allocate nothing on warm calls, so per-frame previews and "can this fit anywhere" scans stay GC-free.
- `GetNearestCellFromWorld` (and the `TryGet`/`Snap` helpers built on it) resolves the containing cell in constant time on rectangular Unity grids without cell gaps (with a positive cell size on every axis that has several layers). Other layouts, points outside the board and points over excluded disabled cells fall back to a scan of all cells. A point exactly on an edge shared by two cells resolves to the containing (floored) cell that `Grid.WorldToCell` and `GetCellFromWorld` report, the one on the positive side of that edge; the old full scan returned the lower-index cell there.
- Gizmos build the coordinate label strings once per generated field instead of on every Scene repaint.

## Scene workflow

Configure size/shape/origin on `FieldGenerator`, then use **Regenerate Field**. Runtime code that edits `Config.Size` must call `GenerateField()` explicitly. Regeneration replaces `FieldCell` instances; existing views or object bindings refresh their references from `OnFieldGenerated`. Choose movement in the Inspector with `MovementPreset`; code can still assign `Config.MovementRule` (keep the preset at `UseMovementRule`). `GizmoSettings` controls the built-in debug drawing; `FieldGenerator.I` refers only to the latest initialized generator, so prefer explicit references with several boards.

## Architectural role

`FieldGenerator` should not own game-specific rules. Match3, TicTacToe, SlidingMerge, Dice, inventory grids and custom games attach focused services that read/write cell state. Input and view layers should reuse its conversion helpers instead of duplicating grid math: a drag/drop view applies its pointer offset, calls `SnapWorldToCellCenter` for the preview and `TryGetCellPositionFromWorld` for the final placement.

## See also

- [GridPathfinder](GridPathfinder.md)
- [GridGameBuilder](GridGameBuilder.md)
- [GridPlacementEntry](GridPlacementEntry.md)
- [GridPlacementResult](GridPlacementResult.md)
- [GridSlotAllocator](GridSlotAllocator.md)
- [GridShapeMask](GridShapeMask.md)
- [FieldDebugDrawer](FieldDebugDrawer.md)
- [SlidingMerge](SlidingMerge/SlidingMergeBoardService.md)
