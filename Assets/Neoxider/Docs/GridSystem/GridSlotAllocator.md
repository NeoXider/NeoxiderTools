# GridSlotAllocator

**Назначение:** маленький helper для распределения одно-клеточных слотов поверх `FieldGenerator`.

Используйте его, когда игре нужен placement по порядку предпочтений без дублирования проверок границ, walkability и occupied state: bench, autobattler board, hotbar, tactical rows, inventory quick slots или market row.

## Основной API

- `IsAvailable(position)` - true, если клетка существует, enabled, walkable и не occupied.
- `Capacity` - возвращает количество линейных слотов для прямоугольной 2D-доски или `0`, если линейные слоты не поддерживаются.
- `HasAvailableSlot` - true, если есть хотя бы одна enabled/walkable/unoccupied клетка.
- `TryGetSlotPosition(slotIndex, out position)` - переводит линейный индекс слота в `Vector3Int` для прямоугольной 2D-доски в row-major порядке: `0=(0,0,0)`, `1=(1,0,0)`, `width=(0,1,0)`.
- `TryGetSlotIndex(position, out slotIndex)` - переводит valid `z=0` позицию обратно в линейный индекс.
- `IsAvailable(slotIndex)` - проверяет доступность слота по линейному индексу.
- `TryFindFirstAvailable(preferredPositions, out position)` - ищет первый доступный слот в заданном порядке.
- `TryAllocateFirstAvailable(preferredPositions, contentId, out position, out result)` - ищет и записывает одно-клеточный placement.
- `TryAllocateFirstAvailable(preferredSlotIndices, contentId, out slotIndex, out result)` - ищет и записывает первый доступный линейный слот в заданном порядке.
- `Allocate(position, contentId)` - пишет занятую клетку через `FieldGenerator.PlaceContentFootprint`.
- `Allocate(slotIndex, contentId)` - пишет занятую клетку по линейному индексу; invalid index возвращает `GridPlacementResult` с `Placed=false`.
- `Release(position, emptyContentId, notify)` - очищает content и occupied state.
- `Release(slotIndex, emptyContentId, notify)` - очищает content и occupied state по линейному индексу.
- `Clear(emptyContentId, notify)` - очищает все enabled клетки, которыми управляет allocator.

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

## Linear slot index

Для компактных 2D-досок, где UI уже работает с индексами (`0..5` для 3x2 autobattler board), можно не хранить собственный mapping:

```csharp
GridSlotAllocator allocator = new GridSlotAllocator(fieldGenerator);

if (allocator.IsAvailable(4))
{
    GridPlacementResult result = allocator.Allocate(4, unitId);
    // slot 4 на поле 3x2 соответствует позиции (1, 1, 0).
}

if (allocator.TryAllocateFirstAvailable(new[] { 3, 4, 5 }, unitId, out int slotIndex, out GridPlacementResult slotResult))
{
    // Задний ряд принял юнита в slotIndex.
}
```

Linear API намеренно работает только для `GridType.Rectangular` с `Size.z == 1`. Для hex/custom/3D полей используйте позиционный API через `Vector3Int`.

## Заметки

`GridSlotAllocator` намеренно работает только с одно-клеточными слотами. Для multi-cell pieces используйте `FieldGenerator.CanPlaceContentFootprint` и `PlaceContentFootprint` напрямую.
