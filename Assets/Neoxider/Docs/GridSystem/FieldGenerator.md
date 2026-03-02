# FieldGenerator

**Что это:** генерирует и хранит runtime-сетку (форма, состояние ячеек, поиск пути). Конфиг в **Config**, события OnFieldGenerated, OnCellChanged, OnCellStateChanged. Требует компонент **Grid**. Пространство имён `Neo.GridSystem`, файл `Scripts/GridSystem/FieldGenerator.cs`.

**Как использовать:** Add Component → Neoxider → GridSystem → FieldGenerator; настроить **Config** (форма, размеры); при необходимости подписаться на события. Для отладки — [FieldDebugDrawer](FieldDebugDrawer.md).

---

## Основное

- **Config** — конфигурация генерации (форма поля, параметры ячеек).
- **OnFieldGenerated** — после генерации поля.
- **OnCellChanged** / **OnCellStateChanged** — при изменении ячейки.

## См. также

- [FieldDebugDrawer](FieldDebugDrawer.md), [FieldSpawner](FieldSpawner.md), [FieldObjectSpawner](FieldObjectSpawner.md)
