# Neo.NoCode: audit

Status: source audit and scoped fixes complete; Unity compilation and test execution pending centrally.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/NoCode/Neo.NoCode.asmdef`.
- Scope: 7 C# files; nested assemblies excluded.

## Scope

Read the root `AGENTS.md`, the entire `WORKER-CONTRACT.md`, this assembly's exact manifest entry and initial report. Reviewed all **7/7 C# inventory files**, 1,090 baseline lines, without including another assembly's source in the edit scope:

| File under `Assets/Neoxider/Scripts/NoCode/` | Review coverage and result |
| --- | --- |
| `ComponentFloatBinding.cs` | All 407 baseline lines: public setters, host/root resolution, destroyed Unity objects, cached reflection metadata, member eligibility, exception boundaries, raw/numeric conversions, reactive instance access, gated diagnostics. Fixed host identity, explicit root clearing and overloaded-indexer handling. |
| `Neo.NoCode.InternalsVisibleTo.cs` | All 3 lines: friend assembly matches the existing `Neo.Editor.Tests` name. No change needed. |
| `NoCodeBindText.cs` | All 74 lines: target priority, lazy resolution, enable/validation lifecycle, destroyed/replaced targets and invariant formatting. Fixed target cache invalidation/recovery. |
| `NoCodeFloatBindingBehaviour.cs` | All 192 lines: Once/Reactive/Poll modes, unscaled polling, late-source retries, listener ownership, rewiring, disabled/manual refresh and callback reentrancy. Fixed stale subscriptions and disabled-view behavior. |
| `NoCodeFloatUpdateMode.cs` | All 21 lines: serialized enum values remain 0/1/2; behavior agrees with component modes. No change needed. |
| `NoCodeFormattedText.cs` | All 349 lines: multiple/missing/null sources, fallback polling, subscription reconciliation, duplicate sources, formatting errors, boxed-value reuse, target resolution, event deduplication and enable/disable lifecycle. Fixed source lifetime and target-write issues. |
| `SetProgress.cs` | All 44 lines: assigned Slider/Image targets, normalized output, reversed/clamped ranges and approximately equal endpoints. No standalone defect established; inherits base subscription corrections. Existing regressions cover both target types and reactive updates. |

Read-only context: existing NoCode EditMode tests, the NoCode/test asmdefs, `BindingSourceGameObjectResolver`, `ReflectionCache`, reactive listener registration/removal/dispatch and `ListenerCount`, `NeoDiagnostics`, and relevant `SetText`/`TimeToText` APIs. Reuse check used the installed NeoxiderTools `tools.md`/`extensions.md` references; fixes continue using the existing resolver/cache, typed reactive properties and UI APIs. No replacement service/package was introduced.

Four existing guides under `Assets/Neoxider/Docs/NoCode` are owned. New tests use only the assigned prefix. No other assembly, existing test, shared configuration, scene, prefab or sample layout was edited.

## Findings and fixes

### NOCODE-01 — Medium: reactive subscriptions survive source rewiring

**Trigger:** change `Binding.SourceRoot`/`MemberName`, replace the reactive object stored in a member, or rewire/remove a formatted source, then refresh.

**Observable consequence:** the initial read can display the new value, but subsequent changes to the new reactive instance do not reach the UI. The old property retains a listener/view reference. Formatted bindings additionally retain subscriptions for removed entries.

**Baseline evidence:** `NoCodeFloatBindingBehaviour.TrySubscribeReactive` returned immediately on `HasReactiveSubscription` (baseline lines 113–124). `NoCodeFormattedText.SubscribeReactiveSources` only retried unresolved entries after its first subscription pass (147–205); it never reconciled already-resolved instances.

**Fix:** each refresh compares currently resolved reactive instances to owned listeners. The single-value component unsubscribes before switching instance/type. The formatted component compares the three typed subscription sequences, rebuilds only when they differ and recalculates whether polling is needed. Ordinary/missing sources keep their fallback; resolving the last missing source to reactive can stop it. Existing serialized fields and public signatures are preserved.

**Regressions:** `SingleBinding_SourceRootChanged_*`, `SingleBinding_ReplacedReactiveInstance_*`, parameterized `SingleBinding_MemberChanged_*`, `FormattedText_RewiredSource_*`, `FormattedText_ReplacedReactiveInstance_*`, parameterized `FormattedText_MemberChanged_*`, `FormattedText_RemovedSource_*` and `FormattedText_DuplicateSources_*`. Assertions check old/new `ListenerCount`, actual TMP output and single notification delivery for duplicate sources.

### NOCODE-02 — Medium: disabled refresh re-subscribes; in-flight pushes update disabled views

**Trigger:** call the public formatted refresh or a subclass/editor float refresh after disabling; alternatively, an earlier source listener disables the view during the same reactive notification.

**Observable consequence:** a disabled view acquires listeners again and receives later pushes. Even a correctly removed listener can receive the current notification because the reactive dispatcher intentionally uses a subscriber snapshot.

**Baseline evidence:** both subscription methods lacked an `isActiveAndEnabled` guard. All six typed callbacks unconditionally refreshed. The existing reactive dispatch implementation snapshots listeners before invoking them.

**Fix:** subscription synchronization releases listeners and returns while disabled/inactive or outside Reactive mode. All typed callbacks ignore notifications after deactivation. Explicit manual refresh remains a one-time read/write while disabled.

**Regressions:** `SingleBinding_DisabledManualRefresh_*`, `SingleBinding_DisabledByEarlierListener_*`, `FormattedText_DuplicateSources_NotifyOnceAndReleaseOnDisabledRefresh`, and `FormattedText_DisabledByEarlierListener_*`. Observe listener release, unchanged target text and event counts; no assertion relies only on absence of an exception.

### NOCODE-03 — Medium: text target caches lose late/replaced/rewired output

**Triggers and consequences:**

- `NoCodeFormattedText` first formats with no UI target. A target is later added, but an unchanged value returns early at baseline lines 311–315, leaving that target blank indefinitely.
- Destroy all previously cached text targets and add a replacement. Both components retained `_resolved == true`, so the replacement was never looked up.
- Change an explicit `NoCodeBindText` target in the Inspector while enabled. Its inherited validation invalidated only the source; cached output references continued writing to the old target.

**Fix:** resolve again when no cached target is alive. Formatted refresh distinguishes a newly resolved target from a changed value: it writes unchanged text to that target without duplicating `OnTextChanged`. `NoCodeBindText.OnValidate` marks its target cache unresolved without performing component searches during validation.

**Regressions:** `FormattedText_LateTarget_*`, `FormattedText_DestroyedTarget_*`, `SingleBinding_DestroyedTextTarget_*` and `SingleBinding_InspectorTargetChanged_*`. Check new target contents, old target contents where applicable and zero duplicate formatted-value events.

### NOCODE-04 — Medium: cached binding reads the previous host

**Trigger:** reuse one `ComponentFloatBinding` with no explicit root by passing host A, then host B.

**Observable consequence:** B's read returns A's value, despite the documented host fallback.

**Baseline evidence:** `EnsureCache` returned an existing live component/member before checking host context (144–153).

**Fix:** retain the successful host as nonserialized cache state; changing hosts invalidates resolution. No serialized layout change.

**Regression:** `Binding_ReusedWithDifferentHost_ReadsCurrentHost` checks A=10, B=20 and switching back to A.

### NOCODE-05 — Medium: a destroyed explicit root cannot be cleared through its setter

**Trigger:** destroy `SourceRoot`, then assign `binding.SourceRoot = null` to restore intentional host fallback.

**Observable consequence:** resolution keeps failing because Unity's overloaded equality treats the destroyed object as null, so the old setter skips assignment. The shared resolver correctly refuses to silently use host fallback for a destroyed explicit reference.

**Baseline evidence:** `SourceRoot` compared `_sourceRoot != value` (53), while `BindingSourceGameObjectResolver` explicitly distinguishes a destroyed sentinel from an unassigned reference.

**Fix:** compare managed reference identity in the setter so explicit clearing actually stores null and invalidates the cache. Retain failure for a destroyed root until it is deliberately cleared/replaced.

**Regression:** `Binding_DestroyedExplicitRoot_CanBeClearedToUseHost` checks failure before clearing and the host's actual value afterward.

### NOCODE-06 — Low: unsupported overloaded indexers throw from the Try API

**Trigger:** resolve `Item` on a component declaring indexers with different parameter types.

**Observable consequence:** reflection throws `AmbiguousMatchException` before the existing non-indexed-property check can reject the member.

**Baseline evidence:** `TryResolveSupportedSourceMember` directly called `ReflectionCache.GetProperty` (261); the dependency forwards to `Type.GetProperty` without resolving overload ambiguity.

**Fix:** handle that ambiguity locally and continue the supported-field lookup. No changes to the dependency or its other callers.

**Regression:** `Binding_OverloadedIndexers_AreRejectedAsSources` observes the public resolver returning false with no member for a real overloaded-indexer type.

All reproductions above are established from current source/control flow and expressed as executable regressions. They have **not** been executed in Unity in this worker session.

## Documentation

- `Assets/Neoxider/Docs/NoCode/README.md`: canonical scene/runtime contract, actual target priorities, missing-source behavior, polling controls, source rewiring, listener lifetime, target recovery and event semantics. Corrected the verified release sample path and explained developer/imported layouts without changing samples.
- `ComponentFloatBinding.md`: public source setters, host/cache behavior, destroyed-root clearing, raw/numeric resolution, invalid members, a C# example, float precision and stripped-build considerations.
- `NoCodeFloatBindingBehaviour.md`: a custom C# view example, explicit refresh after runtime configuration, lifecycle override requirements and polling/disabled behavior.
- `GettingStarted.md`: concrete Inspector steps for text/progress/multiple sources, late-object retries and the available C# refresh paths.
- Corrected the binding XML summary: reflection metadata is cached, but values are still read reflectively on each refresh.

## Substantial improvement proposal

**Proposed, not implemented: typed value sources plus an explicit binding-session API.**

**Problem:** the two wrappers duplicate subscription/polling/lifetime rules; this audit found the same ownership defects in both implementations. Source selection still uses reflection/string member names. The single-source wrapper exposes source configuration but only a protected runtime refresh, while formatted sources have no equivalent public configuration API. Idle reactive views cannot discover a replaced source until an explicit refresh or another notification. Float conversion also loses precision for large integer values.

**Architecture/public API:**

- A plain C# `IFloatValueSource.TryRead(out float value)` contract and optional `IObservableFloatValueSource.Subscribe(Action<float>)`, returning `IDisposable` with no immediate callback by default. Plain values and reactive values remain separate capabilities.
- A plain C# `FloatBindingSession` owns the current source and disposable listener. `Bind(source, apply)`, `Refresh()`, `Tick(float unscaledTime)` and idempotent `Dispose()` make replacement, polling and cleanup explicit. Bind disposes the previous subscription before reading/subscribing to its replacement; enable/disable policy stays in the scene wrapper.
- Adapter implementations use existing `ReactiveProperty*.Subscribe` and `ComponentFloatBinding` rather than adding another reflection/search stack. Keep reflected/scene lookup in the adapter; typed code paths avoid string lookup. No ScriptableObject stores scene references.
- Add public `ConfigureSource`/`Refresh` on the single-value wrapper and `ConfigureSources`/format configuration on the formatted wrapper. A formatted session owns unique subscriptions for its sources and preserves text-event deduplication. Exact integer/text formatting should use a separately designed typed source path rather than pretending float is lossless.

**Compatibility/migration:** retain enum values, serialized fields, current source descriptors, Inspector drawers, target priority and default poll timing. Existing serialized bindings build adapters internally; typed configuration is opt-in. Do not rename/move serialized fields or change assemblies in the first phase. Introduce API documentation and an adapter parity test suite before replacing the duplicated internals.

**Acceptance scenarios:**

1. Pure C# fake sources verify initial read, Bind(A)→Bind(B), old-source disposal, failed reads, interval polling and idempotent disposal without a scene.
2. Repeated enable/disable, manual refresh while suspended and disable during a notification produce no retained listeners or late automatic UI writes.
3. Float/int/bool source replacement, duplicated formatted slots, mixed reactive/plain sources and late scene resolution preserve existing visible output; all audit regressions pass.
4. All existing serialized examples load without migration, including Once/Reactive/Poll and imported samples.
5. An IL2CPP player exercises the typed path without reflection preservation; the legacy adapter's preservation requirements are documented and verified separately.

**Effort/risk:** approximately 3–5 engineering days plus a coordinator-owned Unity/player validation pass. Medium risk at lifecycle/event ordering and formatted subscription deduplication; mitigate through an additive API, adapter parity checks and keeping serialized authoring unchanged. No additional dependency is needed.

## Verification and remaining risks

### Checks actually performed

- Read every inventory source file in full and inspected the final scoped runtime diff. Reviewed source resolution, serialization, lifecycle/reentrancy, caching, public API, dependencies, version-sensitive calls, logging and existing tests.
- Scoped Python static checks passed for **13 source/docs/test/metadata files**: strict UTF-8 decoding, no replacement characters/trailing whitespace, explicit C# declarations without `var`, and balanced C# delimiters after excluding comments/string literals. These are consistency checks, not compilation or behavioral test results.
- Compared owned sources with baseline `ca198556`: **28 serialized field declarations unchanged** and **35 existing public/protected declaration lines retained**. The only added protected override is target-cache validation; public signatures remain unchanged.
- All local Markdown links in the four updated guides resolve. The release NoCode demo scene exists under `Samples~`; the developer `Samples` path is absent, matching the active layout.
- The new test's `.meta` GUID occurs once in package metadata. Existing `Neo.Editor.Tests` references cover the used APIs; no additional assembly reference was requested.
- Scoped `git diff --check -- <owned source/docs/new test paths>` passed with exit code 0.

### Regression artifact and central validation

New file: `Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeBindingTests.cs`, with a unique-GUID `.meta`. **20 declared test cases**, including parameterized float→int/bool changes. Tests observe TMP contents, formatted events, typed listener counts and binding results. Editor validation dispatch in the setup simulates Inspector rewiring; it is not the assertion under test.

Coordinator should compile and run this fixture together with existing `NoCodeBindEditModeTests`, `NoCodeBindLateSpawnTests` and `NoCodeBindTextLazyResolveTests`. In particular retain the existing mixed reactive/plain-source polling and late-spawn coverage after subscription reconciliation. No build/test runner, Unity/MCP, install, shared configuration change, commit or child agent was used here.

**Unverified:** C# compilation/import; execution of new/existing regressions; actual UI rendering, Player/IL2CPP stripping and both Unity-version branches. Static delimiter/API checks cannot establish these results. Reflection remains in the value-read path; reconciliation now also reads existing reactive member references during refresh. A purely reactive idle source is replaced only after an explicit refresh/re-enable or a subsequent source notification, as documented.

**Cross-boundary actions:** central compile/tests only. No code/document edit is requested from another assembly owner. `ReflectionCache` remains untouched; overloaded-indexer rejection is handled at the NoCode contract boundary.

Кратко: проверены все 7 файлов; исправлены подписки и жизненный цикл UI, восстановление текстовых целей, контекст host и очистка уничтоженного SourceRoot. Добавлено 20 регрессионных случаев. Статические проверки пройдены; запуск Unity и тестов оставлен координатору по контракту.
