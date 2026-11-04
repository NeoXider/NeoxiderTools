# SaveProviderExtensions

**What it is:** extension methods for `ISaveProvider` (`Scripts/Save/SaveProviderExtensions.cs`, namespace `Neo.Save`): 64-bit integers and `int[]` / `float[]` arrays. All numbers are written in the invariant culture, so a save stays readable after the device locale changes.

**How to use:**
1. Add `using Neo.Save;`.
2. Call the methods on any provider, e.g. `SaveProvider.CurrentProvider.SetLong("cents", value)`. The static facade also offers `SaveProvider.GetLong` / `SetLong`.

---

## API

| Method | Description |
|--------|-------------|
| `long GetLong(this ISaveProvider, string key, long defaultValue = 0L)` | Load a 64-bit integer. Uses `ILongSaveProvider` when implemented; otherwise parses an invariant integer string and widens a value stored with `SetInt`. Missing or non-integer values return `defaultValue`. |
| `void SetLong(this ISaveProvider, string key, long value)` | Save a 64-bit integer. Uses `ILongSaveProvider` when implemented; otherwise stores an invariant integer string. |
| `void SetIntArray(this ISaveProvider, string key, int[] array)` | Save an int array as a comma-separated string. `null` or empty array deletes the key. |
| `int[] GetIntArray(this ISaveProvider, string key, int[] defaultValue = null)` | Load an int array. A missing key, an invalid element or an element outside the `int` range returns `defaultValue` or an empty array. |
| `void SetFloatArray(this ISaveProvider, string key, float[] array)` | Save a float array. |
| `float[] GetFloatArray(this ISaveProvider, string key, float[] defaultValue = null)` | Load a float array. |

Reads accept values that older builds wrote with the device culture as a fallback.

---

## Examples

### Code
```csharp
ISaveProvider provider = SaveProvider.CurrentProvider;

// Money in cents does not fit in int
provider.SetLong("WalletCents", 12_345_678_901L);
long cents = provider.GetLong("WalletCents");

// A counter previously saved with SetInt keeps its value after switching to long
long kills = provider.GetLong("Kills"); // reads the old int entry

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
- [ISaveProvider](ISaveProvider.md) — base interface and `ILongSaveProvider`
- ← [Save](README.md)
