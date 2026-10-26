# Neo.UI.Editor: approved implementation request

Original actor: `neo-audit-1023-b08-08`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Expose minimal addressed preview stop seam for primary fixture if existing path inaccessible. Never StopAllPreviews for test isolation. Parent UIMeshRig canonical backlink belongs UI actor.

## Assigned review actions

### UI-EDITOR-NAVIGATION (P3; approved-source-correction)

Link new UI/Editor.md from canonical UIMeshRig guide and reconcile runtime safety status after fix/validation.

### EDITOR-TEST-REMAINING-ISOLATION (P1; approved-source-correction)

Native B confirms3 Shop fixtures still use real provider, fixed prefs/timer keys and global repaint/StopAllPreviews shared state. Reuse exact-provider/unique-key/owned-preview scopes, preserve assertions; await exact final report owner mapping.

## Relevant full native sections

Source: NativeReview-B.md

### B-20 / P2 — editor tests modify other clients' animation/preview lifetimes

Owner Neo.Editor.Tests / `neo-audit-1023-b03-04`; seams owned by Neo.Editor `neo-audit-1023-b03-03` and Neo.UI.Editor `neo-audit-1023-b08-08`. Current `Assets/Neoxider/Tests/Edit/PropertyAttribute/NeoInspectorAnimationTests.cs:188`, EnsureRepaint_WhileAnimationIsOn_JoinsUntilTheLeaseLapses, ticks the shared list at current time +60. Trigger: a pre-existing Inspector client has a normal live lease. Expected: only the fixture client's lease expires; actual: other leases expire, global timestamps advance, and asserted baseline count fails. `UI/UIMeshRigEditorTests.cs:455,472,488` StopAllPreviews similarly stops every borrowed preview to test one owned motion. Minimal repair: isolated/per-client lease predicate plus exact shared-state restoration where integration is necessary; address the owned preview via existing target stop path. Add a live foreign-client/foreign-preview sentinel. Broad editor-state reset is not isolation and is not required by the correction.
Source: NativeReview-C.md

### C-19 / P1 — runtime mesh layout replacement can delete its renderer or a nested foreign rig

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs:230` (`ApplyToOwner`) invokes `RemoveExistingPoints:333–354` before creation. An owned point on the rig root causes whole renderer/root deletion; an owned child point containing an inactive nested IUIMeshRigOwner destroys the nested foreign rig, although its own points are skipped. Expected reject protected removal shape before any mutation. Minimum full preflight of all removed branches for root/nested owners using existing resolver, including inactive descendants, then ordinary replacement unchanged. Existing array-return API can report an explicit invalid-operation failure before destruction; new planner/Try API is optional. Add Edit and real Play/deferred-destroy assertions of both hierarchies, keys/mode/count and normal replacement. Owner **Neo.UI / `neo-audit-1023-b08-07`**; editor owner **`neo-audit-1023-b08-08`** already fixed and reported its own path. No extra authoring framework required.
Source: NativeReview-C.md

### Neo.UI.Editor — reviewed

Full audit/plan, both changed source diffs, all twenty-one cases and new guide read. Root/nested-owner preflight before Undo/deletion, source-parent duplication, owner-scoped key allocation, final mode Undo record and Selectable prefab recording are coherent. Fixtures assert hierarchy/rest/pose/keys, actual Undo and prefab target property modifications, restore Selection and delete unique temporary prefab assets. Redo, saved-scene/prefab reconstruction and isolated Undo grouping remain unverified; tests observe overrides, not a completed disk round trip. No additional confirmed editor regression. Runtime C-19 is distinct and still open; docs correctly avoid promising Inspector guards on runtime calls. Optional immutable authoring plans/stable diagnostics are not prerequisite. Canonical UIMeshRig entry needs Editor.md link. Search/allocations occur only explicit authoring/preview/repaint, not shared player camera rays.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.UI.Editor.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Runtime rig guards canonical editor docs integration

Root read full final-independent UI receipt. Runtime UIMeshRigLayoutBuilder now also preflights root-point and nested foreign-owner replacement rejection across all3adapters, before mutation. In owned Docs/UI/Editor.md final C# path paragraph, replace stale statement that runtime builder lacks Inspector safety guards: describe shared rejection policy and editor-only Undo/mode/preview differences. Preserve runtime noUndo and disposable non-rig branch limitation; do not edit runtime-owned source/docs. Continue all original approved own source/tests/docs work, exact addressed StopPreview seam for primaryfixturealreadyconsumed. No execution.

## Snapshot3 actual UIEDITOR-NATIVE-NESTED-POSE

ROOT has closed source3 execution after every owned native job stopped. SAME original actor only, gpt-6.1-sol/max. No Unity/compiler/checkers/tests until ALL source writers finish and root opens a NEW freeze. Root personally read all98 graphical Edit failure-message groups and all43 Play groups; Local/full-play-failure-triage.md (70 lines) and critical-failure-triage.md (35 lines) are independently reviewed. Actual XML paths are Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (7c6d24e1...) and21-full-play-graphics.xml (2b152164...). Read your exact cases/messages/stacks and actual runtime/vendor APIs before editing. CENTRAL RESERVATION: primary Neo.Editor.Tests b03-04 alone owns ALL Editor-only native helper moves/extractions, new Neo.Tests.Fixtures, shared test asmdefs, and its owned legacy test adaptations. Do NOT move/extract Mono helpers, edit shared asmdefs, or race any file in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. If the map is not yet present, work only the explicitly disjoint runtime/Play/new helper-free fixtures below; coordinate any caller containing embedded helpers before changing it. Preserve every substantive positive/negative/count/payload/ownership oracle and all public/serialized APIs/settings. Do not make production changes to hide a fixture precondition, add ExecuteAlways, blanket Ignore/error whitelists, manual Update/mocks as native evidence, reflection into private UserCode as receive proof, or weaker comparisons without actual native-baseline/numeric evidence. No other owners/vendor assets/314 migration targets, no children/commits/push; release Samples~ and protected DocsEn.meta untouched. Update own honest implementation receipt and canonical docs only if behavior/contract actually changes; report unresolved native capabilities explicitly. Stop all source writes when done. Three own new UIEditorAuthoring DuplicateNestedPoint_PreservesParentPoseAndRestPose cases fail native position/rest comparison74 (actual4.7759/16.6385). Read actual duplicate/parent/world/local/rest conversion and demonstrate root cause. Fix bounded editor duplication bug or exact native baseline arrangement, preserving parent identity/position/rotation/scale, local/world/rest pose and Undo/serialized references. No relaxed positional tolerance hiding multi-unit drift. Legacy UIMeshRigEditorTests belongs PRIMARY (raycastPadding expected4 but actual3.87 likely native clamp); do not edit it/shared asmdefs or move helpers.
