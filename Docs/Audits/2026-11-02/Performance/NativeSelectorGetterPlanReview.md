# Selector logical getter plan — independent bounded review

2026-11-02. **PLAN ADMISSIBLE within the stated two-file/three-public-getter scope.** No concrete design blocker was found after inspecting canonical arithmetic, validation order, real-item branch and all private `_model` consumers. This is plan admission only. Root's separate implementation authorization, final candidate source review, functional tests and calibrated after-capture remain required. No implementation, compile, Unity, process, native load or product/config write was performed by this reviewer. The only write is this report.

## Inputs and measured scope

| Input | SHA256 |
|---|---|
| PLAN.md (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-04-selector-plan/PLAN.md`, not committed) | `c52e80811adb037836b0d84b8d05b6dbb21d34f2ee3714836825b5006efbdb39` |
| source-inputs.json (machine-local `D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-04-selector-plan/source-inputs.json`, not committed) | `3bde983bbbc5e86c4e0d1d962b988c708840e8557dc71a7b614a85373d8c37b9` |
| Selector.cs | `8117a7719e9f3808a7c48e0afc1f2bfeeed4336c106957d0d133b618a6fc3d15` |
| SelectorModel.cs | `45928c9c1ab01181de097ce5ca72d3614eabf6015d80c7a051a6fd87619d116e` |

Both full product byte identities and ten supporting input hashes matched current files. Relevant complete method bodies/callers and every `_model` reference were inspected. Three Selector baseline evidence chains match the independently accepted [six-case baseline review](NativeLibraryHotPathCaptureReview.md); they were reused without a new raw load/run. N1000 observed4.795700 ms/4238400 bytes/2400 GC.Alloc calls is **per100 groups of three getters**, not one getter; pure-model control0.008100 ms/0bytes is a different, equivalent-output read path. Those are before observations, not promised after values or a timing budget.

## Bounded reuse/design disposition

The plan reuses existing canonical scalar logic, with no runtime framework or cache:

- Extract exact GetEffectiveIndex/GetLogicalActiveCount bodies into internal static scalar overloads; existing public instance APIs delegate. Preserve class/public/serialized shape and parameter/failure behavior.
- Public IsAtStart/IsAtEnd alone use the existing internal static GetBounds and raw `_currentIndex` comparisons. Do not clamp, wrap or rewrite current state during reads.
- Public CountActive alone selects the same real-items branch or scalar logical helper. Positive raw `_count` takes precedence; `_count<=0 && HasItems` counts live objects using the unchanged CountActiveItemsInArray. Other cases use logical Count, including the empty fallback.
- Keep private GetCurrentBounds, ComputeCountActive, ComputeLogicalCountActive and all mutation/lifecycle/notification callers unchanged. Keep Configure and E/U defensive copies where they remain meaningful. No new public exposure, asmdef/dependency/setting, frame cache, snapshot cache, mutation optimization or product marker.

The static GetBounds is **internal**, not public. Selector already calls it from Count/IndexOffset setters in this assembly; the proposed internal reuse needs no visibility or friend-assembly expansion. Expected reduction is removal of repeated Configure/copy work from only these logical public reads; actual after CPU/GC remains unverified.

## Canonical arithmetic and validation order

Current model behavior was checked directly, not inferred from a proposed replacement:

1. GetBounds(count<=0,...) returns `(0,0)` before offset arithmetic. Positive count uses long min/max, validates representable Int32 logical bounds, and throws ArgumentOutOfRangeException with parameter `indexOffset` on failure.
2. GetEffectiveIndex returns **0** for Count<=0 before GetBounds. For positive count it validates bounds **before** long CurrentIndex+IndexOffset and before effective clamping. Minimum effective index is−1 only when allow-empty; maximum isCount−1.
3. GetLogicalActiveCount returns0 for Count<=0, then uses that validated effective index; negative effective selection gives0, otherwise non-fill1 or `Math.Min(Count,effectiveIndex+1)`. Keep the validation even when non-fill could appear to return1 immediately. Keep widened addition and the current safe max-prefix behavior; do not reuse checked IndexWithOffset as a replacement.
4. Boundary booleans compare the **raw current index** against logical bounds. Empty count/raw−1 gives start=true/end=false, raw+1 gives false/true, while logical active remains0. Effective clamping does not define boundary booleans.
5. Real-item CountActive currently does **not** validate offset/bounds at all. Therefore `_count<=0`, populated Items and an otherwise unrepresentable offset must still read live active count rather than acquire a new exception. IsAtStart/End may still reject that offset because their Count comes from Items length. Branch selection must precede any new scalar bounds call.

One handwritten-oracle clarification for future test fixtures: the plan's N3/offset2/current−2 example has start=true only with **allowEmptyEffectiveIndex=false** (bounds−2..0). With allow-empty=true the bounds are−3..0 and start=false; logical fill-active1 remains the same. Make that flag explicit in the hand-derived matrix. This is a bounded test-spec clarification, not a blocker to the canonical design and not permission to change either behavior.

## Private-model dependency audit

All `_model` references were enumerated. No supported consumer was found that requires one of the three public getters to have refreshed the private model first:

| Consumer/path | Existing synchronization / required disposition |
|---|---|
| UniqueRemainingCount, IsExcluded, IsValidIndex | Each relevant model read explicitly SyncModelFromFields; preserve it. Empty IsValidIndex early-return remains as-is. |
| CreateModelSnapshot | Explicit sync, then constructs/configures a **new** snapshot. Private model does not escape. Preserve snapshot/defensive-copy semantics. |
| Next, Previous, Set, ExecuteSetRandom, SetFirst/Last, ToggleFillMode, Reset | Explicit sync before model operation/result. No later model read depends on a getter. Preserve commands and same-index Set behavior. |
| ResetUnique, SetExcludedIndices, ApplyExclude/Include/IncludeAll | Explicit sync before model mutation; subsequent SyncFieldsFromModel consumes that fresh operation result. Preserve copies, save/network ordering and callbacks. |
| ApplyModelResult/SyncFieldsFromModel | Called from the synchronized mutation paths above. Copy model state to fields, rebuild inspector snapshots and dispatch existing effects unchanged. |
| ApplyUpdateSelection, SyncIncludedIndicesInspector, RefreshItemsFromChildren | Retain private GetCurrentBounds and its sync at lines100/1129/1936. Clamping/activation/refresh must not silently switch to the new read-only route. |
| PublishSelection/ToggleIndex→NotifyCountActiveChangedIfNeeded | Preserve private ComputeCountActive/ComputeLogicalCountActive sync, equality guard, last-notified update and event order. Public getter changes must not alter notification calculation. |

The five current GetCurrentBounds call sites are exactly100/781/793/1129/1936; only the two public getters781/793 are optimized. ComputeLogicalCountActive's two callers are within ComputeCountActive; its two consumers are public CountActive and NotifyCountActiveChangedIfNeeded. This supports the plan's intentional separation rather than changing the shared helpers globally. Callback/reentry cases still need execution after implementation: later commands/queries perform their own synchronization, but that source reasoning is not a new native PASS.

## Behavior/test gates for a later implementation

No existing expected result may be adjusted to a new helper's result. Use public-contract/hand-derived oracles and retain the existing selector/view/network fixtures. Required focused coverage:

- Live serialized field changes between reads, including within one Unity frame, without calling a command that refreshes `_model`; valid/empty/overflow/offset cases and raw-bound versus effective-clamp distinction. Include the explicit false/true allow-empty variants above and the real-items invalid-offset branch.
- Positive virtual count over populated Items, zero/negative raw count falling back to live Items length/count, inactive-parent activeSelf, null/destroyed array entries, in-place caller mutation of the public Items array, SelectorItem.ActiveValue and missing-component fallback. Reuse the current real-items helper without new caching/activation.
- Repeated reads preserve observable/serialized state, E/U/included snapshots, object identity/activation and zero getter notifications/save/network. Execute Next/Set/random/snapshot after raw field edits both with and without preceding reads. Preserve same-index Set publication/save behavior, reentry, fill/additive events, lifecycle and network initial-snapshot order.
- Deferred/self/throwing enumerable updates and isolated snapshots remain defensive; no extraction may weaken Configure/SetExcludedIndices/SetUsedIndicesForUnique. Keep private legacy synchronization bodies/callers unchanged in the eventual diff.

The plan's retained class filters are appropriate existing coverage: SelectorTests, AuditFixesSelectorTests, Audit20261023NeoToolsViewSelectorTests/InputTests and ViewNetworkTests on an admitted optional-network graph. Their names/hash identities are retained inputs; no tests were run by this plan review, and missing network capability must remain an explicit limitation.

## Before/after evidence boundary

Future measurement must retain f125 producer/626 reader behavior: N1/100/1000, K100 groups, exact E/U/included/scalar flags, eight warm frames, retained4096 calibration, 120+120 frames, transition gap, strict marker topology, all outcome/state checks and source/native frame joins. Only reviewed product-source pins and resulting producer/source/compiled/run identities may change legitimately. Old raw/metadata/counters/CSV/receipts stay immutable. Use verified exact anchored selectors so `_1` cannot include `_100/_1000`.

Report actual per100-group CPU quantiles, allocation calls/bytes and pure-model/main-frame/all-thread context separately; do not subtract control into a pass, invent a threshold, promise zero for all paths or generalize to Items activation, saving/network, Player/AOT or other SDK. Root retains separate implementation authorization. No product candidate or new native result is admitted by this report.
