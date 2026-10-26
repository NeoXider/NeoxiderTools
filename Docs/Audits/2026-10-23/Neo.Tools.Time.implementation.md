# Neo.Tools.Time — 11.0.0 implementation receipt

Status: **SOURCE-FINAL for Neo.Tools.Time**. All three assigned bounded corrections, owned public documentation, and focused regression sources are written. Acceptance execution is **UNEXECUTED**, behind the global source-writer barrier. The dependent Bonus override/economic regressions remain the Bonus owner's integration task.

Target: `demo` / `11.0.0`. Original actor/session retained. `implementation-state.json` recorded `globalReviewApproved: true` before these edits. The original audit and plan remain historical evidence and were not rewritten.

## Approved actions completed in this assembly

| Action | Actual source change | Compatibility |
| --- | --- | --- |
| `BONUS-CLOCK-BOUNDARY` | Added protected virtual `TimerObject.GetUtcNow()` and routed all three RealTime start/deadline writes and both RealTime elapsed/remaining reads through it. | Default is still device UTC; Seconds mode and `_t`/`_a`/`_rt` keys remain unchanged. No Time-to-Bonus dependency. |
| `TIME-TYPED-MUTATION-GENERATION` | SetTime, SetProgress, and the two-argument SetDuration advance the existing state generation; AddTime and one-argument SetDuration delegate to those paths. Continuations after reactive, progress, milestone, day and start/stop callbacks abandon superseded work, including UI/completion and pending Reset/Play operations. | Existing public methods, fields and normal non-reentrant event order remain. A nested typed time command wins; setters do not implicitly start playback. |
| `TIME-DEFAULT-CLOCK-RESTORE` | `NeoTrustedTime.SetClockForTests(null, ...)` now delegates to complete `ResetStaticState()` semantics. | Non-null clocks retain their original `markRestored` behavior. Null ignores that supplied flag and enables normal persisted high-water restoration on the next default-clock access. |

## Additive API available to dependent owners

`Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs` now defines:

```csharp
protected virtual DateTime GetUtcNow()
{
    return DateTime.UtcNow;
}
```

All five RealTime save/load UTC calculations call this hook. `Neo.Bonus` can implement `protected override DateTime GetUtcNow()` returning `NeoTrustedTime.UtcNow`. The clock must be available during base initialization and disable-time saving. A clock override changes persisted UTC arithmetic, not frame-delta ownership or automatic claim behavior.

## Exact files changed during this implementation phase

Runtime:

- `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs` — hook, five UTC sites, typed mutation/continuation revisions.
- `Assets/Neoxider/Scripts/Tools/Time/TrustedClock.cs` — null-clock reset path.

Owned public documentation:

- `Assets/Neoxider/Docs/Tools/Time/README.md` — default-clock restoration and the derived UTC hook.
- `Assets/Neoxider/Docs/Tools/Time/TimerObject.md` — programmer override example, unchanged Inspector setup, callback supersession, persistence compatibility and clock-domain migration limits.

Owned regression sources:

- Updated `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeTimerProbe.cs` — typed UTC delegate override and configurable Seconds/RealTime save seam; existing probe methods preserved.
- Added `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeMutationTests.cs` plus `.meta`.
- Added `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeUtcHookTests.cs` plus `.meta`.
- Added `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockRestoreTests.cs` plus `.meta`.
- Added `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimePersistenceScope.cs` plus `.meta`.

This receipt is the only implementation artifact added under `Docs/Audits/2026-10-23`. No shared tracker, asmdef, package metadata, scene, prefab, sample layout, other owner source, legacy fixture, audit or plan was changed in this phase. Earlier coherent audit fixes in Timer/GameTimeController and other owned sources remain intact. No optional TimerState/snapshot framework or animation subsystem rewrite was introduced.

## Authored acceptance — UNEXECUTED

`Audit20261023NeoToolsTimeMutationTests`: eleven ordinary cases plus two conditional TMP cases.

- One-shot terminal progress handlers call SetTime(5), SetProgress(0.5), AddTime(-5), or SetDuration(20, false). Assert settled seconds/duration, final 50%, Image fill 0.5, zero superseded completion, and no implicit restart.
- Setters from started, reset-reactive and stopped callbacks preserve their values without duplicate old notifications or a later stale reset. A setter during StartTimer's reset prevents its superseded Play.
- Day-time, midnight and wrapped-day progress callbacks preserve the nested value/UI and suppress the old stop/completion as applicable.
- With `UNITY_TEXTMESHPRO`, infinite reactive reentry and an Image dirty callback leave text rendering the nested value, without the former stale seconds write. The dirty callback test asserts that the engine callback actually ran.

`Audit20261023NeoToolsTimeUtcHookTests`: eleven cases.

- Count-down finite, count-up finite and infinite save anchors use a fixed overridden UTC source; advancing only that source changes restored remaining/elapsed seconds correctly.
- Paused finite/infinite timers remain frozen after a large UTC advance.
- The unoverridden base hook saves a deadline within the surrounding device-UTC read window.
- Seconds mode restores actual stored seconds and active state even when a UTC delegate would throw, and creates no RealTime anchor.

`Audit20261023NeoToolsTimeClockRestoreTests`: four cases.

- Both null-clock flag values recreate the default clock and restore the isolated persisted high-water value, rather than keeping the supplied clock/mark.
- Both non-null `markRestored` values retain their original restore-versus-skip behavior and the exact supplied clock instance.

The new persistence fixtures use an in-memory ISaveProvider, unique timer keys, and no writes/deletes to the previous provider's storage. The scope restores the prior provider instance and the exact three pre-test NeoTrustedTime cache values in finally/Dispose. Reflection is confined to preserving/restoring those globals for fixture isolation; assertions observe public clock/state/storage behavior. Shared-state execution requires the coordinator's exclusive isolated runner; no fixture annotation enforces serialization. Saving is disabled before test component destruction. New metadata was authored with four UUID GUIDs; integrated GUID/import validation has not run.

NI8-01/P1 follow-up: removed the unsupported NUnit annotation from `Audit20261023NeoToolsTimeUtcHookTests.cs` and `Audit20261023NeoToolsTimeClockRestoreTests.cs`. All 15 cases and the existing teardown/provider/cache restoration remain unchanged. This follow-up changes only those two fixture declarations and this receipt; runtime and the pending Bonus consumer dependency remain untouched. Source-final; no Unity, compiler, tests, or product checkers were executed.

Total new authored cases: **28 with TMP / 26 without TMP**. The earlier 27 audit cases remain authored historical regression sources; none is reported as executed here.

## Deliberate event and persistence behavior

A typed command issued by a listener invalidates remaining work from the old time state. In-flight listeners of the same UnityEvent continue according to Unity's dispatch rules; the timer suppresses later old channels, UI work and completion after that invocation returns. Direct public-field writes are not automatically versioned. No transactional UnityEvent framework was added.

No serialized-field/key migration or asset rewrite is needed. Existing stored timestamps are read as stored; selecting a different clock for a derived timer does not retroactively convert records created in another clock domain. Base TimerObject remains device-UTC by default. Consumers that used stale completion or reset-after-callback behavior must follow the corrected latest-command semantics documented in TimerObject.md.

## Cross-owner dependency and root handoff

Time's additive API is ready and does not depend on another owner's pending source API. The most recent read of `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs` still showed trusted eligibility reads but no `GetUtcNow()` override. The **Neo.Bonus owner** must add the protected override above and finish the ahead-clock sequential-claim/deadline/restore regression sources. Do not mark the global `BONUS-CLOCK-BOUNDARY` integration verified based on this Time-only receipt.

Root/primary test owners should consume the null-clock restoration semantics when finalizing deterministic Bonus fixtures. Existing shared assembly references were not changed; the historical plan's missing-UniTask argument for Play fixture placement was superseded by the approved native review. This work adds focused Edit regression sources without depending on that obsolete rationale.

## Actual evidence and remaining execution debt

Actual operations during implementation were small cmd.exe/Python source reads, reference enumeration, manual source/diff review, scoped file edits and metadata bookkeeping. Global approval and the manifest assignment were read; historical artifacts were retained. No previous audit check result has been promoted into a pass on this new source.

No Unity, compiler, test runner, product checker, installer, service, commit, push, or child agent has been launched during implementation. All acceptance execution remains behind the global source-writer barrier.

After every writer finishes, root must compile and discover/run the new and existing Time cases, the dependent Bonus offset/claim lifecycle cases, and scheduler cancellation/empty-loop scenarios on the frozen source. Installed Unity 6.3/6.6, optional DG_TWEENING/TMP configurations, nonempty Edit/Play discovery, console output, UI lifecycle, UTF-8/meta/GUID/package checks and uninstalled version/vendor branches remain centrally unverified. Animation ownership remains a needs-evidence proposal, not a completed or claimed fix.
