# ISaveProvider

**Purpose:** A unified API interface for all save backends. Abstracts storage — your code works the same regardless of whether data is saved to PlayerPrefs or a JSON file. Implementations: `PlayerPrefsSaveProvider`, `FileSaveProvider`.

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

---

## Events

| Event | Description |
|-------|-------------|
| `event Action OnDataSaved` | Data was written to disk. |
| `event Action OnDataLoaded` | Data was loaded from storage. |
| `event Action<string> OnKeyChanged` | A key's value changed. Parameter is the key name. |

---

## Examples

### No-Code (Inspector)
The provider is created via `SaveProviderSettingsComponent` or `SaveProvider` (singleton). In Inspector, choose the provider type (`PlayerPrefs` or `File`).

### Code
```csharp
// Get the current provider
ISaveProvider provider = SaveProvider.I;

// Save progress
provider.SetInt("Score", 1500);
provider.SetBool("MusicEnabled", false);
provider.Save();

// Load
int score = provider.GetInt("Score", 0);
bool music = provider.GetBool("MusicEnabled", true);

// Subscribe to changes
provider.OnKeyChanged += key => Debug.Log($"Key changed: {key}");
```

---

## See Also
- [FileSaveProvider](FileSaveProvider.md) — JSON file implementation
- [PlayerPrefsSaveProvider](PlayerPrefsSaveProvider.md) — PlayerPrefs implementation
- [SaveProvider](SaveProvider.md) — singleton wrapper
- [SaveProviderExtensions](SaveProviderExtensions.md) — array extensions
- ← [Save](README.md)
