# Neoxider Docs

Добро пожаловать в канонический индекс документации **NeoxiderTools** `v8.0.0`.

Используйте этот файл как главную точку входа в пользовательскую документацию.

- [Полезные компоненты](./UsefulComponents.md) - быстрый вход по сценарию `Add Component / GameObject`
- [Корневой README пакета](../README.md) - установка, обзор, samples
- [English onboarding](../DocsEn/README.md) - англоязычный вход по модулям и ключевым страницам
- [No-Code: привязка float → UI (Neo.NoCode)](./NoCode/README.md) — `NoCodeBindText`, `SetProgress`
- [No-Code: аудит, границы, варианты решений](./NO_CODE_AUDIT.md) — без нодового «визуального программирования»; пресеты, данные, каталоги действий
- **Локальные копии черновиков аудита (не в git):** `Local/Audits/`, см. [`/Local/README.md`](../../../Local/README.md)

## Оглавление модулей

| Модуль | Описание | Документация |
|--------|----------|--------------|
| **NoCode** | Привязка числа к `SetText`/TMP и Slider/Image без отдельных вью-скриптов | [`NoCode/README.md`](./NoCode/README.md) |
| **Animations** | Float, Color, `Vector3` и другие runtime-анимации значений | [`Animations/README.md`](./Animations/README.md) |
| **Audio** | `AudioManager`, микшер, random music и audio helper-компоненты | [`Audio/README.md`](./Audio/README.md) |
| **Bonus** | Слоты, колесо фортуны, коллекции и награды по времени | [`Bonus/README.md`](./Bonus/README.md) |
| **Cards** | MVP-архитектура, покер, «Пьяница» и карточный runtime-flow | [`Cards/README.md`](./Cards/README.md) |
| **Core** | Уровень/XP (LevelComponent) и пулы ресурсов (HealthComponent) для Progression и RPG | [`Core/README.md`](./Core/README.md) |
| **Condition** | No-Code условия: проверка полей, AND/OR логика, события | [`Condition/NeoCondition.md`](./Condition/NeoCondition.md) |
| **Editor** | Окна настроек, поиск missing scripts, авто-билд и сервисные утилиты | [`Editor/README.md`](./Editor/README.md) |
| **Extensions** | 300+ extension-методов для C# и Unity API | [`Extensions/README.md`](./Extensions/README.md) |
| **GridSystem** | Генерация сеток, origin-якорь, pathfinding, Match3 и TicTacToe | [`GridSystem.md`](./GridSystem.md) |
| **Level** | Менеджер уровней, загрузка сцен и карта прогресса | [`Level/README.md`](./Level/README.md) |
| **NPC** | Навигация NPC, патруль, chase, animator driver и RPG-ready combat composition | [`NPC/README.md`](./NPC/README.md) |
| **Parallax** | Параллакс-слои и связанные визуальные компоненты | [`Parallax/ParallaxLayer.md`](./Parallax/ParallaxLayer.md) |
| **PropertyAttribute** | `[Button]`, `[GUIColor]`, inject-атрибуты и inspector helper-утилиты | [`PropertyAttribute/README.md`](./PropertyAttribute/README.md) |
| **Quest** | Конфиги квестов, менеджер, цели и runtime-состояние | [`Quest/README.md`](./Quest/README.md) |
| **Progression** | XP, уровни, unlock tree, perk tree и persistent progression | [`Progression/README.md`](./Progression/README.md) |
| **RPG** | HP, уровни, баффы, статусы, melee/ranged/aoe, evade, target selectors, attack presets и no-code bridges | [`Rpg/README.md`](./Rpg/README.md) |
| **Reactive** | Реактивные сериализуемые свойства `float`, `int`, `bool` | [`Reactive/README.md`](./Reactive/README.md) |
| **Save** | `PlayerPrefs`, JSON-файлы, provider API, scene/global saves и атрибуты сохранения | [`Save/README.md`](./Save/README.md) |
| **Settings** | `GameSettings`, сервис настроек, UI `SettingsView`, URP/Quality presets, интеграция с `SaveProvider` | [`Settings/README.md`](./Settings/README.md) |
| **Shop** | Магазин, валюта и покупки | [`Shop/README.md`](./Shop/README.md) |
| **StateMachine** | Код + No-Code, runtime state machine и визуальный редактор | [`StateMachine/README.md`](./StateMachine/README.md) |
| **Tools** | 150+ компонентов: движение, физика, спавнеры, таймеры, ввод и utility runtime | [`Tools/README.md`](./Tools/README.md) |
| **UI** | UI-панели, анимации кнопок, переключатели и presentation helpers | [`UI/README.md`](./UI/README.md) |

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

## Гайды и туториалы

| Название | Описание | Ссылка |
|----------|----------|--------|
| **Vampire Survivors 3D** | Пошаговое руководство по созданию клона Vampire Survivors в 3D | [`VampireSurvivor_Guide.md`](./VampireSurvivor_Guide.md) |
