# GridSystem module

## Назначение

GridSystem - базовый конструктор сеточных игр и систем. Модуль разделен на универсальное ядро поля и подключаемые игровые слои, чтобы один и тот же grid можно было использовать для Match3, TicTacToe, 2048-like игр, тактических полей, inventory grids и custom board games.

## Архитектура

- `FieldGenerator` - ядро: размер, форма, клетки, координаты, состояние, pathfinding facade.
- `GridGameBuilder` - удобная scene/Inspector сборка нужных модулей.
- `GridShapeMask` - ScriptableObject для reusable формы поля.
- `FieldSpawner` / `FieldObjectSpawner` - размещение объектов по клеткам.
- `FieldDebugDrawer` - Gizmos-отладка.
- `Match3BoardService` - прикладной слой для swap/match игр.
- `TicTacToeBoardService` - прикладной слой для ходовых board games.
- `SlidingMergeBoardService` - прикладной слой для 2048, Threes, block-merge и drop-and-merge механик.

## Документация

- [FieldGenerator](./FieldGenerator.md)
- [GridGameBuilder](./GridGameBuilder.md)
- [GridShapeMask](./GridShapeMask.md)
- [FieldDebugDrawer](./FieldDebugDrawer.md)
- [FieldSpawner](./FieldSpawner.md)
- [FieldObjectSpawner](./FieldObjectSpawner.md)
- [InternalTypes](./InternalTypes.md)
- [SlidingMerge](./SlidingMerge/SlidingMergeBoardService.md)
- [Match3](./Match3/Match3BoardService.md)
- [TicTacToe](./TicTacToe/TicTacToeBoardService.md)

## Быстрый старт

1. Добавьте `GridGameBuilder` на GameObject.
2. Выберите `Features`: например `DebugDrawer + SlidingMerge` для 2048-like игры.
3. Настройте `FieldGenerator.Config`: `Size`, `GridType`, `MovementRule`, origin и shape overrides.
4. Нажмите `Ensure Grid Components` или запустите сцену.
5. Подключите собственный view/UI к событиям выбранного gameplay service.

## Samples

Текущий рабочий sample path: `Assets/Neoxider/Samples/Demo/`.

GridSystem-сцены лежат в `Scenes/GridSystem/`, setup/view-скрипты - в `Scripts/GridSystem/`.

## English

См. английскую версию: [`../../DocsEn/GridSystem/README.md`](../../DocsEn/GridSystem/README.md).
