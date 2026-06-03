# Merge

`Neo.Merge` is a generic pure C# merge engine for connected groups. It is not tied to Unity scenes, grids, inventory slots, or any specific game.

Use it when a mechanic needs to find equivalent connected items, choose the result item, compute a new value, clear the rest, and optionally continue cascading from the result.

## Runtime API

- `MergeRequest<TItem, TValue>` defines items, seeds, value access, neighbors, match rules, result selection, merged value, cascade mode, and mutate/dry-run mode.
- `MergeResolver.Resolve(request)` returns `MergeResult<TItem, TValue>`.
- `MergeResult` contains resolved groups and changed items.

## Examples

- Grid games: use `Neo.GridSystem.Merge.GridMergeResolver` to adapt `FieldGenerator` cells.
- Dice games: use `Neo.GridSystem.Dice.DiceBoardService` for dice placement and dice-specific merge rules.
- Custom systems: pass any graph/list/inventory nodes as `TItem` and provide a neighbor callback.

## See also

- [GridSystem](../GridSystem/README.md)
- [Dice](../GridSystem/Dice/README.md)
