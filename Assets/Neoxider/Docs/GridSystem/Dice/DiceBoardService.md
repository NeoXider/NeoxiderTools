# DiceBoardService

`DiceBoardService` - scene wrapper for the reusable Dice grid mechanics.

## What It Does

- Requires a `FieldGenerator` on the same GameObject.
- Places `DicePiece` footprints through the generic grid placement API.
- Resolves dice merges through `GridMergeResolver`.
- Writes dice values into `FieldCell.ContentId`.
- Keeps `FieldCell.IsOccupied` synchronized with dice content.
- Raises `OnBoardChanged` once per logical placement/merge operation.

## What Stays Outside

Score, spawn-pool progression, game over rules, UI text, drag/drop input, and demo visuals belong to a game/demo controller. `DiceBoardService` only owns reusable board placement and merge mechanics.

## Key API

| API | Description |
|-----|-------------|
| `CanPlace(DicePiece piece, Vector3Int anchor)` | Checks if a dice footprint can be placed at the anchor. |
| `Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)` | Places a piece and optionally resolves dice merges. |
| `ResolveMerges(IEnumerable<Vector3Int> seeds)` | Resolves merge groups starting from seed cells. |
| `ClearBoard()` | Clears all grid content to `EmptyContentId`. |
| `EmptyContentId` | Content id treated as empty, default `-1`. |
| `MinMergeGroupSize` | Minimum connected same-value group size, default `3`. |
| `MergeStep` | Value increment after merge, default `1`. |
| `MaxContentId` | Optional merged value cap, `0` means unlimited. |
| `RequireWalkable` | Whether placement/merge requires walkable cells. |

## Weighted Dice Generation

`DicePieceGenerator.GenerateWeighted(...)` accepts `DiceValueWeight` entries for designer-controlled dice faces. Entries with `Weight <= 0` are ignored. A pool without positive weights throws `ArgumentException`, so invalid dice configs fail loudly. Forced pair generation removes the first rolled value before rolling the second one, preventing duplicated pair values when at least two positive weighted values exist.

## Merge Rule

Dice merges are configured as: connected same-value cells, side adjacency, minimum group size, result at the seed/anchor cell, and result value `old + MergeStep` capped by `MaxContentId` when configured.

## See Also

- [Dice README](./README.md)
- [FieldGenerator](../FieldGenerator.md)
- [GridMerge README](../Merge/README.md)
