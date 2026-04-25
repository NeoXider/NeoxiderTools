# ChanceManager

**Purpose:** Core runtime class for weighted random selection. Manages a list of chance entries (weights, labels), normalization, and evaluation. Used by `ChanceSystemBehaviour` as its engine.

## API

| Method / Property | Description |
|-------------------|-------------|
| `ChanceManager(params float[] weights)` | Constructor with initial weights. |
| `int Evaluate()` | Evaluate and return a random index based on weights. |
| `bool TryEvaluate(out int index, out Entry entry)` | Try to evaluate; returns false if empty. |
| `void AddChance(float weight, string label)` | Add a new entry. |
| `void SetChance(int index, float weight)` | Set weight for an existing entry. |
| `void RemoveChance(int index)` | Remove an entry by index. |
| `void ClearChances()` | Remove all entries. |
| `void Normalize()` | Normalize all weights to sum to 1.0. |
| `float GetNormalizedWeight(int index)` | Get normalized probability for an entry. |

## See Also
- [ChanceSystemBehaviour](ChanceSystemBehaviour.md)
- ← [Tools/Random](README.md)
