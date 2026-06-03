# GridSystem

GridSystem is a constructor module for grid-based games and Unity systems. It provides the shared field core, while concrete rules are added as separate layers: GridMerge, Dice, Match3, TicTacToe, SlidingMerge for 2048-like mechanics, pathfinding, spawners, and debug/view components.

## Principle

- `FieldGenerator` owns field shape, coordinates, cells, and base state.
- Game rules live in separate services and use `FieldCell.ContentId`, `IsEnabled`, `IsWalkable`, `IsOccupied`, `Type`, and `Flags`.
- `GridGameBuilder` quickly assembles a scene object from selected modules through the Inspector.
- `GridMergeResolver` adapts the generic `Neo.Merge` connected-group engine to `FieldGenerator` cells.
- `DiceBoardService` places single/pair dice pieces and resolves dice merges through GridMerge.
- Demo/view components are replaceable and are not required by rule services.
- NoCode/Inspector workflows should call typed C# APIs instead of hiding rules inside UnityEvent chains.

## What It Can Build

- Match3 and similar swap/match games.
- TicTacToe and other cell-based board games.
- 2048, Threes, drop-and-merge, block-merge, and other sliding/merge games.
- Dice Merge and other connected-group placement puzzles.
- Tactical grids and pathfinding.
- Inventory grids, board views, puzzle layouts, and custom-shaped boards.

## Main Components

- `FieldGenerator` - field core: generation, shape, cell state, coordinates, world/grid conversion.
- `GridGameBuilder` - scene constructor that adds selected runtime modules.
- `GridShapeMask` - reusable ScriptableObject shape mask.
- `GridPathfinder` - pure pathfinding service with reason diagnostics.
- `GridMergeResolver` - connected-group merge adapter for `FieldCell.ContentId`.
- `DiceBoardService` - dice placement and dice merge service.
- `FieldSpawner` / `FieldObjectSpawner` - object spawning by cell.
- `FieldDebugDrawer` - Gizmos debug for shape and cell state.
- `Match3BoardService` - swap/match/resolve/refill.
- `TicTacToeBoardService` - turns, moves, win/draw.
- `SlidingMergeBoardService` - 2048-like slide/merge/spawn.

## Samples

Current development sample path: `Assets/Neoxider/Samples/Demo/`.

Release/UPM path before packaging: `Assets/Neoxider/Samples~/Demo/`.

GridSystem demo scenes live under `Scenes/GridSystem/`; setup/view scripts live under `Scripts/GridSystem/`.

## See Also

- [GridSystem README](GridSystem/README.md)
- [FieldGenerator](GridSystem/FieldGenerator.md)
- [GridGameBuilder](GridSystem/GridGameBuilder.md)
- [Dice](GridSystem/Dice/README.md)
- [Generic Merge](Merge/README.md)
- [SlidingMergeBoardService](GridSystem/SlidingMerge/SlidingMergeBoardService.md)
- [Match3BoardService](GridSystem/Match3/Match3BoardService.md)
- [TicTacToeBoardService](GridSystem/TicTacToe/TicTacToeBoardService.md)
