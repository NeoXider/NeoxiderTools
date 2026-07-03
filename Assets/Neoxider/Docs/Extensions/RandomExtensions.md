# RandomExtensions

**Purpose:** Extension methods and utilities for random number generation and collections.

## API

| Method | Description |
|--------|-------------|
| `GetRandomElement<T>(this IList<T>)` | Random element from list/array. |
| `Shuffle<T>(this IList<T>)` | Shuffle elements in-place. |
| `GetRandomElements<T>(this IList<T>, int count)` | Get N random elements. |
| `GetRandomIndex<T>(this ICollection<T>)` | Random valid index. |
| `Chance(this float probability)` | Returns `true` with given probability (0–1). |
| `RandomRange(this Vector2)` | Random float between x and y. |
| `RandomBool()` | Random boolean. |
| `RandomColor(float alpha = 1f)` | Random color. |
| `GetRandomEnumValue<T>()` | Random enum value. |
| `GetRandomWeightedIndex(this IList<float>)` | Weighted random index. |
| `GetRandomWeighted<T>(this IList<T>, IList<float> weights)` | Weighted random **element** (not index); parallel weights list, same length. |
| `GetRandomWeighted<T>(this IList<T>, Func<T,float> weightSelector)` | Weighted random element using a per-element weight selector. |

## See Also
- ← [Extensions](README.md)
