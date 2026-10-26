# Neo.HtmlToUitk.Editor — индивидуальный план реализации

Статус: план; перечисленные ниже изменения ещё не выполнены. Продолжение исходной аудиторской сессии, `gpt-6.1-sol`, reasoning `max`. Основание: полностью прочитанные PLAN-CONTRACT, завершённый аудит, актуальный manifest и текущие исходники/регрессии/руководство.

Для точных путей: `E = Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/`, `T = Assets/Neoxider/Tests/Edit/HtmlToUitk/`. Все относительные имена ниже раскрываются через эти корни.

## Уже выполнено

Проверены 22/22 C# файла. Семь исправлений уже находятся в шести исходниках: канонизация путей и исключение generated output из Watch; снимок async-параметров; привязка завершения к исходному заданию; отказ от молчаливой потери JSON-настроек; отклонение не-object корня; rename-away уведомления; запрет повторного выделения ресурсов после Dispose. Это не будущие задачи.

Добавлены три файла `Audit20261023NeoHtmlToUitkEditor*Tests.cs`, 15 сценариев, метаданные и `Assets/Neoxider/Docs/UI/HtmlToUitkEditor.md`. Пройдены только статические проверки: inventory, UTF-8, типы, delimiters, GUID, scoped diff и воспроизведение сравнения путей через framework API. Компиляция, NUnit/UnityTest, CLI, импорт и взаимодействие окна не запускались. Phase 2 изменяет только этот план.

## Оставшиеся исправления и долг проверки

Владелец исходников во всех пунктах — `Neo.HtmlToUitk.Editor`; запуск проверок централизует координатор.

1. **P1, потеря несохранённого документа; подтверждено исходником.** В `E/Window/HtmlToUitkWindow.cs`, `LoadJson` заменяет `_doc` и сбрасывает dirty без проверки текущих правок; Open/New/Reload входят в этот путь. Воспроизведение: изменить A, открыть B, вернуться к A — изменение отсутствует на диске. Минимум: общий guard Save/Discard/Cancel перед заменой, загрузка кандидата до присваивания, внутренний `TrySave` с результатом; не очищать unsaved при неуспехе. При Cancel текущие документ/путь/dirty сохраняются; ошибка записи видна и не считается сохранением. Сам сценарий UI ещё не исполнен.

2. **P2, timeout overflow; подтверждена арифметика, утечка процесса — непроверенный риск.** `E/Bridge/CliProcessBridge.cs:Run`: `Math.Max(5, TimeoutSeconds) * 1000`; `int.MaxValue` превращается в отрицательный timeout. После исключения внешний catch не гарантирует остановку уже запущенного процесса. Вычислять ограниченное значение через `long` до старта; обеспечить bounded cleanup после timeout/исключения, не скрывать ошибку termination. Наблюдать нормальный результат быстрого probe при огромном timeout и отсутствие живого PID после принудительного timeout/ошибки ожидания. Не вводить бесконечное ожидание очистки.

3. **P2, старый diagnostics перекрывает текущий stdout; подтверждён путь.** `E/Logic/ReportReader.cs:FromRun/TryReadFile` сначала принимает файл с двухсекундным допуском. CLI `Tools/HtmlToUitk/src/HtmlToUitk.Convert/Jobs/JobRunner.cs:103` при ValidateOnly/refusal не обновляет файл, а `JobEntry.cs:124` выдаёт актуальный JSON в stdout. Старый PASS плюс новый FAIL за одну секунду даёт чужие findings; exit reconciliation не восстанавливает их. Минимум: текущий распознанный stdout имеет приоритет; ValidateOnly не использует оставшийся output report. Для file fallback явно проверять provenance. Не требовать безусловной перезаписи файла: CLI `WriteIfChanged` сохраняет timestamp при одинаковых результатах. Acceptance: текущая ошибка/предупреждение сохраняет rule/message/counts, повторный идентичный успешный запуск остаётся успешным. Изменение `E/Bridge/InProcessBridge.cs` нужно лишь при добавлении общего baseline-контракта.

4. **P2, неполные watch dependencies; подтверждён исходник, интеграционный эффект требует проверки.** `E/Import/GeneratedAssetPostprocessor.cs` пропускает movedFrom и USS; `E/Window/HtmlToUitkWindow.cs:MakeLines/Edit` не перестраивает watcher после изменения CSS/USS/partials. Внешняя CSS-папка вне прежних roots не получает новых подписок. Сначала добавить old/new и USS с дедупликацией, обновлять roots после редактирования этих списков; затем применить dependency index из предложения. Acceptance: внешний новый CSS вызывает один запуск, rename сообщает старый путь, USS импорт доставляет событие; unrelated/generated изменения не запускают задание после подключения индекса.

5. **P1 перед выпуском, verification debt.** Запустить существующие 15 сценариев и прежние HtmlToUitk fixtures. Отдельно проверить медленный A → открыть B → завершить A, изменение output во время запуска, Watch с `../Assets/...`, неуспешное сохранение. Ни исправление окна, ни реальный texture import пока не имеют выполненной UI/import регрессии.

## Существенное улучшение: независимый владелец выполнения

**Предложение, не исправленный дефект:** окно сейчас владеет Poll; OnDisable снимает callback, не передавая завершение/импорт другому владельцу. Разработчику нужны наблюдаемые очередь, завершение и отмена без зависимости от открытого окна.

Переиспользовать `JobDocument`/MiniJson для снимка, `IJobBridge`/`JobRunResult`, существующие path helpers, `IFileView`/InputExpander/GlobMatcher и DebounceGate. Новый runtime manager, MonoBehaviour и NoCode relay не нужны.

Точный состав:

- Новые `E/Logic/JobExecutionRequest.cs`, `E/Logic/IJobExecutionSession.cs`: immutable JSON/пути/flags/ID и `State`, `Task<JobRunResult> Completion`, `CanCancel`, `bool TryCancel()`. Они используют только data/System API; не зависят от RunOptions из Bridge или Unity, сохраняя pure-logic compile glob.
- Новый `E/Bridge/JobExecutionService.cs`: `Capture(JobDocument, string, RunOptions)` на editor thread; `Submit(JobExecutionRequest)` возвращает session и владеет очередью/temp files. Адаптировать `E/Bridge/HtmlToUitkRunner.cs`, сохранив Run/RunAsync.
- Новый `E/Logic/ICancellableJobBridge.cs`: дополнительный overload Run с CancellationToken; менять старый IJobBridge не требуется. CLI реализует capability, legacy in-process честно возвращает недоступность активной отмены.
- Новый `E/Window/JobExecutionEditorAdapter.cs`: единственный editor-update/reload владелец refresh. Предлагаемый default: закрытие окна оставляет уже отправленный запуск сервису; явная отмена подавляет success import. Очистку при domain reload проверить отдельно.
- Новый `E/Logic/JobDependencyIndex.cs`: текущие file/glob/config зависимости и output exclusions; подключить через `E/Watch/HtmlToUitkWatcher.cs`, postprocessor и окно. Начать с конкретного класса, не вводить интерфейс без второй реализации.

Минимальные новые регрессии: `T/PureLogic/Audit20261023NeoHtmlToUitkEditorExecutionTests.cs` для queue/state/dependencies; `T/Editor/Audit20261023NeoHtmlToUitkEditorProcessTests.cs` для controlled process/PID/streams; `T/Editor/Audit20261023NeoHtmlToUitkEditorWindowTests.cs` для switch/save/close. Проверять результаты и владение ресурсами; не тестировать приватные поля отражением. Cancel до gate не создаёт файл/процесс; после terminal нет повторных completion/refresh. Legacy bridge не обещает отмену, которую не умеет выполнить.

## Владельцы, совместимость и порядок

`Neo.HtmlToUitk.Runtime` (`neo-audit-1023-b03-08`) владеет MiniJson и каноническим `HtmlToUitk.md`: повторно использовать API без правок; обратную ссылку на editor guide добавить его владельцем после согласования итоговых docs. `Neo.HtmlToUitk.Tests.Editor` (`neo-audit-1023-b04-01`) владеет тестовой инфраструктурой: согласовать fixtures/probe, не менять чужие тесты/asmdef. CLI JobEntry/JobRunner находятся вне этой сборки: coordinator назначает владельца, если позднее нужен report run ID. Срочное исправление stdout от нового CLI протокола не зависит.

Порядок: центральная базовая проверка → небольшие fixes 1–4 с регрессиями → immutable sessions/queue → capability отмены → editor adapter → dependency index → документация → package/release validation. API Run/RunAsync, поля ассетов, JSON extension keys и importer marker сохраняются; миграция scene/prefab/ScriptableObject не требуется. Sample layout не менять; проверки не привязывать к одному Samples/Samples~/imported пути. Новым package-файлам нужны GUID meta.

Обновлять только editor guide: C# Capture/Submit/dispose, legacy cancellation и Inspector/window save/watch; scene/NoCode пользователи по-прежнему получают те же generated assets. Изменение runtime UX не планируется. Оценка: безопасные fixes 1–2 дня, sessions/index 2–4 дня плюс центральная QA; риск средний из-за process/reload/protocol границ.

## Приёмка и условия остановки

После завершения параллельных source-аудитов координатор компилирует затронутые editor/test assemblies на Unity 6.0–6.4 и 6.5+; каждый добавленный version branch проверяется своей версией. Новые compatibility gates держать централизованно. Выполнить EditMode, filesystem/process и window/import сценарии; PlayMode для этих editor-only изменений не требуется. Runtime handoff проверяет runtime-владелец, если изменится формат generated assets.

Не объявлять release-ready при compile failure, висящем PID, повторном импорте/feedback loop, потере dirty state или чужих diagnostics. Отсутствующая версия Unity/probe означает явный непроверенный gate. Политика закрытия окна пока предложение для решения координатора; symlink/case sensitivity и полная schema validation остаются отдельными непроверенными рисками, без автоматического расширения объёма.
