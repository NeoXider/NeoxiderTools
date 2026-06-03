# GridPlacementResult

**Purpose:** result of writing a multi-cell footprint into `FieldGenerator`.

`GridPlacementResult` is returned by `FieldGenerator.PlaceContentFootprint(...)`. It is useful for gameplay services and view layers because it reports the changed cells, their logical positions, and the reason when placement fails.

## Fields

- `Placed` - `true` when the footprint was written successfully.
- `FailureReason` - short reason when placement is rejected.
- `Cells` - changed `FieldCell` instances.
- `Positions` - logical positions of changed cells.

## Example

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

## See Also

- [FieldGenerator](FieldGenerator.md)
- [GridPlacementEntry](GridPlacementEntry.md)
