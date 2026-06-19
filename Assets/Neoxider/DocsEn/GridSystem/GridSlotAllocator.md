# GridSlotAllocator

**Purpose:** a small helper for one-cell slot allocation on top of `FieldGenerator`.

Use it when a game needs ordered slot placement without duplicating bounds, walkability, and occupancy checks: benches, autobattler boards, hotbars, tactical rows, inventory quick slots, or market rows.

## Main API

- `IsAvailable(position)` - returns true when the cell exists, is enabled, walkable, and unoccupied.
- `TryGetSlotPosition(slotIndex, out position)` - maps a linear slot index to `Vector3Int` on a rectangular 2D board in row-major order: `0=(0,0,0)`, `1=(1,0,0)`, `width=(0,1,0)`.
- `TryGetSlotIndex(position, out slotIndex)` - maps a valid `z=0` board position back to a linear slot index.
- `IsAvailable(slotIndex)` - checks slot availability by linear index.
- `TryFindFirstAvailable(preferredPositions, out position)` - scans preferences in order.
- `TryAllocateFirstAvailable(preferredPositions, contentId, out position, out result)` - finds and writes a single-cell placement.
- `Allocate(position, contentId)` - writes one occupied cell through `FieldGenerator.PlaceContentFootprint`.
- `Allocate(slotIndex, contentId)` - writes one occupied cell by linear index; invalid indices return a `GridPlacementResult` with `Placed=false`.
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

## Linear slot index

For compact 2D boards where UI or gameplay already works with indices (`0..5` for a 3x2 autobattler board), callers can avoid local mapping code:

```csharp
GridSlotAllocator allocator = new GridSlotAllocator(fieldGenerator);

if (allocator.IsAvailable(4))
{
    GridPlacementResult result = allocator.Allocate(4, unitId);
    // Slot 4 on a 3x2 board maps to position (1, 1, 0).
}
```

The linear API intentionally supports only `GridType.Rectangular` fields with `Size.z == 1`. Use the `Vector3Int` position API for hex, custom, or 3D fields.

## Notes

`GridSlotAllocator` intentionally stays one-cell focused. Multi-cell pieces should use `FieldGenerator.CanPlaceContentFootprint` and `PlaceContentFootprint` directly.
