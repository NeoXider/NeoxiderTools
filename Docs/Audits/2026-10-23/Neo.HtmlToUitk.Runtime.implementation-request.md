# Neo.HtmlToUitk.Runtime: approved implementation request

Original actor: `neo-audit-1023-b03-08`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### HTML-EDITOR-NAVIGATION (P3; approved-source-correction)

Link HtmlToUitkEditor.md from canonical HtmlToUitk.md.

### HTML-TEST-NAVIGATION (P3; approved-source-correction)

Link new HtmlToUitkTesting.md from canonical HtmlToUitk.md; refresh current test counts only after real final run.

### HTML-ANIMATOR-REPLACEMENT-REENTRY (P2; approved-source-correction)

Cancelled callback starts newer same element+animation ID run; outer Start appends competing older replacement. Per-key request generation, preserve unrelated runs/snapshot StopAll, exact style/event regression.

## Relevant full native sections

Source: NativeReview-C.md

### C-36 / P2 — replacement animation callback can create two owners for the same key

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Animation/UitkAnimator.cs:220–231` Start, Finish:343–360. Start fade A; one-shot Cancelled handler calls PlayManual(fade, same element, Off); external PlayManual replaces A. Finish removes A and callback starts C, then outer replacement checks only disposed and appends B. ActiveCount becomes two for one (element, animation ID), with concurrent style writers and restoration snapshots. Expected latest reentrant same-key request owns the run; unrelated element/ID starts remain unaffected.

Minimum key-scoped request generation/replacement guard before callback dispatch and before committing run, not a rescan/cancel loop that can recurse forever. Add real-panel one-shot cancellation-restart test observing one active run, correct eventual/restored styles and stopping latest run once. Existing cancellation cases cover StopAll/Dispose, not restart. Owner **Neo.HtmlToUitk.Runtime / `neo-audit-1023-b03-08`**. Strict metadata compiler/general execution framework is not prerequisite.
Source: NativeReview-C.md

### Neo.HtmlToUitk.Runtime — reviewed

Full audit/plan, all seven changed source diffs, animation finish/start/sample and router dispatch/focus/blur bodies, all four complete new fixtures and full factual canonical HtmlToUitk doc diff read. Exact finite JSON output/strict grammar, selector invalid-form rejection, generation-before/after external router dispatch, modal focus separation, fractional terminal timeline reuse/captured bases, snapshot cancellation and blur owned-style/RT finally cleanup are coherent. C-36 remains. Public/serialized names and centralized PanelCompatibility/BackdropBlur gates preserved; strict parser rejection intentionally changes malformed data admission only.

Fifty-nine prepared cases use pure public values, fake backend visible/focus/event outcomes, real EditorWindow scheduler and UIDocument/GPU ownership. Own windows/RTs/textures/docs disposed, GPU Null explicitly skipped; no skipped case counts as acceptance. Native filter/camera-render error restoration, true focusedElement/detach/disabled ConfigureRoot and both incompatible native branches remain unexecuted. History/source-check receipts predate these edits and prove no current pass. Blur EveryFrame allocates level/temporary chain lists and performs down/up blits per configured owner; scheduled animation updates are per distinct run, selector collection per play. Share immutable prepared tracks/parsed selectors only after mutation contract; concurrent roots keep separate style/run state. Separate blur owners cannot share one output blindly when camera/texture/size/settings differ. No redundant physics camera ray introduced. Strict frozen metadata compiler is optional additive design; use existing parser/timeline/selector rather than a second stack.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.HtmlToUitk.Runtime.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Snapshot3 actual HTMLRUNTIME-NATIVE-BLUR-AND-DIAGNOSTICS

ROOT has closed source3 execution after every owned native job stopped. SAME original actor only, gpt-6.1-sol/max. No Unity/compiler/checkers/tests until ALL source writers finish and root opens a NEW freeze. Root personally read all98 graphical Edit failure-message groups and all43 Play groups; Local/full-play-failure-triage.md (70 lines) and critical-failure-triage.md (35 lines) are independently reviewed. Actual XML paths are Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (7c6d24e1...) and21-full-play-graphics.xml (2b152164...). Read your exact cases/messages/stacks and actual runtime/vendor APIs before editing. CENTRAL RESERVATION: primary Neo.Editor.Tests b03-04 alone owns ALL Editor-only native helper moves/extractions, new Neo.Tests.Fixtures, shared test asmdefs, and its owned legacy test adaptations. Do NOT move/extract Mono helpers, edit shared asmdefs, or race any file in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. If the map is not yet present, work only the explicitly disjoint runtime/Play/new helper-free fixtures below; coordinate any caller containing embedded helpers before changing it. Preserve every substantive positive/negative/count/payload/ownership oracle and all public/serialized APIs/settings. Do not make production changes to hide a fixture precondition, add ExecuteAlways, blanket Ignore/error whitelists, manual Update/mocks as native evidence, reflection into private UserCode as receive proof, or weaker comparisons without actual native-baseline/numeric evidence. No other owners/vendor assets/314 migration targets, no children/commits/push; release Samples~ and protected DocsEn.meta untouched. Update own honest implementation receipt and canonical docs only if behavior/contract actually changes; report unresolved native capabilities explicitly. Stop all source writes when done. Actual graphical Edit RuntimeBlurTests.FallbackRetargetAndRelease101 expects same RenderTexture but reads null. Inspect native UITK StyleBackground texture vs renderTexture representation and original baseline, retarget/release ownership. Correct actual runtime fault or precise typed fixture getter without losing fallback output reuse, other-user-background restoration and resource release assertions. ModulePrinciples reports raw Debug logs in owned UitkAnimator/UitkScreen/BreakpointWatcher. Use actual existing gated/debug-only diagnostic contract, minimal dependency if required, no raw unbounded runtime logging or source-text disguise. Source style rule also flags public root in UitkActionBindings; preserve public API/wire/serialized compatibility, inspect whether this is a true new API or a contextual scanner false positive before renaming. Primary owns principle-test infrastructure; coordinate exact justified legacy/DTO exception rather than weakening policy. No CLI/PureLogic/compiler/suite execution; report which .NET subgraph inputs truly changed so root can reuse or rerun appropriately.

## NI39 preserve first Release cleanup oracle

SAME original HtmlRuntime actor, gpt-6.1-sol/max. Independent native B NI39 source review found one exact oracle regression in your completed S3 fixture: RuntimeBlurTests123–129 calls Release TWICE before restoration/output-release assertions; frozen3 asserted those immediately after ONE Release. Correct only test ordering: immediately after FIRST Release assert second target image/size restored, secondOutput released, Output null and IsUsingNative false, retaining all original resource/identity/background oracles. THEN second Release and retain/repeat stable-state assertions to prove idempotence separately. Do not rely on a second call repairing incomplete first cleanup; no runtime bug or new framework/fixture implied. Retain your typed renderTexture fix/gated logs/API compatibility work and all other cases. Only owned blur fixture and own honest receipt; no source-helper moves/shared refs/314targets. No Unity/compiler/tests/checkers while other source writers work. Update source-final receipt with NI39 correction and stop; native acceptance awaits next freeze. Root already accepts the bounded reviewer finding; no user approval needed.
