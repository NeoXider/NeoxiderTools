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
| `GetXpToNextLevel(int totalXp)` | int | Remaining XP to the next defined level; negative input is treated as zero XP. |
| `ValidateDefinition()` | IReadOnlyList&lt;string&gt; | Reports ascending-order and duplicate-level issues. |

Configure the track before manager initialization and validate it before runtime use. Keep level numbers unique and ascending, and legacy XP thresholds nonnegative and ascending. `OnValidate` sorts entries in the Editor; helper calculations assume that valid ordering. Runtime reward dispatch uses the resolved Core level, not these XP thresholds.

Level grants saturate the available perk point balance at `int.MaxValue`. Entries at or below the provider's level on initialization are treated as already reached; ordinary initialization does not replay their rewards. `ActivatePremium()` replays premium entries through the level captured when activation begins, while levels reached by replayed XP use normal level reward dispatch once.

Reloading the same active Progression key preserves its session reward watermark, including after a Core downgrade. Explicit Progression reset clears that history; loading a different key establishes a baseline at the incoming Core level. Provider mirror updates alone neither grant nor mark level rewards. These session rules do not add a persistent per-reward claim ledger to V2 saves.

Keep rewards independent of scene object references inside this asset. Use reusable quest/item assets and the existing reward service fallbacks for scene targets.

## See Also

- [ProgressionManager](../ProgressionManager.md)
- [Module Root](../README.md)
