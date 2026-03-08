# Tools / Components / AttackSystem

**Legacy.** For new projects use the [RPG module](../../../Rpg/README.md) with `RpgStatsManager`, `RpgCombatant`, `RpgAttackController`, `RpgProjectile`, and `RpgEvadeController`.

## Recommended replacement

| Legacy | Replacement |
|--------|-------------|
| Health | [RpgStatsManager](../../../Rpg/README.md) or `RpgCombatant` |
| AttackExecution | [RpgAttackController](../../../Rpg/RpgAttackController.md) + [RpgAttackDefinition](../../../Rpg/RpgAttackDefinition.md) |
| Evade | [RpgEvadeController](../../../Rpg/RpgEvadeController.md) |
| AdvancedAttackCollider | [RpgAttackController](../../../Rpg/RpgAttackController.md) + [RpgProjectile](../../../Rpg/RpgProjectile.md) |
| IDamageable compatibility | [RpgStatsDamageableBridge](../../../../Docs/Tools/Components/AttackSystem/RpgStatsDamageableBridge.md) |

## Russian docs (per-component)

| Page | Description |
|------|-------------|
| [AttackSystem README](../../../../Docs/Tools/Components/AttackSystem/README.md) | Overview |
| [RpgStatsDamageableBridge](../../../../Docs/Tools/Components/AttackSystem/RpgStatsDamageableBridge.md) | IDamageable → RpgStatsManager bridge |
| [Health](../../../../Docs/Tools/Components/AttackSystem/Health.md), [Evade](../../../../Docs/Tools/Components/AttackSystem/Evade.md) | Defence *(legacy)* |
| [AttackExecution](../../../../Docs/Tools/Components/AttackSystem/AttackExecution.md), [AdvancedAttackCollider](../../../../Docs/Tools/Components/AttackSystem/AdvancedAttackCollider.md) | Attack *(legacy)* |
