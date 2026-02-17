# Changelog

All notable changes to this project will be documented in this file.

## [5.8.9] - Unreleased

### Добавлено

- **TimeReward** — накопление наград и гибкий первый запуск:
  - `_rewardAvailableOnStart` (по умолчанию `false`): при отсутствии сохранения награда доступна сразу (true) или после полного кулдауна (false).
  - `_maxRewardsPerTake`: -1 = забрать все накопленные, 1 = одна за раз, N = не больше N за раз.
  - `GetClaimableCount()` — количество наград, доступных к выдаче.
  - `OnRewardsClaimed(int)` — событие с количеством выданных за один Take.
  - Интервал обновления таймера по умолчанию изменён на `updateTime = 0.2f`.
- **CooldownRewardExtensions** — утилиты для кулдаун-наград:
  - `GetAccumulatedClaimCount(DateTime, float, DateTime)` — число накопленных наград;
  - `CapToMaxPerTake(int, int)` — ограничение числа за один забор;
  - `AdvanceLastClaimTime(DateTime, int, float)` — сдвиг времени последней выдачи.

### Улучшено

- **TimeReward** — `TakeReward()` выдаёт до `GetClaimableCount()` наград за раз; при сохранении времени используется сдвиг на число выданных наград. Документация: полная схема работы (flowchart), таблица «механика → настройки», примеры.
- **Docs** — добавлена страница `CooldownRewardExtensions.md`, обновлён `TimeReward.md`, индекс `Extensions/README.md`.

## [5.8.8] - Unreleased

### Добавлено

- **Extensions/Time** — новые расширения для работы со временем:
    - `DateTimeExtensions`: `ToRoundTripUtcString`, `TryParseUtcRoundTrip`, `GetSecondsSinceUtc`, `GetSecondsUntilUtc`, `EnsureUtc`;
    - `TimeParsingExtensions`: `TryParseDuration` — парсинг SS, MM:SS, HH:MM:SS, DD:HH:MM:SS;
    - `TimeSpanExtensions`: `ToCompactString`, `ToClockString`.
- **TimeReward** — `GetFormattedTimeLeft`, `TryGetLastRewardTimeUtc`, `GetElapsedSinceLastReward`; настройки `_displayTimeFormat`, `_displaySeparator`.
- **Timer** — `Play()`, `SetRemainingTime(float)`, `SetProgress(float)`.
- **TimerObject** — `SetDuration(float newDuration, bool keepProgress = true)`.
- **TimeToText** — `TrySetFromString(string raw, string separator = null)`, опция `_allowNegative`.
- **PrimitiveExtensions.FormatTime** — перегрузка с `trimLeadingZeros` (например `01:05` → `1:05`).

### Улучшено

- **TimeReward** — интеграция с `DateTimeExtensions` и `TimeParsingExtensions`; `FormatTime(float, TimeFormat, string, bool)`.
- **Docs** — добавлены `TimeFormatting.md`, `DateTimeExtensions.md`, `TimeParsingExtensions.md`, `TimeSpanExtensions.md`; обновлены `TimeReward`, `Timer`, `TimerObject`, `TimeToText`, `PrimitiveExtensions`, `Tools/Time/README.md`, `Extensions/README.md`.

## [5.8.7] - Unreleased

### Добавлено

- **GridSystem** — крупное расширение модуля сеток:
    - добавлены состояния клетки: `IsEnabled`, `IsOccupied`, `ContentId`, `FieldCellFlags`;
    - добавлен shape pipeline: `GridType`, `GridShapeMask` (SO), ручные override (`DisabledCells`, `ForcedEnabledCells`,
      `BlockedCells`, `ForcedWalkableCells`);
    - добавлено origin-позиционирование поля относительно объекта: `Origin2D`, `OriginDepth`, `OriginOffset` (по
      умолчанию центр);
    - pathfinding вынесен в сервис `GridPathfinder` с `GridPathRequest`, `GridPathResult`, `NoPathReason`;
    - добавлены игровые надстройки:
        - `Neo.GridSystem.Match3` (`Match3BoardService`, `Match3MatchFinder`, `Match3TileState`);
        - `Neo.GridSystem.TicTacToe` (`TicTacToeBoardService`, `TicTacToeWinChecker`, `TicTacToeCellState`);
    - добавлены demo-сцены:
        - `~Samples/Demo/Scenes/GridSystem/GridSystemMatch3Demo.unity`
        - `~Samples/Demo/Scenes/GridSystem/GridSystemTicTacToeDemo.unity`
    - добавлены demo setup/UI скрипты для обеих сцен.
- **Extensions/NumberFormatExtensions** — новый универсальный форматтер чисел для `int`, `long`, `float`, `double`,
  `decimal`, `BigInteger`:
    - нотации: `Plain`, `Grouped`, `IdleShort`, `Scientific`;
    - режимы округления: `ToEven`, `AwayFromZero`, `ToZero`, `ToPositiveInfinity`, `ToNegativeInfinity`;
    - новый конфиг `NumberFormatOptions`;
    - extension API: `ToPrettyString(...)`, `ToIdleString(...)`.
- **Tools/Move** — новые контроллеры:
    - `PlayerController3DPhysics` — 3D контроллер через `Rigidbody` (движение, бег, прыжок, mouse-look, lock курсора);
    - `PlayerController2DPhysics` — 2D контроллер через `Rigidbody2D` (движение, бег, прыжок, coyote time, jump buffer,
      optional camera follow);
    - `CursorLockController` — отдельный скрипт для управления видимостью/блокировкой курсора и toggle по клавише;
    - `PlayerController3DAnimatorDriver` — отдельный драйвер анимации для 3D контроллера (idle/walk/run/jump +
      directional blend);
    - `PlayerController2DAnimatorDriver` — отдельный драйвер анимации для 2D контроллера (idle/walk/run/jump + BlendTree
      режимы HorizontalOnly/TwoAxis).

### Улучшено

- **GridSystem API docs** — добавлена/обновлена XML-документация (EN) для публичных методов и свойств во всех скриптах
  `Assets/Neoxider/Scripts/GridSystem/**`.
- **GridSystem docs** — полностью переработана `Docs/GridSystem.md`: архитектура, shape/passing rules, pathfinding,
  Match3/TicTacToe API, практические примеры и запуск demo.
- **PROJECT_SUMMARY** — обновлён раздел `GridSystem` с новыми runtime-файлами (`GridPathfinder`, `GridShapeMask`,
  `Match3/*`, `TicTacToe/*`).
- **Extensions** — точечные улучшения стабильности и производительности:
    - `NumberFormatExtensions.ApplySeparators` переписан на односканирующий алгоритм без каскадных `Replace`;
    - `RandomExtensions.GetRandomEnumValue` получил кеш значений enum;
    - `RandomExtensions.GetRandomWeightedIndex` валидирует отрицательные веса и нулевую сумму;
    - `StringExtension` получил `ToColorSafe(...)`, а `ToColor(...)` использует безопасный парсинг;
    - `PrimitiveExtensions.NormalizeToUnit/Denormalize` проверяют входные значения на `NaN/Infinity`;
    - уточнены null-контракты в XML-документации `TransformExtensions` и `EnumerableExtensions`.
- **SetText** — интегрирован с новым форматтером:
    - форматирование в `Set(float)` переведено на `NumberFormatOptions`;
    - добавлены настройки нотации/округления в Inspector;
    - добавлены `SetBigInteger(BigInteger)`, `SetBigInteger(string)`, `SetFormatted(...)`.
- **Docs** — обновлены `Docs/Tools/Text/README.md` и `Docs/Tools/Text/SetText.md` с описанием нового API форматирования.
- **Versioning** — повышена версия пакета до `5.8.7` (`package.json`, `Assets/Neoxider/README.md`, `Docs/README.md`,
  `PROJECT_SUMMARY.md`).
- **Tools/Move/CameraConstraint** — переработан:
    - источник bounds теперь строго выбирается из 3 вариантов: `SpriteRenderer` / `BoxCollider2D` / `BoxCollider`;
    - `constraintZ` по умолчанию выключен, чтобы компонент не сдвигал камеру по глубине без явного запроса;
    - улучшены вычисления bounds для perspective-камеры и обновлены debug gizmo;
    - добавлен `autoUpdateBounds` и улучшен runtime пересчет при изменении параметров камеры/экрана.
- **Tools/Move player controllers** — добавлена поддержка dual input backend:
    - по умолчанию используется New Input System (`AutoPreferNew`), при необходимости доступен Legacy Input Manager;
    - режим выбирается в контроллерах через `_inputBackend` (`AutoPreferNew` / `NewInputSystem` / `LegacyInputManager`).
- **Tools docs (Move)** — обновлены `README` и документация по `CameraConstraint`, добавлены страницы для
  `PlayerController3DPhysics`, `PlayerController2DPhysics`, `CursorLockController`.
- **InteractiveObject** — добавлен view-gate для клавиатурной интеракции:
    - `requireViewForKeyboardInteraction`, `minLookDot`, `requireDirectLookRay`;
    - клавиатурное взаимодействие можно ограничить только объектами в направлении взгляда;
    - добавлен режим `keyboardInteractionMode` (`ViewOrMouse` / `DistanceOnly`).
    - добавлен debug-луч проверки взаимодействия (`drawInteractionRayForOneSecond`, `interactionRayDrawDuration`) с
      цветовой индикацией результата.
- **Bonus/TimeReward** — расширен runtime API и сценарии запуска кулдауна:
    - добавлены публичные методы управления таймером: `StartTime`, `StopTime`, `PauseTime`, `ResumeTime`, `RestartTime`,
      `SetRewardAvailableNow`, `RefreshTimeState`, `SetAdditionalKey`;
    - добавлены статусные свойства: `IsTimerRunning`, `IsTimerPaused`, `IsRewardAvailable`, `RewardTimeKey`,
      `SaveTimeOnTakeReward`;
    - добавлены события: `OnTimerStarted`, `OnTimerStopped`, `OnTimerPaused`, `OnTimerResumed`;
    - добавлен режим ручного старта кулдауна: при `saveTimeOnTakeReward = false` время может сохраняться в `StartTime()`;
    - сохранение времени переведено на UTC round-trip формат (`"o"`) с поддержкой чтения legacy-значений.

## [5.8.5] - Unreleased

### Улучшено

- **TextMoney** — добавлены режимы отображения `Money`, `LevelMoney`, `AllMoney` (выбор через `_displayMode`), обновлена
  подписка на события под выбранный режим.
- **TextLevel / TextScore** — UI-режимы отображения переведены на enum (`Current/Max` и `Current/Best`) с сохранением
  обратной совместимости для старого поля `_best`.
- **Docs** — обновлена документация `Shop/TextMoney.md`, `Shop.md`, `Docs/README.md`, `PROJECT_SUMMARY.md` под новую
  логику отображения денег/уровня/счета и версию пакета.
- **Cards** — унифицирована визуальная раскладка карт между `DeckComponent`, `HandComponent` и `BoardComponent`:
    - добавлен общий `CardLayoutType` (`Fan`, `Line`, `Stack`, `Grid`, `Slots`, `Scattered`) и `CardLayoutCalculator`
      для переиспользуемого расчета позиций/поворотов;
    - добавлены `CardLayoutSettings` и `CardAnimationConfig` для централизованной настройки layout/анимаций;
    - `DeckComponent` получил визуальный pipeline: `BuildVisualStackAsync`, `ShuffleVisualAsync`, `DealToHandAsync` +
      `[Button]`-обертки и события (`OnVisualStackBuilt`, `OnShuffleVisualStarted`, `OnCardDealt` и др.);
    - `BoardComponent` расширен режимом `BoardMode` (`Table/Beat`) и использует тот же `CardLayoutType`, включая
      случайный режим `Scattered` для "биты";
    - добавлены/обновлены enum: `CardLayoutType`, `ShuffleVisualType`, `StackZSortingStrategy`, `BoardMode`;
    - обновлены docs: `Docs/Cards/DeckComponent.md`, `Docs/Cards/BoardComponent.md`.

## [5.8.4] - Unreleased

### Исправлено

- **NeoxiderPages** — удалена встроенная копия DOTween Pro (`Runtime/Plugins/DOTweenPro`). При импорте сэмпла в проект,
  где DOTween Pro уже установлен, возникала ошибка CS0433 (тип `DOTweenAnimation` определён в двух сборках). DOTween Pro
  теперь является внешней зависимостью.

## [5.8.3]

### Исправлено

- **UPM Samples** — исправлены пути сэмплов в `package.json`: `"path"` теперь указан как `Samples~/Demo` и
  `Samples~/NeoxiderPages` (согласно документации Unity). Ранее пути были без префикса `Samples~/`, из-за чего Unity не
  находил файлы при импорте (папка оставалась пустой, размер 0 KB).

## [5.8.2]

### Исправлено

- **UPM Samples** — устранены ошибки CS0101 (duplicate definition) при импорте сэмплов Demo Scenes и NeoxiderPages.
  Сэмплы перенесены в папку `Samples~`: Unity не компилирует её содержимое в пакете, поэтому после Import классы (
  BtnChangePageEditor, PMEditor и др.) определяются один раз в проекте.

---

## [5.8.1] - 2025-02-06

- Обновление версии и ссылок в документации.

## [5.8.0] - 2025-02-06

### Добавлено

- **UPM Samples** — Demo Scenes и NeoxiderPages можно устанавливать через Package Manager (кнопка Import в секции
  Samples). Папка `Samples` в пакете; при импорте файлы копируются в `Assets/Samples/Neoxider Tools/<version>/`.

### Улучшено

- **NeoxiderPages (v1.1.0)** — универсальный поиск PageId по всему проекту:
    - Режимы Buttons и Dropdown в PM, UIPage и BtnChangePage показывают все PageId из любых папок (в т.ч. после импорта
      Sample в `Assets/Samples/...`).
    - Создание новых PageId (Generate & Assign, Generate Default PageIds, Reset на BtnChangePage) использует
      предпочитаемую папку: существующая папка с PageId или `Assets/NeoxiderPages/Pages`.

---

## [5.7.0] - Unreleased

### Добавлено

- **NeoCondition** — No-Code система условий (новый модуль `Neo.Condition`)
    - Проверяет значения полей/свойств любых компонентов через Inspector без кода
    - Автопоиск компонентов и полей через dropdown в кастомном редакторе
    - AND/OR логика, инверсия (NOT) на каждое условие
    - Режимы проверки: Manual, EveryFrame, Interval
    - Поддерживаемые типы: int, float, bool, string
    - События: OnTrue, OnFalse, OnResult(bool), OnInvertedResult(bool)
    - Only On Change — вызов событий только при изменении результата
    - Play Mode: отображение текущего значения и результата в Inspector
    - **Source Mode** — выбор источника данных:
        - `Component` — чтение полей/свойств компонентов (по умолчанию)
        - `GameObject` — чтение свойств самого объекта: activeSelf, activeInHierarchy, isStatic, tag, name, layer
    - **Find By Name** — поиск целевого GameObject в сцене по имени (`GameObject.Find`):
        - Результат кешируется пока объект жив
        - Автоматический повторный поиск при уничтожении объекта
        - Preview найденного объекта в Edit Mode для настройки
        - **Wait For Object** — ожидание спавна объекта без Warning (для префабов)
        - **Prefab Preview** — ссылка на префаб из Project для настройки компонентов/свойств до спавна (только Editor)
    - **Check On Start** — по умолчанию `true` (ранее `false`)
    - **Защита от null** — безопасная обработка уничтоженных объектов/компонентов:
        - Однократные Warning-логи (не спамит в EveryFrame)
        - Автоматический сброс кеша, условие возвращает `false`
        - Ошибка в одном условии не ломает остальные
    - **Визуализация** — цветовая полоска в Inspector: голубая (Component), жёлтая (GameObject), зелёная (Find By Name),
      красная (NOT)
    - Демо-скрипты: `ConditionDemoUI`, `ConditionDemoSetup`, `HealthTextDisplay` (используют существующие `Health` и
      `ScoreManager`)
- **Counter** — универсальный счётчик (Int/Float), Add/Subtract/Multiply/Divide/Set, Send по Payload, события по типу,
  сохранение по ключу

### Улучшено

- **[Button] атрибут** — унифицирован: убраны `#if ODIN_INSPECTOR` блоки из 43 файлов. Теперь везде используется единый
  `[Button]` (Neo.ButtonAttribute), который работает и с Odin Inspector, и без него
- **MagneticField** — Toggle теперь bool-галочка, работает с любым режимом (Attract, Repel, ToTarget, ToPoint,
  Direction)
- **MagneticFieldEditor** — наследуется от CustomEditorBase (инспектор в стиле Neo), исправлен serializedObject в
  OnSceneGUI
- **NeoUpdateChecker** — автоматическая проверка обновлений раз в 10 минут, ручная кнопка ⟳ с кулдауном 10 секунд;
  корректная обработка GitHub API rate limit (403); фоллбек поиска package.json; логирование всех этапов проверки
- **CustomEditorBase** — версия и статус обновления всегда отображаются в инспекторе; ошибки показываются оранжевым
  цветом
- **README.md** — полностью переписан: быстрый старт, таблица модулей, примеры использования, установка через UPM
- **Docs/README.md** — добавлен полный индекс модулей и подмодулей Tools с ссылками на документацию
- **PROJECT_SUMMARY.md** — исправлены дубли (Shapes.cs, Enums.cs), разделена склеенная строка Counter/Loot
- **package.json** — добавлено поле `unityRelease`, обновлены keywords (no-code, state-machine)

### Исправлено

- **SingletonCreator** — исправлен CS0108 warning (`title` hides inherited member)
- **GM.set_State** — исправлен NullReferenceException при обращении к `EM.I` до инициализации (добавлен null-conditional
  `?.`)

---

## [5.5.2] - Unreleased

### Добавлено

- **Tools/View/Selector**: рандомный выбор
    - `SetRandom()`
    - Настройки: `_useRandomSelection`, `_useNextPreviousAsRandom`
- **NeoxiderPages (Neo.Pages)**: опциональный модуль PageManager в `Assets/NeoxiderPages/`
    - 2 asmdef: `Neo.Pages` (runtime) и `Neo.Pages.Editor`
    - Документация: `Assets/NeoxiderPages/Docs/README.md`
- **Level/TextLevel**: UI вывод текущего/лучшего уровня (на базе `Neo.Tools.SetText`)
- **Tools/Components/TextScore**: UI вывод текущего/лучшего счета (на базе `Neo.Tools.SetText`)

### Улучшено

- **Tools/View/Selector**: автоподхват дочерних объектов включен по умолчанию (`_autoUpdateFromChildren = true`)

---

## [5.4.2] - Unreleased

### Добавлено

- **Physics/MagneticField**: новый режим `Direction` (притяжение по направлению)
    - Параметры: `direction`, `directionIsLocal`, `directionGizmoDistance`
    - API: `SetDirection(Vector3 newDirection, bool local = true)`
    - Scene View handle: редактирование точки направления (конец вектора)
- **Physics/MagneticField**: Scene View handle для `ToPoint` (перемещение `targetPoint` прямо в сцене)
- **Tools/Time/TimerObject**: рандомная длительность для `looping`
    - `useRandomDuration` работает и без `looping` (на старте), а при `looping` — на каждый цикл
    - Выбор `duration` в диапазоне [`randomDurationMin`, `randomDurationMax`]
- **Tools/Time/TimerObject**: режим `infiniteDuration`
    - Время увеличивается без максимума, прогресс/процент не обновляются
    - Автоматически отключает `looping` и `useRandomDuration` (в `OnValidate`)

### Исправлено

- **Physics/MagneticField**: режимы `ToTarget`/`ToPoint` больше не “переезжают” к цели
    - Радиус поля считается вокруг магнита, а цель/точка/направление используется как направление силы
- **Docs**: обновлена документация `MagneticField.md` и `Physics/README.md`
- **Tools/View/Selector**: порядок методов приведён к `SetFirst()` затем `SetLast()`

### Удалено

- **Tools/Time/TimerObject**: удалены Auto Actions (no-code автодействия на завершении)

---

## [5.4.0] - Unreleased

### Добавлено

- **NPC**: новая модульная система навигации (подход «как Cinemachine»)
    - `NpcNavigation` — хост‑компонент с галочкой активен NPC и контекстным меню удаления модулей
    - Модули: `NpcNavAgentModule`, `NpcFollowTargetModule` (с `movementBounds`), `NpcPatrolModule` (points/zone),
      `NpcAggroFollowModule` (Combined), `NpcAnimationModule`
    - Core‑логика модулей без `MonoBehaviour`/корутин
- **AiNavigation**: помечен как устаревший, рекомендуется переход на `NpcNavigation`
- **AiNavigation**: Добавлена поддержка зоны патрулирования через BoxCollider
    - Новое поле `patrolZone` - можно задать BoxCollider для случайного патрулирования
    - Агент будет выбирать случайные точки внутри зоны и перемещаться к ним
    - Работает в режимах Patrol и Combined
    - Если задан patrolZone, то массив patrolPoints игнорируется
    - Методы `SetPatrolZone(BoxCollider)` и `ClearPatrolZone()` для управления в рантайме
    - Свойство `UsesPatrolZone` для проверки режима
    - Визуализация зоны через Gizmos (полупрозрачный зелёный куб)

### Исправлено

- **Physics модуль**: Исправлены ошибки компиляции с атрибутами Odin Inspector
    - Добавлены директивы `using Sirenix.OdinInspector` в компоненты ExplosiveForce, ImpulseZone, MagneticField

### Улучшено

- **Physics модуль**: Рефакторинг всего кода модуля
    - Улучшена структура и читаемость кода
    - Оптимизированы внутренние процессы

---

## [5.3.5] - Unreleased

### Добавлено

- **InteractiveObject**: Добавлена проверка препятствий (стен) между объектом и точкой проверки
    - Опция `checkObstacles` (по умолчанию включена) - проверяет наличие препятствий через raycast
    - Опция `obstacleLayers` - настройка слоев, которые блокируют взаимодействие
    - Предотвращает взаимодействие через стены и другие препятствия
    - Работает с 2D и 3D коллайдерами
    - Автоматически исключает коллайдер самого объекта из проверки
- **InteractiveObject**: Исправлена работа клавиатурного ввода для событий Down/Up
    - Теперь `onInteractDown` вызывается при нажатии клавиши, если объект в радиусе взаимодействия
    - Ранее требовалось наведение мыши (isHovered), что блокировало клавиатурное взаимодействие
- **Модуль Physics**: Новый модуль с интересными физическими компонентами
    - **ExplosiveForce**: Взрывная сила, которая толкает объекты в радиусе
        - Режимы активации: при старте, с задержкой, вручную
        - Фильтрация по слоям, опциональное добавление Rigidbody
        - Затухание силы по расстоянию (линейное/квадратичное)
        - Режимы силы: AddForce, AddExplosionForce
        - События OnExplode и OnObjectAffected
    - **ImpulseZone**: Зона импульса, применяющая импульс при входе объекта
        - Различные направления импульса (от центра, к центру, по направлению, кастомное)
        - Фильтрация по слоям и тегам
        - Одноразовое или многократное срабатывание с cooldown
        - События OnObjectEntered и OnImpulseApplied
    - **MagneticField**: Магнитное поле для притяжения/отталкивания объектов
        - Режимы: притяжение к себе, к Transform цели, к точке в пространстве, отталкивание, переключение
        - Постоянное воздействие с затуханием по расстоянию
        - События при входе/выходе объектов из поля и при переключении режима
        - Настраиваемые интервалы для режима переключения
    - Все компоненты поддерживают:
        - Фильтрацию по слоям (LayerMask)
        - Опциональное добавление Rigidbody на объекты без физики
        - UnityEvent для интеграции с другими системами
        - Визуализацию в редакторе через Gizmos
        - Публичный API для программного управления
        - Полную XML документацию

---

## [5.3.4] - Unreleased

### Добавлено

- **Система провайдеров сохранения (SaveProvider)**: Новая профессиональная система сохранения данных
    - Статический класс `SaveProvider` с API аналогичным PlayerPrefs
    - Интерфейс `ISaveProvider` для создания собственных провайдеров
    - Реализованные провайдеры: `PlayerPrefsSaveProvider` (по умолчанию) и `FileSaveProvider` (JSON файлы)
    - ScriptableObject `SaveProviderSettings` для настройки через Inspector
    - Автоматическая инициализация с PlayerPrefs по умолчанию
    - Поддержка событий (OnDataSaved, OnDataLoaded, OnKeyChanged)
    - Расширения для работы с массивами
- **VisualToggle**: Полностью переработан и улучшен
    - Добавлена поддержка UnityEvent (On, Off, OnValueChanged)
    - Добавлены кнопки в Inspector для тестирования
    - Улучшена интеграция с Toggle компонентом
    - Добавлена защита от рекурсии при работе с Toggle
    - Добавлена опция `setOnAwake` для автоматического вызова событий текущего состояния при старте
    - Расширенное API с методами Toggle(), SetActive(), SetInactive()
    - Полная XML документация
    - Свойство IsActive для получения/установки состояния

### Улучшено

- **SaveManager**: Теперь использует систему провайдеров вместо прямых вызовов PlayerPrefs
- **GlobalSave**: Интегрирован с системой провайдеров для единообразной работы
- **Все компоненты сохранения**: Рефакторинг для использования SaveProvider
    - Money, ScoreManager, TimeReward, Collection, Box, Map, Leaderboard, Shop
- **VisualToggle**: Теперь объединяет возможности ToggleView и старого VisualToggle
    - Поддержка множественных Image, цветов, текста и GameObject'ов
    - Автоматическое сохранение начальных значений
    - Улучшенная обработка событий

### Удалено

- **ToggleView**: Удален, полностью заменен улучшенным VisualToggle
    - Все возможности перенесены в VisualToggle
    - StarView обновлен для использования VisualToggle

---

## [5.3.3] - Unreleased

### Улучшено

- **KeyboardMover**: Поддержка 3D - выбор плоскости (XY/XZ/YZ)
- **MouseMover2D**: Расширенный AxisMask для 3D (XZ, YZ, Z)
- **Follow**: Полная переработка
    - Режимы сглаживания: MoveTowards, Lerp, SmoothDamp, Exponential
    - Distance Control, Deadzone, публичный API
- **CameraConstraint**: Типы границ (SpriteRenderer/Collider2D/Collider/Manual), 3D камеры
- **DistanceChecker**: FixedInterval режим, ContinuousTracking, оптимизация
- **InteractiveObject**: useMouseInteraction, useKeyboardInteraction, контроль дистанции
- **AiNavigation**: Патрулирование и Combined режим
    - walkSpeed/runSpeed, SetRunning()
    - Режимы: FollowTarget, Patrol, Combined
    - Combined: aggroDistance, maxFollowDistance
    - События патруля, публичный API

### Исправлено

- **Follow**: Некорректный Lerp, Time.smoothDeltaTime, проверки лимитов
- **AiNavigation**: Краш "GetRemainingDistance", проверки isOnNavMesh
- **FindAndRemoveMissingScriptsWindow**: Утечка памяти

### Удалено

- **MoveController**: Неиспользуемый компонент

### Производительность

- sqrMagnitude в DistanceChecker, AiNavigation (~20-30%)
- Кеширование хешей Animator в AiNavigation (~10-15%)

---

## [5.3.2]

---

## [5.3.1]

### Добавлено

- **DrunkardGame**: Готовая игра "Пьяница" с no-code настройкой
    - События счёта карт, победы, раундов, войны
    - Поддержка HandComponent и BoardComponent
    - Параметр `Player Goes First` для порядка ходов
    - Анимация возврата карт в руку
- **DeckConfig**: `GameDeckType` — выбор количества карт в игре (36/52/54)
- **HandComponent**: Событие `OnCardCountChanged(int)`, методы `DrawFirst()/DrawRandom()`, параметр `Add To Bottom`
- **CardComponent**: Методы `UpdateOriginalTransform()`, `ResetHover()`
- **TypewriterEffectComponent**: Метод `Play()` теперь опционально берёт текст из TMP_Text если параметр пустой
- **CustomEditor**: Анимированная радужная линия слева (настройки через Tools → Neoxider → Visual Settings)

### Улучшено

- **CardComponent**: Hover Scale как дельта (0.1 = +10%)
- **HandComponent**: Симметричная раскладка веером, анимация добавления карт

### Исправлено

- **CardComponent**: Искажение масштаба после FlipAsync, hover при частых кликах
- **HandComponent**: NullReferenceException при вызове до Awake
- **DrunkardGame**: Карты войны не исчезают, остаются на столе
- **CustomEditorBase**: OnValidate только в Edit Mode

---

## [5.3.0]

### Добавлено

- **Cards**: Новый модуль карточных игр в MVP архитектуре
    - `CardData` — неизменяемая структура карты с поддержкой сравнения и козырей
    - `DeckModel`, `HandModel` — модели колоды и руки
    - `CardView`, `DeckView`, `HandView` — визуальные компоненты с анимациями DOTween
    - `CardPresenter`, `DeckPresenter`, `HandPresenter` — презентеры MVP
    - `DeckConfig` — ScriptableObject со спрайтами по мастям и кастомным редактором
    - No-code компоненты: `CardComponent`, `DeckComponent`, `HandComponent`, `BoardComponent`
    - Типы колод: 36, 52, 54 карты
    - Раскладки руки: Fan, Line, Stack, Grid
    - Методы сравнения: `Beats()`, `CanCover()` для игры «Дурак»
    - **Poker**: подмодуль покерных комбинаций
        - `PokerCombination` — от HighCard до RoyalFlush
        - `PokerHandEvaluator` — определение комбинации из 5-7 карт
        - `PokerRules` — сравнение рук, определение победителей, Texas Hold'em

---

(Previous versions...)
