# GridSystem

GridSystem - модуль-конструктор для сеточных игр и систем Unity. Он дает общее ядро поля, а конкретные игровые правила подключаются отдельными слоями: Match3, TicTacToe, SlidingMerge для 2048-like механик, pathfinding, spawners и debug/view компоненты.

## Принцип

- `FieldGenerator` хранит форму поля, координаты, клетки и базовое состояние.
- Игровые правила живут в отдельных сервисах и используют `FieldCell.ContentId`, `IsEnabled`, `IsWalkable`, `IsOccupied`, `Type`, `Flags`.
- `GridGameBuilder` помогает быстро собрать сценовый объект из нужных модулей через Inspector.
- Demo/view компоненты не являются обязательной частью правил. Их можно заменить собственным UI, 2D/3D view или сеточной доской.
- NoCode/Inspector workflow должен вызывать typed C# API, а не прятать механику в UnityEvent-цепочки.

## Что можно собирать

- Match3 и похожие swap/match игры.
- TicTacToe и другие настольные игры на клетках.
- 2048, Threes, drop-and-merge, block-merge и другие sliding/merge игры.
- Тактические поля и pathfinding.
- Inventory grids, board views, puzzle layouts, custom-shaped boards.

## Основные компоненты

- `FieldGenerator` - ядро поля: генерация, shape, cell state, координаты, world/grid conversion.
- `GridGameBuilder` - scene-конструктор, который добавляет выбранные runtime-модули.
- `GridShapeMask` - reusable ScriptableObject-маска формы.
- `GridPathfinder` - pure pathfinding service с диагностикой причин.
- `FieldSpawner` / `FieldObjectSpawner` - спавн объектов по клеткам.
- `FieldDebugDrawer` - Gizmos-отладка формы и состояния клеток.
- `Match3BoardService` - swap/match/resolve/refill.
- `TicTacToeBoardService` - turns, moves, win/draw.
- `SlidingMergeBoardService` - 2048-like slide/merge/spawn.

## Быстрый старт

1. Создайте GameObject.
2. Добавьте `GridGameBuilder`.
3. В `Features` выберите нужные модули: например `DebugDrawer + SlidingMerge` для 2048-like игры или `DebugDrawer + Match3` для Match3.
4. Настройте `FieldGenerator.Config`: размер, тип формы, movement rule, origin, shape mask.
5. Нажмите `Ensure Grid Components` или запустите сцену.
6. Подключите собственный view/UI к событиям выбранного сервиса.

Для ручной сборки можно добавить `Grid`, `FieldGenerator` и нужные сервисы напрямую без `GridGameBuilder`.

## Документация

- [GridSystem README](GridSystem/README.md)
- [FieldGenerator](GridSystem/FieldGenerator.md)
- [GridGameBuilder](GridSystem/GridGameBuilder.md)
- [SlidingMergeBoardService](GridSystem/SlidingMerge/SlidingMergeBoardService.md)
- [Match3BoardService](GridSystem/Match3/Match3BoardService.md)
- [TicTacToeBoardService](GridSystem/TicTacToe/TicTacToeBoardService.md)
- [GridShapeMask](GridSystem/GridShapeMask.md)
- [FieldSpawner](GridSystem/FieldSpawner.md)
- [FieldObjectSpawner](GridSystem/FieldObjectSpawner.md)
- [FieldDebugDrawer](GridSystem/FieldDebugDrawer.md)

## Samples

Текущий рабочий sample path: `Assets/Neoxider/Samples/Demo/`.

Release/UPM path перед упаковкой: `Assets/Neoxider/Samples~/Demo/`.

GridSystem demo-сцены находятся в `Scenes/GridSystem/`, setup/view-скрипты - в `Scripts/GridSystem/`.
