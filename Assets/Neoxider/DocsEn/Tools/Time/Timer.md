# Timer

**Purpose:** A lightweight, non-MonoBehaviour timer class. Supports one-shot and repeating modes, pause/resume, and callback on completion. Used internally by various systems.

## API

| Method / Property | Description |
|-------------------|-------------|
| `Timer(float duration, Action onComplete, bool repeat)` | Constructor. |
| `void Start()` | Start the timer. |
| `void Stop()` | Stop the timer. |
| `void Pause()` / `Resume()` | Pause/resume. |
| `void Tick(float deltaTime)` | Advance the timer. |
| `float RemainingTime { get; }` | Time remaining. |
| `float Progress { get; }` | Progress (0–1). |
| `bool IsRunning { get; }` | Whether the timer is running. |

## See Also
- [TimerObject](TimerObject.md)
- ← [Tools/Time](README.md)
