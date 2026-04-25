# FileSaveProvider

**Purpose:** An `ISaveProvider` implementation that stores all data in a JSON file under `Application.persistentDataPath`. Supports multiple save slots (switch via `ChangeSlot`). Auto-saves dirty data on finalization.

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `FileSaveProvider(string fileName = "save.json")` | Constructor. Creates/loads the file from `persistentDataPath`. |
| `void ChangeSlot(string fileName)` | Saves current data (if dirty) and switches to another file. |
| `SaveProviderType ProviderType` | Always `SaveProviderType.File`. |
| `int GetInt(string key, int default)` | Read an integer by key. |
| `void SetInt(string key, int value)` | Write an integer. Fires `OnKeyChanged`. |
| `float GetFloat(string key, float default)` | Read a float. |
| `void SetFloat(string key, float value)` | Write a float. |
| `string GetString(string key, string default)` | Read a string. |
| `void SetString(string key, string value)` | Write a string. |
| `bool GetBool(string key, bool default)` | Read a boolean. |
| `void SetBool(string key, bool value)` | Write a boolean. |
| `bool HasKey(string key)` | Check if a key exists. |
| `void DeleteKey(string key)` | Delete a key. |
| `void DeleteAll()` | Delete all data. |
| `void Save()` | Flush data to disk. Fires `OnDataSaved`. |
| `void Load()` | Reload data from disk. Fires `OnDataLoaded`. |

---

## Events

| Event | When it fires |
|-------|---------------|
| `OnDataSaved` | After successfully writing the file to disk. |
| `OnDataLoaded` | After loading data from the file. |
| `OnKeyChanged(string key)` | After any key's value changes. |

---

## Examples

### No-Code (Inspector)
`FileSaveProvider` is a pure C# class (not a MonoBehaviour). For No-Code, use `SaveProviderSettingsComponent` which creates the appropriate provider automatically.

### Code
```csharp
// Create a provider backed by slot1.json
var save = new FileSaveProvider("slot1.json");

// Write data
save.SetInt("Level", 5);
save.SetString("PlayerName", "Alex");
save.SetBool("TutorialDone", true);
save.Save(); // flush to disk

// Read data
int level = save.GetInt("Level");           // 5
string name = save.GetString("PlayerName"); // "Alex"

// Switch slot (current data is auto-saved if dirty)
save.ChangeSlot("slot2.json");
int otherLevel = save.GetInt("Level"); // data from slot2
```

---

## See Also
- [ISaveProvider](ISaveProvider.md) — the interface this class implements
- [PlayerPrefsSaveProvider](PlayerPrefsSaveProvider.md) — alternative provider via PlayerPrefs
- [SaveProvider](SaveProvider.md) — singleton wrapper
- ← [Save](README.md)
