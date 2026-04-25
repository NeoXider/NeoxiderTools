# LevelButton

**Purpose:** UI button for the level map. Automatically changes its visual state (Locked, Available, Passed) based on player progression, and notifies the `LevelManager` when clicked.

## Setup

1. Create a level button prefab (UI Button).
2. Add the `LevelButton` script.
3. Configure the `_closes` array (objects shown when locked) and `_opens` array (objects shown when unlocked).
4. If `LevelManager` has a reference to the parent object (`_parentLevel`), it will auto-find and initialize this button.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_button` | Standard UI `Button` component. |
| `_closes` | Array of `GameObject`s active when locked (e.g., padlock icon, gray background). |
| `_opens` | Array of `GameObject`s active when available or passed. |
| `_textLvl` | Text component to display the level number (will be set to `level + 1`). |
| `activ`, `level` | (Info) Current button state and assigned level index. |

## Events
- `OnChangeVisual(int idVisual)` — Returns status: 0 (locked), 1 (current/available), 2 (passed).
- `OnDisableVisual` — Fired when the level is locked.
- `OnEnableVisual` — Fired when the level is passed.
- `OnCurrentVisual` — Fired for the current, newly unlocked level.

## See Also
- [LevelManager](LevelManager.md)
- [Module Root](../README.md)
