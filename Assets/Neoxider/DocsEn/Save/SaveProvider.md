# SaveProvider

## Overview
`SaveProvider` is the static facade on top of the active `ISaveProvider`. It exposes a PlayerPrefs-like API while allowing the actual backend to be replaced.

- **Namespace**: `Neo.Save`
- **Path**: `Assets/Neoxider/Scripts/Save/SaveProvider.cs`

## How to use
1. Call `GetInt`, `SetString`, `GetBool`, and similar methods from runtime code.
2. If no custom backend is configured, the system falls back to `PlayerPrefsSaveProvider`.
3. Use `SaveProviderSettingsComponent` or `SetProvider()` to swap the backend.
4. Subscribe to `OnDataSaved`, `OnDataLoaded`, and `OnKeyChanged` when you need notifications.

## What it does
- Initializes lazily on first access.
- Tries to load `SaveProviderSettings` from `Resources`.
- Falls back to `PlayerPrefsSaveProvider` when no settings asset exists.
- Forwards all calls to the active provider.
- Forwards provider events through a stable static event surface.
- Correctly detaches old provider event handlers when `SetProvider()` is used.

## Public API
- `ISaveProvider CurrentProvider`
- `void SetProvider(ISaveProvider provider)`
- `int GetInt(string key, int defaultValue = 0)`
- `void SetInt(string key, int value)`
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

## Events
- `OnDataSaved`
- `OnDataLoaded`
- `OnKeyChanged`

## Example
```csharp
using Neo.Save;

public static class SettingsStorage
{
    public static void SaveVolume(float value)
    {
        SaveProvider.SetFloat("audio.volume", value);
        SaveProvider.Save();
    }

    public static float LoadVolume()
    {
        return SaveProvider.GetFloat("audio.volume", 1f);
    }
}
```

## See also
- [`SaveManager`](./SaveManager.md)
- [`README`](./README.md)
