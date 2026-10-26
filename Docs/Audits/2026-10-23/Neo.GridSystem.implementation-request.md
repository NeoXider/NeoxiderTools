# Neo.GridSystem: approved implementation request

Original actor: `neo-audit-1023-b03-05`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### GRID-COORDINATE-OVERFLOW (P1; approved-source-correction)

Both FieldGenerator and GridPlacementService add anchor+offset in int and can wrap int.MinValue twice onto valid zero; validate all coordinates widened before any write.

### GRID-MERGE-OVERFLOW (P1; approved-source-correction)

GridMergeRequest.Increment and fallback result calculator add int unchecked; saturate signed range while preserving ordinary and Dice behavior.

### GRID-STALE-OCCUPANCY (P1; approved-source-correction)

Removing old spawned object after regeneration frees new occupant by coordinate; spawn callback can regenerate before occupancy write. Guard live-cell identity and retire detached metadata preserving object/event semantics.

### GRID-GIZMO-STYLE-ALLOCATION (P2; approved-source-correction)

B Grid disposition: new identical GUIStyle per coordinate creates10000styles for100x100 repaint. Hoist one style per draw invocation, output unchanged.

## Relevant full native sections

Source: NativeReview-B.md

### B-05 / P2 — footprint coordinate addition can wrap an invalid placement into the board

Owner: Neo.GridSystem / `neo-audit-1023-b03-05`; independently confirms the Dice handoff. Current `Assets/Neoxider/Scripts/GridSystem/FieldGenerator.cs:853` and `GridPlacementService.cs:155` add `Vector3Int` coordinates before validation. Trigger: one-cell board, anchor.x and entry.Offset.x both int.MinValue, other coordinates zero. Expected: reject out-of-range footprint with no writes/events. Actual source arithmetic resolves x=0 and can place in the origin. Smallest correction: one assembly-owned checked/widened coordinate helper used by both validation paths before cell lookup; keep valid negative offsets. Add public-path and Dice-through-placement regressions.
Source: NativeReview-B.md

### B-06 / P2 — direct generic grid merge still wraps valid integer content

Owner: Neo.GridSystem / `neo-audit-1023-b03-05`. Current `Merge/GridMergeRequest.cs:66` factory and `Merge/GridMergeResolver.cs:41` fallback still use `value + step` / `value + 1`. Trigger: resolve an int.MaxValue group through these generic entry points. Expected: documented representable result or explicit rejected operation; actual unchecked addition yields an unrelated negative ID. Dice's new custom lambda is protected and must not be changed again. Smallest correction: one widened increment policy for factory and fallback, with both integer endpoints and normal/cascade regressions; choose and document saturation/rejection before mutation.
Source: NativeReview-B.md

### B-07 / P1 — stale object removal changes a replacement generation's occupancy

Owner: Neo.GridSystem / `neo-audit-1023-b03-05` (plan P1 independently confirmed). `Assets/Neoxider/Scripts/GridSystem/FieldObjectSpawner.cs`, `SpawnAt`/`RemoveObject`, retains FieldCell keys but calls `generator.SetOccupied(cell.Position, ...)` on the current board. Trigger: spawn A, regenerate, spawn B at the same coordinate, remove A. Expected: B stays occupied/tracked; actual the current coordinate is freed using old metadata. OnObjectSpawned/OnObjectRemoved callbacks can also regenerate before later occupancy writes. Smallest repair: validate current cell identity and registration after callbacks and before writes, retiring detached metadata without touching replacement cells; preserve chosen object lifetime. Full lease/core redesign is optional. Verify replacement-cell state and absence of an incorrect freed/occupied event.
Source: NativeReview-B.md

### Neo.GridSystem — full artifacts, all 15 source diffs and complete 38-case fixture reviewed

Owner/task `neo-audit-1023-b03-05`. Generated-size lookup/projection/iterator lifetime, committed-state notification order, disabled seed opt-in, directed swaps, rollback, blocker policy and lazy spawner guards match the current implementation. Tests inspect actual paths/reasons/cells/event snapshots and restore Random state/owned objects. Odin import is still compile-only debt. B-05–B-07 remain minimal corrections distinct from a full board/lease proposal. Reentrant mutable request behavior and external pool ownership must retain explicit policy; arbitrary direct FieldCell writes cannot be made observable automatically.

Aggregate review: core query/merge/match work is called on request; there is no per-target camera physics Update path. Repeated reverse Match3 swaps can be optimized only when direction policy makes them equivalent. G15's duplicate Gizmos axis loops are actually removed. `FieldGenerator.cs` still creates a new identical GUIStyle per drawn coordinate inside the cell loop (around line 934): a 100x100 coordinate repaint creates 10,000 styles. Hoist one style per draw invocation as a focused allocation improvement; FPS/native-memory impact is unmeasured. Cells2D and path/merge snapshots intentionally allocate caller-owned collections; do not introduce a global result cache that hides board revisions or custom predicates. Data-only board/lease APIs remain staged future proposals and must not delay identity/arithmetic corrections.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.GridSystem.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root accepted NI20 nested first blocker publication

ROOT FULL-read all238lines NI20, full84line ownreceipt, accepts NI20-01/P2 existingB07residual. SAME original Grid owner: two valid nested blocking SpawnAt at same initiallyfree cell currentlyeachseesotherregisteredblocker andbothskipoccupiedpublication. Distinguish registeredinflight vs actuallypublished/currentoccupied soexactcurrentcell settles occupied=true withonefirsttransition. Preserveoriginalregister/spawncallback/eventorder, generation/registration/removalcallbackguards, one lastfree, decorativeobjects, multipleobjectspercell/ordinarymanualspawn policy. No newlease/framework/externaloccupancyownership. Real one-shot nestedSpawnAt sourcefixture checks actualFieldCell.IsOccupied plusregistry/query, RequireUnoccupiedplacement rejection, exactevent/order/count, bothremovalorders and ordinarysequential/mixedcontrols. All7previousPlay+39boundarycases retained. SharedPlay Neo.GridSystem ref alreadyrootpresent, doNOTwait/editconfig. Updateownspawnerguide+receipt. Noexecution/checkers/compiler/tests untilglobalfreeze.

## Root found nested published blocker removal duplicate

ROOT FULL-read new92line receipt1211ac27... andactualFieldObjectSpawner82–153 after yourNI20fix. Narrow confirmednewrepeat: initiallyfreecell outerA registers; OnObjectSpawned(A) doesB=SpawnAt(samecell,blocking), Bnowpublishesoccupiedtrue+OnCellOccupied1; sameoutercallbackRemoveObject(B), Aregistrationkeepscelloccupied; outerAreturns withcelltrue/nootherblocker, ORcondition127 !HasBlockingObjects becomestrue andOnCellOccupied132 firesAGAIN thoughcellneverfreed. Needboundedfix distinguishentry/publishedstate sufficiently so nestedpublishedthenremovedblocker cannotduplicateouteroccupied. Preservecurrentregistration/generationguards andordinarymanualadmission/firstlast, including previouslypreoccupiedcelllegacyfirstregistrationnotificationifretained byexistingpolicy. An initialoccupied snapshot combinedwithcurrentoccupied guard may suffice for THISnewregression; inspectcallorder, no newregistry/framework/externalownership. Addrealone-shotnestedspawn-then-remove nativecase: exactactualcelltrue, oneoccupiedtransition/event, onecurrentA, Bdeferreddestroy, placementreject, lastAremoveonefree; ordinarysequential/initiallyoccupiedpositivepolicyexplicit. Existing6new+7prior+39boundarycases retained. Ownruntime/fixture/guide/receiptonly, noexecution untilfreeze.
