# LevelConditionAdapter

Evaluates a level or XP threshold through `IConditionEvaluator` in `Neo.Core.Level.Bridge` (namespace `Neo.Core.Level`). A C# consumer supplies the GameObject context and chooses when to evaluate.

## Setup

1. Add **Neoxider → Core → Level Condition Adapter** to a GameObject.
2. Assign **Level Provider** to a `LevelComponent`, or leave it empty to resolve one on the evaluation context.
3. Select **Mode**, set the nonnegative **Threshold**, and optionally enable **Invert**.
4. Call `Evaluate(context)` from a consumer of `IConditionEvaluator`, or `EvaluateCurrent()` to check the adapter's own GameObject.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| Level Provider | Explicit `LevelComponent`; takes priority over the context. |
| Mode | One of the comparisons below. |
| Threshold | Integer boundary, inclusive; defaults to 1. |
| Invert | Negates the comparison result. |

| Mode | Comparison before inversion |
|------|-----------------------------|
| `LevelAtLeast` | `provider.Level >= threshold` |
| `XpAtLeast` | `provider.TotalXp >= threshold` |
| `XpToNextLevelAtMost` | `provider.XpToNextLevel <= threshold` |

`XpToNextLevel` may be zero at the level cap or when no next-level requirement is available. In that case, `XpToNextLevelAtMost` passes any nonnegative threshold.

## Runtime API

```csharp
using Neo.Condition;
using Neo.Core.Level;

// adapter and player are existing scene references.
IConditionEvaluator evaluator = adapter;
bool passed = evaluator.Evaluate(player);
bool cachedResult = adapter.LastResult;
bool localResult = adapter.EvaluateCurrent();
```

`Evaluate(context)` uses the explicit provider first, then a `LevelComponent` on `context`. `EvaluateCurrent()` supplies the adapter's own GameObject as that context. Resolution does not search parents, children, or the scene, and is repeated on every call, so a provider added later can be found. A destroyed explicit reference falls back to the context.

With no provider, the comparison is false before inversion: evaluation returns `false` normally and `true` when inverted. `LastResult` always stores the returned, post-inversion value; it is a read-only runtime property, not an Inspector setting. Reading it does not run another evaluation. A null context is supported when an explicit provider is assigned, and otherwise follows the missing-provider behavior.

Mode, threshold, inversion, and the explicit provider are currently configured in the Inspector. For conditions assembled entirely in C#, use `ILevelProvider.Level`, `TotalXp`, or `XpToNextLevel` directly. The adapter currently resolves `LevelComponent` only; it does not discover arbitrary `ILevelProvider` implementations.

## Scene and NoCode conditions

[NeoCondition](../../../Condition/NeoCondition.md) configures its checks through `ConditionEntry` rows. To author a live level check in its Inspector, select the provider GameObject, choose its `LevelComponent`, then use one of these members:

| Member | Example comparison |
|--------|--------------------|
| `LevelStateValue` | `>= 5` for a level gate |
| `XpStateValue` | `>= 250` for accumulated XP |
| `XpToNextLevelStateValue` | `<= 25` for the remaining XP |

Configure NeoCondition's check mode and connect its `OnTrue` / `OnFalse` events to the desired scene actions. The adapter's `IConditionEvaluator` API is available to C# consumers; the NeoCondition member workflow above provides the Inspector route without depending on a previously cached `LastResult`.

## See Also

- [Level bridges](README.md)
- [LevelNoCodeAction](LevelNoCodeAction.md)
- [LevelComponent](../Components/LevelComponent.md)
- [Core module](../../README.md)
