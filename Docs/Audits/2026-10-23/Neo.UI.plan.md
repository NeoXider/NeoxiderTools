# Neo.UI — индивидуальный план реализации

Фаза 2; тот же аудитор, `gpt-6.1-sol`, reasoning `max`. Основание: [завершённый аудит](./Neo.UI.md), PLAN-CONTRACT и текущая запись manifest. Сейчас изменяется только этот план. Перечисленные ниже реализации ещё не выполнены.

Для точных путей используются корни: `S = Assets/Neoxider/Scripts/UI`, `M = Assets/Neoxider/Scripts/Tools/Managers`, `E = Assets/Neoxider/Tests/Edit/UI`, `P = Assets/Neoxider/Tests/Play/UI`, `D = Assets/Neoxider/Docs/UI`.

## 1. Уже выполнено

Прочитаны все 28 исходников Neo.UI. Исправлены девять файлов: `S/Simple/UI.cs`, `S/Simple/ButtonChangePage.cs`, `S/Animation/ButtonShake.cs`, `S/View/VisualToggle.cs`, `S/View/VariantView.cs`, `S/CategoryBar/CategoryBar.cs`, `S/MeshRig/UIMeshRigLayoutPreset.cs`, `S/MeshRig/UIMeshRigPoint.cs`, `S/AnimationFly.cs`. Исправления покрывают страницы/статику, CanvasGroup и ToggleGroup, reactive-состояние, глубину shake, категории, bind-координаты/владение точками, камеры и ресурсы полётов. Повторно реализовывать UI-01–UI-14 не требуется.

Обновлены десять существующих руководств. Добавлены 22 тестовых метода / 25 случаев в пяти fixtures `Audit20261023NeoUI{CategoryBar,MeshRig,View,Lifecycle,AnimationFly}Tests`; первые три находятся в E, последние два — в P. Статическая проверка 49 файлов и scoped `git diff --check` прошли. Компиляция, выполнение тестов и визуальная проверка не проводились.

## 2. Подтверждённые остаточные дефекты и проверки

| Приоритет / статус | Доказательство и минимальное следующее действие | Владелец / наблюдаемый результат |
|---|---|---|
| P1: интеграционный дефект, подтверждён текущим кодом при планировании | `S/PausePage.cs:OnEnable` сначала ставит scale 0, затем вызывает `M/GM.cs:Pause`, сохраняющий уже 0. При закрытии GM восстанавливает 0 поверх восстановленной страницей 1. Создать регрессию `P/Audit20261023NeoUIPausePageTests.cs`; согласовать порядок захвата/освобождения с финальным GM: исходный scale захватывается до обоих writers, GM при собственном time-control получает исходный scale, его Resume завершается до окончательного восстановления страницы. | Neo.UI + Neo.Tools.Managers. Один цикл открытия/закрытия возвращает 1 и ненулевой baseline, OnResume видит восстановленное время; исправления MGR-03 не откатываются. |
| P2: чужая пауза возобновляется страницей | `PausePage.OnDisable` безусловно вызывает Resume, даже если GM был Pause до открытия. Сохранить конкретный GM и факт собственного перехода в Pause; освобождать только его. Файл и regression fixture те же. | Neo.UI; после закрытия уже приостановленный GM остаётся Pause, замещающий экземпляр GM не получает чужой Resume. |
| P2: подтверждённое пересечение страниц | A/B сохраняют scale 1/0; закрытие A раньше B даёт преждевременный resume, затем финальный scale 0. Одной перестановки вызовов недостаточно; нужна общая модель владения из раздела 3. | Managers — ядро, UI — адаптер. Оба порядка закрытия сохраняют паузу до последнего владельца. |
| P1: долг проверки, не новый дефект | Запустить пять точных fixtures выше и существующие UI/CategoryBar/AnimationFly/MeshRig suites после завершения исходных аудитов. Отдельно проверить сериализацию ссылок в cached-page тесте. | Координатор. Реальные состояния/события/координаты соответствуют assertions; `NeoUIEditorAuthoringTests` относится другому владельцу. |
| P2: неподтверждённые риски | Перемещение целого контейнера и добавление/удаление nested owner: проверить `UIMeshRigPoint.ResolveOwner` и кеши трёх renderer adapters после предварительного заполнения. При воспроизведении добавить `E/Audit20261023NeoUIMeshRigHierarchyTests.cs` и минимальную инвалидацию в `S/MeshRig/{IUIMeshRigOwner,UIMeshRigPoint,UIMeshRigGraphic,UIMeshRigWorldRenderer,UIMeshRigSpriteRenderer}.cs`. | Neo.UI. Старая RigPoints исключает перенесённые точки, новая включает их однократно. Без воспроизведения массовый рефакторинг не выполнять. |

## 3. Существенное улучшение: общее владение паузой

**Предложение, не реализация.** Сцены, реклама и модальные окна должны независимо удерживать паузу. Текущие GM/EM и `SceneFlowController.Pause(bool)` дают переходы/глобальные записи, но не leases; поиск в Managers/Level/Time/Move готового реестра не выявил.

Разместить обычное C# ядро в Neo.Tools.Managers, чтобы Level не зависел от UI: новые `M/PauseRequest.cs`, `M/PauseLeaseRegistry.cs`, `M/PauseCoordinator.cs`. API: `IDisposable PauseLeaseRegistry.Acquire(PauseRequest request)`, вычисляемое `EffectiveState`; сценовый `PauseCoordinator.Acquire(request)` применяет результат к времени, курсору и явно назначенному GM. Dispose идемпотентен. Минимальный time scale среди активных запросов побеждает; запрос видимого свободного курсора действует до последнего cursor lease. Финальный ForceLockedHidden имеет явно документированный приоритет над восстановлением исходного курсора.

Managers-владелец определяет в `M/GM.cs` внутренний переход состояния без второго независимого time-scale writer; публичные legacy Pause/Resume сохраняются. Baseline каждого управляемого канала захватывается первым владельцем и восстанавливается последним. Внешние writers должны подключаться через тот же координатор; произвольную прямую запись Time.timeScale нельзя объявлять согласованной.

В `S/PausePage.cs` добавить необязательный serialized coordinator и держать один lease между enable/disable. Старые поля/enum и fallback сохраняются, активы автоматически не переписываются. Neo.Level-владелец отдельно предусматривает opt-in forwarding из `Assets/Neoxider/Scripts/Level/SceneFlowController.cs`; обе сборки уже ссылаются на Managers. Владельцу Neo.Tools.Move нужно согласовать `CursorLockController.cs`, чтобы он не перезаписывал курсор во время lease.

Сначала pure-contract тесты в новом `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsManagersPauseLeaseTests.cs` силами Managers, затем UI PlayMode fixture выше: оба порядка A/B, повторный Dispose, исходная пауза, смешанные флаги, отключение/уничтожение и unload. Документы: `D/PausePage.md` для C# Acquire и scene/UnityEvent activation, `D/README.md`; Managers/Level/Move обновляют собственные руководства. Оценка 2–3 дня плюс интеграционная QA; риск средний. До общей реализации минимальные исправления раздела 2 поставляются отдельно.

## 4. Существенное улучшение: отслеживаемые полёты

**Предложение P2.** Очистка объектов при уничтожении уже реализована. Недостающее поведение — терминальный статус: в `S/AnimationFly.cs` IsCompleted учитывает только прибытия, request читается после задержек, OnItemArrived вызывается до регистрации завершения. Отключение GameObject, уничтожение визуала и исключение callback требуют отдельной политики; runtime-воспроизведение этих рисков ещё отсутствует.

Новые `S/AnimationFlyPlan.cs` и `S/AnimationFlyHandle.cs`; изменить `S/AnimationFly.cs`. Предложенный API: `AnimationFlyHandle PlayTracked(AnimationFlyRequest request)`, `Result`, `Status` (Running/Completed/Canceled/Failed), `IsFinished`, идемпотентный `Cancel()` и событие `Finished`. Снимок параметров/делегатов фиксируется при принятии запроса. Повторно использовать существующие преобразования, траектории DOTween, ключи пула и набор owned visuals; отдельный реестр связывает ресурсы с запросом.

Legacy overloads и nested Request/Result не перемещать; IsCompleted сохраняет смысл прибытия. Новая tracked-ветка отменяется при disable/destroy; существующее disable-поведение legacy не менять. Отмена подавляет будущие arrival rewards, не откатывая уже выданные per-item rewards. Исключение callback переводит tracked-запрос в Failed с гарантированной очисткой и gated диагностикой; терминальный переход фиксируется до внешних уведомлений. Глобальный Empty не мутировать.

Минимум: новые `E/Audit20261023NeoUIFlightPlanTests.cs`, `P/Audit20261023NeoUIFlightCancellationTests.cs`: отмена до второго spawn, повторная отмена, callback-отмена/исключение, изменение исходного request, внешний Destroy, два независимых полёта, KeepAlive, нулевой Count. Терминальное событие однократно, соседний запрос продолжает работу. Для сцен добавить только `CancelAllTracked()` и описание в `D/AnimationFly.md`; запуск из C# остаётся typed. Оценка 2–4 дня, риск порядка callback/reward средний.

## 5. Порядок, совместимость и закрытие

1. После фиксации исходников Managers выполнить минимальные pause-регрессии/исправления, затем центральную проверку уже выполненных UI-01–UI-14.
2. Ввести pause-core/API, затем UI/Level adapters; реализовать tracked flights отдельной поставкой. Новые runtime-файлы требуют расширения inventory координатором; metadata создаётся при реализации. Новые asmdef или зависимости пока не нужны.
3. Обновить канонические документы рядом с поставляемыми API. Nested editor UI — только владельцу Neo.UI.Editor; настройки версий остаются в `UIMeshRigPanelCompat`, идентификаторы — через NeoDiagnostics/NeoComponentHealth.
4. Координатор компилирует editor и player на Unity 6.0–6.4 и 6.5+, выполняет EditMode/PlayMode, проверяет panel reload, raycasts, Animator/preview, sprite atlas/masks/lights. Успех одной версии не подтверждает другую.
5. Сохранить serialized names, GUID, prefabs/scenes и release `Samples~`; поддержать developer `Samples` и импортированный `Assets/Samples/NeoxiderTools/10.19.0/...`. Переименование samples не требуется. Проверить package paths и scoped diff/UTF-8 перед поставкой.

Оптимизацию аллокаций `UIMeshRigElement.CollectPointStates`/`UIMeshRigGeometryBuilder.Build` оставить P3 до измерений Profiler; ухудшение производительности не доказано. Остановить закрытие конкретной поставки при воспроизводимом провале, непроверенной compile-ветке либо несогласованной политике внешних pause/cursor writers. Решение об этой общей политике фиксирует координатор; остальные независимые исправления продолжаются. Эта фаза не запускает проверки Unity и не объявляет предложения внедрёнными.
