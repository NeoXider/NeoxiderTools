# GridSystem module

## Назначение

GridSystem - базовый конструктор сеточных игр и систем. Модуль разделяет универсальное ядро поля и подключаемые игровые слои, чтобы один и тот же grid можно было использовать для Match3, TicTacToe, 2048-like игр, Dice Merge, тактических полей, inventory grids и custom board games.

## Архитектура

- `FieldGenerator` - ядро: размер, форма, клетки, координаты, состояние и pathfinding facade.
- `GridMergeResolver` - adapter из универсального `Neo.Merge` connected-group resolver к `FieldGenerator` / `FieldCell.ContentId`.
- `DiceBoardService` - reusable слой размещения dice pieces и dice merge.
- `GridGameBuilder` - scene/Inspector сборка нужных модулей.
- `GridSlotAllocator` - ordered one-cell slot allocation для benches, tactical rows и autobattler boards.
- `GridShapeMask` - ScriptableObject для reusable формы поля.
- `FieldSpawner` / `FieldObjectSpawner` - размещение объектов по клеткам.
- `FieldDebugDrawer` - Gizmos-отладка.
- `Match3BoardService` - прикладной слой для swap/match игр.
- `TicTacToeBoardService` - прикладной слой для ходовых board games.
- `SlidingMergeBoardService` - прикладной слой для 2048, Threes, block-merge и drop-and-merge механик.

## Документация

- [FieldGenerator](./FieldGenerator.md)
- [GridPlacementEntry](./GridPlacementEntry.md)
- [GridPlacementResult](./GridPlacementResult.md)
- [GridSlotAllocator](./GridSlotAllocator.md)
- [GridGameBuilder](./GridGameBuilder.md)
- [GridShapeMask](./GridShapeMask.md)
- [FieldDebugDrawer](./FieldDebugDrawer.md)
- [FieldSpawner](./FieldSpawner.md)
- [FieldObjectSpawner](./FieldObjectSpawner.md)
- [InternalTypes](./InternalTypes.md)
- [Dice](./Dice/README.md)
- [Generic Merge](../Merge/README.md)
- [SlidingMerge](./SlidingMerge/SlidingMergeBoardService.md)
- [Match3](./Match3/Match3BoardService.md)
- [TicTacToe](./TicTacToe/TicTacToeBoardService.md)

## Быстрый старт

1. Добавьте `GridGameBuilder` или `FieldGenerator` на GameObject.
2. Настройте `FieldGenerator.Config`: `Size`, `GridType`, `MovementRule`, origin и shape overrides.
3. Для connected-group merge используйте `GridMergeResolver`.
4. Для Dice Merge добавьте `DiceBoardService` и управляйте score/progression/game-over в своем контроллере.
5. Подключите собственный view/UI к событиям выбранного gameplay service.

## Samples

Текущий рабочий sample path: `Assets/Neoxider/Samples/Demo/`.

GridSystem-сцены лежат в `Scenes/GridSystem/`, setup/view-скрипты - в `Scripts/GridSystem/`.

## English

См. английскую версию: [`../../DocsEn/GridSystem/README.md`](../../DocsEn/GridSystem/README.md).
