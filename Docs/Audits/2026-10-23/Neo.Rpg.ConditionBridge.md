# Neo.Rpg.ConditionBridge: audit

Status: source audit and scoped fixes complete; Unity compilation and regressions pending coordinator integration.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Rpg/Bridge/Condition/Neo.Rpg.ConditionBridge.asmdef`.
- Scope: 1/1 inventoried C# files reviewed completely; nested assemblies excluded.
- Ownership: exact `Neo.Rpg.ConditionBridge` manifest entry; only its source, assigned documentation topic, this report, and new uniquely prefixed regression files changed.
- Execution: file-only. No Unity/MCP, compiler/build/test runner, installations, shared configuration changes, commits or child agents.

## Scope

- `Assets/Neoxider/Scripts/Rpg/Bridge/Condition/RpgConditionAdapter.cs`

The entire file was inspected: all 19 mode arms, character/context resolution, controller dependencies, thresholds and inversion, legacy IDs, result state and synchronous events, `EvaluateCurrent`, attributes and assembly migration. The owned directory's actual C# inventory was checked against the manifest and contains exactly this file.

Dependency context read without edits: `IConditionEvaluator`, relevant `ConditionEntry` boolean-method workflow, `RpgConditionEvaluationMode`, character getters and initialization, `RpgStatId`, resource getters, effect lookup entry points, evade readiness, attack readiness and optional character resolution, existing RPG/Condition tests and test asmdefs. The previous read-only condition audit was used only as a lead; its historical test results are not validation for this revision.

### Review results by contract

- **Character and context:** explicit live reference wins; otherwise query context itself and its active parent hierarchy using Unity's default lookup. Null context can still use the assigned character. Destroyed Unity references fall back rather than being dereferenced. No scene-wide search, child search or cached hierarchy results.
- **All 19 modes:** HP/percent, level, death, buffs/statuses, action permission, invulnerability, evade/attack readiness, four resource comparisons, two stat comparisons, upgrade points/count and XP were reviewed. Comparisons retain inclusive `AtLeast`, strict `Below`, and percent thresholds divided by 100. Missing resources/stats continue to follow the underlying zero-value policy; thresholds are not newly clamped.
- **IDs and serialization:** typed resource/stat selectors remain intact. Nonblank legacy `_resourceId` is trimmed and takes priority; whitespace falls back to the typed selector. All 16 serialized field names/types/order, four public declarations and `MovedFrom` metadata match baseline. The source's existing `.meta` is untouched.
- **Lifecycle/resources:** adapter owns no coroutines, subscriptions, disposable runtime services or cached scene objects beyond assigned references. Each call reads current state. `LastResult` updates before callbacks; nested evaluations can legitimately become its latest value.
- **Dependencies/version/logging:** controllers remain explicitly assigned and enforce their own rules. No dependency, asmdef or serialized-layout changes. No raw runtime logging, Unity identity calls or new version branches. Cross-version compilation is unverified.
- **Authoring/C#:** `Evaluate(GameObject)`, `EvaluateCurrent()` and `LastResult` remain the public API. Inspector events stay serialized. `NeoCondition` can read the parameterless boolean method; custom consumers can pass a context through `IConditionEvaluator`. A typed, event-free configuration API is proposed below, not implemented.

## Findings and fixes

The evidence below is a deterministic source-level trace, not a claimed Unity execution. Current implementation and focused regressions are present on disk.

| Finding | Severity / trigger | Baseline observable consequence | Fix and regression evidence |
| --- | --- | --- | --- |
| CB-01: missing-character results silently skip events | P2. `Evaluate(null)` without an assigned character, or evaluation of an empty/despawned context. | The early return sets `LastResult = _invert` but invokes neither `On Evaluated` nor the matching branch. Event-driven consumers retain their previous response although the returned condition changed. | Replaced the early return with a guarded false switch result and common inversion/event dispatch. `Evaluate_MissingCharacter_PublishesInvertedResultAndMatchingEvent`: four cases cover null/empty context and both inversion settings, observing event value, order, matching branch, return and `LastResult` at notification. |
| CB-02: a nested evaluation replaces its caller's result | P2. An `On Evaluated` listener evaluates the same adapter for a context with the opposite HP result. | For an outer true / inner false call, the outer event announces true, but its branch uses overwritten `LastResult`, fires false and returns false. The converse is also wrong. | Each call retains a local result for notifications, branch selection and return. `LastResult` still tracks the latest computation. `Evaluate_ReentrantListener_PreservesEachCallsReturnValueAndBranch`: two opposite-result cases check both return values, evaluated order, inner/outer branches and final state. |
| CB-03: controller-only predicates are blocked by an unrelated character lookup | P2. A ready controller is explicitly assigned but the adapter context has no `RpgCharacter`. | The unconditional character-null return prevents `CanEvade`/`AttackReady` from querying that controller. `RpgEvadeController.CanEvade` permits an absent receiver; `RpgAttackController.CanUseAttack` also permits an absent character source. The adapter returns a false negative. | Controller arms run before the character-null guard; other modes still require a character. `Evaluate_ControllerMode_UsesAssignedControllerWithoutCharacter`: four cases cover both controllers and both inversion settings, first establishing real controller readiness. Two additional cases retain the assigned-controller requirement when a character is present. |

Source: [RpgConditionAdapter.cs](../../../Assets/Neoxider/Scripts/Rpg/Bridge/Condition/RpgConditionAdapter.cs), particularly `Evaluate`, its original character-null return, and result/event dispatch. Dependency evidence: `Scripts/Rpg/Components/RpgEvadeController.cs` (`CanEvade`) and `RpgAttackController.cs` (`CanUseAttack(RpgAttackDefinition, out string)`). Those dependency files were not changed.

New regression fixture: [Audit20261023NeoRpgConditionBridgeTests.cs](../../../Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeTests.cs). Eight methods define **31 NUnit cases**, authored but not executed: ten directly target the fixes, while 21 protect strict/inclusive numeric boundaries, percentage units, explicit-source priority, destroyed-reference fallback, parent lookup/current-state reads, legacy ID trimming/priority, and missing controllers. Reflection only obtains the Inspector-owned events for listener setup; assertions observe public results, real character/controller state and emitted notifications.

Compatibility note: method signatures, serialized fields and saved type migration are unchanged. Intentional observable corrections are event delivery for missing dependencies and allowing controller readiness without a context character. Existing valid character predicates retain their original expressions.

## Documentation

Added the assigned narrow integration topic [Rpg/ConditionBridge.md](../../../Assets/Neoxider/Docs/Rpg/ConditionBridge.md), with a unique `.meta`, and redirected the component's `NeoDoc` attribute to it. It explains assembly references, all modes, C# calls, explicit/controller/context resolution, threshold units, legacy IDs, missing-data/inversion rules, event order/reentrancy, and the Inspector/`NeoCondition` workflow.

The existing parent-owned `Rpg/RpgConditionAdapter.md` was read and left unchanged. **Coordinator action:** link `ConditionBridge.md` from the RPG module entry and link/delegate the old class page to the integration topic so event/dependency semantics have one maintained source. No parent or shared README was edited.

## Substantial improvement proposal

### Proposed: event-free typed condition queries with availability results (not implemented)

**Problem.** Every configuration field is private and serialized, so programmers cannot construct or reuse an RPG condition through a typed public configuration API. Reusing the adapter also executes scene events. A boolean cannot distinguish an unmet predicate from a missing character/controller or invalid ID; inversion currently turns unavailable input into true, which is compatible behavior but often insufficient for a caller that needs diagnostics or an explicit unavailable policy.

**Architecture / public API.** Add a small `RpgConditionSettings` value containing mode, thresholds, typed resource/stat IDs, effect/attack IDs and inversion. Add an event-free `RpgConditionQuery.Evaluate(in RpgConditionSettings settings, RpgCharacter character, RpgEvadeController evade = null, RpgAttackController attack = null)` returning `RpgConditionEvaluation` with `bool Result` and a typed availability/status value (`Available`, `MissingCharacter`, `MissingController`, `UnknownId`, `InvalidMode`). Reuse existing character/controller APIs and resource state; do not create another stats store. The query performs no scene search, logging or UnityEvent invocation. The component resolves its scene dependencies, projects existing fields into the settings value, calls the query, and retains its current event dispatch contract. Existing C# callers can continue to use the component or opt into the query.

**Compatibility / migration.** Keep all existing serialized component fields and enum values; use an internal settings projection rather than moving fields into a nested serialized object. Preserve the legacy resource-ID priority in that projection. Keep `Evaluate`'s current boolean/inversion policy, including missing dependencies; the new status lets new callers choose a stricter policy explicitly. Keep the existing namespace, assembly, `MovedFrom` metadata and Inspector events. Any future ScriptableObject preset stores only settings, never scene references.

**Acceptance scenarios.** Construct/evaluate equivalent HP, custom-resource, stat and attack predicates from C# without adding an adapter. Verify component/query parity across all 19 modes and both inversion settings. Verify availability is distinguishable from a valid false result, including unknown IDs and a missing controller. Verify pure query calls emit no scene events, while component calls emit the same ordered events and retain reentrant return-value isolation. Import an existing serialized component with a legacy resource ID and verify unchanged results. Compile and run focused cases centrally on the supported pre-6.5 and 6.5+ Unity versions.

**Effort / risk.** Approximately 1-2 developer days plus the central Unity matrix. Main risk is divergent legacy-ID/inversion behavior between the wrapper and query; use one evaluator and parity tests. Distinguishing an unknown attack ID requires a typed lookup/result contract coordinated with the parent RPG owner; do not parse controller failure strings. Do not introduce a broad generic condition framework or migrate serialized data in this change.

## Verification and remaining risks

### Checks actually run

- Scoped `git diff --check --` for the source, new docs/test/metas and this report: exit 0. Git reports the existing LF-to-CRLF working-copy policy; no repository configuration was changed.
- Read-only Python static consistency check: manifest inventory equals the actual owned C# inventory (**1/1**); all **16** serialized field declarations, **4** public declarations and `MovedFrom` match `ca198556`; all **19** enum cases still appear once and retain their original predicate expressions.
- The same check strictly decoded changed package files as UTF-8, checked replacement-character absence and trailing whitespace, checked explicit C# types/no `var`, checked absence of raw logging/Unity identity calls, resolved documentation links and the updated `NeoDoc` target, and confirmed both new GUIDs occur in exactly their own package `.meta`. **PASS.**
- Inspected the scoped source diff. Counted **8 regression methods / 31 authored cases**; this is a static inventory, not a test result.

### Required integration and unverified risks

1. **Coordinator must add `Neo.Rpg.ConditionBridge` to `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef.references` before compiling the new typed fixture.** Static inspection confirms that reference is currently absent. Other fixture dependencies are already referenced. Shared asmdef changes are forbidden to this worker, so no edit was made. Until integrated, the fixture's adapter type is unresolved in that test assembly.
2. Coordinator must compile/import and run `Neo.Tests.Edit.RPG.Audit20261023NeoRpgConditionBridgeTests` (31 cases), then apply the project's Unity version matrix. No Unity, Play Mode, test runner, build runner or compiler was started here; there is no runtime pass claim.
3. Parent-owned documentation navigation/class page should link to the new topic as described above. No other cross-boundary source edits are required for these fixes.
4. Prefab/scene migration and Inspector authoring were reviewed from source only. Effect/progression modes were inspected but are not exhaustively exercised by the new fixture; the focused tests cover the changed behavior and relevant retained numeric/resolution contracts.

Кратко: единственный исходник проверен целиком; исправлены пропущенные события, подмена результата при вложенной оценке и лишняя зависимость проверки контроллера от персонажа. Регрессии добавлены, статические проверки выполнены; запуск Unity и тестов остаётся за координатором.
