# GridSystem module

## Purpose

GridSystem is the base constructor module for grid-based games and tools. It separates the generic field core from optional gameplay layers, so the same grid can power Match3, TicTacToe, 2048-like games, tactical boards, inventory grids, puzzle fields, and custom board games.

## Architecture

- `FieldGenerator` - core field generation, shape, cells, coordinates, state, and pathfinding facade.
- `GridGameBuilder` - scene/Inspector helper that assembles selected modules.
- `GridSlotAllocator` - ordered one-cell slot allocation for benches, tactical rows, and autobattler boards.
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
- [GridPlacementService](./GridPlacementService.md)
- [GridPlacementEntry](./GridPlacementEntry.md)
- [GridPlacementResult](./GridPlacementResult.md)
- [GridSlotAllocator](./GridSlotAllocator.md)
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

## Runtime C# API

Reference the `Neo.GridSystem` assembly from your own asmdef. Use explicit component references for multiple boards; `FieldGenerator.I` refers only to the latest initialized generator.

```csharp
using Neo.GridSystem;

FieldGenerator field = GetComponent<FieldGenerator>();
field.GenerateField(new FieldGeneratorConfig(new Vector3Int(4, 4, 1)));
GridPlacementService placement = new GridPlacementService(field);
GridPlacementRequest request = GridPlacementRequest.Single(Vector3Int.zero, contentId: 7);
GridPlacementResult result = placement.Place(request);
```

`Config.Size` describes the next generation; `GeneratedSize` describes the current cells. C# callers must call `GenerateField()` to apply a new size. Cell lookup, slot indices, world anchoring, gameplay scans and Gizmos use the generated dimensions while a resize is pending.

Placement rejects duplicate target coordinates and validates the complete footprint before writing. Anchor-plus-offset arithmetic is widened on all three axes before range and board-bound checks; invalid values cannot wrap onto a valid cell. Valid negative offsets remain supported. Placement and `GridMergeResolver` notifications run after the complete operation has committed, so observers can read the final board. `Notify = false` / `NotifyOnContentChanged = false` let a caller publish its own update after additional state changes.

`GridMergeResolver.Resolve(field, request)` scans enabled cells by default. Set `RequireEnabled = false` to include disabled cells when `Seeds` is null; set `RequireWalkable = false` as well if those cells are not walkable. `Mutate = false` returns a merge preview without writing cell content or raising change notifications. Occupancy remains caller-owned in this resolver.

`GridMergeRequest.Increment(..., step)` saturates the result at `int.MinValue` / `int.MaxValue`. The resolver's default calculator uses the same policy for `content + 1`. Successful groups still clear their non-result cells, including when saturation retains the source value. A custom `GetMergedContent` delegate keeps its own numeric policy; the optional Dice layer retains its configured upper cap.

`FieldObjectSpawner` keeps surviving objects bound to their original cell instances when the field is regenerated. Removing a detached object retires its metadata and destroys that instance without freeing a new occupant at the same coordinate. See [FieldObjectSpawner](FieldObjectSpawner.md) for callback and external-despawn ownership rules.

## Scene and Inspector Workflow

Use `GridGameBuilder.EnsureConfigured()` to add selected wrappers. Field generation, shape and origin buttons support authoring in Edit Mode. Match3 and SlidingMerge gameplay buttons are available in Play Mode. Their public methods remain callable from C#; UnityEvents expose results to scene views and UI.

Use one gameplay service per board unless your own composition coordinates its content and occupancy conventions. Spawners accept scene prefab references, while `GridShapeMask` assets contain only reusable coordinates and mask settings.

## Samples

The Demo sample ships hidden in `Samples~`; import it via Package Manager to get the scenes.

The release root is `Assets/Neoxider/Samples~`; active sample development uses `Assets/Neoxider/Samples`. Package Manager imports copies into `Assets/Samples/NeoxiderTools/<version>/<sample name>`.

GridSystem scenes live in `Scenes/GridSystem/`; setup/view scripts live in `Scripts/GridSystem/`.

