# PlayerPrefsSaveProvider

**What it is:** the default `ISaveProvider` implementation (`Scripts/Save/Providers/PlayerPrefsSaveProvider.cs`, namespace `Neo.Save`) backed by Unity `PlayerPrefs` — the registry on Windows, a preferences file on macOS/Linux, platform storage elsewhere. Booleans are stored as `int` (0/1) with a `Bool_` prefix.

**How to use:**
1. Do nothing: `SaveProvider` uses it when no settings asset selects another backend. Or select **PlayerPrefs** in `SaveProviderSettings`.
2. In code, construct it directly: `new PlayerPrefsSaveProvider()`.

---

## API

All `ISaveProvider` methods: `GetInt`, `SetInt`, `GetFloat`, `SetFloat`, `GetString`, `SetString`, `GetBool`, `SetBool`, `HasKey`, `DeleteKey`, `DeleteAll`, `Save`, `Load`.

Specifics:
- `GetBool` / `SetBool` use `PlayerPrefs.GetInt` / `SetInt` with key `"Bool_" + key`.
- `HasKey` checks both the regular key and `"Bool_" + key`.
- `Load()` is a no-op (PlayerPrefs load automatically), but still fires `OnDataLoaded`.
- `Save()` calls `PlayerPrefs.Save()` on every call.
- `GetLong` / `SetLong` come from [SaveProviderExtensions](SaveProviderExtensions.md): the value is stored as an invariant-culture string, and `GetLong` also reads a key written with `SetInt`.

---

## Examples

### No-Code (Inspector)
In `SaveProviderSettingsComponent`, select the **PlayerPrefs** type — the provider is created automatically.

### Code
```csharp
PlayerPrefsSaveProvider provider = new PlayerPrefsSaveProvider();

provider.SetString("PlayerName", "Alex");
provider.SetBool("TutorialDone", true);
provider.SetLong("WalletCents", 12_345_678_901L);
provider.Save(); // calls PlayerPrefs.Save()

string name = provider.GetString("PlayerName"); // "Alex"
bool tutorial = provider.GetBool("TutorialDone"); // true
long cents = provider.GetLong("WalletCents");
```

---

## See Also
- [ISaveProvider](ISaveProvider.md) — interface
- [FileSaveProvider](FileSaveProvider.md) — JSON file alternative
- ← [Save](README.md)
