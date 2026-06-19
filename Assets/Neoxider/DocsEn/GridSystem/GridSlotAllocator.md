# GridSlotAllocator

**Purpose:** a small helper for one-cell slot allocation on top of `FieldGenerator`.

Use it when a game needs ordered slot placement without duplicating bounds, walkability, and occupancy checks: benches, autobattler boards, hotbars, tactical rows, inventory quick slots, or market rows.

## Main API

- `IsAvailable(position)` - returns true when the cell exists, is enabled, walkable, and unoccupied.
- `TryFindFirstAvailable(preferredPositions, out position)` - scans preferences in order.
- `TryAllocateFirstAvailable(preferredPositions, contentId, out position, out result)` - finds and writes a single-cell placement.
- `Allocate(position, contentId)` - writes one occupied cell through `FieldGenerator.PlaceContentFootprint`.
- `Release(position, emptyContentId, notify)` - clears content and occupancy.

## Example

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
    // Place the unit view at slot.
}
```

## Notes

`GridSlotAllocator` intentionally stays one-cell focused. Multi-cell pieces should use `FieldGenerator.CanPlaceContentFootprint` and `PlaceContentFootprint` directly.
