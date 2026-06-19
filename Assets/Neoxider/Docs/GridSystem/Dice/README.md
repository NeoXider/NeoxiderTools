# GridSystem Dice

`Neo.GridSystem.Dice` - переиспользуемый слой размещения игральных кубиков поверх `FieldGenerator` и универсального ядра `Neo.Merge`.

## Runtime API

- `DicePiece` описывает одиночный кубик, пару или фигуру большего размера через local offsets; есть `CellCount` и методы поворота, работающие для любого числа клеток (поворот вокруг якоря).
- `DicePieceGenerator` создает одиночные/парные фишки из пула значений, с равными шансами и без одинаковых значений в паре. `CreateDefaultPool()` сохраняет исходный merge-пул 1-5, `CreateD6Pool()` дает классические грани 1-6, `CreateSequentialPool(minValue, maxValue)` подходит для кастомных numbered dice/progression-пулов.
- `DiceValueWeight` и `DicePieceGenerator.GenerateWeighted(...)` создают одиночные/парные фишки из weighted-пула. Нулевые веса игнорируются, пустой usable pool дает `ArgumentException`, а парная фишка не дублирует одно и то же значение, если в пуле есть минимум два положительных значения.
- `DiceBoardService` проверяет размещение, пишет значения в `FieldCell.ContentId` и запускает dice merge через `GridMergeResolver`. Правила merge настраиваются: `MinMergeGroupSize`, `MergeStep`, `MaxContentId` (0 = без ограничения), `RequireWalkable`.

По умолчанию модуль: размещает кубики, сливает 3+ одинаковых значения, касающихся гранями, результат `old + step`, cascade от result cell. Сервис сам выставляет occupancy и шлёт одно согласованное `OnCellStateChanged` на клетку и одно `OnBoardChanged` на размещение. Очки, progression пула, win/loss и UI остаются в игре или sample-сцене.

## Sample

Playable sample scene:

`Assets/Neoxider/Samples/Demo/Scenes/GridSystem/GridSystemDiceMergeDemo.unity`

Sample использует спрайты `Assets/Neoxider/Sprites/Dice`, drag/drop, поворот пары в tray, score, progression пула и game-over.
