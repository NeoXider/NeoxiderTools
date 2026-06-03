# Match3BoardService

**Что это:** gameplay service для Match3-полей поверх `FieldGenerator`. Отвечает за генерацию фишек, поиск валидного swap, resolve совпадений, collapse, refill и shuffle при отсутствии ходов.

---

## API

- `InitializeBoard()` - заполнить enabled клетки стартовыми фишками.
- `TryFindValidSwap(out a, out b)` - найти соседний swap, который даст match, без изменения поля.
- `TrySwapAndResolve(a, b)` - выполнить соседний swap и resolve, если ход валиден.
- `FindMatches()` - получить текущие match-группы.
- `ShuffleIfNoMoves()` - перемешать поле, если ходов нет.
- `ResolveCurrentMatchesButton()` - Inspector helper для ручной проверки.

## События

- `OnBoardChanged` - состояние поля изменилось.
- `OnMatchesResolved(int count)` - сняты совпадения.
- `OnBoardShuffled` - поле перемешано из-за отсутствия ходов.
- `OnResolvePhase` - C# event для фаз resolve: swap, clear, collapse, refill, completed.

## Правила формы

Сервис использует только клетки, которые можно применять в игре:

- `IsEnabled == true`;
- `IsWalkable == true`;
- `IsOccupied == false`.

Disabled holes и blockers не участвуют в match/collapse. Collapse работает по независимым usable-сегментам, поэтому кастомные формы и поля с дырками не проталкивают фишки через недоступные клетки.

## Для view

View должен быть заменяемым. Подпишитесь на `OnBoardChanged` и перерисуйте клетки по `FieldCell.ContentId`. Demo view из samples - только пример, не обязательная зависимость.

## См. также

- [FieldGenerator](../FieldGenerator.md)
- [GridGameBuilder](../GridGameBuilder.md)
- [SlidingMerge](../SlidingMerge/SlidingMergeBoardService.md)
