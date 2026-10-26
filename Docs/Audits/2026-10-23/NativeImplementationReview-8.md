# Native implementation review 8 — Neo.Tools.Time and Neo.Tools.Spawner

Status: **SOURCE REVIEW COMPLETE for both assigned providers; NI8-01 and NI8-02 OPEN with the original owners.** Reviewed Time runtime changes and Spawner production/cleanup changes are coherent within the boundaries below; the new Time fixture annotations and first-construction retirement hole prevent blanket approval. All compilation, discovery, tests, native lifecycle and workload acceptance are UNEXECUTED.

Reviewer: SAME `/root/audit_abilities`, `gpt-6.1-sol` / `max`; this is independent review of other owners' Time/Spawner scopes, not approval of the reviewer's Abilities implementation. Repository `D:/Git/NeoxiderTools`, branch/target `demo` / `11.0.0`. Only this Markdown report is written. No Unity/MCP, compiler, test/validator/checker, installer/service, process management, product edit or child agent is used. Global execution gate remains closed.

## Reviewed contract and coverage ledger

Complete reads: both implementation requests and receipts; the implementation contract; every current changed hunk in all four Time and eight Spawner runtime files; those twelve current source files in full; all eight Time and five Spawner prefixed fixture/helper sources in full, with their thirteen metadata files; all five Time and ten Spawner changed public guides in full. The initially truncated TimerObject load/reset interval and short Spawner diff middle were separately reread. The identity table below enumerates these exact artifacts.

Integration context was read without editing: full `ISaveProvider` to reconcile the in-memory fixture implementation, the relevant `ReactiveProperty.Value`/subscription entry points, and Singleton resolution/Awake/TryGetInstance bodies. Those last two are partial contextual reads, not full Reactive/Managers module audits. Only this Markdown report was written. The hash program reads bytes and records identity only; it is not a product checker, test or compiler.

Original implementation owners:

- Time: `neo-audit-1023-b08-05`, assembly `Neo.Tools.Time`.
- Spawner: `neo-audit-1023-b08-03`, assembly `Neo.Tools.Spawner`.
- Bonus, Draw and Demo remain consumer owners. This provider review does not accept their pending integration merely because an additive API exists.

## NI8-01 — P1: unavailable NUnit attribute reintroduced in two Time fixtures

**Confirmed current source defect; OPEN with original Time owner.** Exact current files:

- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeUtcHookTests.cs:11` — `[NonParallelizable]`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockRestoreTests.cs:9` — `[NonParallelizable]`.

The complete implementation contract explicitly states that this installed Unity NUnit configuration lacks the attribute, that it caused CS0246 on both preliminary Unity versions, and that it must never be reintroduced. Current independent source reads show the attribute present on both newly authored classes. The Time implementation receipt also says the global-provider fixtures are non-parallelizable.

Reproduction/expected acceptance: compile the owning Edit test assembly in either installed verification Editor with these source attributes. Under the already established assembly/NUnit contract this repeats the unresolved type diagnostic rather than discovering the cases. The correct source must rely on the shared exclusive runner plus exact provider/clock restoration, without the unavailable annotation. This review did not execute compilation; historical diagnostics are contract evidence, not a current run.

Smallest correction: SAME Time owner removes the two annotations and fixes the receipt's serial-execution claim to the approved exclusive-runner/isolation contract. Preserve the actual try/finally/Dispose cleanup. No asmdef/package/NUnit dependency change is needed. Root was notified immediately; only the original owner may edit those files.

## NI8-02 — P2: retirement during first pool-construction callback is lost

**Source-traced current retirement defect; OPEN with original Spawner owner.** Exact paths/methods:

- `Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs:65–67`, `PrewarmPools`, and `:81–83`, `GetOrCreatePool`: register the pool only after its constructor returns.
- `Assets/Neoxider/Scripts/Tools/Spawner/SpawnUtility.cs:180–181`, `GetOrCreateFallbackPool`: same ordering; caller `Spawn:129–130` immediately retrieves from that result.
- `Assets/Neoxider/Scripts/Tools/Spawner/NeoObjectPool.cs:48–59`, constructor prewarm: invokes create/get callbacks and then release callbacks before the manager/fallback registration exists.

Concrete reproduction, through either a manager first Get or a fallback first Spawn: attach an IPoolable probe whose first creation/get callback once invokes `SpawnUtility.RetirePool(exactSource)`. Configure positive initial prewarm (the defaults are ten). The callback does not return or re-rent its transitioning instance and does not throw. The unified/source retirement marker is written, but both registries lack the in-construction pool, so it is not retired. Its constructor continues prewarming because `IsRetired` remains false. The outer factory then inserts that pool despite the marker, and the already-started Spawn/Get returns an active rental. On return it is retained rather than destroyed; inactive clones and the new registration remain until a later explicit retirement or unrelated teardown. Future top-level unified Spawn calls are blocked by the marker, which does not clean this leaked pool.

Expected: retirement of that exact exclusive source remains terminal across callback boundaries. Construction must not publish a live rental/registration after its source was retired; retained clones must be cleared and valid returns must follow the retired destructive policy. Another same-named source/storage root must remain untouched. Returning false for a registration not yet present does not undo the API's documented terminal marker.

Smallest bounded correction: make in-flight pool registration/terminal retirement visible before prewarm callbacks, or give that construction an equivalent exact-source retirement admission path; after callbacks never overwrite/reinsert a retired registration or publish its rental. Cover both manager factories (including preconfigured startup) and fallback creation, with null-result handling at fallback Spawn if the factory rejects. A local construction/registration ordering correction is sufficient; no general pool lease/transaction framework is required.

Required new acceptance sources: manager and fallback first-callback retirement, exact source terminal rejection, no retained retired clones after owned destruction timing, destructive late return when a rental genuinely remains outstanding, and survival of another same-named source/root. Current later-retirement cases do not exercise the constructor-before-registration window. Root received this source trace while the review continued. No reproduction was executed.

## Neo.Tools.Time — bounded runtime correction supported; fixture blocker remains

### Typed state revision, running state and ordinary cadence

`TimerObject.cs:1057–1153` increments the existing playback generation in SetTime, SetProgress and the two-argument SetDuration. AddTime delegates to SetTime; the one-argument SetDuration delegates to the versioned overload. These setters do not set isActive. They therefore retain an already-running state but do not implicitly start a timer that the outer terminal path has stopped.

UpdateTimer captures generation before updating; the finite terminal branch deactivates before notification, then checks after InvokeEvents and completion before any loop restart. Day/midnight and infinite branches check after their callback-bearing channels. InvokeEvents captures the time/progress and revision, checks after reactive time, progress, percent, milestone and day callbacks, then reaches UI only if still current. Milestone arrays remain a coherent local pair for each dispatch. UpdateUI checks revision after setting Image.fillAmount before writing text, preventing that engine callback from rendering old seconds after a nested setter. Reset and Stop validate after their callback; StartTimer validates the Reset generation before its pending Play; Play validates its started event before animation/initial notifications.

Normal finite updates still consume the accumulated interval once, zero the old buffer before callbacks, and publish in the same reactive → progress → percent → milestones → day → UI sequence. This preserves nested new-run buffered time. No per-frame catch-up scheduler was added. Same-UnityEvent remaining listeners still finish normal dispatch; already delivered notifications are not rolled back. Direct field writes, arbitrary nested Tick, disabling without a typed state command, animation tween/coroutine ownership and nonfinite configuration beyond the documented delta admission are not newly isolated by these revision checks.

Earlier Timer.cs changes retain per-run cancellation ownership and generation-gated finally cleanup, cancellation before countdown, completion-handler restart and an actual player-loop yield for an empty looping cycle. Nonempty cycles retain their existing yield/interval cadence. Duration/AddTime clamps and GameTimeController finite speed admission are consistent with the updated guides. Scheduler/native execution is still required; the synchronous handler-completed Timer fixtures do not prove an uninterrupted real player-loop run.

### UTC provider boundary and default restoration

The additive `protected virtual DateTime GetUtcNow()` at TimerObject:189 defaults to device UTC. Exactly the three RealTime start/deadline writes at :433/:453/:458 and two elapsed/remaining reads at :493/:539 use it. Seconds save/load returns without consulting the hook. The paused finite/infinite frozen-seconds path remains ahead of timestamp recomputation, with unchanged `_t`, `_a`, `_rt` keys and legacy missing-active/frozen-data fallback. Override availability during Init/Awake and disable saving is documented; no clock-domain conversion of old records is silently claimed.

`TrustedClock.cs:200–226` sends null injection to complete ResetStaticState, clearing clock/restored/persisted cache; next Clock access recreates and restores the saved high-water. Supplied clocks keep markRestored and their exact identity. Date ceiling saturation and the existing 90-second session tolerance/15-second persistence threshold remain documented. No Time-to-Bonus dependency was introduced.

**Provider-only disposition:** the hook and typed/default-clock corrections are supported by source review. Bonus must still supply/review its trusted override and sequential claim/deadline/restore controls in its own scope. This report does not close global BONUS-CLOCK-BOUNDARY or approve pending Bonus economic integration.

### Regression sources, isolation and docs

All new Time sources are nonvacuous: four terminal typed mutations assert changed state, 50%/Image value, no old completion and no implicit start; started/reset/stopped/StartTimer cases check notification/restart suppression; day cases check midnight/progress behavior; conditional TMP cases check actual reentry and final text. UTC cases assert the persisted anchor and a clock-only 30-second restore across finite countdown/up and infinite modes, frozen paused state, the default UTC read window, and Seconds mode with a throwing UTC delegate. Null/supplied clock tests cover both markRestored values and exact clock identity/high-water behavior.

PersistenceScope uses the current ISaveProvider members, an in-memory provider, and exact prior provider/three clock cache fields restored by Dispose/finally; destruction disables saving first. This isolates new persistence storage. Older unique-key TimerObject tests still mutate only their own `_t/_a/_rt` key family on the active provider; isolated verification hosts remain necessary. The global-provider annotation/receipt claim is specifically blocked by NI8-01 rather than accepted as isolation evidence.

Source case counts reconcile to the receipt's 28 new cases with TMP / 26 without, plus 27 historical audit cases; they are authored counts, not discovery or results. All five guides accurately distinguish plain UniTask Timer from Tick-driven TimerObject, restart/resume, default/custom UTC, persistence keys and clock-domain limits. No other actionable Time source defect was found in this assigned correction scope.

## Neo.Tools.Spawner — production and release boundary supported; first-construction retirement remains open

### First synchronous callback and production ownership

Spawner:234–253 creates a production generation and sets isSpawning before StartCoroutine can synchronously invoke a first spawn/wave callback. The returned handle is published only if that generation remains current/running; otherwise only its own returned handle is stopped. A nested Stop/Start therefore keeps the inner generation's handle and running flag. Stop, Clear and OnDisable invalidate generations; only Stop's producer handle is cancelled, preserving independent expiry, while Clear explicitly cancels all coroutines before its snapshot release pass.

RunSpawnProduction checks before/after wave/object callbacks and yields, before count/wave/final-state publication. SpawnObject captures generation/wave before allocation and suppresses superseded later object/wave/RPC tails while retaining tracking of an already-created object. Ordinary event order remains. Public manually started SpawnObjects enumerators keep caller-owned Coroutine cancellation; a generation is not represented as ownership of the caller's external handle. The new first-object case performs immediate callback Stop/Start, another later Stop/Start with a ten-second replacement, then checks that old 0.02-second producers cannot wake while their independent expiries still run. The first-wave case distinguishes two wave starts from one surviving body. These are meaningful real-Play source controls, all UNEXECUTED.

Earlier generation-tracked Clear/expiry/count and Despawner reentry changes remain coherent. They protect against returning an instance that another rental generation has acquired. Source-preserving Inspector fields and public methods remain available; the behavioral migration (suppress an invalidated producer's remaining tail) is documented. This is not a general exception/transactional spawn framework.

### Exact retirement and one-shot resources after a registered acquisition

Outside NI8-02's in-construction window, manager retirement removes its exact GameObject-keyed registration before Retire, and unified retirement handles manager plus fallback markers without creating/searching for a manager (`Singleton.TryGetInstance` only reads the assigned instance). Managed references to destroyed Unity sources can still retire their entries through ReferenceEquals admission. Weak markers do not themselves retain the destroyed key. Shared prefab-name storage roots are deliberately preserved; no name is treated as exclusive root ownership.

NeoObjectPool.Retire marks terminal state before clearing inactive entries. Retrieval rejects retirement before Get and rechecks after callbacks; outstanding valid rentals retain their cached IPoolable set and return via release/unspawn/cleanup then destruction. A release callback retiring the already registered pool is handled by the post-Release clear. Clear remains a separate reusable/inactive cache operation. NI8-02 is a specific missed creation boundary, not evidence that ordinary late-return or same-name ownership paths are broken.

PooledObjectInfo cleanup validates exact owner/generation/outstanding state and an empty single slot. It rejects stale/foreign/returned registrations without replacing another pending callback. Invocation captures and detaches the slot before calling it, then validates the captured owner/generation. Release, capacity eviction and OnDestroy therefore cannot replay that registration. OnDestroy marks returned state before external-destruction cleanup. The hook is separate from cached IPoolable discovery and can be installed after acquisition. Valid pool release preserves cached IPoolable → active-server Mirror UnSpawn → cleanup → storage/deactivation order. No old owner is authorized to return/mutate a later generation merely by possessing the GameObject reference.

Hook/lifecycle callbacks retain the explicit nonthrowing and no-return/re-rent-of-the-transitioning-instance restriction. A cleanup must dispose its exact local record even though IsInPool is true. Actual Draw material records and every old Drawer Delete/OnDestroy access remain **pending consumer review**, as do Demo's exact-source disposal and shared-name preservation. Provider API and material surrogate tests do not close those consumers' assigned actions.

### Regression sources, composition and docs

Five Edit lifetime cases assert inactive clearance/terminal Get/destructive late return, same-named source/shared-root survival, exact borrowed versus private material identity before reuse, foreign/stale/occupied/returned hook rejection and one cleanup through capacity eviction. Play cases assert release-before-cleanup, external destruction, 100 exact retirements returning PooledObjectInfo count to the original baseline while another same-named pool survives, destroyed-source retirement and fallback shared-root preservation. None exercises retirement during a first constructor prewarm callback; NI8-02 requires that additional control.

The conditional Mirror suite checks host event deduplication, server/client unregister-before-cleanup and new net-id reuse, plus retired active rental late return. It observes real public registries/callback context, but has not run or established a remote-client/custom-handler guarantee. MIRROR symbol presence in the actual test assembly and nonzero discovery must be checked centrally; zero conditional cases is not success.

All five prefixed Spawner sources reconcile to the receipt's 42 authored cases including historical cases, not discovered/passing tests. Fixture cleanup owns objects/pools, saves/restores time scale/fallback policy/Random where used, and removes delayed producers. Global fallback clearing, singleton assumptions, host shutdown and ClearSpawners require the isolated/exclusive native runner; these fixtures must not run against user scenes or an unrelated live host. Material tests require the built-in shader and native Unity destruction timing.

All ten Spawner guides were read. They describe actual callback names, prewarm, inactive retention versus active capacity, final-pose timing, manual IEnumerator ownership, exact retirement and one-shot cleanup. Their terminal retirement promise needs NI8-02's source correction rather than weakening the documented exclusive-source lifecycle. An empty shared storage root is intentionally retained, not claimed to be automatically reclaimed.

## Aggregate work, verification debt and handoff

Time owns distinct per-timer elapsed/revision/UI state; equality of duration does not make those states shareable. Interval buffers throttle dispatch and trusted clock persistence is thresholded. Spawner's production/expiry coroutines and finite collider rejection attempts remain per producer/instance, with IPoolable discovery cached per created object. This patch adds weak per-source retirement markers and one per-rental cleanup slot, not a per-frame global search. Registry construction/prewarm is the concrete retained-resource boundary in NI8-02. No invented FPS or GC-byte measurement is asserted.

Root should return NI8-01 to SAME Time owner and NI8-02 to SAME Spawner owner, then obtain independent reread identities for those corrected deltas. Keep Bonus, Draw and Demo/Survivor integrations pending until their own source receipts/reviews are complete. No optional timer snapshot, generic pool lease/refcount, network extraction or global transaction framework is required for these findings.

After every source writer finishes, root alone lifts the gate for installed Unity 6.3/6.6 compilation, nonempty Edit/Play discovery, Time normal player-loop/empty-loop and optional TMP/DG_TWEENING acceptance, Mirror-on/off host/remote/custom-unspawn behavior, material/shader/native lifecycle, disabled-domain-reload and incoming-Awake scene-load fallback timing, consumer cross-module regressions, metadata/GUID/UTF-8/docs/package validation. Nothing here is a current compiler/runtime pass; preliminary diagnostics and source counters remain explicitly separate evidence.

## Reviewed identities

Source identity checkpoint follows below. Paths are absolute under `D:/Git/NeoxiderTools/`; hashes cover raw current bytes at the stated checkpoint and are bookkeeping only. ReactiveProperty/Singleton are explicitly partial contextual reads; all other listed sources/guides/fixtures/metadata/contracts were completely read. Any subsequent owner correction requires a new source-read/hash checkpoint before its finding is closed.

Checkpoint: `2026-10-23T19:31:28.481499+00:00` (UTC; local date 2026-10-24).

| Absolute path | Bytes | SHA256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | 15329 | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Time.implementation-request.md` | 6693 | `5b1932d1270aba95d851ebd3aa172d9fe922688f847840e9eb0d6441c2d90ea6` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Time.implementation.md` | 9575 | `2b1c5b94fd91c14e8d6680953e7120628ac37a099091f52403a7c42acc474edb` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Spawner.implementation-request.md` | 12865 | `6d7d5dc2d35d33d22f2277b916f3786584115cf5f593f3097d571b67eff3f3e7` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Spawner.implementation.md` | 11367 | `4e4e79f1d6434e5eeeac69486633094ad4b9623920e9222c4fe57cf6324a040b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs` | 40889 | `2131a3f3d8fc214490bd06b12b47e52c5bb3b60bd74257c3041301eab8840489` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Time/TrustedClock.cs` | 10317 | `164dc0f9eb40153ff43fbc98baedb08e3167040e802101db70eacdfb552abbcf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Time/Timer.cs` | 14723 | `d25ef8ae5d0a512aeac16a089069ea6a327ae8f964d20ec89ef97e333c4056d9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Time/GameTimeController.cs` | 1682 | `99c5a473164e97a342e13ea8debeeb9e4d690af94ba5832947b648375d670754` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/NeoObjectPool.cs` | 10348 | `de0a2e7da678d6ef1700ac8cad7e18a583204c7fde48f020c9b2d19ad5cfe0f5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PooledObjectInfo.cs` | 3390 | `bcaacd8cf2e83c5c4abd766d9b43c6dd146dccbc9ddb74ecb7e5ba5b6371feda` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs` | 5925 | `6a99ae511847557b7d06b7cc8310021c9416e8da10248a27be7220e18ae2763e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/SpawnUtility.cs` | 9154 | `ca95ae47f98ecff8b20e66b6bde54bc8e3d99042d8211c137e4a1b85a7755852` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/Spawner.cs` | 34460 | `e1896b93f6e00bdf9840ad23576ee6cc3bceec29e7d8c5df569aaea2dd1e579a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/Despawner.cs` | 4743 | `f639b78e394e68e0273c9fccae347695376af627ebc51a06cbf3575f8e6a5ddd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PoolExtensions.cs` | 882 | `ee1845f4df7fa9554762728bc6a23f1648240898bed3e1cef05c6eacf92b820b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/SimpleSpawner.cs` | 1418 | `837cb18b281e139cc86dbe52171ff4ffad2e80f9860fbc59a48a865e56cb92f3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Time/README.md` | 3195 | `82df84c577b03a3b6697f452acc8cb2c7792a67fd593a2d683aab8aafcf4ec34` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Time/TimerObject.md` | 9269 | `45929ca03506d67e13566a5b8187c90850bed71661835bc53f636d46aa4c881d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Time/Timer.md` | 3502 | `db524023d82aee7612c8004c2fe66ae3f94d91f090efac76cc2627315680f333` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Time/GameTimeController.md` | 1586 | `f06f713e8022589f5c4d0b4d3dcc1d731526d4f6e575c1c62088542738e35382` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Time/TimeFormatting.md` | 1846 | `93b4e21eccc7f7dd61545eba3707992c0d5266b1a87d2d5013adc838f0227510` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/README.md` | 3084 | `6c0827d71d019b27b0036572973a298c962b9be56ad6b77015685ccb92a68c59` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/NeoObjectPool.md` | 4983 | `c978b505006f0f127088791989f77a91ac7037779652578c3147cb7e85b1ec6d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/PooledObjectInfo.md` | 3115 | `689358dc2ab46880c64894007348976853cc619961189baf28d65bf67cad26f7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/PoolManager.md` | 3501 | `393b86fc9249f0672a08ef8341de8edcafe2b39c4ab1bed7b810bed9091e0b50` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/SpawnUtility.md` | 3893 | `7c3cf727f1a8ca727ba22d85fd3830e7e639978a591ef394b498178145c47a09` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/Spawner.md` | 7580 | `37a5309011ab9715f2bab08f1066570f13f5cbe7162b73ac8e8ae8116da96d50` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/IPoolable.md` | 1131 | `012d681fcf2d49b5f518e7c5cbc28584d9013d24eeb8b45e3f8f07cb85be088f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/PoolableBehaviour.md` | 1523 | `3cbbc31109aad51567799e50fa7f776485f8d9f912b000ea8c91ea690f96a154` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/Despawner.md` | 2459 | `48d4813ede96846375bdc9aff7a3e277755ab3fae2c358b6fab2364f91392c33` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/SimpleSpawner.md` | 1390 | `1ae7fd503cbd3a8487e023deafc3c66a854bae488ee5f76e4477567a9092d652` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeMutationTests.cs` | 11014 | `ac11a33d5a8b0c7604c70d33e1efdce68d41e5b26713de643d23b941df09dc81` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockRestoreTests.cs` | 2172 | `441b74971e16175d78757aadf42d870739df9a552733f8071b562fa47c7e78ff` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockAndScaleTests.cs` | 2483 | `f710110ed2cc299f1daf0d598e174a3b5c5b3111f1913ab056448ed660612aaa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeTimerObjectTests.cs` | 9947 | `344cce270f609677b6f77f46d54adf60b0cdacb5312ee79ade36eb35955044c2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimePersistenceScope.cs` | 3673 | `23da0c508ebac597cb40244a0e9544efc38ac72612b8e8a5893c659e8367fe39` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeTimerTests.cs` | 5500 | `25ef6f95515170a205bc38d81a07f0d833685e604aecef9a3b7c8d3ea1c63efa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeUtcHookTests.cs` | 5117 | `e4687bae1b2947f962dcfd6abcf88fcd4a306fc47e4389f1ae6642e2b178db13` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeTimerProbe.cs` | 1058 | `8b2ffcd98040d22240c6b611f6a1376933575fd859cb13499dcf4243084e2098` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsSpawnerTests.cs` | 11047 | `56849da7c6eb2be557d98697e3991986601dd2462e7196ae3c6b694f6be73bcc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsSpawnerLifetimeTests.cs` | 9507 | `0dd04fe912abaae5f1f7086d72e2f31328f204eec8bde4fc2e5a6d91052a851c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPlayTests.cs` | 23770 | `083df1a92e526dfcd9738e81422e020e1638e4b3ce1129884b2eb194ef1d47f3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPoolProbe.cs` | 452 | `f069948a6367c7f04acc0400bdf080cf5f778d89e2f452843eaef6e548df738b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsSpawnerNetworkTests.cs` | 8762 | `6129b469bd533dd621f9f9fa456579d765d80dea14ecd8ef18835ac79f64ecb1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeMutationTests.cs.meta` | 240 | `61d885f81c0df03ca729f65ca603fa71b66ef82bb159ee848b6962d010d4090d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockRestoreTests.cs.meta` | 240 | `67cbc7ec71429d97f1b3bda5f10245022cef3da3d8e8686aa893c22c972df972` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockAndScaleTests.cs.meta` | 240 | `cc53ed13ecac0df336407e79f530284ab50734379990ab078b72ca718c504011` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeTimerObjectTests.cs.meta` | 240 | `c45d32a06147969842fe3354ce0d682dab01bf5383cb11446ce5828418d6e6c9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimePersistenceScope.cs.meta` | 240 | `889c7215771a4c10bbddf159ab7a149a64bda04eadbf2a6516cc75c6c3b0e40c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeTimerTests.cs.meta` | 240 | `97969a4f6fc553049ced09b9eefef196e506958788a3bd459f70d7f159556bca` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeUtcHookTests.cs.meta` | 240 | `8de23d208a8638090a60a64a099079ee32cbbb161ccb2b0375eadbf2b6b9cc36` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeTimerProbe.cs.meta` | 240 | `e0fcd6187df75367f98567f01e95042bc283805a76f1f140c1803fcc12066b11` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsSpawnerTests.cs.meta` | 251 | `d2d195d7db83731e4d77ec872956bd30d28062f2dc85ea05350dae69e4a747c6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsSpawnerLifetimeTests.cs.meta` | 240 | `d1cda876a3447a3c6d0304331079fcb98d6f2d914e0bee70ac1edfc31ebd1fa2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPlayTests.cs.meta` | 251 | `62a2fbf736e824e2fdfb2a9f53e1ec90e37abbadb58a7c7f2179f625b66f3d6e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPoolProbe.cs.meta` | 251 | `840e7086bd080fa5e2e2529fd33762b710e6b065695df4f0609fe504748ae3b7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsSpawnerNetworkTests.cs.meta` | 251 | `002bd60408c4fdb531d2d5a6376fe30c76c4c09ba8cfccc4b64dffd5b355ce95` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Providers/ISaveProvider.cs` | 4059 | `dc614ab88969fd45948d93fbcc81f14f9631b8b8654ef550f4832f7839e07bcd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Reactive/ReactiveProperty.cs` | 21394 | `14df81d1e13d76b658e20e8560dfa14376ce882c716a827da80f34e03256b2a4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Managers/Singleton.cs` | 6132 | `fcc91f59e26dec884c2604943d0de58aee7c8023c2a1fae20ac89b113e71c874` |
