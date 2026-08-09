# Tools / Components

Reusable components: counters, Animator driver, score, typewriter, loot, lifecycle events, and Interface.
Scripts live in `Scripts/Tools/Components/`. Legacy AttackSystem documentation remains indexed here for
discoverability, but its compatibility sources now live in `Scripts/Rpg/Combat/` and compile in the
optional `Neo.Rpg.Combat` assembly.

## Main components (docs)

| Component | Description |
|-----------|-------------|
| [Counter](./Counter.md) | Universal counter (int/float), events, optional save |
| [ScoreManager](./ScoreManager.md) | Score source for UI |
| [AnimatorParameterDriver](./AnimatorParameterDriver.md) | Drive Animator params from code/UnityEvent |
| [TypewriterEffect](./TypewriterEffect.md) | Typewriter text effect |
| [Loot](./Loot.md) | Loot/drop logic |
| [UnityLifecycleEvents](./UnityLifecycleEvents.md) | Lifecycle as UnityEvents |

## Submodules

- [AttackSystem](AttackSystem/README.md) — legacy Health, Evade, AttackExecution, and
  AdvancedAttackCollider; sources in `Scripts/Rpg/Combat/`.
- [Interface](Interface/README.md) — InterfaceAttack and related.

## See also

- [Condition](../../Condition/README.md)
- [Tools/Time](../Time/README.md)
- [Save](../../Save/README.md)
