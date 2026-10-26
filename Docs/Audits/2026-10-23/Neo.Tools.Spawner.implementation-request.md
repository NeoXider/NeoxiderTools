# Neo.Tools.Spawner: approved implementation request

Original actor: `neo-audit-1023-b08-03`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Publish minimal exact-source retirement API and per-generation one-shot cleanup hook early for Draw/Demo. Prevent creation after retirement, clear inactive instances, destroy outstanding rentals on late return; root by name may be shared, never delete by inferred name ownership. Keep cached IPoolable and network callback ordering.

## Assigned review actions

### DRAW-AUTOMATIC-POOL-RETENTION (P2; approved-source-correction)

Drawer destroys automatic source but PoolManager retains source-keyed pool/clones/closure. Assess smallest safe retirement of own generated source pool without destroying shared active leases.

Root decision: Retire only exact unique automatic source registration, clear own inactive, no new creation; outstanding rentals destroy on valid return. Preserve shared assigned pools and potentially shared prefab-name storage roots; generic refcount framework optional.

### DRAW-EXTERNAL-MATERIAL-LEASE (P2; approved-source-correction)

External pool return/reacquire can leave old Drawer material record that later destroys material used by new lease. Validate generation/ownership transfer on all return paths; no lifecycle claims from timer-only generation guard.

Root decision: Drawer guards all pooled accesses by captured owner+generation; existing PooledObjectInfo/NeoObjectPool one-shot generation cleanup before reuse/eviction restores borrowed material and frees old clone. No cached-IPoolable-only late-added callback or generic registry prerequisite.

### SPAWNER-START-CALLBACK-REENTRY (P1; approved-source-correction)

StartCoroutine first spawn callback Stop→Start runs before _spawnRoutine assigned; old production resumes/overwrites new handle. Per-production generation before start, after callback/yield and before handle/final-state writes.

### DEMO-EXACT-POOL-RETIREMENT (P1; approved-source-correction)

Survivor deletes prefab.name root affecting another pool yet retains registration. Use exact unique automatic source retirement shared with Draw; never delete shared manager roots by name.

## Relevant full native sections

Source: NativeReview-C.md

### C-23 / P2 — automatic Drawer pool registrations outlive their unique destroyed source

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:409–430` creates unique automatic source; `OnDestroy:190–202` destroys it. `Tools/Spawner/PoolManager.cs:35,69–78` retains the source-keyed NeoObjectPool, whose `_prefab`/component cache/ObjectPool retain source/instances; Clear has no registration retirement. Repeat create/draw/return/destroy one hundred automatic-source Drawers: registrations/inactive clones remain until manager dies; the retained pool may later try Instantiate on dead source. Expected disposing this unique owner releases registration/retained resources without disturbing assigned/shared prefabs.

Bounded joint repair, not full lease framework: Spawner owner **`neo-audit-1023-b08-03`** provides exact-source retirement that removes registration, clears inactive objects, prevents further creation, and destroys any outstanding valid rental on late return. Draw owner **`neo-audit-1023-b06-08`** returns its current rentals then retires **only** its automatically created unique source, never assigned prefab pools. Preserve other pools/storage: current storage lookup is by prefab.name and can share a root; retirement must not assume root exclusivity. Test counts/resources return to baseline across repeated owner disposal, outstanding external rental late return, and a second assigned/shared pool survives. Reference-counted shared registrations/general policy API remain optional.
Source: NativeReview-C.md

### C-24 / P2 — old Drawer cleanup can change a reissued renderer and its material

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:680–741` (`DestroyLineMaterial`/`Delete`), OnDestroy and `_lineMaterials`. A creates pooled line/material, external caller returns it, B rents same renderer and installs its own material, then A.Delete/OnDestroy runs. Only delayed expiry captures generation; Delete returns B's active renderer, and A's material record can be a borrowed/restored material in B. Expected every old-owner operation leaves current rental activity/material/geometry intact.

Minimum capture `(OwnerPool, SpawnGeneration)` for every pooled acquired renderer and check before all instance mutations, not only timer. Add a one-shot generation-specific release/eviction cleanup on existing metadata/common pool release, **outside cached IPoolable discovery**, so a material owner attached after creation can restore/free its exact clone before reuse. Preserve established IPoolable/network callback context/order and cleanup once. This can be a small typed hook, no general registration/lease registry required. Tests: external return/reissue with two Drawers, old Delete and full OnDestroy, capacity eviction and no retained/destroyed borrowed material. Owners **Spawner `neo-audit-1023-b08-03` → Draw `neo-audit-1023-b06-08`**. Existing generation expiry test is useful but insufficient. Full arbitrary pool-callback rollback is not requested.
Source: NativeReview-C.md

### C-25 / P1 — callback restart occurs before Spawner owns its coroutine handle

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/Spawner.cs:232` (`StartSpawn`), `SpawnObjects:266–314`, `SpawnObject:457`. StartCoroutine advances synchronously to first yield before assignment to `_spawnRoutine`. A one-shot OnObjectSpawned listener calls StopSpawn then StartSpawn: Stop sees no current returned handle yet; inner loop B starts, outer A resumes while shared isSpawning is true, then its returned handle overwrites B. Both produce; future Stop can cancel only A. Expected only latest production session continues, independent delayed expiry preserved.

Smallest per-production generation allocated before coroutine dispatch, validated after external callbacks/yields and before returned-handle publication/final isSpawning write. Superseded handle must not overwrite/stop current B; public manually-started enumerator ownership remains explicit. New real Play regression restarts from first spawn/wave callback and observes only two initial spawns then latest cadence, final Stop and no resurrection. Owner **Neo.Tools.Spawner / `neo-audit-1023-b08-03`**. Current direct StopThenRestart test starts outside callback and does not cover the synchronous first-step window.
Source: NativeReview-C.md

### Neo.Tools.Spawner — reviewed

Full audit/plan, all eight changed source diffs, full NeoObjectPool/PoolManager and relevant producer/despawner/fallback bodies, all four complete new regression/support sources, and all ten documentation diffs read. Foreign/double return, destroyed inactive retrieval, max-retention normalization, duplicate manager prewarm guard, Despawner per-generation reentry and Spawner snapshot Clear/count/expiry are coherent within documented lifecycle-callback restrictions. C-25 remains; C-23/C-24 are bounded joint work required for Draw composition, even though original Spawner plan calls registration retirement out of feature scope.

Fixtures observe activity/owner/count/geometry, preserve Random.state/timeScale/fallback policy, clear owned pools/spawners and destroy objects; host tests stop host and clear spawners. Global ClearFallbackPools/NetworkClient.ClearSpawners and singleton assumptions require isolated native execution; symbol-gated zero-discovery is not a pass. Callback fixtures cover reentrant spawn on release, but not first synchronous production dispatch. Mirror active-registry unspawn source matches intended before-storage order; weaving/host/remote custom handler behavior is unexecuted. Pool lifecycle callbacks remain forbidden from throwing/returning their own transitioning instance; do not widen these guarantees accidentally.

Aggregate: configured per-producer coroutines/finite retry collider sampling and pool component-cache discovery, no shared camera ray. Broad IPoolable discovery occurs once per created instance; cached set intentionally excludes later-added hooks, hence C-24 cannot rely on adding a late PoolableBehaviour. Source-key pools/retention and inactive storage are actual retained resources; this is not an invented FPS issue. Single-load fallback created during incoming Awake then cleared by sceneLoaded remains a concrete integration scenario to execute before claiming scene safety; true ordering/provider context must be observed. Capsule test covers containment rather than uniform density; preserve per-axis/scaled collider semantics. Optional PoolLease/pose-first/warmup/active-limit/network-assembly extraction are independent proposals.

Full read fingerprints for Neo.Tools.Spawner:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Spawner.md` | `a717e6015f25c74776db060424860a5ee84dc9f4a4c6c01823a82aabe5b717d9` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Spawner.plan.md` | `9acd54eaba67db1217b610fa33ba959fd0ec625c8c6707bc43e96b173ce35329` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/Despawner.cs` | `f639b78e394e68e0273c9fccae347695376af627ebc51a06cbf3575f8e6a5ddd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/NeoObjectPool.cs` | `30da991d202d8d75732963e1f4c918f0ddf8e41d25e43870cfbb361cbdcb57d2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PoolExtensions.cs` | `ee1845f4df7fa9554762728bc6a23f1648240898bed3e1cef05c6eacf92b820b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs` | `ebaf2fed22f60a56438c028a08f633b20a11b8f5050e60ca6c41a485df65c1c6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PooledObjectInfo.cs` | `7120f1b50d71ca39aa6bd20fdfb67065b4e461e17db585f50e83d070e54205f6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/SimpleSpawner.cs` | `837cb18b281e139cc86dbe52171ff4ffad2e80f9860fbc59a48a865e56cb92f3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/SpawnUtility.cs` | `7fbf7baf53401b9c9386cb87e7f39d0f35527023a849a0dcf98b74f84ad8fbaa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/Spawner.cs` | `a6e0fdfe80e3856a3da3a6e0e9ca02fcdb0f3a33928e5711dbc4d8fd3b53d2c6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsSpawnerNetworkTests.cs` | `2b9fb58d5fbb0faa4fe6a386175bb3536c1cde13d2d61a45d3613bb373f09cdd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPlayTests.cs` | `0af6c7682182c79b8b8b9f6bd2a812a092e5dd9d394efa92abe112a2b9bbee3b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPoolProbe.cs` | `f069948a6367c7f04acc0400bdf080cf5f778d89e2f452843eaef6e548df738b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsSpawnerTests.cs` | `56849da7c6eb2be557d98697e3991986601dd2462e7196ae3c6b694f6be73bcc` |
Source: NativeReview-C.md

### C-35 / P1–P2 — Survivor teardown deletes pools by name and leaves their registrations

`D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorGame.cs:84–107`: tracks `template.name + "_Pool"`, destroys manager child root by name. PoolManager:80–90 can share that root for different same-name prefab identities and `_pools` still retains both entries. Dispose one Survivor with same-named unrelated prefab: its storage can be deleted; repeated scenes retain dead template keys/pools. Expected retire exact owned registrations after returning own rentals, preserve other prefab pools.

Reuse bounded retirement from C-23, store actual template identities/registration ownership, do not infer ownership from names or reflect into dictionaries. Shared root destruction must be reference-safe; outstanding rental late return must follow retired policy. Owners **Spawner `neo-audit-1023-b08-03` → Demo `neo-audit-1023-b05-05`**, joint acceptance with Draw. Test distinct same-named templates, active rental teardown, repeated recreation and unaffected external pool. General shared lease framework remains optional.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Spawner.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Final NI7/NI8 integration handoff

ROOT personally FULL-read NI8 and accepts NI8-02/P2. First OnCreate/Get callback can RetirePool(exactSource) during constructor/prewarm BEFORE manager/fallback registration, then outer factory inserts/returns live rental. Make fully initialized in-flight pool visible to retirement BEFORE prewarm callbacks, or equivalent local exact-source admission. Prevent reinsertion/publishing after retirement; cover manager GetOrCreate, startup PrewarmPools and fallback (including null-result handling). Keep public constructor/API compatibility and normal prewarm/callback order. Add real first-callback retirement manager/fallback/startup controls, terminal rejection, no retained clones after destruction timing, outstanding late-return destructive control and another SAME-NAMED source/root survival. Preserve callback no-return/re-rent-of-transitioning-instance policy, no general framework. Source/docs/receipt only; no execution.

## Snapshot3 actual SPAWNER-NATIVE-FIXTURE-COMPOSITION

ROOT has closed source3 execution after every owned native job stopped. SAME original actor only, gpt-6.1-sol/max. No Unity/compiler/checkers/tests until ALL source writers finish and root opens a NEW freeze. Root personally read all98 graphical Edit failure-message groups and all43 Play groups; Local/full-play-failure-triage.md (70 lines) and critical-failure-triage.md (35 lines) are independently reviewed. Actual XML paths are Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (7c6d24e1...) and21-full-play-graphics.xml (2b152164...). Read your exact cases/messages/stacks and actual runtime/vendor APIs before editing. CENTRAL RESERVATION: primary Neo.Editor.Tests b03-04 alone owns ALL Editor-only native helper moves/extractions, new Neo.Tests.Fixtures, shared test asmdefs, and its owned legacy test adaptations. Do NOT move/extract Mono helpers, edit shared asmdefs, or race any file in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. If the map is not yet present, work only the explicitly disjoint runtime/Play/new helper-free fixtures below; coordinate any caller containing embedded helpers before changing it. Preserve every substantive positive/negative/count/payload/ownership oracle and all public/serialized APIs/settings. Do not make production changes to hide a fixture precondition, add ExecuteAlways, blanket Ignore/error whitelists, manual Update/mocks as native evidence, reflection into private UserCode as receive proof, or weaker comparisons without actual native-baseline/numeric evidence. No other owners/vendor assets/314 migration targets, no children/commits/push; release Samples~ and protected DocsEn.meta untouched. Update own honest implementation receipt and canonical docs only if behavior/contract actually changes; report unresolved native capabilities explicitly. Stop all source writes when done. Three own Play Network/SpawnerNetworkTests fail RPC SendRPCInternal415 because PrepareHostSpawner177–179 adds Spawner after active identity Awake. Build complete configured identity+Spawner graph inactive, activate once, assert actual cached netIdentity and spawn barrier before API use. Preserve native host wave/object counts, pooling first/reused netIds and both registration maps, retired outstanding rental late-return release-before-unregister and one-shot cleanup. Do not patch production RPC/identity behavior to bypass bad fixture composition, call internal Awake twice, or directly write identity caches.
