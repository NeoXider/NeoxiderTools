# TicTacToeBoardService

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `CurrentPlayer` | Current Player. |
| `IsFinished` | Is Finished. |
| `OnBoardReset` | On Board Reset. |
| `OnDrawDetected` | On Draw Detected. |
| `OnPlayerChanged` | On Player Changed. |
| `OnWinnerDetected` | On Winner Detected. |
| `Winner` | Winner. |

## Behavior

- Win detection (`TicTacToeWinChecker.GetWinner`) checks rows, columns and both diagonals independently of the field `MovementRule` (which only drives pathfinding/neighbors). Win length defaults to 3, or the smallest board dimension when larger.
- Only `PlayerX` and `PlayerO` marks count toward a win; empty and unset cells are ignored.

## See Also

- [Module Root](../README.md)