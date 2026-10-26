# Neo.StateMachine — индивидуальный план реализации

Фаза 2: только план; новые исправления и API ниже ещё не реализованы. Основание: завершённый `Neo.StateMachine.md`, актуальные исходники и manifest; тот же аудитор, `gpt-6.1-sol`, reasoning `max`.

Обозначения точных корней: `S = Assets/Neoxider/Scripts/StateMachine`, `T = Assets/Neoxider/Tests/Edit/StateMachine`, `D = Assets/Neoxider/Docs/StateMachine`. Runtime принадлежит `Neo.StateMachine`; `S/NoCode/Editor` — отдельному владельцу `Neo.StateMachine.Editor`.

## Уже выполнено

Полностью проверены 12 исходников. SM-01–09 исправлены: обновление Priority, удаление после смены источника, переходы наследников StateData, fallback после некорректной цели, отмена устаревшего результата при смене состояния callback’ом, контекст exit actions, отсутствующие Override, результат Validate и null-массив States. Изменены семь исходников, восемь английских страниц; добавлены 32 EditMode-сценария в двух fixtures и два вспомогательных типа с уникальными meta.

Прошли статические проверки UTF-8, деклараций API/полей, GUID, ссылок и scoped `git diff --check`. Компиляция, NUnit, реальный Unity lifecycle и обе версии API не проверены. Эти исправления повторно реализовывать не нужно.

## Оставшиеся подтверждённые дефекты

| Приоритет / владелец | Доказательство и минимальная коррекция | Наблюдаемая приёмка |
|---|---|---|
| P1, runtime | `S/StateMachine.cs:128–155`: OnExit вызывается до смены CurrentState; вложенный ChangeState снова вызывает тот же OnExit. Неограниченный callback способен переполнить стек. Сначала добавить диспетчеризацию вложенных команд и локальные payload событий; `Stop:397–409` включить в тот же механизм. | Однократный запрос третьего состояния из OnExit вызывает каждый exit ровно один раз. Тест ограничивает запрос одним вызовом и безопасно обнаруживает повторный exit без падения процесса. |
| P2, runtime | `StateMachine.cs:337–355` сравнивает только ссылку. Listener может выполнить A→B→тот же cached A, после чего старое решение A→C применяется. Добавить внутреннюю монотонную generation при фактическом enter/stop; проверять её в EvaluateTransitions и условных CanTransitionTo/TryChangeState. | После A→B→A старый переход в C не исполняется; отдельная последующая оценка остаётся возможной. |
| P2, runtime | `S/NoCode/StateMachineData.cs:159–165` передаёт общие transition instances; `StateMachine.cs:170–174` меняет общий `StatePredicate.cs:395–415` timestamp. Вход B сбрасывает ожидание A. Исправление — контекст выполнения предложения A, без клонирования assets. | Две машины с одним asset и разным временем входа переходят независимо; повторный вход A не меняет B. |

Также подтверждено по исходникам: `StateMachineLog.Enabled` общий, обе обёртки присваивают его в OnEnable. P3: после основных исправлений выделить диагностический scope на машину в `StateMachine.cs`, обеих обёртках и `NoCode/StateAction.cs`, переиспользуя существующий gateway. Приёмка: включение второго компонента с противоположной настройкой не меняет логи первого. Это отдельное улучшение, ещё без runtime-воспроизведения.

## Предложение A: контекст выполнения и независимые часы

Проблема разработчика: один конфигурационный asset нельзя безопасно использовать для нескольких персонажей с duration-переходами. Переиспользовать `IState`, StateTransition, существующие hooks, composites и Push/Pop с finally. Каталоги Neo содержат таймеры/форматирование, но подходящего контракта clock injection не найдено; достаточно `Func<float>`, отдельный сервис таймеров или новая зависимость не нужны.

Новые файлы `S/StateEvaluationContext.cs`, `S/StateMachineOptions.cs`: readonly контекст с текущим состоянием, временем входа/оценки и generation; options с clock и режимом execution frame. Добавить `StateMachine(StateMachineOptions options)`, context-aware `StatePredicate.Evaluate(in StateEvaluationContext context)` и `StateMachine.SetCurrentStateEnterTime(float time)` для адресной перенастройки часов. Конфигурационный контекст не хранит scene references.

В `S/StateMachine.cs`, `S/StateTransition.cs`, `S/StatePredicate.cs`, `S/StateMachineEvaluationContext.cs` провести контекст через проверки и вложенные predicates. Сохранить virtual dispatch существующего Evaluate(IState), short circuit и ровно одну инверсию. Duration в новом режиме читает entry time своей машины; вход другой машины не перезаписывает её anchor. Общие mutable custom actions/predicates автоматически изолированными не объявлять; их runtime factories — последующий отдельный проект.

Совместимость требует явного этапа: старый двухпараметровый конструктор сохраняет legacy-режим и семантику публичного SetEnterTime, включая существующие тесты. Новый options-конструктор включает frame. В `S/StateMachineBehaviour.cs` и `S/StateMachineBehaviourBase.cs` добавить явный opt-in с legacy-default для уже сериализованных компонентов. В frame-режиме адресную коррекцию времени выполняет машина; standalone SetEnterTime остаётся прежним. Переключение defaults всей библиотеки — отдельное решение координатора после проверки миграции; до принятия frame старые shared-assets сохраняют известное ограничение.

Не менять имена serialized fields, managed-reference типов, GUID или scene bindings; не использовать JSON-клонирование, теряющее delegates/CLR types. Новые package-файлы потребуют уникальных meta. Оценка: 2–4 дня и интеграционная QA; основные риски — legacy virtual overrides, смешанные composites и неправильное обещание изоляции пользовательского состояния.

Минимальные новые регрессии: `T/Audit20261023NeoStateMachineExecutionFrameTests.cs` — fake clock, два владельца, повторный вход, адресная смена anchor, nested context после исключения, legacy override и inversion. Старые `StateDurationPredicateTests.cs` остаются обязательным контрактом.

## Предложение B: завершённые lifecycle-транзакции

В `S/StateMachine.cs` использовать очередь команд, guard и фиксированные аргументы событий; публичные ChangeState/Stop вне callback остаются синхронными, вложенные команды выполняются после текущего exit/enter/events. Generation из безопасного исправления выше переиспользуется. Добавить `RequestState(TState)` / `RequestStop()` с результатом Applied/Queued/Rejected в новом `S/StateRequestResult.cs`; лимит обработки, например 32 команды за dispatch, прекращает циклическую цепочку и выдаёт gated diagnostic.

Нормальный порядок — Exit(A), Enter(B), Changed(A,B), затем следующая команда. Stop из OnEnter(B) должен дать корректные события B, затем B→null, без null entered payload. Повторный Stop — без дополнительных exit. Через finally освобождать guard; rollback пользовательских побочных эффектов не обещать.

`T/Audit20261023NeoStateMachineLifecycleDispatchTests.cs` проверяет этот порядок, однократные exits, Stop из enter, A→B→A, исключение и ограниченный цикл. Только необходимые публичные request-адаптеры добавляются в обе обёртки, включая строковый путь для StateData. Оценка: 1–2 дня плюс QA; изменение порядка вложенных callbacks документировать как исправление ранее небезопасного поведения.

## Порядок, зависимости и долг проверки

1. Координатор после завершения остальных source-аудитов компилирует текущие изменения; запускает обе новые audit fixtures с 32 cases, `StateMachineOptimizedTests.cs`, `StateDurationPredicateTests.cs`. Это проверка уже сделанного, не новая реализация.
2. Runtime: сначала regression для A→B→A и generation; затем ограниченный dispatcher и события; затем additive frame API и duration-изоляция. После каждого этапа — только затронутые регрессии.
3. После стабилизации API — обёртки. Владелец `Neo.StateMachine.Editor` обновляет явно перечисленные Inspector-поля в `S/NoCode/Editor/StateMachineBehaviourBaseEditor.cs`, затем проверяет Validate/multi-select в `StateMachineEditor.cs`. `TransitionEditorWindow.cs` уже использует TypeCache; изменения формы predicates нужны только при реальном изменении authoring. Runtime не получает зависимость от editor. `Neo.Condition`, `Neo.Extensions`, `Neo.PropertyAttribute` сохраняются без изменений.
4. Настоящие disable/destroy/re-enable/reload, два owner и отключение owner из enter проверить в PlayMode: новый `Assets/Neoxider/Tests/Play/StateMachine/Audit20261023NeoStateMachineLifecyclePlayTests.cs`. В прочитанном `Tests/Play/Neo.Tests.Play.asmdef` отсутствует Neo.StateMachine: ссылку добавляет только координатор до включения этого теста. EditMode SendMessage текущих fixtures не заменяет lifecycle-проверку.
5. Обновить существующие `D/README.md`, `StateMachine.md`, `StatePredicate.md`, `StateTransition.md`, обе страницы Behaviour, `NoCode_StateMachine_Usage.md`, `NoCode/StateMachineData.md`: примеры C# и Inspector opt-in, очередность событий, legacy/frame semantics. `D/Editor.md` — другой владелец. Сэмплы меняет координатор при необходимости; сохранять Samples~/Samples/imported root, без переименований layout.

## Приёмка и остановка

Координатор проверяет компиляцию Unity 6.0–6.4 и 6.5+, включая обе ветки reset в `StateMachineEvaluationContext`; obsolete-ID совместимость остаётся в общих NeoDiagnostics/NeoComponentHealth, новых локальных version gates не добавлять. Затем проверяет импорт UPM sample, сохранность сериализации, отсутствие новых console errors и утечек контекста после reload. Не пройденная ветка компиляции, регрессия legacy SetEnterTime/virtual dispatch или потеря scene bindings останавливает соответствующий этап и выпуск; статическая проверка не считается заменой. Неизмеренные allocation/performance и полная изоляция custom runtime state остаются рисками, а не подтверждёнными исправлениями.
