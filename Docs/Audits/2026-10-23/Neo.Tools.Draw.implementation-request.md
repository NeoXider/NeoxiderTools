# Neo.Tools.Draw: approved implementation request

Original actor: `neo-audit-1023-b06-08`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### DRAW-AUTOMATIC-POOL-RETENTION (P2; approved-source-correction)

Drawer destroys automatic source but PoolManager retains source-keyed pool/clones/closure. Assess smallest safe retirement of own generated source pool without destroying shared active leases.

Root decision: Retire only exact unique automatic source registration, clear own inactive, no new creation; outstanding rentals destroy on valid return. Preserve shared assigned pools and potentially shared prefab-name storage roots; generic refcount framework optional.

### DRAW-EXTERNAL-MATERIAL-LEASE (P2; approved-source-correction)

External pool return/reacquire can leave old Drawer material record that later destroys material used by new lease. Validate generation/ownership transfer on all return paths; no lifecycle claims from timer-only generation guard.

Root decision: Drawer guards all pooled accesses by captured owner+generation; existing PooledObjectInfo/NeoObjectPool one-shot generation cleanup before reuse/eviction restores borrowed material and frees old clone. No cached-IPoolable-only late-added callback or generic registry prerequisite.

### DRAW-RESET-REENTRY (P2; approved-source-correction)

Reset Distance0 callback begins new stroke then outer reset clears new geometry; silently stage old geometry/metrics before notify and guard generation after each callback including outer Begin.

## Relevant full native sections

Source: NativeReview-C.md

### C-22 / P2 — Drawer reset callbacks can erase a newly started stroke

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:286–301`, `ResetPoints`: Distance.Value=0 notifies before raw/rendered geometry is cleared. With a nonzero completed/active distance, a one-shot listener on zero calls BeginLine(Vector3.up); the external reset resumes and clears that new stroke and overwrites PointCount. Expected geometry and both metrics committed before notifications, then version check preserves replacement. Minimum existing SetValueWithoutNotify/ForceNotify and stroke-generation checks across Reset and Begin startup notifications, keeping metric order explicit; test actual first rendered point/count/distance rather than private generation. Owner **Neo.Tools.Draw / `neo-audit-1023-b06-08`**. Current Append guards do not protect Reset; plan independently records it.
Source: NativeReview-C.md

### Neo.Tools.Draw — reviewed

Full audit/plan (including both proposals), entire changed Drawer diff, affected reset/start/geometry/material/pool/deletion/projection bodies, complete twenty-three-case fixture and both guide diffs read. Material records track actual allocations, actual owner pool replaces mutable usePooling provenance, collider conversion/reuse and expiry generation guard are coherent within documented ownership. C-22 remains. Test fixture asserts native object/material/renderer/collider states and actual timed reuse; owns/destroys objects, clears created pools, and explicitly enters/exits Play. It resets PoolManager statics instead of preserving existing ones and uses a tagged camera; run isolated. It destroys only Drawer component in lifecycle case, so full hierarchy/scene teardown remains unverified. Do not equate that case with unload safety.

Aggregate: held input can project the same pointer ray twice per frame (mouse-down plus held branch) but uses analytic Plane.Raycast rather than Physics world query; cache identical projection locally if measuring need. Raw input and smoothing are unbounded before maxPoints reduction; 1000 points/10 doubling passes gives ~1,024,000 intermediates, a structural estimate, no measured FPS. Live follow copies renderer points/list/collider arrays each update. Optional bounded StrokeBuilder/reusable buffers directly address those sites while preserving unrestricted output by default. Automatic Drawer source pool remains registered after owner source destruction; confirmed retained pool/clone/closure references in Spawner require coordinated release API/policy, not an unsafe global Clear. External cross-Drawer returns still lack material ownership transfer; generation guards currently protect expiry only. Treat advertised interop expansion and lease API separately from required local Reset fix.

Full read fingerprints for Neo.Save:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Save.md` | `5c2a584a939db2fe9a102149548dbcaf75cdecd5f67279e259dbf1d802e5da13` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Save.plan.md` | `aefad16b73f7e133b8450eb0b6f8c71bb5ee7f3b89fd66b158bb3b75f4cb80cf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` | `a677c26b4ec381ff7f68be318801754d3d36b6bd4b0c40a6622d89193f8e05ba` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveManager.cs` | `0f892ab9c35b1d91e75dac5334f8c630691d422571b9d903b335cd81be18fc64` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Save/SaveProvider.cs` | `8dd4d03a753a09a8b86d9730763cc1c490217469accff2e4d424a375a29afb72` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs` | `94859d2e42de107807cd94fac62a23c6a3a3f38401af8b1f78c3b34e754dd568` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs` | `9d2ebafe0496dd2da10df6279a7091ca95fd4edff4e8a2854f49e7d40a140c35` |

Full read fingerprints for Neo.Tools.Draw:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Draw.md` | `27f58231f591c6370d30af4dd93c02b5996f4fb3da739c2ada148c1d33c685a7` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Draw.plan.md` | `b6570d3f0023c1ec134deb323bd1340cd3fbf613e1d061396f8fcbf3426f5982` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs` | `3f25cdefd8a55ca33c5e2c9bba2f2c4b4f44dafd5f89e00f49984a8bb008b456` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawTests.cs` | `b0f8467402a5c0d133c078f9b33633a5ca0b21da88329b9ef7a0536ce26b06bf` |
Source: NativeReview-C.md

### C-23 / P2 — automatic Drawer pool registrations outlive their unique destroyed source

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:409–430` creates unique automatic source; `OnDestroy:190–202` destroys it. `Tools/Spawner/PoolManager.cs:35,69–78` retains the source-keyed NeoObjectPool, whose `_prefab`/component cache/ObjectPool retain source/instances; Clear has no registration retirement. Repeat create/draw/return/destroy one hundred automatic-source Drawers: registrations/inactive clones remain until manager dies; the retained pool may later try Instantiate on dead source. Expected disposing this unique owner releases registration/retained resources without disturbing assigned/shared prefabs.

Bounded joint repair, not full lease framework: Spawner owner **`neo-audit-1023-b08-03`** provides exact-source retirement that removes registration, clears inactive objects, prevents further creation, and destroys any outstanding valid rental on late return. Draw owner **`neo-audit-1023-b06-08`** returns its current rentals then retires **only** its automatically created unique source, never assigned prefab pools. Preserve other pools/storage: current storage lookup is by prefab.name and can share a root; retirement must not assume root exclusivity. Test counts/resources return to baseline across repeated owner disposal, outstanding external rental late return, and a second assigned/shared pool survives. Reference-counted shared registrations/general policy API remain optional.
Source: NativeReview-C.md

### C-24 / P2 — old Drawer cleanup can change a reissued renderer and its material

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:680–741` (`DestroyLineMaterial`/`Delete`), OnDestroy and `_lineMaterials`. A creates pooled line/material, external caller returns it, B rents same renderer and installs its own material, then A.Delete/OnDestroy runs. Only delayed expiry captures generation; Delete returns B's active renderer, and A's material record can be a borrowed/restored material in B. Expected every old-owner operation leaves current rental activity/material/geometry intact.

Minimum capture `(OwnerPool, SpawnGeneration)` for every pooled acquired renderer and check before all instance mutations, not only timer. Add a one-shot generation-specific release/eviction cleanup on existing metadata/common pool release, **outside cached IPoolable discovery**, so a material owner attached after creation can restore/free its exact clone before reuse. Preserve established IPoolable/network callback context/order and cleanup once. This can be a small typed hook, no general registration/lease registry required. Tests: external return/reissue with two Drawers, old Delete and full OnDestroy, capacity eviction and no retained/destroyed borrowed material. Owners **Spawner `neo-audit-1023-b08-03` → Draw `neo-audit-1023-b06-08`**. Existing generation expiry test is useful but insufficient. Full arbitrary pool-callback rollback is not requested.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Draw.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Snapshot3 actual DRAW-NATIVE-LEASE-AND-PLANE

SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. Seven failures: automatic/external rental source retirement after owner disposal, and five exactVector3 one vs(1,1,0) expectations. Establish genuine owner disable/destroy callbacks and existing XY/Z draw-plane configuration. Keep lease/rental/reentrant stroke ownership, nonempty geometry and external outstanding late-return controls. Do not promise Z preservation contrary to configured plane or infer runtime leak from unadmitted Edit OnDestroy. Fix only demonstrated runtime or exact fixture setup.
