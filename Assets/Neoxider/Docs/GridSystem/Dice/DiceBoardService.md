# DiceBoardService

`DiceBoardService` is the MonoBehaviour wrapper for reusable Dice placement and merge mechanics on top of `FieldGenerator`.

It does not own score, spawn-pool progression, game-over rules, UI, or input. Those rules belong to a concrete game/demo controller.

## Plain C# core — `DiceBoard`

Since 9.12.0 the whole logic lives in `Neo.GridSystem.Dice.DiceBoard`, a plain C# class: `new DiceBoard(fieldGenerator)` with the same members (`CanPlace`, `Place`, `ResolveMerges`, `ClearBoard`, `EmptyContentId`, `MinMergeGroupSize`, `MergeStep`, `MaxContentId`, `RequireWalkable`) and C# events `BoardChanged` / `MergesResolved`. It still needs a live, generated Unity `FieldGenerator`; only the dice scene wrapper is optional. Use it for tests, replay logic, and custom loops over that field. `DiceBoardService` exposes the core via `Board` and re-raises its events as UnityEvents.

## Scene and UnityEvent setup

1. Add `DiceBoardService` to the field's GameObject. Its required `FieldGenerator` and `Grid` components provide the grid state and coordinate conversion.
2. Configure the field size, shape/disabled cells, walkability, and movement directions. Dice merges follow those directions; choose `FourDirections2D` for side-adjacent merges.
3. Set the dice rules in the Inspector before the first `Board` access (normally `Awake`). Inspector values are copied at initialization; use the public service properties for live rule changes from C#.
4. Generate the field before calling `Place`. `ClearBoard()` initializes enabled cells to `EmptyContentId` and unoccupied state, preserving disabled cells. It can be wired to `OnFieldGenerated` or a UI button.
5. Connect `OnBoardChanged` to board refresh actions and `OnMergesResolved` to merge feedback. Each successful placement raises one board event after its optional merge pass. A standalone merge pass raises events only when a group merges. `ClearBoard()` raises a board event even if the enabled cells are already empty.

Placement and rotation use typed C# APIs (`DicePiece`, `Vector3Int`, `CanPlace`, `Place`). UnityEvents provide scene outputs and parameterless clearing; input, scoring, and progression remain in the consuming controller.

Reserve `EmptyContentId` for empty cells: dice pools and possible merged results must use other IDs. Changing the marker does not migrate existing field content; clear or migrate the board explicitly when changing it.

## API

| Member | Description |
|--------|-------------|
| `CanPlace(DicePiece piece, Vector3Int anchor)` | Checks whether every dice cell fits inside the board and passes placement filters. |
| `Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)` | Writes the dice footprint to grid content and optionally resolves merges. |
| `ResolveMerges(IEnumerable<Vector3Int> seeds)` | Runs dice merge resolution from seed cells. |
| `ClearBoard()` | Clears enabled cells to `EmptyContentId` and unoccupied state; disabled cells are preserved. |
| `EmptyContentId` | Empty grid content id, default `-1`. |
| `MinMergeGroupSize` | Required connected same-value group size. |
| `MergeStep` | Increment applied to the merged value. |
| `MaxContentId` | Positive values cap merged content. With no explicit cap (0), results saturate at `int.MaxValue`; addition cannot wrap into negative IDs. |
| `RequireWalkable` | Whether placement and merge checks require walkable cells. |

### Placement results and events

Check `DicePlacementResult.Placed` after `Place`. A null/empty piece or rejected footprint returns an unplaced result and raises no board event. Placement requires every target cell to exist, be enabled, be unoccupied, and satisfy `RequireWalkable`; duplicate target positions are rejected. Turning off `RequireWalkable` allows non-walkable cells but still rejects disabled and occupied cells.

`PlacedPositions` records the original footprint, including positions that a subsequent merge may clear. Inspect `MergeResult.Groups` for result cells and consumed groups instead of treating every placed position as a surviving die. With `resolveMerges: false`, placement still writes content/occupancy and raises one board event; its merge result is empty and no merge event is raised. Call `ResolveMerges` later to resolve selected seed positions, or pass null to scan enabled cells.

The core raises `MergesResolved` before `BoardChanged` for a merging placement. The wrapper forwards them as `OnMergesResolved` and `OnBoardChanged`. Cell state notifications carry matching content and occupancy; board notifications follow the placement's optional merge pass. Failed validation raises neither board nor merge events.

## Upgrading to 11.0

Public method signatures, serialized component fields, and UnityEvent names are preserved. Existing scene/prefab wiring needs no migration. Account for these corrected boundary behaviors in consuming code:

- A pool containing repeated values produces different values inside a pair. With only one distinct value, even `forcePair: true` returns a single; inspect `CellCount` or `IsPair` before accessing a second cell.
- Positive weights totaling more than `int.MaxValue` now throw `ArgumentException` for `weights` before RNG use. Reduce oversized weights; they previously overflowed during aggregation or selection.
- Merged values saturate at the configured positive cap or `int.MaxValue`, rather than wrapping into unrelated negative IDs.
- Inclusive sequential pools ending at `int.MaxValue` terminate. Unrepresentable counts are rejected before allocation; representable counts still need enough memory.
- Rotation applies to every offset, including an off-anchor single cell, while preserving values and Z offsets.

## Weighted Dice Generation

`DicePieceGenerator.GenerateWeighted(...)` accepts `DiceValueWeight` entries for designer-controlled dice faces. Entries with `Weight <= 0` are ignored, and weights for repeated values are combined. A pool without positive weights, or with a sum of positive weights above `int.MaxValue`, throws `ArgumentException` for `weights` before any random draw. The upper bound keeps both aggregation and selection compatible with the injected `Func<int, int>` random source.

Forced pair generation removes the first rolled value before rolling the second one, preventing duplicated pair values when at least two positive weighted values exist. A single distinct positive weighted value produces a single piece. The ordinary `Generate` method also excludes repeated copies of the first value from the second draw.

```csharp
DicePieceGenerator pieces = new();
DiceValueWeight[] weights = { new(1, 8), new(2, 2), new(3, 1) };
DicePiece pair = pieces.GenerateWeighted(weights, true);
```

For deterministic generation, pass a random delegate to `new DicePieceGenerator(range)`. For each positive exclusive maximum it receives, the delegate must return a value in `[0, maximum)`.

## Related

- [Dice README](./README.md)
- [GridSystem module](../README.md)
- [FieldGenerator](../FieldGenerator.md)
- [GridPlacementEntry](../GridPlacementEntry.md)
- [Generic merge engine](../../Merge/README.md)
