# Neo.StateMachine: audit

Status: source audit complete; focused fixes, regressions and public documentation authored. Static validation recorded below; Unity compilation and test execution remain with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/StateMachine/Neo.StateMachine.asmdef`.
- Scope: all 12 manifest C# files reviewed completely (3,228 baseline lines); nested assemblies excluded.
- Ownership: runtime files listed below, existing `Assets/Neoxider/Docs/StateMachine` pages except `Editor.md`, this report, and new `Audit20261023NeoStateMachine*` tests/metadata. No editor assembly, existing tests, shared configuration, scenes or prefabs edited.

## Scope

- `Assets/Neoxider/Scripts/StateMachine/ConditionEntryPredicate.cs`
- `Assets/Neoxider/Scripts/StateMachine/IState.cs`
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateAction.cs`
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateData.cs`
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs`
- `Assets/Neoxider/Scripts/StateMachine/StateCondition.cs`
- `Assets/Neoxider/Scripts/StateMachine/StateMachine.cs`
- `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs`
- `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviourBase.cs`
- `Assets/Neoxider/Scripts/StateMachine/StateMachineEvaluationContext.cs`
- `Assets/Neoxider/Scripts/StateMachine/StatePredicate.cs`
- `Assets/Neoxider/Scripts/StateMachine/StateTransition.cs`

## Review coverage

| Owned file | Review outcome |
|---|---|
| `IState.cs` | All five lifecycle contracts reviewed; optional tick hooks and caller-driven ticking retained. |
| `ConditionEntryPredicate.cs` | Slot lookup, missing/destroyed objects, owner fallback, condition delegation and inversion reviewed; SM-07 fixed. |
| `StateCondition.cs` | Legacy bool/float/event conditions reviewed, including approximate float equality and stored event results. Existing semantics retained. |
| `StatePredicate.cs` | Every predicate reviewed: numeric/string boundaries, inversion, event/delegate null handling, duration anchoring and composite forwarding. Corrected the custom-predicate XML example to implement abstract `EvaluateInternal`. Shared runtime anchors are proposal A. Existing composite null-child policies retained. |
| `StateTransition.cs` | Exact type/asset source checks, enabled flag, AND short circuit, predicate mutation and timer notifications reviewed. Corrected XML examples to use public properties and explicit types. |
| `StateMachine.cs` | State/transition cache modes, registration/removal, priorities, target activation, callbacks, stop, events and diagnostic gate reviewed. SM-01 through SM-05 fixed; recursive lifecycle commands are proposal B. |
| `StateMachineEvaluationContext.cs` | Both thread-local stacks, nested push/pop restoration, destroyed override handling and subsystem/play-exit reset reviewed. Existing try/finally scopes retained; Unity version branches require central compilation. |
| `StateMachineBehaviour.cs` | Initialization, typed/name selection, load/re-enable, ticking, teardown, serialized fields and reflection activation reviewed. SM-06 fixed; reflection local now has an explicit type. |
| `StateMachineBehaviourBase.cs` | Same lifecycle/context review plus runtime names, elapsed time, counters, public controls and UnityEvent adapters. SM-06 fixed. |
| `NoCode/StateAction.cs` | Every action reviewed: legacy direct references, context slots, null events, gated logging and scene-name/index precedence. No scene/build changes made. |
| `NoCode/StateData.cs` | Serialized migration attributes, all action lists, null entries, action exception containment and context forwarding reviewed. Shared/custom mutable actions require an execution-instance design. |
| `NoCode/StateMachineData.cs` | State lookup, initial reference/name compatibility, transition loading, validation and legacy positions reviewed. SM-08/09 fixed; no asset cloning introduced. |

Read `Neo.StateMachine.asmdef` and the existing `Neo.Editor.Tests` references, the existing state-machine and duration regressions, and relevant condition/context contracts. NeoxiderTools module/idiom/extension references were checked before adding scoped logic; existing transition and context primitives were reused. No additional package or reusable helper subsystem was needed. Local review was performed without a child agent or independent QA, as requested.

## Demonstrated findings and completed fixes

Evidence below is source-path evidence and authored regression coverage, not a claim that Unity executed the reproductions.

| ID / severity | Trigger, observable consequence and source | Completed correction / regression |
|---|---|---|
| SM-01 / P2 | Warm `GetAvailableTransitions`, then raise the second transition's public `Priority`. The sorted view remains stale and selects the old winner. `StateMachine.GetAvailableTransitions`. | Detect an out-of-order cached view and sort it before returning. `PriorityChangedAfterCacheWarmup_SelectsNewHighestPriority`, both cache modes. |
| SM-02 / P2 | Edit a registered source selector, unregister the transition, then restore the selector. Old removal searches the new bucket and the original rule can fire again. `StateMachine.UnregisterTransition`. | Remove the rule by reference from registration buckets and the global list, independent of its current selector. `UnregisterAfterSourceEdit_RemovesOriginalRegistration`, both cache modes. Source edits still require re-registration to update indexing. |
| SM-03 / P2 | Use a subclass of `StateData` as the source with default caching. Registration uses `typeof(StateData)` while lookup uses the concrete type, making the transition unreachable. `RegisterTransition` / `AnchorTimeBasedPredicates` / `EvaluateTransitions`. | Index by the source asset's concrete type; exact asset filtering remains in `CanTransition`. `DerivedStateData_CanUseItsRegisteredTransition`, both cache modes. |
| SM-04 / P2 | Give the highest-priority rule an incompatible, abstract or unconstructible CLR target. `TryApplyTransitionTarget` reports success even when creation failed, blocking valid fallback rules. | Private activation reports actual success; reject non-class, incompatible, abstract, open-generic, Unity-object and missing-public-constructor targets before activation. `UnusableTarget_DoesNotBlockLowerPriorityTransition`, three invalid target kinds. |
| SM-05 / P2 | A predicate or `OnTransitionEvaluated` listener calls `Stop()` or explicitly chooses another state. The old evaluation subsequently overwrites that choice. `EvaluateTransitions`. | Preserve the evaluated source identity and abandon the old result when callbacks changed it. `EvaluationCallback_RespectsExplicitStateChoice`, four predicate/listener and stop/change combinations. |
| SM-06 / P2 | Exit a state through component disable, destruction or reload. These paths call `Stop` outside the context scope, so exit actions cannot resolve their owner/override, or inherit an unrelated outer scope. Both behaviour classes. | Route all six lifecycle/reload stop sites through existing `RunWithContext`. `LifecycleExit_UsesOwnerAndOverrideContext`, both adapters and three exit paths; asserts action context, execution count, target activation and final state. |
| SM-07 / P2 | Select an empty, destroyed or out-of-range Override while Owner satisfies the condition. `ConditionEntryPredicate.EvaluateInternal` silently substitutes Owner and can fire the wrong transition; a negative enum also resolves Owner. | Only Owner may use the live MonoBehaviour-state fallback; missing override/negative index returns false before optional inversion. `MissingOverride_DoesNotEvaluateAgainstOwner` and `ConditionSlot_UsesOnlySelectedContext`, seven cases including valid Owner/Override and a false override value. |
| SM-08 / P2 | Validate an asset with a missing initial state or missing/foreign NoCode endpoint. Warnings are emitted conditionally but `Validate(true)` still reports success. `StateMachineData.Validate`. | Accumulate the invalid result for the existing diagnostics; retain early rejection of empty arrays and unknown initial references/names. `Validate_RejectsInvalidNoCodeConfiguration`, five mutations of a verified valid configuration. Loading remains independent of validation. |
| SM-09 / P2 | Assign `States = null`, then set `InitialStateName` or perform named lookup. LINQ/array lookup receives null and throws instead of treating the asset as empty. `States` / `GetStateByName`, consumed by both wrappers. | Normalize null arrays and make lookup use the normalized public collection. `NullStates_HasEmptyLookupAndRemainsInvalid` verifies the empty result, missing initial state and invalid configuration. |

Removed the two executable `var` locals and corrected explicit types/public members in touched XML examples. No public signatures or serialized field names were changed. Existing constructor defaults, lifecycle flags and action serialization migration attributes were preserved.

По-русски: исправлены ошибки приоритетов и удаления переходов, поддержка наследников StateData, блокирование fallback некорректной целью, потеря контекста exit actions, подмена Override объектом Owner и ложноположительная проверка конфигурации. Общие часы predicates и рекурсивные lifecycle-вызовы требуют отдельных архитектурных изменений, описанных ниже.

## Documentation

Updated eight existing English pages: canonical `README.md`, `StateMachine.md`, `StateTransition.md`, `ConditionEntryPredicate.md`, `NoCode_StateMachine_Usage.md`, `NoCode/StateMachineData.md`, `StateMachineBehaviour.md`, and `StateMachineBehaviourBase.md`.

The pages explain public C# entry points and scene/NoCode setup, dynamic priority ordering, source re-registration, target construction, callback cancellation, lifecycle exit context, missing-slot semantics, null/invalid configurations and remaining shared-instance limits. Corrected the typed component setup, component-method versus core context distinction, module links and current `Samples~` demo paths while retaining developer/imported sample layouts. `Editor.md` belongs to the nested worker and was not edited.

## Substantial improvement proposal A: per-machine execution frames

**Status: proposed, not implemented. Existing P2 behavior remains.**

**Problem/evidence:** `StateMachineData.LoadIntoStateMachine` registers the asset's transition objects directly. `StateMachine.AnchorTimeBasedPredicates` calls `StateDurationPredicate.SetEnterTime`, storing one mutable timestamp on a shared predicate. Entering a state on machine B therefore restarts machine A's wait when they use the same asset. This also occurs when distinct `StateData` assets of the same type share a duration predicate. Custom actions/predicates may have additional mutable fields. Blind JSON cloning would lose runtime delegates and CLR type configuration, while cloning ScriptableObjects adds ownership/destruction obligations.

**Proposed architecture/public API:** keep `StateMachineData`, `StateData` and built-in condition definitions as reusable authoring data. Give each `StateMachine<TState>` a runtime execution frame containing entry time, a monotonic state version and optional scene bindings. Introduce a typed `StateEvaluationContext` and an additive context-aware evaluation overload on `StatePredicate`. The default overload should delegate to existing virtual evaluation paths so user predicates keep working. `StateDurationPredicate` reads elapsed time from the execution frame; its existing `SetEnterTime` remains available for standalone evaluation. An optional injected clock makes duration checks deterministic without waiting for Unity time. Custom stateful predicates/actions can opt into runtime factories rather than relying on unrestricted cloning.

**Compatibility/migration:** retain existing asset references, managed-reference type names and serialized fields; do not rewrite scenes or require a factory for current stateless actions. Stage the context overload first, then migrate built-in duration evaluation. Document the meaning of legacy `SetEnterTime` when a machine frame is active and preserve existing standalone tests. Context changes span runtime and editor authoring expectations, requiring coordinator integration with `Neo.StateMachine.Editor` and samples.

**Acceptance scenarios:** two machines share the same asset, enter at different fake-clock times, and transition independently; nested machines restore owner/override and clock scopes even on exceptions; re-entering A resets only A; custom legacy predicates retain virtual dispatch and inversion; exiting/disposal releases runtime bindings without destroying authoring assets. Existing duration, NoCode action and reload tests must remain green in Unity 6.0–6.4 and 6.5+.

**Effort/risk:** approximately 2–4 development days plus integration QA. Main risks are custom-predicate dispatch compatibility, mutable custom actions, allocation frequency and clear ownership of runtime instances. Start with the duration frame rather than a general-purpose graph clone.

## Substantial improvement proposal B: bounded lifecycle command dispatch

**Status: proposed, not implemented. Existing P1/P2 behavior remains.**

**Problem/evidence:** `ChangeState` keeps the outgoing state current while invoking `OnExit`. If that hook synchronously calls `ChangeState` again, the same state's exit is invoked recursively and may overflow the stack (P1). If an enter action disables its owner, `OnDisable` can stop the machine before the outer `OnStateEntered`/`OnStateChanged` calls finish, producing null/incorrect event payloads (P2). SM-05 only protects transition evaluation callbacks; it does not make lifecycle hooks transactional, nor detect leaving and returning to the same cached instance during one callback.

**Proposed architecture/API:** serialize lifecycle changes through one dispatcher with immutable event payload locals, a state-version counter and a bounded pending-command queue. Add `RequestState(TState)` / `RequestStop()` for commands made inside lifecycle callbacks, with an explicit accepted/queued result. Drain after a complete exit/enter/event transaction and stop cyclic command chains with a gated diagnostic. Use the state version, rather than reference identity alone, to invalidate stale transition evaluations.

**Compatibility/migration:** introduce the request APIs without renaming `ChangeState`, `Stop` or serialized events. Define event order and opt into deferred reentrant dispatch before changing legacy synchronous behavior by default. The editor worker would expose the new request methods where needed; existing direct APIs remain usable outside callbacks.

**Acceptance:** exit requesting another state exits each state once; enter requesting Stop produces coherent ordered payloads; repeated stop is idempotent; callback A→B→A invalidates A's old evaluation; cyclic requests terminate within the documented bound. **Effort/risk:** 1–2 days plus event/lifecycle QA; synchronous listener assumptions and `TryChangeState` result semantics require explicit review.

## Verification and remaining risks

- Added `Audit20261023NeoStateMachineRuntimeTests.cs` and `Audit20261023NeoStateMachineBehaviourTests.cs`, plus `Audit20261023NeoStateMachineDerivedState.cs` and `Audit20261023NeoStateMachineGenericHost.cs`, under `Tests/Edit/StateMachine`, with four unique GUID `.meta` files. There are 10 test methods expanding to 32 NUnit cases. They assert actual current/previous states, selected transitions, validation/lookup results, context objects and GameObject activation.
- Tests use the existing `Neo.Editor.Tests` assembly, which already references `Neo.StateMachine`, `Neo.Condition` and Unity's editor/test assemblies. No reference/configuration change is required.
- Lifecycle regressions explicitly dispatch Unity callback messages in Edit Mode; they do not constitute a Play Mode lifecycle/import verification.
- Actual static validation passed: strict UTF-8 decoding and no replacement characters in 29 scoped files; public declarations and private/protected field declarations unchanged against `ca198556` in all 12 runtime files; explicit-type and obsolete-instance-ID scans plus lexical delimiter counts in 16 C# files; 25 local documentation links resolved; four new metadata GUIDs unique across `Assets`; 10 methods / 32 authored cases counted; existing test references confirmed; all six lifecycle/reload stop sites verified inside the existing context scope. Delimiter counts are a mechanical check, not C# compilation.
- `git diff --check -- <the 12 manifest sources, 8 edited docs, 4 new test/helper sources and metadata, this report>` returned exit 0. New test files were also checked directly for trailing whitespace and a final newline, because untracked files are absent from `git diff`.
- The first static inventory assertion stopped because the runtime prefix also matched the nested editor worker's newly added test file. Re-running with the four exact owned test filenames passed; that worker's test was not changed.
- Unity, builds, test runners, installs, commits and child agents were not invoked. Compile/import, runtime behavior, actual Play Mode lifecycle callbacks and both Unity version branches remain unverified here by explicit worker contract.
- Remaining limitations: proposals A/B above; source-index changes still need unregister/register; equal-priority ordering is unspecified; legacy action/event/predicate state is mutable; low-level `StateMachineLog.Enabled` is shared across components and is set by wrapper enable callbacks; no exception rollback is promised for user lifecycle hooks. Existing composite null-child and initial null exit-event semantics are retained for compatibility.
- Coordinator actions: compile both Unity API version branches; run the new 32 cases and existing state-machine/duration tests; verify actual disable/destroy/re-enable/reload in Play Mode. Coordinate nested-editor UI expectations for `Validate` now returning false for its existing invalid-endpoint diagnostics. No other worker's source or tests need edits for the fixes.
