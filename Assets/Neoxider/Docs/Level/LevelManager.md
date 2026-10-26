# LevelManager

**Purpose:** Level selection and map (chapter) progress controller. `CurrentLevel` is a zero-based selection. `MaxLevel` is the next uncompleted level index / number of recorded completions on the selected map. `SaveLevel()` writes progress through `SaveProvider`; level selection alone does not save or load a scene.

## Setup

1. Add `Add Component > Neoxider > Level > LevelManager` to a global object.
2. Configure the `_maps` array. If your game only has one world, leave one element.
3. Define the `_saveKey`.
4. (Optional) Assign `_parentLevel` — the parent Transform for level buttons. Inspector validation collects buttons once in hierarchy order, including inactive children. The `_lvlBtns` array may be empty for games without a map UI.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_saveKey` | Base prefix for saving progress in `SaveProvider`. |
| `_maps` | Array of world configurations (`Map`). Each stores level count, loop logic, etc. |
| `_currentLevel` | The currently selected level. |
| `_mapId` | The index of the currently active map. |
| `_onAwakeNextLevel` | If `true`, the manager automatically selects the latest uncompleted level on start. |
| `_onAwakeNextMap` | If `true`, select the first incomplete map on start and advance to a later incomplete map after completing the current map. |
| `_parentLevel` | Transform containing the UI level buttons (`LevelButton`). Auto-populates `_lvlBtns`. |

## API

```csharp
// Go to the next level
LevelManager.I.NextLevel();

// Select a zero-based level directly (also available to Inspector UnityEvents)
LevelManager.I.SetLevel(2);

// Complete the current level (saves progress)
LevelManager.I.SaveLevel();

// Jump to the latest uncompleted level
LevelManager.I.SetLastLevel();

// Restart the current level
LevelManager.I.Restart();

// Select a map; then select its saved frontier explicitly if desired
LevelManager.I.SetMapId(1);
LevelManager.I.SetLastLevel();
```

`SetLevel` clamps negative selections to zero on non-looping maps. Finite maps with a positive count clamp to their last level. Infinite maps (and the legacy non-looping zero-count configuration) retain the existing upper limit of `MaxLevel + 1`. Looping maps use `GetLoopLevel(id, count)`, which wraps positive and negative integers into `[0, count)` and returns zero for a non-positive count. Selection and saved progress saturate at `int.MaxValue` rather than overflowing.

`SetLastLevel()` always selects the saved frontier; it does not advance a fresh looping map to level 1. On looping maps, `CurrentLevel` wraps while `MaxLevel` and `GetLastLevelId()` remain absolute progress. For example, after 4 completions with 3 unique levels, `MaxLevel == 4` and `SetLastLevel()` selects level 1. `SaveLevel()` compares the selection with the wrapped frontier, so saving continues after the first loop. Replaying another finite level does not advance progress. Call `SaveLevel()` once per successful run; a one-level loop uses the same selection for every completion.

A looping map requires a positive `countLevels`; without any levels, `SaveLevel()` does not create progress.

`SetLastMap()` chooses the first incomplete configured map, or the last configured map when all are complete. Null slots are skipped without renumbering save keys. `SetMapId` rejects null or out-of-range slots without changing the current selection or firing change events. Switching maps does not automatically change `CurrentLevel`.

Automatic map advancement also skips empty or already completed intermediate maps. Switching to a different map in a progress-event listener is retained.

The public event fields can be assigned from C# when constructing components dynamically. Subscribe before invoking selection/completion methods; scene-authored listeners can be configured in the Inspector. Persistence flush policy belongs to `SaveProvider`; call `SaveProvider.Save()` at an explicit checkpoint if required by the game.

## Events

- `OnChangeLevel` — Fired when the current level changes.
- `OnChangeMaxLevel` — Fired when the player beats a new level.
- `OnChangeMap` — Fired when the active map changes.

## See Also
- [LevelButton](LevelButton.md) - Buttons for the level map UI.
- [SceneFlowController](SceneFlowController.md) - For actual Unity scene loading.
- [Level module](README.md)
