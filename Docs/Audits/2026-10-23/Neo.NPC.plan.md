# Neo.NPC — индивидуальный план реализации

Дата: 2026-10-23. Та же сессия аудитора, `gpt-6.1-sol`, reasoning `max`.
Основание: [аудит](Neo.NPC.md), актуальный manifest, текущие исходники, документация, регрессии
и границы зависимостей. Код в этой фазе не изменён.

## 1. Уже выполнено

Просмотрены 13 исходников; изменены семь. Закрыты NPC-01–NPC-14: Awake, остановка/disable,
пути/partial arrival, патруль, Combined, замена цели, восстановление навигации, зависимости,
шаг анимации, runtime-кнопки. Обновлены семь английских страниц NPC.

Добавлены 27 случаев в `Assets/Neoxider/Tests/Edit/NPC/Audit20261023NeoNPCNavigationTests.cs`
и `Audit20261023NeoNPCComponentTests.cs`. Выполнены только статические проверки: inventory,
UTF-8, whitespace/delimiters, ссылки, GUID и `git diff --check`; сохранены 71 сериализованное
поле и 48 публичных сигнатур. Компиляция и Unity/NUnit не запускались; исправления требуют
центральной проверки.

Далее `S/` означает `Assets/Neoxider/Scripts/NPC/`, `E/` — `Assets/Neoxider/Tests/Edit/NPC/`,
`D/` — `Assets/Neoxider/Docs/NPC/`. Каждый указанный ниже файл имеет этот точный префикс.

## 2. Оставшийся подтверждённый дефект

**P2, Neo.NPC: смена режима внутри начального события.** Новое замечание фазы планирования.
`S/NpcNavigation.cs:164–165` вызывает `SetModeInternal`
с уведомлением до `initialized = true`. Обработчик `onModeChanged`, вызывающий
`SetMode(Patrol)`, попадает в ветку `:302–305`: меняется только `mode`. `currentBehaviour`
остаётся FollowTarget, и `OnEnable:175` запускает его при публичном `Mode == Patrol`.
Это доказательство по control flow; живое воспроизведение ещё не выполнено.

Минимальная коррекция в `S/NpcNavigation.cs`: после начального уведомления согласовать выбранное
поведение с последним отложенным режимом без промежуточного Enter. Сохранить единственный Enter
на OnEnable. Новая регрессия `E/Audit20261023NeoNPCLifecycleTests.cs`: обработчик меняет режим
в Awake, затем наблюдаются `Mode == Patrol`, один `onPatrolStarted`, путь к патрульной точке
и отсутствие входа FollowTarget. Обновить `D/Navigation/NPCNavigation.md`. Оценка: полдня,
риск повторного вызова событий средний; без новых публичных API и сериализованных полей.

## 3. Проверочный долг и неподтверждённые риски

| Приоритет / владелец | Источник и минимальная проверка | Наблюдаемый результат / дальнейшее действие |
|---|---|---|
| P1, координатор + Neo.NPC | Обе новые EditMode fixtures и восемь существующих `NpcCombatDecisionCoreTests`. | Все случаи выполнены; native preconditions подтверждены. До этого нет статуса Unity-pass. |
| P2, Neo.NPC + Neo.Tests.Play | `S/NpcNavigation.cs`, `S/NpcNavigation.Behaviours.cs`: реальные Awake/OnEnable/disable, pooling, остановка и повторный вход. Текущие тесты вызывают callbacks вручную. | Агент остановлен после disable; один переход события; re-enable согласован с IsActive. |
| P2, Neo.NPC | `S/Navigation/NpcFollowTargetCore.cs`, `NpcPatrolCore.cs`: отказ SetDestination после успешного sample, удаление/добавление NavMesh, нулевая длина пути. | Отказ не даёт PathUpdated/arrival; правила восстановления и одиночного arrival закрепляются после воспроизведения. Это пока риски. |
| P2, Neo.NPC | Native remainingDistance/velocity и Animator setup в новых fixtures; `S/Navigation/NpcAnimationCore.cs`: несколько Tick, пауза с нулевым шагом, возобновление. | Конечные параметры, предсказуемое сглаживание. При проблеме fixture дождаться native состояния ограниченным числом кадров; не заменять проверку skip/no-throw. |
| P3, Neo.PropertyAttribute + Neo.Editor | Четыре PlayModeOnly-кнопки `S/Combat/NpcRpgCombatBrain.cs`. | В редакторе действия недоступны, AutoResolve доступен; C# методы сохраняются. Сначала проверить существующий inspector, исправлять владельцу только подтверждённый сбой. |

Стоимость miss не измерялась. Отсутствие agent-type filter, повторение Combined попыток и
ограниченность snapshot — ограничения для улучшения. Прочие ошибки UnityEvent reentrancy не доказаны.

## 4. P2: планировщик пути и ограниченное восстановление — предложение

**Проблема:** `NpcDestinationResolver` учитывает area mask, но не agent type/связность;
одна попытка содержит до 61 sample. Combined повторно входит после каждого miss, partial
патруль требует внешнего восстановления. Нужен общий явный результат для разных жанров.

**Reuse:** сохранить bounded sampler, `NpcFollowTargetCore`, `NpcPatrolCore`, внешний clock
и gated `NeoDiagnostics`; Unity NavMeshQueryFilter/CalculatePath использовать через адаптер.
Новые менеджеры, пакеты и NoCode-компоненты не нужны.

**Предлагаемый API:** `INpcPathPlanner.TryPlan(in NpcNavigationRequest request,
out NpcNavigationPlan plan)`. Request содержит start/desired, agentTypeID, areaMask, radius
и политику Complete/AllowPartial. Plan возвращает sampled position, path status и typed failure.
Успех планирования ещё не означает принятие агентом; существующий PathUpdated остаётся событием
принятого native запроса. Добавить overload конструкторов follow/patrol с planner, старые сохранить.

**Точные новые файлы:** `S/Navigation/NpcPathPlanning.cs` (контракты),
`S/Navigation/UnityNavMeshPathPlanner.cs`, `S/Navigation/NpcPathRetryState.cs` (состояние/clock,
без scene references). Изменить `S/Navigation/NpcDestinationResolver.cs`,
`S/Navigation/NpcFollowTargetCore.cs`, `S/Navigation/NpcPatrolCore.cs`,
`S/NpcNavigation.cs`, `S/NpcNavigation.Behaviours.cs`.

Сначала контракты и planner fake; затем agent-aware адаптер; затем opt-in retry/backoff и
явный Stop/Retry/Skip для partial patrol; последними Inspector-параметры существующего компонента.
Новые `E/Audit20261023NeoNPCPlanningTests.cs` проверяют clock и бюджет обращений; native отказ
агента проверяется отдельно, не подменяется ошибкой planner.

Приёмка: две перекрывающиеся поверхности разных agent types; disconnected islands без arrival;
100 одинаковых Tick до retry deadline дают одну попытку; после deadline и появления NavMesh
движение возобновляется; замена цели сбрасывает старый запрос. Невалидные числа отклоняются до
native вызовов в новом API. Legacy defaults, сигнатуры и частоту старых событий не менять
молча; новые политики включаются явно. Строгое удержание всей траектории внутри BoxCollider
не входит в эту работу. Оценка 3–5 дней, риск совместимости средний.

## 5. P3: полный snapshot навигации — отдельное предложение

Текущий brain намеренно восстанавливает только mode/target/activity. Предложить
`NpcNavigation.CaptureState()` и `RestoreState(NpcNavigationState state)`; новый
`S/Navigation/NpcNavigationState.cs` хранит runtime mode, targets/destination, running/custom speed,
patrol cursor и ожидание. Потребуются изменения `S/NpcNavigation.cs`, `S/NpcNavigation.Behaviours.cs`,
`S/Navigation/NpcNavAgentCore.cs`, `S/Navigation/NpcFollowTargetCore.cs`, `S/Navigation/NpcPatrolCore.cs`,
`S/Combat/NpcRpgCombatBrain.cs`, `S/Combat/NpcCombatPreset.cs`; новые
`E/Audit20261023NeoNPCSnapshotTests.cs`. Для конкурирующих контроллеров добавить локальный
token/version, чтобы старый snapshot не отменял позднюю внешнюю команду.

Приёмка: chase/hold/clear восстанавливают custom speed, паузу, waypoint и остаток ожидания;
уничтоженная цель допустима; повторный release идемпотентен. Input takeover использует существующий
`RpgAttackController.EnableBuiltInInput`, без правок Neo.Rpg; восстановление также требует ownership.
Политику более полного восстановления включать явно. Scene references остаются runtime-данными,
не помещаются в ScriptableObject. Оценка 2–3 дня, риск reentrancy средний. Это развитие после
основных проверок, не условие закрытия уже выполненного аудита.

## 6. Порядок, совместимость и завершение

1. После завершения конкурирующих source-аудитов координатор фиксирует результаты 27+8 случаев;
   затем regression-first исправление начального события и проверка затронутого lifecycle.
2. Реализовать предложение §4 отдельными проверяемыми порциями; §5 независимо от него, если
   нужен полный takeover. Обычные локальные исправления не требуют нового согласования.
3. Обновить `D/README.md`, `D/InternalTypes.md`, `D/Navigation/NPCNavigation.md`,
   `D/Combat/NpcRpgCombatBrain.md`, `D/Combat/NpcCombatPreset.md`, `D/Combat/NpcCombatScenarios.md`:
   C# примеры и те же возможности в существующем Inspector; custom editor не нужен.
4. Координатор/Neo.Tests.Play владеют `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef`:
   перед новым `Assets/Neoxider/Tests/Play/NPC/Audit20261023NeoNPCLifecyclePlayModeTests.cs`
   нужна ссылка Neo.NPC. EditMode ссылки уже есть. Neo.Rpg сначала завершает изменения selector/
   attack contracts; NPC проверяет интеграцию после них. Network/Save/Managers/Extensions зависимости
   не менять, перенос legacy combat на Neo.Abilities сюда не включать.
5. Централизованно скомпилировать поддерживаемые Unity 6.0–6.4 и 6.5+; версия/ветка не считается
   проверенной по одному другому редактору. Version gates остаются в NeoDiagnostics/NeoComponentHealth,
   без локальных `#if`. Выполнить focused EditMode, затем PlayMode: переходы, partial/rejected пути,
   pause/resume, уничтожение цели и lifetime объектов; требовать реальные состояния и отсутствие новых ошибок.
6. Сохранить сериализацию/ассеты; новые package-файлы получают уникальные `.meta`, координатор
   дополняет inventory. Release validation
   проверяет активный `Samples~`, developer `Samples` и импортированный sample root; layout не
   переименовывать ради NPC. Общие changelog/package/settings обновляет координатор.

Завершение: результаты подтверждены, API/сцены совместимы, UTF-8 и whitespace checks проходят.
Default partial/retry и takeover policy фиксирует координатор; предположение — legacy defaults
и явное включение возможностей. В фазе 2 создан только план; Unity/build/test процессы, коммиты
и дочерние агенты не запускались.
