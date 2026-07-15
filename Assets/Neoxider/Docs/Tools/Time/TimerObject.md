# TimerObject

**Purpose:** A highly versatile timer for cooldowns, buffs, level timers, or day/night cycles. Supports state persistence (`SaveProvider`), unscaled time, automatic UI updates, and firing events at various progression stages.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Duration** | Total duration of the timer (in seconds). |
| **Count Up** | Count time from 0 to `Duration` (True) or from `Duration` to 0 (False). |
| **Use Unscaled Time** | Ignore game pauses (`Time.timeScale`). |
| **Looping** / **Infinite** | Restart the timer automatically when completed, or count infinitely. |
| **Random Duration** | Randomize the duration between a min/max range on every start. |
| **Auto Start** | Automatically begin counting on `OnEnable`. |
| **Progress Image** | Reference to a UI `Image`. The timer will automatically update its `fillAmount` (No-Code). |
| **Save Progress** | Persist timer state. Can save "seconds" (pauses while game is closed) or "real time" (timer continues running while game is closed). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void StartTimer(float newDuration = -1f)` | Restarts the timer. You can optionally pass a new duration. |
| `void Play()` | Starts or resumes the timer from its current value. |
| `void Reset()` | Resets the time to the initial state but does not start it. |
| `void Tick(float deltaTime)` | Advances the timer deterministically using a caller-provided delta while preserving pause, time-scale, update interval, events, milestones, completion, and looping. |
| `float Progress { get; }` | Returns the current progress from 0.0 to 1.0. |
| `float CurrentTime { get; }` | Returns the current time value in seconds. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnTimerStarted` / `OnTimerStopped` | *(none)* | Fired when the timer starts or is stopped/paused. |
| `OnTimerCompleted` | *(none)* | Timer reached the end (fired on every cycle if `Looping = true`). |
| `OnProgressChanged` | `float progress` | Fired every `updateInterval` with the completion percentage (0.0 - 1.0). |
| `OnMilestoneReached` | `float progress` | Fired when specific milestones (defined in `milestonePercentages`) are reached. |

## Examples

### No-Code Example (Inspector)
Drag a UI `Image` (with Image Type set to `Filled`) into the `Progress Image` field. Set `Count Up = false` (countdown). When the game starts, the visual bar/circle will deplete automatically without writing any code.

### Code Example
```csharp
[SerializeField] private TimerObject _abilityCooldown;

public void UseAbility()
{
    if (!_abilityCooldown.IsRunning)
    {
        // Cast ability
        Fireball();
        
        // Start cooldown timer
        _abilityCooldown.StartTimer();
    }
}
```

### Custom clock and deterministic tests

`Tick` is the supported entry point when Unity's frame clock is not the owner of time—for example in deterministic tests, replay playback, or a server simulation. Pass elapsed seconds in the timer's selected clock domain. `timeScale`, `updateInterval`, active state, and pause policy remain owned by `TimerObject`.

```csharp
timer.Play();
timer.Tick(0.25f);
Debug.Assert(Mathf.Approximately(timer.CurrentTime, expectedTime));
```

## Deriving from TimerObject

`TimerObject` is designed as a reusable base class. Its Unity lifecycle hooks are `protected virtual`: `Awake`, `OnEnable`, `Update`, `OnDisable`, and `OnValidate`.

- Prefer overriding `Init()` for runtime initialization.
- Call the `base` implementation from lifecycle overrides unless the derived type intentionally replaces that responsibility.
- Override `Update()` only when supplying a different clock. Otherwise inherit it and let `TimerObject` call `Tick`.
- Override `SaveState()` and `GetSaveKey()` for custom persistence instead of duplicating `OnDisable`.

```csharp
public sealed class MatchTimer : TimerObject
{
    protected override void OnValidate()
    {
        base.OnValidate();
        duration = Mathf.Max(1f, duration);
    }
}
```

## See Also
- [GameTimeController](GameTimeController.md)
- ← [Tools/Time](../README.md)
