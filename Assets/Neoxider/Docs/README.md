# NeoxiderTools Docs

Канонический вход в русскую документацию **NeoxiderTools** `v9.6.0`. Это карта переиспользования: перед новой реализацией проверьте, нет ли уже готового runtime API, MonoBehaviour-обертки или sample-сцены.

## Быстрый вход

- [README пакета](../README.md)
- [English docs](../DocsEn/README.md)
- [**Getting Started** — первая сцена за 5 минут](./GettingStarted.md)
- [**Cookbook** — рецепты и интересные примеры (кросс-модульные сборки)](./Cookbook.md)
- [Сводка проекта и reuse map](../PROJECT_SUMMARY.md)
- [Полезные компоненты](./UsefulComponents.md)
- [Совместимость пакета](./PackageCompatibility.md)
- [Sample-сцены](./Samples.md)

## Не писать с нуля

| Задача | Готовые блоки | Вход |
|--------|---------------|------|
| Сеточное поле, координаты, shape masks, pathfinding | `FieldGenerator`, `GridShapeMask`, `FieldDebugDrawer` | [GridSystem](./GridSystem/README.md) |
| Multi-cell pieces/items, drag/drop placement | `GridPlacementEntry`, `GridPlacementResult`, `PlaceContentFootprint` | [FieldGenerator](./GridSystem/FieldGenerator.md) |
| Merge связанных одинаковых элементов | `Neo.Merge`, `MergeResolver`, `GridMergeRequest.Increment(...)` | [Merge](./Merge/README.md) |
| Dice Merge, 2048-like, Match3, TicTacToe | `DiceBoardService`, `SlidingMergeBoardService`, `Match3BoardService`, `TicTacToeBoardService` | [GridSystem](./GridSystem/README.md) |
| Деньги, магазин, мультивалюта | `Money`, `IMoneySpend`, `Shop`, `TextMoney` | [Shop](./Shop/README.md) |
| Scene/global save | `SaveManager`, `SaveProvider`, `GlobalSave`, `SaveableBehaviour` | [Save](./Save/README.md) |
| HP/Mana/resources, XP/levels | `HealthComponent`, `ResourcePoolModel`, `LevelComponent`, `LevelCurveDefinition` | [Core](./Core/README.md) |
| RPG бой, projectiles, buffs/statuses | `RpgCharacter`, `RpgAttackController`, `RpgProjectile` | [Rpg](./Rpg/README.md) |
| Reward fly animation между world/canvas | `AnimationFly.Play(AnimationFlyRequest)`, sprite/prefab visuals, reward timing | [AnimationFly](./UI/AnimationFly.md) |
| NoCode условия, действия, state transitions | `NeoCondition`, `ConditionEntryPredicate`, NoCode bridges | [Condition](./Condition/README.md) |
| Movement, free-fly camera, pooling, timers, input | `Tools/Move`, `Tools/Spawner`, `Tools/Time`, `Tools/Input` | [Tools](./Tools/README.md) |

## Модули

| Модуль | Что внутри | Вход |
|--------|------------|------|
| **Animations** | Runtime-анимации значений: `float`, `Color`, `Vector3` | [Animations](./Animations/README.md) |
| **Audio** | Audio manager, mixer helpers, random music, audio UI | [Audio](./Audio/README.md) |
| **Bonus** | Слоты, колесо фортуны, коллекции, timed rewards | [Bonus](./Bonus/README.md) |
| **Cards** | Deck/hand/board runtime, card views, custom decks, poker, Drunkard sample | [Cards](./Cards/README.md) |
| **Condition** | NoCode-условия, reflection-проверки, AND/OR, события | [Condition](./Condition/README.md) |
| **Core** | Level/XP и базовые ресурсы HP/Mana-style | [Core](./Core/README.md) |
| **Editor** | Editor windows, missing scripts scan, настройки, сервисные утилиты | [Editor](./Editor/README.md) |
| **Extensions** | Extension-методы для C# и Unity API | [Extensions](./Extensions/README.md) |
| **GridSystem** | Конструктор сеточных игр: generation, placement, Dice, GridMerge, Match3, TicTacToe, SlidingMerge | [GridSystem](./GridSystem/README.md) |
| **Level** | Level manager, загрузка сцен, карта прогресса | [Level](./Level/README.md) |
| **Merge** | Universal pure C# merge engine for connected groups | [Merge](./Merge/README.md) |
| **Network** | Mirror wrappers, no-code sync, lobby, discovery | [Network](./Network/README.md) |
| **NoCode** | Сценовые C# contracts и inspector wrappers; SO не хранят scene object references | [NoCode](./NoCode/README.md) |
| **NPC** | Навигация, target finder, patrol/chase, RPG combat brain | [NPC](./NPC/README.md) |
| **Parallax** | Parallax layers and camera-following background tiles | [Parallax](./Parallax/README.md) |
| **Progression** | XP, уровни, unlock tree, perk tree | [Progression](./Progression/README.md) |
| **PropertyAttribute** | Inspector attributes: button, color, inject helpers | [PropertyAttribute](./PropertyAttribute/README.md) |
| **Quest** | Quest configs, цели, manager, runtime state | [Quest](./Quest/README.md) |
| **Reactive** | Generic `ReactiveProperty<T>` и serializable wrappers для `float`, `int`, `bool` | [Reactive](./Reactive/README.md) |
| **Rpg** | `RpgCharacter`, ресурсы, статы, атаки, buffs/statuses, save/network/no-code bridges | [Rpg](./Rpg/README.md) |
| **Save** | Save providers, attributes, scene/global save flow | [Save](./Save/README.md) |
| **Settings** | Game settings, scene service, UI bindings | [Settings](./Settings/README.md) |
| **Shop** | Typed shop API, purchases, bundles, currency UI | [Shop](./Shop/README.md) |
| **StateMachine** | Runtime state machine, C# core и scene/NoCode wrappers | [StateMachine](./StateMachine/README.md) |
| **Tools** | Movement, free-fly camera, input, physics, spawn, timers, UI helpers | [Tools](./Tools/README.md) |
| **UI** | UI panels, button animations, toggles, AnimationFly, presentation helpers | [UI](./UI/README.md) |

## Владение Gameplay

`Gameplay` не является отдельным модулем Neoxider. Gameplay-системы принадлежат конкретным runtime-модулям: `Rpg`, `Quest`, `Progression`, `Cards`, `GridSystem`, `Tools`, `NoCode` и другим. Не добавляйте новые папки `Docs/Gameplay` или `Scripts/Gameplay`, пока не появится реальная runtime-сборка/API с понятным владельцем и тестами.

## Samples и add-ons

| Раздел | Вход |
|--------|------|
| **Sample-сцены** | [Samples](./Samples.md) |
| **Examples** | [Examples](./Examples/README.md) |
| **NeoxiderPages** | [NeoxiderPages](./NeoxiderPages/README.md) |
| **UI Extension** | [UI Extension](./UI%20Extension/README.md) |
| **TODO** | [TODO](./TODO.md) |
| **Ideas** | [Ideas](./IDEAS.md) |

## Гайды

- [Multiplayer Guide](./Network/Multiplayer_Guide.md)
- [NoCode Network Spec](./Network/NoCode_Network_Spec.md)
- [Vampire Survivors 3D guide](./VampireSurvivor_Guide.md)
