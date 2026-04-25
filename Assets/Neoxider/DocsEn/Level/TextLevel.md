# TextLevel

**Purpose:** A UI component that automatically subscribes to the `LevelManager` and displays the current or maximum level in a `TextMeshPro` text field.

## Setup

1. Add `Add Component > Neoxider > Level > TextLevel` to an object with `TextMeshPro`.
2. Select the display mode (`Current` or `Max`).
3. On game start, the text automatically updates and reacts to level changes.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_displayMode` | `Current` — display the currently played level. `Max` — display the maximum unlocked level. |
| `_displayOffset` | How much to offset the number for display (since levels in code start at 0, an offset of `1` displays level "1"). |
| `_levelSource` | Optional reference to a specific `LevelManager` (needed only if there are multiple in the scene). Otherwise, uses the singleton `LevelManager.I`. |

## See Also
- [LevelManager](LevelManager.md)
- [Module Root](../README.md)
