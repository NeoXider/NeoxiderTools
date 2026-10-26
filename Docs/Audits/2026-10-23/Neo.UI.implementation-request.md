# Neo.UI: approved implementation request

Original actor: `neo-audit-1023-b08-07`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### UI-RUNTIME-RIG-REPLACEMENT (P0; approved-source-correction)

Runtime ApplyToOwner default replacement deletes root GO/owned point branch containing inactive nested owner. Preflight whole set before any mutation/Destroy; reject safe with explicit exception, preserve ordinary replacement.

### UI-EDITOR-NAVIGATION (P3; approved-source-correction)

Link new UI/Editor.md from canonical UIMeshRig guide and reconcile runtime safety status after fix/validation.

### UI-PAUSE-GM-BASELINE (P1; approved-source-correction)

Page sets0 before GM.Pause captures, GM.Resume restores0 after page restores1. Capture true baseline, order own GM resume before final page restore.

### UI-PAUSE-GM-OWNERSHIP (P2; approved-source-correction)

Page unconditionally resumes previously paused/replacement GM. Track exact instance and actual self-owned Pause transition; release only own.

### UI-PAUSE-OVERLAP (P2; approved-source-correction)

A/B own snapshots1/0 close out of order causes early resume then final0. Shared first/last per-channel ownership even without optional coordinator; no required scene wiring.

## Relevant full native sections

Source: NativeReview-C.md

### C-19 / P1 — runtime mesh layout replacement can delete its renderer or a nested foreign rig

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs:230` (`ApplyToOwner`) invokes `RemoveExistingPoints:333–354` before creation. An owned point on the rig root causes whole renderer/root deletion; an owned child point containing an inactive nested IUIMeshRigOwner destroys the nested foreign rig, although its own points are skipped. Expected reject protected removal shape before any mutation. Minimum full preflight of all removed branches for root/nested owners using existing resolver, including inactive descendants, then ordinary replacement unchanged. Existing array-return API can report an explicit invalid-operation failure before destruction; new planner/Try API is optional. Add Edit and real Play/deferred-destroy assertions of both hierarchies, keys/mode/count and normal replacement. Owner **Neo.UI / `neo-audit-1023-b08-07`**; editor owner **`neo-audit-1023-b08-08`** already fixed and reported its own path. No extra authoring framework required.
Source: NativeReview-C.md

### C-20 / P1–P2 — PausePage and GM have conflicting snapshot ownership

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/PausePage.cs:54–88`, `OnEnable/OnDisable`; current `Tools/Managers/GM.cs:190–233`. Default page opens at scale1, writes0, GM.Pause then saves0; close page restores1, GM.Resume restores0. Expected a single ordinary pause returns original nonzero time and resume observers see it. Minimum capture/order reconciliation with the actual final GM adapter, preserving its corrected event-before/after state contracts. P2 same source unconditionally calls current GM.I.Resume even if that GM was already paused or a different instance replaced it: retain actual acquired GM and whether this page performed its transition; release only its own pause.

Separate confirmed overlapping-pages case: A/B capture1/0; closing A first resumes prematurely, closing B last restores0. Reordering local calls cannot fix arbitrary owners. Use existing shared pause owner/root as immediate documented policy or a scoped common ownership registry; a full cross-module lease/cursor platform is optional, not prerequisite for single-page/foreign-GM fixes. Owner **Neo.UI / `neo-audit-1023-b08-07`**, coordinate **Neo.Tools.Managers / `neo-audit-1023-b07-06`**, preserve serialized defaults and GM public methods. Test baselines1/0.35, existing GM pause, replacement manager and both closure orders when claiming overlap support. No runtime execution here.
Source: NativeReview-C.md

### Neo.UI — reviewed

Full audit/plan, all nine changed runtime source diffs, affected lifecycle/pool/request/page/category/point bodies, all five complete fixtures and all ten factual guide diffs read. Optional-page/static ownership, CanvasGroup/ToggleGroup gates, category list alias/listener ownership, shake depth, explicit Canvas camera, aspect-space point placement and direct point reparent invalidation are locally coherent. Public/serialized names preserved; KeepAlive ownership transfer is explicit. C-19/C-20 remain required bounded repairs. New tests observe actual states, identities, coordinates, events and pooled representations; hierarchy/assets cleaned, waits bounded. UI statics and singleton assumptions need isolated runtime execution; cached-scene-reference JSON setup and native atlas/mask/panel/raycast paths are unexecuted.

Aggregate work: active flight tween/spawn work and allocations scale with accepted visuals; pool lookup includes space and visuals return/destroy under owner. UIMeshRigElement geometry build creates point-profile/curve data, and geometry builder allocates output arrays; cache immutable templates/reuse caller-owned buffers only with output parity and measured allocation counts. Distinct deformed targets/geometry cannot be replaced with one target result. Actual UI Toolkit/uGUI picking is separate from the shared camera physics contexts in INTERACTION-QUERY-DESIGN. Whole-container owner-cache changes remain a specific hierarchy-policy risk, not claimed solved by point-only tests. Tracked cancellation/status and immutable delayed request plans are valuable optional API work; current destruction cleanup does not claim disable cancellation/terminal status.

Full read fingerprints for Neo.Audio.Editor:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Audio.Editor.md` | `4b8bfc7c9c2ac04be0f2273ab9b1c76c03a6b1c2dd4a8499e698d212de23531b` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Audio.Editor.plan.md` | `3c929e0bac96350190889281ef28e07ffe9bb2e25a8c6ac0b25b16f779577811` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/Editor/AMEditor.cs` | `0e9b745a4211e622946b1f61a2d1e5a0376b219521d0fb140efa331de7d69e68` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/Editor/AudioEntryDrawer.cs` | `75ce6e7578ffaded1251d3b3b8cf0143bc538a30f345d7447f3fe518f8b613f1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Audio/Editor/AudioIdDrawer.cs` | `216d306898626c5bfe381a927463b0d6ca400cb3df3aac2f63729af6fbb59354` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioEditorTests.cs` | `376fee4913c2cf3067150274953411832542a6e207fffc3fcae383ab294c89c0` |

Full read fingerprints for Neo.UI.Editor:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.UI.Editor.md` | `c7824349f80f216b2c9a47ff8a2611c38cdfb9f4abd27285eb706adc8ca819c4` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.UI.Editor.plan.md` | `1b5a7bc2c7f5a4379b1ab8afdd9e04e81855c66e6dff6bfb5648cf15cdd4f933` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Editor/UIMeshRigGraphicEditor.cs` | `7dbb76c825d69f7ffaa92aace0fd2456a28e29bdbcc596aef4d3aa9c344850f6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Editor/UIMeshRigMenu.cs` | `51c6645df2921e290027c81540e79bd1c515e72888407741b49975831fe30646` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIEditorAuthoringTests.cs` | `96970c15199cf31661df91698d3ee8c5042f2100d2cc3fa65c740bce1ca9e087` |

Full read fingerprints for Neo.UI:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.UI.md` | `4426be8ef1faf99ec7ce7b2759471e02cc57ea752bc622a02f83b50793dbe93c` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.UI.plan.md` | `9fcc3e88d5e7f525f26c012b59f6821e15873750e7466eaf36af99e63a5fd699` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Animation/ButtonShake.cs` | `f26b1ebc5a7a20dc5179eb9e949a9ee2df5d30cf0f60c6efb36d9eda26e49705` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/AnimationFly.cs` | `00c9f1500adf522760faf87ec6aba32bd28c767ebb6f6fbfedd0f0fa69625b0b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/CategoryBar/CategoryBar.cs` | `709d4178c8b9970b49c59ea65b1283bc8e4b4a76b9d3f376d18e7049e019bb43` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs` | `eeacda823c27dbe816ccac37842f7ba3545256e6b8c803fcf57e2bc5c2b3ddc5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigPoint.cs` | `b0bd28e898ef17fb29f4537d32978aeda9bd8e0a7dd08717906a93b05a06d945` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Simple/ButtonChangePage.cs` | `b8ee2115342d9c67533fe7e3a9d71c65b68d07d962f5b9785f8f651468b56205` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/Simple/UI.cs` | `52707c7c78f41a8ee6c804b420f47b4ff49b49783591f5779078365fb49bdbfe` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/View/VariantView.cs` | `850841fbcbd25317350c6e3cc30a7d40320103f529d2e04287d0016baec19f64` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/View/VisualToggle.cs` | `f61b0fbebe9921d8d120775201bd38f6082c47e4d8c09f4b1b34b2f97bfb2111` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIAnimationFlyTests.cs` | `9383a693f217b1a48bcecc90f8fc3fd521921cac67f1004522f183e614d438d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUILifecycleTests.cs` | `b5adad1ff7257405b28ba0bc122671975e37653aa2114955d8f06f57eef8fe39` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUICategoryBarTests.cs` | `5c833a3aab199444ac5c2572da8f4761c89c073e7cca74a281293d19ab101d34` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIMeshRigTests.cs` | `6e15676ff91e4e36b532ce18a1436cfb6113a79537e93182b5bbf6fd6200e8aa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIViewTests.cs` | `baf25db691488b9c0685d31265dda2aa747af948a6d3124e9f0b4e03b6d7dddd` |

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.UI.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## GM exact property published dependency ready

Provider-ready handoff: root read current Managers API receipt18lines and actual GM.cs getter/setter/Pause/Resume. public ulong TransitionVersion is published, exact unchecked(previous+1UL) before each effective state transition/callback, same-state unchanged. It is the same monotonic stale-broadcast fence. Finish your dependency receipt/sourcefinal against exact property now; preserve completed source/tests/docs, do NOT repeat patches or wait for Managers sourcefinal to acknowledge the published stable API. Inspect current exact API; narrow binding only if needed, no runtime rewrite. All execution still blocked. Root previously held your worker for unchanged dependency; this is the explicit provider-ready resume.

## Snapshot3 actual UI-NATIVE-CATEGORY-OWNERSHIP

SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. Two new CategoryBar destruction cases retain spawned child1 vs0; distinguish unadmitted Edit OnDestroy from true owned view disposal. Preserve external root/marker/sibling identity, exact own child removal and owner component removal vs whole object. Play PausePage CursorOwners308 nativeLocked->None requires focused native write/read control; preserve first/last owner/baseline/next-group assertions and coordinate MOVE, no weaker default or arbitraryNone acceptance. No UI.Editor/legacy primary edits.

## NI51-02 authored-child post-destruction native window

SAME original b08-07/session6.1sol/max. RootFULL-read NI51all68lines SHAb6ae5438... andlatest193linereceipt44593d96... . Confirmed NI51-02/P2 CategoryBarTests5f25d947...97-143: realbar-nullpositive followedimmediatelybysurvivornegativehasnogenuinenativecleanupwindowinEditor-assemblyUnityTest. FixONLYownCategoryBarTestssource/receipt: boundedactualframe/phaseorcompletionpredicatewaitsfornativeStart/ownerdestruction/oldviewand replacementdestruction; afterobservedbar-null at leastoneactualnativeframebefore authoredchildren,parent/identityandexactlistener/consumeroracles. Preservecomponent-onlydeletion,all5cases/2nativecases,selection0/consumer1, no-siblingandsibling2->1/exactmarkerreturnhidden/prefab/template/foreignsibling. Positiveclone/owner-nullchecksaremeaningfulandremain; nomorearbitrarynullpollcounts. Preserveownedonlycleanup/acquiredPlayexit,faultsnotpass. ExistingPausePagePlayassemblycursor13/matrix/focusLockedpositiveisunchanged; noblanketrewriteofnativePlayyieldnull. No runtime/public/serialized/defaults/GUIDs/helpers/sharedrefs/otherowners changes or framework/Skip/tolerances. Accuratecurrentreceipt/newhashes; nativeUNEXECUTED untilALLwriters/newfreeze. NoUnity/compiler/tests/checkers/processcontrol/install/children/commit/push. Stopsourceforindependentreview.
