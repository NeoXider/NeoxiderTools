# Neo.Tools.Managers: audit

Status: source audit and scoped fixes complete; Unity compilation and test execution pending centrally.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Managers/Neo.Tools.Managers.asmdef`.
- Scope: 6 C# files; nested assemblies excluded.

## Scope

Read `AGENTS.md`, the complete worker contract, this assembly's exact manifest entry, and the original report before editing. Reviewed every owned C# file, all 842 original source lines. The source inventory matches the six files belonging to `Neo.Tools.Managers`; no nested assembly source is included.

| File under `Assets/Neoxider/Scripts/Tools/Managers/` | Original lines | Review coverage and result |
| --- | ---: | --- |
| `Bootstrap.cs` | 144 | `IInit`, serialized manual targets, optional loaded-scene/inactive discovery, priority ordering, duplicate tracking, runtime registration, reentrant callbacks, removal/destruction during a batch, exceptions, persistent lifetime. Fixed queue mutation and Unity-null defects. |
| `EM.cs` | 132 | All gameplay helpers, nullable event dispatch, optional singleton resolution, former serialized event names, `OnAwake`, application focus/pause/quit forwarding. Helpers broadcast without changing state. Restricted runtime Inspector actions to Play Mode. |
| `GM.cs` | 221 | All enum states and methods, same-state assignments, previous-state tracking, start notification, FPS side effect, optional hub notification, synchronous listeners, pause ownership, destruction and logging. Fixed pause/state defects; preserved serialized configuration and event APIs. |
| `Singleton.cs` | 167 | Lazy lookup including inactive objects, miss caching, auto-creation and hook timing, aliases, non-resolving accessors, duplicate ownership, initialization flags, persistence, deferred destruction and static reset. Fixed early-resolution persistence and re-resolution of a doomed instance. |
| `SingletonById.cs` | 87 | Invalid/empty keys, case-sensitive dictionary lookup, new-wins replacement, deferred old destruction, ownership check on unregister, persistence, immutable authoring Id and static reset. Implementation unchanged; replaced placeholder documentation with actual contracts and lifecycle limitations. |
| `SingletonRuntimeReset.cs` | 91 | Both generic bases, assembly/type enumeration failures, partial `ReflectionTypeLoadException` results, abstract/open type filtering, indirect subclasses, closed-base method resolution, repeated idempotent resets. Existing indirect-subclass fix is present; no speculative reset rewrite. |

Read existing `BootstrapTests`, the singleton sweep regression in `AuditFixesStaticsTests`, relevant module-principle constraints, and Edit/Play test assembly references as context. The new tests need only references already present. Existing tests, asmdefs, scenes, prefabs, sample layout, shared configuration, and unrelated `ScoreManager.md` were not edited.

Reuse receipt: consulted the `neoxider-tools` manager catalog and object-lifetime extension catalog, then inspected `ObjectExtensions.IsValid`. Bootstrap's small interface lifetime adapter delegates Unity validity to that existing API through the assembly's existing `Neo.Extensions` reference. No replacement manager framework or dependency was added.

## Findings and fixes

Locations below identify pre-change source evidence. The new tests assert callback traces, counters, game state, time scale, or actual scene ownership; none are source-text or no-exception-only regressions.

### MGR-01 — High: bootstrap callbacks can repeat current initialization or strand startup registrations

- Trigger: an `IInit.Init()` calls `Bootstrap.Register()` for another target.
- Evidence: `Bootstrap.cs:81-85,129-141` starts a fresh pending pass after startup, but the current target is not marked initialized until its callback returns. Startup uses a single snapshot while `_bootstrapCompleted` is false.
- Observable consequence: runtime parent initialization runs twice for a simple parent/child chain; a chain creating fresh services can recurse without bound. A child registered during the first startup pass remains uninitialized until some later new registration happens.
- Fix: one guarded drain finishes the current priority batch, then drains newly registered targets in additional sorted batches. A `finally` releases the guard on exceptions. Replaced the touched `var` declaration with `List<IInit>`.
- Regression evidence: `Startup_DrainsRegistrationsMadeDuringInit_AfterTheCurrentPriorityBatch` asserts `high, low, late`; `RuntimeRegistration_FromInit_DoesNotReenterTheParent` asserts exactly `parent, child` and one initialization each. `FailedInit_ReleasesTheDrainGuard_ForExplicitRetry` verifies a failed runtime registration can be explicitly retried.

### MGR-02 — Medium: removed or destroyed targets remain callable from a bootstrap snapshot

- Trigger: an earlier initializer unregisters or destroys a later initializer, or a destroyed component is passed through an `IInit` reference.
- Evidence: `Bootstrap.cs:76,94,131-140` uses CLR interface null checks and invokes the original snapshot without rechecking registration or Unity lifetime.
- Observable consequence: canceled initialization still executes. A destroyed component can run managed callback code or fail on native Unity state access.
- Fix: reject dead Unity registrations, check current membership and lifetime immediately before each invocation, and prune dead registrations/initialized records at the next drain. A real CLR-null check still permits `Unregister` to remove destroyed interface references. A destroyed manual component is skipped during collection.
- Regression evidence: `Unregister_DuringAnEarlierInit_CancelsThePendingTarget`, `DestroyedUnityTarget_IsSkippedWhenAnEarlierInitDestroysIt`, and `Register_RejectsADestroyedUnityTargetHeldThroughIInit` assert the actual callback trace and successful live registrations. Unregister/re-register is also checked to allow a later initialization.

### MGR-03 — High: GM can leave time frozen or restore the wrong value after pause

- Triggers: `Pause()` followed by `StopGame()`, `Preparing()`, direct state reassignment, destruction, a changed `useTimeScalePause` option, or a listener that immediately resumes during pause notification. An already-zero external time scale is another boundary case.
- Evidence: `GM.cs:103-106,129-137,179-216` writes pause time after synchronous notifications, restores it only in `Resume()`, conditions restoration on the current option, and replaces a captured zero with one. There is no destruction cleanup.
- Observable consequence: a non-paused game state can retain `Time.timeScale == 0`; listeners see the old time scale; toggling the option strands the capture; resume can unpause time owned by another system.
- Fix: track whether this manager actually captured time. Capture/apply pause before callbacks; release the capture before notifying any distinct transition out of Pause and during `OnDestroy`. Restore the exact captured value independently of the option's later value. `Menu`/`StartGame` retain their existing resume-event flow; other direct exits restore time without adding a new `OnResume` event.
- Regression evidence: four gameplay exit cases plus direct-state notification ordering, captured zero, option changes, `OnPause`/`OnStateChange` reentrancy, resume callback observations, destruction-handler cleanup, and event-only pause assert actual `State` and `Time.timeScale` values. Destruction-handler coverage uses a protected lifecycle seam in Edit Mode; real Unity destruction remains a central Play Mode integration check.

### MGR-04 — Medium: repeated Pause state assignment destroys the resume target

- Trigger: `Pause(); State = GameState.Pause; Resume();`.
- Evidence: `GM.cs:46-47` overwrites `_lastState` even when the assigned state is unchanged.
- Observable consequence: `Resume()` assigns Pause again rather than the prior gameplay state.
- Fix: update the previous state only on a distinct transition. Repeated assignments still emit the existing `OnStateChange` notification.
- Regression evidence: `AssigningTheSamePauseState_DoesNotOverwriteTheResumeTarget` checks both the restored Game state and captured time scale.

### MGR-05 — Medium: resolving an inactive persistent singleton bypasses persistence

- Trigger: access `I` on an inactive manager with persistence enabled before its Unity `Awake`, then activate it.
- Evidence: `Singleton.cs:36-64` initializes a lazy result without persistence; `Awake` at `95-115` applies persistence only when `_instance` was null, so it skips an already-resolved instance.
- Observable consequence: a manager configured to survive scene loads remains in its scene and is destroyed when that scene unloads.
- Fix: apply persistence during lazy runtime initialization. `Awake` recognizes the same registered instance, applies persistence, and initializes only if required. Persistence calls are restricted to Play Mode so editor-side resolution does not invoke an unsupported Unity operation.
- Regression evidence: `ResolvingBeforeAwake_AppliesPersistence_AndInitializesOnlyOnce` asserts real `DontDestroyOnLoad` scene ownership, instance identity, and an initialization count of one before and after activation.

### MGR-06 — Medium: DestroyInstance can immediately return the object scheduled for destruction

- Trigger: call `DestroyInstance()` and access `I` again in the same frame.
- Evidence: `Singleton.cs:143-150` clears the cache while Unity's `Destroy` is deferred; the lazy scene search at `36-43` has no pending-destruction exclusion.
- Observable consequence: `I` resolves the doomed instance again, including when `CreateInstance` was expected to provide a replacement. The returned object disappears at frame end.
- Fix: mark explicit pending destruction and exclude the marked object from resolution and registration. Clear the failed-search cache on explicit destruction and successful resolution.
- Regression evidence: Play Mode cases assert an immediate null result with creation disabled, an immediate distinct initialized replacement with creation enabled, actual old-object destruction after a frame, and replacement cache survival after old `OnDestroy`.

### MGR-07 — Medium: runtime Inspector actions are available during authoring

- Trigger: click a GM state action or EM broadcast button outside Play Mode.
- Evidence: all 9 GM and 11 EM actions used unrestricted `[Button]` attributes. GM mutates global time/state and both components invoke scene listeners synchronously.
- Observable consequence: an Inspector click can execute gameplay callbacks and mutate authoring state outside a game session.
- Fix: use existing `[Button(PlayModeOnly = true)]` metadata on those 20 actions. Public C# signatures and serialized UnityEvent bindings remain intact.
- Validation: static attribute inventory and existing `ButtonAttribute.PlayModeOnly` API inspection. No attribute-only regression test was added; actual Inspector rendering is owned by the editor assembly and needs central verification.

### Cross-cutting review

- Existing public/serialized fields, enum values, former names, method signatures, and singleton configuration remain compatible. New state fields are private and nonserialized; no migration or asmdef edits are required.
- GM diagnostic output remains behind `_debugLog`; keyed-singleton warnings use gated `NeoDiagnostics`. No new runtime logging was introduced.
- Owned sources have no `GetInstanceID` / editor instance-ID calls or Unity-version branches. Version-specific compilation is unverified, rather than assumed from source inspection.
- No ScriptableObject or dedicated NoCode action exists in this assembly. Scene workflows use MonoBehaviour references and UnityEvents over typed APIs. Scene searches remain optional bootstrap discovery or singleton-resolution fallback.

## Documentation

Updated existing canonical English pages under `Assets/Neoxider/Docs/Tools/Managers/`:

- `README.md`: one module entry table, assembly/namespace guidance, C# entry points, and the GM-versus-EM scene binding distinction.
- `Bootstrap.md`: plain C# and scene targets, priority batch semantics, registration during callbacks, cancellation, destroyed targets, exception/retry behavior, loaded-scene discovery and its one-time scope. Corrected the implication that singleton managers automatically implement `IInit`.
- `GM.md`: typed access and scene UnityEvents, exact time-scale ownership, all pause exits, event ordering, low-level State semantics, repeated assignments and Play Mode buttons.
- `EM.md`: C# subscribe/unsubscribe lifecycle, nullable programmatically configured events, event-only helpers, lazy hub resolution and application forwarding.
- `Singleton.md`: aliases and cache-only queries, inactive lazy resolution, cached misses, lifecycle overrides, auto-create hook ordering, persistence and explicit deferred destruction.
- `SingletonById.md`: replaced the placeholder with a concrete subclass example, public lookup contracts, invalid/case-sensitive keys, replacement ownership, fixed registration Id, inactive-object behavior and reset limitations.

Кратко: проверены все шесть файлов; исправлены повторная и пропущенная инициализация, вызовы уничтоженных целей, зависание времени после паузы и ошибки жизненного цикла singleton. Документация описывает C# API и настройку сцены. Unity и тестовые раннеры по контракту не запускались.

## Substantial improvement proposal

### Extract an observable plain-C# initialization queue with explicit failure recovery

**Status: proposed, not implemented.** The fixes above preserve the existing API and synchronous behavior.

**Problem.** Initialization tracking currently lives inside a persistent MonoBehaviour. Startup exceptions leave no public recovery path, callers cannot inspect pending/failed services, and a callback that continually registers fresh services can consume an unbounded synchronous startup. The meaningful queue tests need a Unity component even though the scheduling rules are plain C#.

**Architecture and proposed public API.** Keep `IInit` unchanged and introduce an independently testable `InitializationQueue` in the existing assembly, without new assembly/configuration work:

```csharp
public sealed class InitializationQueue
{
    public void Register(IInit target);
    public bool Unregister(IInit target);
    public InitializationReport RunPending(InitializationRunOptions options);
    public bool RetryFailed(IInit target);
    public IReadOnlyList<InitializationEntry> Entries { get; }
}
```

Entries expose target, priority and `Pending` / `Running` / `Succeeded` / `Failed` status. Reports expose completed count, failed target/error, and remaining work. Default options preserve descending-priority batches and fail-fast exceptions; explicit options allow report-and-stop and an invocation budget with continuation. An optional lifetime predicate lets the Bootstrap wrapper reuse Unity validity checks while a plain C# caller uses ordinary object lifetime. Registering during callbacks uses the same guarded drain, with failure status recorded before the exception leaves the queue.

`Bootstrap` retains its serialized manual list, automatic-discovery toggle, singleton behavior and existing `Register`/`Unregister` methods. It delegates scheduling to the queue and can add a narrow public retry/run API that returns the report. MonoBehaviour collection and scene validity stay in the wrapper; no UnityEvents are required for the core. An optional report event can support scene tooling without hiding the typed result.

**Compatibility/migration.** Existing scenes, field names, `IInit` implementers and call sites stay valid. Existing wrapper calls retain synchronous completion and exception propagation. Budgeted execution and recovery are opt-in. Reuse current registration equality/order semantics initially; changing identity comparison or priority ordering would be a separate documented compatibility decision. No automatic retry of a failed service and no automatic rerun of successful services.

**Acceptance scenarios.** Plain-C# tests must prove parent/child registration exactly once, descending priority within each batch, cancellation before invocation, failure plus explicit retry without repeating successes, deterministic continuation after a budget is exhausted, and a callback-generated service stream returning control when budgeted. Wrapper tests must prove manual/auto scene registration, destroyed Unity targets, explicit scene-object lifetime, and startup recovery through the new public API. Inspector tooling must display the same report/status as C# callers.

**Effort/risk.** Approximately 2–4 developer days plus central Unity regression verification. Medium risk around reentrant cancellation, retained failed-service references, and exception compatibility; mitigate by first extracting the already-covered behavior, then adding opt-in recovery/budget options. No async initialization, dependency graph, service locator, or compulsory editor framework is needed for this proposal.

## Verification and remaining risks

### New regression assets

- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsManagersBootstrapTests.cs`: 6 Edit Mode cases using plain `IInit` targets and a minimal protected initialization seam.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsManagersGameStateTests.cs`: 13 Edit Mode cases (including four parameterized gameplay exits), asserting game state/time and public hub listener observations. Minimal protected seams invoke lifecycle handlers that Unity does not normally call for inactive Edit Mode objects; fixtures release their hub cache and restore global time.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsManagersSingletonTests.cs`: 3 Play Mode coroutine cases using actual activation, persistence and deferred destruction. A unique singleton type isolates the fixture; auto-create configuration and created GameObjects are cleaned up.
- Each new C# asset has a unique GUID `.meta`. No shared tests or test definitions were edited.

### Validation actually performed

The following scoped checks were actually executed successfully. They do not compile C# or execute the authored tests:

- Manifest inventory against nearest asmdef ownership: exactly 6 owned C# files; every file reviewed. No nested sources were silently included.
- Strict UTF-8 decoding, selected known encoding-marker checks, and trailing-whitespace inspection: passed for all 19 scoped files, including the two unchanged source files, six docs, three new tests/metas, and this report.
- C# lexical inspection: balanced delimiters after excluding comments/strings in all 6 owned sources and 3 new regression files; no `var`, obsolete instance-ID calls, or new Unity-version branches.
- Baseline-to-worktree declaration inspection: retained all 47 existing field name/type declarations and 76 public/protected declaration lines; all `FormerlySerializedAs` entries unchanged. This is a static compatibility check, not a compiled ABI guarantee.
- Inspector inventory: exactly 11 EM and 9 GM actions have `PlayModeOnly = true`. The property is present in baseline `ButtonAttribute` and its existing drawer uses it; no foreign-source feature is required.
- Documentation links: all relative Markdown links resolve in all 6 updated canonical pages.
- Regression inventory: 6 Bootstrap + 13 GM + 3 singleton cases = 22 authored cases. All filenames have the assigned prefix. Both existing test assemblies reference Managers and Unity TestRunner; no reference edit was made.
- Metadata: 3 valid distinct GUIDs, each with exactly one occurrence across `Assets`. The first GUID search used an end-of-line pattern that did not match Windows CRLF; rerunning that check with ripgrep `--crlf` passed. No asset content repair was needed.
- SHA-256 comparison: reviewed `SingletonById.cs` and `SingletonRuntimeReset.cs` remain byte-identical to audit-start snapshots.
- `git diff --check -- <the 19 scoped source/doc/test/meta/report paths>`: exit **0**, no diagnostics. New/untracked file whitespace was also inspected directly because ordinary `git diff` does not cover it.
- Scoped `git status`: only the intended 4 modified sources, 6 modified docs, 3 new regression files/metas and this report. Other concurrent workers' files were left alone.

### Remaining risks and coordinator actions

1. Unity compilation, all 22 new regression cases, and the existing Bootstrap/singleton-sweep tests remain **unexecuted**. Run them centrally under the supported Unity versions. New tests use existing Edit/Play assembly references; no missing reference is currently identified.
2. Check actual destruction of a paused GM in Play Mode, loaded-scene bootstrap lifetimes, and the Play Mode restriction in the shared Inspector. The Edit Mode destruction seam proves handler behavior, not Unity callback delivery.
3. `SingletonRuntimeReset` still discovers closed types/methods through reflection and scans all assemblies twice. IL2CPP stripping and unusual reflection environments require a central player build; no preservation guarantee is claimed from inspection.
4. Keyed registries clear at subsystem registration and have no lookup-time repopulation. Objects retained without another `Awake` would remain unregistered. Verify Enter Play Mode combinations with domain/scene reload disabled; no lifecycle redesign or scene change was made under this worker scope.
5. `Time.timeScale` remains a global shared resource. The GM fix restores its own capture; it does not arbitrate concurrent independent pause owners or serialize arbitrarily nested game-state transitions. Synchronous UnityEvent listeners can initiate nested transitions.
6. Initial bootstrap failure remains fail-fast with no automatic public startup recovery; the proposal addresses that gap. Automatic discovery still runs once and scans all loaded scenes. A persistent Bootstrap requires explicit registration/unregistration for later plain C# service lifetimes.
7. No child agents, Unity/MCP, builds, test runners, installs, shared services, git commits or branch operations were used. Cross-boundary work is limited to the coordinator verification items above; no foreign code/doc fix is requested.
