# RandomExtensions

**What it is:** static extension methods for random picks, shuffles, weighted selection and random primitives.
File: `Scripts/Extensions/RandomExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call the helper on the collection or value: `items.GetRandomElement()`, `deck.Shuffle()`, `weights.GetRandomWeightedIndex()`.
3. For deterministic results (tests, replays, daily seeds), pass a seeded `System.Random` or an index source to the overloads that accept one.

---

## Methods

### Collections

| Method | Description |
|--------|-------------|
| `T GetRandomElement<T>(this IList<T>)` | Random element (`UnityEngine.Random`). Null throws `ArgumentNullException`, empty throws `ArgumentException`. |
| `T GetRandomElement<T>(this IList<T>, System.Random random)` | Same with an injected random source. |
| `int GetRandomIndex<T>(this ICollection<T>)` / `(…, System.Random random)` | Random valid index. |
| `IList<T> Shuffle<T>(this IList<T>, bool inplace = true)` | Fisher–Yates shuffle with `UnityEngine.Random`. `inplace: false` shuffles a copy. |
| `IList<T> Shuffle<T>(this IList<T>, System.Random random, bool inplace = true)` | Shuffle with an injected `System.Random`; the same seed always gives the same order. |
| `IList<T> Shuffle<T>(this IList<T>, Func<int, int> nextIndex, bool inplace = true)` | Shuffle with any index source: `nextIndex(n)` must return a value in `[0, n)`; an out-of-range value throws `ArgumentOutOfRangeException`. |
| `IEnumerable<T> GetRandomElements<T>(this IList<T>, int count)` | `count` distinct elements of a shuffled copy. `count <= 0` returns empty; more than the collection holds throws. |
| `T GetRandomWeighted<T>(this IList<T>, IList<float> weights)` | Weighted random **element** with a parallel weights list of the same length; `default` when invalid. |
| `T GetRandomWeighted<T>(this IList<T>, Func<T, float> weightSelector)` | Weighted random element using a per-element weight selector. |
| `int GetRandomWeightedIndex(this IList<float>)` / `(…, System.Random random)` | Weighted index. Weights must be finite and non-negative with a positive total, otherwise `-1`. A zero-weight entry is never returned. No allocation per call. |

All shuffle overloads validate the collection first: a null collection throws `ArgumentNullException`, an empty one
throws `ArgumentException` (existing contract). A null random source or index source throws `ArgumentNullException`.

### Values

| Method | Description |
|--------|-------------|
| `bool Chance(this float probability)` | `true` with the given probability (0–1); outside the range throws. |
| `bool RandomBool()` / `bool Random(this bool)` | 50/50. |
| `Color RandomColor(float alpha = 1f)` | Random RGB color. |
| `T GetRandomEnumValue<T>()` | Random declared value of an enum, from a typed per-enum cache (no allocation after the first call). An enum without values throws `InvalidOperationException`. |
| `RandomizeBetween`, `RandomFromValue`, `RandomToValue`, `RandomRange(Vector2 / Vector2Int)` | Range helpers over `UnityEngine.Random.Range`. |

## Examples

```csharp
using System;
using System.Collections.Generic;
using Neo.Extensions;

List<string> deck = new() { "A", "K", "Q", "J" };

deck.Shuffle();                          // UnityEngine.Random
deck.Shuffle(new Random(dailySeed));     // same order for every player today
deck.Shuffle(max => rng.NextInt(max));   // your own seeded game RNG

float[] lootWeights = { 70f, 25f, 5f };
int tier = lootWeights.GetRandomWeightedIndex(new Random(runSeed));
```

In a test, a scripted index source makes the result exact:

```csharp
List<int> list = new() { 1, 2, 3, 4 };
list.Shuffle(max => 0); // always swap with the first slot -> { 2, 3, 4, 1 }
```

## Scene / NoCode workflow

There is no component. Call the methods from a scene component (for example a deck or loot table) and expose a
public method to UnityEvents if a button should trigger the shuffle. For weighted chances configured in the
Inspector, see `ChanceManager` in [Tools/Random](../Tools/Random/README.md).

## See Also
- [Numeric, time and random edge cases](NumericEdgeCases.md)
- ← [Extensions](README.md)
