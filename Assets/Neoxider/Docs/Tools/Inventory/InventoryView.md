# InventoryView и InventoryItemView

## InventoryView

Компонент рендера инвентаря в UI.

- Источник: `InventoryComponent`.
- Режимы:
  - `SpawnFromPrefab` — автоматически спавнит `InventoryItemView` по данным из `InventoryDatabase`.
  - `ManualList` — использует заранее заданные `InventoryItemView`.
- Источник данных для списка:
  - `DatabaseItems` — только из `InventoryDatabase` (порядок как в базе).
  - `SnapshotItems` — только из фактического содержимого инвентаря (порядок = порядок добавления).
  - `Hybrid` — сначала слоты из базы в порядке базы, затем недостающие из снимка.
- Обновляется по `OnInventoryChanged` и (опционально) `OnLoaded`.
- Есть опция refresh на следующий кадр при `OnEnable`, чтобы корректно отображать стартовое состояние после загрузки.

## InventoryItemView

Представление одной записи предмета (все UI-поля опциональны):

- `Image` для иконки,
- `TMP_Text` для названия,
- `TMP_Text` для количества.

Подходит и для автоматически созданных элементов, и для ручной раскладки.
