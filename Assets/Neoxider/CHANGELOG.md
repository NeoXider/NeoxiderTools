# Changelog

All notable changes to this project will be documented in this file.

## [7.2.0] - 2026-02-23

### Tools / Input

- **KeyInputCompat** — новый статический класс совместимости со старой (Input Manager) и новой (Input System Package) системой ввода: `GetKeyDown(KeyCode)`, `GetKeyUp(KeyCode)`, `GetKey(KeyCode)`. Сначала вызывается legacy Input; при `InvalidOperationException` используется новая система через рефлексию. Используется в MultiKeyEventTrigger, InventoryHand, InventoryDropper, CursorLockController — все они работают при любой активной системе ввода.
- **MultiKeyEventTrigger** — опция **Debug** (bool): при включении в консоль пишется лог о нажатой клавише.

### Tools / Inventory

- **HandView** — новый компонент «вьюшка руки» на префабе предмета (тот же, что WorldDropPrefab): смещение позиции (**Position Offset**), поворота (**Rotation Offset**) и базовый масштаб (**Scale In Hand**). InventoryHand при отображении ищет HandView на экземпляре и применяет эти значения первыми; затем применяется общий масштаб руки (Fixed или Relative).
- **InventoryHand** — масштаб в руке: режим задаётся enum **HandScaleMode** (Fixed / Relative); по умолчанию **Relative** (дельта). Итоговый масштаб = базовый (из HandView или 1) × handScale. Разрешён **индекс слота -1** (ничего не в руке): при включённом **Allow Empty Slot** и **Allow Empty Effective Index** у Selector допустимы SetSlotIndex(-1) и переход на -1 через Selector.Previous() с 0; в руке ничего не отображается, EquippedItemId = −1.
- Документация: HandView.md, обновлены InventoryHand.md, Tools/Inventory/README.md, UsefulComponents.md.

### Tools / View

- **Selector** — при режиме «только Count» (без списка Items) в UpdateSelection добавлена привязка _currentIndex к допустимым границам (GetCurrentBounds), чтобы Next/Previous стабильно работали при виртуальном Count.

## [7.0.0] - 2026-02-23

### BREAKING: мажорная версия — подписки и события

Мажорная версия из‑за **breaking changes**:

- **Подписки сломаются:** во многих компонентах удалены отдельные события (UnityEvent&lt;float&gt;, UnityEvent&lt;int&gt;, UnityEvent&lt;bool&gt;). Вместо них используются реактивные поля (ReactivePropertyFloat, ReactivePropertyInt, ReactivePropertyBool). Подписка теперь только через поле компонента и его `.OnChanged` (например `counter.Value.OnChanged`, `money.Money.OnChanged`). Свойства вида `OnValueChanged => Value.OnChanged` не добавлялись — подписывайтесь напрямую на реактивное поле.
- **Удалённые скрипты (запланировано на отдельный шаг):** устаревшие TimeReward, AiNavigation, WheelFortune, UIReady планируются к удалению; префабы и сцены нужно будет обновить. См. `Docs/Plan_RemoveDeprecatedScripts.md`.

Необходимо в проектах: перенести подписки на новые реактивные поля (`.OnChanged`) и при появлении удаления скриптов — заменить или убрать использование TimeReward, AiNavigation, WheelFortune, UIReady.

**Переименования (breaking):** в **ScoreManager** — целочисленные значения счётчиков через свойства `ScoreValue`, `BestScoreValue`, `TargetScoreValue` (реактивы: `Score`, `BestScore`, `TargetScore`, `Progress`, `CountStarsReactive`). В **Health** — текущее HP через `HpValue` и реактивы `Hp`, `HpPercent`. В **Drawer** — число через `DistanceValue`, в **TypewriterEffectComponent** — через `ProgressValue`.

### Реактивные поля (ReactiveProperty)

- Добавлены **ReactivePropertyFloat**, **ReactivePropertyInt**, **ReactivePropertyBool** (API в стиле R3: CurrentValue, Value, OnChanged, OnNext, SetValueWithoutNotify, ForceNotify, AddListener, RemoveListener, RemoveAllListeners). Отдельная сборка **Neo.Reactive**: папка `Scripts/Reactive/`, файл `ReactiveProperty.cs`, пространство имён `Neo.Reactive`. Подключение: ссылка на сборку Neo.Reactive и `using Neo.Reactive;` в скриптах, использующих реактивные типы.
- **Counter** переведён на реактивное поле `Value` (ReactivePropertyFloat); типизированные события OnValueChangedInt/Float и Send сохранены.
- Множество компонентов переведены на реактивные поля вместо пары «поле + UnityEvent»: ToggleObject, Money, LightAnimator, CooldownReward, Evade, TimerObject, Drawer, TypewriterEffectComponent, AMSettings, VisualToggle, FloatAnimator, DistanceChecker, MagneticField, ItemCollection, Box, Health, ScoreManager, HandComponent, Selector, FakeLoad, TicTacToeBoardService, DrunkardGame, LevelManager, UI (Simple), NpcNavigation, NeoCondition, LeaderboardItem, TextLevel, LevelButton, DialogueData, TimeToText, SpinController (по плану). Подписка везде через соответствующее реактивное поле и его `.OnChanged`.

## [6.0.7] - 2026-02-19

### Condition — NeoCondition: вызов методов с аргументом (int/float/string)

- **ConditionEntry** — добавлена поддержка вызова методов с одним параметром в дополнение к свойствам и полям.
  - Новый enum `ArgumentKind { Int, Float, String }` и поля: `_isMethodWithArgument`, `_propertyArgumentKind`, `_propertyArgumentInt`, `_propertyArgumentFloat`, `_propertyArgumentString`.
  - В выпадающем списке Property отображаются методы с одним параметром (int/float/string) и возвращаемым типом int/float/bool/string, с подписью вида `GetCount (int) → Int [method]`.
  - Под полем Property при выборе метода отображается поле аргумента (Argument int/float/string).
  - Аналогичная поддержка для **Other Object** (сравнение с переменной-методом): `_otherIsMethodWithArgument`, `_otherPropertyArgumentKind`, `_otherPropertyArgumentInt/Float/String`.
- **NeoConditionEditor** — в `DrawPropertyDropdown` добавлен сбор методов с одним параметром, отрисовка полей аргумента по типу; для Other Object — тот же dropdown и поля аргументов; сброс флагов/аргументов при смене компонента; в `ResetConditionEntry` сброс всех новых полей; в Play Mode — preview значения для методов с текущим аргументом.
- **Обратная совместимость**: старые условия без флагов работают как раньше (только свойства/поля).
- **Документация**: в NeoCondition.md добавлены описание Property/Argument, пример 8 «Проверка количества предмета по id» (InventoryComponent.GetCount(int), Argument = itemId).

### Tools / Inventory — кнопки в инспекторе для тестирования

- **InventoryComponent** — добавлены атрибуты `[Button]` для быстрой проверки в Inspector:
  - Add: «Add 1», «Add N» (AddItemById, AddItemByIdAmount), «Add 1 (Selected Id)» — по выбранному Condition Helper id.
  - Remove: «Remove 1», «Remove N», «Remove 1 (Selected Id)».
  - Drop: «Drop Selected», «Drop By Id», «Drop First», «Drop Last».
  - «Clear», «Save», «Load» — с явными подписями кнопок.
- Добавлен `using Neo` для атрибута `[Button]`.

## [6.0.6] - 2026-02-19

### Editor — Presets в окне Create Neoxider Object

- В окне **GameObject → Neoxider → Create Neoxider Object...** в начале списка добавлена секция **Presets (готовые префабы)**.
- Категории: System (System Root), Combat (Simple Weapon, Bullet), Player (First Person Controller), Interaction (Interactive Sphere, Trigger Cube, Toggle Interactive).
- Секция и подкатегории сворачиваются; по клику создаётся экземпляр префаба в сцене.
- В `NeoxiderPresetCreateMenu` добавлен публичный метод `CreatePreset(string relativePrefabPath)` для вызова из окна и других редакторских скриптов.

### Editor — создание спрайта из префаба

- **Tools → Neoxider → Create Sprite from Prefab...** — окно выбора префаба и сохранения его превью как Sprite-ассет (диалог выбора пути в проекте, PNG + TextureImporter Sprite).
- Утилита `PrefabToSpriteUtility`: копирование не-readable превью через RenderTexture, сохранение PNG, импорт как Sprite.

### Shop — исправления и устойчивость

- **Money** — namespace приведён к `Neo.Shop`; в `Save()` после SetFloat вызывается `SaveProvider.Save()`; в ChangeMoneyEvent/ChangeLevelMoneyEvent/SetText добавлены проверки массивов и элементов на null.
- **Shop** — подписки на кнопки: делегаты хранятся в списке, отписка выполняется тем же экземпляром (RemoveListener корректно снимает подписки). Проверка границ id в Buy(id), Visual(), VisualPreview(); Load() при null/пустых _prices инициализирует массив; в Save() вызывается SaveProvider.Save(). Поле переименовано в `moneySpendSource` с `[FormerlySerializedAs("IMoneySpend")]`.
- **TextMoney** — отложенная инициализация: при отсутствии Money.I используется WaitWhile (как в TextScore), затем Init() с RemoveListener перед AddListener.
- **ButtonPrice** — в SetButtonText/SetPriceText добавлены проверки массивов _textButton/_textPrice на null.
- Документация: Money.md, Shop.md, SHOP_IMPROVEMENTS.md обновлены.

## [6.0.5] - Unreleased

### Tools / Inventory — новый модуль инвентаря и подбора предметов

- Добавлен новый подмодуль **Inventory** (`Neo.Tools.Inventory`) с разделением на Data/Core/Runtime/UI.
- **Core API**: `InventoryManager`, `InventoryEntry`, `InventorySaveData` — чистая C# логика инвентаря без MonoBehaviour (Add/Remove/Has/GetCount, лимиты, snapshot/load).
- **Data**: `InventoryItemData` (id, имя, иконка, maxStack, category) и `InventoryDatabase` (lookup/валидация id, источник maxStack).
- **No-Code Runtime**:
  - `InventoryComponent` — события `OnItemAdded`, `OnItemRemoved`, `OnItemCountChanged`, `OnCapacityRejected`, `OnInventoryChanged`, сохранение/загрузка через SaveProvider, свойства для NeoCondition (`TotalItemCount`, `UniqueItemCount`, `SelectedItemCount`, `IsEmpty`).
  - `PickableItem` — подбор по trigger 2D/3D и ручной `Collect()` через UnityEvent, фильтр по тегу, пост-обработка (disable colliders / deactivate / destroy).
  - `InventoryPickupBridge` — мост для вызовов подбора из UnityEvent (`InteractiveObject`, `PhysicsEvents` и др.).
- **UI binders**: `InventoryItemCountText`, `InventoryTotalCountText` переведены на **TextMeshPro** (`TMP_Text`).
- **UI Views**: добавлены `InventoryView` и `InventoryItemView` (режим auto-spawn префаба и manual-режим), с опциональными полями иконки/названия/количества.
- **Drop module**: добавлен `InventoryDropper` как отдельный подключаемый компонент дропа (делегирование из InventoryComponent), с опциями авто-добавления `Rigidbody`/`Rigidbody2D`, коллайдера, импульса и автоконфигурации `PickableItem`.
- `InventoryComponent` переведен на базовый `Singleton<InventoryComponent>` с поддержкой multi-instance через `Set Instance On Awake`.
- **Initial state**: добавлен `InventoryInitialStateData` и режимы загрузки `UseSaveIfExists`, `MergeSaveWithInitial`, `InitialOnlyIgnoreSave`.
- **Runtime events/API**: в `InventoryComponent` добавлен `OnBeforeLoad` и `GetSnapshotEntries()` для UI и внешних систем.
- **InventoryView**: расширен выбор источника данных (`DatabaseItems`, `SnapshotItems`, `Hybrid`), подписка на `OnLoaded` и опциональный refresh на следующий кадр для корректного стартового отображения.
- **Prefab preview/icon fallback**: добавлен extension `PrefabPreviewExtensions` (`GetPreviewTexture`, `GetPreviewSprite`); если у `InventoryItemData` не задан `Icon`, он автоматически берется из `WorldDropPrefab` (sprite/preview).
- **InventoryDropper input**: дроп по умолчанию на клавишу `G`, master bool `CanDrop`, и дополнительные методы `DropByIdOne`, `DropConfiguredById`, `SetDropEnabled`, `SetDropItemId`.
- Документация обновлена: добавлены `InventoryDropper.md`, `InventoryView.md`; индексы `Docs/Tools/README.md` и `Docs/README.md` обновлены.

### Editor / Create — готовые preset-префабы в меню

- Добавлен новый editor-скрипт `NeoxiderPresetCreateMenu` с разделом **GameObject → Neoxider → Presets** (в верхней части меню).
- В Presets доступны быстрые готовые сборки из префабов:
  - System Root (`--System--`)
  - Player (`Player (First Person Controller)`)
  - Combat: `Simple Weapon`, `Bullet`
  - Player: `First Person Controller`
  - Interaction: `Interactive Sphere`, `Trigger Cube`, `Toggle Interactive`
- Логика сделана отдельной категорией **Presets** (а не смешана с компонентами), чтобы не перегружать динамический список `Create Neoxider Object...` и держать готовые сборки в одном месте.

### Tools / Move — недоработки и улучшения модуля

- **UniversalRotator** — `using UnityEditor` обёрнут в `#if UNITY_EDITOR` (исправление для билдов).
- **ScreenPositioner** — проверка `_targetCamera == null` в ApplyScreenPosition с логом; режим «use screen position» использует `_screenEdge` вместо жёсткого BottomLeft; опция `_updateEveryFrame` и обновление в LateUpdate для динамического размещения; поля унифицированы в `[SerializeField] private`.
- **DistanceChecker** — гистерезис (`hysteresisOffset`, пороги approach/depart); настраиваемый `continuousEventThreshold` для onDistanceChanged; метод `SetCurrentObject(Transform)`; пересчёт порогов в Awake/OnValidate/SetDistanceThreshold.
- **Follow** — опциональный автопоиск цели по тегу (`findTargetByTag`, `targetTag`); событие `onTargetLost` при потере цели; тултип для `activationDistance`; публичные геттеры `GetFollowPosition()`, `GetFollowRotation()`.
- **AdvancedForceApplier** — события `OnApplyFailed` (при отсутствии Rigidbody) и `OnApplyForce`; опция `clampSpeedEveryFixedUpdate`; публичные методы `SetTarget(Transform)`, `SetDirectionMode(DirectionMode)`, `SetCustomDirection(Vector3)`.
- **CameraConstraint** — событие `onConstraintFailed` и однократный `Debug.LogWarning` при ошибке инициализации; `SetBoundsSource(Object)` возвращает `bool`.
- **CameraRotationController** — выбор кнопки мыши (`mouseButton`) и опциональный модификатор (`modifierKey`); отдельный множитель `mouseSensitivity`; события `onRotateStart`, `onRotateEnd`.
- **CursorLockController** — режимы курсора: **LockAndHide**, **OnlyHide**, **OnlyLock** (enum `CursorStateMode`); в документации — описание режимов и использование с New Input System (вызов `SetCursorLocked`/`ToggleCursorState` из callback).
- **PlayerController2DPhysics / PlayerController3DPhysics** — события `OnMoveStart`, `OnMoveStop` при начале/окончании движения; при `_groundCheck == null` в Awake выводится однократный `Debug.LogWarning`; `Teleport` уже присутствовал в обоих.
- **KeyboardMover** — настраиваемые имена осей `horizontalAxis`, `verticalAxis`; опция ввода **Input Backend** (Legacy / New Input System / AutoPreferNew) с использованием `OptionalInputSystemBridge.ReadMove()`.
- **ConstantMover / ConstantRotator** — тултип для `useDeltaTime` (units per second vs per frame); публичные методы `SetSpeed(float)` и `SetDegreesPerSecond(float)`.
- **MouseMover2D / MouseMover3D** — в Awake при отсутствии камеры подставляется `Camera.main`, при отсутствии — однократный `Debug.LogWarning`; настраиваемые `mouseButton` и `arrivalThreshold`; **MouseMover3D** реализует интерфейс `IMover` (`MoveDelta(Vector2)`, `MoveToPoint(Vector2)` с проекцией на плоскость); в `RaycastCursor` проверка `cam == null`.
- Документация обновлена: CursorLockController.md, KeyboardMover.md, MouseMover3D.md, ConstantMover.md, IMover.md, PlayerController2DPhysics.md, PlayerController3DPhysics.md, README Move.

### Tools / Components — исправления и устойчивость

- **ScoreManager** — исправлена логика `SetBestScore(int?)`: без аргумента обновляет рекорд из текущего счёта, с аргументом — заданное значение (если больше текущего рекорда). Убран лишний вызов `OnValueChange` в `ResetScore()` (сеттер Score уже вызывает событие).
- **Counter** — в `SaveValue()` после `SetFloat` вызывается `SaveProvider.Save()` (соответствие документации и поведению Money).
- **Loot** — проверка `lootItems == null || lootItems.Length == 0` в `DropLoot()` и `GetRandomPrefab()` (защита от NullReferenceException). Namespace приведён к `Neo.Tools`.
- **TextScore** — в `Init()` перед подпиской вызывается `RemoveListener(Set)` для исключения двойной подписки при повторной инициализации.
- **Health** — в XML-документации класса указана реализация IDamageable, IHealable, IRestorable для интеграции с AttackExecution.
- Документация: ScoreManager.md (описание SetBestScore), SCRIPT_IMPROVEMENTS.md — актуализирован (выполненные пункты отмечены).

## [6.0.4] - Unreleased

### Tools / Random — универсальный No-Code и API

- **ChanceSystemBehaviour** — расширены события для настройки и подписки без кода:
  - **On Index And Weight Selected (int, float)** — выбранный индекс и нормализованная вероятность (0..1).
  - **On Roll Complete** — вызывается один раз после каждого броска (UI, звук).
  - **Events By Index** — список UnityEvent по исходам: при выпадении индекса N вызывается событие на позиции N (разные действия на каждый исход без кода).
  - **LastSelectedIndex**, **LastSelectedEntry** — результат последнего броска.
  - **EvaluateAndNotify()** — бросок с вызовом всех событий, возврат Entry.
  - **SetResultAndNotify(int)** — задать результат по индексу и вызвать события.
  - **GetNormalizedWeight(int)**, **GetOrAddEventForIndex(int)** — для кода.
  - Обратная совместимость: **OnIdGenerated** и **OnIndexSelected** сохранены.
- **ChanceManager** — **TryEvaluate(out int index, out Entry entry)** и **TryEvaluate(float randomValue, out int index, out Entry entry)** для удобного доступа к индексу и записи одним вызовом (в т.ч. детерминированный бросок).
- **ChanceData** — в документации указано использовать ChanceSystemBehaviour для сценарных событий по индексу.
- Документация: ChanceSystemBehaviour.md, ChanceManager.md, README Random обновлены.

## [6.0.3] - Unreleased

### UI: SceneFlowController и реестр устаревших

- **SceneFlowController** — новый компонент загрузки сцен и действий приложения: LoadScene(int), LoadScene(string), LoadScene() без параметров; режимы Sync, Async, AsyncManual, Additive; прогресс через Text/TMP, Slider, Image и UnityEvent\<float\> OnProgress; события OnLoadStarted, OnReadyToProceed, OnLoadCompleted; Quit, Restart, Pause, ProceedScene. Документация: Docs/UI/SceneFlowController.md.
- **UIReady** — помечен `[Obsolete]` с рекомендацией использовать SceneFlowController; остаётся рабочим для обратной совместимости.
- **DEPRECATED_OR_REMOVAL_CANDIDATES.md** — добавлен реестр устаревших скриптов и кандидатов на удаление (TimeReward, AiNavigation, HandLayoutType, HandComponent.LegacyLayoutType, UIReady) со ссылками на замены.

### Create Neoxider Object (окно и меню)

- **Цвета категорий** — в окне Create Neoxider Object категории верхнего уровня (UI, Tools, Bonus, Shop, Audio, Level,
  Save, Condition, Animations, GridSystem, Parallax, NPC) выделены разными цветами для быстрого распознавания.
- **Подпапки Tools** — в меню и в окне Tools разбит на подкатегории: **Physics**, **Movement**, **Spawner**, *
  *Components**, **Dialogue**, **Input**, **View**, **Debug**, **Time**, **Text**, **Interact**, **Random**, **Other**,
  **State Machine**, **FakeLeaderboard**, **Managers**, **Camera**. Пути CreateFromMenu обновлены (например
  Neoxider/Tools/Movement/PlayerController3DPhysics, Neoxider/Tools/Physics/ExplosiveForce).
- **Документация** — UsefulComponents.md обновлён: описание подкатегорий Tools и цветов в окне.

## [6.0.2] - Unreleased

### GameObject → Neoxider (CreateFromMenu)

- **CreateFromMenu** — атрибут `[CreateFromMenu("Neoxider/…", PrefabPath = "…")]` добавлен на все основные компоненты с
  AddComponentMenu: UI, Tools, Shop, Audio, Bonus (в т.ч. TimeReward, SpinController, SlotElement, Row, Box,
  ItemCollection, ItemCollectionInfo, WheelMoneyWin), Condition (NeoCondition), Animations (ColorAnimator,
  FloatAnimator, Vector3Animator), GridSystem (FieldGenerator, FieldSpawner, FieldDebugDrawer, FieldObjectSpawner,
  Match3BoardService, TicTacToeBoardService), Bootstrap. Компонент и объект можно создавать и через Add Component, и
  через **GameObject → Neoxider → Create Neoxider Object…**; при указанном префабе создаётся из префаба, иначе — объект
  с компонентом (fallback).
- **UsefulComponents.md** — раздел «GameObject → Neoxider» обновлён: список пунктов строится по рефлексии из типов с
  атрибутом, перечислены категории меню.

### Документация: меню Create и Add Component через Neoxider

- Во всей документации пути создания ассетов (ScriptableObject) и пункты Add Component приведены к **Neoxider** вместо *
  *Neo**: «Create > Neo > …» → «Create > Neoxider > …», «Add Component → Neo → …» / «GameObject → Neo → …» → «Neoxider».
  Обновлены IMPROVEMENTS.md (CreateAssetMenu в примерах), RainbowSignature.md, README модулей (Collection, Cards, Shop,
  StateMachine, Level, NeoxiderPages, NPC, ChanceManager, DeckConfig, ItemCollectionData и др.), разделы «Добавить» в
  описаниях компонентов (GM, EM, Bootstrap, Money, Leaderboard, PoolManager, UIPage, PageSubscriber,
  StateMachineBehaviour и т.д.), NeoCondition.md, UI Extension README.

### UI / Tools

- **PausePage** — опциональное и универсальное поведение:
    - Все функции опциональны: масштаб времени (`_useTimeScale`), уведомление GM (`_sendPause`), управление курсором (
      `_controlCursor`).
    - При включённом **Control Cursor** при открытии паузы курсор показывается и разблокируется, при закрытии
      восстанавливается предыдущее состояние (совместимо с CursorLockController и PlayerController 2D/3D).
    - Масштаб времени во время паузы задаётся полем `_timeScaleOnPause` (0 = полная пауза), при снятии паузы
      восстанавливается сохранённый `Time.timeScale`.
- **CursorLockController** — опциональное применение состояния по жизненному циклу:
    - **Apply On Enable** / **Lock On Enable** — при включении компонента (OnEnable) выставить lock/unlock курсора.
    - **Apply On Disable** / **Lock On Disable** — при выключении компонента (OnDisable) выставить lock/unlock.
    - В документации уточнено совместное использование с PausePage и PlayerController3DPhysics /
      PlayerController2DPhysics без конфликтов.
- **Документация** — обновлены PausePage.md и CursorLockController.md.

## [6.0.1] - Unreleased

### Пул объектов (PoolManager, Spawner)

- **Дефолтный max size пула** — 100 (раньше 10000). В **PoolConfig** и настройках по умолчанию добавлено поле **maxSize
  **; при 0 используется 100.
- **PooledObjectInfo** — выставляется один раз при создании объекта в **NeoObjectPool.CreatePooledObject()**, а не при
  каждом Get. Добавлен метод **Return()** для возврата в пул без вызова PoolManager.Release.
- **Spawner** — при использовании пула родитель передаётся в **PoolManager.Get(..., parent)** и больше не задаётся
  вторым вызовом SetParent.
- **Расширения (Neo.Extensions)** — **ReturnToPool()** для GameObject, **SpawnFromPool(position, rotation, parent)** для
  префаба; при отсутствии PoolManager SpawnFromPool делает Instantiate.
- **PoolableBehaviour** — базовый класс с виртуальными OnPoolCreate/OnPoolGet/OnPoolRelease для объектов из пула.
- **Документация** — обновлены PoolManager.md, добавлены PooledObjectInfo.md, PoolableBehaviour.md.

## [6.0.0] - Unreleased

### Документация в инспекторе

- В блоке **Documentation** (в стиле Events/Actions) отображается превью .md и кнопка **Open in window**. Документ
  привязывается к компоненту через атрибут `[NeoDoc("path/from/Docs.md")]` или по соглашению `TypeName.md` в Docs.
- Опциональный пакет **Markdown Renderer** для рендера Markdown в окне: установка через **Package Manager → Add package
  from git URL** → `https://github.com/NeoXider/MarkdownRenderer.git`. Без пакета по кнопке открывается выбор .md-ассета
  в Project.

### Прочее

- Мажорное обновление версии: 5.8.x → 6.0.0.

## [5.8.15] - 2025

### Bonus — CooldownReward и устаревание TimeReward

- **CooldownReward** — новый компонент наград по кулдауну, наследник **TimerObject**: один скрипт объединяет таймер (
  RealTime), сохранение и логику наград (TakeReward, GetClaimableCount, накопление, max per take). Рекомендуется для
  нового кода. Меню: GameObject → Neoxider → Bonus → CooldownReward.
- **TimeReward** — помечен `[Obsolete]` с рекомендацией использовать CooldownReward; остаётся рабочим для обратной
  совместимости.
- **Docs** — в Bonus/TimeReward/README.md описан CooldownReward как рекомендуемый компонент, TimeReward — как
  устаревший.

### Tools/Time — расширение для наследования

- **TimerObject** — точки расширения для наследников: `protected virtual string GetSaveKey() => saveKey` (подстановка
  ключа в SaveState/LoadState), `protected virtual void SaveState()` (вызов сохранения из производного класса). Поля
  `saveProgress` и `saveMode` сделаны `protected` для настройки в наследниках.

### Зависимости

- **Neo.Bonus.asmdef** — добавлена ссылка на Neo.Tools.Time (для CooldownReward).

## [5.8.14] - Unreleased

### Bonus/Collection — улучшения и исправления

- **CollectionVisualManager** — подписки на кнопки теперь корректно снимаются при уничтожении: делегаты кешируются в
  массиве и передаются в `RemoveListener` той же ссылкой, что и в `AddListener` (раньше лямбды создавали новые делегаты
  и отписка не срабатывала).
- **Collection** — единое хранилище: запись состояния предметов переведена с `PlayerPrefs.SetInt` на
  `SaveProvider.SetInt` в `AddItem`, `RemoveItem`, `ClearCollection` для согласованности с `Load()`/`Save()`.
- **Collection.GetPrize** — оптимизация с O(n²) до O(n): выбор из ещё не полученных предметов по списку индексов без
  повторного поиска.
- **Collection** — защитные проверки в `UnlockAllItems` и `ClearCollection`: синхронизация длины `_enabledItems` с
  `_itemCollectionDatas`, проверка `_itemCollectionDatas` в `ClearCollection`.
- **Box** — прогресс не уходит в минус после `TakePrize` (clamp ≥ 0); в сеттере `progress` вызывается
  `SaveProvider.Save()` для сохранения данных.
- **ItemCollectionData** — публичные свойства переименованы в PascalCase: `ItemName`, `Description`, `Sprite`,
  `ItemType`, `Rarity`, `Category` (вместо `itemName`, `description`, `sprite` и т.д.).
- **Box** — поля `addProgress`/`maxProgress` заменены на `[SerializeField] private` с публичными свойствами
  `AddProgressAmount`, `MaxProgress`.
- **ItemCollection** — поле `button` заменено на приватное с публичным свойством `Button`.

## [5.8.13] - Unreleased

### Добавлено

- **NeoCondition** — сравнение переменной с переменной другого объекта:
    - В условии добавлен режим **Compare With** → **Other Object**: вместо порога (число/текст) можно указать второй
      объект, компонент и поле/свойство. Условие проверяет «левая переменная op правая
      переменная» (==, !=, >, <, >=, <=). Поддерживаются оба источника с Find By Name и режимом GameObject.

- **TimerObject** — опциональное сохранение состояния (по умолчанию выключено):
    - `saveProgress`: включить сохранение/восстановление текущего времени и признака «идёт/на паузе»; работает при
      счётчике вверх и вниз.
    - `saveMode`: **Seconds** (по умолчанию) — сохранять текущее значение в секундах, при загрузке продолжить с него; *
      *RealTime** — сохранять целевое время (UTC), при загрузке пересчитывать оставшееся время от текущего момента.
    - `saveKey`: ключ для SaveProvider. Состояние сохраняется при OnDisable, загружается в Awake.

- **InteractiveObject** — опция `includeTriggerCollidersInMouseRaycast` (по умолчанию `true`): учитывать или
  игнорировать trigger-коллайдеры в луче наведения мыши (hover). Позволяет отключить реакцию на триггеры, если нужен
  только «твёрдый» коллайдер.

### Исправлено

- **NeoCondition** — при пустом **Other Source Object** (сравнение с другой переменной) теперь используется **тот же
  объект, что и слева**. Условия вида `Health.Hp == Health.MaxHp` на одном GameObject работают без указания второго
  объекта (раньше подставлялся объект с NeoCondition и сравнение давало неверный результат).
- **TimeReward** — логика накопления наград и сдвига времени перенесена в сам компонент; зависимость от
  `CooldownRewardExtensions` убрана для корректной сборки во всех конфигурациях (в т.ч. UPM).
- **InteractiveObject** — поддержка новой Input System (мышь и клавиатура через рефлексию при Active Input Handling =
  Input System Package); учёт trigger-коллайдеров в mouse hover raycast (настраивается через
  `includeTriggerCollidersInMouseRaycast`).

## [5.8.9] - 2025-02-17

### Добавлено

- **TimeReward** — накопление наград и гибкий первый запуск:
    - `_rewardAvailableOnStart` (по умолчанию `false`): при отсутствии сохранения награда доступна сразу (true) или
      после полного кулдауна (false).
    - `_maxRewardsPerTake`: -1 = забрать все накопленные, 1 = одна за раз, N = не больше N за раз.
    - `GetClaimableCount()` — количество наград, доступных к выдаче.
    - `OnRewardsClaimed(int)` — событие с количеством выданных за один Take.
    - Интервал обновления таймера по умолчанию изменён на `updateTime = 0.2f`.
- **CooldownRewardExtensions** — утилиты для кулдаун-наград:
    - `GetAccumulatedClaimCount(DateTime, float, DateTime)` — число накопленных наград;
    - `CapToMaxPerTake(int, int)` — ограничение числа за один забор;
    - `AdvanceLastClaimTime(DateTime, int, float)` — сдвиг времени последней выдачи.

### Улучшено

- **TimeReward** — `TakeReward()` выдаёт до `GetClaimableCount()` наград за раз; при сохранении времени используется
  сдвиг на число выданных наград. Документация: полная схема работы (flowchart), таблица «механика → настройки»,
  примеры.
- **Docs** — добавлена страница `CooldownRewardExtensions.md`, обновлён `TimeReward.md`, индекс `Extensions/README.md`.

## [5.8.8] - Unreleased

### Добавлено

- **Extensions/Time** — новые расширения для работы со временем:
    - `DateTimeExtensions`: `ToRoundTripUtcString`, `TryParseUtcRoundTrip`, `GetSecondsSinceUtc`, `GetSecondsUntilUtc`,
      `EnsureUtc`;
    - `TimeParsingExtensions`: `TryParseDuration` — парсинг SS, MM:SS, HH:MM:SS, DD:HH:MM:SS;
    - `TimeSpanExtensions`: `ToCompactString`, `ToClockString`.
- **TimeReward** — `GetFormattedTimeLeft`, `TryGetLastRewardTimeUtc`, `GetElapsedSinceLastReward`; настройки
  `_displayTimeFormat`, `_displaySeparator`.
- **Timer** — `Play()`, `SetRemainingTime(float)`, `SetProgress(float)`.
- **TimerObject** — `SetDuration(float newDuration, bool keepProgress = true)`.
- **TimeToText** — `TrySetFromString(string raw, string separator = null)`, опция `_allowNegative`.
- **PrimitiveExtensions.FormatTime** — перегрузка с `trimLeadingZeros` (например `01:05` → `1:05`).

### Улучшено

- **TimeReward** — интеграция с `DateTimeExtensions` и `TimeParsingExtensions`;
  `FormatTime(float, TimeFormat, string, bool)`.
- **Docs** — добавлены `TimeFormatting.md`, `DateTimeExtensions.md`, `TimeParsingExtensions.md`,
  `TimeSpanExtensions.md`; обновлены `TimeReward`, `Timer`, `TimerObject`, `TimeToText`, `PrimitiveExtensions`,
  `Tools/Time/README.md`, `Extensions/README.md`.

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
    - добавлен режим ручного старта кулдауна: при `saveTimeOnTakeReward = false` время может сохраняться в
      `StartTime()`;
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
