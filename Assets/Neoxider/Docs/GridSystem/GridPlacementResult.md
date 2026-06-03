# GridPlacementResult

**Что это:** результат попытки записать multi-cell footprint в `FieldGenerator`.

---

`GridPlacementResult` возвращается из `FieldGenerator.PlaceContentFootprint(...)`. Он удобен для gameplay-сервисов и view-слоёв: можно сразу узнать, какие клетки изменились, какие logical positions заняты и почему placement не прошёл.

## Поля

- `Placed` - `true`, если footprint успешно записан в поле.
- `FailureReason` - короткая причина отказа, если placement невозможен.
- `Cells` - изменённые `FieldCell`.
- `Positions` - logical positions изменённых клеток.

## Пример

```csharp
GridPlacementResult result = field.PlaceContentFootprint(anchor, entries);

if (!result.Placed)
{
    Debug.Log(result.FailureReason);
    return;
}

foreach (Vector3Int position in result.Positions)
{
    // Spawn or move a visual for the written cell.
}
```

## См. также

- [FieldGenerator](FieldGenerator.md)
- [GridPlacementEntry](GridPlacementEntry.md)
