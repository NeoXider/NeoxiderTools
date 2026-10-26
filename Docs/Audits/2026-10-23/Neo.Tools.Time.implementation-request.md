# Neo.Tools.Time: approved implementation request

Original actor: `neo-audit-1023-b08-05`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### BONUS-CLOCK-BOUNDARY (P1; approved-source-correction)

Bonus plan: TimerObject saves device UTC but CooldownReward checks trusted UTC; clock offset permits repeated sequential claims.

Root decision: Time routes all5 realtime read/write sites through protected virtual GetUtcNow default device UTC; Bonus override trusted UTC, preserve keys and unrelated Timer defaults.

### TIME-TYPED-MUTATION-GENERATION (P2; approved-source-correction)

SetTime/SetProgress/SetDuration do not bump version; nested setter50% followed by outer100% and completion while current5/10. Invalidate typed state changes and guard UI/completion continuations.

### TIME-DEFAULT-CLOCK-RESTORE (P3; approved-source-correction)

SetClockForTests(null) leaves restored flag true, skips persisted high-water when standard clock recreated. Null delegates complete ResetStaticState semantics while preserving nonnull markRestored.

## Relevant full native sections

Source: NativeReview-B.md

### B-11 / P2 — TimerObject typed time mutations do not invalidate an old notification batch

Owner: Neo.Tools.Time / `neo-audit-1023-b08-05` (plan F1 confirmed). `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs:693–764,989–1081`, InvokeEvents and SetTime/SetProgress/SetDuration. Trigger: count-up duration 10, updateInterval 0; a one-shot progress-1 listener calls SetTime(5); Tick(10). Expected: final time 5, last percent 50, no completion for the superseded end state. Actual: setter publishes 50 but does not advance playbackGeneration; outer callback continues with percent 100 and completion. Smallest correction: include typed state mutations in the revision contract and stop stale notifications/UI/completion across finite/infinite/day branches, preserving the fact that SetTime does not start playback. Existing restart regressions do not cover this setter trigger.
Source: NativeReview-B.md

### B-12 / P3 — restoring the default trusted clock bypasses persisted high-water restoration

Owner: Neo.Tools.Time / `neo-audit-1023-b08-05` (plan F2 confirmed). `Assets/Neoxider/Scripts/Tools/Time/TrustedClock.cs:199–204`, NeoTrustedTime.SetClockForTests(null) defaults markRestored=true, then Clock creates a default instance while EnsureRestored returns early. Expected per XML: restored default behavior, including reading its persisted high-water key. Actual: newly created default clock omits that saved mark. Smallest correction: null path resets static restoration state; retain markRestored semantics for supplied clocks. Regression must use an isolated provider/key and restore previous static clock/provider state.
Source: NativeReview-B.md

### Neo.Tools.Time — full artifacts, all source diffs, four complete regression files reviewed

Owner/task `neo-audit-1023-b08-05`. Per-run cancellation sources/generation, pre-start cancellation, empty-cycle yield, duration clamps, buffered-delta ownership, scene pause/stop, milestone snapshots, paused infinite persistence and date saturation are supported by reviewed code. Timer tests intentionally finish synchronously through callbacks and check state/events; they do not prove an uninterrupted loop yields under the actual player scheduler. TimerObject tests use actual Tick/state/events and protected typed probe, not copies of arithmetic. Persistence tests use unique GUID key families on the current SaveProvider, delete only owned `_t/_a/_rt` keys and disable saving before object teardown; this is scoped real-provider mutation, not an isolated in-memory provider. Clock tests inject plain clocks; speed tests restore global time scale in finally.

B-11/B-12 remain minimal corrections; large TimerState/snapshot/clock-driver extraction is optional and must preserve default one-notification-per-update/raw-UTC semantics. Current Play assembly now already references UniTask, so the plan's missing-UniTask rationale for fixture placement is stale. Optional DG_TWEENING/TMP branches and actual Time/Bonus inheritance/event ordering remain compile/runtime debt.

Aggregate work: timers own distinct elapsed/generation/UI state, so equal duration does not make their results shareable. Tick buffers throttle dispatch; shared trusted clock uses a persistence threshold. No per-object physics/scene-search loop was introduced. Animation ownership on repeated Play/disable remains a needs-evidence path; do not use KillAll or introduce another global clock to fix a local owner.
Source: NativeReview-C.md

### C-08 / P1 — CooldownReward reads trusted time but its inherited timer persists device time

Current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs:229–243` (`GetClaimableCount`), take/save at 277; `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs:397–529` (`SaveState`/`LoadState`, device UTC sites 423, 443, 448, 483, 529). Reproduction: trusted UTC is ten minutes ahead of device UTC, cooldown 60 seconds, accumulation enabled/cap one; a take writes device-now+60 through inherited SaveState, then a sequential second take compares this deadline with trusted now and is already eligible. The recursion guard has completed and cannot prevent repeated economic grants. Restore/start countdowns also cross clock domains. Expected: consume and restore use the same clock as eligibility.

Smallest repair: Time owner `neo-audit-1023-b08-05` adds a protected virtual UTC-now hook defaulting to device UTC and routes all five real-time save/load calculations through it; Bonus owner `neo-audit-1023-b02-04` overrides with NeoTrustedTime. Preserve plain TimerObject defaults and existing keys. Add ahead-clock sequential-claim/deadline/restore regressions plus the unchanged base-timer clock contract. The broad reward schedule/profile framework remains optional; it is not a prerequisite for this fix.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Time.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Final NI7/NI8 integration handoff

ROOT personally FULL-read NI8 and accepts NI8-01/P1. Remove unsupported NonParallelizable from new UtcHook and ClockRestore fixtures and correct serial-execution receipt claim. Preserve all cases and exact prior provider/cache cleanup; shared exclusive isolated runner replaces unsupported annotations. Runtime hook/typed/default-clock scope sourceapproved. Bonus pending override is SAME Bonus consumer dependency, not your edit. No execution.
