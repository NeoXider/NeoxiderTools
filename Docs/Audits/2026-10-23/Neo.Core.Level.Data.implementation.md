# Neo.Core.Level.Data — implementation receipt

Target: `demo` / `11.0.0`. Status: **SOURCE-FINAL** for this owner. Independent source/documentation work is complete; execution awaits the global writer barrier.

Latest completion: **CORE-LEVEL-CURVE-SCAN-CONTROL**, 2026-10-24. The source3 fixture correction and its pending native verification are recorded in the final section. Earlier source-phase evidence and artifact identities below are historical.

## Public API and dependencies

**Additive APIs: none.** Existing `LevelCurveDefinition`, `LevelCurveEntry`, `ILevelCurveDefinition` and `ILevelCurveEntry` signatures remain intact. No snapshot factory, validator or immutable-threshold framework is being added. The substantial improvement remains a proposal in [the historical plan](Neo.Core.Level.Data.plan.md).

**Pending additive API dependencies: none.** Formula/Custom continue to consume `Neo.Core.Level.LevelCurveEvaluator` from the `Neo.Core` assembly. No asmdef, scene, prefab, sample layout or configuration change is required. The API/dependency declaration was published in this receipt before the owned documentation edits.

## Approved work

**Initial review-action IDs: none**; the initial [implementation request](Neo.Core.Level.Data.implementation-request.md) assigned no additional confirmed runtime defect. Its later source3 addendum assigns **CORE-LEVEL-CURVE-SCAN-CONTROL**, completed below. The following earlier audited finding IDs are preserved as completed source work, rather than repeated as new implementation actions:

| Audit ID | Retained correction |
| --- | --- |
| F1 | Formula/Curve preview XP rounds upward. |
| F2 | Level/remaining-XP queries share the floored authored endpoint and existing 10,000 scan limit; final key access avoids `.keys` copies. |
| F3 | Numeric results saturate safely; Curve remaining XP subtracts before conversion. |
| F4 | Curve comparison preserves exact integer XP by promoting the sampled threshold to double. |
| F5 | An absent Custom table returns a failed/null lookup consistently with evaluation fallback. |

The approved documentation/navigation work is complete within the owned Data directory. No duplicate regression fixture or optional framework was introduced. Original audit and plan remain historical artifacts, with their verification statements unchanged.

## Exact files and work performed

Paths below are relative to the repository root.

| File | Disposition |
| --- | --- |
| `Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveDefinition.cs` | Preserved the coherent F1–F5 runtime patch and `NeoDoc("Core/Level/Data/CurveEvaluation.md")`. No further implementation-phase edit. |
| `Assets/Neoxider/Scripts/Core/Level/Data/LevelCurveEntry.cs` | Fully read; unchanged. Constructor clamps and serialized fields retained. |
| `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelDataTests.cs` | Retained all 11 methods / 20 meaningful NUnit case declarations; the later source3 correction changes only the scan-limit arrangement and adds exact endpoint precondition assertions, as recorded below. No duplicate or unrelated legacy-test edit. |
| `Assets/Neoxider/Docs/Core/Level/Data/CurveEvaluation.md` | Completed the public API reference, configuration setter contracts, legacy `CurveType` limitations, model recalculation/shared-asset behavior, direct C#/Inspector/NoCode paths, query cost and deliberate 11.0 compatibility notes. |
| `Docs/Audits/2026-10-23/Neo.Core.Level.Data.implementation.md` | New dedicated API/dependency and source-final receipt. |

The existing documentation/test `.meta` files and GUIDs are retained. No new package file or folder metadata is needed in this phase. No other owner's code, tests, docs, receipt or shared configuration was edited.

The Data document links to the canonical parent authoring guide and the component/bridge topics. Its configuration table distinguishes readonly authoring selections from `SetLinear`/`XpPerLevel`, explains that changing `XpPerLevel` alone preserves mode/type, and describes the mutable curve/table exposure. It states that assets do not publish change notifications; `LevelModel.SetCurveDefinition` can recalculate an XP model, whereas reassigning the same asset to `LevelComponent` is not a refresh command. Scene actions use existing provider APIs and store scene references only in adapters.

## Compatibility and migration

All 11 serialized field names/types and existing public signatures in the retained Data patch remain intact. There are no new enums, serialized selections, scene references, caches or version-specific branches.

The intentional 11.0 behavior remains: previews of 12.25 XP require 13 integer XP; fractional endpoints X=2.4 and X=2.6 both end at level two; terminal/scan/caller caps return zero remaining XP; integer XP immediately below a large threshold does not round up into an early level. Projects relying on the former rounded-up endpoint can reauthor the last key at their intended integer level. These corrections do not automatically rewrite assets/scenes/prefabs. Saturated preview values remain representation limits rather than reachability promises.

## Authored acceptance sources — initial source phase

Existing fixture: `Neo.Core.Tests.Audit20261023NeoCoreLevelDataTests`.

- Fractional Formula and Curve requirements: 12.25, 12.5 and 12.75 each report 13; immediately below/at threshold gives the expected level and remaining XP.
- Formula preview overflow saturates. A Curve threshold of 3,000,000,000 reports a saturated preview, but still reports 1,000,000,000 remaining from 2,000,000,000 accumulated XP.
- At threshold 16,777,220, XP=16,777,219 remains level one with one XP remaining; XP=16,777,220 reaches level two.
- Fractional endpoints, four endpoints at/before level one, the 10,000 scan limit, an explicit cap and an empty curve have explicit level/remaining-XP expectations. Fractional endpoint cases also observe `LevelModel` state through the public interface.
- An absent Custom table returns `false/null`, zero required XP and the base-level/no-progression fallback. Temporary ScriptableObjects are destroyed in teardown.

These were regression **source declarations**, not execution receipts, when the initial source phase completed. The subsequently supplied source3 XML and the unexecuted correction are distinguished below. No additional fixture was needed.

## Cross-owner handoff and remaining evidence

**Source/API blockers: none.** Existing Core signatures are sufficient. Current source already contains the Core owner's saturated linear evaluation and subtract-before-conversion helper; the historical overflow lead is not pending Data work.

**Documentation integration:** `Neo.Core` parent-guide owner/coordinator should add a backlink in `Assets/Neoxider/Docs/Core/LevelCurveDefinition.md` to `Level/Data/CurveEvaluation.md`. That file is outside Data ownership. The owned topic already links back to that parent and is reachable through `NeoDoc`. This navigation integration does not block Data source-final status.

**After EVERY source writer finishes**, the coordinator's Unity operator must compile/discover/run the focused Data fixture and existing Level/Core integration tests on the frozen snapshot in installed Unity 6.3 and 6.6. Observe Formula/Data threshold parity, the deliberate fractional-end change, caps and model/component/NoCode propagation. Core/Data boundary acceptance also includes `SetLinear(1)` at `int.MaxValue` XP resolving to `int.MaxValue` level and zero remaining XP; its Core correction is not claimed runtime-verified here. Broader supported-version, native scene and package/import evidence remains centrally pending.

Finite nonnegative nondecreasing authored schedules remain a documented precondition; invalid schedules have no new diagnostics system. Curve query cost remains up to 10,000 samples per level query, with a separate scan for remaining XP. The optional immutable snapshot and structured diagnostics in the historical plan remain **proposals**, not implemented APIs.

## Actual implementation-phase evidence

- Read the full implementation contract/request, current owned source/documentation/regression source, own historical audit/plan and relevant Core/component/bridge boundaries. Branch inspection returned `demo`; `globalReviewApproved` was true.
- Performed source/documentation reads, patch/diff review and artifact hash bookkeeping only. Scoped `git diff --check` exited **0** for tracked changes; new/untracked documents are not covered by ordinary Git diff checking and remain subject to the coordinator's frozen-snapshot checks.
- No compiler, Unity/MCP, test runner, product validator/checker, service or install was executed. No commit/push or child agent was used. No old verification receipt was rewritten to imply a current pass.

Initial source-final artifact identities (SHA-256; the later corrected fixture hash is recorded below):

| Artifact | SHA-256 |
| --- | --- |
| `LevelCurveDefinition.cs` | `7dc74c2fd574a81f173af1387537a79eea886c213af55f8b00705949027ab9e9` |
| `LevelCurveEntry.cs` | `a99d988916153baca18f0a2bc0d438ce5eade964a9fe58e5cf1148827b0c6b1d` |
| `Audit20261023NeoCoreLevelDataTests.cs` | `6dfde6b543707b27ffe605370d0abbd9e8cfbff6e8ef893de0204824d6fe9877` |
| `CurveEvaluation.md` | `cc603a74168c5ca0a950732407c5b364b0fb564974b237649ba6dc8233638578` |
| Historical audit | `c9206079d0f8bf8ad3cdbe5415996be1b9257b84554a66b86b50484be9e021ee` |
| Historical plan | `fae1c24904a4aae0825de038801ca61927b8bf840e3bc3f06ab2aeff87ca8268` |

## Source3 follow-up — CORE-LEVEL-CURVE-SCAN-CONTROL

Status: **SOURCE-FINAL**, 2026-10-24. No pending source/API dependency; this owner stops source writes after this correction. Root must open a new global freeze before any execution.

### Actual native evidence read

Read `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`, SHA-256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`. Its `Neo.Core.Tests.Audit20261023NeoCoreLevelDataTests` fixture records **20 total, 19 passed, 1 failed** on the previous source snapshot.

The failed case is `Curve_ScanLimit_HasNoNextLevel`, id `2047`, run at `2026-10-24 00:58:25Z`. Message: `Expected: 10000 / But was: 9997`. Its stack points to the snapshot copy of `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelDataTests.cs:136`, at the first level assertion. This is supplied historical native evidence, not a run performed by this actor or a pass for the corrected source.

The old control authored zero-tangent Hermite keys `(1,0)`, `(10000,100)`, `(10001,200)`. The root-reviewed flat-end float quantization means that this arrangement is not a reliable strictly increasing linear threshold control. Current runtime scans in order until the first unmet sampled requirement; the failing result alone establishes no distinct production-policy defect. No plateau, XP-comparison, or progression policy was changed to accommodate the fixture.

Read the installed Unity `UnityEngine.CoreModule.xml` API documentation under both `6000.3.14f1` and `6000.6.0f1`: `Keyframe(float time, float value, float inTangent, float outTangent)` specifies both tangents and defaults to `WeightedMode.None`. No Unity process or compiler was used to inspect these vendor APIs.

### Completed correction and retained oracles

Changed only `Curve_ScanLimit_HasNoNextLevel` in `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelDataTests.cs`:

- Authored `(1,0)`, `(10000,9999)`, `(10001,10000)`, with explicit in/out tangents of **1** at every key. Both segments have matching unit secants/tangents, representing the linear control `XP = level - 1` without the old flattened endpoint.
- Kept the exact level oracle: `EvaluateLevel(9999) == 10000`.
- Kept the no-next-level scan-cap oracle: `GetXpToNextLevel(9999) == 0`, even though the authored level 10001 requires another XP.
- Added exact public preview checks: level 10000 requires 9999 XP; authored level 10001 requires 10000 XP. These retain a real threshold beyond the runtime scan cap rather than making the curve end at that cap.

All **20 existing cases / 11 methods** remain, including fractional/invalid-endpoint, empty, null-table and explicit-max controls. No Ignore, error whitelist, helper extraction, new MonoBehaviour, asmdef, runtime, public/serialized API or public-documentation change was made. The central reservation map is present and does not reserve this helper-free fixture. Other owners' reserved files, release samples and `DocsEn.meta` were untouched.

Corrected fixture SHA-256: `b4753e7fd6928e3680bd5637579b669b901fc0a4e77e4037609f29dcb857dd17`. The earlier hash above remains as the initial source-final identity; the correction is not attributed to the old source snapshot.

### Remaining evidence debt

The corrected source has **not been compiled or executed**. After all writers finish and root opens a new freeze, rerun the full 20-case fixture on Unity 6.3 and 6.6; observe exact level 10000 and zero remaining XP in this control, while retaining all other positive/negative cases. No new native result is claimed from the tangent arrangement or from historical 19/20 results. No unresolved native capability was established for this helper-free curve fixture; correctness of the corrected native interpolation/control remains unverified until that run.
