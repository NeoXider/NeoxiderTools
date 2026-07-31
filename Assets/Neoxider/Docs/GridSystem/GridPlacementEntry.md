# GridPlacementEntry

**Purpose:** one footprint entry for generic content placement in `FieldGenerator`.

`GridPlacementEntry` is used with `FieldGenerator.CanPlaceContentFootprint(...)` and `FieldGenerator.PlaceContentFootprint(...)` when a piece occupies one or more cells: dice pairs, inventory blocks, puzzle pieces, tetromino-like shapes, or other composite objects.

## Fields

- `Offset` - cell offset relative to the placement anchor.
- `ContentId` - value written to `FieldCell.ContentId`.
- `OccupiesCell` - whether the placement should mark the cell as occupied.

## Example

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

## See Also

- [FieldGenerator](FieldGenerator.md)
- [GridPlacementResult](GridPlacementResult.md)
