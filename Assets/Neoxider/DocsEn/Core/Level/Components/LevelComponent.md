# LevelComponent

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `1` | 1. |
| `HasMaxLevel` | Has Max Level. |
| `Level` | Level. |
| `LevelCurveDefinition` | Level Curve Definition. |
| `LevelState` | Level State. |
| `LevelStateValue` | Level State Value. |
| `MaxLevel` | Max Level. |
| `OnLevelUp` | On Level Up. |
| `OnProfileLoaded` | On Profile Loaded. |
| `OnProfileSaved` | On Profile Saved. |
| `OnXpGained` | On Xp Gained. |
| `TotalXp` | Total Xp. |
| `UseXp` | Use Xp. |
| `XpState` | Xp State. |
| `XpStateValue` | Xp State Value. |
| `XpToNextLevel` | Xp To Next Level. |
| `XpToNextLevelState` | Xp To Next Level State. |
| `XpToNextLevelStateValue` | Xp To Next Level State Value. |
| `_hasMaxLevel` | Has Max Level. |
| `_levelCurve` | Level Curve. |
| `_maxLevel` | Max Level. |
| `_onLevelUp` | On Level Up. |
| `_onProfileLoaded` | On Profile Loaded. |
| `_onProfileSaved` | On Profile Saved. |
| `_onXpGained` | On Xp Gained. |
| `_saveKey` | Save Key. |
| `_startXp` | Start Xp. |
| `true` | True. |

## Runtime Contract

- `AddXp(amount)` increases total XP, evaluates the level through `LevelCurveDefinition`, and raises `OnLevelUp` only when the level actually changes.
- `SetLevel(level)` with `UseXp = true` synchronizes `TotalXp` to the minimum XP required for that level so curve recomputation does not roll the level back.
- `SetLevel(level)` with `UseXp = false` directly sets the level without XP progression.
- `LevelState`, `XpState`, and `XpToNextLevelState` are kept in sync for UI and NoCode bindings.
- `LevelNoCodeAction` uses the same runtime API: its `AddXp` action raises level-up only on a real level change.

## See Also

- [Module Root](../../README.md)
