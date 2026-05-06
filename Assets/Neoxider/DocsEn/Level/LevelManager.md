# LevelManager

**Purpose:** Global level and map (chapter) controller. Tracks the current level (`CurrentLevel`), the maximum completed level (`MaxLevel`), and supports multiple maps (worlds). Automatically saves progress via `SaveProvider`.

## Setup

1. Add `Add Component > Neoxider > Level > LevelManager` to a global object.
2. Configure the `_maps` array. If your game only has one world, leave one element.
3. Define the `_saveKey`.
4. (Optional) Assign `_parentLevel` — the parent Transform for level buttons, so the manager can auto-find and setup all `LevelButton`s.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_saveKey` | Base prefix for saving progress in `SaveProvider`. |
| `_maps` | Array of world configurations (`Map`). Each stores level count, loop logic, etc. |
| `_currentLevel` | The currently selected level. |
| `_mapId` | The index of the currently active map. |
| `_onAwakeNextLevel` | If `true`, the manager automatically selects the latest uncompleted level on start. |
| `_onAwakeNextMap` | If `true`, the manager automatically selects the latest uncompleted map on start. |
| `_parentLevel` | Transform containing the UI level buttons (`LevelButton`). Auto-populates `_lvlBtns`. |

## API 

```csharp
// Go to the next level
LevelManager.I.NextLevel();

// Complete the current level (saves progress)
LevelManager.I.SaveLevel();

// Select a specific level
LevelManager.I.SetLevel(5);

// Restart the current level
LevelManager.I.Restart();
```

## Events
- `OnChangeLevel` — Fired when the current level changes.
- `OnChangeMaxLevel` — Fired when the player beats a new level.
- `OnChangeMap` — Fired when the active map changes.

## See Also
- [LevelButton](LevelButton.md) - Buttons for the level map UI.
- [SceneFlowController](SceneFlowController.md) - For actual Unity scene loading.
- [Module Root](../README.md)
