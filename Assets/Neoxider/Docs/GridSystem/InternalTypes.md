# GridSystem — Internal Types

**What it is:** a reference list of the plain data and helper types behind the GridSystem components.

**How to use:** follow the links for types with their own page; the rest are small data types used by those APIs.

---

| Type | Description |
|------|-------------|
| `FieldCell` | Game field cell: position, type, enabled/walkable/occupied state, content id, flags, payload. |
| `FieldGeneratorConfig` | Field generator config: size, grid type, movement, shape, overrides, passability, origin. |
| `GridMovementPreset` | Inspector-selectable built-in movement rule (`FieldGeneratorConfig.MovementPreset`). |
| `MovementRule` | Neighbor offsets; static presets and `FromPreset(GridMovementPreset)`. |
| `GridPathfinder` | Breadth-first pathfinding and reachability. See [GridPathfinder](GridPathfinder.md). |
| `GridPathRequest`, `GridPathResult`, `NoPathReason` | Path query input, output and failure reason. |
| `Match3MatchFinder` | Horizontal/vertical run search used by `Match3BoardService`: `FindMatches` (groups), allocation-free `HasMatch` and `CollectMatchedCells` (reused set). |
| `Match3TileState` | Built-in Match-3 tile ids. |
| `TicTacToeCellState` | Tic-tac-toe cell state (`Empty`, `PlayerX`, `PlayerO`). |
| `TicTacToeWinChecker` | n-in-a-row winner and winning-line detection. See [TicTacToeBoardService](TicTacToe/TicTacToeBoardService.md). |
| `SlidingMergeResolver`, `SlidingMergeResult`, `SlidingMergeStep` | 2048-style rules and move results. See [SlidingMergeBoardService](SlidingMerge/SlidingMergeBoardService.md). |

## See also

- [GridSystem](README.md)
