# Neo.StateMachine.Editor — индивидуальный план реализации

Продолжение исходного аудитора, фаза 2, 2026-10-23. Прочитаны [аудит](Neo.StateMachine.Editor.md), полный `PLAN-CONTRACT.md`, manifest, пять текущих исходников, гайд, регрессии и runtime-контракты. Изменяется только план; реализации и Unity-проверки ниже — будущая работа.

## Уже выполнено

В пяти файлах `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/` исправлены: скрытый `contextOverrides`; отсутствие настроек большинства предикатов; Undo и редактирование отсоединённых переходов; незакрытая GUI-группа при удалении; вставка action до успешного конструктора; неверная обработка нескольких выбранных конфигураций; cleanup временного editor. Сохранены публичные объявления и существующая сериализованная схема. Добавлены `Assets/Neoxider/Docs/StateMachine/Editor.md` и 15 случаев в `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs`.

Прошли статические inventory/API/UTF-8/GUID/link/whitespace проверки и scoped `git diff --check`. Компиляция, 15 тестов и IMGUI workflows worker не запускались. Новых подтверждённых неисправленных дефектов не установлено. Переоткрытие окна после восстановления перехода через Undo/domain reload — документированное ограничение.

## Приоритеты и проверочный долг

| Приоритет / статус | Источник и воспроизведение | Владелец, минимальное действие и наблюдаемый результат |
|---|---|---|
| P1, проверка перед релизом | Пять source-файлов и regression fixture; результата компиляции у worker нет | Координатор после стабилизации source-аудитов компилирует editor/player и тесты на Unity 6.0–6.4 и 6.5+, выполняет 15 случаев. Наблюдать количество/значения действий, `Evaluate`, Undo/Redo, отсутствие skips. Сбой: воспроизведение, минимальная owned коррекция, соответствующая регрессия. |
| P1, проверка взаимодействия | `StateMachineBehaviourBaseEditor.cs:51`, `TransitionEditorWindow.cs:79,220`, `StateMachineEditor.cs:232`; reflection-тесты не рисуют Inspector | Координатор проверяет Context Overrides, Settings встроенного/custom/Condition Entry предиката, Remove, multi-selection и save/reopen. Удаляется нужная строка без layout errors; чужие assets неизменны. Исправлять подтверждённое отклонение. |
| P1, интеграционная проверка | Runtime `StateMachineBehaviourBase`, `ConditionEntryPredicate` | Координатор и владельцы `Neo.StateMachine`/`Neo.Condition`: Play Mode smoke — Owner читает хозяина, Override1 читает элемент 0, изменение значения меняет результат условия. Runtime-кнопки недоступны в Edit Mode. Перед исправлением локализовать editor/runtime источник. |
| P2, ограниченное покрытие | Fixture проверяет память/JSON; сохранение `.asset`, GUI и ошибочная регистрация editor не исполнены | Дополнить `Audit20261023NeoStateMachineEditorTests.cs` сценариями save/reload временного asset, позднего action callback и чужого owner. Cleanup проверять без подмены глобальной регистрации редакторов. |
| P2, документационный долг | Гайд `Editor.md` существует; текущий module README на него не ссылается | Координатор/владелец `Neo.StateMachine` добавляет ссылку в `Assets/Neoxider/Docs/StateMachine/README.md`. Пользователь находит editor workflow из канонической точки входа. |

Runtime-владелец уже исправил `Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs:191`: отсутствующий initial state/неверные endpoints дают `Validate == false`. Повторно не планировать. `Neo.Editor` владеет `CustomEditorBase`/GUI, `Neo.Editor.Tests` — shared asmdef; существующий reflection-fixture не требует новых references.

## P2: предложение стабильной идентичности переходов

**Не реализовано.** Сейчас `StateTransition` — inline-класс без ID; `TryGetCurrentTransition` ищет живой объект по ссылке. После десериализации ссылка теряется, индекс может указывать на соседа, имена не уникальны. Нужна возможность восстановить выбранный переход после Undo, переупорядочивания и domain reload без ошибочного редактирования другой строки.

Повторно использовать существующие `StateTransition`, `StateMachineData.Transitions`, serialized properties, Undo, `CustomEditorBase` и `NeoxiderEditorGUI`. `NeoDiagnostics.StableId(Object)` не заменяет логический ID обычного C# перехода. Новый менеджер, graph framework и runtime→editor зависимость не нужны.

Предлагаемые контракты:

- `StateTransition.Id`: read-only `string`, backed by новым serialized `transitionId`; legacy null/отсутствие возвращает пустую строку.
- `StateMachineData.TryGetTransitionById(string id, out StateTransition transition)`: ordinal lookup; пустой, отсутствующий или неоднозначный ID возвращает `false` и `null`, без mutation/logging.
- Editor utility `StateMachineTransitionIdentityUtility.RepairTransitionIds(StateMachineData owner)`: возвращает количество исправленных записей; один Undoable authoring operation, повторный вызов без изменений не dirty-ит asset. Сохраняет первый непустой уникальный ID, генерирует GUID только для отсутствующих/повторных записей.
- `TransitionEditorWindow.TryShowWindowById(StateMachineData owner, string transitionId)`: `bool`; открывает только однозначно найденный переход. Отдельное имя сохраняет компилируемость существующего `ShowWindow(null, null)`, которую дополнительный двухаргументный overload мог бы нарушить. Старый `ShowWindow(StateTransition, StateMachineData)` остаётся.

| Точные файлы | Владелец и изменение |
|---|---|
| `Assets/Neoxider/Scripts/StateMachine/StateTransition.cs` | `Neo.StateMachine`: additive поле/свойство Id; без генерации в runtime |
| `Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs` | `Neo.StateMachine`: чистый lookup-контракт; идентификаторы не меняют правила переходов |
| `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineTransitionIdentityUtility.cs` + `.meta` | Предлагаемый новый файл `Neo.StateMachine.Editor`, будущая область feature-work: serialized repair, Undo и уведомление открытых окон до замены IDs |
| `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditor.cs` | Explicit Upgrade/Repair button; новый transition получает новый ID в операции добавления, даже если Unity клонирует предыдущий элемент |
| `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/TransitionEditorWindow.cs` | Serialized asset owner + selected ID; повторный lookup после Undo/reload; legacy/ownerless fallback остаётся безопасным |
| `Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorIdentityTests.cs` + `.meta` | Новые проверки lookup, migration и реального выбора/изменения перехода; shared asmdef не менять |
| `Assets/Neoxider/Docs/StateMachine/Editor.md` | Editor C# API, кнопка migration и scene/NoCode workflow |
| `Assets/Neoxider/Docs/StateMachine/StateTransition.md`, `Assets/Neoxider/Docs/StateMachine/NoCode/StateMachineData.md` | Runtime-владелец документирует optional Id/lookup и отсутствие влияния на gameplay |

Миграция явная, без asset mutation из repaint, `OnValidate` или runtime evaluation. Старые assets продолжают работать с пустыми IDs. Уникальность ограничена одним owner: копия целого asset может сохранить IDs; новый/скопированный transition внутри owner получает другой ID. При repair неоднозначных IDs открытые окна этого owner сбрасывают выбор до изменения данных: старый duplicate ID не должен автоматически привязать окно к первому соседу. Неизменённые gameplay поля, предикаты, initial state и runtime API сохраняются.

Минимальные регрессии: legacy asset без поля; lookup пустого/неизвестного/повторного ID; два одинаково названных перехода после reorder/Undo/redo/reload; удаление выбранного и Undo удаления; repair при открытом окне на duplicate; идемпотентный repair и Undo migration; новый transition не наследует ID предыдущего. Проверять изменение только нужного перехода и сохранение прежнего `Evaluate`, а не только наличие свойства. Оценка: 1–2 дня реализации плюс централизованная проверка; риск средний, основные зоны — migration и восстановление editor selection.

## Порядок и условия завершения

1. Дождаться стабилизации source-аудитов, выполнить P1 compilation/регрессии; исправить только доказанные сбои. Отдельно подтвердить GUI и scene slots. Пока этого нет, отмечать verification debt открытым.
2. Если improvement включён координатором в feature scope: сначала runtime-владелец реализует additive Id/lookup и их контракты; затем editor repair, новая идентичность при Add, восстановление окна и новые регрессии. Не писать ID consumer раньше согласованного runtime API. Компонентные adapters не требуются.
3. Обновить английские C# и scene/NoCode guides, добавить каноническую ссылку; проверить UTF-8, публичную совместимость, unique GUID и scoped diff. Русский внутренний план не заменяет публичную документацию.
4. Координатор повторяет compilation обеих Unity-линеек; Editor assembly не попадает в player. Version-sensitive Object APIs при необходимости проходят только через общие `NeoDiagnostics.StableId`/`NeoComponentHealth.StableId`, без локальных `#if`. EditMode покрывает authoring/Undo/serialization; PlayMode нужен только для указанного scene-context smoke, поскольку gameplay-правила proposal не меняет.

Финальный feature acceptance: выбранный переход переживает reorder/Undo/reload, удалённый не заменяется соседом, legacy assets выполняют те же правила, repair полностью откатывается. При неоднозначном ID окно прекращает mutation и предлагает повторный выбор/repair. Провал compilation, запись в чужой asset, потеря serialized данных или неоткатываемая migration останавливают выпуск этой feature; остальные независимые проверки продолжаются.

Активен `Assets/Neoxider/Samples~`; сцены, layout и sample assets менять не требуется. Release checks учитывают developer/imported layouts по AGENTS.md; package/asmdef/changelog — ответственность координатора. Он выбирает релиз для optional ID API; отложенное улучшение не блокирует уже исправленный editor после закрытия P1. Unity-проверки и предложение пока не выполнены.
