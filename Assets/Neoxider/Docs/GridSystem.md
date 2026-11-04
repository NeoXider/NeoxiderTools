# GridSystem

**What it is:** an overview of the GridSystem constructor module for grid-based games and Unity systems (`Scripts/GridSystem/`). It provides the shared field core, while concrete rules are added as separate layers: pathfinding and reachability, GridMerge, Dice, Match3, TicTacToe / n-in-a-row, SlidingMerge for 2048-like mechanics, spawners, and debug/view components.

**How to use:** start with the [GridSystem README](GridSystem/README.md) for setup and the C# API, then open the page of the layer your game needs.

---

## Principle

- `FieldGenerator` owns field shape, coordinates, cells, and base state.
- Game rules live in separate services and use `FieldCell.ContentId`, `IsEnabled`, `IsWalkable`, `IsOccupied`, `Type`, and `Flags`.
- `GridGameBuilder` quickly assembles a scene object from selected modules through the Inspector.
- `GridMergeResolver` adapts the generic `Neo.Merge` connected-group engine to `FieldGenerator` cells.
- `DiceBoardService` places single/pair dice pieces and resolves dice merges through GridMerge.
- Demo/view components are replaceable and are not required by rule services. Placement, `GridSlotAllocator`, `GridMergeResolver`, Dice, SlidingMerge and TicTacToe notify through `FieldGenerator.OnCellStateChanged` after each committed change; `Match3BoardService` publishes only board-level events (`OnBoardChanged`, `OnResolvePhase`), so Match3 views refresh from those.
- NoCode/Inspector workflows should call typed C# APIs instead of hiding rules inside UnityEvent chains.

## What it can build

- Match3 and similar swap/match games.
- TicTacToe, Gomoku and other n-in-a-row board games.
- 2048, Threes, drop-and-merge, block-merge, and other sliding/merge games.
- Dice Merge and other connected-group placement puzzles.
- Tactical grids: pathfinding, movement ranges, custom passability.
- Inventory grids, board views, puzzle layouts, and custom-shaped boards.

## Main components

- `FieldGenerator` - field core: generation, shape, cell state, coordinates, world/grid conversion, Inspector movement presets.
- `FieldGenerator` - also provides origin-aware drag/drop helpers: `TryGetCellPositionFromWorld`, `TrySnapWorldToCellCenter`, `SnapWorldToCellCenter`.
- `FieldGenerator` - also provides reusable multi-cell placement through `CanPlaceContentFootprint` and `PlaceContentFootprint`.
- `GridGameBuilder` - scene constructor that adds selected runtime modules.
- `GridShapeMask` - reusable ScriptableObject shape mask, also buildable from code.
- `GridPathfinder` - pure BFS pathfinding with reason diagnostics and reachability queries.
- `GridMergeResolver` - connected-group merge adapter for `FieldCell.ContentId`.
- `DiceBoardService` - dice placement, merge and "can the piece fit anywhere" checks.
- `FieldSpawner` / `FieldObjectSpawner` - object spawning by cell.
- `FieldDebugDrawer` - Gizmos debug for shape and cell state.
- `Match3BoardService` - swap/match/resolve/refill.
- `TicTacToeBoardService` - turns, moves, configurable win length, win/draw, winning line.
- `SlidingMergeBoardService` - 2048-like slide/merge/spawn and locked-board detection.

## Samples

The Demo sample ships hidden in `Samples~`; import it via Package Manager to get the scenes.

Release/UPM path before packaging: `Assets/Neoxider/Samples~/Demo/`; during sample development: `Assets/Neoxider/Samples/Demo/`.

GridSystem demo scenes live under `Scenes/GridSystem/`; setup/view scripts live under `Scripts/GridSystem/`.

## See also

- [GridSystem README](GridSystem/README.md)
- [FieldGenerator](GridSystem/FieldGenerator.md)
- [GridPathfinder](GridSystem/GridPathfinder.md)
- [GridPlacementEntry](GridSystem/GridPlacementEntry.md)
- [GridPlacementResult](GridSystem/GridPlacementResult.md)
- [GridGameBuilder](GridSystem/GridGameBuilder.md)
- [Dice](GridSystem/Dice/README.md)
- [Generic Merge](Merge/README.md)
- [SlidingMergeBoardService](GridSystem/SlidingMerge/SlidingMergeBoardService.md)
- [Match3BoardService](GridSystem/Match3/Match3BoardService.md)
- [TicTacToeBoardService](GridSystem/TicTacToe/TicTacToeBoardService.md)
