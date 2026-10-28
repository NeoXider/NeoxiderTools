# NeoxiderTools Docs

Documentation entry point for **NeoxiderTools** `v11.0.1`, with English guides and short Russian routes. Start here for public runtime APIs, scene setup, and optional Inspector / NoCode workflows.

> **11.0.0.** Windows verification recorded on 26–27 October 2026 covers Unity `6000.3.14f1` and `6000.6.0f1`, including focused CardGame sizing/gameplay acceptance and three manual OS-click rounds. See [Verification 11](../Verification11.md) for exact inputs, overlapping scopes and limitations. See the tagged release and its asset manifest. For an existing 10.x project, read [Migration to 11](./Migration11.md).

## Choose your starting point

| Goal | Entry |
|------|-------|
| C# API без сцены / Runtime API | [Runtime-model quick start](./GettingStarted.md#start-with-a-c-runtime-model), then [Core](./Core/README.md), [Merge](./Merge/README.md), or [StateMachine](./StateMachine/README.md) |
| Первый компонент / First component | [Getting Started — first audio scene](./GettingStarted.md#your-first-scene-in-5-minutes-audio-manager-am), then [Audio](./Audio/README.md) |
| Inspector / NoCode | [NoCode Getting Started](./NoCode/GettingStarted.md) and [Condition](./Condition/README.md) |
| Взаимодействия / Interaction | [Interaction contracts and components](./Tools/InteractableObject/README.md) |
| Примеры / Samples | [Sample scenes and source/imported paths](./Samples.md) |
| Обновление с 10.x / Upgrade from 10.x | [Migration to 11](./Migration11.md) |
| Проверки / Verification | [Recorded results and limits](../Verification11.md), [EditMode and isolation](./Testing.md), [PlayMode](./PlayModeTesting.md), and [Package compatibility](./PackageCompatibility.md) |
| Host requirements / Требования проекта | [Package compatibility](./PackageCompatibility.md) and [IL2CPP / code stripping](./IL2CPP.md) |
| Dependencies / Зависимости и MCP | [Versions, upgrade constraints and editor tooling](./Dependencies.md) |

Use each module's README as its canonical entry. It links to the runtime API and the scene / NoCode setup. Runtime models can be owned by your code; scene services document their component dependencies. ScriptableObjects store reusable configuration, while scene components hold scene references.

**По-русски:** выберите строку под свою задачу. C#-ядро можно использовать отдельно; компоненты и NoCode подключаются по необходимости. Для обновления существующего проекта начните с migration guide, а для запуска тестов — с требований к изолированному host.

For HtmlToUitk, start with the [primary guide](UI/HtmlToUitk.md) and
[current370-per-SDK native/CLI/manual verification](UI/HtmlToUitkVerification11.md). Unity deliveries do
not contain the external converter: `HtmlToUitk.Cli.v11.0.0.zip` is separate, using tested.NET 10/LatestMajor.
Legacy delivery is choose-one Core or Complete; [Samples](Samples.md#legacy-release-archives-choose-one)
explains the layouts. Final archive import status is in [Verification 11](../Verification11.md).

## Namespace policy

Runtime gameplay types live in their module namespace (`Neo.Audio`, `Neo.Bonus`, `Neo.Level`,
`Neo.Parallax`, `Neo.UI`, and so on). The root `Neo` namespace is intentionally reserved for the small
authoring-attribute surface used across every module: `[Button]`, `[GUIColor]`, `[FindInScene]`,
`[NeoDoc]`, `[RequireInterface]`, `[CreateFromMenu]`, `[LegacyComponent]`, and their editor drawers.
Keeping these attributes at the root preserves the terse, stable `using Neo;` workflow; new runtime
components must use a module namespace.

## Start here

- [Package README](../README.md)
- [**Getting Started** — C# runtime model or first audio scene](./GettingStarted.md)
- [**NoCode Getting Started** — build gameplay without writing code](./NoCode/GettingStarted.md)
- [**Cookbook** — recipes & interesting examples (cross-module combos)](./Cookbook.md)
- [Project summary](../PROJECT_SUMMARY.md)
- [Useful components](./UsefulComponents.md)
- [Package compatibility](./PackageCompatibility.md)
- [Sample scenes](./Samples.md)
- [Migration from 10.x to 11](./Migration11.md)
- [Testing — EditMode contracts and isolation](./Testing.md)
- [PlayMode testing — lifecycle and integration scope](./PlayModeTesting.md)
- [IL2CPP and code stripping](./IL2CPP.md)

Static-reset and Play Mode transition fixtures need an isolated host. The testing guides explain provider/scene ownership and global-state effects; broad test-suite dependencies can exceed those of a single runtime module.

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
| Data-driven abilities/modifiers, buffs/debuffs/DoT/shields, cast pipeline (v10 successor to Rpg) | `AbilityDefinition`, `ModifierDefinition`, `AbilityUnitBehaviour`, `AbilityCasterBehaviour` | [Abilities](./Abilities/README.md) |
| RPG combat, projectiles, buffs/statuses (superseded by Abilities in v10) | `RpgCharacter`, `RpgAttackController`, `RpgProjectile` | [Rpg](./Rpg/README.md) |
| Reward fly animation between world/canvas points | `AnimationFly.Play(AnimationFlyRequest)`, sprite/prefab visuals, reward timing | [AnimationFly](./UI/AnimationFly.md) |
| NoCode conditions, actions, state transitions | `NeoCondition`, `ConditionEntryPredicate`, module NoCode bridges | [Condition](./Condition/README.md) |
| Custom interaction input, hover/click, physics events | `IInteractiveTarget`, `InteractiveObject`, `InteractionRayProvider` | [Interaction](./Tools/InteractableObject/README.md) |
| Movement, free-fly camera, pooling, timers, input | `Tools/Move`, `Tools/Spawner`, `Tools/Time`, `Tools/Input` | [Tools](./Tools/README.md) |

## Modules

| Module | What it covers | Entry |
|--------|----------------|-------|
| **Abilities** | Data-driven Dota-style abilities and modifiers: units, properties, buffs/debuffs/DoT/shields, cast pipeline, multiplayer-ready receipts; v10 successor to Rpg | [Abilities](./Abilities/README.md) |
| **Animations** | Runtime value animation for float, color, and `Vector3` | [Animations](./Animations/README.md) |
| **Audio** | Audio manager, mixer helpers, random music, audio UI | [Audio](./Audio/README.md) |
| **Haptics** | Device vibration: named feels, settings switch, repeat throttle, MobileHapticFeedback backend | [Haptics](./Haptics/Haptics.md) |
| **Bonus** | Slots, wheel rewards, collections, timed rewards | [Bonus](./Bonus/README.md) |
| **Cards** | Deck/hand/board runtime, poker, Drunkard sample | [Cards](./Cards/README.md) |
| **Condition** | No-code conditions, reflection checks, AND/OR logic, events | [Condition](./Condition/README.md) |
| **Core** | Level/XP helpers and core resources | [Core](./Core/README.md) |
| **Editor** | Editor windows, missing-script scan, settings, maintenance tools | [Editor](./Editor/README.md) |
| **Extensions** | Extension methods for C# and Unity APIs | [Extensions](./Extensions/README.md) |
| **GridSystem** | Grid-game constructor: field generation, placement, Dice, GridMerge, Match3, TicTacToe, SlidingMerge | [GridSystem](./GridSystem/README.md) |
| **HTML → UITK** | Supported HTML/CSS subset to native UI Toolkit controls, typed actions, lifecycle, and external CLI conversion | [HTML → UITK](./UI/HtmlToUitk.md) |
| **Inventory** | Aggregated and slot-grid inventories, pickups, transfers, save integration | [Inventory](./Tools/Inventory/README.md) |
| **Interaction** | Reusable C# contracts, scene hover/click, 2D/3D physics events, ray modes, range checks | [Interaction](./Tools/InteractableObject/README.md) |
| **Level** | Level manager, scene loading, level map flow | [Level](./Level/README.md) |
| **Merge** | Pure C# connected-group merge engine for grids, inventories, lists, and custom graphs | [Merge](./Merge/README.md) |
| **Network** | Mirror wrappers, no-code sync, lobby, discovery, realtime .io toolkit (snapshots, prediction), bootstrap, telemetry | [Network](./Network/README.md) |
| **NoCode** | Scene UI bindings and Inspector wrappers around C# contracts; ScriptableObjects do not hold scene object references | [NoCode](./NoCode/README.md) |
| **NPC** | Navigation, target finder, patrol/chase, RPG combat brain | [NPC](./NPC/README.md) |
| **Parallax** | Parallax layers | [Parallax](./Parallax/README.md) |
| **Progression** | XP, levels, unlock tree, perk tree | [Progression](./Progression/README.md) |
| **PropertyAttribute** | Inspector attributes: button, color, inject helpers | [PropertyAttribute](./PropertyAttribute/README.md) |
| **Quest** | Quest configs, objectives, manager, runtime state | [Quest](./Quest/README.md) |
| **Reactive** | Serializable reactive properties for `float`, `int`, and `bool` | [Reactive](./Reactive/README.md) |
| **Rpg** | `RpgCharacter`, resources, stats, attacks, buffs/statuses, save/network/no-code bridges (superseded by Abilities in v10) | [Rpg](./Rpg/README.md) |
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
| **Character Movement Fundamentals** | [Character controller and optional demo scenes](./Tools/Move/CharacterController/README.md) |
| **UI Extension** | [UI Extension](./UI%20Extension/README.md) |

## Guides

- [Multiplayer Guide](./Network/Multiplayer_Guide.md)
- [Realtime IO Guide](./Network/Realtime_IO_Guide.md) - server-authoritative realtime games: handshake, snapshots, interpolation, prediction, events, bootstrap
- [NoCode Network Spec](./Network/NoCode_Network_Spec.md)
- [Survivor demo — Vampire-Survivors kit on `Neo.Abilities`](./Abilities/SurvivorDemo.md)
- [Vampire Survivors 3D guide](./VampireSurvivor_Guide.md)
