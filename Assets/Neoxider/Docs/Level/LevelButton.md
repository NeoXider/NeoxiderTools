# LevelButton

**Purpose:** UI button for the level map. Automatically changes its visual state (Locked, Available, Passed) based on player progression, and notifies the `LevelManager` when clicked.

## Setup

1. Create a level button prefab (UI Button).
2. Add the `LevelButton` script.
3. Configure the `_closes` array (objects shown when locked) and `_opens` array (objects shown when unlocked).
4. If `LevelManager` has a reference to the parent object (`_parentLevel`), it will auto-find and initialize this button.

The UI `Button` is resolved on `Start` if it was not serialized, so a component created at runtime also binds its click handler. That handler is removed on destruction. Visual arrays and their individual entries are optional; omitted references do not prevent state/event updates. A click on a locked button or a button without a live manager is ignored.

## Runtime API

```csharp
button.SetLevelManager(levelManager);
button.SetVisual(1, 2); // Current frontier, zero-based level 2 (label "3").
button.Click();         // Selects level 2 through LevelManager.SetLevel.
```

`SetVisual(int idVisual, int level)` sets `activ`, the assigned level, optional icons/text, and invokes visual events. IDs are **0 = locked, 1 = current frontier, 2 = passed**. Both current and passed levels can be selected. `Click()` selects a level; use the manager's `OnChangeLevel` for gameplay or scene loading.

The optional label computes the one-based display number using widened integer arithmetic. In 11.0, `SetVisual(1, int.MaxValue)` displays `2147483648`; `level` and visual event payloads retain their existing `int` values. This presentation correction does not change level selection or unlock rules.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_button` | Standard UI `Button` component. |
| `_closes` | Array of `GameObject`s active when locked (e.g., padlock icon, gray background). |
| `_opens` | Array of `GameObject`s active when available or passed. |
| `_textLvl` | Optional text component displaying `level + 1`, with the addition performed as `long`. |
| `activ`, `level` | (Info) Current button state and assigned level index. |

## Events

- `OnChangeVisual(int idVisual)` — Returns status: 0 (locked), 1 (current/available), 2 (passed).
- `OnDisableVisual` — Fired when the level is locked.
- `OnEnableVisual` — Fired when the level is passed.
- `OnCurrentVisual` — Fired for the current, newly unlocked level.

## See Also
- [LevelManager](LevelManager.md)
- [Level module](README.md)
