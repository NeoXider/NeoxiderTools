# TicTacToeBoardService

**What it is:** a scene service (`Scripts/GridSystem/TicTacToe/TicTacToeBoardService.cs`, namespace `Neo.GridSystem.TicTacToe`) for turn-based marks, win and draw detection and round reset over a generated field. It covers classic TicTacToe and n-in-a-row variants (Gomoku-style 5 in a row, custom-shaped boards). Views and input are replaceable; the winner logic is the static `TicTacToeWinChecker`.

**How to use:**
1. Add `TicTacToeBoardService` beside a `FieldGenerator`, typically sized `(3, 3, 1)`.
2. Configure shape holes, blocked cells and external occupancy on the field; set `Win Line Length` (0 = automatic) for larger boards.
3. Keep reset-on-start enabled, or call `ResetBoard()` before accepting moves.
4. Call `TryMakeMove(position)` from input, connect player/winner/draw/reset events to UI, and refresh cell views from `FieldGenerator.OnCellStateChanged`.

---

## Fields

| Field | Description |
|-------|-------------|
| `Reset On Start` | Call `ResetBoard()` in `Start`. |
| `Win Line Length` | Marks in a row needed to win. `0` = automatic: 3, or the smallest generated dimension when every dimension exceeds 3 (for example 4 on a 4x4x4 cube). On 2D boards the automatic length is always 3; set 5 for Gomoku. |

## Runtime API

| Member | Description |
|--------|-------------|
| `ResetBoard()` | Clear playable cells to `Empty` (`0`) and restart with player X. |
| `TryMakeMove(Vector2Int / Vector3Int)` | Place the active player's mark; return false for an invalid or finished move. |
| `IsBoardFull()` | True when no empty playable cell remains. |
| `CurrentPlayer`, `Winner`, `IsFinished` | Current round state. |
| `WinLineLength` | Runtime access to the win length; takes effect on the next move. |
| `WinningLine` | Cells of the winning run (end to end) after a win, empty otherwise. Use it to highlight the line. |
| `OnPlayerChanged`, `OnWinnerDetected` | Player state as an integer enum value. |
| `OnDrawDetected`, `OnBoardReset` | Round outcome/reset notifications. |

```csharp
TicTacToeBoardService board = GetComponent<TicTacToeBoardService>();
board.WinLineLength = 5;
board.ResetBoard();
if (board.TryMakeMove(new Vector2Int(7, 7)) && board.IsFinished && board.Winner != TicTacToeCellState.Empty)
{
    foreach (Vector3Int cell in board.WinningLine)
    {
        // Highlight cell.
    }
}
```

Pure C#: `TicTacToeWinChecker.GetWinner(field)`, `GetWinner(field, winLineLength)`, `TryGetWinningLine(field, line, out winner, winLineLength)` and `ResolveWinLineLength(field)`.

## Behavior

- Playable cells are enabled, walkable and unoccupied. Marks are stored in `ContentId`; `IsOccupied` is reserved for external blockers, not X/O marks.
- A successful move first commits the whole round state (`Winner`, `IsFinished`, `WinningLine`, `CurrentPlayer`), then raises `FieldGenerator.OnCellStateChanged` for the marked cell, then `OnWinnerDetected`, `OnDrawDetected` or `OnPlayerChanged`. `ResetBoard()` raises it for every cleared mark before `OnBoardReset`. Generator-driven views therefore stay in sync without polling.
- During that cell notification `CurrentPlayer` already holds the player who moves next (or the player who just moved when the round finished). A cell listener may call `TryMakeMove` (the reply is made for the next player) or `ResetBoard()`; the outer move then skips its own winner/draw/player events, because the nested call has already replaced that state and published its own. For AI replies prefer `OnPlayerChanged`.
- Reset preserves blockers and their content. Moves cannot target blockers, blockers break win lines, and empty blocked cells do not prevent a draw.
- Win detection checks rows, columns and both diagonals independently of the field `MovementRule`. Only `PlayerX` and `PlayerO` marks count; empty and unset cells are ignored.
- `GetWinner(field, winDirections)` accepts custom axes. Each axis is scanned in both directions; zero offsets are ignored, including a list containing only zero offsets.
- The service lazily resolves its required generator for direct public calls before `Awake`. It does not require a demo view.

## See also

- [GridSystem module](../README.md)
- [FieldGenerator](../FieldGenerator.md)
