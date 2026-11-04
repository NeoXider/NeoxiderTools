# FileSaveProvider

**What it is:** an `ISaveProvider` / `ILongSaveProvider` implementation (`Scripts/Save/Providers/FileSaveProvider.cs`, namespace `Neo.Save`) that keeps typed values in memory and writes them to one JSON file under `Application.persistentDataPath` or a supplied persistence root, optionally AES-encrypted. Direct provider instances have no finalizer-based save.

**How to use:**
1. Select **File** in a `SaveProviderSettings` asset (Inspector path), or construct it in code: `new FileSaveProvider("slot1.json")`.
2. Write values with `Set*`; they are staged in memory.
3. Call `Save()` at checkpoints. `SaveProvider` and `SaveManager` also flush on focus loss, pause and quit.

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `FileSaveProvider(string fileName = "save.json", FileSaveProviderOptions options = null)` | Loads the selected file; options can specify `PersistenceRoot` and `Encryption`. |
| `string FilePath` | Full path of the active slot file. |
| `bool HasUnsavedChanges` | Whether values changed since the last successful `Save()` or `Load()`. |
| `void ChangeSlot(string fileName)` | Compatibility entry point; keeps the current slot if flushing pending data fails. |
| `bool TryChangeSlot(string fileName)` | Flushes pending data and switches files only on success. Returns `false` for an empty file name or after a failed flush, retaining values for retry. Invalid path characters can still throw. |
| `SaveProviderType ProviderType` | Always `SaveProviderType.File`. |
| `int GetInt(string key, int default)` | Read an integer by key. |
| `void SetInt(string key, int value)` | Write an integer. |
| `long GetLong(string key, long default)` | Read a 64-bit integer. Entries written by `SetInt`, or as an integer string by `SetString`, are widened. |
| `void SetLong(string key, long value)` | Write a 64-bit integer (type tag `long`). |
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

Reads are type-strict: `GetInt` returns the default for an entry written by `SetLong`, `SetFloat` or `SetString`. Numbers are stored in the invariant culture; integer values that older builds wrote with the device culture still load.

---

## Events

| Event | When it fires |
|-------|---------------|
| `OnDataSaved` | After a `Save()` call completes: after the commit, or immediately when there was nothing new to write. |
| `OnDataLoaded` | After loading data from the file. |
| `OnKeyChanged(string key)` | After every `Set*` call for the key, including a write of the value it already holds. |

---

## Write skipping

Writing the value a key already holds (same type and text) does not mark the provider dirty. `Save()` with no pending change skips the disk write and the backup rotation, unless:

- the file was removed externally;
- the data was recovered from the backup (the damaged primary is rewritten);
- a plaintext file was loaded while encryption is enabled (it is rewritten encrypted).

This keeps the overlapping flushes of `SaveManager`, the `SaveProvider` focus/quit hooks and settings panels from rewriting an unchanged file. `OnDataSaved` is still raised, so save indicators keep working. Serialization reuses its wrapper objects instead of building a new key/value list on every flush; the JSON text, the file write and optional encryption still allocate.

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
save.SetLong("WalletCents", 12_345_678_901L);
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

A valid empty profile is `{"items":[]}` and intentionally stays empty instead of restoring an older backup. Loading also rejects null records, null value containers, and missing type tags. Structurally damaged primary files fall back to the complete backup without exposing partial records or rewriting either file during loading. The first save after recovery preserves that known-good backup instead of replacing it with the damaged primary. Normal backup rotation resumes after a successful save. Empty string keys are retained across disk round trips; null keys are invalid. Existing JSON field names and AES/Base64 encoding are unchanged; the `long` type tag is new, and older package versions keep such entries when they rewrite the file.

`OnDataSaved` reports a completed `Save()` call. A rejected slot switch does not fire `OnDataLoaded`. File I/O failures, encryption failures and backup recovery use the critical error channel, including when debug logging is disabled. Slot switching changes provider data; explicitly restore scene components when required, and refresh any project-owned caches.

---

## See Also
- [ISaveProvider](ISaveProvider.md) — the interface this class implements
- [PlayerPrefsSaveProvider](PlayerPrefsSaveProvider.md) — alternative provider via PlayerPrefs
- [SaveProvider](SaveProvider.md) — static facade
- [SaveFileEncryption](SaveFileEncryption.md) — encryption helpers
- ← [Save](README.md)
