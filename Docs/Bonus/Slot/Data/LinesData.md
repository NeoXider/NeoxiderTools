# LinesData

**What it is:** a `ScriptableObject` with the payline definitions of a slot machine: each line lists, per column,
the window row it passes through. Script: `Scripts/Bonus/Slot/Data/LinesData.cs`, namespace `Neo.Bonus`.

**How to use:**
1. Create an asset via **Create > Neoxider > Bonus > Slot > Lines Data**. A new asset contains three straight
   lines for a 3x3 window (`0,0,0`, `1,1,1`, `2,2,2`).
2. Add one entry per payline and give its **Cor Y** one value per reel column. Row `0` is the bottom row of the
   visible window.
3. Assign the asset to **Check Spin > Lines Data** on [SpinController](../SpinController.md), or set
   `checkSpin.LinesDataAsset` from code before a spin.

---

## Fields

| Field (Inspector) | Property | Type | Description |
|-------------------|----------|------|-------------|
| Lines | `lines` | `LinesData.InnerArray[]` | Payline definitions; the array index is the line id. |
| Lines > Cor Y | `InnerArray.corY` | `int[]` | Window row per column, bottom = `0`. |

## Validation

[CheckSpin](../CheckSpin.md) keeps only lines whose `corY` length equals the column count and whose rows lie
inside the visible window. When no line is valid, or no asset is assigned, it falls back to horizontal lines over
the **Fallback Window Row Min/Max** range. `GetEffectiveLines(columns, rows)` returns the lines actually used;
`GetPaylineDefinitionCount(columns, rows)` counts them without building the list.

```csharp
LinesData.InnerArray[] lines = spinController.checkSpin.GetEffectiveLines(columnCount, windowRowCount);
```

## See also

- [CheckSpin](../CheckSpin.md) — payline evaluation.
- [Slot data assets](README.md)
