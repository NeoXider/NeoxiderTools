# Map

**Purpose:** A data class (`[Serializable]`) describing the parameters of a single chapter/map/world in the `LevelManager`. It is not a MonoBehaviour.

## Description

Each `Map` is stored in the `_maps` array of the `LevelManager` component. It handles:
1. Saving the maximum completed level (`_level`) for this specific map via `SaveProvider`.
2. Controlling map completion logic (looping, infinite, or finite).

## Key Fields

| Field | Description |
|-------|-------------|
| `isInfinity` | If `true`, the map has no end. |
| `countLevels` | The total number of unique levels in this map. |
| `isLoopLevel` | If `true`, after passing `countLevels`, the levels will repeat from the beginning (e.g., 1, 2, 3, 1, 2, 3...). |
| `level` | (Read-only) The maximum reached level on this map. |

## See Also
- [LevelManager](LevelManager.md)
- [Module Root](../README.md)
