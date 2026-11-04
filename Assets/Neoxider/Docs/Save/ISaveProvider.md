# ISaveProvider

**What it is:** the backend contract of the save system (`Scripts/Save/Providers/ISaveProvider.cs`, namespace `Neo.Save`). Code written against it works the same with PlayerPrefs or a JSON file. Implementations: `PlayerPrefsSaveProvider`, `FileSaveProvider`. The optional `ILongSaveProvider` (`Scripts/Save/Providers/ILongSaveProvider.cs`) adds native 64-bit integer storage.

**How to use:**
1. Get the active provider with `SaveProvider.CurrentProvider`, or construct one (`new FileSaveProvider("slot1.json")`).
2. Read and write typed values, then call `Save()` to flush.
3. To plug in another backend (cloud, platform SDK), implement `ISaveProvider` and activate it with `SaveProvider.SetProvider(provider)`. Implement `ILongSaveProvider` as well when the backend has a native 64-bit type.

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `SaveProviderType ProviderType { get; }` | Provider type (`PlayerPrefs` or `File`). |
| `int GetInt(string key, int defaultValue = 0)` | Read an integer. Returns `defaultValue` if key is missing. |
| `void SetInt(string key, int value)` | Write an integer. |
| `float GetFloat(string key, float defaultValue = 0f)` | Read a float. |
| `void SetFloat(string key, float value)` | Write a float. |
| `string GetString(string key, string defaultValue = "")` | Read a string. |
| `void SetString(string key, string value)` | Write a string. |
| `bool GetBool(string key, bool defaultValue = false)` | Read a boolean. |
| `void SetBool(string key, bool value)` | Write a boolean. |
| `bool HasKey(string key)` | Check if a key exists in storage. |
| `void DeleteKey(string key)` | Delete a key and its value. |
| `void DeleteAll()` | Delete all data. |
| `void Save()` | Force flush to disk. |
| `void Load()` | Force reload from storage. |

## 64-bit integers

`GetLong(key, defaultValue)` and `SetLong(key, value)` are available on every provider as extension methods from [SaveProviderExtensions](SaveProviderExtensions.md):

- When the provider implements `ILongSaveProvider` (as `FileSaveProvider` does), the call goes to its native methods.
- Otherwise the value is stored as an invariant-culture string through `SetString`. `GetLong` reads such strings, and also widens a value stored with `SetInt`.

`ILongSaveProvider` members:

| Member | Description |
|--------|-------------|
| `long GetLong(string key, long defaultValue = 0L)` | Read a 64-bit integer. Implementations must also return values written by `SetInt` or as an invariant integer string. |
| `void SetLong(string key, long value)` | Write a 64-bit integer. |

---

## Events

| Event | Description |
|-------|-------------|
| `event Action OnDataSaved` | A `Save()` call completed (the file provider also raises it when there was nothing new to write). |
| `event Action OnDataLoaded` | Data was loaded from storage. |
| `event Action<string> OnKeyChanged` | A `Set*` call wrote the key. Parameter is the key name. |

---

## Examples

### No-Code (Inspector)
The provider is created via `SaveProviderSettingsComponent` or `SaveProvider` (static facade). In Inspector, choose the provider type (`PlayerPrefs` or `File`).

### Code
```csharp
// Get the current provider
ISaveProvider provider = SaveProvider.CurrentProvider;

// Save progress
provider.SetInt("Score", 1500);
provider.SetLong("WalletCents", 9_000_000_000L);
provider.SetBool("MusicEnabled", false);
provider.Save();

// Load
int score = provider.GetInt("Score", 0);
long cents = provider.GetLong("WalletCents");
bool music = provider.GetBool("MusicEnabled", true);

// Subscribe to changes
provider.OnKeyChanged += key => Debug.Log($"Key changed: {key}");
```

---

## See Also
- [FileSaveProvider](FileSaveProvider.md) — JSON file implementation
- [PlayerPrefsSaveProvider](PlayerPrefsSaveProvider.md) — PlayerPrefs implementation
- [SaveProvider](SaveProvider.md) — static facade
- [SaveProviderExtensions](SaveProviderExtensions.md) — `long` and array extensions
- ← [Save](README.md)
