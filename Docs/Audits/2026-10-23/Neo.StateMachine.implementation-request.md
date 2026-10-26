# Neo.StateMachine: approved implementation request

Original actor: `neo-audit-1023-b06-02`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### STATE-SHARED-DURATION (P2; approved-source-correction)

Shared asset duration predicate stores one enter timestamp; B resets A wait. Minimal per-machine timing context preserving standalone/custom predicate API.

### STATE-LIFECYCLE-REENTRY (P1; approved-source-correction)

OnExit ChangeState recursively exits same current state; enter→disable yields stale/null payloads; same-instance A→B→A bypasses reference guard. Bounded lifecycle dispatch and monotonic state generation with immutable payloads.

### STATE-EDITOR-NAVIGATION (P3; approved-source-correction)

Link StateMachine/Editor.md from canonical module entry; runtime scope remains outside nested Editor guide.

## Relevant full native sections

Source: NativeReview-C.md

### C-16 / P1 — reentrant state change from OnExit recurses on the same current state

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachine.cs:128–156` (`ChangeState`): A.OnExit calls machine.ChangeState(C) while A is still CurrentState at 148, so A.OnExit repeats without a bound and can overflow the stack. Expected one exit per committed departure and coherent ordered events. A safe regression requests once and counts duplicate exits; do not intentionally crash the runner. Minimum bounded reentrant command dispatcher for ChangeState/Stop, immutable payload locals and finally guard release, retaining synchronous normal calls and normal Exit(A)/exited(A)/Enter(B)/entered(B)/Changed(A,B) order. Nested requests drain after current dispatch with a documented cap/gated diagnostic, no unbounded recursion or arbitrary rollback promise. Owner **Neo.StateMachine / `neo-audit-1023-b06-02`**. This is a confirmed defect, not optional architecture despite audit proposal B labeling.
Source: NativeReview-C.md

### C-17 / P2 — shared duration predicates reset another machine's wait

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs:159–165` registers shared transitions; `StateMachine.cs:170–174` notifies predicates with entry time; `StatePredicate.cs:395–424` stores one timestamp. A enters at t0, B enters same configured transition/predicate at t4; A evaluated at t5 with duration five now waits until t9. Same defect occurs in shared And/Or/Not children. Expected independent per-machine elapsed time for reusable authoring data.

Smallest correction: machine-owned scoped entry/evaluation-time context used by built-in duration in the **default** runtime and wrapper paths, preserving legacy virtual Evaluate(IState) dispatch, inversion once and nested/finally scopes. No graph cloning, global custom-state isolation claim or timer service prerequisite. New options/frame API as legacy-default opt-in does not close the defect. Explicit compatibility decision is necessary: existing `StateDurationPredicateTests.SetEnterTimeInThePast_SatisfiesTheThreshold` intentionally edits the globally shared predicate after machine entry. Keep standalone SetEnterTime behavior; provide a clearly named legacy escape or an explicit addressed machine-anchor API/migration for this machine-global edit, and test both contracts. Do not silently change custom override dispatch or claim deep immutable assets. Owners **runtime `neo-audit-1023-b06-02`**, editor `neo-audit-1023-b06-03` only if authoring exposes a compatibility option. Acceptance: two default machines/fake clock, shared composites, re-entry, legacy standalone/manual mode, nested exception cleanup.
Source: NativeReview-C.md

### C-18 / P2 — state lifecycle callbacks can publish null/stale payloads or revive stale transitions

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/StateMachine/StateMachine.cs:152–156`: B.OnEnter disables its owner, wrapper OnDisable calls Stop, CurrentState becomes null, then outer OnStateEntered receives null and OnStateChanged receives (A,null), losing B's coherent lifecycle. `Stop:397–409` also reads mutable PreviousState after callbacks. Separate repro `EvaluateTransitions:337–355`: predicate/listener changes A→B→same cached A; reference guard passes and stale A→C applies. `CanTransitionTo/TryChangeState:184–222` also evaluates callbacks without generation reconciliation.

Expected newest explicit choice retained, events name the state actually entered/exited, one terminal stop and no stale transition across a departure/re-entry. Reuse C-16 dispatcher plus monotonic state generation and captured event arguments; check generation after callback-driven condition/evaluation paths, including addressed Try paths. Test enter-disable ordered B then stop, A→B→A stale-cancel, repeated Stop and bounded cyclic requests. Owner **Neo.StateMachine / `neo-audit-1023-b06-02`**. Shared global log gate is a separate P3 composition limitation, not prerequisite.
Source: NativeReview-C.md

### Neo.StateMachine — reviewed

Full audit/plan, all seven changed runtime diffs, core lifecycle/evaluation/anchoring/composite bodies, all four complete new test/helper sources, existing duration tests and all eight factual documentation diffs read. SM-01–09 local fixes are coherent; source-index mutation remains unregister/register by contract. Current 32 cases assert selected states, events/config validity, source contexts and actual action effects; cleanup owns all assets/objects. EditMode SendMessage simulates callback entry and is explicitly not real Play lifecycle proof. C-16–C-18 remain mandatory bounded repairs. Correcting default shared-duration behavior must precede optional generalized execution factories; keep standard callbacks/standalone custom dispatch rather than mandatory asset cloning.

Aggregate: per-wrapper Update/Fixed/Late and enabled auto-evaluation, priority-view scan/sort on changed priorities, per-callback context lambdas; no built-in duplicate physics query. Shared mutable custom actions and global StateMachineLog.Enabled remain composition risks/contract decisions. Avoid speculative caching that suppresses side-effecting custom conditions; profile counts/allocations without invented frame rates.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.StateMachine.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root accepted E13 failed constructor reentry regression strengthening

ROOT FULL-read174line NativeImplementationReview-13.md. Runtime/defaulttiming/FIFO sourceapproved, no runtime rewrite. SAME actor implement E13-01 meaningful2path controls in EXISTING owned fixture: public EvaluateTransitions startsA with highpriority A→badB then lower A→D; publicparameterless badB constructor one-shot ChangeState(exactC) then throws; assert C retained, legitimate lifecycle/event transcript and noD. Without reentrantchoice failedconstructor ordinaryfallbackD muststillwork. Assert publicstate/events notprivategeneration; clean owned statichook in finally and retain ordinarylatercommand recovery/no recursive unboundedfixture. Update receipt cases. PrimaryEditorTests currentlegacy postentrytimingcalls nowmachine.SetCurrentStateEnterTime at78/92 alreadyobserved integrated; no serialized authoring option/editor work needed. No extra fixture/framework, no unrelatedcode/tests/asmdef edit, no execution.

## Source3 released caller STATEMACHINE-POST-RELOCATION-LIFECYCLE

Continue SAME original actor/session gpt-6.1-sol/max. General execution/freeze remain FALSE. NO Unity/compiler/test/checker/install/service/children/commit/push before ALL writers finish/newfreeze. Primary b03-04 has SOURCE-FINAL completed its54 helper relocations; root FULL-read dedicated21-line audit/17-line plan/112-line mapping and source-final declaration. ROOT NOW RELEASES ONLY your original module fixture CALLER files for the bounded behavioral correction below. The existing moved helper files under Tests/Fixtures, shared asmdefs/refs, original GUID/type mapping and extraction stay PRIMARY-EXCLUSIVE; do not relocate/recreate helpers or undo aliases. Read current source after relocation and actual source3 XML before patching. No production API/friend/serialized settings changes just to hide a fixture precondition. Retain every exact substantive positive/negative/count oracle, no blanket Ignore/expected-error/no-op/private fake lifecycle/ExecuteAlways/manual Update as native acceptance. Actual native callback claims require supported top-level Play transitions or real PlayerLoop/physics; pure public C# contract tests must say what they cover. Preserve UTF8/no-var, Samples~ and protected DocsEn.meta; no314 migration targets. Update honest own source receipt including deferred closure and stop. Proposed correction is unexecuted until later native run. Own BehaviourTests and generic-host callers released. Source3 E63–E65 illegal Edit SendMessage, generic helper null and missing exit/change callbacks after owner disable. Keep new runtime generic host aliases; establish real active owner OnEnable/OnDisable and addressed machine timing anchors, preserve exact entered/exited/changed transcript, scene context, shared duration data independence and reentry/stop. No synthetic native SendMessage or production stop policy rewrite solely for never-Awake Edit component.
