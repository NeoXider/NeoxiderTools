# Tools / Components / AttackSystem

**Legacy.** For new projects use the [RPG module](../../../Rpg/README.md) (`RpgStatsManager`, buffs, status effects, persistence). Scripts in `Scripts/Tools/Components/AttackSystem/`. Full docs are in Russian.

## Recommended replacement

| Legacy | Replacement |
|--------|-------------|
| Health | [RpgStatsManager](../../../Rpg/README.md) |
| AttackExecution, Evade, AdvancedAttackCollider | RpgStatsManager + [RpgNoCodeAction](../../../Rpg/RpgNoCodeAction.md) + custom attack/evade logic |
| IDamageable compatibility | [RpgStatsDamageableBridge](../../../../Docs/Tools/Components/AttackSystem/RpgStatsDamageableBridge.md) |

## Russian docs (per-component)

| Page | Description |
|------|-------------|
| [AttackSystem README](../../../../Docs/Tools/Components/AttackSystem/README.md) | Overview |
| [RpgStatsDamageableBridge](../../../../Docs/Tools/Components/AttackSystem/RpgStatsDamageableBridge.md) | IDamageable → RpgStatsManager bridge |
| [Health](../../../../Docs/Tools/Components/AttackSystem/Health.md), [Evade](../../../../Docs/Tools/Components/AttackSystem/Evade.md) | Defence *(legacy)* |
| [AttackExecution](../../../../Docs/Tools/Components/AttackSystem/AttackExecution.md), [AdvancedAttackCollider](../../../../Docs/Tools/Components/AttackSystem/AdvancedAttackCollider.md) | Attack *(legacy)* |
