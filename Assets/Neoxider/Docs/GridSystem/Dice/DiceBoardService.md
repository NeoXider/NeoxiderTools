# DiceBoardService

`DiceBoardService` is the MonoBehaviour wrapper for reusable Dice placement and merge mechanics on top of `FieldGenerator`.

It does not own score, spawn-pool progression, game-over rules, UI, or input. Those rules belong to a concrete game/demo controller.

## API

| Member | Description |
|--------|-------------|
| `CanPlace(DicePiece piece, Vector3Int anchor)` | Checks whether every dice cell fits inside the board and passes placement filters. |
| `Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)` | Writes the dice footprint to grid content and optionally resolves merges. |
| `ResolveMerges(IEnumerable<Vector3Int> seeds)` | Runs dice merge resolution from seed cells. |
| `ClearBoard()` | Clears all cells to `EmptyContentId`. |
| `EmptyContentId` | Empty grid content id, default `-1`. |
| `MinMergeGroupSize` | Required connected same-value group size. |
| `MergeStep` | Increment applied to the merged value. |
| `MaxContentId` | Optional cap for merged values. |
| `RequireWalkable` | Whether placement and merge checks require walkable cells. |

## Weighted Dice Generation

`DicePieceGenerator.GenerateWeighted(...)` accepts `DiceValueWeight` entries for designer-controlled dice faces. Entries with `Weight <= 0` are ignored. A pool without positive weights throws `ArgumentException`, so invalid dice configs fail loudly. Forced pair generation removes the first rolled value before rolling the second one, preventing duplicated pair values when at least two positive weighted values exist.

## Related

- [Dice README](./README.md)
- [FieldGenerator](../FieldGenerator.md)
- [GridMerge README](../../Merge/README.md)
