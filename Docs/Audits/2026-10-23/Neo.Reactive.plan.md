# Neo.Reactive — phase 2 implementation plan

Original auditor/session; planning only. This phase changes only this file. Proposed implementation remains pending.

## Current outcome

Completed: reviewed 2/2 source files; fixed stale `ListenerCount`, unsafe explicit-target immediate delivery, weak multicast callback truncation, and `RemoveListener(null)`. Added `Audit20261023NeoReactiveRegressionTests.cs` with six methods/eight cases and its metadata. Updated `Docs/Reactive/README.md`, `ReactiveProperty.md`, and XML contracts. Public declarations and serialized fields were preserved.

Validation was static: scoped diff/whitespace, UTF-8, lexical structure, declarations, links and GUID uniqueness. Unity compilation, GC, tests and player builds remain unexecuted. Current source, owned guides/tests and dependency boundaries were reread.

## Priorities and remaining work

**Unfixed confirmed runtime defects: none identified.** Do not repeat R1–R4 as pending implementation.

| Priority/status | Evidence and owner | Smallest next action and observable result |
| --- | --- | --- |
| P1 verification debt | Coordinator; four fixes have source evidence and eight unexecuted cases. | After audit writers finish, compile and run the new fixture plus `ReactivePropertyTests`, `ReactivePropertyLifetimeTests`, `ReactivePropertyConcurrencyTests`. Dead targets count zero without emission; failed initial callbacks return disposable handles; both multicast callbacks receive values; null removal preserves real listeners. |
| P2 unverified platform/lifecycle risk | Reflective `ReactiveWeakInvokerCache<T>.Build` versus reflection-free `SubscribeWeak<TTarget>`. Coordinator. | Verify Mono/IL2CPP delivery, collection and disposal: weak targets remain collectable; fallback preserves callbacks. Add only missing player/lifecycle coverage. |
| P3 confirmed XML inconsistency | `ReactiveProperty.cs` class summary says disappeared subscriptions are “reported once”; `InvokeSafe` silently drops collected targets and count sweeping is also silent. Reactive owner. | Narrow the summary to describe gated/throttled destroyed-target notification warnings and silent collection/count cleanup. IntelliSense must match the already-correct guide; no runtime change. |
| P2 proposal, not a defect claim | `AddSubscription` deduplicates delegates; returned handles share registrations. Weak fallback is only externally visible through gated warnings. | Implement the optional observation API below without changing these established legacy contracts. |

Count reads remain O(n). Capturing an explicit weak target inside its handler retains it. Multicast fallback retains a whole strong delegate and preserves the existing group exception/lifetime behavior. These are documented limitations, not newly demonstrated independent failures.

## Substantial improvement: independent observation with retention metadata

Separate views cannot independently own identical legacy subscriptions or inspect weak-to-strong fallback. Preserve legacy behavior and add:

```csharp
public interface IReactiveObservationSource<T> : IReadOnlyReactiveProperty<T>
{
    ReactiveSubscriptionResult Observe(UnityAction<T> listener,
        ReactiveListenerMode requestedMode = ReactiveListenerMode.Strong,
        bool invokeImmediately = false);

    ReactiveSubscriptionResult ObserveWeak<TTarget>(TTarget target,
        Action<TTarget, T> handler, bool invokeImmediately = false)
        where TTarget : class;
}
```

`ReactiveSubscriptionResult` is an immutable result exposing `IDisposable Handle`, `ReactiveListenerMode EffectiveMode`, and `ReactiveSubscriptionFallbackReason FallbackReason`. Reasons: `None`, `MulticastDelegate`, `UnsupportedOpenInvoker`. Single static callbacks use strong storage with `None`, since no target needs retention. Null inputs return an empty disposal handle and create no registration. Determine metadata before immediate dispatch; initial exceptions still follow existing safe delivery.

Reuse `Subscription`, `Register`, `SubscriptionHandle`, `EmptyDisposable`, `InvokeSafe`, snapshot buffers, `ReactiveWeakInvokerCache<T>` and `NeoDiagnostics`. Each new observation creates an independent registration. Mark legacy-deduplicated registrations separately so `AddSubscription` never accidentally reuses an observation. `RemoveListener` removes every matching delegate registration; disposing a handle removes only its own registration, subject to the current in-flight snapshot guarantee. `RemoveAllListeners` clears both code paths while retaining persistent Inspector callbacks.

Do not extract a new registry/value engine initially: existing focused helpers suffice, and leaving `_value` authoritative avoids duplicate serialized/runtime state. Keep old interfaces unchanged; concrete properties additionally implement the new capability interface. No new component, editor adapter, dependency or mandatory NoCode migration is needed.

## Exact future files

Paths below are relative to `Assets/Neoxider/`:

- `Scripts/Reactive/ReactiveProperty.cs`: P3 summary correction, new observation entry points, legacy registration identity and effective-retention selection.
- `Scripts/Reactive/ReactiveSubscription.cs`: additive capability interface, result and fallback enum; preserve existing public contracts/cache behavior.
- NEW `Tests/Edit/Reactive/Audit20261023NeoReactiveObservationTests.cs` and `.meta`: ownership, metadata and compatibility regressions. Preserve existing tests.
- NEW `Tests/Play/Reactive/Audit20261023NeoReactiveLifetimePlayTests.cs` and `.meta`: temporary host lifecycle and player/AOT scenarios; coordinator handles any new folder metadata.
- `Docs/Reactive/README.md` and `Docs/Reactive/ReactiveProperty.md`: API/migration examples, strong fallback detection and unchanged Inspector workflow.

Both existing test assemblies already reference `Neo.Reactive`; no asmdef edit is currently required. New metadata needs unique GUIDs. Preserve `_value`, `_onChanged`, constructors, wrapper names, enum values and all serialized assets. No samples/scenes move or require migration; transient test hosts avoid dependence on `Samples`, `Samples~` or imported sample paths.

## Implementation order, acceptance and boundaries

1. Coordinator verifies the completed fixes first. Reactive owner corrects the remaining XML wording; any central failure receives a focused reproduction/fix before API expansion.
2. Implement additive types and independent registrations, then minimum useful Edit Mode coverage: two identical observations deliver twice, disposing one leaves one; legacy duplicates still share ownership; legacy/observation registrations do not contaminate deduplication; immediate failure/reentrancy preserves snapshot behavior and usable handles.
3. Verify metadata: multicast fallback preserves both callbacks and reports its reason; a boxed value-type receiver with shared observable output exercises deterministic unsupported-invoker fallback without private reflection. A held explicit-target weak handle must not prevent collection. Actual IL2CPP support/fallback remains a player check.
4. Verify a temporary MonoBehaviour host through repeated enable/disable and destruction: initial state refreshes once per enable, disabled hosts receive nothing, counts return to zero. Silent loading followed by `ForceNotify` refreshes the view; persistent Inspector calls survive runtime listener removal. Add only missing focused coverage, then update both guides.
5. Central release gate: compile on Unity 6.0–6.4 and 6.5+, execute relevant Edit Mode and lifecycle Play Mode cases, and check an IL2CPP player. Keep version switches centralized in `NeoDiagnostics.StableId`; add no local identity API branches. Record exact versions/backend/results, UTF-8, scoped diff and package/sample-path consistency. Compilation alone does not prove AOT weak behavior.

Cross-assembly order: `Neo.Extensions` owner finalizes diagnostics before central Reactive compilation; preserve its gates. Then `Neo.NoCode` owner verifies `NoCodeFormattedText.cs` AddListener/RemoveListener lifecycle. `Neo.Network` owner verifies `Core/NetworkReactiveProperty.cs` bridge silent-set/forced-notify behavior and `Core/NetworkReactiveSync.cs` wrapper compatibility. These are consumer checks, with no required edits or reverse dependencies. Shared test/package configuration remains coordinator-owned.

Stop release claims on compile failures, lost/duplicate delivery, retained explicit weak targets, altered legacy semantics or missing version/player evidence. Correct owned failures; route others to their manifest owner. Optional API scheduling is a product decision; verified phase-1 fixes can integrate independently. Estimate: 3–5 days plus central validation; medium identity/reentrancy/retention risk. No Unity/build/test processes ran during planning.
