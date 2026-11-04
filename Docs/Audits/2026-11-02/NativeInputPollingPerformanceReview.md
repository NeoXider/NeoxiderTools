# Native input polling — baseline evidence and pending correction review

Проверка 2026-11-02, 09:20 UTC. **Baseline evidence ACCEPTED в указанном ниже объёме. Исправление input GC ещё не рассмотрено и не принято.** Root выполнил capture/readback; этот проход независимо перепроверил сохранённые данные и текущие исходники без Unity, компилятора, тестов, Player, сборки или управления процессами. Единственная запись этого прохода — данный отчёт. Внешние driver/README, raw, receipt, конфигурация и production/test source не изменялись; private preference values не читались и не публиковались.

Автор ранее подготовил tiny batch driver. Здесь выполнена независимая от root summary проверка execution/evidence; это не отдельное независимое ревью авторства driver.

## Проверенная цепочка свидетельств

Входы:

- [Capture manifest](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/capture-manifest-u63-r1.json), SHA256 `bcda5967d3fa363cbe02e31bc73604a909d3fff5a8bba554163e18122d721500`.
- [Baseline summary](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/performance-baseline-u63-r1.json), SHA256 `a452afcfc7bea9895f87f18fe351be50c77bbfbc824376beea9136bb039aa690`.
- [Smoke XML](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-profile-u63-smoke-r1/results.xml), SHA256 `9c2e0cf5296a1b1560eff7008bfe504c2a3f02179c0ffa60c3686aaededf2088`: фактически **3** Passed leaf — Camera3D N=1/100/1000, exit 0. Prefix-фильтр не был одним N=1 тестом.
- [Remaining XML](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-profile-u63-remaining-r1/results.xml), SHA256 `95b04c6ec3319fc7d9abb432b83eb0ca1ab7a50b2fc8a0f02b8ea9f304f39184`: 9 Passed leaf — Camera2D и KeyboardLook2D/3D N=1/100/1000, exit 0. Совместно ровно 12 уникальных требуемых leaf, без skip. Каждый leaf output сопоставлен с единственной raw/counter парой manifest.

Для **каждого из 12** readback проверены фактический SHA256/размер raw, SHA256 counter CSV, все output/coverage/calibration SHA256, EXPORTED/exit 0, один `profileLoaded`, Unity `6000.3.14f1`, совпадение process id с launch receipt и дословное равенство JSON receipt строке из native `editor.log`. Raw expected/before/after SHA совпадают с текущими bytes. Native bounds каждого capture — `0..252`; все выбранные frames находятся внутри них.

Каждый CSV содержит 240 различных native frames: positive и stable-miss по 120, ordinal `0..119`, последовательные frames внутри фазы, ровно N `AutomaticUpdate` invocations в каждой строке. Сопоставление `(phase,ordinal)` с counter CSV даёт единственный постоянный offset `Time.frameCount − profilerFrame` на capture. В каждой строке counters: native=1, cache=N−1, classificationPasses=N, classifiedHits=N² для positive и 0 для stable-miss, callbacks/LOS/fallback=0, frameAdvances=1. Формулы и квантили p50/p95/p99/max пересчитаны независимо; все совпали с baseline summary. Для p95/p99 использован nearest rank, для p50 — median.

Калибровка каждого capture содержит `knownPayloadBytes=4096`, `actualMetadataAllocatedBytes=4128`. Reader source проверяет реальную GC.Alloc metadata внутри калибровочного marker; нулевая/отсутствующая metadata не превращается в доказанный zero-GC. Raw повторно через Unity не загружался, новый parser не создавался: проверены raw identity и сохранённые результаты неизменённого native reader, а его scope/count/calibration алгоритм сопоставлен с source.

Все 12 frozen source manifest содержат по 5939 файлов; каждый зафиксированный файл сопоставлен с текущими bytes/SHA256, различий нет. Защищённые 12 путей совпали с `pre-main-merge.json`. Все 12 публичных prefs-comparison receipt показывают 51→51, без AddedNames/ChangedOriginalNames. Это сверка зафиксированных путей, не заявление о полном отсутствии иных untracked файлов.

[Queue R2](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/readback-queue-u63-r2.json) хранит COMPLETE, 9 новых + 3 reused exports. [Queue R1](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/readback-queue-u63-r1.json) сохранён как FAILED: его supervisor source вызывает `WaitForExit/Refresh` и сравнивает `ExitCode`, а ошибка содержит пустое значение exit code. При этом Camera2D N=1 child launcher прямо записал `Unity profiler readback exit 0`, native receipt — EXPORTED, launch — exit 0. Это подтверждает supervisor-level расхождение; root объяснил его null после Refresh. R1 не переписан и не принят как native failure. Конкретные exports приняты по их полным собственным свидетельствам.

## Измеренные границы GC и CPU

GC — bytes **на выбранный frame**, суммарно по всем N production `AutomaticUpdate` subtrees. В колонке AutoGC указано точное значение **каждой** из 240 строк; это сильнее одного max. All-thread диапазон объединяет обе фазы одного capture.

| Route | N | AutoGC bytes/frame | All-thread GC min..max | Positive Auto CPU p50, ms | Stable-miss Auto CPU p50, ms |
|---|---:|---:|---:|---:|---:|
| Camera2D | 1 | 0 | 1384..1384 | 0.016700 | 0.014300 |
| Camera2D | 100 | 0 | 1384..5118 | 8.708650 | 0.319900 |
| Camera2D | 1000 | 0 | 1796..10420 | 970.085148 | 3.605650 |
| Camera3D | 1 | 0 | 1384..1384 | 0.019300 | 0.014650 |
| Camera3D | 100 | 0 | 1384..5118 | 8.738700 | 0.269500 |
| Camera3D | 1000 | 0 | 1796..10420 | 955.598798 | 3.085700 |
| Keyboard2D | 1 | 3384 | 23758..27956 | 0.043200 | 0.041050 |
| Keyboard2D | 100 | 338400 | 358822..363088 | 11.617500 | 2.980750 |
| Keyboard2D | 1000 | 3384000 | 3405250..3413890 | 954.784398 | 27.804550 |
| Keyboard3D | 1 | 3384 | 23806..24754 | 0.040350 | 0.036150 |
| Keyboard3D | 100 | 338400 | 358822..363108 | 11.304700 | 2.675200 |
| Keyboard3D | 1000 | 3384000 | 3404454..3417072 | 1029.907647 | 26.758550 |

Reader складывает top-level `AutomaticUpdate` samples всех N, исключая повторный учёт вложенного subtree; количество invocations проверяется отдельно. Classification/native/callback inclusive CPU вложен в Auto CPU и не прибавляется к нему. All-thread GC включает fixture/input driver, Editor и прочие profiler scopes; в каждой строке он не меньше AutoGC. Нельзя приписывать его целиком production input adapter или получать типизированную причинную разбивку простым вычитанием camera baseline.

Keyboard workload действительно чередует native press/release edges и выполняет query на каждом выбранном frame. Driver создаёт реальные виртуальные Input System devices, через public queue передаёт их настоящему PlayerLoop и восстанавливает заимствованные настройки/current devices. Он принудительно выбирает optional fallback в своём изолированном fixture. Это не проверка OS-клавиатуры и не отдельный NewOnly consumer profile. Его `onAfterUpdate` diagnostics и reflection queue выделяют память **вне** `AutomaticUpdate`; это одна из причин сохранять отдельный all-thread показатель.

Все external names пусты; каждая CSV/coverage строка для external/EventSystem/union имеет **NOT_MEASURED**. EventSystem отсутствует как измеренная стоимость, surrogate не использован. Измерение относится к Unity 6.3 Editor D3D11 с включённым profiler. Capture-time power/load не контролировались. Player/FPS/platform-wide zero-GC и относительное улучшение здесь не доказаны. N² raw visits и большой positive classification CPU — отдельный measured workload для SAMECLI02; данный отчёт не принимает его будущую оптимизацию.

## Текущий input source и локализация причины

Сверенные baseline hashes:

| Source | SHA256 |
|---|---|
| `OptionalInputSystemAdapter.cs` | `d2498b1e6635fe8e3560d7a15b65d4ab6d8b81a202bfcc1f21ce7b8d96f90d0c` |
| `KeyInputCompat.cs` | `44bfa760fabc48f8cbef61b842003e40d74f527c5a90639b5344674ff03726e3` |
| `InputBackendCompat.cs` | `7239385234ae3ee189d3339e4cd6f10701ae6a657dbe7bcc0c70c08e51b09eb8` |
| Reader / capture / batch driver | `ca85a496e03cb644c2440ae945818c38cee726911bdeb78e26a9e87c2dafa4ee` / `a747d304c1757d354133d4c304aacef1e027709ed9a8aae06a13364d1e4ccc7a` / `746d315f9ef9e5a6f5eade27ef23bdb42793e773c62ba379a958cbe8f82829e2` |

Imported и внешний driver совпали; README остался SHA256 `1a292c13f136685a5a5c42d151bc3e63b8f5ab72bb04e339d7a74936df6241a7`. API assembly в receipt и текущая installed `UnityEditor.CoreModule.dll` совпали: `30019c1fad50b70962ead76b716ba30b78b4d98e874203d2e434dbe7d0385b11`.

`InteractiveObject.UpdateKeyboardInput` всегда читает down и up перед проверкой edges. В fallback каждый `KeyInputCompat` read вызывает `TryReadKeyState`: live current keyboard, key-name mapping, live control property, live bool state property. Это три metadata lookup на read и шесть на target pass. `GetCachedProperty` до проверки dictionary hit создаёт строковый composite key из `AssemblyQualifiedName`, property и flags. Letter/digit mapping также конструирует строку при каждом вызове. Bool `PropertyInfo.GetValue` возвращает object с value-type boxing path. Эти конкретные источниковые механизмы объясняют, почему metadata cache сам по себе не гарантирует allocation-free polling после warm-up.

**Граница причинного вывода:** baseline доказывает 3384×N байт внутри production Auto scope и устойчивую связь с keyboard workload. Он не распределяет эти 3384 байта между qualified-name/key construction, mapping, reflection/boxing или другими operations. Для такого распределения нужен focused synchronous caller-thread oracle и/или дополнительные actual profiler scopes. Утверждение «все 3384 байта — только boxing» либо точная экономия конкретной строки из этих totals не принимается.

Текущий adapter хранит metadata (`Type`/`PropertyInfo`/`MethodInfo`), не конкретные devices/controls. Возможная замена cache key/typed getter должна сохранить live reads, смену current device/control, отсутствие устройства, reset и observable getter faults. Нельзя кэшировать edge value по `Time.frameCount`: Input System способен обновиться повторно в одном Unity frame. Сохранить legacy-first/shared bounded fallback; не добавлять hard Input System dependency, новые asmdef/defines, dynamic-code generation или сильные ссылки, удерживающие снятые devices. Эти пункты являются критериями review будущего diff, не выводом о уже готовом candidate.

## Минимальный последующий gate

Переиспользовать существующие контракты, а missing causal/lifetime checks добавлять в уже существующий test слой:

1. Edit fixtures `Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests` и `Neo.Tests.Edit.Audit20261023NeoToolsInputBackendTests`: mapping Return/KeypadEnter/unsupported, различие unsupported и valid-false, допустимые bool state names, touch identity/phase/missing controls, shared backend failure count и reset. `ConfiguredNewOnlyReads_DoNotRequireALegacySelectionException` принимается только в реальном NewOnly profile; skip в Both не закрывает его. Эти существующие tests не измеряют warm polling allocation.
2. Focused candidate causal tests: до изменения production исполнить применимые red-first tests на старом API; warm-up и setup/assertions вне allocated-bytes interval; внутри — повторные meaningful true/false/down/up/live-change reads. Отдельно проверить replacement/current disappearance/reset, getter faults и отсутствие strong dead-device retention. Fake controls годятся для изоляции metadata/boxing, но не для реального Input System device или AOT доказательства. Конкретные фильтры/ожидаемые leaf будут определены после source-final candidate.
3. Native consumer regression: существующие шесть `PlayerLoop_KeyboardLook2D/3D_1/100/1000` и keyboard-tap `AimChangeRevalidatesUp`, `RangeChangeRevalidatesUp`, `DisableReenableCancelsUp`, `InputFlagChangeSuppressesUp` из partial `Neo.Tests.Play.Audit20261023NeoToolsInteractableObjectLifecycleTests`. При изменении общего bool getter сохранить и реальные mouse down/up lifecycle controls. Touch fake coverage нужно сохранить, поскольку общий metadata helper обслуживает также vector/touch paths. `InputPlayModeTests.MultiKeyEventTrigger_Creation_DoesNotThrow` и warmed MouseEvent subscriber-snapshot test не являются causal polling GC oracle.
4. Actual before/after keyboard raw: тот же неизменённый workload/capture/reader, оба dimension routes, те же phase/N/stamps/calibration/counts и корректные native edges. Сначала узкий N=1 smoke, затем необходимая N matrix. Сравнивать AutoGC отдельно от all-thread GC, сохранять camera controls и external NOT_MEASURED. Изменённая нагрузка/выключенный backend/подмена input state не принимаются как улучшение. Player/AOT/stripping, optional-package absent и настоящий NewOnly host остаются самостоятельными проверками.

**Ожидание source-final:** SAMECLI01 input candidate ещё не читался. Source acceptance, red/green и measured improvement остаются OPEN. Root единолично выполняет интеграцию/Unity и передаёт exact finalized source/hash для следующего bounded review; существующий baseline полностью сохраняется.

## Source-final review SAMECLI01 — 09:48 UTC

Предыдущий пункт ожидания закрыт только в части **полного source review**. Прочитаны весь `REVIEW.md`, `base-inputs.json`, полный adapter 517 строк, patch и полные test files 267/151 строк из `candidates/lib-1102-01-inputgc`. **Вердикт: SOURCE ADMISSIBLE для root test-only import и red-first исполнения на прежнем production adapter. Подтверждённых source blockers не найдено.** Product adoption, compile/green и measured improvement остаются OPEN; Unity/компилятор/тесты в этом проходе не запускались.

Независимо проверенные identities:

| Candidate artifact | SHA256 |
|---|---|
| Product `OptionalInputSystemAdapter.cs` | `29ed698b7d58af735201f68d77ee679e3403cde1430a56e00c59e6905922ec24` |
| Edit `Audit20261102NeoToolsInputAllocationTests.cs` | `030761624be036f40cc5b8cced9f2529b0baa3afe6d77b864b5f68a7d6e37d59` |
| Play `Audit20261102NeoToolsInputAllocationPlayTests.cs` | `66184b8e62170eb9466e14e9766c2458c9181ab78398b77411d4bd029b32e302` |
| `base-inputs.json` | `ec416c3073190173f019d736add7e783ffd5e716ca90e7ecb36286949925ba75` |
| `REVIEW.md` | `7a63cf15d90483072d51d88ddd2a6c83382b4c82f98987061a14cdc8ca46c185` |
| `adapter.patch` | `3260f6f01e1d39a22974e32b019f4f9943dcf5960404387fd9e435e08a1151fc` |

Все 13 sourceInputs совпали одновременно с current project, copied base и manifest SHA. Текущий adapter при этой проверке оставался `d2498b1e...`, оба новых project test path отсутствовали. В памяти применены все patch hunks с проверкой исходного context: результат в точности равен candidate, +76/−6 строк. Все три candidate файла — UTF-8, без `var`. Сверены 42 конкретные path/hash ссылки request/review/evidence, включая шесть keyboard raw/counter/CSV/receipt наборов; их фазовые/GC значения сохраняют ранее принятый baseline. Две provenance-only записи protectedMeasurementSources честно не выдают полный path/hash за собственную проверку автора; ранее этот QA отдельно проверил reader и driver полными SHA.

### Почему source допускается к проверке

Изменение сосредоточено в одном adapter. ValueTuple `(Type,string,int)` убирает повторное создание qualified-name composite key, сохраняя existing metadata lookup и cached-null entries. Статические таблицы сохраняют 26 letter + 10 digit mappings; 36 special mappings, включая различие Return/KeypadEnter, остались прежними. KeyInputCompat, InputBackendCompat, asmdefs, serialized/public API и optional type resolution не меняются.

Bool cache хранит `ConditionalWeakTable<control, dictionary<stateName, Func<bool>>>`; closed getter ссылается только на соответствующий control. По [контракту BCL ConditionalWeakTable](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.conditionalweaktable-2?view=netstandard-2.1#remarks) value→key ссылка сама по себе не удерживает ключ после исчезновения внешних ссылок. В candidate нет отдельной static strong коллекции device/control. На каждом read заново берутся live current device и live control property; getter вызывается каждый раз, input value/frame/edge не кэшируется. Reset заменяет weak table и очищает прежние metadata caches. Реальная корректность lifetime на Unity Mono всё равно требует своего native результата.

Используется [существующий BCL overload Delegate.CreateDelegate(Type,object,MethodInfo,bool)](https://learn.microsoft.com/en-us/dotnet/api/system.delegate.createdelegate?view=netframework-4.8.1), закрытый на заранее известный `Func<bool>`. В проекте уже есть closed delegates в LightAnimator и ConditionalWeakTable в Spawner/UI; новый provider/framework или vendored code не вводится. Нет Expression.Compile, Reflection.Emit или runtime generic specialization. Rejected binding кэшируется как null и вызывает прежний `PropertyInfo.GetValue`; cold binding и такой fallback могут выделять память. Это не AOT/stripping acceptance.

Разрешённые три state names и проверка bool property остались на прежнем месте. Delegate invocation faults оборачиваются в `TargetInvocationException`, сохраняя исходный exception в InnerException. Reflection control/current getters остаются прежними, поэтому их faults не превращаются в false. Indexed/unbindable property возвращается к reflection failure. В source не найдено изменения live-state, device replacement, fault или backend selection контракта; это вывод code review, не результат исполнения.

### Admissibility тестов и следующий root gate

Edit fixture имеет **10 leaf**: положительная калибровка caller-thread counter, три точных allocation окна, live replacement/disappearance/reset, два cold/warm bool-fault случая, invalid-state/control-getter fault, indexed fallback и weak lifetime. PollChangingState проходит все восемь held/down/up комбинаций многократно в одном Unity frame; constant-false shortcut не удовлетворит correct-count oracle. Missing metadata проверяет availability и false output. Mapping проверяет все 72 поддержанные и два unsupported key. В измерительных окнах нет setup, NUnit assertions, test reflection или native update; primitive toggles и численный учёт результатов входят в измеряемый synchronous call path. [BCL counter](https://learn.microsoft.com/dotnet/api/system.gc.getallocatedbytesforcurrentthread) считает managed allocations текущего thread, а не retained/native memory; retained 4096-byte payload control обязателен.

Play fixture имеет **5 leaf**. Три раздельных PlayerLoop-arranged phase leaf проверяют raw native и public output для released/no-edges, held/down и released/up **до** exact-zero allocation assertion. Это позволяет увидеть actual positive allocation red даже если другой phase leaf уже провалился. PollPublic сверяет direct adapter availability/held/down/up mask и тот же mask KeyInputCompat; queue/yield/assertions находятся вне synchronous window. Отдельные leaf проверяют реальную замену current keyboard/reset и два manual InputSystem.Update в одном Unity frame. Последний является отдельным manual-update control; его нельзя выдавать за PlayerLoop scenario. Он проверяет held/down после press и held/up после release, но не полный native/public mask после каждого manual update; полные маски покрыты тремя обычными phase leaf и восьмикомбинационным Edit control.

Для red-first root импортирует **только оба test file** при неизменном `d2498b1e...`. Калибровка и state/fault/lifetime controls должны быть разобраны как самостоятельные результаты. Три `WarmKeyMapping_*`, `WarmBoolPolling_*`, `WarmMissingMetadata_*` Edit leaf и три `WarmPublicPolling_*` Play leaf являются кандидатами causal red: требуется дойти до правильного state/count oracle, затем получить failure именно exact-zero allocation assertion. Premise/setup/state failure, skip, ноль выполненных leaf или неподдерживаемый counter такой red не заменяют.

`WarmReaders_DoNotKeepReleasedDeviceOrControlAliveWithoutReset` использует NoInlining helper и две GC.Collect, но требует немедленной смерти weak references. Conservative Mono/JIT stack lifetime может повлиять на такой oracle. **Не ослаблять его заранее:** сначала сохранить результат на старом production. Failure уже на старом adapter является отдельным fixture/runtime concern; при old PASS/new FAIL это конкретный lifetime regression нового cache. Нужен разбор actual failure, а не автоматическая атрибуция новому product или его автоматическое оправдание.

После принимаемого old allocation red root может применить exact reviewed adapter и потребовать полный green **10 Edit + 5 Play** плюс существующие affected adapter/backend/native consumer controls. Финальное улучшение требует прежних actual keyboard raw/capture/readback с теми же N/phases/edge workloads/калибровкой и отдельным AutoGC scope. Reflection fallback, cold replacement, vector/touch allocation, actual NewOnly/absent-package, Player/AOT/stripping и версия Unity за пределами проверенного host остаются явно ограниченными или pending. Подготовленный пустой UPM host не является input acceptance.

В этом follow-up дописан только данный отчёт. Product/test candidate и все исходные measurement artifacts оставлены без изменений; новых результатов исполнения или zero-GC утверждений нет.

## Actual old-product calibration failure — 09:51 UTC

Root выполнил [input-polling-u63-edit-red-r1](D:/Temp/Neoxider-LibraryClosure-20261102/runs/input-polling-u63-edit-red-r1/results.xml): **10 executed = 9 Passed / 1 Failed, exit 2**. XML независимо прочитан; SHA256 `aace81d7460ced1028f466df6fe0d45097ca19406dc936ba4966d4a99cfacf72`. Единственная failure — `AllocationCounter_ObservesRetainedCallerThreadPayload`, строка 33: при retained `new byte[4096]` counter сообщил **0**, ожидалось ≥4096.

Следовательно, counter в этом run не пригоден для заявленного allocation oracle. PASS трёх `WarmKeyMapping_*` / `WarmBoolPolling_*` / `WarmMissingMetadata_*` подтверждает достижение их state/count assertions, но **не подтверждает отсутствие allocations**. Это не требуемый causal allocation red и не product/performance green. Причина нулевого counter по этому наблюдению не установлена; предположение о конкретном GC/runtime механизме не добавляется.

Old-product weak-reference lifetime control **Passed**; немедленная сборка в этом конкретном запуске не помешала oracle. Остальные functional/fault/replacement/mapping controls также сохранили свои фактические результаты. Необходимость сравнить lifetime после изменения cache остаётся.

Текущий production adapter независимо сохранил `d2498b1e...`; KeyInputCompat/InputBackendCompat hashes остались прежними. Prefs comparison независимо подтверждает 51→51 без добавлений/изменений; root сообщил неизменность frozen/protected paths. Adapter candidate не применён. Положительно откалиброванные 12 native raw baseline остаются действительными: они использовали native GC.Alloc metadata, а не failed caller-thread counter.

Root возвращает SAMECLI01 только исправление measurement fixtures с доказанной native calibration, сохраняя exact-zero budget, input workload и immutable первый run. Новый test-only source-final, его admission и фактический old allocation red остаются OPEN. В этом дополнении нет code/process изменений или новых запусков.

## Source-final measurement revision 2 — admission review

Полностью рассмотрены revised Edit/Play source, identical private NativeAllocationCounter helpers, `REVIEW.md`, `NATIVE-QA.json` и metadata. **SOURCE ADMISSIBLE для exact test-only overwrite v1 и узкого root native calibration.** Product `29ed698b...` остаётся неприменённым; native backend ещё **NotAdmitted / NotRun**, byte fallback и old allocation red не исполнены. Новый unrelated classifier compile failure с 0 tests, сообщённый root, не является input fixture/product результатом; root сначала устраняет его в соответствующей области.

Независимо проверено:

| Revision 2 artifact | SHA256 |
|---|---|
| Edit fixture, 19855 bytes / 428 lines | `5fbde0d98b11b7401ac007e6ad810c02c38cdbe69e956bf528f90982b43e70e5` |
| Play fixture, 14360 bytes / 308 lines | `de0541a50865ea464c73a3df80b0452634f2b74790d5e4a40a1013f4250db774` |
| `base-inputs.json` | `5c3fa857cd95009c41d35669888b8489512f39700909c0cba95587b9f4e172e6` |
| `REVIEW.md` | `764e1dc683feb4be21c44f199147bcbe9760bd170d0b80346d92d382405d818f` |
| `NATIVE-QA.json` | `8cc60cb6b95d03794852b53cec2701425a371467b76e124665d2d7cee716c0f0` |

На момент source check current project test bases оставались exact v1 `030761624...` / `66184b8e...`; production adapter — `d2498b1e...`, external product candidate — прежний `29ed698b...`. Все sourceInputs совпали с current; четыре revision-v1 artifacts сохранены с прежними SHA. Оба private helper body идентичны. Nonmeasurement controls и polling/mapping helper bodies сравнены с v1 дословно и сохранены; names/attributes по-прежнему дают **10 Edit + 5 Play**. Файлы UTF-8, без `var`.

### Primary API и смысл измерения

Установленный [MethodMeasurement.cs](D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework.performance@aa81a99c4a75/Runtime/Measurements/MethodMeasurement.cs:217) пакета **3.4.0** создаёт GC.Alloc ProfilerRecorder с теми же CollectOnlyOnCurrentThread / SumAllSamplesInFrame / WrapAroundWhenCapacityReached; Start/Stop и EndGCRecorderAndMeasure читают `GetSample(0).Count`, затем сбрасывают свой recorder. [Документация пакета](D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework.performance@aa81a99c4a75/Documentation~/measure-method.md:22) прямо определяет allocation **calls**. Это API-pattern reuse без нового package/asmdef reference; чужой measurement framework не копируется.

Primary hashes независимо совпали: MethodMeasurement `1ff5d103a5af1e6766f41f3fb5a4d0d233ff5320775fe6774700d3ed5e909fde`, measure-method.md `ea0f178ff2b70fdd3b592828f4428876d2ae0986d6aeddd9faa5f53bb428d654`, package.json `4c43f1f8102333d1413042004e705545e2242ef9f8287f83ca521360e267a8cc`, Unity Companion LICENSE `202adffac8b9d21d3b148a294d241894f3bcef85724c01b6a0bd52269a3798c8`. Installed UnityEngine.xml `e41decb51fc467307e0f69dd4d8a84183b58a220db844b6c7530e1090f742b93` подтверждает filtering по initialization thread, sum внутри frame, instance Reset (stop/clear), Start/Stop и Dispose unmanaged instance. Наличие API/pattern не заменяет admission на данном Unity host.

Новый helper использует свой ProfilerRecorder capacity 1, очищает только этот instance перед каждым синхронным окном и после Stop суммирует native sample **Count**. Он не использует failed managed counter, LastValue предыдущего frame, heap delta, borrowed Recorder.Get или global profiler/history reset. Setup/native input update/assertions находятся вне polling окна; owner thread проверяется при Begin/End/Dispose. Constructor failure/Ignore проходит cleanup, `using` закрывает recorder и открытые markers при failure polling. Valid=false — explicit NotSupported skip, не PASS; неверные calibration counts на valid recorder — failure, не допуск к zero assertion.

До каждого production polling window выполняются discarded boundary warm-up и admission: три empty windows должны дать 0, остановленный recorder исключает отдельный retained4096 outside payload, два in-window retained4096 payload должны дать ровно 1 каждый, а второй positive повторяется в том же Unity frame/thread. Intermediate empty после positive защищает от stale value. Payload length остаётся 4096 и GC.KeepAlive сохраняет его до завершения измерения; threshold не уменьшен. Положительная калибровка и correct output oracle обязательны прежде интерпретации allocation assertion. После реального допуска ожидаемые old результаты — Edit 7P/3 allocation-F и Play 2P/3 allocation-F; это ожидаемые формы, **не новые результаты**.

### Обязательный отдельный raw byte gate

`NATIVE-QA.json` содержит осмысленные проверяемые predicates: две `Retained4096` descendant scope внутри `NativeWindow`, по одному GC.Alloc с metadata bytes ≥4096; один `Excluded4096` вне NativeWindow; три EmptyWindow с zero allocation у своего parent; один PollingWindow для каждого отдельно отфильтрованного measured leaf. Нужны actual caller-thread/sample ancestry, положительный old allocation в этом polling subtree и после fix ровно 0 descendant calls/metadata bytes. Missing marker/frame не является zero, all-thread GC и inclusive ancestors не подменяют scoped bytes. Один count нельзя объявлять 4096 байтами.

**Ограничение готовности fallback:** это source-final план с точными predicates, а не уже выполненный capture/readback и не дополнительный готовый generic raw exporter. При failed constructor admission существующий test завершится **до PollingWindow**; простой повтор того же filtered test не создаст fallback polling evidence. Root должен реально выполнить предусмотренный bounded owned raw driver/QA adjustment, сохранив state controls, scope ownership и byte gates. Existing fixed AutomaticUpdate reader/capture/driver остаются immutable и сами по себе не экспортируют новые InputPolling scopes. До этого byte gate OPEN; он не может быть закрыт одним PASS allocation-call counter.

Следующий разрешённый шаг — root test-only import revised copies после проверки exact v1 bases, затем **один calibration leaf** на старом adapter, с собственным raw scope byte validation. Только admitted measurement позволяет переходить к old red и позднее product green; при failure/skip остановить adoption и сохранить actual evidence. First failed managed-counter run и прежние 12 calibrated raw baseline сохраняются. В этом review дописан только данный отчёт, без code/compile/test/process изменений.

## Actual first raw byte readback — native load failure, 11:20 UTC

Root импортировал exact reader `1b4f221872ed02914bc1986199ff539600270fb492a9ef3a933ab4e932ddcd00`; он скомпилирован и вызван в [input-raw-u63-calibration-readback-r1](D:/Temp/Neoxider-LibraryClosure-20261102/runs/input-raw-u63-calibration-readback-r1/launch.json). Launch/log/output JSON независимо прочитаны: PID 34380 завершился штатным exit **2**, `ProfilerDriver.LoadProfile` вернул false, native log содержит `Deserializer encountered error` для `calibration.raw`.

[Failed byte receipt](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/input-native-gc-r2/calibration-readback-r1.json) фиксирует совпавший expected/before raw SHA256 `4a20d0e3c7cc9025931dcb8ccb4392675769e46c8c5c28bbc4b7b6873fa3ea20`, размер **1267156280** bytes, 0 completion events, bounds −1/−1, 0 valid thread views и пустой scopes array. Ошибка возникла **до Scan**. Нулевые default totals не являются GC результатом; native byte admission и проверка нового scope parser остаются OPEN. Prefs comparison независимо сохранил 51→51 без изменений. Исходные receipt/log/raw сохраняются; этот reviewer raw не загружал и не перечитывал.

Сам по себе допустимый header либо большой размер не доказывает корректность потока или конкретный лимит Unity loader; причина native deserialization rejection здесь не установлена. Это failed capture/load path, а не allocation zero, input functional failure или разрешение ослабить metadata/parent/count проверки. Отдельные результаты native allocation-call calibration/red не подменяют отсутствующие bytes.

Root меняет **стратегию записи** с CLI whole-Editor-startup stream на bounded owned API window по ранее доказанному capture pattern. SAMECLI01 готовит внешний thin wrapper вокруг существующих v2 fixture methods, с setup/warm-up вне записи и обязательными flush/restore. Его source admission и новые native capture/readback ещё требуются. Blind retry того же raw и изменение strict reader predicates не запланированы. В этом дополнении изменён только отчёт; code/process/profile-load действий не было.

## Actual old native-call red and guarded product application gate

**Вердикт: exact `29ed698b7d58af735201f68d77ee679e3403cde1430a56e00c59e6905922ec24` ADMISSIBLE к guarded root application для green/iteration. Конкретных source/execution blockers этого шага не найдено.** Это соответствует явно обновлённому root QA-плану: положительная native-call калибровка и meaningful old allocation red допускают применение исправления для проверки. Final byte-zero/performance adoption остаётся OPEN. Ранее записанное требование получить byte artifact до product application этим уточняется только для guarded iteration; final byte gate не отменён и бюджет не ослаблен.

Самостоятельно прочитаны три actual XML, launch/prefs receipts и frozen manifests:

| Run | Actual outcome | XML SHA256 |
|---|---|---|
| `interaction-core-input-calibration-u63-r1` | 31P/0F/0skip, exit0: **Core30 + один input calibration**, а не 31 input test | `543bad6424b32735284d52a041aacefc84c060b8b1908b56f945eaafe58beb82` |
| [input-native-u63-edit-red-r2](D:/Temp/Neoxider-LibraryClosure-20261102/runs/input-native-u63-edit-red-r2/results.xml) | 10 executed = **7P/3F/0skip**, exit2 | `b8053021ba3a373f4c8d11fdf7dcdac0e75bfb3ed85a8db38d782a8feeb2dd03` |
| [input-native-u63-play-red-r2](D:/Temp/Neoxider-LibraryClosure-20261102/runs/input-native-u63-play-red-r2/results.xml) | 5 executed = **2P/3F/0skip**, exit2 | `1172a00d008ea1c3331953fbb06778add4649b1a96bb5521b5802c91caa0296e` |

Все Edit failures находятся именно на `Exactly zero native GC.Alloc calls` assertion: `WarmKeyMapping_AllSupportedNamesAndUnsupportedKeysAllocateNothing` — **294912 calls**, строка 51; `WarmBoolPolling_LiveHeldDownUpPositiveAndNegativeAllocateNothing` — **86016 calls**, строка 66; `WarmMissingMetadata_RejectsWithoutAllocatingOrInventingState` — **12288 calls**, строка 81. Перед этими строками в exact v2 source стоят correct-count/mapping/live-state assertions; до них конструктор counter проходит две retained4096 положительные и empty/excluded/stale-window native admission проверки. Actual failure message/stack не относятся к premise, calibration, unsupported capability или состоянию. Отдельный calibration leaf Passed с ожидаемым admission output. Cold/warm fault envelopes, control replacement/disappearance/reset, indexed reflection fallback и weak lifetime controls также Passed.

Все три Play failures — `WarmPublicPolling_ReleasedWithoutEdgesAllocatesNothing`, `WarmPublicPolling_PressedWithDownEdgeAllocatesNothing`, `WarmPublicPolling_ReleasedWithUpEdgeAllocatesNothing`: **по 294923 native GC.Alloc calls**, точная budget assertion на строке 55. В закреплённом source raw native mask, unsupported/invalid outputs, public warm-up mask и measured correct count проверяются раньше этой строки; native counter admission также завершён. Следовательно, actual red достигнут после реальных положительных/отрицательных/edge oracles. Real current keyboard replacement/reset и два InputSystem.Update в одном Unity frame Passed. Manual-update control сохраняет свою ранее указанную ограниченную область и не выдаётся за PlayerLoop phase.

Во всех трёх runs frozen manifest содержит **5945** файлов; каждый зафиксированный файл независимо сопоставлен с current bytes/SHA, различий на момент проверки нет. В частности, old subject — `d2498b1e...`, Edit fixture — `5fbde0d...`, Play — `de0541a5...`; current и copied product base совпали с old SHA. External product candidate повторно подтвердил exact `29ed698b...` и сохраняет ранее полностью рассмотренный diff. Protected 12 путей совпали с pre-main-merge; все три public prefs comparisons — 51→51 без added/changed names. Эта проверка не выполняла Unity/компилятор/tests/process controls и не писала product/test source.

Это **native allocation-event red** по корректно откалиброванному synchronous current-thread backend, в отличие от failed managed-counter v1. Counts не объявляются bytes и не распределяются по типам аллокаций или отдельным строкам adapter. Независимая ошибка загрузки CLI startup raw не отменяет выполненные inline native-call calibration/state/control результаты. Она сохраняет свой FAILED byte-artifact статус.

Следующий root шаг разрешён в существующем scope: применить только exact source-reviewed adapter к совпавшему old base, проверить compile и revised Edit10/Play5 green, сохранить state/fault/lifetime/backend controls и native admission. Это применение для проверки, без публикации или final closure. Bounded raw capture/readback должен отдельно доказать прежние ≥4096 calibration/exclusion/empty predicates и focused polling byte zero; production before/after benchmark, real NewOnly/absent-package, Player/AOT/stripping и остальные заявленные пределы остаются самостоятельными gates. До их исполнения нет окончательного byte-zero/global-zero-GC/FPS/performance adoption утверждения.

## Actual green: 10 Edit + 5 Play — native allocation-call regression accepted

**Узкий native-call regression gate ACCEPTED:** root применил exact `29ed698b...` по предыдущему guarded iteration disposition; самостоятельно проверены полные завершённые Edit и Play XML, identity transitions, source freezes и публичные receipts. Получено **15/15 Passed, 0 skip**, с теми же v2 fixtures и controls. Это green для шести warmed scoped **native GC.Alloc call** окон и сохранённых behavior/lifetime/fault контрактов в данном Unity 6.3 Editor host. Byte-zero/raw runtime benchmark, SDK/Player/AOT и final performance adoption этим не закрываются.

| Completed green run | Actual | XML SHA256 |
|---|---|---|
| [input-native-u63-edit-green-r2](D:/Temp/Neoxider-LibraryClosure-20261102/runs/input-native-u63-edit-green-r2/results.xml) | 10P/0F/0skip, exit0 | `678d221404cd4c349035c11919bef14a7b6bba8c6fee15dc548e39a9536189c9` |
| [input-native-u63-play-green-r2](D:/Temp/Neoxider-LibraryClosure-20261102/runs/input-native-u63-play-green-r2/results.xml) | 5P/0F/0skip, exit0 | `0bd112d83bd7be9e077e83e9fd40a24f5466d22339ec5ec2f8a80d64eb230ee8` |

Каждый fullname в green совпал с соответствующим old red leaf; новых/пропавших/переименованных leaf нет. Old 7 Edit + 2 Play behavior/control PASS сохранились. Все шесть прежних failures перешли в PASS на прежней exact-zero assertion, без изменения ожидаемых результатов или allowance:

| Warmed scoped window | Old native calls / result | New native calls / result |
|---|---:|---:|
| Edit key mapping | 294912 / allocation FAIL | 0 / PASS |
| Edit live held/down/up | 86016 / allocation FAIL | 0 / PASS |
| Edit missing metadata | 12288 / allocation FAIL | 0 / PASS |
| Play released/no edges | 294923 / allocation FAIL | 0 / PASS |
| Play held/down | 294923 / allocation FAIL | 0 / PASS |
| Play released/up | 294923 / allocation FAIL | 0 / PASS |

Значение 0 в green установлено успешным прохождением сохранённой `allocationCount Is.Zero` assertion в exact source; XML не содержит отдельного числового dump каждого window. Pass не получен через skip или ранний successful return. Перед каждой такой assertion constructor native counter прошёл собственные two-positive4096 + empty/excluded/stale/frame/thread controls; отдельный Edit calibration leaf также Passed с admission output. Правильный measured result count/mapping или held/down/up mask проверяется до budget assertion. Прежняя invalid managed-counter реализация не используется.

Edit по-прежнему проходит все восемь меняющихся held/down/up комбинаций и availability/unsupported/missing-control oracles. New-cache weak lifetime проверен **без reset до GC/weak-reference assertions** и Passed на данном host. Cold/warm getter fault envelope, observable control getter fault, indexed reflection fallback, replacement/disappearance/reset сохранились. Это probe/control lifetime proof, не самостоятельный all-device/Player lifetime результат.

Play использует те же реальные виртуальные Input System devices и native PlayerLoop-arranged released/down/up states. Все direct adapter + KeyInputCompat masks и availability assertions пройдены; `InputBackendCompat.UsesLegacyInput == false` в measured phase также пройден. Фикстура не переключена на legacy для сокрытия optional adapter работы. Real current keyboard replacement/reset и repeated manual updates в одном Unity frame Passed; scope manual-update control остаётся тем, который описан выше. Вместе с source без cached input value/frame эти результаты поддерживают live reads, сохраняя ранее отмеченные пределы полного native mask coverage.

Source/provenance:

- [Project application receipt](D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-01-inputgc/project-application-receipt.json), SHA256 `2ee0707afc4e3f321488c51701d2319976e5e261e1443473c7cb4fa60d23a3b9`, фиксирует только `d2498b1e...`→`29ed698b...`, iteration-only disposition и byte gate OPEN.
- Во **всех 5947** frozen files каждого green run current bytes/SHA совпали. При сравнении общих old-red/green путей единственное изменение — `OptionalInputSystemAdapter.cs`. Два новых frozen paths — ранее отдельно рассмотренный InputPolling raw readback helper и его meta; measured fixtures/workload не изменены.
- Fixtures остались exact `5fbde0d...` / `de0541a5...`; current product совпал с external candidate `29ed698b...`. KeyInputCompat `44bfa760...`, InputBackendCompat `72393852...` и Input asmdef `97237be5...` unchanged; скрытого backend/config/dependency изменения в этих identity deltas нет.
- Все protected 12 путей независимо совпали; оба public prefs comparisons — 51→51 без добавлений/изменений. Private preference values не читались. Внешний reflection fallback сохранён в source и может выделять память на другом bind/host; этот green не превращает fallback/AOT в allocation-free обещание.

Failed CLI startup byte artifact остаётся сохранённым FAILED до Scan. Новый bounded capture wrapper и strict native byte readback ещё требуют своих source/native результатов. Активный на момент данного review affected-input run `input-existing-u63-edit-r1` не читался и не включён в green coverage; он и остальные affected/runtime/profile gates остаются отдельными. В этом review изменён только данный отчёт; code, compile, tests и процессы исполнял root.

## Actual affected regression slice after Input29ed

**Affected slice ACCEPTED в фактически выполненном scope:** завершённые Edit17 и Play190 самостоятельно проверены по полным raw XML, logs, launch/summary/index, source manifest и public prefs comparison. Всего **207 executed = 206 Passed / 1 explicit Skip**, оба native exit0. Ни один skipped case не преобразован в PASS; это не полная SDK/profile/performance приёмка.

| Run | Actual result | XML SHA256 |
|---|---|---|
| [input-existing-u63-edit-r1](D:/Temp/Neoxider-LibraryClosure-20261102/runs/input-existing-u63-edit-r1/results.xml) | Adapter14P + Backend2P/1Skip = **16P/1Skip** | `2edd75d405aa9fcb86c521fb3dd58512dac42730d34366004dcf8a82be273fe1` |
| [input-interaction-u63-affected-play-r1](D:/Temp/Neoxider-LibraryClosure-20261102/runs/input-interaction-u63-affected-play-r1/results.xml) | **190P/0F/0skip** | `54c6ca6d36921291e676176b44cb0ebad0bf40c2a4c80b1be90a4c07682d63b0` |

Единственный skip — exact fullname `Neo.Tests.Edit.Audit20261023NeoToolsInputBackendTests.ConfiguredNewOnlyReads_DoNotRequireALegacySelectionException`, reason: нужен Input System Package (New) validation host. Текущий Both profile не закрывает NewOnly. Все 17 canonical fullnames сохранены относительно соответствующего подмножества `edit-main-u63-baseline-r1`; никаких omitted/new/duplicate input leaf нет. Остальной full baseline имел собственные несвязанные failures и здесь не объявляется целиком прошедшим.

Play case set **в точности** равен union следующих прежних identity sets, без пересечения и duplicates:

- 129 из accepted `interaction-index-u63-functional-r2`: Interaction19, Lifecycle39, PhysicsEvents12, SharedQuery48, GlobalPolicy3, NativePhase8.
- 61 соответствующий input leaf из `play-main-u63-baseline-r1`: behavior58 (включая Dispatch partial), UI2 и InputPlay1. Статус всего исторического Play baseline здесь не расширяется.

В current XML нет **ни одного `Capture_*` leaf**; launch использовал явную retained selection, а не весь partial Lifecycle fixture. Все **12** `PlayerLoop_Camera2D/3D_1/100/1000` и `PlayerLoop_KeyboardLook2D/3D_1/100/1000` сохранены и Passed. Это functional PlayerLoop/input/shared-query scope; прохождение этих cases не является новым длительным CPU/GC benchmark. Новый завершённый combined12 after-capture также не используется как CPU/GC evidence: его native readback остаётся pending.

Reference XML hashes для воспроизводимого identity comparison: `interaction-index-u63-functional-r2` — `9152a7407b05e35b3ddcbbdc6287a582ba081a623cae443f9c591a7642476330`; Play baseline — `47f6101d3df8fa5917d458c353b0dedc225aa6a35c008ff0e52faa4ad4a38c5f`; Edit baseline — `cbf65583c78966ca65564dc26d97095f5fc60e2ad90e6f7adba766fe0b6672ef`. Оба current testcase-index в точности совпали с XML по `(canonical fullname,result)`; summary counts и все записанные hashes launch/XML/log также независимо совпали.

Полные current logs (588/649 строк) завершены `Test run completed. Exiting with code 0 (Ok)`. Их SHA256 — `16dcc440724ba4ce7bb1c6a0d4275e2851ab3b905d87844518acdb87c1019f8c` и `37356f44a95e7c880d25e86cd016437637021d4b7f77b0bcae3feb578a2cf852`. В них нет C# compilation failure, unhandled exception или test assertion failure; присутствует startup licensing access-token error с последующим entitlement resolution. Поэтому не заявляется, что log вообще не содержит строки Error.

UI cursor/coordinate case действительно Passed с rendered depth0, culled=false, available pointer и viewport640×480. Его diagnostic показывает **legacy=True** в этом Both host; это корректный current UI functional результат, а не доказательство optional-backend/NewOnly cursor маршрута. NoEventSystem buffer-clearing case также Passed. Native virtual-device interaction и отдельные new allocation Play fixtures сохраняют свои ранее описанные optional-backend controls.

Все **5947** frozen files каждого current affected run совпали с current bytes/SHA. Product — exact `29ed698b...`, measurement fixtures — прежние `5fbde0d...` / `de0541a5...`; source/config/test изменений между freeze и проверкой не найдено. Protected 12 путей совпали независимо. Оба public prefs receipts подтверждают 51→51, без added/changed names, registry modification и публикации private values; сами private values не читались.

Это закрывает перечисленный affected regression slice после применения Input29ed. Failed startup byte artifact, bounded calibration/polling byte gate, production benchmark readback, actual NewOnly/absent-package и version/Player/AOT gates сохраняют свои отдельные pending/failed qualifications. В этой приёмке дописан только данный отчёт; новых Unity/compile/test/process действий не было.

## Actual combined12 benchmark — calibrated AutomaticUpdate byte evidence

**Combined benchmark evidence ACCEPTED в данном workload/host scope.** Independently проверены все 12 завершённых native readback, оба phase каждого capture и immutable baseline12. Это уже фактические scoped CPU/GC результаты из raw, а не только capture-test PASS. Во всех **2880 combined выбранных frames** AutomaticUpdate GC bytes = **0**; каждый frame содержит ровно N invocations. Для keyboard baseline соответствующее значение каждой строки было 3384×N bytes. Этот результат относится к полному production AutomaticUpdate subtree в данном warmed setup; focused InputPolling 12/14-marker byte gate остаётся отдельным OPEN.

Основные identities:

| Evidence | SHA256 |
|---|---|
| [Combined capture manifest](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/capture-manifest-combined-u63-r1.json) | `1429fdebf1fc2b6ca08c3c0c463425a177502b31c938b93c524b8fef4d403a44` |
| [Combined native summary](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/performance-combined-u63-r1.json) | `606577d995dd34e6b41e9aed9e887e974246c1b877bbe15fa6c2ba1f404e4a49` |
| [Comparison/source-vector receipt](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/performance-comparison-combined-u63-r1.json) | `1639277e5fc7cfa184d0403589483c845f1c587331407af7727392d54644dad1` |
| [Completed readback queue R2](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/readback-queue-combined-u63-r2.json) | `f56a0acda2d723105206fe573e8b1d105a45ddeabff81991e2ba2a86c1a7205b` |
| [Combined capture XML](D:/Temp/Neoxider-LibraryClosure-20261102/runs/interaction-combined-u63-captures-r1/results.xml) | `29466a5a93bf7e030636334fe45daaff5c849283df58274b2fe738992c3cf1ac` |

Baseline summary сохранил `a452afcfc7bea9895f87f18fe351be50c77bbfbc824376beea9136bb039aa690`, baseline manifest — `bcda5967d3fa363cbe02e31bc73604a909d3fff5a8bba554163e18122d721500`. Повторно SHA256-прочитаны **все 24 raw** и counter CSV — исходные baseline12 и combined12; bytes/hash совпали с manifest и native expected/before/after receipts. Никакой raw не загружался этим reviewer через Unity.

Capture XML имеет ровно 12 Passed/no-skip leaves; каждый leaf output связан с единственной соответствующей raw/counter парой. Queue R2 содержит COMPLETE, 12 уникальных completed jobs `profile-readback-combined2-u63-*`, без reused/текущего/error. Каждый из этих 12 native jobs имеет launch exit0, один load completion, exact Unity6000.3.14f1/reader ca85 и совпадающий process id; единственная JSON receipt строка полного native log равна файлу receipt. Все CSV/calibration/external-coverage hashes проверены.

Для каждого capture: 240 уникальных native frames, 120 sequential frames/phase, ordinals0..119, N AutomaticUpdate samples/frame, постоянный counter-Time.frameCount/native-frame offset; actual bounds покрывают все выбранные frames. Calibration payload4096→**4128 metadata bytes** во всех 12. Native1/cacheN−1/classificationPassesN/frameAdvances1 и callbacks/LOS/fallback0 соблюдены во **всех** counter rows. Positive dense geometry имеет H=N: old raw visits **N²**, current — **H+N=2N**; stable miss — 0. При N=1000 это 1000000→2000 visits. Это описание конкретного eligible indexed workload; не универсальная сложность любых blockers/callback epochs/fallback rules.

Все p50/p95/p99/max независимо пересчитаны из old и combined CSV и совпали с summary. Root comparison receipt затем независимо проверен по обоим полным source vectors, всем 24 before/after phase dictionaries и derived median ratios/percent changes.

### Observed CPU p50 — обе фазы

Единица — ms за frame, сумма top-level AutomaticUpdate **всех N**. Вложенные classify/native/callback inclusive costs не добавляются к Auto как независимые величины.

| Route | N | Positive baseline→combined | Stable miss baseline→combined |
|---|---:|---:|---:|
| Camera2D | 1 | 0.016700→0.017600 | 0.014300→0.012350 |
| Camera2D | 100 | 8.708650→0.481250 | 0.319900→0.307150 |
| Camera2D | 1000 | 970.085148→6.222050 | 3.605650→3.355150 |
| Camera3D | 1 | 0.019300→0.018400 | 0.014650→0.014100 |
| Camera3D | 100 | 8.738700→0.389300 | 0.269500→0.248750 |
| Camera3D | 1000 | 955.598798→4.568250 | 3.085700→2.662850 |
| Keyboard2D | 1 | 0.043200→0.018750 | 0.041050→0.012800 |
| Keyboard2D | 100 | 11.617500→0.526350 | 2.980750→0.382750 |
| Keyboard2D | 1000 | 954.784398→6.180250 | 27.804550→3.718700 |
| Keyboard3D | 1 | 0.040350→0.019400 | 0.036150→0.013650 |
| Keyboard3D | 100 | 11.304700→0.464350 | 2.675200→0.310300 |
| Keyboard3D | 1000 | 1029.907647→5.519150 | 26.758550→3.281700 |

Положительный dense N1000 CPU существенно ниже в этих записях; не все CPU величины улучшились. Camera2D N1 positive p50 вырос **5.39%**, p95 0.0253→0.0289 ms. Все 12 combined miss medians ниже baseline, но отдельные tails выше: Camera2D N1 miss p95 0.0193→0.0220, p99 0.0237→0.0358, max0.0279→0.0413 ms; Camera2D N100 miss p95 0.4280→0.4624; Camera3D N1 miss p99 0.0303→0.0330. Threshold не изобретается; это наблюдённые различия, сохранённые вместе с улучшениями.

Ранее classifier-only Camera3D N1000 stable-miss результат **сохраняется**: baseline3.085700→interim4.573150 ms, **+48.2046%**. Independently прочитан [interim CSV](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/readback-index-u63-r1/camera3d-n1000.csv), SHA `b6c9b5c8fef22a6ac00a6bf41f3b5f48c567564dafff2ada6b212e0866788576`; receipt SHA `256433a3a7a6ba371643168bc665a9cb3f81eb9c3a886ccdfebd4bbcf59abdf8`, raw SHA `055d8a6491c0461b27187290d54239ee45a0527f4a2727db08afcfb331c00ab2` перепроверен. Позднейший combined miss2.662850 ms не стирает interim observation и не доказывает, что reversal вызван Input fix: ambient load/power не контролировались.

### GC scope и реальные source cohorts

Camera baseline AutoGC уже был 0. Keyboard positive **и** stable-miss каждой строки изменились 3384/338400/3384000→0 bytes для N1/100/1000. Все combined phases имеют AutoGC p50/p95/p99/max0 при положительной native byte calibration. **All-thread GC не ноль:** observed combined camera frames 1384..5118 bytes, keyboard frames 10378..14708 bytes. Это отдельная aggregate область с fixture/input diagnostics/Editor и прочими потоками; её нельзя объявить library allocations, вычесть как точный типизированный subtotal или объединить со scoped Auto как независимый cost.

Before/after — реальная combined source cohort с двумя runtime corrections, а не один изменённый файл. Из capture freezes самостоятельно получены и затем сверены с root receipt:

- Remaining9 baseline5937→combined5947: **6 modified + 10 added, 0 removed**. Modified: OptionalInputSystemAdapter, InteractionPhysicsQueries, InteractiveObject, PlayerLoopTests, MeasurementWindows и UI/Editor.md.
- Camera3D smoke3 baseline5937→combined5947: **7 modified + те же 10 added**, 0 removed. Дополнительный путь — `Scripts/UI/Editor/UIMeshRigGraphicEditor.cs`, old `2bfd5f4e...`→`26ba64e9...`; этот отдельный UI correction уже присутствовал к remaining9 baseline. Поэтому нельзя писать, что все12 имеют одинаковый шестифайловый delta.
- Added10 — **пять C# файлов и их meta**: Edit/Play InputAllocation fixtures, existing InteractionProfileBatchReadback, InputPollingRawReadback и InteractionIndexedProjectionTests. Это не десять новых runtime modules/dependencies. Полные bytes/SHA каждой modified/added пары находятся в проверенном comparison receipt.

Current runtime hashes: Input `29ed698b...`, Queries `ff5caa6b...`, InteractiveObject `574d2996...`; old — `d2498b1e...` / `91aa73bf...` / `bcb49d73...`. Source capture a747→be181 изменил только post-window ClassifiedHits assertion N²→2N и explanatory comment: обратная замена независимо восстановила exact исходный a747 text. Geometry, input driver, null observers, warm8, 120+120 loops и stamps не изменены. Его workload не подменён ради CPU/GC результата. Reader ca85 и load driver746d остаются прежними.

Все 12 readback freezes содержат по **5947** файлов и совпали с current bytes/SHA, protected12 и prefs51→51 без added/changed names; capture run также exit0/prefs51 unchanged. Snapshot-cohort различия above намеренно сохраняются, а не объявляются неизменностью старого baseline относительно current product.

Это Unity6000.3.14f1 Editor D3D11 с включённым profiler, warmed collinear eligible workload, без контролируемой capture-time нагрузки/питания. CPU model наблюдался позднее и не превращает запуск в controlled hardware trial. Нет Player/FPS/universal speedup/прочих Unity SDK обещания. External/EventSystem и union во всех24 phase coverage — **NOT_MEASURED**, без surrogate.

**Разделение gates:** calibrated combined AutomaticUpdate bytes/CPU evidence принято; отдельный focused `Neo.InputPolling.v2` calibration-only12 / single-leaf14 scope gate не исполнен и этим не закрыт. Failed CLI startup Input raw остаётся FAILED before Scan. Bounded wrapper/native byte acceptance, Player/AOT/stripping, actual NewOnly/absent-package и final cross-profile adoption сохраняются отдельными задачами. В этом review дописан только отчёт; Unity/profile load/compiler/tests/process/source/config действий не было.

## Independent actual bounded calibration + six NEW focused byte gates — 2026-11-02

**ACCEPTED in the measured Unity6000.3.14f1 Editor scope:** the bounded calibration raw is natively valid and all six separately captured current29ed polling windows contain **0 GC.Alloc calls and0 native allocation-metadata bytes**. This supersedes earlier statements that the *new* focused12/14 gate had not executed. **Historical-old focused positive BYTE red remains OPEN**; preserved old native-CALL failures/whole AutomaticUpdate evidence are separate witnesses and are not substituted for that old byte gate. No whole-library/Player/AOT/physical-input/global-zero or blanket closure follows.

This reviewer independently read completed raw XML/index/launch/log/public comparisons, wrapper/native JSON, frozen sources and scope topology. No Unity/compiler/test/process/native raw load was performed. Raw bytes were read only for SHA/length, never interpreted by a new binary parser. The only write in this review is this appendix to NativeInputPollingPerformanceReview.md. Cards' earlier accepted calibration message is context, not a fabricated final Cards report.

### Actual execution, identities and raw preservation

[Completed six-case queue](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/input-bounded-new-u63-queue-r1.json) records12:45:46.911–12:57:34.653 UTC, COMPLETE/Error=null; six unique cases/raws, each actual capture exit0/one wrapper leaf Passed/0skip and a separate native readback exit0. The standalone calibration capture (PID28864) and readback (PID37732) completed first. Actual XML belongs to the wrapper, while source/capture receipt identifies the original method; no original-test XML is synthesized.

Every capture raw length/SHA was recomputed and matches queue (where applicable), closed-raw wrapper receipt and native expected/before/after hashes. Capture summary launch/XML/log SHA/byte counts match actual files. Each native JSON receipt exactly equals its NEO_INPUT_RAW_READBACK log record, with actual process identity, profileLoadedEvents1, valid native bounds/views and RAW_SCOPES_VALIDATED/status0/failure empty. All seven wrappers report originalInvoked/completed=true, ORIGINAL_METHOD_RETURNED, no caught original exception, restored=true and closed raw. Actual first calibration compile log has Tundra success; all selected wrapper runs execute and exit naturally with no C# compile/deserialization failure. Finish(System.Exception) in a successful Debug.Log stack is a method signature, not a thrown exception.

| Case | Capture / readback PID | Native named frame / Main thread ID | Raw bytes | Recomputed raw SHA256 |
|---|---|---|---:|---|
| calibration |28864 /37732|2 /33296|947482|b994cd7129a0a6cc17b3a5f4703f2bd5667633ed308edaa5fa3017b09b01fa6c|
| mapping |39880 /15088|2 /39460|952450|84b29d42e4e715578b462b0301275ffd838013f2e80541ab3e8576a7b61cdd35|
| bool |37044 /16344|2 /37992|948956|5d4e1bd5994211923562a4ece67bef7ba1c21c80a445ab62cab9c62d9c6c7ecd|
| missing |38548 /34080|2 /34292|945525|76f29e556b0fbb4658e39a62947659d43123f27f27bea38ffd1abaa116ce91ca|
| released |5052 /38600|4 /33012|1237812|b9ec18e1a85cda24d6e4e532bc61d05ba986f317f9e60a6d53fc54d1a60fc18b|
| down |34360 /35224|5 /34596|1301342|4ce21055d169ae081f8ed9debf34bf0eaea783c4042e71b260e88f6f5ed04227|
| up |35596 /36284|6 /36132|1300082|dc58117136f4251b79ec56c13fd66234c9c1adb3d2426cf2e2da2e7c5b1fb556|

Each case is under profiles/input-bounded-new-u63-<case>-r1 with capture run of the same name and readback run suffixed-readback; calibration is profiles/input-bounded-native-u63-calibration-r1, native readback run input-bounded-native-u63-calibration-readback-r1. Recomputed native receipt SHAs are: calibration1f4b2f8b4430d405f34c9127435d9ae5a53e3a7757d0232ab761d1c4840272c4; mappingf363cd7c0da5e19293330d73d83f3389cb382cb9a46ce9135d868696125d8884; boolac78a0c54529263dfcf418a2ce56683c111ff7b2de52e468036b67b33ded41a8; missinge92ffc5cde05bfa1c69e092e6487325bf9bb2c60c1af30c35d7dcc52b31ee78a; released6e4a95aeb14216602ecfa58e153f5c8ac453262b870a1bef4f1bcdfd9f90e89b; down341a6ea1088bb1a33458bb132ec1ff2915117cc9b168f444b62cc3f3c5f1f7e7; up606b1b9e2f0c14bbe34c3b23d4eb5ef23a317aaf48c773859e6e007112bbfd2d.

The valid bounded raws do not overwrite or rehabilitate the failed1.267GB CLI-startup raw. Its earlier LoadProfile=false/before-Scan receipt remains failed; missing views/events/scopes0 were never zero-byte proof. The present evidence uses actual successful API recordings/loads.

### Native calibration, ancestry and no double count

The compiled unchanged helper1b4 enforces the pinned v2 protocol8cc; this reviewer independently rechecked every exported scope record rather than relying solely on RAW_SCOPES_VALIDATED. Calibration has exactly12 names in order; every focused leaf has exactly14 =the same12 plus NativeWindow12/PollingWindow13. All scopes within one raw have its stated native frame, Main Thread/index0, nonzero persistent ID and matching group. Native bounds are0..4 for Edit/calibration,0..6/7/8 for Play released/down/up; all have positive valid-view counts. Two header frames + actual forwarded original yields exactly locate named frames2/4/5/6, followed by two real flush frames before the final native frame.

Each of all seven raws has:

- Exactly two Retained4096 children, ids7/11 directly below NativeWindow6/10, each **one GC.Alloc event/4128 bytes**. This is real payload4096 plus observed runtime header/alignment, not assumed equality to4096.
- Exactly one Excluded4096, id3, one event/4128 bytes outside every NativeWindow/named ancestor. Top-level native subtree ranges are disjoint.
- Three EmptyWindow children (ids2/5/9) and their direct parents (1/4/8) with **calls0/bytes0**, preserving empty/exclusion/stale-window controls. Native raw parent sample/name and end ranges agree with the proper NativeWindow, not just a caller label.
- Named-scope **union3calls/12384bytes**, independently equal to the sum of disjoint top-level scopes. Retained parent/child inclusive4128 are not added twice. Discarded warmup is recorded with its separate qualification and is not silently promoted to a zero budget.

Standalone calibration has no polling scope: pollingCalls/Bytes=−1 and NOT_APPLICABLE_CALIBRATION_ONLY. Its12 actual scopes in frame2/ID33296 therefore admit byte-channel calibration, not an invented polling zero.

All six current-new raws have a real PollingWindow13 directly below NativeWindow12 with exact parent inclusive equality and **0calls/0bytes on both**. The scopes are present (native sample pairs parent/child: mapping487/488, bool436/437, missing352/353, released1131/1132, down999/1000, up1078/1079); missing data is not being read as zero. Strict native size metadata was required for every counted GC.Alloc, and the nonzero calibration/excluded events in the same caller frame prove the channel remained visible. No threshold, scope-count relaxation, ignored capability or guessed merged full-fixture capture is used.

### Six nonvacuous original oracles and source chain

Capture receipts' selected case/originalDeclaredTest exactly match the six supported original methods and each native callerDeclaredTest. All declare expectation/mode new; only calibration declares calibration. Wrapper source084be5b5 Editor /cb666256 Play, original fixtures5fbde0d9/de0541a5, subject29ed698b, parser1b4f2218, protocol8cc60cb6 and ca85/746d remain exact. Frozen copies/current bytes and native source identity lists agree; imported wrappers were actually discovered/compiled/executed in their unchanged respective test assemblies, without widening product visibility.

Normal return from these exact frozen original bodies plus true wrapper PASS completes their real assertions, not a surrogate marker count:

- Mapping checks all declared supported key-name mappings plus unsupported None/Joystick cases over4096 iterations.
- Bool polling changes held/down/up positive and negative bits within one Unity frame and requires all4096 direct results to agree.
- Missing metadata requires unavailable/false outcomes for missing control/state, not fabricated state, in every iteration.
- Play released/down/up independently admit native masks0/3/4, compare direct adapter and KeyInputCompat outputs for all4096 iterations and complete the post-budget optional-backend assertion. Actual original yields are forwarded2/3/4, with no replay or replacement input driver.

The original private recorder/calibration/polling workload, output checks and exception boundaries are unchanged. An Assert.Ignore/capability/setup/state/budget/cleanup failure cannot be counted as a new success under the reviewed wrapper; these seven receipts show actual original return without exceptions and restored/closed recording. Play uses the owned Input System test driver; it is not physical OS-input, genuine NewOnly configuration, absent-package, Player/AOT or other-SDK coverage.

All14 capture/readback source manifests have the exact same **5951-file map**. This reviewer independently rehashed all5951 current inputs (86,764,950bytes), found0differences, and matched the pertinent actual frozen source copies/pins. Each capture summary has changedFrozenPaths=[]/protectedDifferences=[]; all fourteen frozen protected-difference lists are empty. All14 public preference comparisons retain monitored51→51, no added/changed originals or registry modification; no private values were opened. The helper's source/current manifest pins are not mistaken for cryptographic binary authenticity alone: actual matching wrapper XML, compile/load logs, owned lifecycle receipts and native controls complete this reviewed chain.

**Disposition:** current-new focused scoped bytes are valid and accepted for all six named warmed workloads. The historical-old focused *positive byte* campaign is not executed/admitted here; historical old native-call RED and previously accepted whole AutomaticUpdate before/after data remain preserved separate evidence. Main was not swapped/restored by this reviewer. No CPU/FPS/allocation claim is made outside the actual named caller scopes; all-thread/startup/fixture costs are excluded, not declared zero. No blanket input/library closure or Player/SDK6.6 guarantee is produced. Recorded 2026-11-02T13:26:09.033Z; only this report was appended.
