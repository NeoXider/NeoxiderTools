# Neo.Tools.Random — implementation receipt

Status: **source-final** for the approved bounded scope. No source dependency blocks completion. Runtime execution and integrated acceptance remain pending the global writer barrier.

Target: branch `demo`, package `11.0.0`; baseline `ca198556`. Read `IMPLEMENTATION-CONTRACT.md`, the dedicated implementation request, original audit/plan, current owned source, all five public documentation pages and the complete regression fixture. Approval metadata at resume recorded `globalReviewApproved: true`, `sourceFreeze: false`, `executionAllowed: false`; the branch read returned `demo`. No shared tracker was changed.

## Approved actions and work actually performed

The dedicated request assigns no additional confirmed runtime defect and requires preserving the coherent earlier patch, finishing canonical documentation/navigation, retaining the substantial improvement as a proposal, and reporting source-final honestly. Its NativeReview-B section confirms the complete 24-case fixture and both source diffs were reviewed. There are no additional numbered runtime action IDs to implement.

Retained completed audit action IDs:

| Action | Source-final implementation retained |
| --- | --- |
| RND-01 | `ChanceManager.Normalize(float)` invalidates the total before every mutation/early return, including indirect calls from normalization properties. |
| RND-02 | `GetId(float)` skips nonpositive weights, uses exclusive cumulative upper boundaries and a last-positive endpoint fallback. |
| RND-03 | Nonserialized cached sum, selection and normalization intermediate arithmetic use double precision; public float signatures remain. |
| RND-04 | Entry weights and normalization targets have explicit finite-value sanitization; NaN rolls return no result; diagnostics identify non-finite serialized weights. |
| RND-05 | `CopyFrom(this)` is a no-op and cannot clear its own entries. |
| RND-06 | `ChanceSystemBehaviour.EvaluateAndNotify()` captures its selected entry before synchronous notifications, so nested calls cannot replace its return value. |

No runtime patch was repeated during this resume. The three runtime files match the source fingerprints from the completed audit; `Data/ChanceData.cs` remains unchanged. The existing regression source and its metadata were preserved, without duplicate tests or unsupported NUnit attributes.

Finished documentation in this implementation phase:

| Exact file | Completed content |
| --- | --- |
| `Assets/Neoxider/Docs/Tools/Random/README.md` | Added the parent Tools backlink while retaining the canonical runtime, component and data-asset entry links. |
| `Assets/Neoxider/Docs/Tools/Random/ChanceManager.md` | Documented constructor/bulk configuration, repeated normalization/ID-repair cost, linear sampling and the temporary normalization list. Added a concrete direct-entry edit plus explicit invalidation example. Bulk construction is not described as guaranteed linear. |
| `Assets/Neoxider/Docs/Tools/Random/ChanceSystemBehaviour.md` | Clarified synchronous listener execution and exception propagation: later notifications depend on earlier listeners finishing. Existing event ordering, asset copying and nested result semantics remain documented. |
| `Assets/Neoxider/Docs/Tools/Random/Data/README.md` | Added direct links to the C# manager contract, scene event adapter and canonical Random module. |

`Assets/Neoxider/Docs/Tools/Random/Data/ChanceData.md` retains the completed asset creation, sharing/isolation, event scope and direct C# copy example. No redundant guide was created. Original audit and plan remain historical evidence and were not edited.

## Public API and compatibility

No new public API, assembly reference or serialized field was added in this implementation phase. Existing `GetId`, `GetChanceId`, `Evaluate`, `TryEvaluate`, normalization/copy methods and wrapper events retain their signatures. Serialized names, UnityEvents, existing metadata GUIDs, scenes, prefabs, ScriptableObject layouts and sample directories remain unchanged.

Deliberate behavior corrections already present in the retained patch: an exact bucket boundary selects the next positive bucket; a zero-weight entry is never selected while a usable table exists; NaN rolls return `-1`/null/false; negative or non-finite entry weights sanitize to zero; invalid explicit normalization targets sanitize to zero, while invalid configured `NormalizeTarget` values use its minimum `0.001`. Self-copy preserves the table. A reentrant outer call returns its own selected entry; last-result properties still describe the latest notification. The new listener-exception documentation describes existing synchronous behavior rather than introducing rollback or exception isolation.

Retained constraints are explicit in public docs: direct `Entry.Weight` edits require `InvalidateTotalWeight()` or normalization; all-zero auto-normalization can redistribute weights; locked sums can exceed the target; public float `TotalWeight` can be positive infinity for a valid raw double sum; IDs are metadata, whereas selection/events use list indices.

## Regression source and acceptance — UNEXECUTED

Existing authored fixture: `Assets/Neoxider/Tests/Edit/Tools/Random/Audit20261023NeoToolsRandomRegressionTests.cs`, class `Neo.Editor.Tests.Audit20261023NeoToolsRandomRegressionTests`. It contains **16 methods / 24 expanded NUnit cases**, with explicit deterministic roll providers where randomness is needed, no global RNG replacement, and exact temporary component cleanup in `finally`.

| Authored cases | Expected observable output, not an execution result |
| --- | --- |
| Five cached-normalization/property/locked/zero-redistribution cases | Totals and normalized probabilities reflect new weights; deterministic indices remain correct. |
| Five clamped endpoint/exact-boundary cases plus provider-zero case | No zero-weight entry is selected; valid index/entry identity matches the chosen bucket. |
| NaN roll case | All checked index/entry/try-evaluation paths report no result. |
| Three large/subnormal arithmetic cases | Finite weights retain usable proportions, a subnormal reaches a finite target, and raw float-total overflow does not break selection. |
| Three invalid-weight and four invalid-target cases | Invalid inputs do not poison valid probabilities or create NaN weights/totals. |
| Self-copy case | Count, entry identity, labels, locks, IDs, notes, total and sampling survive self-copy. |
| Reentrant component case | Outer entry is preserved; nested last-result state is visible; two notifications/completions are observed. |

No new lifecycle fixture or shared test-assembly change was introduced. Central acceptance must still observe `ChanceData` copying at `Awake`, isolation after instance edits, component-versus-asset event scope, ordinary event order and empty-table notifications. These remain acceptance/evidence debt, not claims of additional authored fixtures. The existing `ChanceManagerTests` suite also remains part of central verification.

## Dependencies and deferred improvement

There is **no pending additive API dependency** for the retained fixes or documentation. Runtime continues using existing `Neo.PropertyAttribute` and `Neo.Extensions` references. Extension-owned numeric changes and primary test infrastructure remain with their owners.

`ChanceTableSnapshot` / typed `ChanceSelection` / `Compile()` remain substantial **unimplemented proposals** in the historical plan, not shipped APIs. Before that optional work is selected, coordinate the cumulative sampler with `Extensions-Numeric-Time-Random` and its proposed `WeightedDistribution`; avoid two independent numerical kernels while preserving manager sanitization, metadata, original-index mapping and explicit source-update semantics. No automatic cache-freshness guarantee or allocation/performance improvement was invented for this source phase.

## Current evidence and execution barrier

Actual work during this resume was limited to UTF-8 source/document reads, approval/reference enumeration, fingerprint bookkeeping, owned Markdown edits, scoped diff inspection and this receipt. No Unity, compiler, NUnit/test runner, product/package checker, install, service, commit, push or child actor was started. Earlier static-check receipts in the audit were not rewritten or presented as fresh integrated validation.

Preserved source fingerprints (SHA-256):

| File | SHA-256 |
| --- | --- |
| `Assets/Neoxider/Scripts/Tools/Random/ChanceManager.cs` | `a8e17d11e67a8308164b488c0767ab191d76b8ad1606144dd71db5fbf94a68dd` |
| `Assets/Neoxider/Scripts/Tools/Random/ChanceSystemBehaviour.cs` | `ade97172d471b2eef1317ecfaf56908d740683b603144b721b82a86b1cf09c75` |
| `Assets/Neoxider/Scripts/Tools/Random/Data/ChanceData.cs` | `fc52f09d1e5d14da88cdafcf3553fca306220cf07174c68991e4410bc731c7c9` |
| `Assets/Neoxider/Tests/Edit/Tools/Random/Audit20261023NeoToolsRandomRegressionTests.cs` | `5740d6eee01f141a3d789b40ac6b6582ae6488a14262e2543f683215c4bfd15e` |

Root may begin frozen-source verification only after every writer finishes. Required integrated evidence includes installed Unity 6.3 and 6.6 compilation/discovery and the relevant EditMode/PlayMode acceptance above; broader unsupported Unity branches remain explicitly unverified. Source-final is not a compile/test pass. Package version, migration/changelog integration, shared test references, final product checks and any later root push remain coordinator-owned.
