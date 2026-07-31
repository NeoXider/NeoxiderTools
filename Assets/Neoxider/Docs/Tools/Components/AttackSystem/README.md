# Tools / Components / AttackSystem

**Legacy.** For new projects use the [RPG module](../../../Rpg/README.md) with `RpgCharacter`, `RpgAttackController`, `RpgProjectile`, and `RpgEvadeController`.

## Recommended replacement

| Legacy | Replacement |
|--------|-------------|
| Health | [RpgCharacter](../../../Rpg/RpgCharacter.md) |
| AttackExecution | [RpgAttackController](../../../Rpg/RpgAttackController.md) + [RpgAttackDefinition](../../../Rpg/RpgAttackDefinition.md) |
| Evade | [RpgEvadeController](../../../Rpg/RpgEvadeController.md) |
| AdvancedAttackCollider | [RpgAttackController](../../../Rpg/RpgAttackController.md) + [RpgProjectile](../../../Rpg/RpgProjectile.md) |
| `IDamageable` / `IHealable` compatibility | [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md) |

## Legacy bridge

`RpgStatsDamageableBridge` is the supported compatibility path when old `AttackSystem` components call `IDamageable.TakeDamage(int)` or `IHealable.Heal(int)`, but the target actor already uses `RpgCharacter`.

Typical setup:

1. `RpgCharacter` lives on the actor.
2. `RpgStatsDamageableBridge` lives on the same object or on a child hitbox.
3. The legacy component calls `IDamageable`.
4. The bridge forwards to `RpgCharacter.Damage(...)` or `RpgCharacter.Heal(...)`.

## docs (per-component)

| Page | Description |
|------|-------------|
 · Overview
| [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md) | IDamageable -> RpgCharacter bridge |
| [Health](./Health.md), [Evade](./Evade.md) | Defence *(legacy)* |
| [AttackExecution](./AttackExecution.md), [AdvancedAttackCollider](./AdvancedAttackCollider.md) | Attack *(legacy)* |
