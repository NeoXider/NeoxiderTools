# SaveProvider

**What it is:** the static facade over the active `ISaveProvider` (`Scripts/Save/SaveProvider.cs`, namespace `Neo.Save`). It exposes a PlayerPrefs-like API while allowing the actual backend to be replaced.

**How to use:**
1. Call `GetInt`, `SetLong`, `SetString`, `GetBool`, and similar methods from runtime code.
2. If no custom backend is configured, the system falls back to `PlayerPrefsSaveProvider`.
3. Use `SaveProviderSettingsComponent` or `SetProvider()` to swap the backend.
4. Subscribe to `OnDataSaved`, `OnDataLoaded`, and `OnKeyChanged` when you need notifications.

---

## What it does
- Initializes lazily on first access.
- Tries to load `SaveProviderSettings` from `Resources`.
- Falls back to `PlayerPrefsSaveProvider` when no settings asset exists.
- A scene component without assigned settings retains an active provider or uses normal Resources/PlayerPrefs initialization.
- Invalid required encryption in a Resources settings asset throws `InvalidOperationException` before opening file storage. Initialization remains incomplete, so the next call retries after the asset is corrected.
- Invalid assigned scene settings retain the previous provider. Use `SaveProviderSettings.TryCreateProvider()` for an explicit validation result before calling `SetProvider()`.
- Forwards all calls to the active provider.
- Forwards provider events through a stable static event surface.
- Correctly detaches old provider event handlers when `SetProvider()` is used.
- Flushes the active provider on focus loss and on `Application.quitting`, also without a `SaveManager`. The file provider skips these writes when nothing changed.

In 11.0, invalid enabled encryption no longer opens a plaintext backend. The [settings guide](./Settings/SaveProviderSettings.md) describes key/IV validation and retry. Validation checks configuration, rather than proving decryption or write success. Existing valid settings and file formats are unchanged.

## Public API
- `ISaveProvider CurrentProvider`
- `void SetProvider(ISaveProvider provider)`
- `bool DebugLoggingEnabled` — informational/warning logs; data-loss failures are always logged
- `int GetInt(string key, int defaultValue = 0)`
- `void SetInt(string key, int value)`
- `long GetLong(string key, long defaultValue = 0L)` — also reads keys written with `SetInt` or as an invariant integer string
- `void SetLong(string key, long value)` — native on `FileSaveProvider`, an invariant string on other providers
- `float GetFloat(string key, float defaultValue = 0f)`
- `void SetFloat(string key, float value)`
- `string GetString(string key, string defaultValue = "")`
- `void SetString(string key, string value)`
- `bool GetBool(string key, bool defaultValue = false)`
- `void SetBool(string key, bool value)`
- `bool HasKey(string key)`
- `void DeleteKey(string key)`
- `void DeleteAll()`
- `void Save()`
- `void Load()`
- `void SetSlot(string slotName)` — switches the active file slot only after pending values are saved; a failed flush leaves the original slot and pending values intact. Use `FileSaveProvider.TryChangeSlot()` when a Boolean result is needed.

## Events
- `OnDataSaved`
- `OnDataLoaded`
- `OnKeyChanged`

## Example
```csharp
using Neo.Save;

public static class WalletStorage
{
    private const string CentsKey = "wallet.cents";

    public static void SaveCents(long cents)
    {
        SaveProvider.SetLong(CentsKey, cents);
        SaveProvider.Save();
    }

    public static long LoadCents()
    {
        return SaveProvider.GetLong(CentsKey, 0L);
    }
}
```

## See also
- [`SaveManager`](./SaveManager.md)
- [`SaveProviderExtensions`](./SaveProviderExtensions.md)
- [`SaveProviderSettings`](./Settings/SaveProviderSettings.md)
- [`SaveProviderSettingsComponent`](./SaveProviderSettingsComponent.md)
- [`README`](./README.md)
