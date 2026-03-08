# NPC module

NPC navigation, patrol/chase logic, animator driver, and modular RPG-ready combat composition. Scripts are in `Scripts/NPC/`. Detailed component docs are still primarily in Russian.

## Russian docs (per-component)

| Page | Description |
|------|-------------|
| [NPC README](../../Docs/NPC/README.md) | Module overview |
| [NpcAnimatorDriver](../../Docs/NPC/NpcAnimatorDriver.md) | Animator state sync with movement |
| [NPCNavigation](../../Docs/NPC/Navigation/NPCNavigation.md) | Navigation, patrol, chase |
| [NpcRpgCombatBrain](../../Docs/NPC/Combat/NpcRpgCombatBrain.md) | Modular auto-combat brain for melee/ranged NPCs |
| [NpcCombatPreset](../../Docs/NPC/Combat/NpcCombatPreset.md) | Data preset for chase/hold/attack behavior |
| [NpcCombatScenarios](../../Docs/NPC/Combat/NpcCombatScenarios.md) | Ready-made setup scenarios for melee/ranged/patrol combat NPCs |

## Recommended composition

For combat NPCs, the intended stack is:

- `NpcNavigation`
- `NpcAnimatorDriver`
- `RpgCombatant`
- `RpgTargetSelector`
- `RpgAttackController`
- `NpcRpgCombatBrain`
- `RpgAttackPreset` + `NpcCombatPreset`

This keeps enemies assembled from reusable components instead of custom one-off scripts.

## See also

- [Tools/Move](../Tools/Move/README.md)
- [Animations](../Animations/README.md)
