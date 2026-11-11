# GlobalSave

**What it is:** a static holder of one global data object (`GlobalData`) stored as JSON under the `SavesData` key of the active `SaveProvider` (`Scripts/Save/GlobalSave/GlobalSave.cs`, namespace `Neo.Save`).

**How to use:**
1. Add the fields you need to `GlobalData`.
2. Read `GlobalSave.data` anywhere; the first read loads it (a fresh object on first launch or corrupt JSON).
3. After changing fields call `GlobalSave.SaveProgress()` to stage them, then `SaveProvider.Save()` (or let the focus/quit flush do it).

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `GlobalData data { get; set; }` | The global data. Lazy-loads on first read; assigning stages the new object immediately. Assigning `null` resets to a fresh `GlobalData`. |
| `bool IsReady { get; set; }` | Whether data has been read from storage this session. |
| `void LoadingData()` | Force-load data from storage, replacing the in-memory object. |
| `void SaveProgress()` | Stage the current `GlobalData` in the provider. Does nothing while the data was never read or assigned, so an early quit/checkpoint handler cannot erase the stored profile. |

Static state is reset on `SubsystemRegistration`, so the object is re-read in each play session with domain reload disabled.

## Example
```csharp
// Read coins
int coins = GlobalSave.data.coins;

// Modify and save
GlobalSave.data.coins += 50;
GlobalSave.SaveProgress();
SaveProvider.Save();
```

## See Also
- [GlobalData](GlobalData.md)
- [SaveProvider](SaveProvider.md)
- ← [Save](README.md)
