# Map

**What it is:** a `[Serializable]` data class (`Scripts/Level/Map.cs`, namespace `Neo.Level`) describing one chapter/map/world of a `LevelManager`. It is not a MonoBehaviour.

**How to use:**
1. Configure entries in the `_maps` array of `LevelManager` (level count, infinite/looping rules).
2. Read progress through `LevelManager.Map`, `map.level` and `map.IsComplete`, or use a `Map` on its own after calling `Load`.

---

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

`Load(int mapId, string saveKey)` binds the map to a save namespace and reads its progress. `SaveLevel()` increments and stores progress once; it does not check which level was played. `LevelManager.SaveLevel()` supplies that check. `IsComplete` is true for a finite map once `level >= countLevels`; infinite maps remain incomplete. The misspelled `GetCopmplete()` still works and is marked `[Obsolete]`; use `IsComplete`.

The existing key format remains `Map_<saveKey>_<mapId>__level`, including the two underscores before `level`. Changing map indices or the prefix changes the save namespace. Call `Load` before using a map independently:

```csharp
Map map = new Map { isInfinity = true, isLoopLevel = true, countLevels = 3 };
map.Load(0, "Campaign");
map.SaveLevel();
int levelToPlay = LevelManager.GetLoopLevel(map.level, map.countLevels);
bool finished = map.IsComplete; // always false for an infinite map
```

`ResetProgress()` starts the map over: it deletes the saved key and sets `level` back to 0 (a map that was never bound by `Load` only resets memory, because it has no save key; `Load` with a `null` save key binds the same key as an empty one, `Map__{id}__level`, so that key is deleted too). `LevelManager.TryResetProgress(mapId)` wraps it, binds a map that `Init` has not bound yet, and refreshes the selection UI.

Loaded negative progress is treated as zero; a subsequent completion stores valid progress. At `int.MaxValue`, `SaveLevel()` leaves progress unchanged. `SaveProvider` owns the eventual disk flush.

If the provider throws during `SaveLevel()`, the exception propagates and the map restores its previous in-memory progress so a retry does not skip a level. This is not a disk transaction: provider writes or external save-event side effects cannot be rolled back by `Map`.

## See Also
- [LevelManager](LevelManager.md)
- [Level module](README.md)
