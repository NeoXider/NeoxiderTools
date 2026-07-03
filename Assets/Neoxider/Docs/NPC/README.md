# NPC module

NPC navigation, patrol/chase logic, animator driver, and modular RPG-ready combat composition. Scripts are in `Scripts/NPC/`.

## docs (per-component)

| Page | Description |
|------|-------------|
 · Module overview
| [NpcAnimatorDriver](./NpcAnimatorDriver.md) | Animator state sync with movement |
| [NPCNavigation](./Navigation/NPCNavigation.md) | Navigation, patrol, chase |
| [NpcRpgCombatBrain](./Combat/NpcRpgCombatBrain.md) | Modular auto-combat brain for melee/ranged NPCs |
| [NpcCombatPreset](./Combat/NpcCombatPreset.md) | Data preset for chase/hold/attack behavior |
| [NpcCombatScenarios](./Combat/NpcCombatScenarios.md) | Ready-made setup scenarios for melee/ranged/patrol combat NPCs |

## Recommended composition

For combat NPCs, the intended stack is:

- `NpcNavigation`
- `NpcAnimatorDriver`
- `RpgCharacter`
- `RpgTargetSelector`
- `RpgAttackController`
- `NpcRpgCombatBrain`
- `RpgAttackPreset` + `NpcCombatPreset`

This keeps enemies assembled from reusable components instead of custom one-off scripts.

## See also

- [Tools/Move](../Tools/Move/README.md)
- [Animations](../Animations/README.md)
