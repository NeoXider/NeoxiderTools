# SlidingMergeBoardService

**Что это:** runtime-сервис для 2048-like игр на базе `FieldGenerator`: сдвиг линии, merge одинаковых значений, spawn новых значений и события для UI/score.

---

**Подходит для:** 2048, Threes-like прототипов, block merge, drop-and-merge, puzzle boards с контентом в `FieldCell.ContentId`.

## Компоненты

- `SlidingMergeResolver` - pure C# resolver. Можно использовать без MonoBehaviour.
- `SlidingMergeBoardService` - MonoBehaviour-обертка для сцены и Inspector.
- `SlidingMergeDirection` - направления: `Left`, `Right`, `Down`, `Up`, `Backward`, `Forward`.
- `SlidingMergeResult` - результат хода: изменилось ли поле, количество merge, score delta, steps.

## Правила

- Пустая клетка определяется через `emptyContentId` (`0` по умолчанию в component workflow).
- Участвуют только `IsEnabled && IsWalkable` клетки.
- Disabled/non-walkable клетки режут линию на независимые сегменты.
- В одном сдвиге значение сливается только один раз, как в 2048.
- Значение merge по умолчанию: `a + b`, поэтому `2 + 2 = 4`.
- Для custom правил используйте `SlidingMergeResolver.Slide(...)` с `canMerge` и `merge` delegates.

## Быстрый старт

1. Создайте grid object через `GridGameBuilder`.
2. Включите `SlidingMerge`.
3. Настройте `FieldGenerator.Config.Size`, например `(4, 4, 1)`.
4. На input вызывайте:

```csharp
board.Slide(SlidingMergeDirection.Left);
board.Slide(SlidingMergeDirection.Right);
board.Slide(SlidingMergeDirection.Up);
board.Slide(SlidingMergeDirection.Down);
```

5. View обновляйте по `OnBoardChanged`, score - по `OnScoreDelta`.

## Pure C# пример

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

## Для 2048Blocks-style игры

Система покрывает базовую механику: grid, columns/rows, compact, merge, score, spawn. Бросок блока сверху можно делать отдельным input/view layer: после выбора колонки установить `ContentId` в landing cell и вызвать slide/cascade или custom merge resolver.
