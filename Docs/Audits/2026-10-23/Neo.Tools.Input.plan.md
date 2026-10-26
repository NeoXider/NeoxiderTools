# Neo.Tools.Input — индивидуальный план реализации

Статус: план исходного аудитора, `gpt-6.1-sol`, reasoning `max`. Прочитаны [аудит](Neo.Tools.Input.md), manifest и актуальные source/docs/tests. Изменяется только данный файл.

## 1. Уже выполнено

Просмотрены все 10 принадлежащих сборке C# файлов. Исправлены F1–F11: обход совместимого ввода, зависимость Hold/Click от Press, устаревшие press/gesture состояния, очистка визуальных эффектов при собственном отключении, двойной первый Hold spawn, смешивание пальцев, swipe без начала, неверный выбор оси, KeypadEnter, Try-семантика и null bindings. Обновлены шесть существующих страниц `Assets/Neoxider/Docs/Tools/Input/`.

Добавлены `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsInputAdapterTests.cs` и `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.cs`: 25 объявленных случаев, **не 25 пройденных**. Проверены статически inventory, UTF-8, имена API/полей, ссылки, GUID, scoped diff. Unity/компиляция/тесты/player validation не запускались. Завершённые исправления не являются backlog.

## 2. Оставшиеся подтверждённые пути исправления

Оба пути подтверждены текущим кодом; runtime-воспроизведение/исправления не выполнены. Владелец — `Neo.Tools.Input`.

### P1: отмена в менеджере не завершается в MouseEffect

Источник: `MouseInputManager.ClearPressedState`/`OnDisable`/отсутствие камеры и `MouseEffect.Update`. После Press отключить **только менеджер**, отпустить мышь, включить менеджер. Press очищен без уведомления; эффект сохраняет `_isFollowing`, Release пропущен. F3 устраняет ложный Click, F4 — последствия отключения самого эффекта; межкомпонентная отмена остаётся.

Минимальное исправление: добавить аддитивный C# `event Action OnCanceled` в `MouseInputManager.cs`. Отдельный путь отмены сначала очищает активный press, затем уведомляет ровно один раз; обычный Release очищает состояние без отмены. `MouseEffect.cs` подписывается/отписывается и вызывает существующий `StopFollowing(true)`. Не синтезировать Release/Click и не запускать release-prefab.

Наблюдаемая приёмка: после отключения менеджера или утраты камеры follower неактивен, `trail.emitting == false`, один stop на начатый follow, ноль Release/Click/spawn из отмены; новый Press снова работает. Новый файл: `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputCancellationTests.cs`. Документация: существующие `MouseInputManager.md`, `MouseEffect.md`, `README.md` в каталоге Input. Оценка: 0,5–1 дня с проверкой.

### P2: onSpawn может отключить эффект до завершения OnPress

Источник: `MouseEffect.OnPress → SpawnAt → onSpawn`. Обработчик `onSpawn` отключает компонент; `OnDisable` сбрасывает состояние, но вернувшийся OnPress снова выставляет `_isFollowing = true` и вызывает `onStartFollow`. Это конкретный повторный вход через пользовательский callback.

В `MouseEffect.cs` после callback проверить актуальность компонента/взаимодействия, очистить прерванный follow и выйти. Сохранить обычный порядок событий. Регрессия в `CancellationTests.cs`: onSpawn отключает эффект; start отсутствует, повторное включение без Press не возобновляет следование. Отключение из onStartFollow должно давать одну пару start/stop. Оценка: несколько часов.

## 3. Долг проверки и неподтверждённые риски

| Приоритет, владелец | Основание и минимальное действие | Наблюдаемый результат |
| --- | --- | --- |
| P0, координатор | После завершения меняющих код аудитов импортировать и выполнить оба существующих новых fixture и `Assets/Neoxider/Tests/Edit/Core/SubsystemRegistrationStaticResetEditModeTests.cs`. | Нет новых compile/runtime ошибок; результаты и skips записаны отдельно. |
| P1, координатор + Input | Legacy-only/Both/New-only: существующие тесты используют внутренние processing seams и не доказывают реальный Update. Проверить устройства, физические кнопки, touch и возврат фокуса. | Одна последовательность событий, верные endpoints, отсутствие повторов/зависших visuals. |
| P1, Input + `Neo.Tools.Managers` | `SwipeController.ResetStaticStateForRuntime` очищает Instance, а назначение находится в Awake; поведение без domain/scene reload пока не установлено. Проверить все четыре сочетания. | Static polling снова доступен; нет дубликатов singleton. Исправлять минимальную регистрацию только после воспроизведения. |
| P1, координатор | Reflection и stripping/IL2CPP не проверены; fake controls не заменяют настоящий Touchscreen. | Доступны position/button/phase, нет missing-member ошибок; пакет без Input System компилируется. |
| P2, Input | Primary-touch lifetime, быстрые переходы между Update, retained visuals и стоимость exception/reflection пути требуют измерения. | Записаны transcript, кадры жизненного цикла и аллокации; улучшение не объявлено исправленным дефектом. |

Проверить `MultiKeyEventTrigger`: null/empty bindings дают ноль вызовов, восстановленный Space-binding — один вызов на down. Повторный enable после замены менеджера остаётся документированным ограничением.

## 4. Существенное улучшение: C# recognizer и буфер pointer-событий

**Предложение.** Replay/custom input требует MonoBehaviour/internal seams; render polling теряет переходы. Выделить обычный C# объект, сохранив `Vector2`/`SwipeData` и зависимость от UnityEngine.

Предлагаемый публичный контракт:

```csharp
SwipeRecognizer(SwipeSettings settings);
event Action<SwipeData> OnSwipe;
void Process(in PointerSample sample);
void Configure(in SwipeSettings settings);
void Cancel(int pointerId);
void Reset();

// IPointerSampleSource: извлечь следующий sample FIFO.
bool TryRead(out PointerSample sample);

// SwipeController: null возвращает совместимый источник по умолчанию.
void SetInputSource(IPointerSampleSource source);
```

`PointerSample`: уникальный ID контакта, position, фаза, timestamp; `SwipeSettings`: X/Y thresholds, detection/suppression. Configure действует со следующего sample без сброса. Bounded FIFO сохраняет переходы; переполнение выдаёт Reset. Источник принадлежит вызывающему коду; controller его не Dispose. Подмена отменяет жест; replay использует Process.

Точные новые runtime-файлы в `Assets/Neoxider/Scripts/Tools/Input/`: `PointerSample.cs`, `SwipeSettings.cs`, `SwipeRecognizer.cs`, `IPointerSampleSource.cs`, `BufferedPointerSampleSource.cs`. В `SwipeController.cs` оставить serialized поля, события и статический consume-once фасад, перенаправив правила в recognizer. Использовать уже исправленные алгоритмы и существующие compat helpers; повторно сверить каталог `neoxider-tools` перед реализацией.

Второй, отдельный этап: `Assets/Neoxider/Scripts/Tools/Input/InputSystem/InputSystemPointerSource.cs` и `InputSystemPointerSourceBehaviour.cs`, плюс новая `Neo.Tools.Input.InputSystem.asmdef` в том же каталоге. Typed источник буферизует настоящие Input System события и освобождает подписки при Dispose. Сценовый адаптер нужен только для Inspector-подключения через новое опциональное поле `MonoBehaviour`-провайдера в SwipeController; C# использует SetInputSource. Сценовые ссылки не помещать в ScriptableObject. Координатор назначает владельца новой вложенной сборки и настраивает optional references/defines; базовая сборка не зависит от неё.

Новые минимальные проверки: `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsInputRecognizerTests.cs`, `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsInputBufferTests.cs`, `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputSourceTests.cs`. Проверить четыре направления, равные/разные пороги, suppression/reversal, release-only, чужой ID, Cancel/Reset, смену источника, overflow и replay. В typed integration — Began/Moved/Ended в одном render frame дают один корректный swipe, а persistent phase не создаёт повторов. Реальная backend-проверка должна проходить через источник/Update.

Обновить C#/Inspector workflows в существующих `README.md`/`SwipeController.md`. Оценка: core 1–2 дня, optional adapter 2–3 дня плюс device/IL2CPP QA. Риски: порядок/задержка, fixed/dynamic updates, edges, lifetime подписок.

## 5. Порядок, границы и завершение

1. Стабилизировать общий результат аудитов; координатор закрывает P0. Затем P1/P2 fixes с регрессиями, после них — core extraction, wrapper, optional adapter, docs и package validation.
2. `Neo.Tools.Managers` владеет `Singleton.cs`/`SingletonRuntimeReset.cs`: сначала согласовать наблюдаемое поведение reset, затем локальные hooks Input. Не исправлять чужой generic base отсюда.
3. `Neo.Tools.Move` владеет `Assets/Neoxider/Scripts/Tools/Move/OptionalInputSystemBridge.cs` и использует публичные ReadMove/Look/Jump/Run/Key методы. Их сигнатуры и единицы сохранить; проверку потребителей выполняет владелец Move после Input. Не менять смешанное 2D/3D picking и не добавлять зависимость от `Neo.Tools.InteractableObject.Core`. `Neo.Extensions`/`Neo.PropertyAttribute` остаются существующими зависимостями; использовать `NeoDiagnostics.StableId`, без новых разрозненных version gates.
4. Сохранить все serialized имена, defaults, enum values и UnityEvents. Buffered input/иная политика нескольких пальцев — opt-in; выбор новой политики остаётся продуктовым решением координатора. Сцены/prefabs не мигрировать. Каждый новый package-файл получает уникальный `.meta`; optional assembly/shared test references меняет координатор. Sample layout оставить `Samples~`; будущие примеры проверять также при `Samples` и импортированном `Assets/Samples/NeoxiderTools/<version>/<sample name>`.
5. Завершение требует фактической компиляции Unity 6.0–6.4 и 6.5+, соответствующих Edit/Play результатов, отсутствия новой зависимости в базовом пакете и no-package/IL2CPP проверки optional этапа. Проверять обе реальные version branches компилятором соответствующей версии. Неудачная optional compilation останавливает этот этап, не расширяет зависимости ядра. Непройденная проверка остаётся открытой; ни статический анализ, ни число объявленных тестов не заменяют её.

Создан только данный UTF-8 план; остальные файлы не изменялись.
