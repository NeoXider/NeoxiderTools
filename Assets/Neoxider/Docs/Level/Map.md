# Map

**Purpose:** A data class (`[Serializable]`) describing the parameters of a single chapter/map/world in the `LevelManager`. It is not a MonoBehaviour.

## Description

Each `Map` is stored in the `_maps` array of the `LevelManager` component. It handles:
1. Saving the next uncompleted level (`_level`), equivalent to the number of recorded completions, for this map via `SaveProvider`.
2. Controlling map completion logic (looping, infinite, or finite).

## Key Fields

| Field | Description |
|-------|-------------|
| `isInfinity` | If `true`, the map has no end. |
| `countLevels` | The total number of unique levels in this map. |
| `isLoopLevel` | If `true`, after passing `countLevels`, the levels will repeat from the beginning (e.g., 1, 2, 3, 1, 2, 3...). |
| `level` | (Read-only) Absolute progress frontier, starting at zero. It keeps increasing when selection loops. |

## Runtime API

`Load(int mapId, string saveKey)` binds the map to a save namespace and reads its progress. `SaveLevel()` increments and stores progress once; it does not check which level was played. `LevelManager.SaveLevel()` supplies that check. `GetCopmplete()` (legacy spelling retained) returns true for a finite map once `level >= countLevels`; infinite maps remain incomplete.

The existing key format remains `Map_<saveKey>_<mapId>__level`, including the two underscores before `level`. Changing map indices or the prefix changes the save namespace. Call `Load` before using a map independently:

```csharp
Map map = new Map { isInfinity = true, isLoopLevel = true, countLevels = 3 };
map.Load(0, "Campaign");
map.SaveLevel();
int levelToPlay = LevelManager.GetLoopLevel(map.level, map.countLevels);
```

Loaded negative progress is treated as zero; a subsequent completion stores valid progress. At `int.MaxValue`, `SaveLevel()` leaves progress unchanged. `SaveProvider` owns the eventual disk flush.

If the provider throws during `SaveLevel()`, the exception propagates and the map restores its previous in-memory progress so a retry does not skip a level. This is not a disk transaction: provider writes or external save-event side effects cannot be rolled back by `Map`.

## See Also
- [LevelManager](LevelManager.md)
- [Level module](README.md)
