# Match3BoardService

**Purpose:** gameplay service for Match3 boards on top of `FieldGenerator`. It handles tile generation, valid swap search, match resolve, collapse, refill, and shuffle when no moves remain.

## API

- `InitializeBoard()` - fill enabled cells with starting tiles.
- `TryFindValidSwap(out a, out b)` - find an adjacent swap that creates a match without mutating the board.
- `TrySwapAndResolve(a, b)` - perform an adjacent swap and resolve when the move is valid.
- `FindMatches()` - return current match groups.
- `ShuffleIfNoMoves()` - attempt to produce a board without current matches and with a valid move. If no such board is found within the bounded attempts, restore the original content and return false without publishing a shuffle.
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

Swap search follows the configured movement directions, including one-way rules such as `new MovementRule(new[] { Vector3Int.down })`. A move returned by `TryFindValidSwap` uses that permitted orientation and can be passed to `TrySwapAndResolve`. A zero neighbor offset is ignored. Search uses a snapshot and does not change the live board.

Match finding, initialization, collapse and swap snapshots use `FieldGenerator.GeneratedSize` until the next field generation. The standalone `Match3MatchFinder.FindMatches(field, minMatchLength)` clamps non-positive thresholds to one and never returns an empty match group.

## Scene Setup

Add `Match3BoardService` beside a configured `FieldGenerator`, choose the tile pool and starting-match settings, and connect `OnBoardChanged` to your view. Initialize, resolve and shuffle Inspector buttons operate in Play Mode. From C#, call `InitializeBoard()`, query `TryFindValidSwap`, then submit moves to `TrySwapAndResolve`.

With delayed resolution, treat the service as busy until the completed phase. Disabling it stops the pending coroutine; initialize the board again if your application requires a fresh board rather than the partially resolved state.

## Views

Views are replaceable. Subscribe to `OnBoardChanged` and render cells from `FieldCell.ContentId`. The sample view is only a demo, not a required runtime dependency.

## See Also

- [FieldGenerator](../FieldGenerator.md)
- [GridGameBuilder](../GridGameBuilder.md)
- [SlidingMerge](../SlidingMerge/SlidingMergeBoardService.md)
