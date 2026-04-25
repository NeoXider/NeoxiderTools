# PlayerPrefsSaveProvider

**Purpose:** An `ISaveProvider` implementation via Unity `PlayerPrefs`. The simplest option — data is stored in the registry (Windows) or preferences file (macOS/Linux). Booleans are stored as `int` (0/1) with a `Bool_` prefix.

---

## API

All `ISaveProvider` methods: `GetInt`, `SetInt`, `GetFloat`, `SetFloat`, `GetString`, `SetString`, `GetBool`, `SetBool`, `HasKey`, `DeleteKey`, `DeleteAll`, `Save`, `Load`.

Specifics:
- `GetBool` / `SetBool` use `PlayerPrefs.GetInt` / `SetInt` with key `"Bool_" + key`.
- `HasKey` checks both the regular key and `"Bool_" + key`.
- `Load()` is a no-op (PlayerPrefs load automatically), but still fires `OnDataLoaded`.

---

## Examples

### No-Code (Inspector)
In `SaveProviderSettingsComponent`, select the **PlayerPrefs** type — the provider is created automatically.

### Code
```csharp
var provider = new PlayerPrefsSaveProvider();

provider.SetString("PlayerName", "Alex");
provider.SetBool("TutorialDone", true);
provider.Save(); // calls PlayerPrefs.Save()

string name = provider.GetString("PlayerName"); // "Alex"
bool tutorial = provider.GetBool("TutorialDone"); // true
```

---

## See Also
- [ISaveProvider](ISaveProvider.md) — interface
- [FileSaveProvider](FileSaveProvider.md) — JSON file alternative
- ← [Save](README.md)
