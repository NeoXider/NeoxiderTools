# SlidingMergeBoardService

**Purpose:** runtime service for 2048-like games on top of `FieldGenerator`: slide lines, merge equal values, spawn new values, and expose events for UI/score.

**Good for:** 2048, Threes-like prototypes, block merge, drop-and-merge, and puzzle boards that store game state in `FieldCell.ContentId`.

## Components

- `SlidingMergeResolver` - pure C# resolver; does not require MonoBehaviour.
- `SlidingMergeBoardService` - scene/Inspector wrapper.
- `SlidingMergeDirection` - `Left`, `Right`, `Down`, `Up`, `Backward`, `Forward`.
- `SlidingMergeResult` - move result: changed flag, merge count, score delta, steps.

## Rules

- Empty cells are detected by `emptyContentId` (`0` in the component workflow).
- Only `IsEnabled && IsWalkable` cells participate.
- Disabled or non-walkable cells split a line into independent segments.
- A value can merge only once per slide, matching 2048 behavior.
- Default merge value is `a + b`, so `2 + 2 = 4`.
- For custom rules, call `SlidingMergeResolver.Slide(...)` with `canMerge` and `merge` delegates.

## Quick Start

1. Create a grid object through `GridGameBuilder`.
2. Enable `SlidingMerge`.
3. Set `FieldGenerator.Config.Size`, for example `(4, 4, 1)`.
4. Call from input:

```csharp
board.Slide(SlidingMergeDirection.Left);
board.Slide(SlidingMergeDirection.Right);
board.Slide(SlidingMergeDirection.Up);
board.Slide(SlidingMergeDirection.Down);
```

5. Refresh the view from `OnBoardChanged`; update score from `OnScoreDelta`.

## Pure C# Example

```csharp
SlidingMergeResult result = SlidingMergeResolver.Slide(
    generator,
    SlidingMergeDirection.Left,
    emptyContentId: 0);

if (result.Changed)
{
    // Rebuild or animate board view.
}
```

## 2048Blocks-style Games

The system covers the base mechanics: grid, rows/columns, compact, merge, score, and spawn. A top-down block throw can remain a separate input/view layer: after choosing a column, set `ContentId` on the landing cell and run slide/cascade or a custom merge resolver.
