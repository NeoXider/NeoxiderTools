# Tools / Time

**What it is:** Timers, scene adapters, game speed and the package's clock seams (frame pacing and guarded UTC). Scripts live in `Scripts/Tools/Time/`, namespace `Neo.Tools`, assembly `Neo.Tools.Time`. Use this page as the canonical module entry.

**Contents:**

| Page | Description |
|------|-------------|
| [Timer](./Timer.md) | Plain C# timer scheduled by UniTask; cancellation, pause, per-cycle completion events and an injectable frame clock. |
| [TimerObject](./TimerObject.md) | Scene timer with `Tick`, UnityEvents, reactive time, milestones, `Image`/TMP display, and persistence. |
| [Clocks](./Clocks.md) | `IFrameClock` / `INeoClock` seams, `ManualFrameClock`, `TrustedClock`, `NeoTrustedTime` — the one place other modules take time from. |
| [GameTimeController](./GameTimeController.md) | Set global game speed or wire pause/resume buttons in the Inspector. |
| [TimeFormatting](./TimeFormatting.md) | Format helpers, including the allocation-free `TimerTextFormatter`. |

**Navigation:** [← Tools](../README.md)

---

For code-only countdowns, construct `Timer`, subscribe to its events, then call `Start()` or await `StartAsync()`. Dispose it when its owner is destroyed. `Timer` needs Unity's player loop; assign `FrameClock = new ManualFrameClock(...)` for deterministic frame deltas.

For scene workflows, add `TimerObject`, choose duration/direction, and wire its UnityEvents or assign a filled UI `Image` / TMP label. `Play()` restarts; `Resume()` continues a pause. For an external clock, either assign `FrameClock`, or override its protected `Update()` and drive `Tick(deltaTime)` from exactly one owner.

## Guarded UTC time

`TrustedClock` is a plain C# clock over `INeoClock` (`DateTime UtcNow`, `double MonotonicSeconds`). `ReadUtcNow()` never returns an earlier value than its high-water mark. During a session it caps forward wall-clock jumps at elapsed monotonic time plus 90 seconds of tolerance. Date ceilings saturate at UTC `DateTime.MaxValue`.

```csharp
TrustedClock clock = new TrustedClock();
clock.Restore(savedHighWaterUtc);
DateTime now = clock.ReadUtcNow();
double elapsed = clock.SecondsSince(lastClaimUtc);
DateTime nextSavedHighWater = clock.HighWaterUtc;
```

`NeoTrustedTime.UtcNow` and `NeoTrustedTime.SecondsSince(timestamp)` share a clock and stage its high-water mark through `SaveProvider`, using `NeoTrustedTime.HighWaterSaveKey`. Writes are throttled to advances of at least 15 seconds; durable flushing follows the active save provider. `LastReadWasRewound` and `LastReadWasJumpedForward` describe the latest read. Inject an `INeoClock` for deterministic simulations; `ResetSessionAnchor()` starts a new session anchor without clearing the high-water mark.

`NeoTrustedTime.SetClockForTests(null)` clears the cached clock and restoration/persistence state. The next access recreates the default clock and restores its persisted high-water mark. Passing a non-null clock retains the supplied `markRestored` behavior. Tests that replace this global source should isolate their provider and restore prior state.

These are offline safeguards, with 90 seconds of slack and no verification of elapsed time between processes. They do not replace server validation. `TimerObject` RealTime persistence uses device UTC by default through its protected virtual `GetUtcNow()`. Assign `TimerObject.UtcClock = NeoTrustedTime.AsClock`, or override `GetUtcNow()` in a derived timer, to share the same UTC source as eligibility checks; see [TimerObject](TimerObject.md) and [Clocks](Clocks.md). Ordinary scene timers require no additional setup.

`NeoTrustedTime.AsClock` and `TrustedClock.AsNeoClock()` expose guarded time as a plain `INeoClock`, so any rule that accepts an `INeoClock` can run on trusted time in production and on a fake clock in tests.

## See also

- [Save](../../Save/README.md)
- [Bonus](../../Bonus/README.md)
