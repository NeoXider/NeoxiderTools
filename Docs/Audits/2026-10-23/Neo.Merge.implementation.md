# Neo.Merge — implementation receipt

Status: **source-final**. Original actor/session: `neo-audit-1023-b04-03`; model/reasoning contract: `gpt-6.1-sol`, `max`. Target: `demo`, package `11.0.0`. Resumed 2026-10-24 after the resource interruption.

## Approved actions and completed work

The implementation request assigns no additional confirmed runtime defect. NativeReview-C's `Neo.Merge — reviewed` disposition accepts the earlier bounded correction. The original audit and plan remain historical evidence and were not rewritten.

| Action | Final source |
| --- | --- |
| MERGE-01, preserved | `Assets/Neoxider/Scripts/Merge/MergeResolver.cs`: reject a null selected result before group staging; application remains after all seeds/cascades, preserving zero setter calls on late evaluation failure. External destinations and valid value-type zero remain supported. |
| MERGE-01 public contract, preserved | `Assets/Neoxider/Scripts/Merge/MergeRequest.cs`: document the non-null selector result and pre-application failure contract. |
| MERGE-02, preserved | Seven resolver locals retain explicit types. No runtime rewrite was needed. |
| Assigned canonical documentation/navigation, finished | `Assets/Neoxider/Docs/Merge/README.md`: retain graph, C#, preview, cascade and scene/NoCode contracts; add exact assembly/namespace guidance, explicit 11.0 null-selector migration, current Grid increment saturation semantics, and links to existing FieldGenerator and DiceBoardService guides. |
| Regression source, preserved | `Assets/Neoxider/Tests/Edit/Merge/Audit20261023NeoMergeNullResultTests.cs` and its `.meta`: retain four cases in three methods, using local dictionaries and setter counters. No duplicate fixture or unsupported NUnit annotation was added. |

This resume changed only the owned README and this receipt. The earlier runtime/request/regression source matches the independent review fingerprints below. No shared tracker, configuration, other owner file, asset or sample layout was changed.

## Public API, compatibility and reuse

No additive public API was introduced. Existing `MergeResolver.Resolve`, request/result fields, defaults, pure assembly dependency boundary and scene adapters are preserved. The deliberate 11.0 behavior change is that a selector **returning** null throws `InvalidOperationException` instead of silently clearing the group. A null selector callback itself still selects the seed. Valid existing requests require no serialized-field or asset migration.

The implementation reuses the existing BFS, simulated values, deduplication and deferred setter loop. Custom setters can still partially apply state if they throw during commit; arbitrary callbacks are not transactional. No Unity/version API was introduced.

The substantial immutable plan/read-set/conditional-application design remains an **unimplemented proposal** in `Neo.Merge.plan.md`. It is not a required framework for this release and does not add a dependency on GridSystem or Dice.

## Authored acceptance — UNEXECUTED

Existing owned fixture:

- `Resolve_NullResultItem_PreservesSourceValues(false/true)`: preview and mutation reject null; source values remain unchanged and setter calls stay zero.
- `Resolve_NullResultAfterEarlierSeed_DoesNotCommitAnyGroup`: invalid later seed leaves every earlier staged group unapplied.
- `Resolve_NullResultDuringCascade_DoesNotCommitEarlierIterations`: second selection fails after a valid first iteration; two selections, zero setters and unchanged original values.

After every writer finishes, central verification must compile and discover these four cases plus the seven existing `MergeResolverTests` cases, then exercise GridMerge/Dice parity. Include outside-group destinations, integer zero, custom rules and exact cascade limits. A Grid adapter null-selector case should observe unchanged cells and zero content notifications. Existing Dice coverage must observe complete content/occupancy and intended event counts.

No Unity, compiler, test runner, product checker/validator, installer, service, commit, push or child agent was started by this resume. Historical audit checks are not claimed as current integrated verification. Current compile/EditMode/PlayMode and Unity 6.3/6.6 outcomes remain pending the coordinator's frozen-source verification; other supported versions remain explicitly unverified.

## Cross-owner dependencies

No pending additive API blocks this source-final receipt. `Neo.Editor.Tests` already references `Neo.Merge` and NUnit; no shared asmdef edit is requested. `Neo.GridSystem` owns `GridMergeRequest`/`GridMergeResolver` and its signed saturation/notification behavior. `Neo.GridSystem.Dice` owns occupancy and board/service events. Their source/docs were context only. Root owns integrated migration/release metadata and final verification.

## Actual source inspection and fingerprints

Read both implementation contracts, the exact current manifest entry, complete original audit/plan, current runtime correction, full owned fixture/metadata, pure asmdef and relevant public adapter documentation/source. Reviewed the scoped source/README diff and enumerated existing guide paths. Used `cmd.exe` reads, `rg`, Git's read-only diff/status and SHA-256 artifact bookkeeping; no acceptance process was executed.

| Artifact | Final SHA-256 |
| --- | --- |
| `Assets/Neoxider/Scripts/Merge/MergeRequest.cs` | `a0bdc7127b22f3c0645ae8db2502dc08fd1955e57471582012542e2e852379bd` |
| `Assets/Neoxider/Scripts/Merge/MergeResolver.cs` | `7a305eb80367f67760b74705107b6c2e5095cda9fcb835dc1019ea0099d65b8a` |
| `Assets/Neoxider/Tests/Edit/Merge/Audit20261023NeoMergeNullResultTests.cs` | `278e74e80346fdb92f6202af89f624bda0521d29220d26cdb35109924081e7a4` |
| `Assets/Neoxider/Docs/Merge/README.md` | `235a99dc071ec641fdcf4f39e24a7630b54aa9fb1729cd84430902a5e06b56e5` |
