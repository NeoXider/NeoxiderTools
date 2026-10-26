# Neo.Merge — individual implementation plan

Phase 2, 2026-10-23. Same auditor/session: `gpt-6.1-sol`, `max`. Planning only. Reviewed: complete `PLAN-CONTRACT.md`, completed audit, manifest, five current runtime files, owned README/regressions and GridMerge/Dice boundaries.

## Current outcome

Completed: `MergeResolver.ResolveSeed` rejects null result selection; deferred application protects earlier seeds/cascades. `MergeRequest.SelectResultItem` and `Resolve` document the exception. Seven implicit locals were replaced. Canonical README covers C#, graph/value contracts, mutation, limits and scene/NoCode integration. Four regressions assert exceptions, unchanged values and zero setter calls.

Source review and static UTF-8/whitespace, inventory, delimiter, reference, GUID and link checks passed. Unity compilation/tests/builds were **not executed**. No confirmed defect remains open; do not repeat completed fixes.

## Priorities and verification debt

| Priority / status | Evidence and owner | Smallest next action / observable result |
| --- | --- | --- |
| P1 verification, not a new defect | MERGE-01; `Scripts/Merge/MergeResolver.cs:64`; coordinator | Run four cases in `Tests/Edit/Merge/Audit20261023NeoMergeNullResultTests.cs`: null selection throws; original dictionaries remain identical; setters remain unused, including later-seed/cascade failures. |
| P1 compatibility verification | Existing seven `MergeResolverTests` cases; coordinator | Execute unchanged fixture: external targets receive values; integer item zero works; custom matching, preview, cascades and exact-limit reporting retain behavior. |
| P2 integration verification | `GridSystem/Merge/GridMergeResolver.cs` forwards `SelectResultCell`; owner `Neo.GridSystem` | Add a focused adapter regression if absent: null selection changes neither cell content nor notification count. Do not alter the core guard. |
| P2 proposal | Preview exposes mutable groups and affected items, without canonical final writes; owner `Neo.Merge` | Implement the additive plan API below after baseline verification. |
| P3 documented limitations | Setter exceptions, mutable identity, infinite enumerables and callback side effects; caller/coordinator | No confirmed defect or automatic rewrite. Require a concrete failing supported scenario before changing these contracts. |

Table paths are relative to `Assets/Neoxider/`. New tests retain `Audit20261023NeoMerge`; existing fixtures stay unchanged.

## Substantial proposal: immutable preview and conditional application

Developers currently recompute a preview to apply it, or reconstruct final state across cascade groups. Recalculation can differ after state changes. Provide reusable value-change output for previews, undo and replay, without introducing Unity or a second matching engine.

Reuse `MergeRequest`, BFS, overrides, identity comparer and cascade guard through shared evaluation in `MergeResolver.cs`. Capture snapshots only for planning; preserve legacy normalization, callbacks and deferred writes. Use BCL collections/read-only wrappers; no new dependency. Current source has no `MergePlan`/`MergePlanner` alternative.

Proposed public methods, respectively on `MergePlanner` and `MergePlanApplier`:

```csharp
public static MergePlan<TItem, TValue> Create<TItem, TValue>(
    MergeRequest<TItem, TValue> request);

public static MergeApplyResult<TItem, TValue> ApplyIfUnchanged<TItem, TValue>(
    MergePlan<TItem, TValue> plan,
    Func<TItem, TValue> getValue,
    Action<TItem, TValue> setValue,
    IEqualityComparer<TValue> valueComparer = null);
```

`Create` ignores `Mutate`, requires no setter and never writes. Do not temporarily change caller configuration. Null targets still throw.

`MergePlan` exposes structurally immutable `Groups`, `Changes`, `ReadValues` and `CascadeLimitReached`. `MergePlanGroup` snapshots group membership/clears and seed/result/value metadata; do not expose legacy mutable group objects. Each `MergeValueChange` contains `Item`, `OriginalValue`, `FinalValue`, once per affected item, including external targets and repeated cascade results. Preserve affected-item semantics even when a final value equals its original.

Capture first-read originals for every inspected value, including nonmatching neighbors and external destinations, rather than checking only written items. `ReadValues` can use `IReadOnlyList<KeyValuePair<TItem, TValue>>`. Conditional application compares every captured value before any write; return `MergeApplyResult.Applied = false` with `ConflictItems` when stale. Comparison defaults to ordinary value equality, independently of broad gameplay match predicates. Success applies captured final values without recomputing rules.

This is optimistic validation, not a transaction: callers serialize application and preserve topology, filters and rule configuration. Shallow generic snapshots do not freeze mutable item/value objects. Getter failures before application cause zero writes; setter failures may partially apply and propagate. An atomic batch adapter remains a separate proposal, requiring a real transactional storage owner rather than generic rollback promises.

## Exact implementation files and boundaries

Within `Assets/Neoxider/Scripts/Merge/`, modify `MergeResolver.cs`; add `MergePlanner.cs`, `MergePlan.cs`, `MergePlanGroup.cs`, `MergeValueChange.cs`, `MergePlanApplier.cs`, `MergeApplyResult.cs` and unique `.meta` files. Preserve `MergeRequest.cs`, `MergeResult.cs`, `MergeGroupResult.cs` and `MergeCascadeMode.cs` public contracts.

Add `Assets/Neoxider/Tests/Edit/Merge/Audit20261023NeoMergePlanTests.cs` and `Audit20261023NeoMergePlanApplyTests.cs`, each with unique metadata. Existing `Neo.Editor.Tests` references suffice. Update only canonical `Assets/Neoxider/Docs/Merge/README.md` for programmer examples and scene-adapter responsibilities.

No MonoBehaviour, Editor or NoCode component is needed. Optional later integration belongs to `Neo.GridSystem`, specifically `Scripts/GridSystem/Merge/GridMergeResolver.cs`, and `Neo.GridSystem.Dice`, specifically `Scripts/GridSystem/Dice/DiceBoard.cs`/`DiceBoardService.cs`; their owners control corresponding docs/tests. Current GridMerge already notifies after content writes; Dice suppresses those notifications, synchronizes occupancy and emits its own events. Preserve those existing improvements; do not duplicate them. Core planning has no dependency on either assembly. Adapter work follows core acceptance only if a concrete scene workflow needs it.

## Implementation order, acceptance and stop conditions

1. Coordinator establishes the post-audit baseline after concurrent source audits settle: compile and execute the eleven Merge cases, affected GridMerge tests and Dice cascade/occupancy/event regressions. Correct only observed failures under the relevant owner's scope.
2. Implement snapshots and shared evaluation, then minimal public-behavior tests: zero preview writes; immutable collection snapshots; one final change per cascaded item; external destination captured; parity with legacy output; null targets rejected.
3. Add conditional application and tests: unchanged source applies expected final values; changed written **or inspected nonmatching** values yield conflicts and zero writes; matching equivalence does not conceal changed originals; getter failure writes nothing; setter failure is documented as nontransactional.
4. Update C#/scene documentation, then optional owner-controlled adapters. Preserve public fields/defaults, serialized assets and release `Samples~` layout; add no asmdef/package/settings changes. New API is additive; no migration is required.
5. Coordinator compiles on Unity 6.0–6.4 and 6.5+ and runs focused EditMode checks on both supported environments. Pure Merge needs no PlayMode scenario; adapter changes additionally require PlayMode verification of complete content/occupancy and notification counts. Keep version handling centralized through existing compatibility helpers if ever needed; currently this assembly has no Unity API or version branches.

Stop release closure on compilation failure, legacy regressions, null-selection/stale-plan writes or inconsistent notifications. Missing environments stay unverified. Transactional semantics and adapter UX need a consumer decision; core work and authorized corrections can proceed independently. Core estimate: 2–4 developer days plus QA; optional adapters add 1–2. Risks: snapshot allocations, shallow values and changed legacy semantics.
