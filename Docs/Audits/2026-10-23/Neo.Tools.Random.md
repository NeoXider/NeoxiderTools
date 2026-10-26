# Neo.Tools.Random: audit

Status: source audit and scoped fixes complete; Unity compilation and NUnit execution await central validation.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Random/Neo.Tools.Random.asmdef`.
- Scope: 3 C# files; nested assemblies excluded.

## Scope

All three manifest-owned C# files were reviewed in full. There are no nested assemblies inside this inventory.

| File | Review coverage | Outcome |
| --- | --- | --- |
| `Assets/Neoxider/Scripts/Tools/Random/ChanceManager.cs` | Constructors; entry metadata; cached sums; collection mutation; normalization and locked/zero weights; all selection overloads; custom providers; deep copy; validation; serialization repair; ID assignment | Fixed normalization cache invalidation, boundary selection, numeric overflow/non-finite input handling, and destructive self-copy. |
| `Assets/Neoxider/Scripts/Tools/Random/ChanceSystemBehaviour.cs` | Inline/asset initialization; editor validation; all notifying/non-notifying APIs; event order and per-index dispatch; last-result state; reentrancy; debug logging | Fixed the returned entry during reentrant notification. |
| `Assets/Neoxider/Scripts/Tools/Random/Data/ChanceData.cs` | ScriptableObject initialization/validation; shared manager semantics; all mutation adapters; generated-index event; asset/scene separation | No demonstrated defect requiring a source change. Clarified shared versus copied tables and asset event scope in documentation. |

The runtime has no coroutine/task/subscription ownership, scene searches, or unmanaged resources. The random-provider delegate is caller-owned. Debug sampling is editor-only, opt-in, resets its flag, and uses gated `NeoDiagnostics`; no new logging was introduced. No version-specific object-ID APIs occur in these files. Runtime still depends only on the existing Unity, `Neo.PropertyAttribute`, and `Neo.Extensions` references.

Read-only context: existing `ChanceManagerTests.cs`, `Neo.Editor.Tests.asmdef`, runtime asmdef, all five owned documentation pages, matching `neoxider-tools` reference sections, and the relevant weighted-index implementation in `RandomExtensions.cs`. Existing tests and shared definitions were not edited.

## Findings and fixes

### RND-01 — Medium: direct normalization leaves a stale cached total

- **Trigger / evidence:** Create a raw table `[2, 3]`, read `TotalWeight` (`5`), then call `Normalize(1)`. Entry weights become approximately `[0.4, 0.6]`, but the old cached total stays `5`. `GetNormalizedWeight(0)` becomes `0.08`, and `GetId(0.1)` selects index `1` rather than `0`. The same problem affects enabling `AutoNormalize`, changing `NormalizeTarget`, zero redistribution, and the early return that zeros unlocked weights under an over-budget locked sum.
- **Source:** `ChanceManager.Normalize(float)` (current line 305), previously missing invalidation on every path.
- **Fix:** Invalidate before normalization or any early return. All existing property setters reuse that path.
- **Regressions:** `Normalize_RefreshesCachedTotalProbabilitiesAndSelection`, `Normalize_LockedWeightExceedsTarget_RefreshesTotalAndDisablesUnlockedEntry`, `Normalize_ZeroWeightsBecomeSelectableAfterEvenRedistribution`, `AutoNormalize_EnablingItRefreshesCachedTotal`, `NormalizeTarget_ChangingItRefreshesCachedTotal`.

### RND-02 — Medium: a zero roll selects a disabled entry

- **Trigger / evidence:** On raw weights `[0, 1, 0]`, `GetId(0)` compares `0 <= 0` at index `0` and returns the zero-weight entry. A custom provider returning zero reproduces the same issue through `TryEvaluate`. Inclusive bucket boundaries also select the previous bucket on an exact boundary.
- **Source:** `ChanceManager.GetId(float)` (current line 220).
- **Fix:** Skip nonpositive weights, use exclusive upper boundaries, and fall back to the last positive-weight index for the inclusive upper endpoint.
- **Regressions:** Five endpoint/boundary cases plus `RandomProvider_ZeroNeverSelectsDisabledLeadingEntry`.
- **Compatibility:** Exact positive bucket boundaries now select the next positive bucket. `0` and `1` remain valid, and finite out-of-range values remain clamped. Return values remain list indices, not custom IDs.

### RND-03 — Medium: finite weight arithmetic can overflow and destroy selection

- **Trigger / evidence:** The constructor with `[float.MaxValue, float.MaxValue]` previously computed an infinite float sum, scale zero, and weights `[0, 0]`. With auto-normalization disabled, large raw sums produce zero normalized probabilities and can return a trailing zero-weight entry for roll zero (`0 * infinity` is `NaN`). A subnormal positive weight normalized toward a large finite target also overflows the old float scale.
- **Source:** Cached total and `GetNormalizedWeight` (current lines 78–112), selection (220), normalization (305).
- **Fix:** Keep the nonserialized sum cache and intermediate normalization/selection arithmetic in `double`. Preserve all public float signatures and serialized float weights. Removed the now-unused LINQ dependency in this file and one temporary list from normalization.
- **Regressions:** `Normalize_LargeFiniteWeightsKeepTheirProportions`, `Normalize_SubnormalWeightCanReachLargeFiniteTarget`, `RawLargeFiniteWeightsHaveValidProbabilitiesAndDeterministicRolls`.
- **Compatibility:** A raw sum above `float.MaxValue` still returns positive infinity through the public float `TotalWeight`, while selection and `GetNormalizedWeight` remain valid. Documentation explicitly describes this representational limit.

### RND-04 — Medium: non-finite inputs poison the table or fabricate a result

- **Trigger / evidence:** An infinite weight or normalization target produces infinite/invalid arithmetic rather than usable probabilities. `GetId(float.NaN)` previously fell through to the final index and reported a selection. Existing diagnostics did not identify non-finite serialized weights.
- **Source:** Weight constructors/setter and sanitization, `NormalizeTarget`, `Normalize(float)`, `GetId(float)`, `ValidateWeights` (current lines 438–445).
- **Fix:** Share one private weight sanitizer across entry writes and arithmetic. Negative/non-finite weights become zero. Non-finite explicit normalization targets behave like negative targets (zero); non-finite `NormalizeTarget` assignments use its finite minimum `0.001`. `Sanitize` also repairs a deserialized target. `NaN` rolls return no result, including through a custom random provider. Diagnostics report non-finite serialized values.
- **Regressions:** `GetId_NaNReturnsNoResultThroughAllEvaluationPaths`; three invalid-weight cases; two invalid explicit-target cases; two invalid configured-target cases.
- **Behavioral detail:** Existing even redistribution can revive an entirely zero table during auto-normalization. This is preserved and documented; disable redistribution or auto-normalization to retain an entirely disabled table.

### RND-05 — Medium: self-copy destroys the configuration

- **Trigger / evidence:** `manager.CopyFrom(manager)` clears `entries` before enumerating that same list, removing every entry and its metadata.
- **Source:** `ChanceManager.CopyFrom` (current line 372).
- **Fix:** Treat self-copy as a no-op, like the existing null-source no-op. Copying from another manager still creates new entries and retains the destination random provider.
- **Regression:** `CopyFrom_SelfPreservesEntriesMetadataAndSampling` checks entry identity, labels, locks, IDs, notes, total, and deterministic selection.

### RND-06 — Medium: a listener replaces the entry returned by the outer roll

- **Trigger / evidence:** A roll selects index `0`; its event listener calls `SetResultAndNotify(1)`. The outer `EvaluateAndNotify()` previously returned `LastSelectedEntry` after dispatch, so callers received index `1`'s entry rather than the entry selected by their roll.
- **Source:** `ChanceSystemBehaviour.EvaluateAndNotify()` (current line 138).
- **Fix:** Capture the selected entry before invoking listeners and return that local reference. Last-result properties still describe the most recent notification, including a nested call. Event order and serialized event fields are unchanged.
- **Regression:** `EvaluateAndNotify_ReentrantListenerDoesNotReplaceReturnedRoll` observes the returned entry, nested last-result state, and two actual notification/completion events; its temporary GameObject is destroyed in `finally`.

## Documentation

Updated the five existing owned pages, preserving English as the public documentation language:

- `Assets/Neoxider/Docs/Tools/Random/README.md`: canonical entry, runtime-first example, batch/auto-normalization rules, zero-table behavior, scene and asset setup.
- `ChanceManager.md`: replaced nonexistent/wrong signatures (`int Evaluate`, labeled `AddChance`, `SetChance`, `ClearChances`) with the real API; documented deterministic boundaries, raw large totals, invalid inputs, locks, direct-entry invalidation, IDs, and copying.
- `ChanceSystemBehaviour.md`: actual add-component path, inline/asset initialization, NoCode event wiring, C# subscription examples, event sequence, failed rolls, and reentrant result semantics. Corrected the module-root link.
- `Data/README.md`: actual asset purpose and relationship to the scene adapter.
- `Data/ChanceData.md`: asset creation rather than adding a component; direct/shared versus independent C# use; event scope; keeping scene references in the component.

Кратко: исправлены ошибки расчёта вероятностей, выбор записи с нулевым весом, потеря таблицы при копировании в себя и неверный возвращаемый результат вложенного вызова. Публичные API и сериализованные поля сохранены.

## Substantial improvement proposal

**Proposed, not implemented: an immutable prepared table with a typed selection result.**

**Problem:** The current mutable `Entry` objects require manual `InvalidateTotalWeight` after direct weight changes. Selection is linear in entry count, and normalization allocates a temporary list. Games often keep parallel reward lists and treat selection indices as identity; reordering entries can silently change the reward wired to a position. Custom IDs exist but are returned only indirectly through mutable entry objects. These limitations become significant for large loot tables, deterministic simulations, and reused asset configurations.

**Architecture / public API:** Add a plain C# `ChanceTableSnapshot` containing immutable copies of positive cumulative weights, original indices, custom IDs, and normalized probabilities. Expose `ChanceManager.Compile()` to create a snapshot explicitly and `ChanceTableSnapshot.TryEvaluate(float randomValue, out ChanceSelection selection)` to sample it by binary search. `ChanceSelection` is a readonly value containing `Index`, `CustomId`, and `NormalizedWeight`. Sampling takes an explicit random value, performs no allocation, and has no Unity object or global RNG dependency. Compiling must not mutate source order, IDs, or weights. If a caller requests unique-ID lookup, compilation reports negative/duplicate IDs with an explicit validation result instead of silently changing identity. An optional scene adapter can publish the custom ID while retaining existing index events.

**Compatibility / migration:** Keep existing serialized entries, indices, methods, UnityEvents, and asset formats. Existing users retain the mutable manager and its documented invalidation rules. Performance/determinism-sensitive code opts into compiling once after configuration or a batch edit and reuses the snapshot. Later source edits cannot change an existing snapshot; callers explicitly compile a replacement. Do not introduce implicit snapshot caching until direct `Entry` mutations can reliably notify owners across deserialization. Any future ID event is additive and distinct from `OnIdGenerated`.

**Acceptance scenarios:**

1. `[0, 2, 6, 0]` maps rolls `0`, `0.25`, and `1` to original indices `1`, `2`, and `2`, retaining the selected custom ID and probability.
2. Mutating/removing source entries or editing an asset after compilation does not change the snapshot; recompilation reflects the change.
3. Large finite weights and subnormal weights preserve usable proportions; `NaN` inputs and entirely disabled tables return an explicit failure.
4. Reordering source entries changes only their reported indices after recompilation; valid custom IDs remain stable. Duplicate-ID lookup reports diagnostics without rewriting the source.
5. Equal snapshots and an equal sequence of random values produce identical results in C# tests. Repeated sampling allocates zero bytes and scales logarithmically.
6. Existing serialized assets, index events, and the current public test suite continue to work without migration.

**Effort / risk:** Approximately 2–3 developer days including focused tests, benchmark evidence, and documentation; medium risk around exact floating-point boundaries, source-to-snapshot mapping, and user expectations about snapshot freshness. Avoid alias sampling initially: binary search is smaller, deterministic, and preserves one random-value input per selection.

## Verification and remaining risks

### Checks actually executed

- Read-only Python inventory check: manifest contains exactly the three C# files found under the owned runtime assembly; **3/3 reviewed**. Post-change line counts / SHA-256 prefixes: `ChanceManager.cs` **571 / a8e17d11e67a8308**, `ChanceSystemBehaviour.cs` **210 / ade97172d471b2ee**, `ChanceData.cs` **67 / fc52f09d1e5d14da**.
- Compared public declaration lines and serialized field names/types in all three runtime files against `ca198556`: unchanged. This is a static compatibility check, not a compiler/API compatibility proof.
- Strict UTF-8 decoding and replacement-character checks on all ten scoped package source/docs/test/meta files; explicit local typing and lexical delimiter checks on the runtime inventory and new test source: passed. Report encoding checked separately after writing.
- Relative Markdown links in all five owned public pages resolve on disk.
- Existing `Neo.Editor.Tests` already references `Neo.Tools.Random` and NUnit; no shared asmdef changes required.
- New test filename uses the exact manifest prefix. Added **16 test methods / 24 expanded NUnit cases** in `Assets/Neoxider/Tests/Edit/Tools/Random/Audit20261023NeoToolsRandomRegressionTests.cs`, with a unique GUID `.meta`. Repository GUID search matched only that metadata file.
- Read-only IEEE-754 arithmetic inspection confirmed old `[2, 3]` normalization leaves a `0.08` probability if total `5` is reused; `[0, 1, 0]` with roll zero selects weight zero under the old comparison. A float32 arithmetic check confirmed two `float.MaxValue` weights overflow their total to infinity and normalize to `[0, 0]`, while double arithmetic produces `[0.5, 0.5]`. The initial Python `struct.pack` overflow representation attempt raised `OverflowError`; a corrected `ctypes.c_float` inspection produced the stated IEEE-754 values. These are numerical demonstrations, not execution of the Unity/C# implementation.
- Scoped `git diff --check -- <owned source/docs/new tests/report>` passed. New untracked test/meta/report whitespace was also inspected directly because ordinary `git diff` omits untracked content.

### Central validation still required

Unity/import/compiler, NUnit/Edit Mode, Play Mode, and builds were **not run**, as required by the worker contract. Run the new `Neo.Editor.Tests.Audit20261023NeoToolsRandomRegressionTests` fixture and existing `ChanceManagerTests`, including the component event/reentrancy case, during central validation. Unity 6.0–6.4 and 6.5+ compilation remains unverified. No scenes, prefabs, sample layouts, package/settings files, shared configs, or existing tests were changed; no installs, child agents, or commits were made.

Known retained contracts: direct `Entry.Weight` mutation needs explicit total invalidation/normalization; `Entries` is read-only by contract rather than by a defensive copy; callers must give a nonnegative index to `GetOrAddEventForIndex` and a valid index (or `-1`) to `SetResultAndNotify`. All-zero redistribution and locked sums exceeding the target remain intentional. Serialized null entry objects inserted by unsupported collection mutation are not newly supported.

### Cross-boundary action for coordinator

At inspection, `Assets/Neoxider/Scripts/Extensions/RandomExtensions.cs:221` (`GetRandomWeightedIndex`, owned by `Neo.Extensions`) also accumulated finite weights in `float` and accepted non-finite weights. `[float.MaxValue, float.MaxValue]` overflows the sum and drives the subtraction walk toward the last positive entry rather than a balanced choice. The existing last-positive fallback already protects ordinary endpoint-zero cases there, so replacing this module with that extension would not solve numeric/provider/lock requirements. Route the overflow/non-finite finding to that assembly's worker and reconcile against concurrent edits. No dependency source or tests were changed here.
