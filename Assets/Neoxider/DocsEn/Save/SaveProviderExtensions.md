# SaveProviderExtensions

**Purpose:** Extension methods for `ISaveProvider` — adds `int[]` and `float[]` save/load support. Arrays are serialized as comma-separated strings.

---

## API

| Method | Description |
|--------|-------------|
| `void SetIntArray(this ISaveProvider, string key, int[] array)` | Save an int array. `null` or empty array deletes the key. |
| `int[] GetIntArray(this ISaveProvider, string key, int[] defaultValue = null)` | Load an int array. Missing key returns `defaultValue` or empty array. |
| `void SetFloatArray(this ISaveProvider, string key, float[] array)` | Save a float array. |
| `float[] GetFloatArray(this ISaveProvider, string key, float[] defaultValue = null)` | Load a float array. |

---

## Examples

### Code
```csharp
ISaveProvider provider = SaveProvider.I;

// Save high scores
provider.SetIntArray("HighScores", new[] { 100, 250, 500 });
provider.Save();

// Load
int[] scores = provider.GetIntArray("HighScores");
// scores = [100, 250, 500]

// With default value
float[] times = provider.GetFloatArray("BestTimes", new[] { 99.9f });
```

---

## See Also
- [ISaveProvider](ISaveProvider.md) — base interface
- ← [Save](README.md)
