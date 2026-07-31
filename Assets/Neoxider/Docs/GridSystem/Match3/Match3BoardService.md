# Match3BoardService

**Purpose:** gameplay service for Match3 boards on top of `FieldGenerator`. It handles tile generation, valid swap search, match resolve, collapse, refill, and shuffle when no moves remain.

## API

- `InitializeBoard()` - fill enabled cells with starting tiles.
- `TryFindValidSwap(out a, out b)` - find an adjacent swap that creates a match without mutating the board.
- `TrySwapAndResolve(a, b)` - perform an adjacent swap and resolve when the move is valid.
- `FindMatches()` - return current match groups.
- `ShuffleIfNoMoves()` - shuffle the board if no valid moves remain.
- `ResolveCurrentMatchesButton()` - Inspector helper for manual checks.

## Events

- `OnBoardChanged` - board state changed.
- `OnMatchesResolved(int count)` - matches were cleared.
- `OnBoardShuffled` - board was shuffled due to no available moves.
- `OnResolvePhase` - C# event for resolve phases: swap, clear, collapse, refill, completed.

## Shape Rules

The service uses only gameplay-usable cells:

- `IsEnabled == true`;
- `IsWalkable == true`;
- `IsOccupied == false`.

Disabled holes and blockers do not participate in match/collapse. Collapse works per independent usable segment, so custom shapes and boards with holes do not move tiles through unavailable cells.

## Views

Views are replaceable. Subscribe to `OnBoardChanged` and render cells from `FieldCell.ContentId`. The sample view is only a demo, not a required runtime dependency.

## See Also

- [FieldGenerator](../FieldGenerator.md)
- [GridGameBuilder](../GridGameBuilder.md)
- [SlidingMerge](../SlidingMerge/SlidingMergeBoardService.md)
