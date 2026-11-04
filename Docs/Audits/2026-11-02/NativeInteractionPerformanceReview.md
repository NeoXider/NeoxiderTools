# InteractiveObject shared queries: независимое performance review

Дата: 2026-11-02. База задания `demo` / `3e795e8c`, readonly phase. Прочитаны новый COMMON/02 assignment, runtime query/provider/component/Core, SharedQueries.md, существующие Play query/lifecycle/input fixtures и historical P6 scope. Никаких project production/test/assets/config edits, Git, Unity/compiler/test/process controls не было. Root исполняет baseline; prepared probes существуют только вне Assets.

**Source verdict:** обязательный shared complete-query backend реализован; подтверждённого **измеренного performance regression** в этом проходе нет. Есть незакрытый CPU/GC/aggregate-physics evidence gate и отдельный условный 2D cache-policy риск, требующий native A→B probe. N×H projection — явно описанная работа, не доказательство N одинаковых raycasts и не основание переписать систему в новый controller. Performance correction допускается только после конкретного measured failure/profile; ownership runtime остаётся у root/CLI02.

## 1. Что реально делает текущий код

`InteractionPhysicsQueries.Get/Execute` разделяет complete raw hits точного физического контекста: camera/viewport/targetTexture/display, kind, default physics world, exact origin/direction/distance, layer/trigger/contact policy. Shared mode переиспользует K contexts в стабильном epoch; Fresh/default external calls начинают новый epoch. Saturation16 вызывает полный native fallback и retained growth со spare slot, не truncation. Все фактические initial/fallback calls считаются рядом с Physics call.

`InteractiveObject.Update` сохраняет target-local eligibility/input/range/hover; `BuildInteractionHits3D/2D` проходит raw H hits, фильтрует hierarchy/trigger и заполняет retained per-target array. Затем Core `TrySelectTarget` отдельно сканирует candidate array **вне Classify marker**, но внутри AutomaticUpdate. Для N=1000/H=1000 это 1,000,000 counted raw projection visits и потенциально ещё 1,000,000 Core candidate visits плюс hierarchy/key/input work. `ClassifiedHits` не является полным счётчиком всех CPU операций.

Одна camera query не отменяет разные LOS rays/intervals, contexts Both/разные камеры/маски, cold fallbacks или callback-created epochs. Before/after callback invalidation и Fresh queries должны остаться; переход hover-exit может законно выполнить N queries. Уменьшать эти события/маски/полноту/нормальный порядок ради «одного raycast» нельзя.

| Узкий audited contract | Current source / существующие meaningful tests | Вывод и следующее evidence |
|---|---|---|
| `INTERACTION-SHARED-QUERY` | `Get`, `Execute`, `TryGetCurrentTargetHit`; 12 PlayerLoop cases и SharedQuery dense/parity | Реализовано в source; fresh counts, CPU/GC и external aggregate ещё требуются |
| `S3-INTERACTION-STABLE-MISS-MEASUREMENT` | `AutomaticQueries` сначала проверяет реальный miss/exits, затем следующий stable frame | Source oracle корректно различает transition vs stable; не удалять реальные callback epochs |
| `S3-INTERACTION-NATIVE-INPUT-POSITIVES` | Existing InputDriver QueueKeyboard/Mouse + onAfterUpdate, без ручного InputSystem.Update | Нужны actual native input/device/focus results; отсутствие optional backend — explicit skip, не acceptance всех12 |
| `S3-INTERACTION-EDIT-PHYSICS-ADMISSION` | SharedQuery fixture находится в Play; direct array/List/filter comparisons | Synchronous SendMessage/query slices остаются unit controls, не automatic PlayerLoop proof |
| Lifetime/hover/disabled-camera compatibility | Update generation guards, SetHoverState, TryCreateRay; lifecycle reset, hover mutation, disabled camera tests | Regression boundaries для любого будущего measured fix; здесь новый runtime defect не установлен |

## 2. Исторический P6 — не текущая Profiler приёмка

`Docs/Audits/2026-10-23/INTEGRATION.md:7` заявляет Interactor112 и все12 matrix cases на обоих SDK. Существующая ledger claim `interaction-p6` явно квалифицирует это как historical scope: raw `Local/Audits/.../p6/u63|u66/Receipts` отсутствуют; CPU/GC/external EventSystem budget не установлен. Это не доказательство, что старые tests провалились, и не свежий PASS нынешних bytes. Ledger не переписывался/не дублировался. Текущий Edit baseline сам по себе не исполняет Play матрицу и не даёт Profiler results.

## 3. Точный существующий functional driver

Fixture prefix: **`Neo.Tests.Play.Audit20261023NeoToolsInteractableObjectLifecycleTests.`**. File называется `...PlayerLoopTests.cs`, но CLR fixture — partial **LifecycleTests**; фильтр по выдуманному PlayerLoopTests class неверен.

| N | Camera3D | Camera2D | KeyboardLook3D | KeyboardLook2D |
|---:|---|---|---|---|
| 1 | `PlayerLoop_Camera3D_1` | `PlayerLoop_Camera2D_1` | `PlayerLoop_KeyboardLook3D_1` | `PlayerLoop_KeyboardLook2D_1` |
| 100 | `PlayerLoop_Camera3D_100` | `PlayerLoop_Camera2D_100` | `PlayerLoop_KeyboardLook3D_100` | `PlayerLoop_KeyboardLook2D_100` |
| 1000 | `PlayerLoop_Camera3D_1000` | `PlayerLoop_Camera2D_1000` | `PlayerLoop_KeyboardLook3D_1000` | `PlayerLoop_KeyboardLook2D_1000` |

Готовый **exact semicolon filter** выведен из реальных 12 methods в [existing-playerloop-12.filter.txt](D:/Temp/Neoxider-LibraryClosure-20261102/fixtures/existing-playerloop-12.filter.txt). Root использует его после source gate и проверяет ровно12 discovered/executed nonskipped leaves. Не принимать substring discovery или все остальные lifecycle cases за эти12.

Driver создаёт N отдельных collinear Collider/Collider2D targets, native Camera/RT256² и ScreenCenter provider; keyboard2D использует реальный planar look source. Events-null stable setup проверяется явно, obstacle LOS выключен только в этом отдельном stable-context workload. `yield null` действительно отдаёт управление PlayerLoop; manual Update/SendMessage для этой матрицы не вызываются. После warm-up ожидается:

| Stable frame | Native / cache / passes | Raw visits |
|---|---|---|
| positive N=1/100/1000, H=N | `1 / N−1 / N`, FrameAdvances1 | `1 / 10000 / 1000000` |
| следующий unchanged miss после actual exit transition | `1 / N−1 / N`, FrameAdvances1 | 0 |

В обоих stable frames: fallback/Fresh/LOS/callback boundaries/native-inside-callback=0. Camera3D→Raycasts3D, Camera2D→RayIntersections2D, Keyboard3D→Raycasts3D, Keyboard2D→Raycasts2D. Разные native kinds не объединяются. Первый transition frame логируется отдельно и вправе иметь N native queries / 2N callback boundaries. Keyboard measurement требует реальных down/up edges: удержание E без edges не запускает keyboard-look работу каждый frame.

### Dense/parity selection

Fixture **`Neo.Tests.Play.Audit20261023NeoToolsInteractableObjectSharedQueryTests`**:

- `DenseQuery_ReturnsEveryColliderAndReusesCapacity`: 17/64/1000 × 4 query kinds (3D, intersection2D, planar2D, explicit-filter2D), всего12; cold native2/fallback1, warmed native1/fallback0, same-epoch cached0/one hit. Сопоставить actual complete collider identities, а не только Count.
- `DenseTargetAndBlockerSelection_MatchesCompleteUncachedPhysics`: 2D/3D × 17/64/1000, всего6; wall blocks, его отключение сохраняет target за старым пределом16, complete uncached oracle.
- `PlanarOriginalArguments_MatchDirectNativeColdAndWarm`: legacy/explicit × 5 направлений; exact nonunit/tiny/zero/large-Z-small-XY semantics, native point/distance/collider parity.
- `Global2DTriggers_AndExplicitContactFilter_KeepTheirOwnSemantics`, `DenseExplicit2DFilter_RetainsTriggerPolicyDuringFullFallback`: complete array/List references и hard positives/negatives для глобальных/explicit flags/layers.
- `TargetLocalObstaclePolicies_ShareRawHitsAndKeepDifferentAnswers`, `ActualMasksAndSubEpsilonGeometry_AreDistinctQueries`, `DifferentCamerasAndViewport_KeepSeparateContexts`, `DistinctCenterAndPointerGeometry_UseTwoQueriesForRepeatedConsumers` защищают K contexts и target-local answers.
- `AutomaticUpdate_PreservesDistinctRangeLosWithoutRepeatingCameraQueries`, `UniqueLosDirections_RemainDifferentQueries`, `CallbackMutationAndException_EndTheEpochBeforeAndAfterDispatch`, `HoverCallbackAddingAWall_InvalidatesTheCachedClickHit`, `PublicRangeGetter_IsFreshAfterAnUnannouncedBlockerChange` — обязательные compatibility controls.

Direct native reference casts и EventSystem casts находятся **вне backend counters**; их нельзя потерять из aggregate totals. Tests содержат synchronous/unit arrangement, что должно быть указано в receipt.

## 4. Exact profiler markers и правильная CPU/GC attribution

| Marker | Что действительно включает |
|---|---|
| `Neo.Interaction.AutomaticUpdate` | Полный target pass: input/ray/key/cache, native work, classification, Core selection, callbacks |
| `Neo.Interaction.Classify3D`, `Neo.Interaction.Classify2D` | Raw-hit projection/hierarchy filters/retained buffer growth; **не последующий Core selection** |
| `Neo.Interaction.Native.Raycast3D`, `Neo.Interaction.Native.Intersection2D`, `Neo.Interaction.Native.Raycast2D`, `Neo.Interaction.Native.FilteredRaycast2D` | Каждая реальная native initial/fallback attempt |
| `Neo.Interaction.Callbacks` | Реальный external observer dispatch, включая вложенную работу |

Нужен **суммарный CPU всех N samples за frame**, не последний sample одного target. Если используется ProfilerRecorder, `SumAllSamplesInFrame` обязателен; Valid/unit/sample count проверяются, missing marker не превращается в нулевую стоимость. Эти APIs и SumAllSamplesInFrame подтверждены installed exact6.3 CoreModule XML. Costs вложены: Classify+Native+Callbacks нельзя складывать с AutomaticUpdate как независимые части; nested AutomaticUpdate нельзя двойным счётом добавлять в aggregate.

Existing functional matrix не является zero-GC benchmark: `AssertNoObserverEvents` создаёт object[] на каждый target, NUnit constraints выделяют память, InputDriver reflection QueueKeyboard создаёт arrays/boxed states и diagnostic snapshots. Запрещено выдавать это за runtime allocations или заявлять GC0 после вычитания произвольной оценки. Нужен raw `GC.Alloc` byte attribution **внутри AutomaticUpdate subtree**, а whole-frame/all-thread GC сохраняется отдельно.

Переиспользован публичный `RawFrameDataView`/`GC.Alloc` metadata path из [официального Unity API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Profiling.RawFrameDataView.GetSampleMetadataAsLong.html), с SDK XML-проверкой child ranges/time/metadata APIs. Deep Profile/framework не добавляются. Обязательная known4096-byte calibration outside runtime marker доказывает, что allocation metadata действительно записывается; без неё zero bytes не принимаются.

## 5. Подготовленные sources вне проекта — NotCompiled / NotRun

Каталог [fixtures](D:/Temp/Neoxider-LibraryClosure-20261102/fixtures/README.md), только root может импортировать/исполнить после baseline:

| File | Scope / SHA-256 |
|---|---|
| `Audit20261102InteractionGlobalPolicyProbe.cs` | 3 native policy cases; `e682a390985c46f0d573f34592160aef1025b3a5a2d5e1ca9f02e0ba54f63efe` |
| `Audit20261102InteractionRuntimeMeasurementWindows.cs` | 12 `Capture_...` methods в существующем Lifecycle partial; `12980c93fe16c4970448e8bd696dbd971171a538947db0543785b7705c7cb05d` |
| `InteractionRuntimeProfileReader.cs` | Editor-only profiler readback; `1d4156130e650e7b501d74037fbfc8b28f71973bcfac061d2eb81b2d43ed8af8` |

Measurement source переиспользует существующие CreateTarget/EnsureInput/rig/teardown, а не новый interaction framework. Требует свежий outside-project `NEO_INTERACTION_PROFILE_DIR` и idle owned profiler session; записывает GUID raw profile и counter CSV, восстанавливает profiler flags/file в finally. 8 warm frames, **120 positive + 120 stable-miss stamped frames** на workload. Никаких per-target asserts/reflection/log в выбранном loop; native input driver overhead остаётся вне AutomaticUpdate. Post-capture assertions проверяют все120 samples/phase (native1/cacheN−1/passN/rawN²|0/zero callbacks/Fresh/LOS/fallback), не только удачный sample. Capture success — не CPU/GC performance acceptance.

Reader работает только с **уже загруженным root-owned raw profile**, не запускает процессы и не загружает profile/UI сам. По фазовым stamps выбирает exact120 frames, требует N AutomaticUpdate invocations, калибровку и size metadata; экспортирует per-frame top-level runtime CPU, inclusive class/native/callback costs, runtime-only GC bytes, all-thread GC и main-thread frame time. Отсутствие raw frames/calibration/metadata явно fails. p50/p95/p99 считаются как эмпирические quantiles120 values с указанным sample count; arbitrary ms/FPS threshold не вводился.

### Conditional parity risk IP-01: не confirmed bug из одного key

Filtered2D key сейчас не включает `Physics2D.queriesHitTriggers` (`GlobalTriggers2D` только legacy/intersection kinds), хотя реальная зависимость explicit-filter native overload от глобального флага должна определяться на SDK. Existing tests вызывают ReleaseBuffers перед каждой flag combination и потому **не проверяют изменение флага внутри одного epoch**.

Prepared probe даёт global true→false и false→true при неизменном `ContactFilter2D.useTriggers=true`, одном frame, без ReleaseBuffers/Invalidate между A/B. Сначала hard2-hit trigger+solid positive и explicit-filter trigger-negative; затем complete direct native array/List reference для каждого flag и Shared result. Если nativeA==nativeB, bug не выдумывается; если native отличаются и SharedB stale — это воспроизводимый correctness defect для узкого exact-key correction. Третий case проверяет explicit3D Collide и сохранение одного Shared query при global3D toggle. Flags/owned objects/buffers восстанавливаются в finally; reference casts не выдаются за aggregate query savings.

До запуска IP-01 — **условный риск, не установленный runtime bug и не performance correction**. Root/CLI02 получают fix ownership только после native evidence. Нельзя включать/выключать global policy внутри backend для исправления результата.

## 6. Measurement recipe для root и external EventSystem scope

1. После завершения всех writers зафиксировать exact inputs/Unity/profile/hardware/MIRROR/Input System settings; root компилирует prepared sources в корректных Play/Editor assemblies. Отсутствующая optional native input capability отдельно диагностируется; all12 acceptance невозможна при skipped keyboard6.
2. В графическом Play context исполнить существующий exact12 functional matrix и указанную dense/parity группу. Сохранить XML/логи/каждый case, включая cold/transition/query kinds. Все120 long-run counter rows обязаны совпасть с workload. Functional tests отдельно от profile, чтобы не выдавать Assert allocations за runtime.
3. Для каждого N/route/dimension root выбирает новый owned raw capture. Prepared capture использует только реальный PlayerLoop; reflection input queue выполняется вне runtime marker. Reader исключает её по subtree attribution, но сохраняет full-frame overhead видимым. Сопоставить ordered phase/ordinal CSV с profiler stamps; проверить ровно120 positive и120 miss, missing/dropped data не считать нулём.
4. Отдельно собрать cold allocation/fallback frame, callback transition/observer-bearing workload и unique LOS: они остаются в aggregate receipt, но не подменяют warmed stable contexts. Dense retained memory — per-target O(N×H) high water; отдельно измерить после warm/ReleaseBuffers/teardown.
5. **External aggregate — отдельный arrangement:** owned реальный EventSystem + активный доступный InputModule и обе dimension-compatible PhysicsRaycasters, camera culling/event masks реально включают targets (base camera cullingMask0 для physics-only capture недостаточен). Реальный virtual pointer должен давать положительный hit/input control; после transition — stable pointer. Снять тот же N/geometry с EventSystem off/on, сохраняя его CPU/GC/direct Physics calls. Не выключать его в пользовательском prefab ради бюджета.
6. В installed UGUI EventSystem/PhysicsRaycaster source нет гарантированного `Neo` profiler marker для этих calls. Root получает **реальные** sample names из capture и передаёт reader `externalRootSampleNames`; reader требует observed roots. Default reader выводит **NOT_MEASURED**, а не external0. CPU union Auto+external исключает nested overlap. Все thread/native queries из EventSystem учитывать по raw capture, вне `Counters.NativeCalls`. **`ExternalNativeCalls` в Statistics означает Neo API purpose External, не PhysicsRaycaster/EventSystem totals.**
7. Сохранить raw/CSV/source hashes/units и p50/p95/p99 CPU + runtime/all-frame GC отдельно. Текущие absolute costs не становятся «улучшением». Before/after comparison требует идентичного workload/observer/key/scene/input profile и retained reference evidence; raw public API Shared/Fresh microbenchmark можно делать отдельно, не подменяя им automatic component result.

Ни CPU/frame ms бюджет, ни допустимый performance regression threshold историческим P6 не заданы; изобретать новый flaky threshold для зелёного теста нельзя. Warm allocation observation/CPU attribution и direct-query parity дадут основание root выбрать ровно измеренный hotspot. Возможные кандидаты только после профиля: повторные hit validity/frame queries, projection/hierarchy checks, Core second scan, per-target retained capacity или native fallback. Нельзя заранее объявить любой из них bug или убрать N×H semantics.

## 7. Source identities и итог

Прочитанные runtime hashes: Queries `bb19f00bff9da709c21e396b204f57828cbafa4495d41410d4048265d07d5885`, component `bcb49d73786346f38f7b369396e82e16ad146e37e76b2dfb6d5b73807780b105`, provider `0504766e4853f5a4442fe1b32ec49cd614660cbfae7df7fd284794560dfc3f8e`, CoreQueryMath `50b93c0856073f866ba10c58c52dc3b79dc176b8115c0ab5a14156578e082423`.

Play matrix source `86d8f5445aab2f532182bf65aec7d61b81d049aba5d21d64845f450497538d81`; SharedQuery tests `b9b07706131eb525120dcb9040fa70d86236a364ccf7b9ebb1ec71a540704a19`; Lifecycle setup `5823d234d696c9ed7b38773bdc1d0218bf0d0a00b596f14df882d83e9bd10ce9`; InputDriver `9b57e2e8b407dd4f02f5c660b9ddb9468d56a5be066ffe0ca064f8b5d6e86a46`. Prepared partial helper требует именно совместимых private helpers; перед import при изменении CLI02 перечитать/скомпилировать.

Source review завершён, **измеренная performance closure открыта**. Project source/tests не изменены, prepared sources не импортированы/не компилировались/не исполнялись. Нет новых dependency/framework/сцены/control manager, compiler/Unity/process/Git операций. Позднейший CLI02 final diff reviewer проверит независимо после exact root handoff; остальные 56 действий, ledger и browser work не переаудировались.
