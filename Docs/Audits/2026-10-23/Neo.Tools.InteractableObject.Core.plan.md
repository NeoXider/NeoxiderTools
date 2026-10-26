# Neo.Tools.InteractableObject.Core: implementation plan

Phase 2, 2026-10-23. Continues the same completed [audit](./Neo.Tools.InteractableObject.Core.md).
This phase writes this plan only; all implementation below is future work unless marked completed.

## Current outcome

All four owned C# files were reread. CORE-01 is already fixed in `InteractionQueryMath.IsWithinRange`:
double subtraction/squaring prevents overflow/underflow from accepting out-of-range targets.
CORE-02 is fixed using infinity sentinels, making finite `float.MaxValue` hits selectable.

Completed artifacts: owned XML summaries, English
`Assets/Neoxider/Docs/Tools/InteractableObject/Core/README.md`, and
`Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreQueryMathTests.cs`
with five methods/18 cases and metadata. Signatures/serialized data were preserved. Arithmetic probes
and static UTF-8/whitespace, inventory, GUID, link, delimiter checks passed. Compilation/Unity execution
remain unverified. No remaining confirmed runtime defect was identified.

## Priorities and outstanding evidence

| Priority / classification | Evidence, owner, smallest next action, observable result |
|---------------------------|--------------------------------------------------------|
| P1: completed-fix verification debt | Coordinator, after source audits settle: compile/run existing `InteractionQueryMathTests` and the audit fixture. Axis/range cases `(0, 3e20f, 2e20f)`, `(-3e38f, 3e38f, 3e38f)`, `(0, 1e-23f, 5e-24f)` must reject; inclusive cases accept. Maximum finite hits must be returned; closer blockers reject while retaining target output. Fix only demonstrated failures. |
| P1: compatibility debt | Coordinator: compile/test on actual Unity 6.0/6.4 and 6.5+ editors. Check the leaf without Mirror/package references and tests with `UNITY_INCLUDE_TESTS`. No version branch was added; one editor cannot validate another version. |
| P2: confirmed docs/package gap | Parent README lacks the Core link; `Assets/Neoxider/Docs/Tools/InteractableObject/Core.meta` is absent. Parent owner/coordinator links `Assets/Neoxider/Docs/Tools/InteractableObject/README.md`; coordinator creates folder metadata. Recheck concurrent changes first. Expected: discoverable guide, stable folder GUID. |
| P2: unverified camera contracts | Coordinator verifies cached/main, destroyed cache, disabled live cache, no-camera, and fallback-disabled identity/null results in an isolated EditMode scene. Missing coverage goes in new `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreCameraResolverTests.cs` plus `.meta`; restore scene state. Sticky/disabled caches remain policy, not bugs. |
| P3: unconfirmed precision/performance risks | Run existing ordinary-scale boundary coverage; double comparison can change float-rounded edge decisions. Profile only for demonstrated target-device cost. Parent physics/direction support at extreme coordinates remains outside Core's guarantee; no speculative fix. |

## P2 proposal: selection identity and explicit decisions

**Problem.** Custom input/AI sources can keep parallel hit/receiver buffers, but the current copied
`InteractionRayHit` cannot identify an original entry when geometry and flags are identical. A Boolean
also requires consumers to infer blocked versus absent targets. Re-scanning or duplicating the reducer
can lose first-entry tie behavior and drift from shared obstacle rules.

**Reuse.** Retain `InteractionQueryMath`, `InteractionRayHit` array/count, and `IInteractiveTarget`,
as recommended by the reviewed package catalogs. Add no manager, provider, payload, dependency, or bridge.

Proposed public API in namespace `Neo.Tools`, assembly `Neo.Tools.InteractableObject.Core`:

```csharp
public enum InteractionSelectionStatus { NoTarget, Clear, Blocked }

// New readonly InteractionSelectionResult properties:
// InteractionSelectionStatus Status { get; }
// int TargetIndex { get; }
// int BlockingHitIndex { get; }

// New method on InteractionQueryMath:
public static InteractionSelectionResult SelectTarget(
    InteractionRayHit[] hits, int count, bool requireClearPath);
```

Indices refer to the original populated input prefix, not a reordered/filtered copy. `NoTarget` uses
both indices `-1`; `Clear` supplies a target and blocker index `-1`; `Blocked` supplies both. With
clear-path checks disabled, a valid target is `Clear`. Make `default(InteractionSelectionResult)`
consistently mean `NoTarget` with indices `-1`, for example using private index-plus-one storage.
Construction remains internal so consumers cannot produce contradictory status/index combinations.
Index validity lasts only until the caller reuses or mutates its buffers.

Exact proposed file changes:

- Modify `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionQueryMath.cs`.
- Add `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionSelectionStatus.cs` and `.meta`.
- Add `Assets/Neoxider/Scripts/Tools/InteractableObject/Core/InteractionSelectionResult.cs` and `.meta`.
- Add `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreSelectionResultTests.cs`
  and `.meta`; do not rewrite another worker's existing fixtures.
- Extend `Assets/Neoxider/Docs/Tools/InteractableObject/Core/README.md` with indexed C# dispatch,
  snapshot lifetime, and scene/NoCode guidance.

Implement one allocation-free, single-pass selection reduction. Retain invalid-distance filtering,
count clamping, earliest target/blocker ties, and acceptance at equal blocker distance. Have legacy
`TrySelectTarget` reuse it while returning the copied target even when blocked. Leave nearest-any-hit,
range/padding, `InteractionRayHit`, `IInteractiveTarget`, and camera policy unchanged.

Minimum EditMode cases: identical hits select receiver index zero; closer/equal blockers
produce distinct expected statuses/indices; null/empty/negative/oversized counts behave correctly;
invalid and stale-tail entries cannot be selected; disabling path checks accepts the intended receiver;
zero/maximum finite distances work; default result is safe; legacy Boolean and every returned hit field
retain behavior. A parallel-buffer adapter test asserts only the indexed receiver's interaction counts
change. Check per-query allocations using existing central profiling after warmup.

Compatibility: additive API, no serialized fields/assets or sample migrations, and no asmdef edits.
Core retains no package references; `Neo.Editor.Tests` already references it. Keep `Samples~` unchanged.
Document C# dispatch and the existing Inspector/UnityEvent path. Effort: 1-2 developer days; risks:
default safety, stale indices, blocked-output parity. This proposal is not an outstanding bug.

## Cross-assembly order and completion gates

1. After source audits converge, complete P1 checks; correct demonstrated failures before API work.
2. Implement proposed Core result/reducer/tests. Deferring it does not block releasing existing fixes.
3. `Neo.Tools.InteractableObject` owner (`neo-audit-1023-b07-03`) considers optional adoption only after
   Core is stable. Exact adapter boundary: `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs`,
   especially compacting `BuildInteractionHits3D/2D`; receiver arrays must preserve those compacted indices.
   No adapter/editor change is required to ship Core. Current
   `InteractionRayProvider.cs` already checks the current main camera; do not reimplement that neighbor's fix.
4. `Neo.Editor.Tests` owner (`neo-audit-1023-b03-04`) and coordinator own shared fixture/config integration.
   Add no references unless a demonstrated compiler failure requires coordinated correction.
5. Finish docs/linking/metadata through their owners. Validate GUIDs, links, UTF-8, scoped diff,
   unchanged leaf references, and package import.

Core math/result acceptance is covered by EditMode. A small ordinary-scale PlayMode smoke belongs to
the parent owner: automatic range/path eligibility and local interaction events remain correct in
2D/3D, while manual typed dispatch keeps its documented bypass semantics. No PlayMode simulation at
extreme coordinates is required. Version-specific object IDs, if ever needed by adapters, must use
the central `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId` gates rather than scattered `#if`.

Stop when named contracts and both Unity version families pass; route unrelated failures to owners.
Require execution receipts. Active-camera eligibility and blanket NaN/infinity policy remain separate
product decisions. This phase changes only this plan and runs no Unity/build/test process.
