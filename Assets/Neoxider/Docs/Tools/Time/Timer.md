# Timer

**Purpose:** A serializable, non-MonoBehaviour countdown in `Neo.Tools`, scheduled by UniTask on Unity's player loop. Supports one-shot and repeating modes, pause/resume, and cancellation. Implements `IDisposable`.

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
| `float RemainingTime { get; }` | Time remaining. |
| `float Progress { get; }` | Progress (0–1). |
| `bool IsRunning { get; }` / `IsPaused` | A paused run remains running until stopped, cancelled, or completed. |
| `bool IsLooping { get; set; }` / `UseUnscaledTime` | Repeat cycles or ignore global time scale. |
| `void Dispose()` | Stop the active run and release its cancellation resources. |

## Events and lifecycle

`OnTimerStart` fires once per run. `OnTimerUpdate(remainingSeconds, progress)` fires at the configured interval. `OnTimerEnd` fires for every completed cycle, including looping cycles; cancellation and `Stop()` do not count as completion. `OnTimerPause` and `OnTimerResume` fire on their respective transitions.

Start handlers can call `Stop()` or set progress. End handlers can stop a loop or start another run; cleanup of the completed run does not stop the new one. Loop cycles yield to the player loop even when duration is zero. External cancellation ends the await normally, matching `Stop()`, and clears running/paused flags; use `Reset` or `Stop` when you also need to reset the displayed time.

```csharp
using Neo.Tools;

// In an async method; dispose before the owner goes away.
using Timer timer = new Timer(5f, updateInterval: 0.1f, useUnscaledTime: true);
timer.OnTimerEnd.AddListener(OnCooldownFinished);
await timer.StartAsync(destroyCancellationToken);
```

`Timer` has no manual `Tick(float)` method. Use [TimerObject](TimerObject.md) with a custom update owner when elapsed time must be supplied explicitly. Keep timer operations on Unity's main thread and provide finite numeric configuration.

## See Also
- [TimerObject](TimerObject.md)
- ← [Tools/Time](README.md)
