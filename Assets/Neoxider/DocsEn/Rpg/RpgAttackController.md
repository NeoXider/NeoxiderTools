# RpgAttackController

**What it is:** a unified runtime attack caster for melee, ranged, and area attacks driven by `RpgAttackDefinition`.

**Navigation:** [← RPG](./README.md)

---

## Supported delivery types

| Mode | Runtime behavior |
|------|------------------|
| `Direct` | Raycast / SphereCast / CircleCast forward |
| `Area` | OverlapSphere / OverlapCircle at the impact point |
| `Projectile` | Spawns `RpgProjectile` with the same definition |

## API

- `UsePrimaryAttack()`
- `UsePrimaryPreset()`
- `TryUseAttack(...)`
- `TryUsePreset(...)`
- `GetRemainingCooldown(...)`

## Built-in input

- Enabled by default.
- Primary attack uses left mouse button by default.
- Binding can use either `MouseButton` or `KeyCode`.
- Disable `Enable Built-in Input` for NPC and AI actors.

## AI / skills / spells

Use `RpgAttackPreset` together with `RpgTargetSelector` when the controller should resolve targets automatically instead of relying only on forward casts.

## Runtime contracts

- When `RpgAttackPreset.RequireTarget` is enabled, target resolution happens before `CostAmount` is spent, so a failed target lookup does not consume resources.
- Direct and area attacks resolve targets through `IRpgCombatReceiver` and deduplicate hits by receiver within one physics query, even when a target has multiple child colliders.
