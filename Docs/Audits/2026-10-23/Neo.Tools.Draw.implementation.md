# Neo.Tools.Draw implementation receipt

Status: **SOURCE-FINAL for this owner**. All three assigned consumer corrections, owned public documentation and regression sources are complete. This is not global source freeze or an execution pass. Branch observed `demo`, target `11.0.0`, actor `neo-audit-1023-b06-08`; resumed after the Windows interruption on 2026-10-24.

## API and dependency notice

No new public Draw API was added in this implementation phase. Existing serialized fields, public methods, events, `InputCamera`, and `TryScreenToWorld` remain available. No asmdef or version-specific ID API changes.

Spawner's provider source and source-final receipt are now present. Draw consumes `SpawnUtility.RetirePool(GameObject) : bool` for only its unique automatic source and `PooledObjectInfo.TryRegisterReleaseCleanup(NeoObjectPool, int, Action<GameObject>) : bool` for each acquired pooled renderer. Captured `OwnerPool` plus `SpawnGeneration` guard every later Draw instance mutation/return. There is no remaining provider API blocker, and Draw edits no Spawner source or storage root.

## Completed actions and exact files

| Action | Implementation in `Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs` |
|---|---|
| `DRAW-RESET-REENTRY` / C-22 | `ResetPointsCore` clears raw/rendered geometry and collision, silently commits both zero metrics, then publishes changed Distance followed by PointCount. A captured stroke version stops superseded notifications. `BeginLine` checks its renderer/version after reset, first-point and timer callbacks. `Timer` suppresses stale remaining-percent delivery after replacement. Bulk deletion captures old resource records and preserves a listener-created stroke. |
| `DRAW-AUTOMATIC-POOL-RETENTION` / C-23 | `OnDestroy` blocks new drawing, deletes tracked strokes and standalone acquisitions, retires only `_autoPoolPrefab` through the unified API, then destroys that source. It never retires assigned prefab pools or removes a storage root. Provider retirement preserves outstanding external rentals until valid late return. |
| `DRAW-EXTERNAL-MATERIAL-LEASE` / C-24 | Private `LineResources` records capture metadata, exact owner/generation and exact owned/original materials. `CanAccessLine` guards input/follow, reset, append, end, renderer/collider reads/writes, public style updates, deletion and expiry. The registered generation cleanup restores only its own installed clone, frees that clone once, removes expired tracking and cancels expiry. Delete captures a resource record before reactive callbacks and rechecks it before return. Cleanup runs with IsInPool=true, outside cached IPoolable discovery, without requiring an active-rental predicate. |

Other files changed in this phase:

- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawTests.cs`: 16 additional acceptance cases plus fixture cleanup improvements, detailed below. Existing `.meta` retained (`feabf632531c426599b1afadcab7ce5a`); no new fixture/asmdef needed.
- `Assets/Neoxider/Docs/Tools/Draw/Drawer.md`: C#/Inspector reset/start event order, exact-generation material cleanup, external reuse, automatic retirement, shared containers, cleanup-slot fallback, silent external active-stroke reset and Russian summary.
- `Assets/Neoxider/Docs/Tools/Draw/README.md`: canonical entry links to the guide and Spawner metadata lifecycle contract.
- `Docs/Audits/2026-10-23/Neo.Tools.Draw.implementation.md`: this receipt.

The original audit/plan remain unchanged historical artifacts. Prior coherent geometry/camera/material/collider/timer corrections were retained. Optional bounded stroke-builder/buffer and generic registration/lease frameworks remain proposals; no such framework was introduced.

## Compatibility and deliberate behavior

Serialized names/defaults, `colour` migration, public declarations and ordinary completion behavior are preserved. Reset observers now see cleared geometry and both zero metrics together; superseded Begin/timer work does not overwrite or announce a replacement. Append still publishes its committed PointCount then Distance; Reset explicitly publishes Distance then PointCount when changed.

Pooled Delete/style calls require that Drawer's captured current rental. They ignore unknown/expired pooled renderers; independently acquired rentals must use the pool API. Actual captured ownership governs return even after `usePooling` changes. External return can safely precede another Drawer's acquisition: old Delete/OnDestroy/style/geometry operations cannot affect the new generation. An external return of the active stroke silently clears local raw points/metrics without user callbacks inside pool transition; explicit Draw deletion retains reset notifications. Cleanup follows existing IPoolable/Mirror release observers, restores borrowed material only if the exact installed clone is still assigned, and preserves a caller's replacement material.

The provider offers one cleanup slot per outstanding generation. If occupied, Drawer returns the untouched rental and uses ordinary renderer creation. Lifecycle callbacks/hooks keep the provider's nonthrowing, non-reentrant transitioning-instance contract. Automatic retirement is terminal for only the exclusively owned source; assigned/shared pools and even empty shared containers remain available. No asset, prefab or scene rewrite is required.

## Authored acceptance — UNEXECUTED

The existing prefixed fixture now contains **39 authored cases**: 34 synchronous cases and five UnityTest cases that explicitly enter Play Mode. This is a source count, not a discovery/result claim. The 16 added cases are:

- One reset snapshot/order case checks both metrics, renderer positions and disabled collider inside the callbacks.
- Four reset replacements cover Distance/PointCount listeners through explicit Reset and outer Begin; assert the actual new first rendered/raw point, count=1, distance=0, and absence of a stale start event.
- Four startup replacements cover first-point, timer, remaining-percent and started callbacks; assert replacement geometry, metrics, timer=0.5 and emitted start positions.
- One DeleteAll reset-listener replacement checks retained actual geometry/metrics.
- Two two-Drawer cases cover active/completed external return, material restoration/freeing before reissue, then old append/reset/collider/end/style/Delete/DeleteAll and full old-GameObject destruction. Assert the new rental's owner state, parent, activity, geometry, width, collider and material remain intact.
- One Delete reset-listener case returns/reissues the same renderer during the callback and checks the new rental survives the outer Delete.
- One capacity-one case uses metadata Return and direct owner ReturnObject: exact clones freed on retention/eviction, borrowed material alive/restored, evicted native object gone, and stale Drawer Delete cannot return an independently reissued instance.
- One real-Play source case repeats **100 full owner-GameObject disposals**, checking retired pool state, rejected retrieval, zero inactive objects, removed exact registration, destroyed own material/source and native pooled-object/source counts at baseline. A same-named assigned/shared pool, its active line/material and further retrieval survive.
- One real-Play source case destroys the automatic-source owner while an external rental remains outstanding, checks it stays active/material intact, then checks valid late return destroys that rental while preserving borrowed material.

Fixture teardown now cleans every created Drawer before its pools, preserves/restores prior PoolManager singleton/search state, and assigns an explicit camera before Awake without tagging MainCamera. Reflection only configures fixture pool retention, preserves singleton state and captures the otherwise-private automatic source identity; assertions observe public/native behavior. Isolated/exclusive execution is still required. No unavailable NonParallelizable attribute was added. Existing unrelated legacy fixtures were not edited.

## Actual evidence and remaining verification debt

Performed current-source reads, method/reference enumeration and scoped Git diff/status inspection; observed branch `demo`. Reviewed the complete single-file owned inventory and the changed provider signatures/release ordering. All modifications are confined to the five owned paths above; historical audit/plan and existing test metadata were retained. No compiler, Unity/MCP, test runner, product checker/validator, install, service, commit/push or child actor was started. No historical pass was rewritten to apply to this source.

**Remaining source dependency: none.** Spawner's source-final provider API is present. Coordinator must retain that provider together with this consumer, then release execution only after every source writer finishes.

After the barrier: compile/discover/run the current fixture and legacy Drawer fixture on installed Unity 6.3/6.6; execute native material/eviction, full-hierarchy disposal, 100-cycle/shared-container and outstanding-rental cases. Validate cleanup-slot fallback, disabled-domain-reload isolation, real physics/live follow, orthographic/parallel camera paths and player shader/material availability. Full scene unload and coroutine-owner disable remain explicit execution debt; full GameObject regression source is not evidence of scene-unload safety. Retained physical counts do not claim measured managed-memory/FPS results. Package/docs/UTF-8/meta/GUID validation remains with root after source freeze.

## Snapshot3 correction: DRAW-NATIVE-LEASE-AND-PLANE — SOURCE-FINAL

Read the renewed implementation contract/request, exact E34/E35/E36 action-index entries, the seven assigned XML failures and the current fixture/runtime boundary. Read the relocation reservation map before every edit, including all nested Draw references: none of the owned fixture/receipt paths is reserved. Primary b03-04's latest map status was source-final. No helper extraction/move, reserved caller, Fixtures assembly, shared reference or tracker was changed. Branch observed `demo`; target remains 11.0.0.

### Historical native evidence, not results for the corrected source

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`, SHA-256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`: 39 Draw cases, 32 passed and seven failed in that snapshot. E34 fails the final outstanding-rental destruction assertion at old line799; E35 fails pool retirement in automatic-source cycle1 at old line745. The five E36 failures expect Vector3.one although the configured plane is Z=0.
- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml`, SHA-256 `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`: no Draw cases. This provides no additional Draw native pass evidence.

E34 reached owner/source retirement and outstanding-rental survival checks before failing final destruction. It does not demonstrate a source-registration leak. Neither old lifecycle case established native Update/frame admission before disposal; two null enumerator yields also did not explicitly assert two actual player frames. Runtime changes are not justified by those observations alone.

### Exact correction and preserved oracles

Changed only `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawTests.cs` and this receipt in the renewed source phase.

1. Corrected four original assertion sites covering five E36 cases to exact `(1,1,0)` for the off-plane `Vector3.one` input. `fixedZ`, input coordinates and runtime projection remain unchanged. Reentrant completion now additionally checks the original finished renderer identity, nonempty replacement renderer, raw/rendered first point and metrics. External/reentrant rental cases establish nonempty new geometry before old-owner operations and retain material identity, generation, activity, parent, width and collider controls. No tolerance was widened or ownership check removed.
2. Both automatic-source UnityTests retain top-level `EnterPlayMode`. Before disposal they require Application.isPlaying, advancing Time.frameCount, an active/enabled owner and at least one real automatic Timer notification. The listener is attached after BeginLine's explicit Timer assignment; the test never invokes Update/private lifecycle methods. Only the owned Drawer temporarily enables its existing automatic-input path. Initial two-point geometry, live source and nonretired pool are positive controls.
3. Owner disposal and the external late return now await two actual player frames, with a five-second real-time failure deadline. The deadline detects a stalled/unadmitted player loop; it does not relax any geometry, material, retention or destruction oracle. Owner destruction, exact retirement, zero inactive count, removed registration, destroyed source/clone and 100-cycle native baselines remain required. The same-named shared pool/material/retrieval controls remain intact.
4. The outstanding rental is explicitly the same renderer/metadata, with its captured external generation unchanged through owner disposal. Its valid late return must immediately mark it returned/inactive without retaining it, then physically destroy it after native frames while preserving borrowed material. Existing positive survival-before-return and negative creation-after-retirement checks remain.

All 39 case names/signatures and substantive positive/negative/count/material/ownership assertions remain; added assertions strengthen admission and state provenance. The existing top-level UnityTearDown/ExitPlayMode remains, with no nested yield helper. No production/public/serialized API, default, setting, GUID, foreign scene/provider/session, expected-error whitelist or Ignore was changed. Existing canonical documentation already describes fixed-Z projection and exact rental retirement; no public contract changed, so no guide rewrite was needed. Pure Edit API ownership cases are not promoted to native OnDestroy evidence merely because they destroy an object during cleanup.

### Handoff and unresolved evidence

**Source-final; no remaining source/API or reserved-file dependency for Draw.** Writes stop with this receipt. Actual work here was artifact/source reading and a direct text diff against the frozen snapshot fixture; no compiler, Unity, test runner or product checker was started. The original audit/plan and earlier source work were preserved.

After all writers finish and root declares a new freeze, rerun the seven assigned cases and all39 Draw cases on the requested native hosts. E34 late-return physical destruction and E35 all100 owner-disposal iterations remain unresolved native evidence until the new positive admission/frame controls and all original terminal oracles actually pass. If native admission fails, report that precondition explicitly before classifying a runtime defect. Full scene unload and old-owner native destruction across two Drawers remain distinct from the pure Edit API controls and are still native acceptance debt. No pass is inferred from these source corrections or the historical 32 passes.
