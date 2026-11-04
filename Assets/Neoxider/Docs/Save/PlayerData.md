# PlayerData

**What it is:** a sample `ISaveableComponent` (`Scripts/Save/Example/PlayerData.cs`, namespace `Neo.Save.Examples`) that saves a score, a position and money with `[SaveField]` and moves the object to the loaded position in `OnDataLoaded()`.

**How to use:** add it to a player object in a scene with a `SaveManager`; copy the pattern for your own components.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `IsLoad` | True after `OnDataLoaded()` applied the saved data. |
| `_money` | Saved under the key `Money`. |
| `playerPosition` | Saved position, applied to the transform on load. |
| `playerScore` | Saved score. |

## See Also

- [Save module](README.md)
- [SaveManager](SaveManager.md)