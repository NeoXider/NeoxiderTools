# SlidingMergeBoardService

**What it is:** a scene wrapper (`Scripts/GridSystem/SlidingMerge/SlidingMergeBoardService.cs`, namespace `Neo.GridSystem.SlidingMerge`) for 2048-like games on top of `FieldGenerator`: slide lines, merge equal values, spawn new values, detect a locked board and expose events for UI, score and animation. The rules live in the pure C# `SlidingMergeResolver`, usable without the component.

**How to use:**
1. Create a grid object through `GridGameBuilder` and enable `SlidingMerge` (or add the component beside a `FieldGenerator`).
2. Set `FieldGenerator.Config.Size`, for example `(4, 4, 1)`.
3. Call `Slide(direction)` from input (or wire input UnityEvents to `SlideLeft`/`SlideRight`/`SlideUp`/`SlideDown`).
4. Refresh the view from `OnBoardChanged` or animate from `Slid` / `OnContentSpawned`; update score from `OnScoreDelta`; show game over on `OnNoMovesLeft`.

**Good for:** 2048, Threes-like prototypes, block merge, drop-and-merge, and puzzle boards that store game state in `FieldCell.ContentId`.

---

## Components and types

- `SlidingMergeResolver` - pure C# resolver; does not require a MonoBehaviour.
- `SlidingMergeBoardService` - scene/Inspector wrapper.
- `SlidingMergeDirection` - `Left`, `Right`, `Down`, `Up`, `Backward`, `Forward`.
- `SlidingMergeResult` - `Changed`, `MoveCount`, `MergeCount`, `ScoreDelta`, `Steps`.
- `SlidingMergeStep` - one tile movement: `From`, `To`, `Value`, `IsMerge`, `ResultValue`, and for merges the consumed tile's `MergedFrom` / `MergedValue`.

## Rules

- Empty cells are detected by `emptyContentId` (`0` in the component workflow).
- Only `IsEnabled && IsWalkable` cells participate. Disabled or non-walkable cells split a line into independent segments.
- A value can merge only once per slide, matching 2048 behavior. Default merge value is `a + b`.
- For custom rules pass `canMerge` and `merge` delegates to the resolver, or set `CanMergeRule` / `MergeRule` on the component.
- Cell notifications (`FieldGenerator.OnCellStateChanged`) are raised after every line has been written, so listeners always read the final board.
- Line scans use `FieldGenerator.GeneratedSize`; pending configuration size changes apply only after regeneration.

## Component API

| Member | Description |
|--------|-------------|
| `Slide(direction)` | Slide, raise `Slid` and `OnScoreDelta`, spawn when configured, raise `OnBoardChanged`, then `OnNoMovesLeft` when no direction can change the board. Returns the result. |
| `CanSlide(direction)`, `HasAnyMove()` | Previews; no writes. |
| `SpawnDefaultContent()`, `SpawnContent(id)` | Write a value into a random empty cell; raise `ContentSpawned` / `OnContentSpawned` and `OnBoardChanged`. |
| `ClearBoard()` | Empty every enabled, walkable cell. |
| `EmptyContentId`, `DefaultSpawnContentId`, `SpawnAfterSuccessfulSlide` | Settings (also in the Inspector with `ClearOnStart`, `SpawnInitialContentOnStart`, `InitialSpawnCount`). |
| `RandomIndexProvider` | Optional `Func<int, int>` spawn index source for deterministic tests and replays. |
| `CanMergeRule`, `MergeRule` | Optional custom merge rules. |

## Events

| Event | When |
|-------|------|
| `OnBoardChanged` | After clear, slide and spawn. |
| `OnScoreDelta(int)` | A slide merged at least one pair. |
| `OnBoardFull` | The spawn after a successful slide found no empty cell. |
| `OnNoMovesLeft` | After a successful slide (and its spawn) the board is locked. |
| `OnContentSpawned(Vector3Int)` / C# `ContentSpawned(FieldCell)` | A value was spawned. |
| C# `Slid(SlidingMergeResult)` | A slide changed the board, before its spawn. |

Clear/slide Inspector buttons operate in Play Mode. Initialize the board with `ClearBoard()` before supplying values when using an empty content id different from the field's default `-1`.

## Pure C# example

```csharp
using Neo.GridSystem;
using Neo.GridSystem.SlidingMerge;

SlidingMergeResult result = SlidingMergeResolver.Slide(generator, SlidingMergeDirection.Left, emptyContentId: 0);
foreach (SlidingMergeStep step in result.Steps)
{
    // Animate a tile from step.From to step.To; for merges also from step.MergedFrom.
}

if (result.Changed)
{
    SlidingMergeResolver.TrySpawnRandomContent(generator, 2, out FieldCell spawned, 0, true, count => myRandom.Next(count));
}

bool gameOver = !SlidingMergeResolver.HasAnyMove(generator, emptyContentId: 0);
```

## Performance

The resolver reuses internal buffers between calls; a slide allocates only its result and one step per moved tile, and the default merge rules are cached delegates. `CanSlide` / `HasAnyMove` stop at the first change, record no steps and reuse an internal preview result, so warm calls do not allocate (the game-over check after every move and per-frame UI hints stay garbage-free).

## 2048Blocks-style games

The system covers the base mechanics: grid, rows/columns, compact, merge, score, and spawn. A top-down block throw can remain a separate input/view layer: after choosing a column, set `ContentId` on the landing cell and run slide/cascade or a custom merge resolver.

## See also

- [GridSystem module](../README.md)
- [FieldGenerator](../FieldGenerator.md)
