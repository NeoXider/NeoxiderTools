# Level curve evaluation boundaries

This is the API and evaluation-boundary reference for the `Neo.Core.Level.Data` assembly, opened by the asset's documentation link. Start with [Level Curve Definition](../../LevelCurveDefinition.md) for the Formula, Curve and Custom authoring guide. `LevelCurveDefinition` implements `ILevelCurveDefinition`; `LevelCurveEntry` implements `ILevelCurveEntry`. Both types use the `Neo.Core.Level` namespace. Projects with their own asmdef need a reference to `Neo.Core.Level.Data`; pure interfaces and the domain evaluator belong to `Neo.Core`.

## Types and configuration

| Public API | Contract |
| --- | --- |
| `Mode`, `FormulaType` | Read-only configuration selections. Author Formula/Curve/Custom in the Inspector. |
| `SetLinear(int xpPerLevel)` | Selects Formula mode, Linear type and XP per level, clamped to at least one. |
| `XpPerLevel` | Read/write formula parameter, clamped to at least one. Setting it alone preserves the current mode and formula type. |
| `ConstantOffset`, `QuadraticBase`, `ExpBase`, `ExpFactor`, `PowerBase`, `PowerExponent` | Read-only access to the serialized formula parameters. |
| `AnimationCurve` | The mutable backing curve, used in Curve mode. |
| `CustomEntries` | The backing table exposed as `IReadOnlyList<LevelCurveEntry>`; nullable for absent data. |
| `CurveType` | Legacy classification. Curve mode and Power/PolynomialSingle formulas report `Linear`; use `Mode` and `FormulaType` to identify the full configuration. |
| `LevelCurveEntry()` | Creates a serializable data object with level one and zero required XP. |
| `LevelCurveEntry(int level, int requiredXp)` | Clamps constructor inputs to level ≥ 1 and XP ≥ 0; exposes read-only `Level` and `RequiredXp`. Creating an entry does not add it to an asset's table. |

The entry is a plain serializable class. The definition is a ScriptableObject storing configuration. Mode, non-linear formula parameters and the Custom table are authored through serialized fields. The definition's direct configuration setters are `SetLinear` and `XpPerLevel`; the returned curve remains mutable.

## C# API and integer XP

`EvaluateLevel(totalXp, maxLevel)` and `GetXpToNextLevel(totalXp, maxLevel)` can be called directly on an asset or through `ILevelCurveDefinition`. Supply nonnegative accumulated integer XP. The level is at least one. A positive `maxLevel` caps progression; zero leaves it uncapped by the caller. Formula and Custom evaluation delegate to the pure C# `LevelCurveEvaluator` in `Neo.Core`.

`GetRequiredXpForLevel(level)` returns the cumulative integer XP threshold for previews and debugging. Formula and Curve thresholds round **up**: a value of 12.5 requires 13 integer XP. Results saturate at `int.MaxValue` instead of wrapping. Saturation is a representation limit: it does not promise that the requested level is reachable with `int.MaxValue` XP. Levels below one, missing curves and missing Custom entries return zero.

```csharp
using Neo.Core.Level;
using UnityEngine;

LevelCurveDefinition definition = ScriptableObject.CreateInstance<LevelCurveDefinition>();
definition.SetLinear(100);

ILevelCurveDefinition curve = definition;
int level = curve.EvaluateLevel(250, maxLevel: 50);       // 3
int remaining = curve.GetXpToNextLevel(250, maxLevel: 50); // 50
int threshold = definition.GetRequiredXpForLevel(4);     // 300

Object.Destroy(definition); // The caller owns assets created with CreateInstance.
```

Use `LevelModel.SetCurveDefinition(definition)` for a pure C# state model that consumes the interface. Configure an asset before binding it. A loaded shared asset should be treated as configuration; `SetLinear`, the `XpPerLevel` setter and the exposed `AnimationCurve` mutate that shared definition for all consumers on their next calculation. The asset does not publish change notifications. Calling `model.SetCurveDefinition(definition)` again explicitly recalculates that model's level and remaining XP when XP mode is enabled. `LevelComponent` ignores assignment of the same asset reference, so that assignment is not a refresh command.

## Curve domain and terminal state

Curve mode samples integer levels starting at one. The last reachable integer level is the floor of the last key's X coordinate, bounded to 1–10,000. The existing 10,000-level scan limit applies even when the caller does not supply a cap. For example, a final key at X = 2.4 or 2.6 allows levels 1 and 2. AnimationCurve wrapping does not extend the level domain.

`GetXpToNextLevel` returns zero at the authored end, the scan limit or the caller's positive cap. It never requests XP for a level outside that domain. Missing or empty curves resolve to level one with zero remaining XP. Within the domain, remaining XP is rounded up after subtracting the supplied total, and saturated at `int.MaxValue` if necessary.

Author finite, nonnegative, nondecreasing cumulative XP at integer levels. Curve evaluation stops at the first unmet threshold; decreasing or non-finite samples are not validated as a progression schedule. `GetRequiredXpForLevel` is a preview query and does not validate the requested level against the authored domain. Its defensive numeric conversion maps nonpositive or NaN values to zero and positive overflow to `int.MaxValue`.

Each Curve level query scans up to 10,000 integer samples. The remaining-XP query performs its own level scan, so requesting both values repeats that work. Use the model's stored results between XP/configuration changes when many consumers need the same state. The current asset evaluates its mutable data directly; no snapshot or automatic cache API is provided.

## Inspector and NoCode workflow

1. Create **Create → Neoxider → Core → Level Curve Definition** and choose Formula, Curve or Custom.
2. For Curve, place the first key at (1, 0), use integer X coordinates for intended terminal levels, and check tangents for overshoot between keys. Keep all reachable integer thresholds within the integer XP range.
3. For Custom, use unique level IDs with nondecreasing cumulative XP. Equal thresholds intentionally allow several levels to be reached together. `LevelCurveEntry(int level, int requiredXp)` clamps constructor input to level ≥ 1 and XP ≥ 0. Inspector `[Min]` attributes are authoring hints; validate externally imported serialized values yourself.
4. Assign the asset to the **Level Curve** field on [LevelComponent](../Components/LevelComponent.md). Its XP state and events expose the evaluated result to scene bindings; optional [LevelNoCodeAction](../Bridge/LevelNoCodeAction.md) forwards actions to the component's typed API. Scene references belong to those adapters, while this asset stores configuration.
5. In C#, use `component.AddXp(amount)` or `component.SetLevel(level)`. For Inspector actions, configure `LevelNoCodeAction` with the provider and AddXp/SetLevel operation, then call `Execute()`. Both paths use the provider's runtime API; they do not require scene references inside the curve asset.

`TryGetDefinition(level, out entry)` looks up the first matching Custom entry, skips null entries and returns `false` with a null result for an absent table or missing level. `CustomEntries` exposes the underlying table as an `IReadOnlyList`; it can be null for absent data and is not an immutable snapshot. Missing entries have no interpolated threshold. Keep levels unique and order the table by level for authoring; entry lookup and domain evaluation resolve duplicate data differently.

## 11.0 compatibility notes

Fractional Formula/Curve previews now round upward, so the reported integer threshold is sufficient for that threshold's sample when it lies in the reachable domain. For example, 12.25 requires 13 XP. The sampled float threshold 16,777,220 is not met at 16,777,219 integer XP. Requirements beyond the integer range saturate for previews; a threshold of 3,000,000,000 with 2,000,000,000 accumulated XP still reports the representable remaining amount of 1,000,000,000.

A fractional last X no longer grants a rounded-up level outside the authored domain: both X = 2.4 and X = 2.6 end at level two. Reaching that end, the existing 10,000 scan cap or the caller's cap reports zero remaining XP. Reauthor an endpoint at the intended integer level if a project relied on the previous rounding-up behavior.

Serialized field names, public signatures and asset GUIDs are preserved. These corrections do not rewrite assets, scenes or prefabs, and do not introduce additional scene setup. Formula integration remains shared with the Core evaluator.
