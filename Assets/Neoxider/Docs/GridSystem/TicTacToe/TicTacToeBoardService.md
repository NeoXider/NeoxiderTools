# TicTacToeBoardService

**Purpose:** turn-based marks, win detection, draw detection and round reset over a generated field. Views and input are replaceable.

## Setup

1. Add `TicTacToeBoardService` beside a `FieldGenerator`, typically sized `(3, 3, 1)`.
2. Configure shape holes, blocked cells and external occupancy on the field.
3. Keep reset-on-start enabled, or call `ResetBoard()` before accepting moves. Reset changes playable cell content to `TicTacToeCellState.Empty` (`0`).
4. Connect player/winner/draw/reset events to scene UI and board views.

## Runtime API

| Field | Description |
|-------|-------------|
| `ResetBoard()` | Clear playable cells and restart with player X. |
| `TryMakeMove(Vector2Int / Vector3Int)` | Place the active player's mark; return false for an invalid or finished move. |
| `IsBoardFull()` | Return true when no empty playable cell remains. |
| `CurrentPlayer`, `Winner`, `IsFinished` | Read the current round state. |
| `OnPlayerChanged`, `OnWinnerDetected` | Player state as an integer enum value. |
| `OnDrawDetected`, `OnBoardReset` | Round outcome/reset notifications. |

```csharp
TicTacToeBoardService board = GetComponent<TicTacToeBoardService>();
board.ResetBoard();
bool accepted = board.TryMakeMove(new Vector2Int(1, 1));
```

## Behavior

- Playable cells are enabled, walkable and unoccupied. Marks are stored in `ContentId`; `IsOccupied` is reserved for external blockers, not X/O marks.
- Reset preserves blockers and their content. Moves cannot target blockers, blockers break win lines, and empty blocked cells do not prevent a draw.
- Win detection (`TicTacToeWinChecker.GetWinner`) checks rows, columns and both diagonals independently of the field `MovementRule`. Win length is at least 3; the existing dimension rule raises it only when all three generated dimensions exceed 3.
- Only `PlayerX` and `PlayerO` marks count toward a win; empty and unset cells are ignored.
- `GetWinner(field, winDirections)` accepts custom axes. Each axis is scanned in both directions; zero offsets are ignored, including a list containing only zero offsets.
- The service lazily resolves its required generator for direct public calls before `Awake`. It does not require a demo view.

## See Also

- [Module Root](../README.md)
