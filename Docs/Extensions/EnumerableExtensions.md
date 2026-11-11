# EnumerableExtensions

**What it is:** small collection helpers for `IEnumerable<T>`, `IList<T>` and arrays: safe indexing, wrapping,
emptiness checks and debug strings. File: `Scripts/Extensions/EnumerableExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call the helper on the collection: `items.GetSafe(index, fallback)`, `slots.GetWrapped(i)`, `list.IsNullOrEmpty()`.

---

## Methods

| Method | Description |
|--------|-------------|
| `void ForEach<T>(this IEnumerable<T>, Action<T>)` | Runs the action for each item; a null source or action does nothing. |
| `T GetSafe<T>(this IList<T>, int index, T defaultValue = default)` | Element or `defaultValue` for a null list or an out-of-range index. |
| `T GetWrapped<T>(this IList<T>, int index)` | Element at `index` wrapped into `[0, Count)`, including negative indices. Null/empty throws `ArgumentException`. |
| `bool IsValidIndex<T>(this ICollection<T>, int index)` | `true` for a non-null collection and `0 <= index < Count`. |
| `bool IsNullOrEmpty<T>(this IEnumerable<T>)` | `true` for null or no elements. Sized collections (`ICollection<T>`, `IReadOnlyCollection<T>`, `ICollection`) answer from `Count` without allocating an enumerator; other sequences are probed with `Any()`. |
| `string ToStringJoined<T>(this IEnumerable<T>, string separator = ", ")` | `string.Join` of the items. |
| `string ToIndexedString<T>(this IEnumerable<T>)` | One `[index] item` line per element. |
| `string ToDebugString<T>(this IList<T>)` | `[0: a, 1: b]`. |
| `IEnumerable<T> FindDuplicates<T>(this IEnumerable<T>)` | Lazy: values that occur more than once (each reported once). |
| `int CountEmptyElements<T>(this T[])` | Number of null elements; a null array throws `ArgumentNullException`. |

## Example

```csharp
string[] tips = { "Jump", "Dash", "Block" };
string tip = tips.GetWrapped(day);           // cycles forever, also for negative values
string safe = tips.GetSafe(99, "No tip");    // "No tip"
if (inventory.IsNullOrEmpty()) { /* ... */ }
```

## See Also
- [DictionaryExtensions](DictionaryExtensions.md)
- ← [Extensions](README.md)
