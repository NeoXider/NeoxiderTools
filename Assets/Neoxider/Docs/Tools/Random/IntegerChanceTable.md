# IntegerChanceTable

**What it is:** A plain C# weighted table with integer weights (`Scripts/Tools/Random/IntegerChanceTable.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Random`). Every entry's probability is exactly `weight / TotalWeight`, so odds such as 1 per 1,000,000 stay exact; float-weighted [ChanceManager](ChanceManager.md) normalizes and cannot guarantee that.

**How to use:**
1. Create it in code or as a serialized field (`[SerializeField] private IntegerChanceTable odds;` shows a list of weight/label/id rows in the Inspector).
2. `Add(weight, label)` entries, or edit them in the Inspector (also during Play Mode): the cached sums refresh after every deserialization, so no `OnValidate` code is needed.
3. `Roll()` for an index, or `GetIndex(ticket)` when your own random source produces a ticket in `[0, TotalWeight)`.

---

## Methods and properties

| Member | Description |
|--------|-------------|
| `IntegerChanceTable()`, `IntegerChanceTable(params int[] weights)` | Empty table or one entry per weight; negative weights are stored as zero. |
| `Entry Add(int weight, string label = null, int customId = -1)` | Appends an entry. |
| `void SetWeight(int index, int weight)` / `int GetWeight(int index)` | Change/read a weight. Change weights through the table so the cache refreshes. |
| `void RemoveAt(int index)`, `void Clear()` | Structural edits. |
| `void Invalidate()` | Marks the cached prefix sums stale. Rarely needed: table methods and deserialization (including Inspector edits) already do it; use it after writing rows by other means such as reflection. |
| `long TotalWeight` | Exact sum (64-bit, so many `int.MaxValue` weights do not overflow). |
| `int GetIndex(long ticket)` | Maps a ticket in `[0, TotalWeight)` to an index in O(log n); out-of-range tickets return `-1`. Zero-weight entries are never selected. |
| `int Roll()` / `bool TryRoll(out int index, out Entry entry)` | Rolls with `RandomBelow`; `-1` / `false` when the total is zero. |
| `Func<long, long> RandomBelow` | Uniform source returning `[0, n)`. `null` (default) uses `UnityRandomBelow`. |
| `static long UnityRandomBelow(long n)` | Unbiased sampler over `UnityEngine.Random` (30-bit draws with rejection), so `Random.InitState` keeps rolls reproducible. Accepts any positive `long` up to `long.MaxValue`; `n <= 1` returns 0. |
| `bool TryGetOdds(int index, out long weight, out long total)`, `double GetProbability(int index)` | Exact odds or a double probability. |
| `Entry.Weight`, `Entry.Label`, `Entry.CustomId` | Row data; `CustomId` is caller metadata such as a loot id. |

## Example

```csharp
using Neo.Tools;

IntegerChanceTable slotOdds = new IntegerChanceTable();
slotOdds.Add(1, "Jackpot");      // exactly 1 / 1,000,000
slotOdds.Add(4_999, "Big win");
slotOdds.Add(995_000, "Nothing");

int outcome = slotOdds.Roll();

// Deterministic simulation with your own seeded source:
System.Random rng = new System.Random(42);
slotOdds.RandomBelow = n => (long)(rng.NextDouble() * n);
```

The `System.Random` lambda above is only an illustration; `NextDouble() * n` is not perfectly uniform for very large `n`. Use `UnityRandomBelow` or an integer sampler with rejection when exactness matters for large totals.

## Scene/NoCode workflow

There is no separate component: add a serialized `IntegerChanceTable` field to your own MonoBehaviour or ScriptableObject and author the rows in the Inspector. Inspector edits reach the next `Roll()` without extra code. For UnityEvent-driven rolls with float weights use [ChanceSystemBehaviour](ChanceSystemBehaviour.md).

## See also

- [ChanceManager](ChanceManager.md)
- ← [Tools/Random](README.md)
