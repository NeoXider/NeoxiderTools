# NeoxiderTools Docs

Canonical English entry point for **NeoxiderTools** `v9.2.2`.

## Start here

- [Package README](../README.md)
- [Russian docs](../Docs/README.md)
- [Project summary](../PROJECT_SUMMARY.md)
- [Useful components](../Docs/UsefulComponents.md)
- [Package compatibility](./PackageCompatibility.md)
- [Sample scenes](./Samples.md)

## Do not rebuild first

| Task | Existing blocks | Entry |
|------|-----------------|-------|
| Grid field, coordinates, shape masks, pathfinding | `FieldGenerator`, `GridShapeMask`, `FieldDebugDrawer` | [GridSystem](./GridSystem/README.md) |
| Multi-cell pieces/items and drag/drop placement | `GridPlacementEntry`, `GridPlacementResult`, `PlaceContentFootprint` | [FieldGenerator](./GridSystem/FieldGenerator.md) |
| Connected equal-item merge | `Neo.Merge`, `MergeResolver`, `GridMergeRequest.Increment(...)` | [Merge](./Merge/README.md) |
| Dice Merge, 2048-like, Match3, TicTacToe | `DiceBoardService`, `SlidingMergeBoardService`, `Match3BoardService`, `TicTacToeBoardService` | [GridSystem](./GridSystem/README.md) |
| Money, shop, multi-currency | `Money`, `IMoneySpend`, `Shop`, `TextMoney` | [Shop](./Shop/README.md) |
| Scene/global save | `SaveManager`, `SaveProvider`, `GlobalSave`, `SaveableBehaviour` | [Save](./Save/README.md) |
| HP/Mana/resources, XP/levels | `HealthComponent`, `ResourcePoolModel`, `LevelComponent`, `LevelCurveDefinition` | [Core](./Core/README.md) |
| RPG combat, projectiles, buffs/statuses | `RpgCharacter`, `RpgAttackController`, `RpgProjectile` | [Rpg](./Rpg/README.md) |
| Reward fly animation between world/canvas points | `AnimationFly.Play(AnimationFlyRequest)`, sprite/prefab visuals, reward timing | [AnimationFly](./UI/AnimationFly.md) |
| NoCode conditions, actions, state transitions | `NeoCondition`, `ConditionEntryPredicate`, module NoCode bridges | [Condition](./Condition/README.md) |
| Movement, free-fly camera, pooling, timers, input | `Tools/Move`, `Tools/Spawner`, `Tools/Time`, `Tools/Input` | [Tools](./Tools/README.md) |

## Modules

| Module | What it covers | Entry |
|--------|----------------|-------|
| **Animations** | Runtime value animation for float, color, and `Vector3` | [Animations](./Animations/README.md) |
| **Audio** | Audio manager, mixer helpers, random music, audio UI | [Audio](./Audio/README.md) |
| **Bonus** | Slots, wheel rewards, collections, timed rewards | [Bonus](./Bonus/README.md) |
| **Cards** | Deck/hand/board runtime, poker, Drunkard sample | [Cards](./Cards/README.md) |
| **Condition** | No-code conditions, reflection checks, AND/OR logic, events | [Condition](./Condition/README.md) |
| **Core** | Level/XP helpers and core resources | [Core](./Core/README.md) |
| **Editor** | Editor windows, missing-script scan, settings, maintenance tools | [Editor](./Editor/README.md) |
| **Extensions** | Extension methods for C# and Unity APIs | [Extensions](./Extensions/README.md) |
| **GridSystem** | Grid-game constructor: field generation, placement, Dice, GridMerge, Match3, TicTacToe, SlidingMerge | [GridSystem](./GridSystem/README.md) |
| **Level** | Level manager, scene loading, level map flow | [Level](./Level/README.md) |
| **Merge** | Pure C# connected-group merge engine for grids, inventories, lists, and custom graphs | [Merge](./Merge/README.md) |
| **Network** | Mirror wrappers, no-code sync, lobby, discovery | [Network](./Network/README.md) |
| **NoCode** | Scene-only C# contracts and inspector wrappers; ScriptableObjects do not hold scene object references | [NoCode](./NoCode/README.md) |
| **NPC** | Navigation, target finder, patrol/chase, RPG combat brain | [NPC](./NPC/README.md) |
| **Parallax** | Parallax layers | [Parallax](./Parallax/README.md) |
| **Progression** | XP, levels, unlock tree, perk tree | [Progression](./Progression/README.md) |
| **PropertyAttribute** | Inspector attributes: button, color, inject helpers | [PropertyAttribute](./PropertyAttribute/README.md) |
| **Quest** | Quest configs, objectives, manager, runtime state | [Quest](./Quest/README.md) |
| **Reactive** | Serializable reactive properties for `float`, `int`, and `bool` | [Reactive](./Reactive/README.md) |
| **Rpg** | `RpgCharacter`, resources, stats, attacks, buffs/statuses, save/network/no-code bridges | [Rpg](./Rpg/README.md) |
| **Save** | Save providers, attributes, scene/global save flow | [Save](./Save/README.md) |
| **Settings** | Game settings, scene service, UI bindings | [Settings](./Settings/README.md) |
| **Shop** | Shop data, purchases, currency UI | [Shop](./Shop/README.md) |
| **StateMachine** | Runtime state machine and no-code data workflow | [StateMachine](./StateMachine/README.md) |
| **Tools** | Movement, free-fly camera, input, physics, spawning, timers, UI helpers | [Tools](./Tools/README.md) |
| **UI** | UI panels, button animations, toggles, presentation helpers | [UI](./UI/README.md) |

## Gameplay ownership

`Gameplay` is not a standalone Neoxider module. Gameplay systems are owned by concrete runtime modules (`Rpg`, `Quest`, `Progression`, `Cards`, `GridSystem`, `Tools`, `NoCode`, etc.). Do not add a new `Docs/Gameplay` or `Scripts/Gameplay` folder unless a real runtime assembly/API is introduced with clear ownership and tests.

## Samples and add-ons

| Section | Entry |
|---------|-------|
| **Sample scenes** | [Samples](./Samples.md) |
| **Examples** | [Examples](./Examples/README.md) |
| **NeoxiderPages** | [NeoxiderPages](./NeoxiderPages/README.md) |
| **UI Extension** | [UI Extension](./UI%20Extension/README.md) |
| **TODO** | [TODO](./TODO.md) |
| **Ideas** | [Ideas](./IDEAS.md) |

## Guides

- [Multiplayer Guide](./Network/Multiplayer_Guide.md)
- [NoCode Network Spec](./Network/NoCode_Network_Spec.md)
- [Vampire Survivors 3D guide](../Docs/VampireSurvivor_Guide.md)
