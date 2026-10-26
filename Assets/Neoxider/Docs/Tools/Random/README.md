# Tools / Random

Weighted selection for loot tables, random events, and other reusable probability tables. Runtime code lives in `Scripts/Tools/Random/`, namespace `Neo.Tools`, assembly `Neo.Tools.Random`.

## Start with the runtime API

```csharp
using Neo.Tools;

ChanceManager table = new(60f, 30f, 10f);
if (table.TryEvaluate(out int index, out ChanceManager.Entry entry))
{
    // Use index to address a parallel reward list; entry contains label, weight, and CustomId.
}
```

`GetChanceId()` and `GetId(float)` return a zero-based list index, not `Entry.CustomId`. Empty tables or tables with no positive weights return `-1`. Constructors and collection changes normalize automatically by default; use `AutoNormalize = false` before adding entries to retain raw weights. For a batch of relative weights, use the constructor or `AddChances` so normalization runs once after the batch.

Zero-weight entries cannot be selected while positive weights exist. An all-zero table is redistributed evenly during normalization by default; disable `DistributeEvenlyWhenZero` or `AutoNormalize` when zero means an entirely disabled table.

## Scene and asset workflow

Add `ChanceSystemBehaviour` through **Component > Neoxider > Tools > ChanceSystemBehaviour**. Configure its inline manager or assign a `ChanceData` asset, then connect a UI Button or another UnityEvent to `GenerateVoid()`. The component copies asset configuration at `Awake` and emits index, probability, per-index, and completion events. Scene references belong in the component's events.

Create reusable configurations with **Assets > Create > Neoxider > Tools > Random > Chance Data**. Code can use `ChanceData.Manager` directly; those callers share the asset's mutable table. Use `CopyFrom` or the scene component for an independent table.

## Component documentation

| Page | Description |
|------|-------------|
| [ChanceManager](./ChanceManager.md) | Runtime selection, normalization, deterministic rolls, and mutation rules |
| [ChanceSystemBehaviour](./ChanceSystemBehaviour.md) | Scene configuration, UnityEvents, and C# event subscriptions |
| [Data/README](./Data/README.md), [ChanceData](./Data/ChanceData.md) | Reusable asset configuration and independent copies |

## See also

- [Tools module](../README.md)
- [Bonus](../../Bonus/README.md)
- [Tools/Spawner](../Spawner/README.md)
