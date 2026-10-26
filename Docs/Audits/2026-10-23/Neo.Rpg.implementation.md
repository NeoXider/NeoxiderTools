# Neo.Rpg — implementation receipt

Дата завершения исходников: 2026-10-24. Ветка/цель: существующие `demo` / `11.0.0`.

**Статус: source-final для Neo.Rpg; точная межсборочная зависимость Neo.Rpg.Network приведена ниже.**
Дельта принятого NI9 review: NI9-01/02/03 и P3 wording завершены в этом же owner/session; подробности в конце.
Это завершённые source/docs/regression-source изменения, а не повторный план. Выполнение и компиляция
проверок отложены до общего разрешения после завершения всех writers. Ни один новый тест здесь не
объявляется прошедшим.

Прочитаны IMPLEMENTATION-CONTRACT.md, индивидуальный implementation-request, NativeReview-C-contracts,
собственные завершённые audit/plan и текущие затронутые исходники. Правки завершённого аудита сохранены;
`Neo.Rpg.md`, `Neo.Rpg.plan.md`, manifest и общие trackers не переписаны. Исторический полный обзор
59 C# файлов остаётся в аудите; этот receipt описывает последующую реализацию согласованных action IDs.

## Source3 — RPG-POST-RELOCATION-INITIAL-PUBLICATION

**Последний статус (2026-10-25): source-final для released E94 caller.** Предыдущий reserved source-долг
закрыт после явного release ROOT; новое выполнение по-прежнему запрещено до общего freeze.
В этом resume изменены только
`Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs` и этот receipt.

После relocation прочитаны текущие caller, runtime-compatible `Audit20261023NeoRpgSnapshotProbe`,
публичный init/adapter/publication путь `RpgCharacter`, contract/request и reservation map. Map всё ещё
содержит историческую reservation caller; явный новый release ROOT разрешает только caller, сохраняя
PRIMARY-exclusive владение helper/GUID/type mapping/asmdef. Probe и его metadata не редактировались.

Повторно прочитаны точный E94 failure и остальные own Character/Binding cases в source3 Edit XML,
own Play groups и action index. Raw SHA обоих XML совпадают с identities ниже. Исторический Edit snapshot
содержит Character30 Passed/1 Failed и Binding5 Passed/4 Failed; E94 достиг assertion с пустым order,
а не исключения от null helper. Прежнее `SetActive(true)` не запускало native Awake в Edit. Это неверный
init precondition fixture, не доказанный production publication defect; старые Passed не доказывают
новую source identity.

Коррекция сохраняет имя/case и все исходные substantive oracles:

- Probe и character обязаны быть Unity-live. Граф остаётся inactive; template, уникальный save key,
  `_loadOnAwake` и все observers настроены до trigger. До trigger order и Snapshots обязаны быть пустыми.
- В fixture явно включён существующий public `isNetworked`, чтобы настоящий public `Damage(0f)` обнаружил
  transport-neutral probe через adapter routing. Probe возвращает false из TryRoute; DamageType вызывает
  реальный EnsureInitialized до проверки величины урона. Возврат0 проверяется, HP не расходуется.
  Это explicit public initialization contract, а не native Awake/OnEnable evidence; lifecycle через
  reflection, ручной Update и private lifecycle вызовы не используются.
- Не ослаблены точные три уведомления `rebuilt → loaded → snapshot`, MaxHp100 во время rebuilt,
  ровно1 snapshot, saved XP17, HP130/160 и Stats.Count2. Saved payload и generation/publication path
  сохранены; дополнительные события по-прежнему нарушают точное сравнение списка.

Runtime/API/serialized definitions/settings/assets, canonical guides, helpers, aliases, asmdef,
shared trackers и остальные cases не менялись. Все9 NI9 guards и завершённые live-binding controls
сохранены. Нет production contract change, требующего правки публичной документации.

Фактическая проверка этой дельты: чтение существующего XML/source, ручная сверка изменённого test body
и raw identity bookkeeping. Character caller SHA256:
`4153e23ddd468b7558e11a1a8643a7806897d49e5acf6d5ef75a5b851a397f31`;
его meta SHA256 остался `08752a6d34a631fae8b3151924ef2443d5197bc2486c2ce5bb9212b540ad64ef`
(GUID `960c0c7770574abf92142e9940672168`). Binding SHA остался указанным в предыдущем resume.
Probe SHA256 остался `486aa3b22e4842b1ffc0a28bad47806a645dee093f577339c65fb6ffa805c01a`,
probe meta SHA256 — `50d16dd6c5dbec406e47ae349cf1952f29bd417cbb76ac404aa01d059392fd81`
(GUID `a74ea8a7cbe142c9bfa6e391ecf4a664`).

**Оставшееся доказательство:** после нового global freeze ROOT должен исполнить исправленный E94
public-contract case и все9 исправленных Binding cases, сохранив positive/count/payload/tail oracles.
Действительный native Awake initial auto-load также не доказан этой публичной C# проверкой; для такого
утверждения нужен поддерживаемый top-level Play transition/native run. Ни Unity/compiler/tests/checkers,
ни installs/services/children/commits/push не запускались. Новый pass не заявляется. Reserved E94 source
dependency закрыт; native verification debt остаётся явным. Записи завершены.

## Snapshot3 — RPG-ACTIVE-BINDING-POSITIVES (предыдущий resume)

**Статус на момент предыдущего resume: disjoint binding source-final; E94 был reserved dependency.** Изменены только
`Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationBindingTests.cs` и этот receipt.
Production source, API/settings, canonical guides, helpers, asmdef, GUID и shared trackers не менялись:
имеющиеся failures не доказывают новый runtime defect.

Прочитаны актуальные contract/request, own groups E91–E94 в full-edit-failure-action-index.json,
соответствующие triage sections и точные назначенные XML cases. Raw evidence identities:

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` —
  SHA256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`.
- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml` —
  SHA256 `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`.

Edit evidence: E91 destruction cases (resource/stat) остановились на ListenerCount1 вместо2;
E92/E93 initial Current cases получили0 вместо1/2. Четыре InFlight cases записаны Passed, но setup
не доказывал наличие старого binding tail, поэтому это не принимается за достаточное отрицательное
доказательство. E94 auto-load order получил пустой список вместо rebuilt/loaded/snapshot. Play XML
содержит Passed для трёх own projectile и восьми own lifecycle cases: это историческая запись того
snapshot, не новый запуск и не доказательство исправленного binding fixture.

Коррекция setup без синтетического lifecycle:

- Output observers регистрируются до public admission; view активируется, затем вызывается настоящий
  public `Character` setter с непосредственным Unsubscribe/SubscribeIfReady. Один SetActive в Edit
  больше не считается доказательством native OnEnable.
- Resource/Stat InFlight сначала требуют enabled view, ровно2 old listeners, initial50, ordinary51 и
  фактический вызов earlier listener. Только затем разрешается disable/rebind. Сохранены исходные
  empty/7 output и resource old-state88 repeated-rebind7/7 assertions: старый tail не может отсутствовать
  незаметно ещё до критического callback.
- InitialCurrentObserver вызывает setter на активном view после регистрации observers. Сохранены
  точные Current counts1/2 и Max/Percent counts0/1; добавлены Current payloads50 или50/7.
- NI9 destruction cases используют тот же активный public setter. Сохранены ВСЕ прежние oracles:
  ListenerCount2 до/после ordinary control, initial50 → ordinary51, DestroyImmediate(source.gameObject),
  managed reactive52 при Unity-null source и enabled отдельном view, отсутствие stale52 output,
  rebind7/8, old ListenerCount1 и отсутствие output после old.Value99.

Имена, TestCase параметры и количество cases не менялись: fixture сохраняет5 methods/9 authored cases;
четыре метода (8 cases) получили setup/positive-control коррекцию, Rebuild case сохранён. Прочие
implementation fixtures и прежние85 authored cases не удалялись/не переносились этим actor. Cleanup
по-прежнему уничтожает только собственные GameObjects/assets; чужие scenes/providers/sessions не тронуты.
Это explicit public-contract tests, не synthetic Awake/OnEnable/Update native evidence.

**Точная отложенная работа:**
`Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs`, case
`Initialization_AutoLoadPublishesOneCompleteSnapshotAfterRuntimeRebuilt` (E94), не редактировался.
Primary `neo-audit-1023-b03-04` эксклюзивно владеет extraction `Audit20261023NeoRpgSnapshotProbe` в
`Assets/Neoxider/Tests/Fixtures/RPG/Audit20261023NeoRpgSnapshotProbe.cs` и reserved caller. Map проверен
перед каждой записью: при последнем чтении status уже source-final, однако Character fixture всё ещё
находится в helperInventory/reservedFiles/changedExistingFiles; это не трактуется как release caller.
Binding fixture и этот receipt отсутствуют в reserved entries.

После явного release root должен возобновить SAME RPG owner для E94: public initialization либо
действительный native Awake, с неизменными точными rebuilt → loaded → snapshot и complete saved
payload/count oracles. Никакой helper move/extraction/shared reference change здесь не выполнен.

Фактически выполнены только чтение evidence/source, ручная сверка изменённых bodies и raw identity
bookkeeping. Новый binding raw SHA256:
`678849266852b98cca80fb241005c4fe20c10a36abbc82d22ef071b30ef02904`;
meta GUID сохранён `cb54c28695bf45ba97f65e9245875379`.
Unity/compiler/tests/checkers/install/services/commits/push/children не запускались. Все9 binding cases
на новом identity ещё не исполнялись: необходим новый global freeze и root-owned run. Неразрешённое
native доказательство — достижение всех positive/tail/disable/rebind/destruction oracles в новом identity;
E94 initialization/auto-load sequence остаётся отдельным reserved долгом. Записи этой дельты завершены.

## Выполненные действия и наблюдаемое поведение

| Action ID | Реализовано в исходниках | Regression-source / наблюдаемая приёмка |
|---|---|---|
| RPG-RESOURCE-REENTRY | Поколения Build/Clear/InvalidateOperations, проверка идентичности runtime; guarded Current/Max setters; немедленные проверки после resolver, reactive setter и уведомления перед продолжением записи/обхода. | Resolver или reactive/event callback заменяет словарь на C=7: C остаётся 7, старый regen/Current/Percent не дописывается, обход не продолжает старый enumerator. |
| RPG-STAT-REFRESH-REENTRY | Поколения facade и derived pass; RefreshStat проверяет актуальность после ValueState, OnStatChanged и adapter notification. HP damage/heal/death/revive и buff-expiry продолжения также не меняют заменённое поколение. | Замена runtime из stat reactive callback либо OnStatChanged оставляет новый stat/resource C=7; reactive rebuild во время Damage/Heal не публикует старый damage/heal event. |
| RPG-INFINITE-TICK | Общий внутренний RpgTickUtility, double arithmetic, конечные delta/interval/amount, максимум 1024 логических ticks на запись за вызов; лишние целые интервалы отброшены, дробная фаза сохранена. | float.MaxValue, 16777216 и 2048.25 дают не более 1024 доставок; предыдущие 0.125 не теряются; следующий короткий вызов достигает точной границы. Nested TickRegen не воспроизводит pending. |
| RPG-EFFECT-INFINITE-TICK | Та же арифметика для status ticks; remainder/pending потреблены до callback; finally освобождает tick guard. UTC expiry и прежний порядок сохранены. | Отрицательная конечная задержка, огромная delta, NaN/infinity, overflow amount, remove/clear, nested Tick и exception; после exception следующий finite tick работает. Damage остаётся перед due status expiry. |
| RPG-APPLIED-GROWTH | Runtime anchor _appliedGrowthLevel и явный ApplyLevelGrowth(); автоматический рост вверх/вниз сохранён. Manual/None/deferred refresh не применяет новый level случайно. | Buff/base/upgrade refresh оставляет прежний anchor; explicit apply применяет текущий level; auto/hybrid учитывает снижение уровня. Зафиксирована v2 restore policy. |
| RPG-INITIAL-SNAPSHOT-REENTRY | Publication guard начинается до очистки/rebuild; готовность выставлена после derived state. Уведомления объединены во время init/rebuild/profile apply; final notification следует после auto-load. Rebuild из callback имеет не более двух немедленных попыток, затем retry следующего Update. | Синхронный transport-neutral probe вызывает CaptureProfile из NotifyStateChanged без рекурсии; одна полная initial snapshot содержит auto-loaded HP 130/160, XP и stats. RuntimeRebuilt остаётся перед OnProfileLoaded и final snapshot. |
| RPG-BASE-MAX-CONTRACT | Схема v2 сохранена. Локальный restore lookup: последний известный ID выигрывает; сохранённый effective Max применяется до Current и percent regen. Обычные resolver работают для отсутствующих строк. BaseMax не меняется; restore lookup не захвачен постоянным regen resolver. | Повторное восстановление HP 130/160 даёт percent 0.8125 и 10%-regen=16, без временного нулевого HP/death; неизвестный ID не создаёт ресурс. Buff Max120 не превращается в140 при round-trip. Последующий обычный refresh намеренно возвращается к BaseMax и modifiers. |
| RPG-MODIFIER-CONSUMERS | HpRegenPerSecond действует на канонический HP, игнорирует targetId, учитывает clamped stacks перед RegenPercent. AddResourceCurrentFlat выдаётся за новую запись/положительный effective stack delta через общий SO/inline путь. IDs/finite signed amounts захвачены до callbacks; limits → raw delta → settled applied event. Reentry/finally и отсутствие replay сохранены. AttackSpeedPercent явно unsupported. | HP rate (2+3×2+1)×1.5=13.5, target Mana не перенаправляет alias. HP90/100 с Max+50/Current+40 становится130/150 до applied event. Cap/nonstack refresh, remove и profile restore не повторяют grant; negative/unknown/NaN, reentry, rebuild и throwing observer покрыты source fixtures. |
| RPG-BINDING-INFLIGHT-DISABLE | Отдельные subscription closures с version, character/id и exact reactive source. Detach использует кешированные старые states/delegates. | Старый callback из snapshot ReactiveProperty подавлен после disable/rebind и до RuntimeRebuilt rebind; initial Current callback не продолжает старые Max/Percent после смены session. |
| RPG-PROJECTILE-COMPLETE-HITS | _maxHitsPerCast сохранён как serialized capacity hint. Saturation вызывает grow/requery до4096, затем полный allocating fallback; distance/tie sort через общий NeoDiagnostics.StableId. 3D pass остаётся перед2D, ProjectileMaxHits ограничивает доставленные попадания. Старое продолжение останавливается после disable/reinitialize. | Public Initialize + реальный Update/physics flight: capacity1, source/ignored colliders, три hurtboxes одного receiver, ближние/дальняя цели. Ровно две ближние цели получают по одному hit, дальняя и source неизменны. Отдельный mixed-dimensional fixture проверяет 3D precedence. Native выполнение ещё не проводилось. |
| RPG-BRIDGE-NAVIGATION | README ведёт в ConditionBridge.md; старый RpgConditionAdapter.md делегирует канонической странице. | Вложенная ConditionBridge документация/код не изменены. |
| RPG-COMBAT-NAVIGATION | README и старый Tools/Components/AttackSystem/README.md ведут в Rpg/Combat/README.md. | Уже существующий Combat.meta сохранён с GUID 4cea9b0f4c93dc74fb54f2093f1f5789; второй meta не создан. |
| RPG-NETWORK-NAVIGATION | Канонический README ссылается на Network.md. | Вложенная Network документация/код не изменены. |

## Точные исходники реализации

База следующих путей: `Assets/Neoxider/Scripts/Rpg/`.

- `Components/RpgCharacter.cs` — поколения, публикация, applied-growth anchor/API, v2 restore, одноразовые grants.
- `Runtime/RpgCharacterResourceService.cs`, `Runtime/RpgResourceRuntime.cs` — guarded mutations/refresh/regen.
- `Runtime/RpgEffectShelf.cs`, `Runtime/RpgStatResolver.cs` — bounded ticks, stack accounting и HP regen alias.
- Новый `Runtime/RpgTickUtility.cs` + meta — локальный общий helper двух существующих RPG clocks.
- `Components/RpgResourceBinding.cs`, `Components/RpgStatBinding.cs` — exact-source subscription guards.
- `Components/RpgProjectile.cs` — complete queries, sorting, delivery/reuse continuation guards.
- `Runtime/RpgCharacterNetworkCommand.cs`, `Bridge/RpgNoCodeActionType.cs`, `Bridge/RpgNoCodeAction.cs` — additive explicit-growth routing.
- `Enums/BuffStatType.cs`, `Runtime/RpgCharacterProfileData.cs` — точные XML contracts без изменения enum identity/schema.

Переиспользованы существующие resource service, effect shelf, resolver, reactive API, optional transport
boundary и общий StableId. Подходящего готового bounded RPG tick drain в просмотренном каталоге не было;
новый helper внутренний и обслуживает оба прежних clocks, без нового shared infrastructure/framework.

## API, совместимость и намеренные изменения 11.0

- Добавлен `public void RpgCharacter.ApplyLevelGrowth()`; существующие сигнатуры и serialized fields сохранены.
- `RpgCharacterNetworkCommandType.ApplyLevelGrowth = 23` добавлен после22; `RpgNoCodeActionType.ApplyLevelGrowth`
  добавлен последним, ordinal34. Старые значения не сдвинуты. Новый mandatory receiver member не добавлен.
- Profile CurrentVersion остаётся2; effective-Max setters и BaseMax сохраняют свои роли. Normal definition
  caps/clamping остаются в силе. Saved Max действует только в пределах restore call, не становится durable base.
- v2 не хранит applied-growth anchor. Init/restore выбирают saved/current level в automatic modes и1 иначе.
  Manual/deferred/None игры вызывают explicit apply после load/snapshot согласно своей политике. Ранее явно
  применённый deferred anchor не round-trip через v2; это документированное ограничение, не скрытая v3 миграция.
- Ранее неограниченный catch-up теперь отбрасывает excess whole ticks после лимита1024. Это per-entry bound,
  не обещание frame budget для всех actors. Пауза regen по-прежнему пропускает весь crossing frame.
- Resource delta buff теперь работает; OnBuffApplied наблюдает settled state. Cap/nonstack accepted refresh
  сохраняет один applied event без grant. Runtime replacement отменяет старое продолжение/event; nested buff
  application во время commit path возвращает false. Exceptions освобождают guard, не откатывают и не replay
  уже принятые stacks/grants; прерванные до доставки оставшиеся grants не воспроизводятся при refresh.
- Projectile raw buffer больше не является hidden target cap; порядок доставки в пределах dimension стал
  distance/tie deterministic. Между dimensions сохранена 3D precedence. StableId используется без новых
  локальных Unity-version #if/GetInstanceID вызовов.

Для общего changelog/migration writer: перечисленные growth, tick backlog, v2 effective-Max lifetime,
one-shot event ordering/reentry и projectile ordering изменения должны войти в общий 11.0 migration note.
Общие changelog/config/asmdef/trackers этим worker не изменялись.

## Regression-source и public docs

Добавлены четыре fixture с собственными meta:

- `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationRuntimeTests.cs`
- `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs`
- `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationBindingTests.cs`
- `Assets/Neoxider/Tests/Play/RPG/Audit20261023NeoRpgImplementationProjectileTests.cs`

Новые fixtures используют observable public contracts; private field access служит authoring/setup и
подключению probe. Projectile regression не вызывает private hit helpers. Unsupported NonParallelizable
не добавлялся. Три прежних audit fixture сохранены.

Обновлены owned public docs `Assets/Neoxider/Docs/Rpg/`: `README.md`, `RpgCharacter.md`,
`RpgProgressionDefinition.md`, `RpgProjectile.md`, `InternalTypes.md`, `Data/BuffDefinition.md`,
`Data/StatusEffectDefinition.md`, `RpgNoCodeAction.md`, `RpgResourceBinding.md`, `RpgStatBinding.md`,
`RpgConditionAdapter.md`; также разрешённый navigation link в
`Assets/Neoxider/Docs/Tools/Components/AttackSystem/README.md`. Основной вход остаётся RPG/README,
существенные contracts изложены по-английски с русскими пояснениями. Тексты записаны UTF-8.

## Точная межсборочная зависимость

**Neo.Rpg.Network owner:** `Assets/Neoxider/Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs`, метод Execute,
на момент read-only просмотра имеет case SetInvulnerable и завершается без ApplyLevelGrowth. Нужен
`case RpgCharacterNetworkCommandType.ApplyLevelGrowth: _character.ApplyLevelGrowth(); break;`
с сохранением действующих trusted-state authorization/rate limits. Без этого additive команда23 из
remote-client facade маршрутизируется, но server dispatcher её не исполняет. Этот файл не изменялся.

Core snapshot guard теперь готов для promised active-server regression владельца Network: реальный
MIRROR adapter должен синхронно CaptureProfile после complete init + auto-load, без рекурсии, half-state
и повторного current grant при ApplyNetworkState. Transport-neutral probe в core не заменяет это
доказательство. Отдельно учитывать описанное ограничение v2 для explicit deferred-growth anchor.

**ConditionBridge owner:** NativeReview-C-contracts C-03 и readiness regression принадлежат ему.
Controller active/enabled gates этим worker не ослаблялись. **Combat owner:** canonical page/meta уже
присутствовали; изменены только разрешённые входные ссылки. Никаких runtime правок nested assemblies.

## Фактическая проверка и оставшийся verification debt

Выполнены только обычное чтение текущих исходников/API, ручной просмотр scoped git diff/status и чтение
metadata. Так подтверждены сохранённые serialized имена, additive enum значения, наличие новых sources
и meta, существующий Combat GUID и конкретный отсутствующий Network dispatcher case. При ручной сверке
регрессионных sources исправлено имя существующего upgrade поля на increasePerPoint. Это не compile/test
pass. Старые audit static результаты не переносятся на новый implementation snapshot.

**Не запускались:** Unity, compiler/build/test runners, product-checkers/validators, installs, services,
commits/push или child agents. Общая файловая система и изменения остальных writers сохранены.

После общего writer barrier coordinator должен:

1. Передать Network owner additive dispatcher case и active-server fixture; затем скомпилировать MIRROR
   и fallback сборки, включая Unity6.0–6.4 и6.5+ stable-ID branch. Ни одна ветка здесь не объявляется рабочей.
2. Выполнить три сохранённых audit fixtures и четыре новых implementation fixtures, затем согласованный
   общий набор tests. Отдельно наблюдать cap/phase recovery, generation replacement, initial auto-load
   snapshot и отсутствие one-shot replay на сетевом получателе.
3. Выполнить native dense 3D/2D projectile fixtures; дополнительно подтвердить tie ordering, насыщение
   полного4096 fallback, pooling/reinitialize callbacks и frame/lifetime assumptions в целевой Unity.
   Grow/requery и fallback прочитаны в исходниках, но native completeness/performance не измерены.
4. Выполнить общие package/doc/sample/encoding checks разрешёнными coordinator инструментами. Этот worker
   не менял release sample layout и не запускал общий validator во время concurrent writes.

Опциональные предложения из исторического плана — durable BaseMax/profile v3 и широкий state/clock
framework — не реализованы и не выданы за fixed work. AttackSpeedPercent consumer также остаётся
отложенным design решением. Дополнительных подтверждённых Neo.Rpg source fixes за пределами выполненных
action IDs этот receipt не оставляет; незавершённые доказательства и Network integration перечислены явно.

## NI9 — завершённая source-коррекция трёх residuals

SAME original owner/session продолжил текущую реализацию. IMPLEMENTATION-CONTRACT.md, полный актуальный
индивидуальный request и все 205 строк NativeImplementationReview-9.md прочитаны. Исходные audit/plan,
прежние approved contracts, 62 implementation cases и их bodies сохранены. Нового framework, nested
assembly правок, shared config/tracker изменений или execution не добавлено.

| Принято root | Исправление | Новые authored controls (UNEXECUTED) |
|---|---|---|
| NI9-01 / P2 | UpgradeStat захватывает _runtimeGeneration до point debit и сразу после UpgradePointsState callback прекращает superseded continuation до investments/count/refresh. | Четыре ApplyTemplate/RebuildRuntime × S7/omitted-S случая: replacement value7, UpgradeCount/investment0, profile без устаревшей строки и лишнего stat refresh. Ordinary success сохраняет cost1, investment1, stat12, points/reactive/stat/resource events. |
| NI9-02 / P2 | Общий чистый internal RpgResourceRuntime.ClampCurrent используется setter и pre-admission Spend. Положительный cost требует candidate < Current до reactive/event/pause/публикации; старые finite/zero/unknown checks сохранены в прежнем порядке. | Service Current1e8/cost1 и clamped floor отклоняются повторно без изменения Current/Max/Percent signals, ResourceChanged, pause0.25 и phase0.125. Cost5 даёт stored debit8; fractional0.25 работает. Десять precedence controls; facade Spend/TrySpendResource отвергают zero stored debit без transport snapshot, затем публикуют нормальный rounded spend. Fractional/unknown/zero/NaN/+Infinity facade controls сохранены. |
| NI9-03 / P2 | В обоих exact-session predicates первым условием стоит cached character != null с Unity semantics, до любых source getters. Enabled/id/version/identity и cached old-state detach сохранены. | Реальные resource/stat earlier listeners вызывают DestroyImmediate(source.gameObject). До destruction проверены live source, ListenerCount2 и ordinary output50→51; после destruction old reactive хранит52, отдельный view остаётся enabled, output52 отсутствует. Rebind даёт7→8, old ListenerCount1 и отсутствие старого output после old.Value99. |
| NI9 guide / P3 | InternalTypes.md уточнён: invalid delta/amount подавляет tick-damage/regen delivery, независимый UTC expiry продолжает вызывать expiry callbacks. | Прежний Status_InvalidDeltaStillExpiresWithoutCorruptingFinitePhase сохранён без изменения ожидаемого expiry behavior. |

**Superseded return policy:** локальный UpgradeStat возвращает true после принятого исходного debit,
даже если его points observer заменил runtime. Это acceptance receipt исходной операции; upgrade не
записывается и debit не откатывается в replacement. False сохраняется для первоначального отказа.
Существующая accepted-request семантика network routing не менялась. Правило отражено в XML/public guide.

**Spend compatibility:** это проверка положительного собственного stored debit до observers, не требование
точного равенства nominal cost. Float backend остаётся прежним: cost5 на1e8 может списать8 и принимается.
Положительный запрос с нулевым stored decrement теперь false, без side effects. Service сначала отвергает
nonfinite, затем сохраняет finite nonpositive no-op success до ID lookup; facade TrySpendResource сохраняет
свой первоначальный unknown-ID precheck. Новый public API/schema/serialized field не добавлен.

Точные runtime файлы этой дельты:

- `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` — UpgradeStat generation fence и XML policy.
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgResourceRuntime.cs` — internal pure ClampCurrent.
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs` — Spend candidate admission/XML.
- `Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs` и `RpgStatBinding.cs` — cached-source liveness.

В существующие prefixed ImplementationRuntimeTests, ImplementationCharacterTests и ImplementationBindingTests
добавлены **8 методов /23 authored cases**: service14, facade2, upgrade5, binding2. Предыдущие 35 методов /
62 cases четырёх implementation fixtures сохранены; теперь **43 метода /85 authored cases**. Это ручной
source inventory, не test discovery. Play projectile fixture и существующие meta не изменены в NI9 resume.

Public docs этой дельты: `Assets/Neoxider/Docs/Rpg/RpgCharacter.md`, `RpgResourceBinding.md`,
`RpgStatBinding.md`, `InternalTypes.md`. Global migration writer должен учесть отказ zero-stored-debit
spend и acceptance-return policy superseded upgrade; shared migration/changelog здесь не менялись.

Фактически выполнены только source/API reads и ручной scoped diff/read review, включая все новые controls.
Unity/compiler/tests/product-checkers/install/services/commits/push/children не запускались; ни одно
ожидаемое наблюдение не выдано за исполненное. После global freeze coordinator должен выполнить прежние
62 cases и новые23, включая actual Unity-null snapshot tail и ordinary controls, и независимо reread
новые source identities. Ранее перечисленные Network/native integration debts этим resume не закрываются.

### NI9 raw source identities

Raw-byte SHA256 bookkeeping for independent reread; no product validation or test pass.

| Path | Bytes | SHA256 |
|---|---:|---|
| `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` | 65252 | `9dfadfbaac83b7797da38ff0064979648ef3214b1fad98a7018a1e733bfd637c` |
| `Assets/Neoxider/Scripts/Rpg/Runtime/RpgResourceRuntime.cs` | 5489 | `5452d4b995547b89b0a035f9e4ae407781bd613a5fa400c927a2b5ff9af5f439` |
| `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs` | 15302 | `3a71ffc8f6528288e5aa9d90fe5a84a375096921e4cef59e5e09b9233936c5f5` |
| `Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs` | 6871 | `542aad718b19a7adb065d87e6a5194ef826b1afc137925f15161831e7ad7aa23` |
| `Assets/Neoxider/Scripts/Rpg/Components/RpgStatBinding.cs` | 4022 | `91e070f366d19ebbe37dc26347f68d85382f857b696aa04676ce87493af9829a` |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationRuntimeTests.cs` | 19018 | `082158e032c46f810eec8eaf40d5708320f0d01c08100095c7ffdac3f630ab75` |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs` | 30796 | `75eeb9be124c256a5e84457a859416126025ace9355744cf14e7cf96d5d6cc23` |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationBindingTests.cs` | 9267 | `a635e3f46afd905e3c7f9d83e4c32a587f142988a140fd7b87aa6ae44696b68d` |
| `Assets/Neoxider/Docs/Rpg/InternalTypes.md` | 5810 | `2ead60e62f9809e368bba2e618a8f129310b1b2416020b4b5f90e1a753e72086` |
| `Assets/Neoxider/Docs/Rpg/RpgCharacter.md` | 17505 | `547b8897909bdfd411d9190066cc1d3dbe35c0e0cd36cdd4f608c1b0f5e8bb2d` |
| `Assets/Neoxider/Docs/Rpg/RpgResourceBinding.md` | 1742 | `025a97a762d4d4d67571e0627ad9ed5377c442d6a1bdfaa045276b8f5f27f3b4` |
| `Assets/Neoxider/Docs/Rpg/RpgStatBinding.md` | 1331 | `9f024a66d118152ab274a1a138813f1e7a3553769d0a81ad0ddeec9bf06d6243` |
