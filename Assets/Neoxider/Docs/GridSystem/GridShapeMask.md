# GridShapeMask

**What it is:** a reusable ScriptableObject that enables or disables listed cells of a `FieldGenerator` board (`Scripts/GridSystem/GridShapeMask.cs`, namespace `Neo.GridSystem`). Use it for custom-shaped boards: holes, crosses, hearts, level silhouettes. It stores only coordinates and a mode, never scene references.

**How to use:**
1. Create the asset with **Create > Neoxider > GridSystem > Grid Shape Mask**, or call `GridShapeMask.Create(mode, cells)` in code.
2. Pick the `Mode` and list logical cell coordinates in `Cells`.
3. Assign it to `FieldGenerator.Config.ShapeMask` and regenerate the field (or call `ApplyShapeMask()`).

---

## Fields

| Field | Description |
|-------|-------------|
| `Mode` | `EnabledCells`: only listed cells stay enabled; on a `Custom` grid type they form the whole board. `DisabledCells`: listed cells are removed from the base shape. |
| `Cells` | Logical cell coordinates (`Vector3Int`) the mode applies to. |

## Methods

| Method | Description |
|--------|-------------|
| `static GridShapeMask Create(GridShapeMaskMode mode, IEnumerable<Vector3Int> cells)` | Runtime instance for code-built boards (not saved as an asset; destroy it with the board). |
| `void SetCells(IEnumerable<Vector3Int> cells)` | Replace the coordinates. |
| `Mode { get; set; }` | Change the interpretation. |
| `bool Contains(Vector3Int pos)` | Membership test. |
| `bool EvaluateCell(bool baseEnabled, Vector3Int pos, GridType gridType)` | Final enabled state used by `FieldGenerator`. |

Mask changes take effect on the next `GenerateField()` or `ApplyShapeMask()`. Per-cell overrides on the config (`DisabledCells`, `ForcedEnabledCells`, `BlockedCells`, `ForcedWalkableCells`) are applied after the mask.

> **Shared assets:** `SetCells` and the `Mode` setter edit the object they are called on. Called on a project asset assigned in the Inspector, they change that asset itself: in the Editor the change survives leaving Play Mode, and every board that references the asset sees it. Change shapes at runtime on a private copy instead: build one with `GridShapeMask.Create(...)`, or clone the asset with `Object.Instantiate(asset)` and assign the copy to `Config.ShapeMask`.

## Example

```csharp
using Neo.GridSystem;
using UnityEngine;

GridShapeMask plus = GridShapeMask.Create(GridShapeMaskMode.EnabledCells, new[]
{
    new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0),
    new Vector3Int(2, 1, 0), new Vector3Int(1, 2, 0)
});

FieldGeneratorConfig config = new FieldGeneratorConfig(new Vector3Int(3, 3, 1), null, GridType.Custom)
{
    ShapeMask = plus
};
field.GenerateField(config);
```

## See also

- [FieldGenerator](FieldGenerator.md)
- [GridSystem module](README.md)
