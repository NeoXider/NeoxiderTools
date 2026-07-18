# LevelCurveDefinition (Level Reward Track)

**What it is:** a `ScriptableObject` from `Scripts/Progression/Data/LevelCurveDefinition.cs` that stores the per-level reward table for the progression system — granted perk points and rewards. The XP-to-level curve itself is owned by the Core `Neo.Core.Level.LevelCurveDefinition` on the `LevelComponent`; this asset only maps a resolved level to its rewards.

## Setup

- Create via **Create → Neoxider → Progression → Level Reward Track**.
- Assign it to `ProgressionManager`'s Level Curve field.

## Level entries (`ProgressionLevelDefinition`)

| Field | Description |
|-------|-------------|
| `Level` | The level this entry rewards. |
| `RequiredXp` | Cumulative XP for the entry (used by the helper `EvaluateLevel`/`GetXpToNextLevel`; runtime level comes from the Core curve). |
| `GrantedPerkPoints` | Perk points granted once when this level is reached. |
| `Rewards` | `ProgressionReward` list dispatched once when this level is reached. |

## API

| Method | Returns | Description |
|--------|---------|-------------|
| `TryGetDefinition(int level, out ProgressionLevelDefinition)` | bool | Looks up the entry for a level (used on level-up to grant rewards). |
| `EvaluateLevel(int totalXp)` | int | Highest reachable level for the XP (prefer the Core `LevelComponent` at runtime). |
| `GetXpToNextLevel(int totalXp)` | int | Remaining XP to the next defined level. |
| `ValidateDefinition()` | IReadOnlyList&lt;string&gt; | Reports ascending-order and duplicate-level issues. |

## See Also

- [ProgressionManager](../ProgressionManager.md)
- [Module Root](../README.md)
