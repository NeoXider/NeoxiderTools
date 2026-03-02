# FieldDebugDrawer

**Что это:** компонент визуализации сетки FieldGenerator в редакторе и в игре: Gizmos для сетки, путей и состояния ячеек. Требует [FieldGenerator](FieldGenerator.md). Пространство имён `Neo.GridSystem`, файл `Scripts/GridSystem/FieldDebugDrawer.cs`.

**Как использовать:** добавить на тот же GameObject, что и FieldGenerator; настроить цвета (GridColor, PathColor и т.д.), при необходимости включить DrawPath и задать DebugPath.

---

## Поля

- **GridColor**, **PathColor**, **BlockedCellColor**, **WalkableCellColor**, **DisabledCellColor**, **OccupiedCellColor**, **CoordinatesColor** — цвета для Gizmos.
- **DrawCoordinates**, **DrawPath** — что отображать.
- **DebugPath** — список ячеек для отрисовки пути (в режиме отладки).

## См. также

- [FieldGenerator](FieldGenerator.md)
