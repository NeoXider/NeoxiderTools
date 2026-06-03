# GridPlacementEntry

**Что это:** один элемент footprint для универсального размещения контента в `FieldGenerator`.

---

`GridPlacementEntry` используется вместе с `FieldGenerator.CanPlaceContentFootprint(...)` и `FieldGenerator.PlaceContentFootprint(...)`, когда фигура занимает одну или несколько клеток: dice pair, inventory block, puzzle piece, tetromino-like shape или другой составной объект.

## Поля

- `Offset` - смещение клетки относительно anchor-позиции placement request.
- `ContentId` - значение, которое будет записано в `FieldCell.ContentId`.
- `OccupiesCell` - должен ли placement помечать клетку как occupied.

## Пример

```csharp
var entries = new[]
{
    new GridPlacementEntry(Vector3Int.zero, 4),
    new GridPlacementEntry(Vector3Int.right, 6)
};

if (field.CanPlaceContentFootprint(anchor, entries))
{
    GridPlacementResult result = field.PlaceContentFootprint(anchor, entries);
}
```

## См. также

- [FieldGenerator](FieldGenerator.md)
- [GridPlacementResult](GridPlacementResult.md)
