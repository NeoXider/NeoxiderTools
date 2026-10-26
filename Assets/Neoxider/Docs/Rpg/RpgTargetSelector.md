# RpgTargetSelector

**What it is:** a reusable `MonoBehaviour` target selector for AI, skill logic, and spell casting.

**Navigation:** [← RPG](./README.md)

---

## Supports

- `Nearest`
- `Farthest`
- `LowestCurrentHp`
- `HighestCurrentHp`
- `LowestHpPercent`
- `HighestLevel`
- `Random`

## Inspector testing

- `SelectTarget()` has `[Button]`
- `ClearTarget()` has `[Button]`

## Runtime behavior

`SelectTarget()` returns the selected collider's GameObject; `TrySelectTarget(out target)` returns
whether selection succeeded. Candidates use `IRpgCombatReceiver`, including custom implementations.
With `Ignore Self`, all colliders resolving to the source receiver are excluded, including child
hurtboxes. Multiple colliders resolving to the same receiver contribute only one candidate.

Assign `Character Source` when the query origin is outside the character hierarchy. In scene workflows,
connect the selected-target event to the consuming AI/skill view, or assign the selector on an attack
controller and use a preset. `ClearTarget()` releases the selection and resets `HasTargetState`.
