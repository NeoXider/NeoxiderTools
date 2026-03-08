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

## Invert option

When `_invert` is enabled, the evaluation result is inverted.

## Usage

1. Add `RpgConditionAdapter` to an object.
2. Select `Evaluation Mode`.
3. Fill `Threshold`, `Level Threshold`, `Buff Id`, or `Status Id`.
4. Use in `NeoCondition` as an `IConditionEvaluator` source, or call `EvaluateCurrent()` from code.
