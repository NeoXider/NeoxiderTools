# FieldGenerator

**Что это:** ядро GridSystem. Компонент генерирует и хранит runtime-сетку, форму, координаты и состояние клеток. Все игровые слои работают поверх `FieldGenerator`.

## Что хранит клетка

`FieldCell` содержит:

- `Position` - логические координаты.
- `IsEnabled` - клетка входит в форму текущего поля.
- `IsWalkable` - клетку можно использовать для pathfinding/игровых правил.
- `IsOccupied` - клетка занята runtime-объектом.
- `ContentId` - игровой контент: фишка Match3, X/O, значение 2048, id предмета.
- `Type` - тип клетки/тайла.
- `Flags` - дополнительные маркеры.
- `UserData` - optional custom payload для C#-слоев.

## Config

- `Size` - размер 3D массива клеток.
- `GridType` - `Rectangular`, `Hexagonal`, `Custom`.
- `MovementRule` - набор соседних направлений.
- `ShapeMask` - ScriptableObject-маска формы.
- `DisabledCells` / `ForcedEnabledCells` - shape overrides.
- `BlockedCells` / `ForcedWalkableCells` - walkability overrides.
- `PassabilityMode` - как pathfinding учитывает occupied state.
- `Origin2D`, `OriginDepth`, `OriginOffset` - привязка логического поля к Unity Grid.

## Основной API

- `GenerateField(config)` - создать/пересоздать клетки.
- `GetCell(...)` - получить клетку.
- `GetAllCells(includeDisabled)` - перечислить клетки.
- `SetWalkable(...)`, `SetEnabled(...)`, `SetOccupied(...)`, `SetContentId(...)` - изменить состояние.
- `GetNeighbors(...)` - получить соседей по `MovementRule` или override-направлениям.
- `FindPathDetailed(...)` - pathfinding с диагностикой причины.
- `GetCellWorldCenter(...)`, `GetCellCornerWorld(...)`, `GetCellFromWorld(...)` - conversion helpers.

## События

- `OnFieldGenerated`
- `OnCellChanged`
- `OnCellStateChanged`

## Роль в архитектуре

`FieldGenerator` не должен знать правила конкретной игры. Match3, TicTacToe, SlidingMerge, inventory grids и custom games подключаются отдельными сервисами и читают/пишут состояние клеток.

## См. также

- [GridGameBuilder](GridGameBuilder.md)
- [GridShapeMask](GridShapeMask.md)
- [FieldDebugDrawer](FieldDebugDrawer.md)
- [SlidingMerge](SlidingMerge/SlidingMergeBoardService.md)
