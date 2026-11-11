# Timer

**What it is:** A serializable, non-MonoBehaviour countdown (`Scripts/Tools/Time/Timer.cs`, namespace `Neo.Tools`) scheduled by UniTask on Unity's player loop. Supports one-shot and looping runs, pause/resume, cancellation and an injectable frame clock. Implements `IDisposable`.

**How to use:**
1. Construct it: `new Timer(duration, updateInterval, looping, useUnscaledTime)`.
2. Subscribe to `OnTimerUpdate` / `OnTimerEnd` (C# `AddListener`).
3. Call `Start()` (fire and forget) or `await StartAsync(token)`.
4. `Dispose()` it when the owner is destroyed.

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `Timer(float duration, float updateInterval = 0.05f, bool looping = false, bool useUnscaledTime = false)` | Constructor; times are seconds. |
| `void Start()` / `Play()` | Start from full duration; a currently running or paused run is left alone. |
| `UniTask StartAsync(CancellationToken cancellationToken = default)` | Await the run, including all cycles when looping. Pre-cancelled calls do not start or emit completion. |
| `void Stop()` | Cancel an active run and reset remaining time to duration. Does not emit completion. |
| `void Pause()` / `Resume()` | Pause/resume. |
| `UniTask Restart(CancellationToken cancellationToken = default)` | Stop, start a new run, and await it. |
| `void Reset(float newDuration, float newUpdateInterval = 0.05f, bool? looping = null, bool? useUnscaledTime = null)` | Stop and configure an idle timer. |
| `void SetRemainingTime(float seconds)` / `SetProgress(float progress)` | Set remaining seconds or progress in the current state; values are clamped. A new run starts at full duration. |
| `void AddTime(float seconds)` | Adjust duration and remaining seconds once; negative values subtract time. |
| `float Duration { get; set; }` | Non-negative duration. While running, a duration change shifts remaining time by the same delta, clamped to the new range. |
| `float UpdateInterval { get; set; }` | Minimum accumulated seconds between update events; zero allows updates every frame. |
| `bool CarryLoopOvershoot { get; set; }` | Looping only, default `false`. When true, each new cycle is shortened by the time the previous cycle's last frame overshot zero (whole skipped cycles are dropped). Also serialized. |
| `IFrameClock FrameClock { get; set; }` | Frame time source read once per frame. Defaults to `UnityFrameClock.Instance`; `null` restores the default. See [Clocks](Clocks.md). |
| `float RemainingTime { get; }` | Time remaining. |
| `float Progress { get; }` | Progress (0–1). |
| `bool IsRunning { get; }` / `IsPaused` | A paused run remains running until stopped, cancelled, or completed. |
| `bool IsLooping { get; set; }` / `UseUnscaledTime` | Repeat cycles or ignore global time scale. |
| `void Dispose()` | Stop the active run and release its cancellation resources. |

## Events and lifecycle

`OnTimerStart` fires once per run. `OnTimerUpdate(remainingSeconds, progress)` fires at the configured interval. `OnTimerEnd` fires for every completed cycle, including looping cycles; cancellation and `Stop()` do not count as completion. `OnTimerPause` and `OnTimerResume` fire on their respective transitions.

Start handlers can call `Stop()` or set progress. End handlers can stop a loop or start another run; cleanup of the completed run does not stop the new one. Loop cycles yield to the player loop even when duration is zero. External cancellation ends the await normally, matching `Stop()`, and clears running/paused flags; use `Reset` or `Stop` when you also need to reset the displayed time.

By default every looping cycle restarts at full duration, so each cycle also lasts the part of its final frame that ran past zero (up to one frame of drift per cycle). Set `CarryLoopOvershoot = true` to subtract that overshoot from the next cycle and keep the long-run period exact. When one long frame spans several cycles, the skipped whole cycles are dropped: `OnTimerEnd` fires once for that frame, the phase is kept, and the next cycle never starts at zero. Non-positive, NaN and infinite frame deltas are ignored.

```csharp
using Neo.Tools;

// In an async method; dispose before the owner goes away.
using Timer timer = new Timer(5f, updateInterval: 0.1f, useUnscaledTime: true);
timer.OnTimerEnd.AddListener(OnCooldownFinished);
await timer.StartAsync(destroyCancellationToken);
```

## Deterministic pacing

`Timer` has no manual `Tick(float)` method; it always advances once per player-loop frame. Assign a `ManualFrameClock` to make each frame's delta deterministic in tests, replays or fixed-step simulations:

```csharp
ManualFrameClock frames = new ManualFrameClock(unscaledDeltaTime: 0.25f);
Timer timer = new Timer(1f, 0f, looping: true) { FrameClock = frames, CarryLoopOvershoot = true };
timer.Start(); // every frame now advances exactly 0.25 s
```

Use [TimerObject](TimerObject.md) with `Tick(deltaTime)` when the caller must own the update loop itself. Keep timer operations on Unity's main thread and provide finite numeric configuration.

## See Also
- [TimerObject](TimerObject.md)
- [Clocks](Clocks.md)
- ← [Tools/Time](README.md)
