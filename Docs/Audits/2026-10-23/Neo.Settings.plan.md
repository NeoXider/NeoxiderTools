# Neo.Settings — индивидуальный план реализации

Статус: план, реализация предложений не начата. Продолжение исходной сессии аудитора; `gpt-6.1-sol`, reasoning `max`. Основание: полный `PLAN-CONTRACT.md`, собственный завершённый аудит, актуальная manifest entry, текущие исходники, документация, регрессии и границы зависимостей. В фазе 2 изменяется только этот файл.

## 1. Что уже завершено

Проверены все десять C# файлов. В `GameSettings.cs`, `GameSettingsComponent.cs`, `SettingsView.cs` исправлены S1–S7: NaN/невалидные defaults и enum, противоречивые fullscreen flag/mode, потеря deferred-сохранения и запись чужого контекста, устаревшие значения UI, оставшиеся callbacks отключённой панели, null FPS list. Публичные сигнатуры, serialized-поля, числовые значения enum и save keys сохранены. Изменение эффективного fullscreen mode при переключении документировано.

Добавлены 19 методов / 24 NUnit cases в `Assets/Neoxider/Tests/Edit/Settings/Audit20261023NeoSettingsTests.cs`, обновлены пять документов `Assets/Neoxider/Docs/Settings/`. Выполнены статические проверки инвентаря, совместимости деклараций, UTF-8, ссылок, парности подписок и scoped `git diff --check`. **Unity-компиляция и исполнение тестов не выполнялись.** Эти исправления не являются оставшейся работой; новых подтверждённых неисправленных дефектов не установлено.

## 2. Приоритеты: проверка прежде дополнительных исправлений

| Приоритет / статус | Доказательство и следующий шаг | Владелец, минимальная коррекция, наблюдаемый результат |
|---|---|---|
| P1 — долг компиляции, не подтверждённая ошибка | `Neo.Settings.asmdef` и `Neo.Editor.Tests.asmdef` используют UI, но не перечисляют `UnityEngine.UI`; Play asmdef перечисляет UI, но не `Neo.Settings`. Проверить реальный граф после завершения меняющих исходники аудитов. | Координатор; владельцы `Neo.Editor.Tests` и `Neo.Tests.Play`. При подтверждении добавить только необходимые прямые references, включая `Neo.Settings` для будущего Play fixture. Результат: runtime и fixtures компилируются без unresolved UI/settings types. |
| P1 — долг EditMode | Запустить новый fixture и существующие `Neo.Tools.Tests.GameSettingsTests`, `Neo.Editor.Tests.GameSettingsStaticResetTests`. Текущая новая фабрика использует `Attach` и `_setInstanceOnAwake=false`, обходя обычный bootstrap. | Neo.Settings + координатор. Исправлять только воспроизведённые сбои; legacy tests не переписывать под новое поведение. Результат: конечные значения, provider output, уведомления и независимые UI listeners соответствуют assertions. |
| P1 — долг lifecycle/PlayMode | `ScheduleDeferredSave` запускает coroutine; текущие синхронные EditMode cases не доказывают timed completion, настоящий pause, scene reload и обычный `Awake/Init`. | Neo.Settings; после актуализации `Neo.Tools.Managers`. Добавить Play fixture, при неподдерживаемой EditMode coroutine перенести туда только зависимые cases. Проверить debounce при `timeScale=0`, disable/destroy, replacement, повторный вход с Domain Reload off, duplicate service. Результат: финальное значение сохраняется, старый сервис не пишет новый prefix, нет лишних подписок/инициализаций. |
| P2 — неподтверждённый риск VSync | `GameSettings.SetGraphicsPreset`/`SetQualityLevel` вызывают `QualitySettings.SetQualityLevel`, но, в отличие от `ApplyAllEngine`, не восстанавливают `s_vSync`. Воспроизведение: `SetVSync(false)`, смена quality с отличающимся profile VSync, сравнение API и engine. | Neo.Settings. Только при расхождении восстановить запрошенный VSync после смены quality в `GameSettings.cs` и добавить engine regression. Результат: `QualitySettings.vSyncCount == (GameSettings.VSync ? 1 : 0)`. |
| P2 — платформенная проверка | Authored display tests проверяют запрошенные/сохранённые значения, не переход окна ОС. | Координатор + Neo.Settings: fullscreen off → manual resolution → fullscreen on; desktop player, доступные WebGL/console targets. Сохраняется windowed mode после manual resolution; неподдерживаемые режимы описаны явно. Исправление только после воспроизведения. |

`SaveState` без `SaveProvider.Save`, чтение старых значений при выключенном persist flag, индексная идентичность разрешения, отсутствие транзакции у `SkipUntilFlush` и reserved `ApplyButton=1` — документированное поведение. Не менять их под видом подтверждённого бага.

## 3. P2 — предложение: обычное C# ядро и отдельная edit session

**Проблема.** Статический фасад смешивает состояние, validation, engine effects, provider и coroutine owner. `SkipUntilFlush` не позволяет создать настоящий Apply/Cancel: draft уже меняет live state и попадает в чужой flush.

**Повторное использование.** Оставить существующие `GameSettings`, `SettingsGroup`, `SettingsPersistMode`, preset/resolution structs, `ISettingsLocalization`, `SaveProvider`/`ISaveProvider`, UI suppression и realtime debounce. `Neo.Reactive` здесь не нужен: существующего change event достаточно. Новый scheduler framework, singleton, NoCode actions, localization backend и editor window не требуются.

**Публичный дизайн.** `SettingsState` — неизменяемое значение девяти настроек; `SettingsConfiguration` — копия defaults, persist flags и каталогов, без scene references. `SettingsController` принимает configuration, `ISettingsStore`, `ISettingsApplicator` и необязательный callback планирования deferred save; без scheduler используется немедленная запись. Его `Current`, `Version`, `Changed`, typed `Set*`, `ResetGroup`, `LoadState`, `Flush` не обращаются к `Screen`, `QualitySettings` или MonoBehaviour. Существующие enum используются как данные; отдельная asmdef не вводится.

`ISettingsApplicator.Apply(SettingsState, IReadOnlyCollection<SettingsGroup>)` изолирует engine effects, `ISettingsStore.Load/Save` — доступ к данным. `GameSettingsComponent.Controller` предоставляет read-only доступ к controller, компонент сохраняет адаптеры и coroutine; `GameSettings` остаётся совместимым фасадом default controller. Применение quality предшествует восстановлению Performance/VSync.

`SettingsController.BeginEdit()` возвращает `SettingsEditSession`: read-only `Draft`, typed setters, `Commit(SettingsPersistMode)` с `SettingsCommitResult`, `Cancel()`. Draft ничего не применяет и не сохраняет. Commit проверяет captured Version, нормализует изменения, публикует один Current/Changed и сохраняет изменённые разрешённые группы. Конфликт возвращается без перезаписи; draft сохраняется для явного Cancel/повторного открытия. Атомарность нескольких provider keys и rollback диска не обещаются: storage exceptions передаются вызывающему коду. Preview и конфликтное автоматическое merge исключены из первой версии.

`SettingsView` получает opt-in `StagedApply=2`; `0` и `1` сохраняют поведение, включая flush при disable. Добавить `ApplyChanges()`/`CancelChanges()` как void UnityEvent adapters, `LastCommitResult` для C# и Inspector-событие конфликта. В staged mode reset меняет draft, disable отменяет draft; чужой live-panel flush не может его захватить. Сервис задаётся optional serialized reference; fallback — `GameSettings.Context`, без scene search.

## 4. Точные файлы и границы

Все новые production types остаются в `Assets/Neoxider/Scripts/Settings/`: `SettingsState.cs`, `SettingsConfiguration.cs`, `SettingsController.cs`, `SettingsEditSession.cs`, `SettingsCommitResult.cs`, `ISettingsApplicator.cs`, `ISettingsStore.cs`, `UnitySettingsApplicator.cs`, `SaveProviderSettingsStore.cs`. Изменяются существующие `GameSettings.cs`, `GameSettingsComponent.cs`, `SettingsView.cs`; keys, data structs и существующие enum numbers сохраняются.

Тесты: существующий собственный `Assets/Neoxider/Tests/Edit/Settings/Audit20261023NeoSettingsTests.cs`; новые `Audit20261023NeoSettingsControllerTests.cs`, `Audit20261023NeoSettingsEditSessionTests.cs` в том же каталоге; `Assets/Neoxider/Tests/Play/Audit20261023NeoSettingsPlayModeTests.cs`. Новым package files нужны уникальные `.meta`; расширение manifest inventory и общих asmdef — координатору.

Документация: существующие `Assets/Neoxider/Docs/Settings/README.md`, `GameSettings.md`, `GameSettingsComponent.md`, `SettingsView.md`, `SettingsPersistMode.md`: независимый C# controller/session пример и Inspector Apply/Cancel wiring; README остаётся единственным входом. Английский стиль сохраняется, краткое русское объяснение — где принято.

Cross-assembly зависимости: `Neo.Save` (`neo-audit-1023-b05-07`) согласует provider semantics до store adapter; `Neo.Tools.Managers` (`neo-audit-1023-b07-06`) — актуальный Singleton lifecycle до bootstrap tests. `Neo.Extensions` (`/root/audit_extensions`) владеет diagnostics/version gate; `Neo.PropertyAttribute` (`neo-audit-1023-b04-08`) — существующими authoring attributes, новых изменений там не требуется. References/fixtures согласуются с `Neo.Editor.Tests` (`neo-audit-1023-b03-04`), `Neo.Tests.Play` (`neo-audit-1023-b06-04`) и координатором. Их файлы этим планом не меняются.

## 5. Порядок и приёмка

1. Зафиксировать актуальный source snapshot после конкурирующих аудитов; выполнить центральную компиляцию/reference checks, существующие regressions, lifecycle/engine воспроизведения. Безусловно сначала закрыть подтверждённые интеграционные ошибки минимальными правками.
2. Добавить state/configuration/controller и fake adapters; затем production adapters и совместимый static facade. Сохранить legacy event ordering, чтение/запись enabled groups и cancellation-only `FlushDeferredSaveCoroutine`.
3. Добавить versioned sessions, затем opt-in component/UI adapters, документацию и минимальные smoke examples без обязательной sample-зависимости.
4. Проверить: два controller не делят state; invalid inputs сохраняют bounds; Cancel даёт ноль engine/store calls; Commit даёт один Changed; устаревшая session возвращает Conflict без записи; чужой flush не сохраняет draft; store failure не объявляется успешным durable commit. Legacy 0/1 scenes и старые keys загружаются без миграции assets.
5. Центрально выполнить EditMode/PlayMode и компиляцию минимум на представителе Unity 6.0–6.4 и на 6.5+, реально проверив обе ветви общего version gate. Не добавлять локальные `#if`/obsolete identity calls. Release validation: UTF-8, scoped diff, GUID, package import и sample paths; сохранять `Samples~`, учитывать dev `Samples` и импорт `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/`. Не переименовывать layout ради этой фичи.

Оценка предложения: 3–5 инженерных дней плюс platform QA; риск средний из-за event/persistence timing. Завершение отдельных этапов допускается независимо. Нельзя объявлять runtime/release проверенными при неисполненных тестах, непроверенной ветви Unity или изменившихся зависимостях. Невоспроизведённый риск не становится исправлением. Решения о поддерживаемых платформах и будущем preview остаются координатору; первая версия предполагает staged без preview. Рутинные подтверждённые исправления не требуют нового запроса разрешения.
