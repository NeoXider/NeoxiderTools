# Структура модулей библиотеки Neoxider

Правила организации папок, asmdef, неймспейсов и принципы при добавлении и рефакторинге модулей.

---

## 1. Общие принципы

- **Один скрипт — одна сущность:** в одном `.cs` файле только один публичный тип (класс, интерфейс, enum, struct). Не объединять в одном файле класс + интерфейс или несколько классов.
- **Модуль = домен:** каждый модуль живёт в своей папке под `Scripts/` (например `Core`, `Rpg`, `Progression`, `Tools/Inventory`) и имеет один корневой asmdef.
- **Namespace = имя модуля:** код модуля использует единый корневой неймспейс, совпадающий с именем asmdef (например `Neo.Core`, `Neo.Rpg`). Подпапки могут соответствовать поднеймспейсам по смыслу, но не обязаны дублировать путь буквально.

---

## 2. Стандартные папки внутри модуля

Используются по необходимости; не в каждом модуле есть все папки.

| Папка | Назначение | Содержимое |
|-------|------------|------------|
| **Interfaces/** | Контракты, доступ из других модулей | Только интерфейсы (`ILevelProvider`, `IRpgCombatReceiver`). Один интерфейс — один файл. |
| **Domain/** | Доменная логика без Unity | Чистая C#-логика: модели, калькуляторы, без `UnityEngine` и `MonoBehaviour`. При необходимости — подпапки по поддомену (например `Level/`, `Resources/` внутри Core). |
| **Data/** | Данные и конфиги | ScriptableObject-определения, сериализуемые структуры данных, списки конфигов. Один тип — один файл. |
| **Components/** | MonoBehaviour-компоненты | Компоненты для сцен и префабов. Можно вводить подпапки по смыслу (например `Components/Combat/`). Один компонент — один файл. |
| **Bridge/** | Связка с NoCode/внешними системами | Адаптеры для NeoCondition (`*ConditionAdapter`), NoCode-действия (`*NoCodeAction`), enum-режимы для них. Один адаптер/действие — один файл; enum режимов — отдельный файл. |
| **Runtime/** | Не-компонентный рантайм | Классы данных профиля, утилиты, внутренние хелперы (например `RpgProfileData`, `RpgCombatMath`). Не MonoBehaviour. |
| **Events/** | Типы событий модуля | Кастомные `UnityEvent<T>` и подобные, если вынесены в отдельные типы. Один тип — один файл. |
| **Enums/** | Перечисления модуля | Публичные enum, если их несколько и вынос в отдельные файлы улучшает читаемость. Один enum — один файл. |

**Корень модуля:** при единственном поддомене или малом объёме допустимы файлы в корне (например `LevelProfileData.cs`, `RpgResourceId.cs`). При росте модуля типы переносят в подходящие папки.

---

## 3. Namespace

- **Корневой неймспейс** совпадает с именем asmdef: `Neo.Core`, `Neo.Rpg`, `Neo.Progression`, `Neo.Tools.Inventory` и т.д.
- **Один неймспейс на весь модуль:** весь код модуля в одном корневом неймспейсе; поднеймспейсы не обязаны повторять путь папок (например всё в `Neo.Rpg`, а не `Neo.Rpg.Components`).
- **В asmdef** задаётся `rootNamespace` равный этому корню (например `"rootNamespace": "Neo.Rpg"`).

---

## 4. Assembly Definition (asmdef)

### Имя

- Формат: `Neo.<Module>` или `Neo.<Parent>.<Module>` для вложенных модулей.
- Примеры: `Neo.Core`, `Neo.Rpg`, `Neo.Progression`, `Neo.Tools.Managers`, `Neo.Tools.Inventory`.

### Размещение

- Один asmdef на модуль, в **корне папки модуля** (рядом с первым уровнем подпапок).
- Пример: `Scripts/Rpg/Neo.Rpg.asmdef` — все скрипты в `Rpg/` и подпапках входят в эту сборку.

### Поля в .asmdef

- **name** — совпадает с именем файла (без расширения), например `Neo.Rpg`.
- **rootNamespace** — тот же идентификатор, например `Neo.Rpg`.
- **references** — только необходимые зависимости (другие asmdef пакета, Unity/Test по необходимости). Не тащить лишние модули.
- **includePlatforms** / **excludePlatforms** — по умолчанию пусто (все платформы).
- **allowUnsafeCode** — по умолчанию `false`.
- **autoReferenced** — обычно `true`, чтобы сборка подтягивалась в билд.

### Когда создавать новый asmdef

- Новый **независимый домен** (новая папка верхнего уровня под `Scripts/` или под `Scripts/Tools/` и т.п.) — свой asmdef.
- Крупный подмодуль с чёткими границами и своими зависимостями — можно вынести в отдельную папку и asmdef (например `Neo.Tools.Dialogue`).
- Мелкие утилиты или тесно связанные с существующим модулем — лучше оставить в существующем asmdef и просто добавить папки/файлы.

---

## 5. Зависимости между модулями

- Зависимости указывать **явно** в `references` asmdef.
- Предпочтительно **слабая связность:** Core не должен зависеть от Rpg; общие контракты выносить в интерфейсы (Interfaces/) и по необходимости в общие модули (например `Neo.Core`).
- **Editor-код:** отдельный asmdef (например `Neo.Editor`, `Neo.Cards.Editor`, `Neo.StateMachine.Editor`) с ссылкой на нужные runtime-сборки; код только для редактора держать в папках `Editor/` или `*Editor/`.

---

## 6. Тесты

- EditMode-тесты — в `Assets/Neoxider/Editor/Tests/`, сборка `Neo.Editor.Tests.asmdef`.
- Тесты ссылаются на нужные runtime-asmdef (Core, Rpg, Progression и т.д.) и при необходимости на `UnityEngine.TestRunner`, `NUnit`.

---

## 7. Пример целевой структуры (модуль среднего размера)

```text
Scripts/Rpg/
  Neo.Rpg.asmdef
  Interfaces/
    IRpgCombatReceiver.cs
  Enums/
    RpgHitMode.cs
    RpgTargetSelectionMode.cs
    ...
  Data/
    RpgAttackDefinition.cs
    BuffDefinition.cs
    BuffStatModifier.cs
    ...
  Events/
    RpgAttackEvent.cs
    RpgStringEvent.cs
  Components/
    RpgStatsManager.cs
    RpgCombatant.cs
    RpgAttackController.cs
    ...
  Bridge/
    RpgConditionAdapter.cs
    RpgConditionEvaluationMode.cs
    RpgNoCodeAction.cs
    RpgNoCodeActionType.cs
  Runtime/
    RpgProfileData.cs
    RpgCombatMath.cs
    RpgTargetingUtility.cs
```

Все файлы в неймспейсе `Neo.Rpg`; один публичный тип на файл.

---

## 8. Чек-лист при добавлении нового модуля

1. Создать папку модуля под `Scripts/`.
2. Создать `Neo.<Module>.asmdef` с `name` и `rootNamespace`, добавить только нужные `references`.
3. Разложить код по папкам: Interfaces, Domain, Data, Components, Bridge, Runtime, Events, Enums — по смыслу.
4. Разнести типы так, чтобы в одном `.cs` был один класс/интерфейс/enum.
5. Задать во всех новых скриптах неймспейс, совпадающий с `rootNamespace`.
6. При необходимости добавить документацию в `Docs/` и запись в `CHANGELOG.md`.

---

## См. также

- [PROJECT_SUMMARY.md](./PROJECT_SUMMARY.md) — обзор пакета и структура репозитория.
- [DOCUMENTATION_GUIDELINES.md](./DOCUMENTATION_GUIDELINES.md) — правила оформления документации.
