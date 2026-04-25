# GlobalSave

**Purpose:** A static class for a single global data object (`GlobalData`). Storage via `SaveProvider` (default PlayerPrefs). Lazy-loads on first access to `data`; auto-saves on assignment. Namespace: `Neo.Save`.

## API

| Method / Property | Description |
|-------------------|-------------|
| `GlobalData data { get; set; }` | Access to the global data. Lazy-loads on first read; auto-saves on write. |
| `bool IsReady { get; }` | Whether data has been loaded from storage. |
| `void LoadingData()` | Force-load data from storage. |
| `void SaveProgress()` | Force-save the current `GlobalData` to storage. |

## Example
```csharp
// Read coins
int coins = GlobalSave.data.coins;

// Modify and save
GlobalSave.data.coins += 50;
GlobalSave.SaveProgress();
```

## See Also
- [GlobalData](GlobalData.md)
- [SaveProvider](SaveProvider.md)
- ← [Save](README.md)
