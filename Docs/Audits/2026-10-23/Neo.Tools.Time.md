# Neo.Tools.Time: audit

Status: source audit and scoped fixes completed; compilation and Unity regression execution remain with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Time/Neo.Tools.Time.asmdef`.
- Scope: all 4 manifest-owned C# files (1,797 baseline lines); nested assemblies excluded. No nested assembly exists beneath this assembly's source root.

## Scope

| Source | Complete review |
| --- | --- |
| `Assets/Neoxider/Scripts/Tools/Time/GameTimeController.cs` (57 baseline lines) | Global time ownership, Awake/destruction reset policy, negative/non-finite speed inputs, Inspector documentation lookup. |
| `Assets/Neoxider/Scripts/Tools/Time/Timer.cs` (431 baseline lines) | `ITimerSubscriber`, every constructor/property/command/event, serialized state, UniTask cycles, linked cancellation lifetime, callbacks that stop/restart, duration changes, looping and zero-duration termination. |
| `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs` (1,067 baseline lines) | `TimerSaveMode`, all Inspector fields and lifecycle hooks, deterministic Tick, finite/infinite/day modes, buffered time, UI/reactive output, milestones, random duration, both persistence modes, optional TMP/DOTween branches, derived-class contracts. |
| `Assets/Neoxider/Scripts/Tools/Time/TrustedClock.cs` (242 baseline lines) | `INeoClock`, `SystemNeoClock`, `TrustedClock`, `NeoTrustedTime`: UTC/monotonic anchoring, high-water restore, rewind/forward policies, DateTime limits, static reset, save throttling and dependency ownership. |

Read completely: repository `AGENTS.md`, `WORKER-CONTRACT.md`, this assembly's exact manifest entry and original report, and the four owned source files. Context included existing `TimerTests`, `AuditFixesTimeTests`, `TrustedClockTests`, test/runtime asmdefs, SaveProvider and reactive APIs, and the `CooldownReward` integration. Relevant NeoxiderTools timer/extension references were inspected before implementation. Existing primitives were retained; no duplicate timer service or runtime helper was introduced.

Only the four source files, five owned public Markdown pages, this report, and four new prefixed test source files plus their metadata were changed. No existing tests, assembly definitions, shared configuration, scenes, prefabs, samples, other worker files, or package metadata were edited.

## Findings and fixes

Line references below identify the inspected `ca198556` baseline. Findings are source-demonstrated; the newly authored C# regressions have **not** been executed in this worker session.

### 1. P1 — Timer cancellation did not own the whole run

- Source: `Timer.cs:265–310`, especially the start notification before cancellation-source creation, use of the mutable source field during the cycle, and incomplete final cleanup.
- Trigger/consequence: `OnTimerStart -> Stop()` ran before a source existed, then execution still allocated a source and entered the countdown. A pre-cancelled zero-duration start emitted start/end events. Cancelling from a paused start could leave `IsPaused` true after termination. Natural completion retained its source until a later operation and could overwrite it on the next start.
- Fix: establish a per-run source/token and initialized state before invoking listeners; reject a pre-cancelled start; check cancellation before accepting completion; dispose the local source in that run's `finally`; clear current running/paused/source state only for the owning generation. Stop clears state before cancelling and leaves disposal to the owning async scope.
- Completion handlers now see a finished non-looping run and can start another run immediately. The previous generation cannot clear its successor's state. A handler's progress change at start is no longer overwritten by cycle initialization.
- Regressions: `PreCancelledStart_DoesNotStartOrComplete`, `StopFromStartHandler_CompletesWithoutCountdownOrCompletionEvent`, `CancellationFromPausedStart_ClearsFlagsWithoutCompletion`, `CompletionHandler_CanStartAnotherRun`.

### 2. P1 — Looping Timer omitted completion events and could freeze on empty cycles

- Source: `Timer.cs:288–295`, `397–428`.
- Trigger/consequence: the outer loop invoked `OnTimerEnd` only when `IsLooping` was false. With looping enabled and duration zero, the cycle completed synchronously and the surrounding do/while never yielded, blocking the caller/player loop.
- Fix: notify on every completed cycle, honor stop/restart from the callback, and yield between empty cycles. Non-empty cycles retain their existing yield instead of gaining an extra frame per repeat. Reset the interval accumulator before update callbacks so an older callback cannot clear a newly started run's buffer afterward.
- Regression: `LoopCompletion_NotifiesAndAllowsHandlerToStop` exercises a cycle completed by the start callback and stopping from the loop-end notification. An uninterrupted zero-duration loop still needs central scheduler/Play Mode verification; no runtime pass is claimed for that path.

### 3. P2 — Duration changes exposed invalid remaining time and progress

- Source: `Timer.cs:156–173`, `381–395`.
- Trigger/consequence: an idle timer with 8 seconds remaining could be shortened to 2 seconds while still reporting 8 seconds and progress -3. Shortening a running timer below elapsed time produced negative remaining time and progress above one.
- Fix: clamp remaining time to the new duration after a duration change. Preserve AddTime's single-delta semantics by calculating the requested remaining value before changing duration and clamping once afterward.
- Regressions: `DurationShrink_ClampsRemainingAndProgress`, `DurationShrinkFromStartHandler_DoesNotExposeNegativeRemaining`, `AddTimeAfterPartialProgress_SubtractsOnceWhenDurationShrinks` (compatibility coverage for the focused clamp change).

### 4. P2 — Scene pause toggle and stopping a paused timer were ineffective

- Source: `TimerObject.cs:880–919`.
- Trigger/consequence: `TogglePause` passed `!isActive` to a method whose early-return condition already matched that value, making both toggle directions no-ops. `Stop` returned immediately for a paused timer, preserving partial time instead of resetting it.
- Fix: toggle with `Pause(isActive)`; allow Stop to reset an inactive timer whose value or buffered time differs from the initial state. Preserve the no-op for an already stopped/reset timer.
- Regressions: `TogglePause_FreezesThenResumesCurrentTimeAndEvents`, `StopAfterPause_ResetsTimeAndNotifies` for both directions.

### 5. P2 — Buffered elapsed time crossed reset boundaries or overwrote callback work

- Source: `TimerObject.cs:545–560`, `594–598`.
- Trigger/consequence: with updateInterval 1, Tick(0.75), Reset(), Tick(0.25) advanced the freshly reset timer by one second. Conversely, completion -> Play -> Tick(0.25) lost the newly buffered quarter second when the old Tick cleared the accumulator after returning from UpdateTimer.
- Fix: clear buffered time in Reset; capture and clear a due batch before advancing and invoking callbacks.
- Regressions: `Reset_DiscardsElapsedTimeBufferedBeforeReset`, `CompletionHandlerTick_PreservesBufferedTimeForRestartedRun`.

### 6. P2 — Callback-driven restart/stop was overwritten by automatic loop or stale notifications

- Source: `TimerObject.cs:631–665`, `688–717`.
- Trigger/consequence: a looping completion handler calling Stop was followed by unconditional automatic reactivation; calling Play produced another automatic restart/start event. A progress callback calling Play could be followed by the old cycle's percent-100 notification and completion, although current time had reset to zero.
- Fix: track the playback generation for explicit Play/Reset operations. Respect changed ownership before completion and automatic looping. Stop publishing the old progress/milestone sequence when a listener changes the generation. Also check active state before automatic looping so Pause remains effective at completion.
- Regressions: `StopFromLoopCompletion_RemainsStopped`, `RestartFromCompletion_DoesNotReceiveAnExtraAutomaticLoopRestart`, `RestartFromProgressHandler_DoesNotPublishStalePercentOrCompletion`.
- Scope of guarantee: direct public-field writes and every possible reentrant mutation are not transactions; this fix specifically protects stop/play/reset ownership. A unified state-transition design is proposed below.

### 7. P2 — Milestones depended on scene-time setup and did not rearm on Play

- Source: `TimerObject.cs:303–306`, `700–709`, `812–829`.
- Trigger/consequence: configuring milestones from C# after initialization left their tracking array null, so nothing fired. Direct Play after a previous run retained reached flags. Growing the public threshold array could index beyond the tracking array, including when changed from a milestone callback.
- Fix: initialize/resize tracking when notifications are produced, rearm on Play, and iterate snapshots of the current threshold/tracking arrays so a callback can replace the configuration safely. A size change rearms the new configuration on its next notification.
- Regressions: `MilestonesConfiguredFromCode_FireAgainAfterPlay`, `MilestoneArrayGrowthFromCallback_RearmsTheNewConfiguration`.

### 8. P2 — Infinite and day modes were not respected by time mutation APIs

- Source: `TimerObject.cs:935–948`, `979–983`, `1001–1019`.
- Trigger/consequence: SetTime/AddTime on an infinite stopwatch clamped elapsed time to the unrelated finite duration. SetDuration also truncated its elapsed seconds. AddTime on a day cycle clamped instead of wrapping like SetTime.
- Fix: infinite setters clamp only at zero and retain elapsed seconds on duration changes; AddTime follows SetTime's existing mode-aware normalization, including positive/negative day wrapping.
- Regressions: `InfiniteTimer_MutationsPreserveUnboundedElapsedSeconds`, `DayCycle_AddTimeWrapsBothDirectionsLikeSetTime`.

### 9. P2 — Paused infinite RealTime saves accumulated offline time

- Source: `TimerObject.cs:409–423`, `449–468`; the frozen-value restore branch at `502–509` applied only after the infinite branch had returned.
- Trigger/consequence: save a paused infinite timer at 25 seconds, reopen after an hour, and it remains paused but reports approximately 3,625 seconds. The finite paused-save fix did not cover this mode.
- Fix: store frozen seconds for inactive RealTime timers before branching by finite/infinite mode and restore them before timestamp arithmetic. Preserve existing `_t`, `_a`, `_rt` keys and timestamp fallback for legacy records without frozen seconds. Restored reactive time matches current time without emitting progress events prematurely during load.
- Regressions: `PausedInfiniteRealTimeSave_RestoresFrozenSecondsInsteadOfOfflineElapsedTime`, `LegacyActiveInfiniteRealTimeSave_StillUsesTimestampWithoutFrozenSeconds`.

### 10. P2 — TrustedClock overflowed at valid DateTime boundaries

- Source: `TrustedClock.cs:104–105`.
- Trigger/consequence: anchor at UTC MaxValue minus ten seconds; the next read adds its elapsed time plus 90 seconds of tolerance and throws `ArgumentOutOfRangeException`. A very large finite custom monotonic reading also overflowed the ceiling calculation.
- Fix: saturate the monotonic ceiling at UTC DateTime.MaxValue when the offset reaches the remaining representable span. Preserve normal raw-time/high-water behavior below the ceiling.
- Regressions: `ClockNearMaximumDate_StillAdvancesWithoutOverflow`, `LargeMonotonicAdvance_SaturatesCeilingAndPreservesRawUtc`.
- Corrected the SystemNeoClock summary: its stopwatch is instance-owned, not process-wide.

### 11. P1 conditional / P2 input boundary — Optional animation imports and global speed

- Source: `TimerObject.cs:833–840`: the DG_TWEENING branch calls DOTween extension methods without importing their namespace. No owned source supplies a global using. Added a guarded `using DG.Tweening;`. This is a conditional compile fix inferred from extension-method resolution; the branch has not been compiled here.
- Source: `GameTimeController.cs:52–55`: NaN/infinite SetTimeScale input was passed through to the Unity global speed setter. Reject non-finite inputs before touching engine state, while retaining negative-to-zero clamping. `NonFiniteTimeScale_PreservesTheLastValidGameSpeed` has NaN and both infinity cases. Actual engine/log behavior awaits central execution.
- Corrected `GameTimeController`'s NeoDoc path from the nonexistent `Tools/GameTimeController.md` to the owned canonical page. The exact path was checked on disk; no reflection-only test was added for metadata.

## Documentation

Updated all five existing pages under `Assets/Neoxider/Docs/Tools/Time`:

- `README.md`: repaired the entry table, distinguished async Timer from scene TimerObject, linked game-speed controls, documented TrustedClock/INeoClock/NeoTrustedTime, save throttling, date limits, and offline security limits.
- `Timer.md`: replaced the nonexistent Action constructor and manual Tick signature with the actual UniTask API, event/cancellation lifecycle, duration changes, looping semantics, explicit-type example, ownership and finite-input requirements.
- `TimerObject.md`: corrected Play versus Resume, Stop and Reset behavior, full StartTimer signature, events and reactive API, countdown image direction, external-clock ownership, mode-aware mutation, frozen RealTime saves, legacy keys, and day-cycle configuration.
- `GameTimeController.md`: corrected Awake versus re-enable behavior, finite-speed rules, and shared global ownership implications.
- `TimeFormatting.md`: identifies CooldownReward as the current reward component and TimeReward as legacy.

Canonical public documentation remains English; no redundant guide or new public docs root was created. Русское резюме: устранены ошибки паузы, сохранения бесконечного таймера, повторных событий и отмены; запуск Unity и тестов остаётся за координатором.

## Substantial improvement proposal

### Deterministic time state with separate drivers, snapshots, and view ownership — proposed, not implemented

**Problem.** Timer and TimerObject maintain separate state machines. Timer owns UniTask scheduling and mutable cancellation state; TimerObject independently combines arithmetic, buffer scheduling, day wrapping, callbacks, UI animation, and wall-clock persistence. Several fixes in this audit address the resulting differences in transition ownership. Large Tick values lose loop overshoot, day notifications aggregate multiple boundaries, runtime scaling and RealTime saves use different time domains, and Time.timeScale remains an implicit dependency even for supplied deltas. Serialized running flags cannot reconstruct an async run across a reload. Animation coroutines/tweens also lack an explicit operation owner.

**Architecture/public API.** Introduce a small plain C# state model within this module (a separate assembly would be a coordinator decision). Suggested contracts:

```csharp
public sealed class TimerState
{
    public TimerState(TimerOptions options);
    public TimerStepResult Advance(double deltaSeconds);
    public void Play();
    public void Pause();
    public void Resume();
    public void Stop();
    public TimerSnapshot Capture();
    public void Restore(TimerSnapshot snapshot);
}
```

`TimerOptions` defines finite/infinite/day behavior and the chosen clock domain. `TimerStepResult` captures previous/current seconds, the number of crossed cycles, and threshold crossings without invoking scene code mid-transition. `TimerSnapshot` is a versioned data-only value carrying mode, active/paused state, seconds and optional UTC anchor. All numeric entry points validate finite values; duration and day length have explicit zero policies. This is a proposed signature sketch, not compilable code added to the package.

Keep Timer as the UniTask driver and TimerObject as the serialized scene/UI adapter over that state. Only one driver advances a state instance. Use the existing INeoClock seam for injectable wall/monotonic persistence, with an explicit raw-versus-trusted policy; avoid introducing a parallel global clock. Publish a completed transition's notifications from a snapshot and detect an explicit restart before continuing its old notification batch. The scene adapter owns and cancels one animation operation on restart/disable/destruction. Optional NoCode wiring continues to call the same typed commands; reusable configuration assets contain data, without scene references.

**Compatibility/migration.** Retain all current serialized field names, constructors, methods, UnityEvents, ReactivePropertyFloat output and sample authoring paths. Adapt old `_t`, `_a`, `_rt` records into snapshots without deleting them. Introduce versioned records only after a successful legacy read and keep legacy reads supported. Default to current one-notification-per-update behavior; make exact multi-cycle notifications and trusted UTC opt-in to avoid multiplying rewards in existing games. Document any event-order adjustment with before/after migration examples. Roll out Timer and TimerObject adapters separately behind the same contract tests; do not change other assemblies in the initial core change.

**Acceptance scenarios.** Equivalent elapsed time split across frames produces the same state and cycle count, including multiple day boundaries and loop overshoot; zero-duration loops stay bounded; pause/stop/reset and cancellation leave one owner; progress/end listeners can restart without stale updates; inactive finite/infinite snapshots survive long offline intervals; legacy saves and raw UTC defaults retain behavior; clock rewind/forward/DateTime limits are deterministic under INeoClock; scaled/unscaled/custom drivers never advance twice; disable/destruction cancels visual work and restores owned scale; Edit/Play suites compile on Unity 6.0–6.4 and 6.5+, with and without optional symbols.

**Effort/risk.** Approximately 5–8 engineering days: state and snapshot contracts 2 days, adapters and migration 2–3 days, compatibility tests/docs and version/optional-dependency verification 1–3 days. Medium/high compatibility risk lies in event order, catch-up rewards, serialized running state, and save-time semantics. A small state core and opt-in catch-up policy reduce the migration surface; this proposal does not authorize changes outside the worker's ownership.

## Verification and remaining risks

### Checks actually run

- Scoped `git diff --check --` for the four owned sources, Time documentation directory, this report and `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTime*`: exit 0. Untracked test source/metadata whitespace is also inspected directly because git diff does not include untracked contents.
- Read-only Python static consistency checks: inventory exactly matches all four manifest source files; 8 affected C# files have balanced delimiters/preprocessor conditionals, explicit types, and no obsolete instance-ID calls. These are lexical checks, not a C# compiler.
- Compared all public declaration lines and `[SerializeField]` declarations in the four sources against `ca198556`: preserved. Existing public serialized fields also retain their public declarations.
- UTF-8 decoding and replacement-character checks of affected source, docs, tests and metadata; relative links across all five public documentation pages and owned NeoDoc attributes resolve.
- All four new MonoImporter metadata files have valid unique GUIDs. Scoped GUID searches under Assets/Neoxider returned exactly one match for each new GUID.
- Existing Edit test assembly references Neo.Tools.Time, Neo.Save, Neo.Reactive and UniTask; no shared asmdef change is required. HEAD remained `ca198556` at validation.

### Regressions authored, not executed

New files under `Assets/Neoxider/Tests/Edit/Tools`, each with its own `.meta`:

- `Audit20261023NeoToolsTimeTimerTests.cs`: synchronous public Timer lifecycle, cancellation, looping notification, restart and range regressions.
- `Audit20261023NeoToolsTimeTimerObjectTests.cs`: deterministic Tick/state/events, milestones, infinite/day mutation, and real save/load behavior through SaveProvider. Uses unique test save keys and deletes only those keys; disables saving before teardown destruction.
- `Audit20261023NeoToolsTimeTimerProbe.cs`: a typed derived adapter exposing the existing protected initialization/persistence seam for tests; no reflection-based private method calls.
- `Audit20261023NeoToolsTimeClockAndScaleTests.cs`: date saturation and invalid global-speed inputs; global speed is restored during teardown.

There are 27 authored NUnit cases including parameterized cases. No existing test was modified. No Unity/MCP, compiler, build/test runner, install, service, commit, or child agent was started.

### Remaining risks / coordinator actions

1. Centrally compile and execute `Audit20261023NeoToolsTime*`, existing TimerTests/AuditFixesTimeTests/TrustedClockTests, and Bonus cooldown integration tests. Compilation, Edit Mode, Play Mode and actual Unity logs remain **unverified** here by the explicit worker contract.
2. Verify optional `DG_TWEENING` and `UNITY_TEXTMESHPRO` branches, and Unity 6.0–6.4 plus 6.5+ compilation. Neither version branch nor optional dependency branch is declared passed from inspection alone.
3. Exercise an uninterrupted zero-duration loop and external cancellation across frames centrally; the source now yields and owns disposal, but these scheduler paths were not run here.
4. Looping Timer listeners now receive the previously omitted end notifications, and paused Stop emits stop/reset behavior. These are intentional behavior corrections; consumers that worked around the defects should remove duplicate manual completion/reset calls.
5. Existing design limits remain explicit: finite numeric configuration outside Tick/SetTimeScale is a caller precondition; date/monotonic sources must satisfy INeoClock; multi-controller global speed is last-writer-owned; scene animations and arbitrary field mutation lack transactional ownership; RealTime persistence uses raw UTC with a 1:1 wall-time rate and saves on disable rather than every pause. The proposed state/driver/snapshot design addresses these without a broad rewrite in this audit.
6. Cross-boundary follow-up: the Bonus owner/coordinator should verify CooldownReward against the corrected base completion/stop semantics. Its claim path uses NeoTrustedTime while inherited TimerObject persistence uses raw UTC; an authoritative policy/migration requires a coordinated change. No Bonus code or docs were edited. No assembly reference or shared configuration change was requested.
