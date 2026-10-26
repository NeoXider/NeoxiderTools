# FileSaveProvider

**Purpose:** An `ISaveProvider` implementation that stores typed values in a JSON file under `Application.persistentDataPath` or a supplied persistence root. Call `Save()` explicitly to write staged changes. Direct provider instances have no finalizer-based save.

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `FileSaveProvider(string fileName = "save.json", FileSaveProviderOptions options = null)` | Loads the selected file; options can specify `PersistenceRoot` and `Encryption`. |
| `void ChangeSlot(string fileName)` | Compatibility entry point; keeps the current slot if flushing pending data fails. |
| `bool TryChangeSlot(string fileName)` | Flushes pending data and switches files only on success. Returns `false` after a failed flush, retaining values for retry. Invalid path arguments can still throw. |
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
| `void Load()` | Explicitly discard staged changes and reload disk data, falling back to the backup. Fires `OnDataLoaded`. |

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
`FileSaveProvider` is a pure C# class. Select **File** in `SaveProviderSettings`, then assign that asset to `SaveProviderSettingsComponent` or place it in Resources as described in the [module entry](README.md). Initialize the provider before scene save consumers. A `SaveManager` can flush registered component state on pause, focus loss, quit, or an explicit `FlushNow()` call.

### Code
```csharp
// Create a provider backed by slot1.json
FileSaveProvider save = new FileSaveProvider("slot1.json");

// Write data
save.SetInt("Level", 5);
save.SetString("PlayerName", "Alex");
save.SetBool("TutorialDone", true);
save.Save(); // flush to disk

// Read data
int level = save.GetInt("Level");           // 5
string name = save.GetString("PlayerName"); // "Alex"

// Switch only after current staged data reaches disk.
if (save.TryChangeSlot("slot2.json"))
{
    int otherLevel = save.GetInt("Level"); // data from slot2
}
```

## Durability and recovery

Saving stages a complete payload in `<file>.tmp`, then replaces the primary file and rotates its previous version into `<file>.bak`. A fallback path supports filesystems without `File.Replace`; device-specific behavior still needs platform validation.

Loading requires a JSON object with exactly one root `items` member whose value is an array. Missing/null/non-array or duplicate root `items` members are rejected before typed deserialization; a nested `items` member or text containing that name does not satisfy this requirement. Unknown root metadata can coexist with the required array. This check applies both to plaintext JSON and to decrypted file payloads: Unity can materialize a missing/null collection as an empty list, so the resulting DTO alone cannot prove that the file contained an explicit array.

A valid empty profile is `{"items":[]}` and intentionally stays empty instead of restoring an older backup. Loading also rejects null records, null value containers, and missing type tags. Structurally damaged primary files fall back to the complete backup without exposing partial records or rewriting either file during loading. The first save after recovery preserves that known-good backup instead of replacing it with the damaged primary. Normal backup rotation resumes after a successful save. Empty string keys are retained across disk round trips; null keys are invalid. Existing JSON field names and AES/Base64 encoding are unchanged.

Для пустого профиля нужен явный корневой массив `items: []`. Отсутствующий, `null` или вложенный `items` не считается пустым профилем: загрузчик восстанавливает целый backup, если он доступен.

`OnDataSaved` reports a completed commit. A rejected slot switch does not fire `OnDataLoaded`. File I/O failures and backup recovery use the existing critical error channel, including when debug logging is disabled. Slot switching changes provider data; explicitly restore scene components when required, and refresh any project-owned caches.

---

## See Also
- [ISaveProvider](ISaveProvider.md) — the interface this class implements
- [PlayerPrefsSaveProvider](PlayerPrefsSaveProvider.md) — alternative provider via PlayerPrefs
- [SaveProvider](SaveProvider.md) — singleton wrapper
- ← [Save](README.md)
