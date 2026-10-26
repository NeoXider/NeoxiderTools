# Neo.Reactive: audit

Status: source audit and scoped static verification complete; Unity compilation and test execution pending with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Reactive/Neo.Reactive.asmdef`.
- Scope: 2 C# files; nested assemblies excluded.

## Scope

Reviewed **2/2 owned C# files completely**. The current assembly directory contains exactly the two manifest files and no nested assembly. Owned documentation is `Assets/Neoxider/Docs/Reactive/`; no excluded/shared documentation was edited.

| File | Review coverage |
| --- | --- |
| `Assets/Neoxider/Scripts/Reactive/ReactiveProperty.cs` | Primitive events and all generic/concrete constructors; equality/value/null semantics; silent writes and forced emission; code/event subscription boundaries; strong/weak/static/multicast callbacks; duplicate registrations; disposable handles; immediate delivery; destroyed/collected targets; reentrant writes and snapshot mutation; buffer ownership/clearing; serialization and gated diagnostics. |
| `Assets/Neoxider/Scripts/Reactive/ReactiveSubscription.cs` | Liveness states; invoker cache synchronization and negative caching; open-instance delegate construction; reference/value-type/static method handling; reflective/AOT fallback; listener modes; read-only/read-write interfaces and compatibility. Executable code in this file required no changes; XML contracts were corrected. |

Read-only context: the existing three Reactive Edit Mode test files, `Neo.Reactive.asmdef`, `Neo.Editor.Tests.asmdef`, relevant `NeoDiagnostics` methods, both owned public guides, and the installed NeoxiderTools module/idiom/extension references. Existing subscription and diagnostics primitives were reused; no replacement helper/framework, new dependency, or assembly reference was introduced.

Scene/NoCode review: these are serialized helper values on a host component, with `OnChanged` as the Inspector adapter. They are not standalone components. Serialized backing-field edits do not execute the property setter, and removing runtime listeners does not remove persistent Inspector calls. Code callbacks and UnityEvent callbacks have different exception boundaries. All of these distinctions are now documented.

## Findings and fixes

### R1 — P2: listener count stays stale until a notification discovers a dead target

- **Trigger:** register a strong/weak Unity object listener, destroy the target, then read `ListenerCount` without changing the value. Collected weak C# targets have the same issue.
- **Observable consequence:** the getter returns `1` for an already dead registration despite promising a live count. With a strong listener, even the destroyed managed shell stays retained until another cleanup trigger.
- **Source evidence:** `ReactiveProperty.cs`, `ListenerCount` and `CompactDeadListeners`. The getter previously called the sweep with `_hasDeadListeners == false`; the sweep returned immediately. Only `InvokeSafe` set that flag after observing a dead target during delivery.
- **Fix:** a count read explicitly requests a full liveness sweep; notification cleanup keeps its existing flag-driven fast path. Counting neither emits values nor logs.
- **Regressions:** destroyed strong and weak targets, plus an explicitly weak C# target collected in a separate non-inlined scope, are counted correctly without any notification. The unchanged property value is asserted as well.

### R2 — P2: explicit-target immediate delivery bypasses subscription safeguards

- **Trigger:** call `SubscribeWeak(target, handler, invokeImmediately: true)` with a throwing handler or an already destroyed Unity target.
- **Observable consequence:** an exception escapes after the subscription has been registered and prevents the caller receiving its disposal handle; a destroyed target can also be invoked immediately. Ordinary `Subscribe` and the delegate weak overload already used safe dispatch.
- **Source evidence:** `ReactiveProperty.cs`, `SubscribeWeak<TTarget>` directly called `handler(target, _value)` after `Register` instead of using `InvokeSafe`.
- **Fix:** route initial delivery through the existing liveness/exception path. This preserves exception gating and returns the normal handle after a failed initial callback.
- **Regressions:** expect the specific exception log, assert initial and later delivered values/call counts, dispose twice and verify no later delivery; separately assert that a destroyed target's managed callback is never reached.

### R3 — P2: a weak multicast subscription silently loses earlier callbacks

- **Trigger:** combine two instance method delegates and pass the result to `AddWeakListener` or `SubscribeWeak` on a platform supporting the open invoker.
- **Observable consequence:** only the final method is dispatched; earlier subscribers never observe the value. Immediate delivery has the same truncation.
- **Source evidence:** `ReactiveProperty.cs`, `CreateSubscription`, previously built one open invoker from `call.Method` and one weak reference from `call.Target`; those describe the final multicast invocation, rather than its entire invocation list.
- **Fix:** inspect the invocation list before selecting a single-target open invoker. A multicast registration uses the existing strong fallback, retaining the complete delegate and reporting the fallback through the existing gated/throttled warning. Single static callbacks keep their fast path. Strong-mode behavior and one-registration counting remain compatible.
- **Regressions:** with and without immediate delivery, both instance targets receive the correct value; duplicate registration is still idempotent; removing the original multicast delegate stops all its callbacks.
- **Limit:** this is an explicitly documented **strong** fallback, not newly implemented weak ownership of multiple targets. Use separate instance subscriptions for independent weak lifetime and exception isolation.

### R4 — P2: removing a null delegate reaches UnityEvent's non-null removal API

- **Trigger:** call `RemoveListener(null)` on a normally constructed property.
- **Observable consequence:** the code-list guard skips null, but the trailing `_onChanged.RemoveListener(call)` still receives it; UnityEvent's delegate target/method access throws. Cleanup therefore fails even though null registration is already treated as a no-op.
- **Source evidence:** `ReactiveProperty.cs`, `RemoveListener`, previously guarded null only around the code-list loop, not around UnityEvent removal.
- **Fix:** return before either removal path for a null delegate.
- **Regression:** remove null, change the value, and assert the existing real subscriber still receives the value and remains registered.

All findings above are supported by inspected source paths and new executable regression cases; **the regressions have not been run in this worker**. No runtime reproduction is claimed.

### Reviewed contracts preserved without speculative fixes

- `_value`, `_onChanged`, public declarations, constructors and the assembly configuration remain compatible with baseline `ca198556`.
- Value comparison still uses `EqualityComparer<T>.Default`; `OnNext` uses the setter, silent writes emit nothing, and forced emission includes unchanged values.
- The outer notification snapshots its value/listeners, nested notification uses a separate buffer, and buffers are cleared after invocation so removed listeners are not retained by snapshots.
- Delegate-based registration remains deduplicated: separate handles for the same delegate share one registration. Explicit-target registrations remain independent. This distinction is documented rather than silently changed.
- Cache construction retains open delegates rather than subscriber instances. Unsupported reflective/AOT instantiations remain negatively cached and use the existing strong fallback. The actual platform branches still need central execution.
- Weak handlers must not capture their target; this existing caller precondition is now explicit in public docs. Static listeners need no weak ownership. Disposal remains idempotent.
- UnityEvent retains its own persistent/runtime listener behavior and exception ordering. `NeoDiagnostics` continues to gate exceptions and gate/throttle warnings.
- No direct Unity object identity/version-specific API, editor-only import, scene search, ScriptableObject scene reference, scene/prefab change or sample-layout dependency was introduced. Access remains Unity-main-thread-only.

## Documentation

- Updated `Assets/Neoxider/Docs/Reactive/README.md`, the canonical entry, with both observation/mutation interfaces, disposable subscription lifecycle, count semantics, duplicate ownership, weak fallback and the scene/Inspector workflow. The example disposes its subscription on disable.
- Updated `Assets/Neoxider/Docs/Reactive/ReactiveProperty.md` with immediate-delivery guarantees, null removal, persistent UnityEvent boundaries, multicast/AOT fallback, noncapturing handler requirements, interface availability, nested notification semantics, allocation costs and count-scan cost.
- Removed contradictory claims that disposable subscriptions do not exist and that explicit-target subscription setup allocates nothing. Removed the unnecessary legacy Rpg dependency from the lifecycle example.
- Corrected relevant XML comments in both owned C# files. No new public guide or documentation folder was created.

Кратко: исправлены устаревший счётчик подписок, небезопасный первый вызов слабой подписки, потеря multicast callbacks и удаление null-делегата. Документация объясняет C# API и Inspector workflow; выполнение Unity-тестов остаётся у координатора.

## Substantial improvement proposal

**Proposed only — explicit subscription leases with observable retention.**

**Problem:** current `Subscribe` handles do not identify independent consumer ownership when the delegate is duplicated. Weak requests can become strong because of AOT support or multicast input, but the returned `IDisposable` cannot report the actual retention mode. Warnings are gated by default, so a caller cannot reliably inspect whether a long-lived property now retains its subscriber. The corrected multicast fallback makes this limitation especially visible.

**Architecture/public API:** add an opt-in `Observe` registration API returning a result such as:

```csharp
public readonly struct ReactiveSubscriptionResult
{
    public IDisposable Handle { get; }
    public ReactiveListenerMode EffectiveMode { get; }
    public ReactiveSubscriptionFallbackReason FallbackReason { get; }
}

public ReactiveSubscriptionResult Observe(
    UnityAction<T> listener,
    ReactiveListenerMode requestedMode = ReactiveListenerMode.Strong,
    bool invokeImmediately = false);
```

The snippet describes an API shape, not implemented code. The result's fallback reason would distinguish `None`, `MulticastDelegate` and `UnsupportedOpenInvoker`. Each `Observe` call owns a distinct registration token even for the same callback. Keep the legacy delegate-deduplication index separate from registration identity; a small plain C# registry should own dispatch snapshots, token removal and retention metadata, while the existing property remains the serialized value/UnityEvent adapter. Add an explicit-target counterpart that avoids reflection, and expose this new capability through an additive interface rather than adding members to the existing read-only interface and breaking external implementers. A caller can reject/dispose a fallback immediately using the result, without enabling global logs.

**Compatibility/migration:** retain existing `AddListener`/`Subscribe` signatures and their documented deduplication behavior. Keep `_value` and `_onChanged` serialization intact and introduce no required scene migration. Consumers opt into `Observe` where ownership scopes or retention requirements matter; existing prefabs and UnityEvent wiring continue unchanged. Clarify how legacy `RemoveListener` removes all matching registrations, while a lease disposes only its own registration. Any lazy runtime registry must be reset/synchronized after deserialization without emitting a value.

**Acceptance scenarios:**

1. Two views observe the same delegate; disposing one lease leaves the second receiving exactly once. Repeated disposal is harmless.
2. Legacy duplicate `AddListener`/`Subscribe` behavior remains unchanged, including snapshot behavior during remove/reentrant delivery.
3. A supported weak instance subscription reports `Weak/None`, does not retain its target through a held handle, and disappears after collection.
4. Multicast and refused AOT instantiations preserve every callback and report `Strong` with the precise fallback reason; callers can reject that retention through the handle.
5. Explicit-target subscriptions work in an IL2CPP player without reflective factory instantiation; captured-target handler restrictions are documented and checked where practical.
6. Inspector values, persistent callbacks, silent save loading, initial delivery and subsequent view refresh still behave identically. Warm non-reentrant delivery keeps its reusable snapshot buffer.

**Effort/risk:** approximately 3–5 engineering days plus Mono/IL2CPP and Unity-version validation. Medium risk: the key migration hazards are mixing legacy deduplication with independent lease identity, preserving callback ordering under reentrancy, and keeping serialized backing values synchronized with lazily recreated runtime state. This proposal is intentionally outside the completed bug fixes.

## Verification and remaining risks

### Actual checks performed

- Inspected the complete manifest inventory and independently enumerated the Reactive source directory: **2/2 C# files**, no nested assemblies.
- Added `Assets/Neoxider/Tests/Edit/Reactive/Audit20261023NeoReactiveRegressionTests.cs`: **6 test methods / 8 cases**, using public behavior rather than private reflection/source assertions. Added its unique GUID `.meta`; did not edit existing tests or shared asmdefs. Existing test assembly references already cover the required types.
- Python standard-library static validation passed for six package files: strict UTF-8 decoding, no replacement characters, trailing whitespace or conflict markers. Explicit C# type use, absence of direct version-specific identity calls, and balanced lexical delimiters passed for both owned sources plus the new test file. These checks are not a C# compilation.
- Compared public/protected declaration lines and serialized field declarations against `git show ca198556:<file>` for both owned sources: unchanged. Both owned Markdown guides' relative links resolve.
- Verified unique fixture method names/case count and searched package asset metadata for the new GUID: exactly one matching file. The first method-name check incorrectly included same-named helpers in different nested test classes; narrowing it to fixture methods resolved the validator's false positive. No product-code failure was concealed.
- Reviewed the scoped source/documentation diff. Final `git diff --check --` with the exact two sources, two guides, new test and metadata, and this report passed. Git checks the tracked changes; whitespace and UTF-8 of the new untracked test, metadata and completed report were also validated directly. The report has all required sections and no queue placeholders.

### Pending central verification / limitations

- **Not run:** Unity import/compilation, Edit Mode or Play Mode tests, builds, IL2CPP, GC/runtime execution, or Unity 6.0–6.4/6.5+ checks. The user expressly reserved all runners and Unity access for the coordinator. Static evidence does not establish that the new NUnit cases pass.
- Coordinator: run the new `Audit20261023NeoReactiveRegressionTests` fixture and the existing three Reactive fixtures after the concurrent audit batch compiles; include player/AOT coverage for the open-invoker fallback and explicit-target form. No new assembly reference is requested.
- Expected tradeoffs remain: count reads are O(n); weak multicast/AOT fallback retains a whole delegate strongly; captured explicit handlers can retain their target; legacy duplicate subscription handles share ownership; UnityEvent callbacks do not have per-callback exception isolation. These are documented, with the ownership/retention improvement proposed above.
- No cross-boundary source defect requiring another worker's edit was identified. No commits, configuration edits, Unity/MCP calls, installs, child agents or runner invocations were performed.
