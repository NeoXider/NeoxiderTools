# Neo.Level — индивидуальный план реализации

Исходная сессия аудита, профиль `gpt-6.1-sol / max`. Контракт, manifest, аудит и текущие source/docs/tests прочитаны. Изменён только план; предложения ниже не реализованы.

Сокращение `Docs/Level/` ниже означает `Assets/Neoxider/Docs/Level/`.

## Текущее состояние

Аудит проверил **5/5 C# файлов**. Уже исправлены F1–F10: необязательные ссылки UI и пустые карты, переходы через пропущенные карты, циклический прогресс, границы индексов, откат счётчика при ошибке записи, состояния/привязка кнопок, публичный `SetLevel`, отложенная подписка `TextLevel`, владение async-загрузкой и прогресс UI. Обновлены шесть страниц `Assets/Neoxider/Docs/Level/`. Повторно реализовывать эти исправления не требуется.

Подготовлен **31 регрессионный случай: 26 + 5**. Прошли статические проверки инвентаря, UTF-8, serialized fields/API, GUID, ссылок и scoped `git diff --check`. **Компиляция, NUnit и Unity lifecycle/native loading не выполнялись.**

## Приоритеты: исправления и долг проверки

### P1 — центральная верификация уже внесённых изменений

Координатор после завершения source-аудитов запускает существующие `LevelManagerTests`, `SceneFlowControllerTests` и:

- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelProgressTests.cs`;
- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs`.

Вторая фикстура входит в Play Mode из Edit assembly. Нужны изолированная сцена/TMP и восстановление статических зависимостей. Наблюдать циклический прогресс, сохранения/события, отсутствие обновлений отключённого текста, UI click и отписку после уничтожения.

Отдельно проверить `SceneFlowController.LoadSceneCoroutine/ReleasePendingLoad`: несколько кадров на 0.9 сохраняют ready-текст; `ProceedScene()` завершает загрузку; последний progress равен 1; completion приходит один раз у пережившего переход контроллера. Деактивация GameObject/уничтожение компонента освобождает native activation и последующую очередь; отключение только компонента сохраняет работающую coroutine. Проверить callbacks, запускающие новый запрос. Это **долг проверки**, не новые подтверждённые дефекты.

### P2 — подтверждённое переполнение подписи LevelButton

Актуальный `Assets/Neoxider/Scripts/Level/LevelButton.cs`, `SetVisual`: `(level + 1).ToString()` переполняется при `SetVisual(1, int.MaxValue)` с назначенным TMP. Ожидается `2147483648`, арифметически получается отрицательное значение. Владелец — `Neo.Level`; минимальная коррекция — сложение через `long` без изменения `level`, serialized fields и событий. Добавить `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelBoundaryTests.cs` и уникальную `.meta`: проверить точный текст, `level`, visual event. Дополнить `Docs/Level/LevelButton.md`. В планировании код не исправлялся.

### P2 — граница Neo.Tools.Text и Neo.Level

Текущий `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` уже складывает offset через `long`, очищает tween и использует `SetLink`: эти замечания закрыты.

Остаётся подтверждённое **ограничение точности**: `Set(int)` переводит результат в `float`; `16777216 + offset 1` теряет единицу. Сначала владелец `Neo.Tools.Text` определяет точный integer-display контракт, переиспользуя существующий `SetBigInteger`; затем `Neo.Level` подключает его в `TextLevel.cs`, без собственного форматтера. Приёмка: точный большой номер с offset, сохранённые prefix/suffix/format settings; поведение анимации явно описано в `Docs/Level/TextLevel.md`.

**Непроверенный интеграционный риск:** private `TextLevel.OnEnable/OnDisable` скрывают одноимённые private callbacks обновлённого `SetText`. Проверить паузу/возобновление незавершённой анимации при `label.enabled = false/true`. При воспроизведении сначала owner `Neo.Tools.Text` вводит protected virtual lifecycle hooks, затем owner `Neo.Level` вызывает base из override; дополнить собственную lifecycle-фикстуру. До проверки это не подтверждённая ошибка Unity.

## P2 — предложение: управляемая операция загрузки

`ISceneFlowSceneLoader` возвращает concrete `AsyncOperation`: подмена проверяет dispatch/отказ, но не управляет 0.9/activation/completion. Предложение закрывает этот пробел.

Файлы в `Assets/Neoxider/Scripts/Level/Runtime/`: новые `ISceneFlowLoadOperation.cs`, `ISceneFlowOperationFactory.cs`, `UnitySceneFlowLoadOperation.cs`; адаптация существующего `SceneFlowController.cs`. Оставить внутри `Neo.Level`, без нового asmdef/зависимости от Progression.

Публичный контракт: операция предоставляет `Progress`, `IsDone`, `AllowSceneActivation`, `Error`; factory создаёт её по имени/индексу. Добавить instance `SceneFlowController.OperationFactory`; null использует adapter существующего static `SceneLoader`. Старый `ISceneFlowSceneLoader`, свойства, режимы и UnityEvents сохраняются. Добавочный `OnLoadFailed` сообщает текст ошибки один раз; completion означает только успех. Unity adapter не обещает terminal error information, которой native API не предоставляет.

Порядок: интерфейсы → adapter → сохранение request identity/cleanup → fake-операции → документация. Новый `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelSceneFlowOperationTests.cs` проверяет в Play Mode без настоящих переходов: ready один раз, текст сохраняется, proceed разрешает activation, отказ позволяет retry, старый cleanup не снимает новый запрос. Сохранить native smoke из P1. Обновить `Docs/Level/SceneFlowController.md` и README: C# injection и Inspector Proceed/failure. Оценка: 1–2 дня плюс engine QA; риск — порядок событий. Глобальную очередь между контроллерами не вводить.

## P3 — предложение: отдельная модель прохождения карт

`LevelManager` объединяет правила, lifecycle, storage и UI; нет явной C# инициализации, повторный callback одноуровневого цикла не отличается от нового прохождения.

Кандидаты проверены: `Neo.Core` владеет `Assets/Neoxider/Scripts/Core/Level/Domain/LevelModel.cs` и `Assets/Neoxider/Scripts/Core/Level/Interfaces/ILevelProvider.cs`, но это **1-based XP/level**, а здесь **0-based выбор контента/карты**. `Assets/Neoxider/Scripts/Progression/Components/ProgressionManager.cs` управляет XP/rewards/trees. Не подменять ими карту и не добавлять циклических зависимостей. Переиспользовать `int.Wrap`, `SaveProvider`/`ISaveProvider`; `Singleton` и `WaitWhile` остаются только у scene adapters.

Точные новые файлы под `Assets/Neoxider/Scripts/Level/Runtime/`: `LevelMapDefinition.cs`, `LevelProgressContracts.cs`, `LevelProgression.cs`, `ILevelProgressStore.cs`, `SaveProviderLevelProgressStore.cs`. Изменяемые adapters: `LevelManager.cs`, `Map.cs`; остальные views подключаются через сохранённые события.

Предлагаемый C# API:

```csharp
LevelSelection Selection { get; }
bool TrySelectMap(int mapId);
bool TrySelectLevel(int contentIndex);
LevelSelection SelectFrontier();
LevelRunId BeginRun();
LevelCompletionResult TryComplete(LevelRunId runId);
event Action<LevelProgressChange> ProgressChanged;
```

`LevelSelection` различает карту, content index и абсолютный счётчик. Run token фиксирует карту/фронтир: повторное или устаревшее завершение не увеличивает прогресс. Store возвращает accepted/rejected/indeterminate; при неопределённом исходе нужна сверка сохранения перед повтором, без обещания дисковой транзакции. Model публикует успешное изменение после принятой записи. `LevelManager.Initialize(LevelProgression)` даёт явный C# путь, корректно снимая прежние подписки. Привязанный `Map` делегирует одной модели; standalone `Load/SaveLevel` сохраняются, второго независимого счётчика не создавать.

Первый этап оставляет `int`, saturation и ключ `Map_<saveKey>_<id>__level`. Расширение до `long` — отдельное решение о versioned storage и legacy projections; автоматической миграции нет. Сохранить все serialized fields, `GetCopmplete`, UnityEvent payloads и явную семантику смены карты.

Новый `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelProgressionModelTests.cs`: модель без GameObjects/global save; восемь завершений дают счётчик 8/следующий content index 2; duplicate run не начисляет повтор; rejected write сохраняет состояние; старое сохранение 4 при count 3 выбирает 1; пропуски карт не меняют save identity. Затем выполнить прежние adapter-тесты, включая прямой `Map.SaveLevel`. Обновить существующие README, `LevelManager.md`, `Map.md` для C#/Inspector; отдельная NoCode-логика не нужна. Оценка: 3–5 дней без миграции формата; риск средний, особенно совместимость Map/events.

## Очерёдность и условия завершения

1. После зависимых аудитов исправить overflow с регрессией. Координатор выполняет P1; подтверждённые интеграционные ошибки исправляют соответствующие owners раньше архитектурных изменений.
2. Ввести operation seam и его проверки; затем независимо вводить progression model, сохраняя проходящую legacy матрицу.
3. После стабильного C# API подключить adapters, актуализировать канонические страницы, проверить serialized compatibility/события. Новым package-файлам нужны уникальные `.meta`; metadata папок/asmdef references — координатору.
4. Центрально скомпилировать Unity 6.0–6.4 и 6.5+ соответствующим компилятором, выполнить scoped Edit/Play проверки и release/package validation. Новые ID-вызовы идут только через существующие `NeoDiagnostics.StableId` / editor `NeoComponentHealth.StableId`; условные ветки не размножать.

Завершение требует компиляции/регрессий и native-сценария без новых console errors. Падающая проверка остаётся открытой с репродукцией/owner. Продуктовые решения: `long` storage migration, точная/приближённая анимация больших чисел, wrapped frontier после полного цикла. Сейчас кнопки все passed; желаемая UX-семантика не доказанный баг.

`Neo.Editor.Tests`/координатор владеют shared test asmdef; `Neo.Extensions` — Wrap/CoroutineHandle; `Neo.Tools.Managers` — singleton lifecycle; `Neo.Save` — flush/commit guarantees. Их код этим планом не изменяется. Каталог `Docs/Level/InternalTypes.md` с Core-типами консолидирует координатор с владельцами Core.

Release layout сохраняется: `Samples~`; при разработке допустим `Samples`, импорт — `Assets/Samples/NeoxiderTools/<version>/<sample name>`. Не переписывать сцены/префабы автоматически и не менять глобальные package paths ради проверки.
