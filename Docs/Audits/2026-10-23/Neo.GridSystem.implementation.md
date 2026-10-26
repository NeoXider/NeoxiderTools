# Neo.GridSystem: implementation receipt

Status: **SOURCE-FINAL; no remaining source/API dependency.** Same actor `neo-audit-1023-b03-05`, branch `demo`, target `11.0.0`. All four assigned source corrections plus root-accepted `NI20-01` / B-07 residual, including the published-blocker-removal follow-up, and their owned documentation/regression sources are complete. The shared PlayMode reference is now present through root integration.

The original [audit](Neo.GridSystem.md) and [plan](Neo.GridSystem.plan.md) remain unchanged historical evidence. Earlier coherent source fixes are preserved. The board core, generation leases and automatic object-lifetime policies remain proposals.

Path prefixes below are exact: `S = Assets/Neoxider/Scripts/GridSystem/`, `D = Assets/Neoxider/Docs/GridSystem/`, `E = Assets/Neoxider/Tests/Edit/GridSystem/`, `P = Assets/Neoxider/Tests/Play/`.

## Completed approved actions

| Action | Exact runtime files | Implemented behavior |
|---|---|---|
| `GRID-COORDINATE-OVERFLOW` | `S/FieldGenerator.cs`, `S/GridPlacementService.cs` | Both footprint validation paths share one helper that adds all three coordinate components in `long`, rejects values outside Int32 before constructing the target position, then uses existing bounds/state validation. Validation completes before writes. Invalid coordinates cannot wrap onto cell zero or another valid cell. Valid negative offsets and out-of-board anchors whose resolved targets fit remain supported. |
| `GRID-MERGE-OVERFLOW` | `S/Merge/GridMergeRequest.cs`, `S/Merge/GridMergeResolver.cs` | The increment factory and null-calculator fallback use the same signed saturating addition. Positive overflow produces `int.MaxValue`; negative overflow produces `int.MinValue`. Ordinary increments, group clearing, previews and cascade limits retain their contracts. Custom calculators, including Dice's existing capped calculator, are untouched. |
| `GRID-STALE-OCCUPANCY` | `S/FieldObjectSpawner.cs` | Spawn occupancy requires the original cell to remain current and its metadata registration to remain present after `OnObjectSpawned`. Removal retires the original registration and empty cell list, schedules destruction and raises `OnObjectRemoved`, then changes occupancy only if the original cell is still current with no remaining blocker. Field-state callbacks are followed by another identity/state check before publishing the spawner occupied/freed event. |
| `GRID-GIZMO-STYLE-ALLOCATION` | `S/FieldGenerator.cs` | When cell/coordinate drawing is enabled, construct one coordinate `GUIStyle` per draw invocation outside the cell loops. Reuse it for all labels with the same alignment, font size, color and coordinate text. No global style/result cache or draw-output change was introduced. |
| `NI20-01` / `GRID-STALE-OCCUPANCY` residual, including published-blocker-removal follow-up | `S/FieldObjectSpawner.cs` | After the original registration/spawn callback and current-registration checks, a blocking call publishes occupancy when the exact live cell is still free. A per-call entry snapshot separately preserves the legacy first blocking registration's notification on an initially occupied cell. A nested blocker can publish then be removed while the outer callback is running; the returning outer call cannot publish occupied again merely because it is now the sole blocker. The same applies when a sequential spawn callback removes the previous published blocker. Manual admission, generation/removal guards and last-blocker freeing remain intact. |

There are **five runtime files changed in this implementation phase**. Nearest-assembly boundaries, nested Dice files and other workers' files are preserved.

## Exact APIs and compatibility

**No additive public API, renamed serialized field, enum migration or runtime assembly reference is introduced.** Existing placement, merge and spawner signatures remain available. Added internal implementation helpers are:

```csharp
// FieldGenerator
internal static bool TryAddCoordinates(
    Vector3Int anchor, Vector3Int offset, out Vector3Int position);
// GridMergeRequest
internal static int SaturatingAdd(int value, int step);
```

These helpers are assembly-internal and are not a new consumer-facing API. Dice continues through the existing public footprint methods. The existing generic `Neo.Merge.MergeResolver` performs connected-group/cascade work; no duplicate merge implementation or new dependency was added.

Deliberate behavior corrections for 11.0.0: overflowing placements reject atomically; overflowing built-in merge increments saturate instead of wrapping; detached registrations cannot write into a replacement board or publish a stale occupancy event. A saturated merge still clears its other group cells. Custom `GetMergedContent` delegates retain their numeric policy.

The ordinary object-event order is preserved. Surviving objects remain bound to their original cells after regeneration, visible in `GetAllObjects()` until removed. Current-cell queries exclude those detached bindings. `RemoveObject` still destroys the requested instance and publishes its removal; it does not rebind or automatically despawn other objects. Registered public metadata is documented as inspection data. External destruction/pooling, competing occupancy writers, arbitrary metadata mutation and callback exception rollback remain outside this bounded correction.

`NI20-01` distinguishes an in-flight registry entry from published occupancy using the current `FieldCell.IsOccupied` state and per-call locals. `wasOccupiedAtEntry` is captured before `Instantiate`, which can invoke callbacks. After creation but before registering this instance, `notifyExistingOccupancy` captures whether the cell was initially occupied and had no prior blocking registration. After spawn callbacks, publication requires a currently free cell, or that retained legacy eligibility with no other current blocker. A call that started free cannot publish again after a nested blocker already occupied the cell and was removed. A sequential replacement call also cannot gain legacy eligibility merely by removing its predecessor during its callback.

These locals add no public API, ownership token, lease, registry layer or external-occupancy policy. The first registration on an initially occupied cell retains its legacy state/occupied notification; the next sequential registration does not duplicate it. In the initially free nested case, the nested blocking call emits `OnCellStateChanged(true)` then `OnCellOccupied` before returning to the outer callback; the outer return emits neither again, even if that callback removes the nested object after publication. When the nested object is decorative, publication remains with the outer blocking call after its callback finishes. Existing current-cell/registration and post-field-callback checks remain unchanged.

## Exact owned documentation files

- `D/README.md`: coordinate rejection, signed merge saturation/custom policy, spawner generation behavior; existing Dice and generic Merge entry links retained.
- `D/FieldGenerator.md`: widened footprint arithmetic and atomic rejection, negative offsets, one coordinate style per draw; runtime and Inspector guidance retained.
- `D/GridPlacementService.md`: overflow failure, valid resolved targets from external anchors, stable requests and mutation-free predicates.
- `D/FieldObjectSpawner.md`: typed/scene APIs, detached metadata lifetime, nested first-blocker publication/removal, retained initially occupied first-registration policy, callback/event order and ownership limits in English plus a Russian generation/nested-spawn summary. Corrected its module backlink to the local `README.md`.

No shared/root documentation, sample asset/layout, asmdef, version metadata, original audit or original plan was edited in this phase.

## Regression source delivered — UNEXECUTED

New files:

- `E/Audit20261023NeoGridSystemBoundaryTests.cs` and `.meta`: **11 methods / 39 authored cases**.
- `P/Audit20261023NeoGridSystemLifecycleTests.cs` and `.meta`: **16 methods / 16 authored cases**. All seven original cases and six earlier `NI20-01` cases are retained; three source cases were added for the published-blocker-removal follow-up and initially occupied policy.

All **55 implementation acceptance cases are UNEXECUTED** against this source snapshot. These counts describe source attributes, not Unity discovery or passing tests. All 39 boundary cases are retained unchanged. The original `E/Audit20261023NeoGridSystemRegressionTests.cs` remains unchanged from the audit phase and is also pending integrated execution.

Boundary acceptance observes actual state/results/events:

1. Both public placement paths reject `int.MinValue + int.MinValue` on x, y and z without content/occupancy writes, output cells or notifications.
2. A valid first entry followed by an overflowing entry leaves the whole board untouched; the invalid sum formerly wrapped onto another valid coordinate.
3. Negative offsets and `int.MaxValue` anchors cancelled by negative offsets still place at the expected cells, preserving content and occupancy semantics.
4. `DiceBoard.CanPlace` / `Place` reject the overflow through the existing public footprint path without board/merge/cell notifications; a valid negative-offset Dice placement succeeds.
5. Factory increments cover both integer endpoints, extreme step magnitudes and ordinary signed results. Every notification snapshot sees the final saturated result and cleared neighbor.
6. The fallback calculator covers upper saturation and ordinary negative/positive values; previews leave content and notifications unchanged. Saturated groups terminate with normal minimum group size, and an ordinary two-group cascade produces the expected final board without a limit flag.

Lifecycle acceptance uses PlayMode destruction and public APIs:

1. Spawn A, regenerate, spawn B at the same coordinate, remove A: B remains occupied/tracked, A produces only its removal event and is actually destroyed after a frame.
2. Regenerate inside `OnObjectSpawned`: the created object remains detached/tracked without occupying or publishing occupancy for the new board.
3. Regenerate and spawn B inside `OnObjectRemoved`: B survives with its valid occupied notification and no later erroneous freed state/event.
4. Remove the instance inside its spawn callback: no registration/occupancy is resurrected and destruction completes.
5. Regenerate during occupied/free `OnCellStateChanged` callbacks: suppress the subsequent obsolete spawner transition event and preserve replacement occupancy.
6. Ordinary multiple blockers/decorative objects retain first/last-blocker behavior, the expected event order and registry cleanup.
7. `NestedBlockers_PublishOnceAndFreeAfterOuterThenInnerRemoval` and `NestedBlockers_PublishOnceAndFreeAfterInnerThenOuterRemoval` use a real one-shot `SpawnAt` inside `OnObjectSpawned`. Both original registrations and query results are observed inside the callbacks while the actual cell flag is still false; after the public calls return, that exact cell is occupied, both objects are tracked and a `RequireUnoccupied` placement is rejected without writes/events. Exact spawn/return/state/occupied order is asserted, followed by one last-free transition in each removal order and real deferred destruction.
8. `SequentialBlockers_PublishOnceAndFreeAfterOuterThenInnerRemoval` and `SequentialBlockers_PublishOnceAndFreeAfterInnerThenOuterRemoval` apply the same state, query, placement and full event assertions to ordinary sequential calls, including admitting the second object to an already occupied cell.
9. `NestedDecoration_DoesNotPublishOrRetainTheOuterBlockersOccupancy` and `NestedBlocker_PublishesWhileItsOuterDecorationIsStillInFlight` cover both blocking/decorative roles. Publication belongs to the valid blocking call; removal of the last blocker frees once and permits unoccupied placement while the decoration remains registered.
10. `NestedPublishedBlockerRemoval_DoesNotRepeatOuterOccupation`: the one-shot A spawn callback synchronously spawns B, waits for B's occupied publication, then removes B before A returns. Assert the exact original cell stays occupied, only A remains in both registries, the query agrees, `RequireUnoccupied` placement rejects and both occupied state/event occur once in the exact expected callback/return/removal order. B stays alive until deferred destruction completes; A stays occupied across that frame, then its removal produces one free state/event and deferred destruction.
11. `SequentialPublishedBlockerRemoval_DoesNotRepeatReplacementOccupation` asserts the same observable contracts when A has already published and B's sequential spawn callback removes A. This distinguishes a first registration on an already occupied cell from a callback that merely removes an existing published registration.
12. `InitiallyOccupiedCell_FirstRegistrationStillNotifiesAndAllowsASecondBlocker` sets the field flag before event subscriptions, then asserts the retained first-registration notification, admission/tracking of a second sequential blocker, no duplicate publication, placement rejection while occupied and one last free. Existing six pair cases retain their initially free setup; the helper's initial-state parameter is additive with default false.

Fixtures clean up their owned GameObjects; lifecycle cleanup yields for deferred destruction. They do not use reflection, source-text assertions or the unavailable NUnit serial-fixture attribute. They require the coordinator's isolated/exclusive test execution contract.

## Actual work and remaining integration evidence

Performed source reads, scoped source/document diffs, test-method/attribute enumeration and reference enumeration. Reviewed arithmetic range checks, callback revalidation points and the authored observable assertions. New metadata GUIDs each have exactly one enumerated occurrence under `Assets`: boundary `7d1200916f72485483b7ef3c816a308e`, lifecycle `8505ed69b3ca4bc6a24a5f96549d76aa`.

**Dependencies resolved:** source reference enumeration now finds `Neo.GridSystem` in the shared `P/Neo.Tests.Play.asmdef` at line 30, added by root. No additional reference or runtime API is awaited. `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` already references `Neo.GridSystem`, `Neo.Merge` and `Neo.GridSystem.Dice`. This worker did not edit either shared asmdef.

The `NI20-01` continuation and published-blocker-removal follow-up change exactly `S/FieldObjectSpawner.cs`, `P/Audit20261023NeoGridSystemLifecycleTests.cs`, `D/FieldObjectSpawner.md` and this receipt. They retain all prior source corrections, the seven original lifecycle methods, the six earlier nested/sequential/mixed cases and the complete 39-case boundary fixture. Current source review covered entry snapshots, callback/return/publication/removal order, registry membership, live-cell identity, both removal orders and the initially occupied positive policy. No current execution evidence is claimed.

**Execution remains prohibited until every source writer finishes.** No Unity/MCP, compiler, test/build runner, product checker/validator, service, installer, commit/push or child agent was launched. Historical checks are not results for this snapshot.

After the global source freeze, the coordinator must collect compilation and nonempty discovery/execution on installed Unity 6.3 and 6.6, with optional Odin coverage where installed, for the new fixtures plus existing GridSystem/Merge/Dice regressions. Check 100x100 coordinate-label appearance/settings and allocation behavior in the Editor; the source hoist is complete but native memory/FPS impact is unmeasured. Existing Match3 cancellation/reinitialization and reload lifecycle evidence from the plan remains central verification debt, not an additional implemented framework.
