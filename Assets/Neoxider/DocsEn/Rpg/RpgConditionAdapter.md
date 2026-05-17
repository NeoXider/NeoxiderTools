# RpgConditionAdapter

**What it is:** a `MonoBehaviour` adapter from `Scripts/Rpg/Bridge/RpgConditionAdapter.cs` that exposes RPG state as a condition for `NeoCondition` and other systems using `IConditionEvaluator`.

**Navigation:** [← RPG](./README.md)

---

## Evaluation modes

| Mode | Description |
|------|-------------|
| `HpAtLeast` | HP >= `_threshold` |
| `HpPercentAtLeast` | HP percent >= `_threshold` (0–100) |
| `LevelAtLeast` | Level >= `_levelThreshold` |
| `IsDead` | Character is dead |
| `HasBuff` | Has active buff by `_buffId` |
| `HasStatus` | Has active status by `_statusId` |
| `CanPerformActions` | Target can act right now |
| `IsInvulnerable` | Target is currently invulnerable |
| `CanEvade` | `RpgEvadeController` is ready |
| `AttackReady` | `RpgAttackController` can use `_attackId` |
| `ResourceAtLeast` / `ResourceBelow` | Checks the current value of any resource through `_resource` |
| `ResourcePercentAtLeast` / `ResourcePercentBelow` | Checks the percent of any resource through `_resource` (threshold 0-100) |
| `StatAtLeast` / `StatBelow` | Checks any stat value through `_stat` |
| `UpgradePointsAtLeast` | Checks free upgrade points |
| `UpgradeLevelAtLeast` | Checks how many times the selected stat was upgraded |
| `XpAtLeast` | Checks current XP |

## Invert option

When `_invert` is enabled, the evaluation result is inverted.

## Usage

1. Add `RpgConditionAdapter` to an object.
2. Select `Evaluation Mode`.
3. Fill `Threshold`, `Level Threshold`, `Buff Id`, `Status Id`, `Attack Id`, `_resource`, or `_stat`.
4. Use in `NeoCondition` as an `IConditionEvaluator` source, or call `EvaluateCurrent()` from code.
