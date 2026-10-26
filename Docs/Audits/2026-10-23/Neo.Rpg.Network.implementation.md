# Neo.Rpg.Network — implementation receipt

Дата: 2026-10-24. Исходный actor/session `neo-audit-1023-b05-06`; назначение `gpt-6.1-sol`, `max`.
Существующая ветка/цель: `demo` / `11.0.0`.

**Статус: source-final для назначенных runtime/docs/regression-source изменений.**
Зависимость от provider `Neo.Rpg` поставлена; отсутствующий dispatcher case добавлен.
Этим actor компиляция, weaving, тесты и product-checkers НЕ запускались. Централизованные source2
компиляции выявили CS0103, исправленный импортом; позднейший source3 full Play выполнил 18 owned cases:
7 passed / 11 failed. Ниже сохранены реальные failures и последующие fixture-only corrections.
Текущие source3 corrections **UNCOMPILED / UNEXECUTED**. Source-final не означает compile/runtime pass.

Полностью прочитаны `IMPLEMENTATION-CONTRACT.md`, собственный implementation-request с опубликованным
growth follow-up, `NativeReview-C-contracts.md` и финальный `Neo.Rpg.implementation.md`. Исторический
audit/plan сохранён; проверены актуальные adapter, fixture, public docs, provider API/guard и используемые
тестовые зависимости. Это продолжение реализации, не новый план или замена исходного actor.

## Завершённые approved actions

| Action / зависимость | Выполнено в собственной области |
| --- | --- |
| `RPG-INITIAL-SNAPSHOT-REENTRY` / C-32 | Прочитан готовый core publication/readiness guard. Добавлены две active-server MIRROR регрессии: первая инициализация и rebuild с реальным `RpgCharacterNetworkAdapter`, auto-load и проверкой опубликованного payload. Адаптерный recursion workaround не добавлялся. |
| `RPG-BASE-MAX-CONTRACT` | Добавлены wire/lifecycle регрессии и документация согласованного v2 effective-Max restore: HP130/160, процент0.8125, regen16/s, неизменённый BaseMax100 и последующий обычный refresh. Durable BaseMax/v3 не реализовывались. |
| Published `RPG-APPLIED-GROWTH` integration | В `Execute` добавлен `case RpgCharacterNetworkCommandType.ApplyLevelGrowth: _character.ApplyLevelGrowth(); break;`. Команда23 больше не заканчивается молча без выполнения. Написаны admission/budget и v2 growth-policy regression-source. |
| `NI18-01` (P3, evidence wording) | Уточнены только комментарии fixture и этот receipt: первая инициализация проверяет facade completion/data, прямой core rebuild — core-to-adapter publication, точное число core notifications проверяет отдельный transport-neutral fixture. Ветки и assertions сохранены. |
| Source2 `RpgResourceId` / CS0103 | В собственный MIRROR fixture добавлен `using Neo.Core.Resources;` для существующего canonical типа из `Neo.Core`. История uncompiled correction сохранена ниже; позднейший source3 уже выполнил owned fixture. Assertions и shared asmdefs не менялись. |
| Source3 `G04–G06` | Ordinary character/identity/adapter собираются inactive с template/flag до единственной активации; добавлен exact identity oracle. Оба command int поля используют зарегистрированный `Write<int>`. Все прежние assertions сохранены; новое исполнение pending. |

Уже согласованные F1–F4 сохранены: opt-out на ingress, актуальный флаг при client startup, общий
identity fallback для attack policy, разрешение required character до Awake. Эти patches не заменялись
и не выдаются за новую повторную реализацию.

## Точные изменённые файлы

1. `Assets/Neoxider/Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs` — только недостающий ростовой case
   дополнительно к сохранённым аудитным изменениям. Команда проходит прежние `CmdExecute`, `IsEnabled`,
   `RateLimitCheck(sender)` и `IsAuthorized(sender)`; отдельного обходного пути не добавлено.
2. `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs` — расширен собственный
   fixture; общий encoder отправляет прежний wire signature и все поля `RpgCharacterNetworkCommand`.
   Добавлены setup/helpers для временных данных, реального server spawn и наблюдения payload.
3. `Assets/Neoxider/Docs/Rpg/Network.md` — command23, порядок init/rebuild/auto-load publication,
   v2 effective Max и one-shot restore, режимы/ограничение applied-growth anchor, typed/UnityEvent workflow.
4. `Docs/Audits/2026-10-23/Neo.Rpg.Network.implementation.md` — данный итоговый receipt.

Новых package-файлов нет; существующий regression `.meta` сохранён. `RpgAttackNetworkAdapter.cs`
сохранил предыдущую согласованную правку. Shared helpers, legacy fixtures, asmdefs, package/settings,
samples, чужие reports, audit/plan и общие trackers не изменялись.

## Authored acceptance — source3 results ниже; corrections UNEXECUTED

Сохранены прежние девять случаев. Добавлены ещё девять: два init/rebuild, два growth ingress/budget,
пять growth-policy round-trip. Всего в исходнике 18 случаев при `MIRROR`; без символа остаётся один
локальный pre-Awake случай. Source3 XML подтвердил discovery/execution этих 18 сценариев; 11 failures
не позволяют объявлять следующие полные acceptance contracts пройденными.

### Реальный adapter после init + auto-load

`ActiveServer_AutoLoadPublishesCompleteSnapshotWithoutReplayingCurrentGrant(false/true)`:

- Запускает active server через существующий dummy-transport helper. Для первой инициализации
  inactive actor получает serialized authoring setup, затем public `NetDamage(0)` кеширует required
  character и инициирует реальный init/capture. Для rebuild actor уже server-spawned и адаптер разрешён
  до очистки runtime. Первая ветка затем также активируется/spawned с проверкой `isServer`/ненулевого netId.
- Читает именно woven `OnSerialize` реального adapter, без snapshot probe. Payload внутри `RuntimeRebuilt`
  и `OnProfileLoaded` должен оставаться прежним; новый completed payload появляется после обоих.
  Проверяется порядок initialization events `rebuilt → loaded`, завершённые данные и отсутствие replay.
- Первая ветка использует `NetDamage(0)`, чей wrapper дополнительно вызывает `NotifyStateChanged`
  после Execute. Поэтому она проверяет facade completion/data и отсутствие intermediate exposure,
  но не может независимо изолировать или посчитать финальные core notifications: готовый payload
  мог быть опубликован wrapper даже при пропущенном финальном уведомлении ядра. Уже spawned ветка
  вызывает core `RebuildRuntime` напрямую и чувствительна именно к core-to-adapter final publication.
  Отдельный transport-neutral core fixture
  `Initialization_AutoLoadPublishesOneCompleteSnapshotAfterRuntimeRebuilt` в
  `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgImplementationCharacterTests.cs` имеет exact-one
  notification oracle; этот счётчик не приписывается двум веткам Mirror fixture.
- Сохранённый профиль содержит HP130/160, Mana7/25, XP17, level3, points2, stats S42/Other11 и активный
  buff Max+50/Current+40. Проверяются итоговые значения, HP percent0.8125, regen16/s, BaseMax100 и отсутствие
  искусственной смерти. При server spawn готовый payload сохраняется.
- Woven payload дважды поступает получателю; public `OnStartClient` вызывает `ApplyNetworkState`.
  Повторная доставка не выдаёт Current+40 и не вызывает `OnBuffApplied`. Active buff действительно
  восстановлен, HP остаётся130/160. Между доставками обычный derived refresh даёт Max150 из Base100+buff50;
  следующая доставка снова восстанавливает160. Перед каждым replay заново подаётся исходный payload:
  receiver в этом bounded single-process fixture не является отдельным сетевым процессом.

Reflection ограничена authoring private serialized fields персонажа/buff/modifiers; private поля
адаптера и внутренние hit/command implementation methods не используются как assertions.
Fixture уничтожает свои actors/assets, удаляет только GUID-уникальные save keys, останавливает session
и восстанавливает прежний Transport. Provider не заменяется, `DeleteAll` не вызывается.

### Command23 и сохранённые ограничения v2

`ApplyLevelGrowthCommand_UsesExistingTrustedStateOptInAndRateLimit(false/true)` подаёт реальные encoded
аргументы через зарегистрированный Mirror delegate. Без legacy opt-in stat остаётся10; с opt-in/None
на level3 становится14 ровно с одним stat event. После server-side level4 немедленный повтор не
применяет16 и не добавляет event: прежний command budget/authority путь сохраняется.

`SnapshotV2_PreservesDocumentedGrowthAnchorPolicy` проверяет None, ManualUpgradePoints, deferred
AllStatsEveryLevel, automatic AllStatsEveryLevel и automatic Hybrid. Явно применённый server stat14
восстанавливается как10 при отсутствующем deferred/manual anchor и как14 в automatic режимах.
Level3 и schema2 сохраняются. Адаптер не пытается исправлять это скрытым ростом при приёме снимка.

Delegate invocation и receiver lifecycle replay НЕ являются доказательством remote admission,
пакетной доставки, ownership transfer или reconnect. Отдельный transport/peer harness остаётся
центральной последующей проверкой.

## API, совместимость и точные зависимости

- Поставленный API provider: `RpgCharacter.ApplyLevelGrowth()` и
  `RpgCharacterNetworkCommandType.ApplyLevelGrowth = 23` после `SetInvulnerable = 22`.
  Старые enum ordinals, serialized имена, RPC signature, schema2, `Net*` и transport interface сохранены.
  В этой сборке новых публичных методов или protocol/schema изменений не добавлено.
- Core readiness/publication guard, restore-only Max lookup и отсутствие one-shot grant при
  `RestoreActiveEffects` реализованы владельцем `Neo.Rpg`, не этим actor. Его final receipt и текущий
  исходник прочитаны; зависимость больше не pending. Полный native acceptance этой интеграции ещё не подтверждён.
- Snapshot Max авторитетен только на границе restore в пределах обычных limits; это не новый BaseMax.
  v2 не хранит applied-growth anchor. Saved/current level применяется автоматически только для
  AllStatsEveryLevel/Hybrid с `autoApplyGrowthOnLevelUp`; иначе restore начинает с anchor1.
  Различие deferred server/receiver growth прямо задокументировано, не замаскировано transport rewrite.
- Политика произвольных legacy client state commands остаётся явным opt-in, с теми же authority/rate
  limits и без acknowledgment. Peers должны поддерживать новую additive команду23.
- Для fixture уже есть ссылки `Neo.Core`, `Neo.Rpg`, `Neo.Rpg.Network`, `Mirror`, `Neo.Save`; shared asmdef changes
  не нужны. Parent README backlink на Network.md уже заявлен provider receipt; чужая документация не менялась.

## Фактически выполнено и долг после global writer freeze

Выполнены только чтение исходников/контрактов, reference enumeration, scoped `git status`/обычный
`git diff`, ручной просмотр новых участков и source/docs/receipt edits. При ручном review receiver
replay уточнён: исходный snapshot заново десериализуется после обычного refresh, чтобы server-side
fixture не принял собственный обновлённый cache получателя за повторно доставленный исходный payload.
Это не compile/test/checker pass. Старые static результаты аудита не перенесены на новый source snapshot.

В продолжении `NI18-01` полностью прочитаны обновлённый request и `NativeImplementationReview-18.md`;
исправлены только evidence comments/receipt. Runtime, обе ветки fixture, данные и assertions не менялись.

### Source2: фактический compile failure и последующее uncompiled исправление

Прочитаны существующие compiler diagnostics обоих централизованных source2 MIRROR-on запусков.
Корень артефактов:
`Local/Audits/assembly-2026-10-23/unity/final/f84c9007d0494cd8a421d84d70d931a0/`.

- `u6000_3_14f1/mirror-on-dev/Receipts/compiler-errors.json`
- `u6000_6_0f1/mirror-on-dev/Receipts/compiler-errors.json`

В каждом зафиксированы шесть owned CS0103: `RpgResourceId` не найден на прежних строках
208, 466, 510, 514, 534, 535 файла
`Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs`.
Это реальные предыдущие compile failures, не успешная компиляция и не результат нового запуска.

По текущему исходнику `Assets/Neoxider/Scripts/Core/Resources/RpgResourceId.cs` canonical тип —
`Neo.Core.Resources.RpgResourceId`, сборка — `Assets/Neoxider/Scripts/Core/Neo.Core.asmdef` (`Neo.Core`).
Read-only просмотр `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` подтвердил существующую ссылку
`Neo.Core`. Минимальная коррекция — единственный импорт `using Neo.Core.Resources;` в MIRROR-блоке
owned fixture. Magic strings, дублирующий тип или production wrapper не добавлялись.

В source2-продолжении изменены только импорт и данный receipt. Существующие initial Max120/9,
v2 profile/snapshot/late-join, callback/count assertions, обе ветки fixture и NI18 wording сохранены;
production/runtime, девять NI9 contracts, метаданные и shared config не редактировались.
Сразу после source2-коррекции snapshot был **UNCOMPILED**; этим actor Unity, compiler, tests и checkers
не запускались. Позднейшие централизованные source3 результаты приведены далее; старые compiler receipts
не изменены. Root gate снова закрыт до завершения всех writers и нового freeze.

### Source3: фактические native failures и bounded fixture-only correction

Полностью прочитаны обновлённые implementation contract/request и независимо reviewed
`Local/Audits/assembly-2026-10-23/full-play-failure-triage.md`, `critical-failure-triage.md`.
Прочитаны все 18 owned cases, их actual messages/stacks в
`Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml`
(SHA256 `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`):

- 8 NRE: AutoLoad(false) на receiver `OnStartClient:300`, AutoLoad(true) на `SpawnCharacter:525` из229;
  ClientStart(true,75) из146; все пять SnapshotV2 modes из366. Семь callback stacks проходят через
  `RpgCharacterNetworkAdapter.OnStartClient:170` → `NeoNetworkComponent.OnStartClient:97`.
- 3 command failures: growth opt-in false/true и disabled-character, `DispatchCommand:458`:
  `Expected: 0; But was: 6`. Они не доказывают прохождение последующих trust/rate/state assertions.
- 7 passed: inactive pre-Awake, ClientStart(false,100), обе delta-policy и три projectile-policy cases.
  Это результаты прежнего source3 snapshot, не нового исправленного исходника.

Фактический vendor `NetworkIdentity.cs:297–314,333–348` кеширует behaviours и назначает их `netIdentity`
в native Awake; `NetworkBehaviour.cs:52,121` читает этот кеш. Старый active factory добавлял adapter
слишком поздно. Теперь `CreateCharacter` добавляет identity/character/adapter inactive, задаёт публичный
`Template` и intended flag до `SetActive(true)`, затем проверяет `adapter.netIdentity Is.SameAs(identity)`.
`SpawnCharacter` проверяет ту же identity до spawn. Намеренно неактивная first-init source branch
остаётся отдельной и активируется лишь в прежней точке после facade initialization/publication.
Internal Awake повторно не вызывается; reflection assignment кеша и production fallback не добавлены.

Фактические `NetworkWriterExtensions.cs:31,46` различают fixed int и `[WeaverPriority] WriteVarInt`;
соответствующий reader — `NetworkReaderExtensions.cs:51`. `Writers.Register:34–58` выбирает priority,
`InitializeWriters:328–357` публикует тот же serializer в `Writer<int>.write`, а `NetworkWriter.Write<T>`
вызывает его. Вместо двух `WriteInt` fixture теперь использует `Write<int>` для Type/Integer.
Порядок и все пять payload fields, injected sender, registered receive delegate и `Remaining == 0`
сохранены вместе с disabled/trusted-toggle/rate controls. Production wire/schema не менялись.

Сверена reservation map: этот owned Play fixture не перечислен среди reserved files. Изменены только
его factory/encoder/pre-spawn oracle и данный receipt. Ни один Mono helper не переносился; shared
helpers/asmdefs и центральная интеграция `Neo.Tests.Fixtures` остаются владельцу b03-04. Runtime, docs,
metas и все substantive resource/stat/progression/buff/callback/intermediate/refresh assertions сохранены.
Scoped source comparison и ручное чтение выполнены; native rerun текущей коррекции **не выполнен**.
Новых подтверждённых runtime defects из этих setup failures не следует. Source writes завершены;
повторный compile/discovery и все 18 cases требуют нового root freeze после остальных writers.

Не запускались Unity/MCP, компиляторы, build/test runners, product validators/checkers, installs,
services, commits/push или child actors. Unsupported `[NonParallelizable]` не добавлялся.

После завершения всех writers координатору остаётся:

1. Скомпилировать frozen snapshot с MIRROR on/off, включая установленную Unity6.3 и6.6 и две версии
   compatibility branch 6.0–6.4/6.5+. Проверить реальное weaving и непустое discovery ожидаемых случаев.
2. Выполнить 18 source cases и связанные provider/legacy fixtures, отдельно наблюдая initial load,
   spawned rebuild, отсутствие half-state/recursion, HP130/160 и отсутствие Current replay.
3. Выполнить согласованную separate-peer проверку настоящей доставки/admission, authority, late join
   и reconnect; dummy fixture их не заменяет. Остальные неподдержанные версии/платформы указать явно.
4. Выполнить общие package/docs/encoding/meta/GUID/diff checks только после barrier; сохранить активный
   release/developer/imported sample layout. Новых local Unity-version `#if`/instance-ID вызовов нет.

Оставшихся назначенных source/docs действий или блокирующей additive API зависимости у этого actor нет.
Опциональные publisher/coalescing, envelope, durable resource bases/v3 и request acknowledgments
остаются предложениями исторического плана и не объявляются реализованными.
