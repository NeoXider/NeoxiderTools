# ChanceData

ScriptableObject configuration for a reusable [ChanceManager](../ChanceManager.md) table. Use namespace `Neo.Tools` and assembly `Neo.Tools.Random`.

## Setup

1. Create **Assets > Create > Neoxider > Tools > Random > Chance Data**.
2. Configure entries, locks, normalization target, and zero-weight redistribution in **Manager**. Editor validation sanitizes the table and repairs custom IDs.
3. Assign the asset to a [ChanceSystemBehaviour](../ChanceSystemBehaviour.md). The component copies configuration at `Awake`; subsequent scene-table edits do not change the asset.
4. Wire scene-specific events on the component. The asset stores reusable data; do not put scene object references in it.

## Runtime API

| Field | Description |
|-------|-------------|
| `Manager` | The asset's mutable table. Direct callers share it. |
| `int GenerateId()` | Roll on the asset's table, invoke its `OnIdGenerated`, and return a list index or `-1`. |
| `OnIdGenerated` | Runs only when `GenerateId()` is called on this asset. Component rolls do not forward to it. |
| `int AddChance(float weight)` | Add a weight and return its index. |
| `RemoveChance`, `GetChance`, `SetChance`, `ClearChances` | Forward to the corresponding manager operations. |

For isolated C# use, copy the configuration into a new manager:

```csharp
// data is an explicitly supplied ChanceData asset reference.
ChanceManager runtimeTable = new();
runtimeTable.CopyFrom(data.Manager);
runtimeTable.EnsureUniqueIds();
if (runtimeTable.TryEvaluate(out int index, out ChanceManager.Entry entry))
{
    // Resolve your game data by index or entry.CustomId.
}
```

`CopyFrom` copies entries and normalization settings, including labels, notes, locks, and custom IDs. It does not copy the source's `RandomProvider`. For direct shared use, call `data.GenerateId()` or `data.Manager.TryEvaluate(...)`; edits through `data.Manager` affect all callers of that asset. The manager guide describes normalization, direct-entry cache invalidation, and invalid numeric inputs.

## See Also

- [Data assets](README.md)
- [Random module](../README.md)
