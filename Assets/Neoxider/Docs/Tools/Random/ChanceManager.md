# ChanceManager

Serializable C# weighted table used by `ChanceSystemBehaviour` and `ChanceData`. It can also be constructed directly without scene objects. Reference assembly `Neo.Tools.Random` and use namespace `Neo.Tools`.

## Selection and deterministic use

```csharp
ChanceManager table = new() { AutoNormalize = false };
table.AddChances(new[] { 0f, 2f, 6f });
int index = table.GetId(0.5f); // 2
float probability = table.GetNormalizedWeight(index); // 0.75

table.RandomProvider = () => 0.1f;
ChanceManager.Entry selected = table.Evaluate(); // Entry at index 1.
```

All selection methods return list indices or entries, never `Entry.CustomId`. `GetId` clamps values to `[0, 1]`; `0` selects the first positive-weight entry and `1` the last positive-weight entry. Buckets use an exclusive upper boundary: equal weights at `0.5` select the second entry. Zero-weight entries are skipped, including at either endpoint. A `NaN` roll produces no result. Without `RandomProvider`, rolls use `UnityEngine.Random.value`.

Empty or zero-total tables return `-1` from index methods, `null` from `Evaluate`, and `false` with `index = -1` / `entry = null` from `TryEvaluate`.

## API

| Method / Property | Description |
|-------------------|-------------|
| `ChanceManager(params float[] weights)` | Constructor with initial weights. |
| `int Count`, `IReadOnlyList<Entry> Entries` | Entry count and entry references in selection order. |
| `int GetChanceId()` | Roll using the configured provider or Unity random. |
| `int GetId(float randomValue)` | Select deterministically from a normalized random value. |
| `Entry Evaluate()`, `Entry Evaluate(float randomValue)` | Select and return the entry. |
| `bool TryEvaluate(out int index, out Entry entry)` | Select and return both index and entry. |
| `bool TryEvaluate(float randomValue, out int index, out Entry entry)` | Deterministic selection of both values. |
| `Entry AddEntry(float weight = 1f, string label = null, bool locked = false, int customId = -1)` | Add an entry with metadata. |
| `int AddChance(float chance)` | Add a weight and return its index. |
| `void AddChances(IEnumerable<float> weights)` | Add a batch, then normalize once if enabled. |
| `void SetChanceValue(int index, float weight)` | Change a weight, invalidate the total, and normalize if enabled. |
| `float GetChanceValue(int index)`, `Entry GetEntry(int index)` | Read a weight or entry; invalid indices return `0` / `null`. |
| `void SetLocked(int index, bool locked)` | Change the normalization lock and normalize if enabled. |
| `void RemoveChance(int index)` | Remove an entry by index. |
| `void Clear()` | Remove all entries. |
| `void Normalize()`, `void Normalize(float targetSum)` | Redistribute unlocked weights toward the configured or explicit target. |
| `float TotalWeight` | Cached sum of usable weights. |
| `float GetNormalizedWeight(int index)` | Get normalized probability for an entry. |
| `void InvalidateTotalWeight()` | Refresh the total on its next read after direct entry edits. |
| `void Sanitize()` | Repair weight/label/target values and normalize if enabled. Call after custom deserialization. |
| `void EnsureUniqueIds()` | Assign nonnegative, unique custom IDs, repairing invalid or duplicate IDs. |
| `void CopyFrom(ChanceManager source)` | Deep-copy entries and normalization settings; null and self-copy do nothing. Keeps the destination random provider. |
| `List<string> ValidateWeights()` | Return diagnostic messages for empty/zero-total tables and invalid or unselectable weights. |

## Weights, locks, and normalization

`AutoNormalize` defaults to `true`. The constructor and collection mutation methods normalize after applying their changes. Set it to `false` before adding weights when values are raw counts or relative weights. Repeated single-entry additions with auto-normalization operate on the already normalized table; use a batch for a new set of relative weights.

`NormalizeTarget` defaults to `1` and has a minimum of `0.001`; negative and non-finite assignments use that minimum. `Normalize(float)` treats negative or non-finite targets as zero. Negative, `NaN`, and infinite entry weights are sanitized to zero.

Locked weights are preserved, and unlocked entries share the remaining target in proportion to their weights. If locked weights already meet or exceed the target, unlocked weights become zero; the total may remain above the target. With only locked entries, normalization leaves their weights unchanged. If all unlocked weights are zero, `DistributeEvenlyWhenZero` (default `true`) splits any remaining target evenly; set it to `false` to leave those entries disabled.

Normalization refreshes the cached total, including when called directly or through `AutoNormalize` / `NormalizeTarget`. Sums and selection use double precision internally, so very large finite weights retain their proportions. The public `TotalWeight` remains a `float` for compatibility and can return positive infinity if a raw sum exceeds `float.MaxValue`; use `GetNormalizedWeight` for probabilities.

## Bulk configuration and sampling cost

The constructor and `AddChances` apply a batch before normalizing. Repeated `AddEntry` / `AddChance` calls normalize and repair IDs after each addition, so filling a large table this way can accumulate quadratic work. Use the existing bulk API when adding a complete set of relative weights. Bulk additions still include ID repair; they do not guarantee linear construction time.

Each selection walks the entries. `Normalize` creates a temporary list of unlocked entries. Configure or normalize when the weights change, then reuse the table for repeated rolls. Measure construction and roll costs in the consuming system before choosing a different sampling representation.

## Entry mutation and custom IDs

Each `Entry` exposes `Weight`, `Locked`, `CustomId`, `Label`, and `Notes`. Prefer `SetChanceValue` and `SetLocked` for edits that must update manager state. Direct `Entry.Weight` edits sanitize the value but require `InvalidateTotalWeight()` or `Normalize()` afterward; they do not trigger auto-normalization. Treat `Entries` as a read-only collection.

For example, editing the raw table from the deterministic example above:

```csharp
table.GetEntry(1).Weight = 4f;
table.InvalidateTotalWeight();
float updatedProbability = table.GetNormalizedWeight(1); // 4 / (4 + 6) = 0.4.
```

Collection mutation methods repair custom IDs. Call `EnsureUniqueIds()` when constructing a table from weights or after directly editing IDs if your game uses them; scene initialization and asset validation also call it. Custom IDs are metadata, not array indices. Changing selection order can change indices while preserving your chosen IDs.

## See Also

- [ChanceSystemBehaviour](ChanceSystemBehaviour.md)
- ← [Tools/Random](README.md)
