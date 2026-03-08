# Neoxider Docs

Добро пожаловать в канонический индекс документации **NeoxiderTools** `v7.6.0`.

Используйте этот файл как главную точку входа в пользовательскую документацию.

- [Полезные компоненты](./UsefulComponents.md) - быстрый вход по сценарию `Add Component / GameObject`
- [Корневой README пакета](../README.md) - установка, обзор, samples
- [English onboarding](../DocsEn/README.md) - англоязычный вход по модулям и ключевым страницам

## Оглавление модулей

| Модуль | Описание | Документация |
|--------|----------|--------------|
| **Animations** | Анимация значений, цветов, `Vector3`, света | [`Animations/README.md`](./Animations/README.md) |
| **Audio** | AudioManager, микшер, play-on-click, music helpers | [`Audio/README.md`](./Audio/README.md) |
| **Bonus** | Коллекции, слот-машины, награды по времени | [`Bonus/README.md`](./Bonus/README.md) |
| **Cards** | Карточные игры, колода, рука, presenter/view | [`Cards/README.md`](./Cards/README.md) |
| **Condition** | No-code условия и UnityEvent | [`Condition/NeoCondition.md`](./Condition/NeoCondition.md) |
| **Editor** | Инспекторы, авто-билд, сервисные окна | [`Editor/README.md`](./Editor/README.md) |
| **Extensions** | Расширения C# и Unity API | [`Extensions/README.md`](./Extensions/README.md) |
| **GridSystem** | Shape/origin/pathfinding, Match3, TicTacToe | [`GridSystem.md`](./GridSystem.md) |
| **Level** | Загрузка сцен, прогресс, карта уровней | [`Level/README.md`](./Level/README.md) |
| **NPC** | Модульная навигация NPC | [`NPC/README.md`](./NPC/README.md) |
| **Parallax** | Параллакс-слои | [`Parallax/ParallaxLayer.md`](./Parallax/ParallaxLayer.md) |
| **PropertyAttribute** | Атрибуты инспектора и inject-хелперы | [`PropertyAttribute/README.md`](./PropertyAttribute/README.md) |
| **Quest** | Конфиги квестов, менеджер, триггеры и состояние прогресса | [`Quest/README.md`](./Quest/README.md) |
| **Progression** | XP, уровни, unlock tree, perk tree и persistent progression | [`Progression/README.md`](./Progression/README.md) |
| **RPG** | Persistent profile с `Auto Save`, combatant-актеры, melee/ranged/aoe атаки, target selectors, presets для AI/skills/spells, built-in input, evade, баффы и статусы | [`Rpg/README.md`](./Rpg/README.md) |
| **Reactive** | Реактивные сериализуемые свойства `float`, `int`, `bool` | [`Reactive/README.md`](./Reactive/README.md) |
| **Save** | Компонентные сохранения, provider API, global data | [`Save/README.md`](./Save/README.md) |
| **Shop** | Магазин, валюта, покупки | [`Shop/README.md`](./Shop/README.md) |
| **StateMachine** | State machine и no-code редактор | [`StateMachine/README.md`](./StateMachine/README.md) |
| **Tools** | Инвентарь, движение, спавн, диалоги, время, ввод | [`Tools/README.md`](./Tools/README.md) |
| **UI** | UI-анимации, кнопки, страницы, toggle | [`UI/README.md`](./UI/README.md) |

## Подмодули `Tools`

| Подмодуль | Что внутри | Документация |
|-----------|------------|--------------|
| `Tools/Components` | `Counter`, `ScoreManager`, `TypewriterEffect`, `RpgStatsDamageableBridge`; *Health/Evade/AttackSystem — legacy, см. [RPG](./Rpg/README.md)* | [`Tools/Components/README.md`](./Tools/Components/README.md) |
| `Tools/Dialogue` | `DialogueController`, `DialogueData`, `DialogueUI` | [`Tools/Dialogue/README.md`](./Tools/Dialogue/README.md) |
| `Tools/Input` | `MouseInputManager`, `MultiKeyEventTrigger`, `SwipeController` | [`Tools/Input/README.md`](./Tools/Input/README.md) |
| `Tools/Inventory` | `InventoryComponent`, `InventoryDropper`, `PickableItem` | [`Tools/Inventory/README.md`](./Tools/Inventory/README.md) |
| `Tools/InteractableObject` | `InteractiveObject`, `PhysicsEvents2D`, `PhysicsEvents3D` | [`Tools/InteractableObject/README.md`](./Tools/InteractableObject/README.md) |
| `Tools/Managers` | `GM`, `EM`, `Bootstrap`, `Singleton` | [`Tools/Managers/README.md`](./Tools/Managers/README.md) |
| `Tools/Move` | Контроллеры движения и курсора | [`Tools/Move/README.md`](./Tools/Move/README.md) |
| `Tools/Physics` | Magnetic, impulse, explosive helpers | [`Tools/Physics/README.md`](./Tools/Physics/README.md) |
| `Tools/Random` | `ChanceManager`, `ChanceSystemBehaviour`, chance data | [`Tools/Random/README.md`](./Tools/Random/README.md) |
| `Tools/Spawner` | Object pooling и спавнеры | [`Tools/Spawner/README.md`](./Tools/Spawner/README.md) |
| `Tools/Text` | `SetText`, `TimeToText` | [`Tools/Text/README.md`](./Tools/Text/README.md) |
| `Tools/Time` | `Timer`, `TimerObject` | [`Tools/Time/README.md`](./Tools/Time/README.md) |
| `Tools/View` | Selector и view-утилиты | [`Tools/View/README.md`](./Tools/View/README.md) |
| `Tools/Debug` | `FPS`, `ErrorLogger` и debug-хелперы | [`Tools/Debug/README.md`](./Tools/Debug/README.md) |
| `Tools/Draw` | Компоненты рисования и визуальной отладки | [`Tools/Draw/README.md`](./Tools/Draw/README.md) |
| `Tools/FakeLeaderboard` | Демо-лидерборд и элементы списка | [`Tools/FakeLeaderboard/README.md`](./Tools/FakeLeaderboard/README.md) |
| `Tools/Other` | Прочие utility-компоненты и интеграции | [`Tools/Other/README.md`](./Tools/Other/README.md) |

## Samples и add-ons

| Раздел | Что внутри | Документация |
|--------|------------|--------------|
| **NeoxiderPages** | Sample-модуль экранов, страниц и `UIKit` workflow | [`NeoxiderPages/README.md`](./NeoxiderPages/README.md) |
| **UI Extension** | Готовые UI-prefab наборы и editor-меню | [`UI Extension/README.md`](./UI%20Extension/README.md) |
