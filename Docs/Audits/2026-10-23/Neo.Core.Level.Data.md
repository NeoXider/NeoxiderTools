# Neo.Core.Level.Data: audit

Status: source audit complete; fixes, regression cases and owned documentation written. Static checks passed. Unity compilation and test execution remain reserved for the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Core/Level/Data/Neo.Core.Level.Data.asmdef`.
- Scope: 2 C# files; nested assemblies excluded.

## Scope and complete inventory review

Проверены полностью оба C# файла из manifest; границы владения соблюдены. Исправлены расчёты XP и границ Curve, добавлены регрессии. Unity и тестовые раннеры не запускались по контракту.

| Owned source | Review and disposition |
| --- | --- |
| `Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs` | Reviewed every field, property and method: Formula/Curve/Custom dispatch, legacy `CurveType` mapping, invalid mode fallback, both XP queries, caps, all numeric conversions, `SetLinear`, mutable configuration exposure, missing curves/tables and lookup. Focused fixes below. |
| `Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveEntry.cs` | Reviewed both constructors, field defaults, serialization attributes and both interface properties. Constructor inputs clamp level to at least one and XP to nonnegative values. No demonstrated defect requiring an edit. Inspector `[Min]` does not validate external serialized input; documented rather than changing serialized data or getters. |

Inventory independently matched manifest: **2/2 C# files**, no nested asmdef under this assembly root. `Neo.Core.Level.Components`, `Neo.Core.Level.Bridge` and `Neo.Core` were not edited.

Context inspected: the assembly definition; `ILevelCurveDefinition`, `ILevelCurveEntry`, `LevelCurveEvaluator`, `LevelModel`, `LevelComponent`; the existing definition tests and Editor test references; the canonical Level Curve Definition guide. Existing tests cover basic integer thresholds and integration, but not fractional preview thresholds, fractional curve endpoints, the scan limit or the large-XP cases added here.

### Other audited contracts

- **Data and composition:** the ScriptableObject stores configuration and no scene references. Pure consumers can use `ILevelCurveDefinition`; Formula/Custom continue using the existing domain evaluator. No scene lookup, singleton dependency, background work, event subscriptions, external resources or runtime logging exists in the owned source. Runtime-created test assets are destroyed in teardown.
- **Serialization/API:** all **11 serialized field names/types** and every public declaration line match baseline `ca198556`. No defaults, field layouts, asset GUIDs or enum values changed. `XpPerLevel` and `SetLinear` retain their minimum-one clamps. `AnimationCurve` and `CustomEntries` expose mutable backing configuration; no silent copying or cache was introduced.
- **Null/boundary behavior:** missing/empty curves retain level one and zero remaining XP; positive caller caps still apply; zero/negative caller caps retain the existing uncapped meaning. Null Custom entries remain skipped; absent tables now have a consistent lookup fallback. Custom missing-level and duplicate-first-match semantics remain unchanged.
- **Compatibility:** no `Object.GetInstanceID`, serialized-reference instance ID API, new version branch, package reference or editor-only runtime dependency was introduced. Existing assembly references are sufficient. Compilation on Unity 6.0–6.4 and 6.5+ has not been performed by this worker.
- **Reuse decision:** searched the NeoxiderTools `extensions`, `modules` and `game-systems` references and the relevant Core/Extensions implementations. Reused `LevelCurveEvaluator`, `LevelModel` and Unity's existing authoring types. No ready helper implements upward-rounded, nonnegative, saturating XP conversion; added one small private `CeilingToXp` method without another assembly dependency. No framework or parallel gameplay system was added.

## Findings and fixes

The following are source-level demonstrations against `ca198556`, not claims of executed Unity reproductions. Current source links identify the fixed implementations.

### F1 — P2: preview XP can be below the actual level threshold

- **Trigger:** Formula/Quadratic base 12.25 at level two, or a Curve sample of 12.25/12.5 at level two.
- **Observable consequence:** `GetRequiredXpForLevel(2)` returned 12, while `EvaluateLevel(12)` remained one. A UI or caller using the advertised cumulative threshold could fail to reach its requested level. Formula used truncation; Curve used nearest-integer rounding.
- **Source/fix:** [GetRequiredXpForLevel](../../../Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs:206) now rounds Formula/Curve requirements upward, using the same integer-XP interpretation as next-level queries. Valid integer Custom requirements are unchanged.
- **Regressions:** `Formula_FractionalRequirement_IsEnoughToReachTheRequestedLevel` and `Curve_FractionalRequirement_IsEnoughToReachTheRequestedLevel`, three cases each. Each checks the threshold, the level immediately below it, the level at it and the remaining XP.

### F2 — P2: Curve endpoint and next-level queries disagree about reachable levels

- **Triggers/consequences:** final X = 2.4 rounded to two, yet `GetXpToNextLevel` evaluated level three and requested XP for an unavailable level. Final X = 2.6 rounded to three and granted a level beyond the authored domain. A sole zero-valued key ending at X = 0.4 rounded to zero, bypassed the end check and granted level 10,000. A curve extending past the existing 10,000 scan limit could request XP beyond that limit.
- **Source/fix:** [EvaluateLevelFromCurve](../../../Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs:127), [GetXpToNextLevelFromCurve](../../../Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs:159) and [GetCurveLevelLimit](../../../Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs:176) share a domain of `max(1, min(10000, floor(lastKey.time)))`. Remaining XP is zero at its end. The final key is read by index, avoiding the old repeated `.keys` array copies.
- **Assumption/compatibility:** integer progression ends at the last integer inside the authored domain, consistent with the existing last-key stopping rule. Fractional endpoints that previously rounded upward now stop at their floor. The existing 10,000 scan cap is preserved and explicitly documented.
- **Regressions:** two fractional-end cases, four endpoints at/before level one, the scan-limit case, an explicit-cap control and an empty-curve control. Fractional-end cases also verify `LevelModel.CurrentLevel` and `XpToNextLevel` through the public interface.

### F3 — P2: unchecked conversion loses large but valid XP differences

- **Trigger:** Curve level-two requirement 3,000,000,000 and total XP 2,000,000,000. The valid remaining amount is 1,000,000,000, but the old implementation first cast the threshold outside `int` range and then subtracted. Formula preview `SetLinear(int.MaxValue)` at level three likewise produced an unrepresentable 4,294,967,294 before an unchecked cast.
- **Observable consequence:** a remaining-XP or preview query could return an unrelated, negative or zero amount depending on the conversion/runtime, rather than a safe bounded result.
- **Source/fix:** [CeilingToXp](../../../Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs:183) clamps nonpositive/NaN values to zero and positive overflow to `int.MaxValue`. Curve next-XP subtraction occurs in double precision **before** conversion, preserving representable differences. Formula evaluation and its next-XP implementation remain delegated and outside this fix.
- **Regressions:** Formula preview saturation; Curve remainder at zero total XP and at 2,000,000,000 total XP. Saturated preview values are explicitly documented as representation limits, not reachability promises.

### F4 — P2: implicit float conversion grants a level early at large XP

- **Trigger:** Curve threshold 16,777,220, total XP 16,777,219.
- **Observable consequence:** `required > totalXp` promoted the integer XP to float. 16,777,219 rounds to 16,777,220 as float32, so the comparison treated the threshold as met one XP early.
- **Source/fix:** [EvaluateLevelFromCurve](../../../Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs:127) promotes the sampled float requirement to double before comparison, preserving exact integer XP.
- **Regression:** `Curve_LargeIntegerThreshold_DoesNotRoundTotalXpUp` checks the preview threshold, the level one XP below it, one remaining XP and the level at the threshold.

### F5 — P3: absent Custom table breaks lookup while evaluation falls back safely

- **Trigger:** a null `_customEntries` backing list, representing absent/malformed serialized data. This state is not assignable through the normal public authoring API.
- **Observable consequence:** `TryGetDefinition` dereferenced `.Count`, and Custom `GetRequiredXpForLevel` therefore threw, although domain evaluation already accepted a null table and returned the base level/no progression.
- **Source/fix:** [TryGetDefinition](../../../Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs:243) returns `false` with a null entry when the table is absent. `CustomEntries` retains its existing nullable backing-list behavior.
- **Regression:** `Custom_NullTable_LookupAgreesWithEvaluationFallback` arranges the absent table with reflection because the public API cannot assign it, then checks lookup output, required XP, resolved level and remaining XP. Reflection is only fixture arrangement, not the asserted behavior.

## Documentation

- Added owned topic [CurveEvaluation.md](../../../Assets/Neoxider/Docs/Core/Level/Data/CurveEvaluation.md) with C# calls, integer rounding/saturation, endpoint rules, the scan cap, shared-asset mutation/ownership, Custom lookup, invalid serialized values and Inspector/NoCode integration.
- Updated the owned class's `NeoDoc` attribute to this topic. The topic links to the existing canonical authoring guide and the component/bridge docs; those other workers' documents were not edited.
- Added unique `.meta` files for the new topic and regression source. Folder metadata remains the coordinator's responsibility.
- Coordinator follow-up: link this narrow topic from `Assets/Neoxider/Docs/Core/LevelCurveDefinition.md` when reconciling the parent module entry. It is already reachable through the class's Inspector documentation link.

## Regression deliverable

New file: [Audit20261023NeoCoreLevelDataTests.cs](../../../Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelDataTests.cs).

There are **11 test methods / 20 NUnit cases**. They observe public thresholds, resolved levels, remaining XP and model state. Configuration uses `SerializedObject` to exercise the real serialized authoring fields; only the null-table corruption scenario uses reflection. Existing `Neo.Editor.Tests` already references `Neo.Core` and `Neo.Core.Level.Data`; no asmdef edit is needed. Tests have been authored and statically reviewed, **not run**.

## Substantial improvement proposal

### Validated immutable threshold definitions for Curve/Custom (not implemented)

**Problem:** Curve evaluation performs a linear scan per query, with a historical 10,000-level ceiling. Inspector data can contain descending thresholds, non-finite samples, duplicate levels or unreachable XP. Custom and Curve have different lookup/evaluation rules, while the public asset exposes mutable backing objects. Caching inside the asset would become stale when callers mutate those objects, and integer saturation cannot tell a caller whether a requested threshold is actually reachable.

**Proposed architecture/public API:**

1. In `Neo.Core`, add a pure immutable `LevelThresholdDefinition : ILevelCurveDefinition`, constructed from copied `(int level, long requiredXp)` thresholds by `TryCreate(..., out LevelThresholdDefinition definition, out IReadOnlyList<LevelCurveIssue> issues)`. Validate increasing unique level IDs, nonnegative nondecreasing cumulative XP and supported numeric limits. Use binary search to resolve level and next threshold. Offer `TryGetRequiredXp(int level, out long requiredXp)` so absence and large thresholds are explicit; no scene or Unity types enter this core.
2. In this Data assembly, add an opt-in `LevelCurveDefinition.TryCreateSnapshot(...)` adapter for Curve/Custom. Sample integer Curve levels once, reject non-finite samples before conversion and report thresholds beyond integer XP as unreachable warnings. Preserve equal-XP thresholds as intentional multi-level jumps. Formula mode continues to reuse the existing analytic evaluator rather than materializing an unbounded table.
3. Return a snapshot independent of the asset's mutable list/curve. Consumers explicitly rebuild it after configuration changes; avoid an implicit invalidation scheme over exposed mutable objects. An optional editor inspector can display structured issues without rewriting the asset or storing scene references.

**Compatibility/migration:** retain `ILevelCurveDefinition`, all current serialized fields and current direct evaluation by default. Existing `LevelModel.SetCurveDefinition` can consume the immutable definition without modification. Projects opt in to snapshot validation; no asset migration is mandatory. Preserve the legacy 10,000 sampling default, with a separately explicit limit for projects choosing a larger compiled domain. Work crosses `Neo.Core`, Data and editor ownership and must be coordinated.

**Acceptance scenarios:**

- An ordinary valid Curve and ordered Custom table match current public level/remaining-XP results at every threshold and on either side of it.
- Duplicate level IDs, decreasing thresholds, NaN/infinity and unreachable integer-XP targets produce indexed diagnostics; configuration is never silently rewritten.
- Mutating the source asset after creating a snapshot does not change an existing model using that snapshot; explicit rebuild produces the updated schedule.
- A declared 100,000-level schedule resolves through binary search without the current per-query scan or hidden 10,000 cap.
- Large thresholds can be queried through the `long` API without pretending a saturated `int` value is reachable; tests cover absent levels, equal thresholds and numeric boundaries.
- The pure factory and evaluation have EditMode tests independent of scene objects; the Data adapter has a small serialized-authoring smoke test.

**Effort/risk:** approximately 2–3 engineering days plus coordinated Unity validation. Medium risk: definition of duplicate/invalid-data handling, compatibility with equal thresholds and keeping snapshot semantics explicit. The new API is additive and opt-in, reducing migration risk. It addresses both runtime query cost and authoring diagnostics without a new scene manager or NoCode-only logic.

## Verification and remaining risks

### Checks actually performed

- Read both owned C# files completely and matched the filesystem inventory against the exact manifest entry: **2/2**, no nested assembly source accidentally included.
- Compared modified source to `git show ca198556:<path>`: all **11 serialized field names/types** and all public declaration lines are unchanged.
- Ran scoped `git diff --check`: exit code **0**. Also checked new/untracked package files directly for trailing whitespace because ordinary Git diff does not include them.
- Ran read-only Python static validation: strict UTF-8 decoding and no replacement characters, explicit C# declarations/no `var`, balanced delimiter counts, the unique test prefix, **20 NUnit case declarations**, existing Editor test references, all **3 documentation links**, and the `NeoDoc` destination.
- Checked both new `.meta` GUIDs against all package metadata with `rg`: each occurs exactly once.
- Calculated arithmetic witnesses only: truncation of 12.25 gives 12 while its ceiling is 13; converting integer 16,777,219 to IEEE float32 gives 16,777,220; rounding endpoint 2.6 gives three whereas its floor is two. These checks support the source analysis and do **not** execute the C# implementation.
- Reviewed the final source diff locally. Independent agent QA was not used because child agents are prohibited for this audit.

### Not executed / integration risks

- Unity import/compilation, NUnit execution, Play Mode and builds were **not run**, as explicitly required by the worker contract. Delimiter/source checks are not compiler validation. Coordinator should run `Neo.Core.Tests.Audit20261023NeoCoreLevelDataTests` and the existing Level curve/model/component tests on supported Unity versions.
- The fractional-end floor rule intentionally corrects results for curves that previously extrapolated to a rounded-up terminal level. Serialized fields and public signatures are unchanged; inspect any project intentionally relying on that old behavior.
- Finite, nondecreasing authored progression remains a precondition. NaN/decreasing curves and malformed Custom entries are not fully validated; the proposal addresses this separately.
- Formula evaluation/remaining-XP arithmetic belongs to `Neo.Core`. A concrete baseline cross-boundary lead is `LevelCurveEvaluator.EvaluateLinear(int.MaxValue, 1)`: `1 + totalXp / xpPerLevel` overflows before the outer minimum-level clamp, so the public Formula definition can resolve to level one at maximum XP. Its next-XP cast/subtraction has similar unchecked numeric limits. **Coordinator/Neo.Core owner:** check current concurrent fixes and add or retain domain regressions; this worker did not modify that code or claim it fixed.
- Parent-guide backlink and any new folder `.meta` belong to the coordinator. No commits, shared config edits, Unity operations, installs or test runners were performed.
