# Neo.Rpg.ConditionBridge: implementation plan

Phase 2, 2026-10-23. Same auditor/session; `gpt-6.1-sol`, reasoning `max`. Scope follows the manifest. This plan is the only phase-2 write; implementation and verification below are future work. Concurrent dependency edits must be rechecked at integration.

## Current outcome

The complete owned inventory, `Assets/Neoxider/Scripts/Rpg/Bridge/Condition/RpgConditionAdapter.cs`, was reviewed. CB-01 missing-character event delivery, CB-02 reentrant return/branch isolation, and CB-03 controller readiness without a context character are already fixed. Do not schedule those fixes again. The adapter still delegates controller readiness to the controller.

Added `Assets/Neoxider/Docs/Rpg/ConditionBridge.md`, updated `NeoDoc`, and authored eight methods / 31 cases in `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeTests.cs`. Audit-time static checks passed: inventory, 16 serialized fields, public declarations, 19 predicate expressions, migration metadata, UTF-8, GUIDs, links and scoped whitespace. No Unity compilation, tests, Play Mode or build ran. Phase 2 reread current source/docs/tests and found the integration issue below.

## Remaining confirmed work and verification debt

1. **P1 — regression fixture now conflicts with controller lifecycle contracts.** Owner: ConditionBridge test owner, after `Neo.Rpg` owner finalizes its changes. Current `RpgEvadeController.cs:55-57` requires `isActiveAndEnabled`; `RpgAttackController.cs:493-505` rejects inactive controllers. The fixture's `SetUp` deactivates `_root`, so all four cases of `Evaluate_ControllerMode_UsesAssignedControllerWithoutCharacter` reach a false readiness precondition. This is confirmed by source, not an executed failure. Smallest correction: in that method only, disable the assigned controller's `EnableBuiltInInput`, finish configuration, activate `_root`, then assert readiness and evaluate without a character. Keep the parent's activity guards. Add focused disabled-controller cases: raw result false, inverted result true. Change only the existing prefixed fixture. Expected: ready active controllers pass without a character; disabled controllers remain unavailable.

2. **P1 — test assembly reference missing.** Owner: coordinator, coordinating with `Neo.Editor.Tests`. Current `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` lacks `Neo.Rpg.ConditionBridge`; the typed fixture cannot compile until integrated. Add that single reference before running it. Other required references already exist. No runtime asmdef change is needed.

3. **P2 — execution/migration debt, not additional demonstrated runtime defects.** Owner: coordinator after source-changing audits finish. Compile the integrated source and run all 31 existing cases plus the new disabled-controller cases. Effect/progression modes, serialized import, live Inspector wiring and supported Unity versions remain unverified. Preserve the three completed fixes unless a concrete failing public-behavior reproduction warrants correction.

4. **P2 — canonical navigation.** Owner: `Neo.Rpg` (`neo-audit-1023-b05-03`). Link the new topic from `Assets/Neoxider/Docs/Rpg/README.md` and delegate the existing `Assets/Neoxider/Docs/Rpg/RpgConditionAdapter.md` integration explanation to it. Observe working links and one authoritative event/dependency description. Those parent-owned files remain untouched here. `Neo.Condition` owns the consuming boolean-method workflow; coordinate any integration failure there rather than editing it from this assembly.

## P2 proposal: typed, event-free queries with availability

**Problem and reuse.** Private Inspector configuration prevents constructing reusable predicates from ordinary C#. Evaluating the component also emits scene events; its boolean cannot distinguish unavailable input from a valid false result. Reuse `RpgConditionEvaluationMode`, `RpgStatId`, character getters/`Resources`/`Stats`, and controller readiness. Current source search found no existing `RpgConditionSettings`, query service or public attack lookup. Do not introduce another stats store, manager, generic condition framework or dependency. This remains an optional legacy-RPG improvement, separate from required integration.

**Public design.** Add a settings value containing mode, thresholds, selectors, effect/attack IDs and inversion; a result value containing `bool Result` and `RpgConditionAvailability`; and:

```csharp
public static RpgConditionEvaluation Evaluate(
    in RpgConditionSettings settings,
    RpgCharacter character,
    RpgEvadeController evade = null,
    RpgAttackController attack = null);
```

`RpgConditionQuery` belongs to `Neo.Rpg.ConditionBridge`. It performs no scene lookup, logging, events or history mutation. It is Unity-aware, not an engine-independent model. The component resolves context, projects its existing fields into settings, calls the query, and retains its local-result event dispatch.

Statuses: `Available`, `MissingCharacter`, `MissingController`, `UnknownId`, `InvalidMode`. Invalid mode takes precedence over dependency absence. Unknown resource/stat IDs retain legacy zero-value comparisons; an unknown attack remains false before inversion. Status must not override the compatibility boolean: an unavailable inverted predicate can still return true. An absent active buff/status is an ordinary false result, not evidence of an unknown registered ID. Preserve existing ID normalization exactly.

Attack registration needs an additive parent API: `bool TryGetAttack(string attackId, out RpgAttackDefinition definition)` in `Assets/Neoxider/Scripts/Rpg/Components/RpgAttackController.cs`, sharing the existing resolver. Parent owner implements that first; do not parse failure strings. The parent must not reference ConditionBridge, avoiding a dependency cycle.

**Exact implementation files.** Add these under `Assets/Neoxider/Scripts/Rpg/Bridge/Condition/`: `RpgConditionSettings.cs`, `RpgConditionEvaluation.cs` (result and availability enum), and `RpgConditionQuery.cs`. Adapt the existing `RpgConditionAdapter.cs`; add `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgConditionBridgeQueryTests.cs`; extend `Assets/Neoxider/Docs/Rpg/ConditionBridge.md`. New package files receive unique `.meta` files. No new assembly/editor wrapper is needed.

**Compatibility/documentation.** Preserve all 16 serialized fields, enum values, MonoScript GUID and `MovedFrom`; project settings from existing fields instead of migrating assets. Retain legacy `_resourceId` priority, threshold units/defaults, inversion and reentrant events. Settings contain no scene references. Explain direct query construction/status handling for programmers and unchanged controller assignment/events for Inspector users. Keep `Samples~`, developer `Samples` and imported sample layouts unchanged. Estimate: 1-2 developer days plus central verification; main risks are wrapper/query divergence and coordinated parent API availability.

## Implementation order, acceptance and stop conditions

1. Finish parent source audits; recheck the activity guards and references. Correct fixture setup, integrate the EditMode reference, then run the focused regressions. These steps do not depend on accepting the proposal.
2. If implementing the proposal, add the parent lookup, then query values/service, then the component projection. Keep one predicate implementation. Add query/component parity cases for all 19 modes and both inversion settings, plus status distinction and legacy resource-ID tests.
3. Observe missing-context events once per call, nested true/false calls retaining independent returns/branches, strict/inclusive numeric boundaries, current hierarchy reads and active/disabled standalone controllers. A query call must leave adapter events and `LastResult` untouched.
4. Update C#/NoCode documentation and parent navigation. Centrally exercise one live controller enable/disable and `NeoCondition.EvaluateCurrent()` integration scenario. If automation is needed, add `Assets/Neoxider/Tests/Play/RPG/Audit20261023NeoRpgConditionBridgePlayModeTests.cs`; coordinator must first add the currently absent bridge reference to `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef`.
5. Centrally compile on supported Unity 6.0-6.4 and 6.5+ baselines, checking any conditional branch with its intended compiler. Add no scattered version checks or identity calls. Validate old serialized component import, package metadata/links, optional assembly boundaries and scoped diffs; no sample restructuring is required.

Mandatory integration is complete only with recorded compilation and focused test results, not this static plan. Do not claim an unrun scene or version passed. If parent contracts change again, update the reproduction before changing expectations. The coordinator may defer the optional query proposal; if its typed attack lookup is unavailable, defer that status capability explicitly rather than guessing from strings or blocking the completed fixes.
