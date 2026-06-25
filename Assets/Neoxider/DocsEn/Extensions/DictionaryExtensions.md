# DictionaryExtensions

**Purpose:** Extension methods for `IDictionary<TKey, TValue>` covering the most-repeated get-or-create and counter patterns. (`GetValueOrDefault` already ships with the standard library — this adds the missing `GetOrCreate` and `Increment`.)

---

## API

| Method | Description |
|--------|-------------|
| `TValue GetOrCreate<TKey,TValue>(this IDictionary<TKey,TValue>, TKey key)` (where `TValue : new()`) | Returns existing value or creates `new TValue()`, stores and returns it. Ideal for buckets: `dict.GetOrCreate(id).Add(item)`. |
| `TValue GetOrCreate<TKey,TValue>(this IDictionary<TKey,TValue>, TKey key, Func<TKey,TValue> factory)` | Same, but builds the value via `factory`. |
| `int Increment<TKey>(this IDictionary<TKey,int>, TKey key, int amount = 1)` | Adds `amount` to the int counter at `key` (creating at zero), returns the new total. Replaces `dict[k] = dict.GetValueOrDefault(k) + 1`. |
| `float Increment<TKey>(this IDictionary<TKey,float>, TKey key, float amount = 1f)` | Same for a `float` counter. |

---

## Examples

### Code
```csharp
var loot = new Dictionary<string, List<Item>>();
loot.GetOrCreate("rare").Add(item);     // no manual "if (!contains) add new list"

var kills = new Dictionary<string, int>();
kills.Increment("goblin");              // 1
kills.Increment("goblin", 3);           // 4
```

---

## See Also
- [EnumerableExtensions](EnumerableExtensions.md) — list/collection helpers
- ← [Extensions](README.md)
