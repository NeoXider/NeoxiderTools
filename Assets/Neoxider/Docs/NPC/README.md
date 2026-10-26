# NPC module

NPC navigation, patrol/chase logic, animator driver, and modular RPG-ready combat composition. Scripts are in `Scripts/NPC/`.

## docs (per-component)

| Page | Description |
|------|-------------|
| [NPCNavigation](./Navigation/NPCNavigation.md) | Navigation, patrol, chase |
| [NpcTargetFinder](./NpcTargetFinder.md) | Auto-binds a scene target (by tag/name) to NpcNavigation |
| [NpcAnimatorDriver](./NpcAnimatorDriver.md) | Animator state sync with movement |
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

## C# entry point

```csharp
using Neo.NPC;
using UnityEngine;

// navigation and player are explicit scene references supplied by the caller.
navigation.SetFollowTarget(player);
navigation.SetMode(NpcNavigation.NavigationMode.FollowTarget);
navigation.SetRunning(true);
navigation.Resume();
```

`SetFollowTarget`, `SetMode`, and `SetRunning` support configuration before the navigation component's
`Awake`; the selected behaviour starts on enable. Path requests need an initialized agent on a NavMesh.
`Stop`, `Resume`, and `IsActive` control movement explicitly; disabling navigation also stops its agent.
For an Inspector workflow, assign the target/route directly or add `NpcTargetFinder`, and connect the
navigation events to presentation. The C# methods remain available without UnityEvent chains.

The navigation cores are ordinary C# classes that adapt Unity transforms and NavMesh agents; they are
not engine-independent simulation classes. See [core contracts](./InternalTypes.md) for direct usage
and [navigation](./Navigation/NPCNavigation.md) for path/failure semantics.

The RPG combat stack above is maintained for existing projects. New combat systems should use
`Neo.Abilities`; NPC navigation can be reused independently of the legacy combat brain.

## See also

- [Tools/Move](../Tools/Move/README.md)
- [Animations](../Animations/README.md)
