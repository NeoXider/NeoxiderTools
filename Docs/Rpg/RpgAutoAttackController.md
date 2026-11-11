# RpgAutoAttackController

**What it is:** a `MonoBehaviour` (`Scripts/Rpg/Components/RpgAutoAttackController.cs`) that calls
`RpgAttackController.UsePrimaryAttack()` on an interval while a target is within range. It does not turn
or move the actor; the primary attack fires in the controller's forward direction.

**How to use:**
1. Add it next to an `RpgAttackController` (required component) and disable the controller's built-in input.
2. Set `targetTag`, `attackRange`, `attackInterval` and `targetFindInterval` in the Inspector, or assign the
   target from code with `SetTarget(Transform)`.
3. For AI that chooses a specific target, call `RpgAttackController.TryUsePreset(preset, target, out reason)`
   directly instead.

---

## Fields

| Field | Purpose |
|-------|---------|
| `targetTag` | Tag searched with `FindGameObjectWithTag` when no valid target is cached. |
| `attackRange` | Distance at which attacks are attempted (min 1). |
| `attackInterval` | Minimum seconds between attempts, successful or not (min 0.1). |
| `targetFindInterval` | Seconds between searches while the target is missing or inactive. |

## Methods

| Member | Description |
|--------|-------------|
| `SetTarget(Transform)` | Assigns the target directly; the tag search resumes only after it is destroyed or deactivated. |
| `SetTargetTag(string)` | Changes the search tag. |
| `SetAttackRange(float)` / `AttackRange` | Attack distance. |
| `SetAttackInterval(float)` / `AttackInterval` | Attempt interval. |
| `Target` | Current target transform. |

A missing or inactive cached target suspends attempts until a valid target is found, so resources are not
spent on a pooled/deactivated actor while the next search is pending.

## See Also

- [RpgAttackController](RpgAttackController.md)
- [Module Root](README.md)
