# Match3BoardService

**What it is:** `Match3BoardService` (`Scripts/GridSystem/Match3/Match3BoardService.cs`, namespace `Neo.GridSystem.Match3`) is the gameplay service for Match3 boards on top of `FieldGenerator`. It handles tile generation, valid swap search, match resolve, collapse, refill, and shuffle when no moves remain.

**How to use:** see Scene Setup below: add it beside a configured `FieldGenerator`, call `InitializeBoard()`, submit moves with `TrySwapAndResolve`, and render from `OnBoardChanged`.

---

## API

- `InitializeBoard()` - fill enabled cells with starting tiles.
- `TryFindValidSwap(out a, out b)` - find an adjacent swap that creates a match without mutating the board.
- `TrySwapAndResolve(a, b)` - perform an adjacent swap and resolve when the move is valid.
- `FindMatches()` - return current match groups (allocates the returned lists; see `Match3MatchFinder` below for allocation-free queries).
- `ShuffleIfNoMoves()` - attempt to produce a board without current matches and with a valid move. If no such board is found within the bounded attempts, restore the original content and return false without publishing a shuffle.
- `ResolveCurrentMatchesButton()` - resolve matches already on the board (for example after external edits); Inspector helper for manual checks.
- `RandomIndexProvider` - optional `Func<int, int>` random source (see Determinism).
- `IsResolving` - true while a delayed resolve runs; swaps and manual resolves are rejected until it completes. It is already true inside the first step's callbacks (the `Swap` phase, or the first `Clear` phase, and their `OnBoardChanged`) and already false inside the completion callbacks (`Completed`, `OnMatchesResolved` and the final `OnBoardChanged`), so a listener can chain the next move or start the next level from them.

`Match3MatchFinder` (static) also works without the service:

- `FindMatches(field, minMatchLength)` - match groups, rows before columns per layer.
- `HasMatch(field, minMatchLength)` - true when any run qualifies; stops at the first one and does not allocate.
- `CollectMatchedCells(field, minMatchLength, HashSet<FieldCell> results)` - fills a reused set with every matched cell once (the union of `FindMatches`) and returns the count without allocating.

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

Swap search follows the configured movement directions, including one-way rules such as `new MovementRule(new[] { Vector3Int.down })`. A move returned by `TryFindValidSwap` uses that permitted orientation and can be passed to `TrySwapAndResolve`. A zero neighbor offset is ignored. Search copies the board once per call (cost grows with cells + tested pairs) and never changes the live board. The copy and the neighbor buffer are reused between calls, so repeated `TryFindValidSwap` calls (hints, shuffle checks) do not allocate once warm.

Match finding, initialization, collapse and swap snapshots use `FieldGenerator.GeneratedSize` until the next field generation. The standalone `Match3MatchFinder.FindMatches(field, minMatchLength)` clamps non-positive thresholds to one and never returns an empty match group.

## Determinism

New tiles (initial fill and refill) and the no-moves shuffle draw from `UnityEngine.Random` by default. Assign `RandomIndexProvider` to use your own source: it receives the exclusive upper bound (tile count or shuffle range) and returns an index; out-of-range results are clamped. With a provider the service does not touch `UnityEngine.Random`, so a seeded provider reproduces the same boards and cascades for replays, tests or a game-wide random service.

```csharp
System.Random random = new System.Random(levelSeed);
match3.RandomIndexProvider = count => random.Next(count);
match3.InitializeBoard();
```

## Performance

Warm `TryFindValidSwap`, rejected and accepted `TrySwapAndResolve` (including the clear/collapse/refill cascade, with Use Resolve Delay off) and `ShuffleIfNoMoves` reuse the service's board copy, neighbor list and resolve buffers and do not allocate. `FindMatches()` allocates its returned groups. A delayed resolve allocates its coroutine (iterator and `Coroutine` handle) once per accepted swap or manual resolve; its stages reuse one cached `WaitForSeconds` per delay value instead of creating one per stage, and it borrows the resolve buffers only inside each synchronous clear step, never across a delay, so a `ShuffleIfNoMoves` or hint search between stages stays allocation-free and a resolve stopped by a disable or `InitializeBoard()` leaves no buffer behind. Buffers are rented per call, so a re-entrant call from an event or provider callback gets its own buffers instead of corrupting the outer one.

## Scene Setup

Add `Match3BoardService` beside a configured `FieldGenerator`, choose the tile pool and starting-match settings, and connect `OnBoardChanged` to your view. Initialize, resolve and shuffle Inspector buttons operate in Play Mode. From C#, call `InitializeBoard()`, query `TryFindValidSwap`, then submit moves to `TrySwapAndResolve`.

With delayed resolution, treat the service as busy until the completed phase; `IsResolving` reports it. A cascade limit of zero finishes immediately and never leaves the service busy. Disabling it stops the pending coroutine; initialize the board again if your application requires a fresh board rather than the partially resolved state. `InitializeBoard()` also cancels a running resolve, even when it is called from one of that resolve's own callbacks: the old cascade stops at its next step and never edits the new board. On an inactive GameObject, where Unity cannot run coroutines, a delayed swap or resolve runs immediately instead.

## Views

Views are replaceable. Subscribe to `OnBoardChanged` and render cells from `FieldCell.ContentId`. The service writes `ContentId` directly and does not raise `FieldGenerator.OnCellStateChanged`, so a view wired only to that event never updates; use `OnBoardChanged` (or `OnResolvePhase` for staged animation). The sample view is only a demo, not a required runtime dependency.

## Кратко по-русски

`Match3BoardService` — сервис Match3 поверх `FieldGenerator`: заполнение, поиск хода, обмен, каскад «очистка → падение → досыпка» и перемешивание без ходов. `RandomIndexProvider` подставляет свой генератор случайных чисел (получает верхнюю границу, возвращает индекс), и тогда поле и каскады воспроизводимы, а `UnityEngine.Random` не трогается. `IsResolving` показывает, что идёт отложенный каскад: внутри первого шага (`Swap` или первый `Clear`) он уже `true`, а в завершающих событиях (`Completed`, `OnMatchesResolved`, последний `OnBoardChanged`) уже `false`, поэтому из них можно сразу сделать следующий ход или начать новый уровень. `InitializeBoard()` прерывает идущий каскад, даже если вызван из его собственного события. Повторные поиск хода, обмен с каскадом (без задержки) и перемешивание после прогрева не выделяют память, а отложенный каскад выделяет корутину один раз на ход и не держит буферы сервиса во время пауз, поэтому перемешивание между шагами тоже обходится без выделений; `Match3MatchFinder.HasMatch` и `CollectMatchedCells` проверяют и собирают совпадения без выделений.

## See Also

- [FieldGenerator](../FieldGenerator.md)
- [GridGameBuilder](../GridGameBuilder.md)
- [SlidingMerge](../SlidingMerge/SlidingMergeBoardService.md)
