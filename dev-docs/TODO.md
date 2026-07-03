# TODO

Current technical tasks that should stay separate from the changelog. This list does not replace release planning; it records near-term public API improvements.

## GridSystem

- Add a generic `GridPlacementService` / rule config on top of the current `FieldGenerator` placement API. A good next shape is `GridPlacementRequest` with `RequireEnabled`, `RequireWalkable`, `RequireUnoccupied`, a custom predicate, and overwrite policy, so gameplay services can reuse placement rules without growing many `FieldGenerator` overloads.
- Consider a non-Mono plain C# `DiceBoard` service over `IGridPlacementBoard` or a `FieldGenerator` adapter, leaving the current `DiceBoardService` as the MonoBehaviour wrapper. This would improve testability and allow Dice mechanics outside scenes, but the existing scene API should stay stable.

## See Also

- [Ideas](IDEAS.md)
- [GridSystem](../Assets/Neoxider/Docs/GridSystem/README.md)
