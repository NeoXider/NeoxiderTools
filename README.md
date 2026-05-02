# Neoxider — коллекция мощных инструментов для Unity

[![Version](https://img.shields.io/badge/version-8.0.0-blue)]() [![Unity](https://img.shields.io/badge/Unity-2022.1+-green)]() [![Namespace](https://img.shields.io/badge/namespace-Neo-orange)]()

> **RU:** Готовые решения для Unity, которые легко интегрируются в ваш проект. Более 150 модулей для быстрой разработки игр без лишних сложностей.
> 
> **EN:** Ready-to-use Unity tools that integrate easily into your project. 150+ modules for fast game development without unnecessary complexity.

**Neoxider** — экосистема готовых инструментов для Unity, созданная разработчиками для разработчиков. Легко настраивается через Inspector, не требует глубокого погружения в код, но остаётся полностью прозрачной и расширяемой. Идеально подходит для прототипирования и продакшн-проектов.

**Neoxider** is an ecosystem of ready-to-use Unity tools, built by developers for developers. Easy to configure through Inspector, no deep code diving required, yet fully transparent and extensible. Perfect for prototyping and production projects.

📖 **[Полная документация (RU) →](Assets/Neoxider/Docs/README.md)** · 📖 **[English docs →](Assets/Neoxider/DocsEn/README.md)** · 📌 **[PROJECT_SUMMARY →](Assets/Neoxider/PROJECT_SUMMARY.md)** · 📝 **[Changelog →](Assets/Neoxider/CHANGELOG.md)**

**Документация (RU):** [Docs/README.md](Assets/Neoxider/Docs/README.md) — канонический индекс всех модулей. **Documentation (EN):** [DocsEn/README.md](Assets/Neoxider/DocsEn/README.md) — английский entry point для верхнеуровневых модулей и ключевых страниц; если глубокая страница ещё не переведена, индекс ведёт в соответствующий русский раздел. Tools-подмодули и samples (NeoxiderPages, UI Extension) включены в оба индекса.

---

## 📑 Содержание

- [No-Code условия — NeoCondition](#no-code-условия--neocondition)
- [Чем примечателен Neoxider](#чем-примечателен-neoxider)
- [Demo Scenes](#demo-scenes)
- [Games built with NeoxiderTools](#games-built-with-neoxidertools) — игры на базе экосистемы
- [Demo Games](#demo-games)
- [Быстрый старт](#быстрый-старт)
- [Таблица модулей](#таблица-модулей)
  - [Condition](#condition--no-code-условия) · [Tools](#tools--инструменты) · [UI](#ui--интерфейс) · [Bonus](#bonus--бонусные-системы) · [Shop](#shop--магазин) · [Save](#save--сохранения) · [Quest](#quest--квесты) · [Cards](#cards--карточные-игры) · [StateMachine](#statemachine--машина-состояний) · [Animations](#animations--анимации) · [Audio](#audio--звук) · [Extensions](#extensions--расширения-c) · [Editor](#editor--инструменты-редактора) · [Level](#level--уровни) · [NPC](#npc) · [Parallax](#parallax) · [GridSystem](#gridsystem) · [PropertyAttribute](#propertyattribute) · [Reactive](#reactive)
- [Топовые модули](#топовые-модули)
- [Установка через UPM](#установка-через-upm) — [Зависимости](#зависимости), [Основной пакет](#основной-пакет), [Ручная установка](#ручная-установка)
- [Установка Demo Scenes и NeoxiderPages](#установка-demo-scenes-и-neoxiderpages)
- [FAQ](#faq)
- [Поддержка и вклад](#поддержка-и-вклад)

---

## No-Code условия — NeoCondition

Проектируйте сложную игровую логику **без единой строчки кода**. Компонент `NeoCondition` позволяет прямо в Inspector:

- **Проверять любые данные** — HP, очки, состояние объекта, любое public поле или свойство любого компонента
- **Комбинировать условия** — AND/OR логика, инверсия (NOT), несколько проверок в одном компоненте
- **Реагировать на изменения** — события `OnTrue`, `OnFalse`, `OnResult` подключаются к любым объектам через UnityEvent
- **Проверять свойства GameObject** — `activeSelf`, `tag`, `layer` и другие — без дополнительных компонентов
- **Работать с будущими объектами** — находите объекты по имени, настраивайте условия для префабов до их спавна через Prefab Preview
- **Выбирать режим проверки** — Interval, EveryFrame, Manual; фильтр Only On Change исключает лишние срабатывания

> **Пример:** «Когда `Health.Hp <= 0` — показать Game Over» — одна настройка в Inspector, ноль строк в коде.

📖 [Документация NeoCondition →](Assets/Neoxider/Docs/Condition/NeoCondition.md)

---

## Чем примечателен Neoxider

- **Production-ready** — каждая подсистема поставляется с примерами, документацией и продуманными интеграциями
- **No-Code там, где нужно** — большинство компонентов настраиваются через Inspector и UnityEvent, но остаются расширяемыми
- **Гибридный подход** — No-Code + Code для максимальной гибкости
- **Модульность** — изоляция через Assembly Definition Files, импортируйте только нужные модули
- **Расширяемость** — наследование, интерфейсы, публичный API у каждого компонента
- **Автоматическое сохранение** — мощный модуль атрибутов сохранения, многие скрипты хранят данные автоматически
- **Документация внутри** — у каждого модуля есть собственный README в `Assets/Neoxider/Docs/`

> Обратите особое внимание модулю **Extensions**, если любите писать код — 300+ методов-расширений для C# и Unity API.
> Множество скриптов поддерживают работу через код: Singleton, ChanceSystem, Timer и другие.

---

<img width="464" height="522" alt="image" src="https://github.com/user-attachments/assets/fbb02b88-fed6-4445-bf19-079382966628" />

## Demo Scenes
![image](https://github.com/user-attachments/assets/90c98f0c-aae2-4837-81ed-b18a10b65ed5)

## Games built with NeoxiderTools

> **RU:** Реальные релизы и демо, где **NeoxiderTools** — основа геймплея (в т.ч. no-code в Inspector).  
> **EN:** Shipping titles and jams that build on **NeoxiderTools** (including inspector-driven workflows).

| Игра · Game | Жанры · Genres | Платформа | Ссылка · Link | Примечание · Notes |
|-------------|----------------|-----------|---------------|-------------------|
| [**Внуки понарошку: пенсия прилагается**](https://myindie.ru/games/game/fake-grandkids) | Arcade, Survival | Windows | [MyIndie — страница · store page](https://myindie.ru/games/game/fake-grandkids) | RU; **UralGameJam 2026**; логика через NeoCondition и экосистему NeoxiderTools · v7.8.0|

## Demo Games
<img width="354" height="623" alt="2025-11-02_22-31-20" src="https://github.com/user-attachments/assets/56c255c1-5e96-410c-b212-ea865ea4521f" />
<img width="372" height="623" alt="image" src="https://github.com/user-attachments/assets/6d16edff-dd20-47bb-90f1-c3fc0e913d68" />
<img width="345" height="703" alt="image" src="https://github.com/user-attachments/assets/2c45a361-201b-499f-b77f-c90b3f02c757" />

---

## Быстрый старт

1. **Установите зависимости** — Unity 2022+ (рекомендуется)
2. **Импортируйте** папку `Assets/Neoxider` в проект (или через [UPM](#установка-через-upm))
3. **Добавьте системный префаб** `Prefabs/--System--.prefab` в сцену — менеджеры событий и UI
4. **Перетаскивайте компоненты** из Inspector — большинство работает без кода через UnityEvent
5. **Изучите документацию** — откройте README в `Docs/` для нужного модуля

## Тесты

- Базовые `EditMode` тесты находятся в `Assets/Neoxider/Editor/Tests/`.
- Они покрывают критичные сценарии для `Save`, `Level`, `Bootstrap` и legacy/editor-поведения.
- Для запуска в Unity используйте `Test Runner` или пакет `com.unity.test-framework`.

---

## Таблица модулей

| Модуль | Описание |
|--------|----------|
| ⚙️ [**Condition**](#condition--no-code-условия) | No-Code условия: проверка полей, AND/OR логика, события |
| 🛠️ [**Tools**](#tools--инструменты) | 150+ компонентов: движение, физика, спавнеры, таймеры, ввод |
| 🖼️ [**UI**](#ui--интерфейс) | UI-панели, анимации кнопок, переключатели |
| 🎁 [**Bonus**](#bonus--бонусные-системы) | Слоты, колесо фортуны, коллекции, награды по времени |
| 🛒 [**Shop**](#shop--магазин) | Магазин, валюта, покупки |
| 💾 [**Save**](#save--сохранения) | PlayerPrefs, JSON-файлы, атрибут `[SaveField]` |
| 📜 [**Quest**](#quest--квесты) | Конфиги квестов, менеджер, цели, runtime-состояние |
| 📈 [**Progression**](Assets/Neoxider/Docs/Progression/README.md) | XP, уровни, unlock tree, perk tree и persistent progression |
| 🃏 [**Cards**](#cards--карточные-игры) | MVP-архитектура, покер, "Пьяница" |
| 🤖 [**StateMachine**](#statemachine--машина-состояний) | Код + No-Code, визуальный редактор |
| ✨ [**Animations**](#animations--анимации) | Float, Color, Vector3 анимации |
| 🎵 [**Audio**](#audio--звук) | AudioManager, микшер, random music |
| 🔌 [**Extensions**](#extensions--расширения-c) | 300+ extension-методов |
| 🛠️ [**Editor**](#editor--инструменты-редактора) | Окна настроек, поиск missing scripts, авто-билд |
| 🗺️ [**Level**](#level--уровни) | Менеджер уровней, карта |
| 🚶 [**NPC**](#npc) | Навигация NPC, патруль, chase и animator driver |
| 🌌 [**Parallax**](#parallax) | Параллакс-слои |
| 🔲 [**GridSystem**](#gridsystem) | Генерация сеток, origin-якорь, pathfinding, Match3/TicTacToe |
| 🏷️ [**PropertyAttribute**](#propertyattribute) | `[Button]`, `[GUIColor]`, inject-атрибуты |
| ⚡ [**Reactive**](#reactive) | Реактивные сериализуемые свойства `float`, `int`, `bool` |

---

## Модули

### Condition — No-Code условия

- **NeoCondition** — проверка любых полей/свойств компонентов и GameObject'ов через Inspector
- **AND/OR логика**, инверсия (NOT), несколько условий в одном компоненте
- **Source Mode** — чтение данных из компонентов или свойств самого GameObject (`activeSelf`, `tag`, `layer`)
- **Find By Name** — поиск объектов в сцене по имени с кешированием
- **Wait For Object + Prefab Preview** — настройка условий для префабов до спавна
- События: `OnTrue`, `OnFalse`, `OnResult(bool)`, `OnInvertedResult(bool)`

📖 [Документация →](Assets/Neoxider/Docs/Condition/NeoCondition.md)

### Tools — Инструменты

Самая большая категория — базовые "кирпичики" для построения игр:

| Подмодуль | Компоненты |
|-----------|-----------|
| **Components** | Counter, Health, ScoreManager, DialogueManager, Loot, TypewriterEffect, AttackSystem |
| **Input** | SwipeController, MouseInputManager, MouseEffect, MultiKeyEventTrigger |
| **Movement** | MovementToolkit, Follow, CameraConstraint, DistanceChecker |
| **Physics** | ExplosiveForce, ImpulseZone, MagneticField |
| **Spawner** | ObjectPool, Spawner, SimpleSpawner |
| **Managers** | Singleton, GM, EM, Bootstrap |
| **Random** | ChanceManager, ChanceSystemBehaviour |
| **Time** | Timer, TimerObject |
| **Debug** | ErrorLogger, FPS |
| **Draw** | Drawer (линии, коллайдеры) |
| **FakeLeaderboard** | Leaderboard, LeaderboardItem |
| **InteractableObject** | InteractiveObject, PhysicsEvents2D/3D |

📖 [Документация →](Assets/Neoxider/Docs/Tools/README.md) | [Physics →](Assets/Neoxider/Docs/Tools/Physics/README.md)

### UI — Интерфейс

- **UI** — менеджер UI-панелей (страниц)
- **ButtonScale / ButtonShake** — анимации кнопок
- **AnimationFly** — анимация "летящих" элементов
- **VisualToggle** — универсальный переключатель визуальных состояний
- **VariantView** — управление визуальными состояниями

📖 [Документация →](Assets/Neoxider/Docs/UI/README.md)

### Bonus — Бонусные системы

- **Slot** — слот-машина
- **WheelFortune** — колесо фортуны
- **Collection** — система коллекций
- **TimeReward** — награды по времени
- **LineRoulett** — линейная рулетка

📖 [Документация →](Assets/Neoxider/Docs/Bonus/README.md)

### Shop — Магазин

- **Shop** — центральный контроллер
- **ShopItem** — визуальное представление товара
- **Money** — система валюты
- **ButtonPrice** — кнопка с ценой
- **TextMoney** — UI отображение денег

📖 [Документация →](Assets/Neoxider/Docs/Shop/README.md)

### Save — Сохранения

- **SaveProvider** — статический API (как PlayerPrefs)
- **ISaveProvider** — интерфейс для кастомных провайдеров
- **SaveManager** — ядро системы
- **GlobalSave** — глобальное хранилище
- **SaveableBehaviour** — базовый класс для сохраняемых компонентов

📖 [Документация →](Assets/Neoxider/Docs/Save/README.md)

### Quest — Квесты

- **QuestConfig** — ScriptableObject квеста: ID, title, description, objectives, start conditions
- **QuestManager** — принятие квестов, учёт прогресса, события и Condition Context
- **QuestState** — runtime-состояние квеста и прогресс по целям
- **QuestNoCodeAction** — универсальный no-code bridge для UnityEvent
- **NotifyKill / NotifyCollect** — инкремент целей-счётчиков без ручного обхода состояний

📖 [Документация →](Assets/Neoxider/Docs/Quest/README.md)

### Cards — Карточные игры

- **MVP архитектура**: Model, View, Presenter
- **CardComponent, DeckComponent, HandComponent, BoardComponent**
- **Poker** подмодуль с комбинациями
- **DrunkardGame** — готовая игра "Пьяница"

📖 [Документация →](Assets/Neoxider/Docs/Cards/README.md)

### StateMachine — Машина состояний

- Код-реализация через `IState` интерфейс
- No-Code конфигурация через ScriptableObject
- Система предикатов для сложных условий переходов
- Визуальный редактор в Inspector

📖 [Документация →](Assets/Neoxider/Docs/StateMachine/README.md)

### Animations — Анимации

- **FloatAnimator** — анимация float значений
- **ColorAnimator** — анимация цветов
- **Vector3Animator** — анимация векторов

📖 [Документация →](Assets/Neoxider/Docs/Animations/README.md)

### Audio — Звук

- **AMSettings** — настройки аудио менеджера
- **RandomMusicController** — контроллер случайной музыки
- **SettingMixer** — управление микшером
- **AudioSimple** — упрощенная система воспроизведения

📖 [Документация →](Assets/Neoxider/Docs/Audio/README.md)

### Extensions — Расширения C#

300+ методов-расширений:
- **Transform** — позиция, ротация, масштаб, иерархия
- **Collections** — ForEach, Shuffle, GetRandom, FindDuplicates
- **String** — CamelCase, Truncate, Bold, Rainbow, Gradient
- **Random** — Chance, WeightedIndex, RandomColor
- **Coroutine** — Delay, WaitUntil, RepeatUntil
- **Color, Audio, Screen, Layout** и многое другое

📖 [Документация →](Assets/Neoxider/Docs/Extensions/README.md)

### Editor — Инструменты редактора

- **NeoxiderSettingsWindow** — окно глобальных настроек
- **FindAndRemoveMissingScripts** — поиск потерянных скриптов
- **TextureMaxSizeChanger** — массовое изменение текстур
- **SaveProjectZip** — резервные копии проекта
- **AutoBuildName** — автоматическое именование билдов
- **NeoUpdateChecker** — автопроверка обновлений через GitHub

📖 [Документация →](Assets/Neoxider/Docs/Editor/README.md)

### Level — Уровни

- **LevelManager** — менеджер уровней
- **LevelButton** — кнопка уровня
- **Map** — карта уровней

### NPC

- **NpcNavigation** — перемещение NPC с логикой патруля и преследования
- **NpcAnimatorDriver** — синхронизация состояния движения с Animator
- Используется вместе с movement/nav workflow и анимационными связками

📖 [Документация →](Assets/Neoxider/Docs/NPC/README.md)

### Parallax

- **ParallaxLayer** — параллакс с предпросмотром, зазорами, рандомизацией

### GridSystem

- **FieldGenerator** — генератор поля
- **FieldCell** — ячейка поля
- **FieldSpawner** — спавн объектов на поле
- **GridShapeMask + Origin** — произвольные формы и якорь построения поля
- **GridPathfinder** — pathfinding с диагностикой причин отсутствия пути
- **Match3 / TicTacToe** — прикладные игровые надстройки + demo-сцены

### PropertyAttribute

- `[Button]` — кнопки в Inspector из методов
- `[GUIColor]` — цветовое оформление полей
- `[RequireInterface]` — валидация интерфейсов
- Inject-атрибуты: `[GetComponent]`, `[FindInScene]`, `[LoadFromResources]`

📖 [Документация →](Assets/Neoxider/Docs/PropertyAttribute/README.md)

### Reactive

- **ReactivePropertyFloat** — сериализуемое реактивное значение `float`
- **ReactivePropertyInt** — сериализуемое реактивное значение `int`
- **ReactivePropertyBool** — сериализуемое реактивное значение `bool`
- **SetValueWithoutNotify / ForceNotify** — управление оповещениями при загрузке и ручной синхронизации

📖 [Документация →](Assets/Neoxider/Docs/Reactive/README.md)

---

## Топовые модули

- **NeoCondition** — No-Code условия: проверяйте любые данные и стройте логику целиком в Inspector
- **Counter** — универсальный счётчик с арифметикой, событиями и автосохранением
- **SpineController** — фасад для Spine с UnityEvent-обёртками и автозаполнением
- **ParallaxLayer** — параллакс с предпросмотром и автоматической переработкой тайлов
- **DialogueManager** — диалоги с персонажами, портретами и событиями на каждой реплике
- **ChanceManager** — декларативная система вероятностей для лута и рулеток
- **ObjectPool / Spawner** — расширяемый пул с волнами и случайным выбором префабов
- **MovementToolkit** — контроллеры движения (клавиатура, мышь, 2D/3D, follow-камеры)
- **Physics** — ExplosiveForce, ImpulseZone, MagneticField с кастомными режимами
- **Timer / TimerObject** — таймеры с паузой, повтором и событиями прогресса

---

## Установка через UPM

### Зависимости

| Пакет | Способ установки |
|-------|-----------------|
| **Input System** (`com.unity.inputsystem`) | Рекомендуется для модулей Input / swipe / нового ввода; в шаблоне проекта уже указан в `Packages/manifest.json`. В UPM-пакете Neoxider указан как зависимость для совместимости версий. |
| **UniTask** | Git URL: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask` |
| **DOTween** | [Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) |
| **DOTween Pro** (для NeoxiderPages) | Asset Store — обязателен для sample-модуля NeoxiderPages |

### Основной пакет

```
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
```

Window -> Package Manager -> **+** -> Add package from git URL.

Если нужна конкретная версия, добавьте тег в конец URL (например, `#5.5.2`):

```
https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider#5.5.2
```

### Ручная установка

Скопируйте папку `Assets/Neoxider` в ваш Unity-проект.

---

## Установка Demo Scenes и NeoxiderPages

После установки основного пакета через UPM, дополнительные модули доступны через **Package Manager**:

1. **Window -> Package Manager** -> найти **Neoxider Tools** (In Project)
2. В правой панели внизу — секция **Samples**
3. Нажать **Import** рядом с нужным модулем:
   - **Demo Scenes** — демо-сцены и примеры использования
   - **NeoxiderPages** — модуль страниц и экранов (PageManager, UIPage, UIKit), требуется **DOTween Pro**

Файлы копируются в `Assets/Samples/Neoxider Tools/<version>/`.

> Альтернативно: скачайте `.unitypackage` из [Releases](https://github.com/NeoXider/NeoxiderTools/releases)

**Быстрый вызов страниц:**

```csharp
UIKit.ShowPage("PageEnd");
// или
PM.I.ChangePageByName("PageEnd");
```

`PageSubscriber` автоматически ищет `PageId` по стандартным именам: `PageGame`, `PageWin`, `PageLose`, `PageEnd` (настраивается в Inspector).

---

## FAQ

**Можно использовать выборочно?** Да, импортируйте только нужные папки — зависимости указаны в документации каждого модуля.

**Есть примеры сцен?** Да, в папке `Demo` — минимальные сцены для каждого крупного модуля.

**Работает с 3D?** Большинство систем — да. Исключение: чисто 2D-решения вроде `ParallaxLayer`.

---

## Поддержка и вклад

Neoxider активно развивается. Нашли баг или хотите предложить модуль — открывайте issue/PR. Все изменения документируются в [Changelog](Assets/Neoxider/CHANGELOG.md).

Удачи в разработке!
