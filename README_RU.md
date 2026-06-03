# NeoxiderTools для Unity

[Русский](README_RU.md) | [English](README.md)

[![Version](https://img.shields.io/badge/version-9.1.0-blue)]() [![Unity](https://img.shields.io/badge/Unity-2022.1+-green)]() [![Namespace](https://img.shields.io/badge/namespace-Neo-orange)]()

`NeoxiderTools` — Unity-пакет для быстрой сборки игровых систем без скрытой магии. Внутри: no-code компоненты для Inspector, runtime-модули, editor-инструменты, samples и документация по точкам расширения.

Пакет подходит для прототипов, game jam проектов и production-игр: условия, сохранения, магазин, RPG-бой, инвентарь, квесты, state machine, UI, сетевые мосты и набор утилит, которые убирают повторяющийся glue-code.

## Быстрые ссылки

- [Русская документация](Assets/Neoxider/Docs/README.md)
- [English docs](Assets/Neoxider/DocsEn/README.md)
- [README пакета](Assets/Neoxider/README.md)
- [Сводка проекта](Assets/Neoxider/PROJECT_SUMMARY.md)
- [Changelog](Assets/Neoxider/CHANGELOG.md)
- [Multiplayer guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md)

## Установка

Установите **NeoxiderTools** и обязательные сторонние пакеты ниже. В Unity: `Window > Package Manager > + > Add package from git URL` (для DOTween — импорт из Asset Store).

### NeoxiderTools (Git URL)

```text
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

### DOTween (обязательно)

Установите из [Unity Asset Store — DOTween (HOTween v2)](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676), затем выполните настройку: `Tools > Demigiant > DOTween Utility Panel`.

### UniTask (обязательно)

```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

### Ручная установка (только NeoxiderTools)

Скопируйте `Assets/Neoxider` в свой Unity-проект. DOTween и UniTask всё равно нужно установить отдельно.

## Требования

- Unity `2022.1+`.
- Автоматически через UPM вместе с NeoxiderTools: `com.unity.textmeshpro`, `com.unity.ai.navigation`, `com.unity.inputsystem`.
- **Обязательно (host-проект):** [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676), [UniTask](https://github.com/Cysharp/UniTask) — см. [Установка](#установка).
- **Опционально:** `DOTween Pro` (sample NeoxiderPages), `Mirror`, `Spine Unity Runtime`, `Odin Inspector`, `MarkdownRenderer`.
- URP опционален. Пакет больше не зависит от `com.unity.render-pipelines.universal`; добавляйте URP отдельно только если проект использует URP-specific rendering features или 2D lights.

## Что внутри

| Область | Что решает | Документация |
|---------|------------|--------------|
| Condition | Проверки через Inspector: поля, свойства, методы, GameObject state, AND/OR, события | [Condition](Assets/Neoxider/Docs/Condition/NeoCondition.md) |
| NoCode | Привязка значений компонентов к UI и действиям без одноразовых view-скриптов | [NoCode](Assets/Neoxider/Docs/NoCode/README.md) |
| RPG | Ресурсы, статы, баффы, статусы, прогрессия, бой, цели, multiplayer-ready API | [RPG](Assets/Neoxider/Docs/Rpg/README.md) |
| Shop | Предметы, бандлы, owned/equipped, multi-currency, inventory bridge, save profile | [Shop](Assets/Neoxider/Docs/Shop/README.md) |
| Inventory | Aggregated и Slot Grid инвентарь, pickups, transfer rules, сохранение | [Inventory](Assets/Neoxider/Docs/Tools/Inventory/README.md) |
| Save | PlayerPrefs/JSON provider flow, typed helpers, save attributes | [Save](Assets/Neoxider/Docs/Save/README.md) |
| Progression | XP, уровни, perk tree, unlock tree, persistent progression | [Progression](Assets/Neoxider/Docs/Progression/README.md) |
| Quest | Конфиги квестов, цели, менеджер, no-code actions, runtime state | [Quest](Assets/Neoxider/Docs/Quest/README.md) |
| StateMachine | Runtime state machine и no-code transition predicates | [StateMachine](Assets/Neoxider/Docs/StateMachine/README.md) |
| Network | Опциональные Mirror wrappers и no-code network action/sync bridges | [Network guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md) |
| Tools | Movement, свободный полет камеры, physics, timers, spawners, interaction, text, managers, view helpers | [Tools](Assets/Neoxider/Docs/Tools/README.md) |
| Extensions | 300+ extension-методов для C# и Unity API | [Extensions](Assets/Neoxider/Docs/Extensions/README.md) |

## No-code пример: NeoCondition

`NeoCondition` позволяет собирать игровую логику прямо в Inspector:

- Проверять поля, свойства, состояние GameObject или методы с одним аргументом.
- Сравнивать с константой или другим объектом.
- Использовать `AND`, `OR` и инверсию.
- Вызывать `OnTrue`, `OnFalse`, `OnResult`, `OnInvertedResult` через UnityEvent.

Пример: `Money.CanSpend(100) == true` включает кнопку покупки, а `== false` показывает сообщение о нехватке денег.

Подробнее: [документация NeoCondition](Assets/Neoxider/Docs/Condition/NeoCondition.md).

## Чеклист первой сцены

1. Установите NeoxiderTools, DOTween и UniTask (см. [Установка](#установка)).
2. Добавьте `Assets/Neoxider/Prefabs/--System--.prefab`, если сцена использует встроенные менеджеры или UI bootstrap.
3. Добавляйте компоненты через `Add Component > Neoxider`.
4. Начните с нужного модуля: [Shop](Assets/Neoxider/Docs/Shop/README.md), [RPG](Assets/Neoxider/Docs/Rpg/README.md), [Condition](Assets/Neoxider/Docs/Condition/NeoCondition.md), [Tools](Assets/Neoxider/Docs/Tools/README.md).
5. Для мультиплеера сначала установите Mirror и откройте [Multiplayer guide](Assets/Neoxider/Docs/Network/Multiplayer_Guide.md).

## Samples

Samples лежат в `Assets/Neoxider/Samples~/` и импортируются через Package Manager при UPM-установке.

| Sample | Назначение |
|--------|------------|
| Demo | Интеграционные сцены для основных систем и gameplay-модулей |
| NeoxiderPages | Опциональный sample навигации страниц: `PM`, `UIPage`, `BtnChangePage`, UIKit helpers |

## Тесты

Тесты пакета находятся в `Assets/Neoxider/Tests/`:

- `Edit` — edit-mode и pure logic проверки.
- `Play` / `PlayMode` — runtime и scene behavior.
- `Editor` — editor-specific проверки пакета.

Запуск через Unity Test Runner. Для тестов в проекте должен быть `com.unity.test-framework`.

## Структура проекта

```text
Assets/Neoxider/
  Scripts/       Runtime-модули и asmdef-разделение
  Editor/        Custom inspectors, окна и editor-утилиты
  Tests/         EditMode и PlayMode тесты пакета
  Docs/          Русская документация
  DocsEn/        Английская документация
  Samples~/      UPM samples
  Prefabs/       Готовые префабы
  Resources/     Настройки и assets пакета
```

## Игры на NeoxiderTools

| Игра | Жанр | Платформа | Ссылка | Примечание |
|------|------|-----------|--------|------------|
| Внуки понарошку: пенсия прилагается | Arcade, Survival | Windows | [MyIndie](https://myindie.ru/games/game/fake-grandkids) | UralGameJam 2026; inspector-driven Neoxider workflow |

## Поддержка

Открывайте issue или pull request в репозитории. При изменении публичного поведения обновляйте [CHANGELOG.md](Assets/Neoxider/CHANGELOG.md) и документацию соответствующего модуля.
