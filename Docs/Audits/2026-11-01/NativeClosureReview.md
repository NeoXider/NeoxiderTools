# Независимое ревью закрытия аудитов — 2026-11-01

Ревизия проверки: `demo`, `d271531103210ebcd71c3a5d5bbafdb06ff425a5`, после merge `origin/main`, пакет 11.0.1. Роль: независимый reviewer; этот файл не заменяет отдельный ClosureLedger.

**Вывод: закрытие всех аудитов по исполнению на текущем дереве не подтверждено.** Все 72 владельца действительно имеют `source-final`, исходники ключевых исправлений присутствуют, но raw receipts и frozen manifests предыдущей приёмки в этом клоне отсутствуют. Новый подтверждённый runtime-дефект Inventory/Abilities/Cards/shared raycasts этим проходом не установлен. На первом проходе выявлены два узких расхождения прослеживаемости: query-summary исправлена root по documentation source, Random hash квалифицирован одним LF без изменения исторического реестра. Остальное ниже явно разделено на source evidence, исторические заявления и непроверенную приёмку.

## Границы и фактически выполненная проверка

- Прочитан корневой `AGENTS.md`; применены правила UTF-8, сохранения API/serialized данных, reusable runtime и честного статуса проверки. В качестве контекста прочитаны правила `unity-game-agent`; Unity по заданному ограничению не использовалась.
- Проверены все 72 записи `implementation-state.json`, наличие всех 144 первоначальных audit/plan и 72 implementation receipts, их разделы остаточных рисков/предложений и SHA-256 receipts. Углублённое сопоставление плана, последующих owner/native/root handoffs и текущего source выполнено для InteractableObject, Inventory, Abilities, Cards и трёх bounded actions. Это не новый полный построчный аудит всего product source 72 направлений.
- Прочитаны `RootIntegration.md`, `Assets/Neoxider/Verification11.md`, `review-actions.json`, `RootInventoryAndDemoAmendment.md`; проверены актуальные sample regression sources и текущая идентичность `CardGame.unity`.
- Использованы только чтение файлов, `git status/log/diff`, `rg`, Python для UTF-8/JSON/SHA-256 и запись этого отчёта. Unity, Player, .NET suites, compiler, services, worktrees и субагенты не запускались. Свежих test passes, Profiler captures или ручной gameplay-приёмки здесь нет.
- Product source, исторические отчёты и пользовательские pre-existing paths не менялись. На первоначальном проходе единственный owned write — этот файл; последующая передача ownership двух ClosureLedger artifacts явно описана в дополнении ниже.

Значения статусов: **подтверждено** — непосредственно наблюдаемое свойство текущего source/артефакта; **дефект** — точное воспроизводимое расхождение текущих файлов; **не проверено** — необходимое evidence отсутствует либо проверка не выполнялась. Подтверждённое наличие source не означает выполненный Unity-тест.

## Общая цепочка закрытия

| Проверка | Статус | Evidence и следующее действие |
| --- | --- | --- |
| 72 направления | подтверждено | `Docs/Audits/2026-10-23/implementation-state.json`: 72 owners, все `source-final`; все audit/plan/receipt files существуют и читаются как UTF-8. Это статус окончания bounded source work. |
| Хеши source-final receipts | подтверждено с одним дефектом ниже | 71/72 текущих receipt bytes совпадают с `sourceFinalReceiptSha256`; единственное несовпадение Random объясняется ровно одним удалённым завершающим LF. |
| 257 review actions | подтверждено | `review-actions.json`: 133 `approved-source-correction`, 113 `source-closed-unexecuted`, 7 `source-implemented-unexecuted`, 3 `bounded-source-investigation-and-native-acceptance-required`, 1 `documented-policy-proposal-deferred`. Нет ни одного action-level executed-final статуса. |
| Авторитетность tracker для текущего исполнения | не проверено | `implementation-state.json:2` сообщает `verification-complete-ready-to-push`, а `review-actions.json:2102` — historical pending freeze. Не следует превращать все 257 записей в открытые runtime bugs или все 72 source-final в executed closure. Нужна текущая action-to-source-to-receipt квалификация в новом ClosureLedger с сохранением истории. |
| Финальный frozen manifest | не проверено | `implementation-state.json:1543`: `Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261026T192828Z/manifest.json` отсутствует. Его заявленный SHA-256 `30c3114416afb2404a011447e49764eec838294e182d717de10b97651ddde70b` нельзя перепроверить по файлу. |
| P32 acceptance/raw XML/manual receipts | не проверено | `implementation-state.json:1584` и `RootIntegration.md:16` указывают на отсутствующий `Local/Audits/assembly-2026-10-23/unity/final/p32/`. Весь `Local/Audits/assembly-2026-10-23` отсутствует; под `Docs/Audits/2026-10-23` нет XML/TRX. Существующие `Docs/QA/HtmlToUitk-20261014/*.xml` относятся к другой дате и не заменяют P32. |
| Старые результаты и текущая 11.0.1 | не проверено | `Verification11.md:3` правильно датирует результаты 26–27 октября; новый package-wide run на `d2715311` не заявлен. В истории после `ac380a5f` есть HTML/runtime, dependency/release и material changes. Без manifest нельзя квалифицировать всё текущее дерево только заголовком `source-final`. |

**NR-CLOSE-01 — блокер evidence, не утверждение о сломанном runtime.** Восстановить исходные raw receipts, source/dependency manifests и qualification records из сохранённого архива, проверить заявленные SHA-256, сопоставить полные leaf identities и источники с текущим деревом. Сначала использовать сохранившиеся доказательства; отсутствие файлов здесь не доказывает, что старые запуски не происходили. Если raw восстановить нельзя, статус соответствующего acceptance остаётся `не проверено`; новые исполнения выполняет уполномоченный оператор после снятия ограничения Unity, с отдельным сохранением исходных failures/skips.

## Подтверждённые дефекты прослеживаемости

### NR-CLOSE-02 / P2 — entry point обещает отсутствующую query-count сводку

**Статус: исходный дефект документации закрыт root по source; native evidence не проверено.** При первом чтении `Assets/Neoxider/Docs/Migration11.md:31` отправлял читателя в `Verification11.md` за recorded 1/100/1000 scope, но сводка отсутствовала. Root затем добавил explicit historical shared-query row и missing-raw qualification. Независимо прочитан diff и первичный `Docs/Audits/2026-10-23/INTEGRATION.md:5–7`: он сообщает Interactor112, включая все 12 camera/keyboard matrix cases, PASS на обеих SDK; весь focused P6 при этом имел 460 Passed / 21 Failed, что не смешивается с interaction scope. Новая строка честно отделяет counters от CPU/GC и внешней EventSystem работы. `SharedQueries.md:142–144` сохраняет историческую pending-source формулировку; она не должна отменять более позднее P6 report claim.

Узкое действие: в новом каноническом closure entry явно связать 12 matrix cases и dense/parity controls с их конкретными последними raw receipts, snapshots, SDK и ограничениями; затем исправить обещание в Migration/Verification. Если receipts недоступны, прямо написать `не проверено`, сохранив P5 как историческое заявление. Не добавлять выдуманные CPU/GC/FPS числа.

### NR-CLOSE-03 / P3 — Random receipt digest отстаёт на один LF

**Статус: дефект metadata, не дефект Random runtime.** `Docs/Audits/2026-10-23/implementation-state.json:1122` хранит:

```text
recorded: 9940edd74f5e16da57ef2570fafc79c2b38413aff6f9006b549a102216ce3700
current:  02d85b7aa6cc97a5b04b4d320d0e028d5febba956149938a9691abe1adc2e83a
```

Текущий `Neo.Tools.Random.implementation.md` плюс **один LF** даёт ровно recorded hash. Текстовое расхождение полностью ограничено удалённой конечной пустой строкой; оснований заново открывать RND-01–06 нет. Узкое действие: записать эту byte-level qualification и текущий hash в новом ledger. Старый receipt/реестр в этом проходе не переписывался.

## Общие camera/player raycasts 1/100/1000

| Предмет проверки | Статус | Текущий source/evidence |
| --- | --- | --- |
| Общая raw query для одинакового контекста | подтверждено по source | `Scripts/Tools/InteractableObject/InteractionPhysicsQueries.cs:240–251` сравнивает geometry/context точно; `:439–464` возвращает общий Entry до `Execute`. Target identity не входит в ключ. Это действительно общий результат, а не только общий Ray. Все пути ниже имеют префикс `Assets/Neoxider/`. |
| Реальные native counters | подтверждено по source | `InteractionPhysicsQueries.cs:477–489`, `:509–550`: `CountNative` непосредственно перед реальными Physics/Physics2D initial/fallback calls. `:577–597` различает purpose и callbacks. Никакой mock backend не подставлен. |
| Полнота saturated query | подтверждено по source; исполнение не проверено | `:482–494` полный `RaycastAll`; `:526–555` полный intersection/planar fallback, filtered overload сохраняет тот же `ContactFilter2D` и List. `:567–574` оставляет spare capacity. Borrowed hit validity/complete проверяются отдельно. |
| Planar arguments и lifetime | подтверждено по source | `:398–419`, `:505–522` сохраняют исходные Vector2 без Ray normalization; `:422–429`, `:624–645` задают callback/frame epochs. Внешние world changes требуют явного `Invalidate`/Fresh (`:336–343`), это документированный предел. |
| Camera 2D/3D × N=1/100/1000 | не проверено как независимый run | `Neo.Tools.InteractableObject.implementation.md:31–37` сообщает P5 PASS всех шести полных camera cases, positive/stable-miss native1/cacheN−1 и transition nativeN при callback boundaries2N. Сам XML отсутствует. Исторические Source3 failures в `UnityVerification.md:197` относятся к более раннему снимку и не должны выдаваться за текущий runtime FAIL. |
| Полные 12 camera/keyboard matrix cases | подтверждено наличие; исполнение не проверено | `Tests/Play/Tools/Audit20261023NeoToolsInteractableObjectPlayerLoopTests.cs:13–24` сохраняет все 12 identities; `:79–91`, `:99–145` отделяет positive, transition miss и следующий stable miss; `:172–189` требует native1/cacheN−1, ноль callbacks/LOS/fallback, один frame epoch и N×H classification. |
| Обычные callbacks и измеряемая область | подтверждено ограничение | Fixture `:148–155` требует null event objects. `SharedQueries.md:119–134` открыто отделяет stable epoch от callbacks и внешних EventSystem queries. Нельзя распространять стабильный native1 на произвольные callback mutations или unique LOS. |
| Aggregate CPU/GC/retained memory и внешние EventSystem calls | не проверено | 1000 collinear targets дают 1 000 000 classification visits в positive slice (`PlayerLoopTests.cs:189`, исторический P5 owner receipt). N native casts устранены в stable shared context, но N×H projection остаётся. Profiler markers есть (`SharedQueries.md:112–123`), capture/CPU/allocation receipts здесь нет. `UnityVerification.execution-plan.md:202` прямо различает counters и Profiler acceptance. |

Узкое следующее действие: восстановить последние raw results **всех 12** cases на каждой SDK и dense 17/64/1000/parity controls; связать их с текущими source hashes. Если обязательная profiler-приёмка действительно отсутствует и в архиве, сохранить её открытой и выполнить один ограниченный native measurement с aggregate external calls, CPU/GC и projection cost. Не создавать новый controller/framework и не ослаблять transition/callback или gameplay assertions ради native1.

## Inventory, Abilities, CardGame

| Область | Статус | Evidence и предел вывода |
| --- | --- | --- |
| Inventory committed finalization/drop origin | подтверждено по source | `Runtime/InventoryComponent.Operations.cs:195–238` удерживает первый fault и пытается Save в finally; `InventoryComponent.Grid.cs:125–149` пытается оба finalizers. `InventoryComponent.Commit.cs:129–148`, `:165–236` сохраняет physical origin и сначала возвращает в него без безусловного overwrite. Тесты/JSON round-trip на SDK в этом проходе не выполнялись. |
| Inventory nullable save sentinel | подтверждено по source | `Core/InventorySaveData.cs:64–88`, `:156–197` различает absent optional instance и отдельный `WireSlot.ItemId=-1`. Старое NI70 замечание не подтверждается текущим source как оставшийся sentinel bug. Native JsonUtility semantics всё ещё требуют receipt. |
| Exact UID selection/use | подтверждено по source | `Core/InventorySelection.cs:62–75` проверяет owner, index, item kind/UID; `Runtime/InventoryComponent.Selection.cs:30–61` берёт exact selection; `InventoryUseAction.cs:80–121` коммитит consumption и сохраняет effect/notification fault. `RootInventoryAndDemoAmendment.md:9–13` не обещает fabricated UID обычным stacks. |
| Inventory sample | подтверждено наличие meaningful scenario; run не проверено | `Tests/Play/SampleRuntimeSceneSmokeTests.cs:247–285`: 9 slots, exact UID/payload podium/recall, CELL count3→2, HP45→70, reset. Это вызовы authored Button listeners через helper, не собственное доказательство OS keyboard/mouse input. Ручное E/key2 заявлено отдельно в `Verification11.md:69`, raw capture здесь отсутствует. |
| Abilities catalog/Clear/payment actions | подтверждено по source | `Domain/AbilitySystem.cs:304–306` использует granted `slot.Blueprint`; `Domain/Events/AbilityEventBus.cs:129–149` сохраняет internal reactions при Clear; `AbilitySystem.cs:394–435` проверяет cap/aggregate cost до payment и отклоняет неуспешный TrySpend. `:432–434` честно не обещает rollback предыдущих платежей при callback mutation. |
| Abilities aggregate dispatch | подтверждено по source; run не проверено | `AbilityUnitBehaviour.cs:137` использует target subscription, `AbilityEventBus.cs:131–139` захватывает matching/global observers. Авторский 1/100/1000 expected snapshot/invocation budget — не CPU benchmark (`Neo.Abilities.implementation.md:122`). |
| Abilities sample | подтверждено наличие meaningful scenario; run не проверено | `SampleRuntimeSceneSmokeTests.cs:288–327` требует damage/heal, immediate cooldown rejection, Zap12 и затем30 на level3, native cooldown и reset; layout и Filled sprites проверяются отдельно. Полный gameplay/visual pass не выводится из наличия fixture. |
| Cards acceptance/layout ownership | подтверждено по source | `Components/HandComponent.cs:188–243`: commit membership, release deck cleanup, SetParent, затем layout/notifications с membership version. `DeckComponent.cs:401–520`: source generation, result admission, accepted cleanup handoff, rollback rejected card только в текущий source. `:564–578` создаёт UI card под Canvas spawn, world path сохраняет отдельное поведение. |
| Cards sample click coverage | подтверждено source scope; run не проверено | `SampleRuntimeSceneSmokeTests.cs:331–396` требует полный deck36, scale0.7, viewport, реальный `EventSystem.RaycastAll`, dispatch в top graphic и ровно один OnRoundStarted с уменьшением hand counts. Это EventSystem dispatch, не самостоятельный OS-mouse run. P27/P29/P31/P32 результаты и ручные rounds описаны в `RootIntegration.md:8–16`; raw отсутствуют. |
| Текущий CardGame asset совпадает с P32 scene hash | подтверждено | `Assets/Neoxider/Samples~/Demo/Scenes/_ExampleGame/CardGame.unity` SHA-256 `b2eb4e86c473628477383f38d5889765e3e8bacd7d0f1e7891959bc040f2bd65` точно совпадает с `RootIntegration.md:16`. Это byte identity сцены, не повторное исполнение P32. |

Все ссылки в первых восьми строках таблицы на `Runtime`, `Core`, `Domain`, `Components` относятся соответственно к `Assets/Neoxider/Scripts/Tools/Inventory/`, `Assets/Neoxider/Scripts/Abilities/` или `Assets/Neoxider/Scripts/Cards/`. Точные basenames сохранены, чтобы не смешивать runtime и одноимённые tests.

## Deferred actions: что нельзя молча считать реализованным

Три `bounded-source-investigation-and-native-acceptance-required` записи не доказывают три оставшихся runtime-дефекта:

| Action | Статус | Сопоставление с текущим source |
| --- | --- | --- |
| `CONDITION-INDEXER-BINDING` (`review-actions.json:194`) | подтверждено source correction; native не проверено | `Assets/Neoxider/Scripts/Condition/ConditionValueSource.cs:256–266` и `:336–346`: AmbiguousMatchException обрабатывается, indexers отвергаются. Старый EnsureCache lead не следует повторять как живой дефект без нового repro. |
| `SETTINGS-QUALITY-VSYNC` (`:527`) | подтверждено source correction; native не проверено | `Assets/Neoxider/Scripts/Settings/GameSettings.cs:268–269`, `:285–286`: requested VSync восстанавливается после SetQualityLevel. Реальный engine preset scenario требует receipt. |
| `OTHER-NAV-NOPATH` (`:793`) | подтверждено source correction; native не проверено | `Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs:996–1031` различает pending/request frame, accepted no-path arrival и invalid/partial blocked. `:362–364` наблюдает предыдущий request до refresh. Native navigation acceptance не выводится из guards. |

Реально оставленные возможности и политики:

- **`DIALOGUE-SHARED-FONT-MUTATION` — подтверждено, policy deferred.** `review-actions.json:625` явно оставляет прежнюю политику; `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueUI.cs:121–135` всё ещё удаляет invalid entries из borrowed TMP font fallback list. `Assets/Neoxider/Docs/Tools/Dialogue/DialogueUI.md:37` это документирует. Это не выполненное read-only font redesign; rendering failure в данном проходе не доказан.
- **RPG durable BaseMax/growth schema — подтверждено как не доставленное расширение.** `review-actions.json:476` принимает restore-call-only v2 semantics и откладывает durable base fields/v3. `RpgCharacter.cs:1801` не копирует effective Max в BaseMax; `Assets/Neoxider/Docs/Rpg/RpgProgressionDefinition.md:42–45` явно не сохраняет отдельный applied-growth anchor. Нельзя обещать durable manual/deferred anchor persistence.
- **RPG AttackSpeedPercent — подтверждено unsupported runtime feature.** `review-actions.json:485` откладывает consumer; `Assets/Neoxider/Scripts/Rpg/Enums/BuffStatType.cs:56` сохраняет enum311, `Assets/Neoxider/Docs/Rpg/Data/BuffDefinition.md:45–47` прямо сообщает отсутствие consumer. Это не закрытая реализация attack timing.
- **Inventory P01 state editing — подтверждено как proposal, не как выполненная функция.** `Neo.Tools.Inventory.plan.md:23–44` предлагает identity/revision payload update и capture equipped state; `Neo.Tools.Inventory.implementation.md:365–366` не объявляет этот дизайн доставленным. Exact UID selection/use не эквивалентен persistent редактированию ammo/durability визуала. Подтверждённый use/demo pass нельзя расширять на этот сценарий.
- **Abilities typed manually stepped delivery/segment hits — подтверждено как proposal.** `Neo.Abilities.plan.md:29–60` и `Neo.Abilities.implementation.md:162` оставляют его будущим. `AbilityProjectileBehaviour.cs:81–101`, `:116–147` использует scene deltaTime, point-radius sampling и enemy filter. Нельзя обещать segment tunneling prevention, allied projectile delivery или domain-clock stepping как выполненную часть closure.
- **Cards P01 zones/instance IDs/reservations/pure Drunkard model — подтверждено как proposal.** `Neo.Cards.implementation.md:49` прямо оставляет их будущими; исправленные R1–R4, ordering и tween ownership не означают реализацию этого framework.

Эти explicit deferrals надо отразить в новом ledger отдельной причиной disposition. Они не являются автоматическим основанием переписывать модули или объявлять прежние bounded fixes незавершёнными. Но абсолютная формулировка «все пункты всех планов реализованы» была бы неверна.

## Остаточные acceptance limits и handoff root

`Assets/Neoxider/Verification11.md` уже сохраняет ограничения: Dice OS drag/drop непроверен, sustained Survivor movement не сертифицирован, audible Audio quality не проверено, HDRP explicit capability skip; declared Unity6000.0 и остальные платформы не сертифицируются двумя SDK6000.3/6000.6. `.NET NotExecuted` также не превращается в passed. Отсутствие новой проверки здесь не отменяет задокументированные прежние положительные результаты и не превращает их в fresh current-source acceptance.

Узкий порядок закрытия:

1. Восстановить raw archive и exact frozen/dependency identities; квалифицировать current source, historical passes, failures, skips и scope limits по action IDs в новом ClosureLedger.
2. Сохранить traceability corrections NR-CLOSE-02/03 в новом closure record; не переписывать исторические результаты ради более удобного статуса.
3. Для raycast acceptance закрыть конкретные 12-case/dense/parity/Profiler evidence gaps, отличая native query count от total work. Для Inventory/Abilities/CardGame восстановить соответствующие P10/P27/P29/P31/P32 receipts и capture scope.
4. Только действительно отсутствующее или изменившееся обязательное execution покрытие передать оператору Unity после освобождения разрешённого окружения. На этом этапе Unity удерживается VampireDota; этот reviewer её не трогал.

До выполнения пункта 1 независимый итог — **source corrections подтверждены в перечисленном scope; полное execution closure не проверено**. Пункты с честной deferred disposition остаются deferred, подтверждённых новых runtime bugs этот проход не добавляет.

## Дополнение: передача ownership ClosureLedger

После OOM исходного CLI1 root передал этому reviewer авторство `ClosureLedger.md` и `ClosureLedger.json`. Новый ledger учитывает все 72 owners/257 actions, exact artifact/current source identities, recorded substantial-improvement dispositions и missing raw. Этот отчёт независим от прежних implementation owners, но после передачи ownership не является независимым approval созданного им ledger. Итоговая вычитка нового ledger остаётся root/другому reviewer. Старые implementation-state/action trackers и source-final hashes не переписывались. Генератор bookkeeping находится только в task-owned `D:/Temp/Neoxider-Audit-20261101/native-closure-ledger-builder.py`; repository writes ограничены тремя явно переданными audit artifacts.

## 2026-11-02 — независимая проверка HtmlUnityNativeInitial evidence

**Статус: целостность и ограниченная квалификация архива подтверждены; CTA acceptance остаётся FAIL.** Проверен [HtmlUnityNativeInitial-20261102.zip](Evidence/HtmlUnityNativeInitial-20261102.zip), 854299 bytes, SHA-256 `b80cce9ca9dd8b42d3f378c42e0df9e130e68bb598161f4cd51b67bf1f2ae831`. Это новые доступные raw receipts трёх конкретных HTML запусков, а не восстановление отсутствующего архива старых 257 audit actions.

### Целостность и состав

- ZIP CRC проверен чтением всех members: ошибок нет. `archive-members.json` объявляет 353 payloads; фактически ровно 353 плюс сам manifest. Все 353 размера и SHA-256 совпали; extras, missing, duplicate names и unsafe relative paths — 0.
- Payloads: 332 shared input files, два отдельных input manifests, 15 launch/log/XML/summary/restoration файлов трёх запусков, три `operator-tools` и `qualifications.json`. Никакой source extraction в проект или запуск архивных скриптов не выполнялся.
- Независимо прочитаны qualifications, launch records, summaries, XML leaves, relevant native log lines и все три operator tools. Логи идентифицируют Unity `6000.3.14f1 (d68c3f99a318)`, D3D11/NVIDIA renderer и Mono paths. Launch/result exit codes и все объявленные summary evidence hashes совпали с соответствующими архивными bytes. Это Windows graphical batch Editor evidence; native SDK6.6, Player и IL2CPP отсюда не следуют.

### XML и retained failures

| Архивный run | Независимый XML разбор | Квалификация |
| --- | --- | --- |
| `edit-new-contracts-and-drafts-u63-r1` | 88 leaves: **79 Passed / 9 Failed / 0 Skipped / 0 Inconclusive**, exit2 | Web contracts — 65 Passed; Draft fixture — 14 Passed / 9 Failed. Восемь failures — `EmptyConstraint`/null-path `ArgumentException`, один — Watch expected2/actual1. Исходный run остаётся failed. |
| `edit-drafts-affected-u63-r2` | 301 leaves: **301 Passed / 0 Failed / 0 Skipped / 0 Inconclusive**, exit0 | Draft23 + replacement10 + watch2 + runner2 + pipeline8 + pure logic256. Все 23 Draft fullnames совпали с run1 без удалённых/добавленных identities; все девять прежних failed identities здесь Passed. Web65 в этот filter не входит. |
| `play-cta-u63-r1` | Ровно 2 leaves: **0 Passed / 2 Failed / 0 Skipped / 0 Inconclusive**, exit2 | Только DesktopHeroCta и MobileHeroCta. Оба retained failures: 0/8664 matching background pixels, assertion threshold >3898.7998. Это фактический oracle failure, не установленная причина дефекта production rendering. |

Во всех трёх XML root counters соответствуют реально перечисленным leaves и summaries; duplicate fullnames отсутствуют. CTA stacks указывают на архивный `Assets/HtmlToUitkShowcase/Tests/PlayMode/VeloraShowcaseInteractionTests.cs:560`, вызванный из `:476`. В сохранённом source `AssertCtaPaintedPixels` выполняется на строке476, а `PointerClick` — только на477. Следовательно, два CTA cases **не подтвердили synthetic pointer path или modal opening**. Физический/ручной ввод, обычное видимое GameView и browser/Unity video parity этим запуском также не установлены. PNG generated resources в shared inputs не являются test screenshots; отдельных CTA readback captures в этом архиве нет.

XML SHA-256: run1 `b714cc341f9c17fcd66f370e65a6c060e06511e4917aedca1fa6f222deb9a02f`; r2 `94f0c59aea97bcc38a30d856ddc2968aeaf1c29ce69af4431ef96f2c1e382823`; CTA `bc039f4f9d1ae21fab1b1d91d092fc6cfc45c3d194b7c4c4912b5dae9771f7fa`. Эти группы пересекаются и не складываются в один новый all-pass suite.

### Source identity: 58 старых C# против 332 prelaunch inputs

Оба r2/CTA input manifests содержат **332 идентичных path→bytes/SHA записей**. Shared tree имеет ровно тот же набор332, без extras/missing; каждый архивный файл совпал по size/hash. Отличаются metadata самих manifests, включая frozenUtc, поэтому речь о совпадении file maps, а не bytes двух manifest-файлов. Указанные frozenUtc — `21:17:05.704980Z` и `21:19:34.741598Z` 1 ноября UTC — предшествуют соответствующим launch StartedUtc `21:17:15.4876043Z` и `21:19:45.0532545Z`. Postrun summaries сообщают changedFrozenPaths=[] и protected count12/differences=[]; это проверяемые записи оператора, а не новое наблюдение reviewer во время тех процессов.

Для run1 **не создаётся ретроспективный 332-input prelaunch freeze**. Его source qualification ограничена root-recorded direct pre/post checks58 C# и12 protected paths, указанными в `qualifications.json`/run1 summary. Независимо проверен существующий [HtmlApiTypecheck-20261101.zip](Evidence/HtmlApiTypecheck-20261101.zip): CRC clean, все83 payload hashes/sizes совпали, все58 source snapshots соответствуют `manifest.json.sources`. Absolute Windows paths этого manifest приведены к package-relative только для сопоставления, исходные bytes не менялись. Все58 присутствуют в позднем shared332; между старым API snapshot и r2 из этих58 изменён **только** `Audit20261101NeoHtmlToUitkEditorDraftTests.cs` — сохранены старый failing вариант и новый вариант с SerializedProperty path read и attached-panel Watch prerequisites. Ни r2 manifest, ни API-only compilation не подменяют отсутствующий широкий run1 freeze.

Live comparison scoped332 на `2026-11-01T21:26:57Z` обнаружил missing0 и ровно один changed path: `Assets/HtmlToUitkShowcase/Tests/PlayMode/VeloraShowcaseInteractionTests.cs`, где уже работает разрешённый CLI02 source writer над capture instrumentation. Поэтому retained CTA failure квалифицируется **по архивному fixture**, а новая instrumentation не считается исполненной. Это моментальное read-only сравнение, не новая freeze текущего дерева.

### Preferences и следующий шаг

`editorprefs-before.json`/`editorprefs-after.json` в ZIP отсутствуют намеренно. Опубликованы только restoration metadata: run1 сообщает original-prefix restoration и удаление своей empty CLI key; r2 — один удалённый owned empty key; CTA — ноль. Статически прочитанный restore tool сначала сравнивает original names/kinds/data и проверяет все extra entries, затем допускает удаление только точной lowercase CLI key с пустым String либо one-NUL Binary; unknown state прекращает операцию до удаления. Эти scripts включены как source evidence из внешнего task directory. **Registry reviewer не запрашивал; приватные значения не читались и фактическое совпадение registry state самостоятельно не перепроверялось.** Утверждение restoration здесь ограничено сохранёнными metadata и guards, а не публикацией private-state proof.

Следующее действие — сохранить эти raw failures неизменными, independently review новый instrumentation diff и только после writer stop/review/freeze выполнить разрешённый targeted CTA rerun с фактическими captures. До этого причина pixel failures и pointer acceptance остаются непроверенными. Это bounded HTML evidence review **не закрывает все257 исторических actions**, arbitrary HTML/CSS/JS backend, visual parity, physical input или платформенные ветки. В этом проходе изменён только `NativeClosureReview.md`; registry, процессы, Unity/compiler, product source, archive bytes и остальные ledgers не менялись.

## 2026-11-02 — финальная bounded integrity qualification CTA stencil

**Статус: archive integrity PASS; сохранённый depth0 run остаётся FAIL, новые depth/stencil-qualified runs имеют подтверждённые raw PASS.** Независимо проверен [HtmlCtaStencil-20261102.zip](Evidence/HtmlCtaStencil-20261102.zip): 1235895 bytes, SHA-256 `b3f06885ec051369509e01b07b87dd0e05ca691f6acbe8b3e38feafe1c35a9f4`. CRC clean; ровно39 declared payloads плюс `archive-members.json`; все39 размеров/SHA совпали. Extras, missing, duplicates и unsafe member paths — 0.

| Сохранённый run | XML leaves / native exit | Scope |
| --- | --- | --- |
| `play-cta-u63-r2` | Ровно **2 Failed**, exit2; skipped/inconclusive0 | Capture-enabled прежний target без depth/stencil; оба pixel failures сохранены. |
| `play-cta-u63-r3` | Ровно **2 Passed**, exit0; failed/skipped/inconclusive0 | Тот же desktop/mobile CTA контракт после fixture prerequisite correction. |
| `play-velora-full-u63-r1` | **16 Passed**, exit0; failed/skipped/inconclusive0 | Полный Velora fixture с той же изменённой allocation, включая два CTA cases; это не дополнительные16 независимых от r3 сценариев. |

XML root/leaf counts соответствуют summaries, native exits и логам Unity6000.3.14f1/D3D11; все объявленные launch/XML/log hashes совпали. Для всех **шести PNG** проверены decoding, размеры1280×900/390×844, byte count, SHA-256, capture test identity/result и точное совпадение hash/filename с output соответствующего XML testcase. Проверка здесь относится к целостности и provenance; семантическая оценка изображений выполняется root/native Demo reviewer. PNG — точные sampled-panel readbacks, не видео и не обычное GameView.

Все три manifests содержат332 paths. В каждом только `Assets/HtmlToUitkShowcase/Tests/PlayMode/VeloraShowcaseInteractionTests.cs` отличается от Initial; остальные **331 файла** разрешаются в shared sources архива `HtmlUnityNativeInitial-20261102.zip` с прежним SHA `b80cce9ca9dd8b42d3f378c42e0df9e130e68bb598161f4cd51b67bf1f2ae831`, и все их sizes/hashes повторно совпали. r3/full file maps идентичны. Manifest frozenUtc предшествует соответствующему launch; postrun records сообщают changedFrozenPaths=[] и protected12/differences=[]. Никакие production styles/resources/runtime files в этих scoped maps не менялись.

Независимо прочитан diff r2→r3: две fixture RenderTexture allocations запрашивают24 вместо0, добавлены depth/stencil positive prerequisites и diagnostics. Исходные pixel thresholds, contrast/glyph assertions, синтетический pointer/modal path и scheduling не ослаблены. Фактически на этой SDK выделен **depth32 / `D32_SFloat_S8_UInt`**, `clearDepthStencil=True`, что записано в XML output; requested24 не выдаётся за actual depth24. Текущий fixture hash `bdb01e74490b7442b326a1ab46f115a0559e22faf405e58981ed9df862196be6` совпал с r3/full snapshots при этом чтении. Это bounded source/receipt qualification; старые failures не переписаны в passes.

Private `editorprefs-before/after` snapshots исключены; присутствуют только restoration metadata без private values. Registry и процессы не запрашивались/не управлялись, Unity/compiler/tests не запускались. Мной добавлен только этот раздел отчёта; source, ZIP и другие ledgers не менялись. Квалификация допускает отчёт о конкретной исправленной fixture и scoped Unity6.3 Editor evidence, **не** release/main acceptance, global257 closure, browser parity, arbitrary web provider, physical input, Player/IL2CPP или all-platform readiness. Публикацией qualified work в `demo` распоряжается root; этот reviewer Git mutations не выполнял.
