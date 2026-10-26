# Neo.Merge: audit

Status: source audit and scoped static validation complete; compilation and EditMode execution pending coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Merge/Neo.Merge.asmdef`.
- Scope: all 5 manifest C# files reviewed completely (327 lines after edits); nested assemblies excluded.

## Scope

| File under `Assets/Neoxider/Scripts/Merge/` | Review and outcome |
| --- | --- |
| `MergeCascadeMode.cs` | Both modes and resolver dispatch reviewed; no defect demonstrated. |
| `MergeGroupResult.cs` | Group snapshots, cleared items, seed/result identity, source/result values reviewed; public mutable result model preserved. |
| `MergeRequest.cs` | Every callback, default, required field, empty predicate, cascade limit, mutation option reviewed; documented non-null result-selector contract. |
| `MergeResolver.cs` | Entire validation, BFS, null/filter handling, value overrides, external result targets, changed-item deduplication, deferred writes, cascade loop and exact-limit check reviewed; fixed null-target data loss. |
| `MergeResult.cs` | Group/changed-item ownership, `HasChanges`, cascade-limit reporting reviewed; no defect demonstrated. |

Read-only context: `Neo.Merge.asmdef`, existing `Tests/Edit/Merge/MergeResolverTests.cs` (all seven tests), `Neo.Editor.Tests.asmdef`, `GridMergeResolver.cs`, `GridMergeRequest.cs`, and the previous Merge audit. The old null-selector lead was independently verified against current source. No other worker's source, tests, docs, assembly definitions or configuration were edited.

### Runtime, ownership and compatibility review

- The engine is plain C#: `noEngineReferences: true`, no assembly references, Unity objects, subscriptions, unmanaged resources, persistent caches or runtime logs. All resolver state is local to a call; enumerators are consumed through `foreach`.
- BFS rejects null seeds/neighbors, supports null neighbor enumerables and cycles, applies item and simulated-value filters, and uses default item identity. `Items` supplies fallback seeds rather than a membership boundary. External result targets are an existing supported feature and remain supported.
- Overrides connect dry runs, later seeds and cascades without invoking setters during evaluation. All final writes occur after seed enumeration. Null-result failure now preserves this staging boundary even after prior successful groups. Setter exceptions can still partially apply writes; no transactional guarantee existed.
- `MinGroupSize` is normalized to at least 1; non-positive cascade limits allow one iteration. The cascade flag probes whether another qualifying group actually remains. Infinite input enumerables and unstable item equality/hash codes remain caller errors, not problems solved by the cascade guard.
- Empty filtering is optional: with no `IsEmptyValue`, even `EmptyValue` is eligible for matching. This is existing configurable behavior, including a deliberate always-equal cascade regression; no incompatible default was introduced.
- Required callbacks and `Items` are validated; `SetValue` is required only for mutation. Validation fills the equality callback and normalizes the request, so concurrent operations should use separate requests. Planning callbacks must not mutate source state themselves.
- No public signatures, fields, enum values, defaults or serialized names were changed. The only behavioral tightening rejects an invalid null target. No Unity-version-dependent API or conditional compilation was introduced. No package, sample, prefab or scene changes were made.
- This assembly has no Inspector/NoCode layer; existing GridSystem and Dice adapters provide scene integration. Their source was read only for the dependency boundary.

## Findings and fixes

### MERGE-01 — P2: null result selection silently destroys a matched group

**Trigger:** a valid nonempty group and a `SelectResultItem` callback returning null for a reference-type item (also applicable to a nullable item with no value).

**Source evidence:** `MergeResolver.ResolveSeed`, formerly the selection and write-staging block, now `Assets/Neoxider/Scripts/Merge/MergeResolver.cs:61`. Previously every group item compared unequal to the null result and was staged as `EmptyValue`; the external-result branch explicitly skipped null. The group was nevertheless appended as a successful merge and, in mutation mode, all staged clears were applied.

**Concrete pre-fix source trace:** values `{ A:1, B:1, C:1 }`, one connected group, seed `A`, minimum 3, empty 0, merged value 2, null selector, `Mutate = true` produced one reported group with `ResultItem = null`, `ResultValue = 2`, and final values `{ A:0, B:0, C:0 }`. The merged value had no destination. This is a manual control-flow trace, not a claimed executed C# reproduction.

**Completed fix:** reject a null result with `InvalidOperationException` immediately after selection (`MergeResolver.cs:64`), before merged-value calculation or group staging. Because commit remains after all seed evaluation (`MergeResolver.cs:31`), earlier seeds and cascade iterations are not written either. Valid result items outside the group continue to receive the merged value. Added XML contracts to `Resolve` and `MergeRequest.SelectResultItem`.

**Regressions added:** `Assets/Neoxider/Tests/Edit/Merge/Audit20261023NeoMergeNullResultTests.cs` contains three test methods / four NUnit cases:

- `Resolve_NullResultItem_PreservesSourceValues(false/true)`: dry run and mutation reject null; every original value is retained and setter calls remain zero.
- `Resolve_NullResultAfterEarlierSeed_DoesNotCommitAnyGroup`: a valid disconnected group is evaluated before the invalid selection; neither group is applied.
- `Resolve_NullResultDuringCascade_DoesNotCommitEarlierIterations`: the second cascade selection fails after a valid first merge; all original values remain and no setter is called.

Assertions observe source state and setter effects as well as the exception. Existing external-target, integer-item, custom-rule and exact-limit tests remain unchanged. New tests have not been executed under this worker's runner restrictions.

### MERGE-02 — P3: explicit-type contract

Replaced the seven existing `var` locals in the already modified resolver with explicit types. This resolves the historical style finding without changing runtime behavior. A scoped static scan found no implicit local declarations in the five source files or new fixture.

Кратко: устранено тихое удаление всей группы при null-результате selector; изменения предыдущих групп и каскадов также остаются неприменёнными при такой ошибке.

## Documentation

Updated the existing canonical `Assets/Neoxider/Docs/Merge/README.md`, preserving its English style and existing module links. It now explains required callbacks, directed graph traversal, optional empty filtering, result selection and null failures, default identity, callback ownership, deferred mutation, setter failure limits, cascade boundaries, and duplicate seeds. Added an explicit-type C# preview/apply example and a scene/NoCode section pointing to existing GridSystem/Dice adapters. No redundant guide or new documentation asset was created. Both existing relative documentation links were verified to resolve.

## Substantial improvement proposal

### Immutable merge plans with explicit value changes (proposal only)

**Problem:** a preview returns groups and affected items but no canonical final-value map. A result item can participate in multiple cascade groups. Callers implementing ghost previews, undo, replay or server-authoritative confirmation must reconstruct changes or call `Resolve` again. Recalculation can select a different result after source state or callback state changes. Generic setters also do not provide an atomic commit boundary for scene notifications.

**Proposed design / public API:** add an engine-independent `MergePlanner.Create<TItem,TValue>(MergeRequest<TItem,TValue>)` returning an immutable `MergePlan<TItem,TValue>` with read-only groups, cascade-limit status and `IReadOnlyList<MergeValueChange<TItem,TValue>> Changes`. Each change records item, original value and final value once, including external targets and items revisited by cascades. Add an application API that checks all expected original values with a supplied value comparer before writing and reports a typed conflict when source state has changed. Offer an optional caller-owned batch/transaction adapter for applications that require atomic state updates and one final notification. A plain `Action<TItem,TValue>` path must explicitly retain its current nontransactional failure semantics; the engine cannot roll back arbitrary side effects.

Reuse the current BFS and override planner rather than creating a second matching engine. Keep graph topology/callback stability as an explicit caller precondition; original-value checks alone cannot detect topology changes.

**Compatibility / migration:** introduce additive types/methods first; retain `MergeRequest`, current public result lists, `Resolve` and all defaults. Existing callers require no migration. After parity tests, `Resolve` may internally plan/apply while returning the legacy result shape. Optional GridSystem integration belongs to its owner and must preserve its notification settings. No scene references or Unity dependency enter the pure assembly.

**Acceptance scenarios:** preview causes zero writes; a multi-stage cascade exposes one final change per item; an external target is included; applying an unchanged plan matches legacy final state; changed original values produce a conflict before any setter call; a failed transactional adapter publishes no partial board notification; null selection cannot produce an applicable plan; existing custom equality, seed-order and exact-limit tests keep their behavior.

**Effort / risk:** approximately 2–4 developer days plus integration QA, assuming the current finite graph/callback contracts. Medium risk: snapshot allocation, legacy mutable-list compatibility, item identity, and transaction ownership need explicit decisions. Start with planner/preview output; transactional adapters can ship separately. This proposal has not been implemented.

## Verification and remaining risks

### Checks actually executed

- Read all five manifest source files and compared the current C# inventory to that entry: exact match, no nested assembly included.
- Scoped Python static inspection exited 0: strict UTF-8 decoding and no replacement characters; explicit local types; balanced C# delimiters after stripping comments/strings; existing `Neo.Merge` and NUnit references in the EditMode assembly; engine-independent assembly settings; both relative README links. A final six-file check also verified trailing whitespace and final newlines, including the untracked fixture, metadata and report. Delimiter checks are not compilation.
- New fixture discovery inspected statically: three test methods, four cases. Its unique GUID `e8b9f84726ac4eadb2343cab801d6d85` was searched across `Assets/**/*.meta`; the only match is its own `.cs.meta`.
- Reviewed the scoped source/documentation diff. `git diff --check --` over owned source/docs, the new fixture/meta and this report completed without whitespace errors. Git emitted its normal LF-to-CRLF working-copy warning for `MergeRequest.cs`; encoding was checked independently and no normalization was performed.
- Resolver SHA-256 after changes: `7a305eb80367f67760b74705107b6c2e5095cda9fcb835dc1019ea0099d65b8a`; new fixture SHA-256: `278e74e80346fdb92f6202af89f624bda0521d29220d26cdb35109924081e7a4`.

### Pending central validation / remaining risks

Unity import/compilation, EditMode execution, PlayMode and builds were not run, as explicitly prohibited for this worker. The coordinator should execute the four new cases, the existing seven Merge resolver tests, and the affected GridMerge/Dice regressions. No assembly-reference change is required: `Neo.Editor.Tests` already references `Neo.Merge` and NUnit.

Cross-boundary integration note: `GridMergeResolver` forwards `SelectResultCell` directly to the core, so a custom callback returning null will now fail before any cell writes instead of silently clearing cells. No GridSystem edits are necessary for the core correction; its owner/coordinator can add an adapter-specific regression if desired. No additional cross-boundary defect was demonstrated.

Remaining contractual limits are documented rather than presented as completed fixes: arbitrary setter failures can partially apply state; infinite enumerables are not bounded by the cascade limit; mutable item identity is unsupported; caller callbacks can mutate external state; preview results currently lack an immutable, directly applicable change plan. Compilation on Unity 6.0–6.4 and 6.5+, and integration behavior, remain unverified by execution in this worker.
