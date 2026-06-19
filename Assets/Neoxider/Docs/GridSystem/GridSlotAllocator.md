# GridSlotAllocator

**Назначение:** маленький helper для распределения одно-клеточных слотов поверх `FieldGenerator`.

Используйте его, когда игре нужен placement по порядку предпочтений без дублирования проверок границ, walkability и occupied state: bench, autobattler board, hotbar, tactical rows, inventory quick slots или market row.

## Основной API

- `IsAvailable(position)` - true, если клетка существует, enabled, walkable и не occupied.
- `TryFindFirstAvailable(preferredPositions, out position)` - ищет первый доступный слот в заданном порядке.
- `TryAllocateFirstAvailable(preferredPositions, contentId, out position, out result)` - ищет и записывает одно-клеточный placement.
- `Allocate(position, contentId)` - пишет занятую клетку через `FieldGenerator.PlaceContentFootprint`.
- `Release(position, emptyContentId, notify)` - очищает content и occupied state.

## Пример

```csharp
GridSlotAllocator allocator = new GridSlotAllocator(fieldGenerator);
Vector3Int[] warriorSlots =
{
    new Vector3Int(0, 0, 0),
    new Vector3Int(1, 0, 0),
    new Vector3Int(2, 0, 0)
};

if (allocator.TryAllocateFirstAvailable(warriorSlots, unitId, out Vector3Int slot, out GridPlacementResult result))
{
    // Поставить view юнита в slot.
}
```

## Заметки

`GridSlotAllocator` намеренно работает только с одно-клеточными слотами. Для multi-cell pieces используйте `FieldGenerator.CanPlaceContentFootprint` и `PlaceContentFootprint` напрямую.
