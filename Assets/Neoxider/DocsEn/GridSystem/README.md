# GridSystem module

## Purpose

GridSystem is the base constructor module for grid-based games and tools. It separates the generic field core from optional gameplay layers, so the same grid can power Match3, TicTacToe, 2048-like games, tactical boards, inventory grids, puzzle fields, and custom board games.

## Architecture

- `FieldGenerator` - core field generation, shape, cells, coordinates, state, and pathfinding facade.
- `GridGameBuilder` - scene/Inspector helper that assembles selected modules.
- `GridShapeMask` - reusable ScriptableObject shape masks.
- `FieldSpawner` / `FieldObjectSpawner` - object placement on cells.
- `FieldDebugDrawer` - Gizmos diagnostics.
- `GridMergeResolver` - adapter from generic `Neo.Merge` connected-group rules to `FieldGenerator` cells.
- `DiceBoardService` - reusable dice placement and dice merge layer.
- `Match3BoardService` - swap/match/resolve/refill gameplay layer.
- `TicTacToeBoardService` - turn-based board-game layer.
- `SlidingMergeBoardService` - 2048, Threes, block-merge, and drop-and-merge layer.

## Documentation

- [FieldGenerator](./FieldGenerator.md)
- [GridPlacementEntry](./GridPlacementEntry.md)
- [GridPlacementResult](./GridPlacementResult.md)
- [GridGameBuilder](./GridGameBuilder.md)
- [GridShapeMask](./GridShapeMask.md)
- [FieldDebugDrawer](./FieldDebugDrawer.md)
- [FieldSpawner](./FieldSpawner.md)
- [FieldObjectSpawner](./FieldObjectSpawner.md)
- [InternalTypes](./InternalTypes.md)
- [Dice](./Dice/README.md)
- [Generic Merge](../Merge/README.md)
- [SlidingMerge](./SlidingMerge/SlidingMergeBoardService.md)
- [Match3](./Match3/Match3BoardService.md)
- [TicTacToe](./TicTacToe/TicTacToeBoardService.md)

## Quick Start

1. Add `GridGameBuilder` to a GameObject.
2. Select features, for example `DebugDrawer + SlidingMerge` for a 2048-like game.
3. Configure `FieldGenerator.Config`: `Size`, `GridType`, `MovementRule`, origin, and shape overrides.
4. Press `Ensure Grid Components` or enter Play Mode.
5. Connect your own view/UI to the selected gameplay service events.

## Samples

Current active sample path: `Assets/Neoxider/Samples/Demo/`.

GridSystem scenes live in `Scenes/GridSystem/`; setup/view scripts live in `Scripts/GridSystem/`.

## Russian

See Russian docs: [`../../Docs/GridSystem/README.md`](../../Docs/GridSystem/README.md).
