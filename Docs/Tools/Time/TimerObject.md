# TimerObject

**What it is:** A MonoBehaviour timer (`Scripts/Tools/Time/TimerObject.cs`, namespace `Neo.Tools`) for cooldowns, buffs, level timers and day/night cycles, with state persistence (`SaveProvider`), unscaled time, automatic `Image`/TMP display, milestones and UnityEvents. It can also be driven entirely from code through `Tick(deltaTime)`.

**How to use:**
1. Add **Neoxider > Tools > TimerObject** and set **Duration**, **Count Up** and **Auto Start**.
2. Optionally assign a filled `Image` (**Progress Image**) and a TMP label (**Time Text**) with a **Time Format** such as `{0:F1}s`.
3. Wire `OnTimerCompleted` and the other events, or call `Play()` / `Pause()` / `Stop()` from buttons or C#.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Duration** | Total duration of the timer (in seconds). |
| **Count Up** | Count time from 0 to `Duration` (True) or from `Duration` to 0 (False). |
| **Use Unscaled Time** | Ignore game pauses (`Time.timeScale`). |
| **Looping** / **Infinite** | Restart the timer automatically when completed, or count infinitely. |
| **Carry Loop Overshoot** | Looping only, off by default. Starts the next cycle with the time the completing tick ran past the end, so the long-run period matches elapsed time. Whole cycles skipped by one long tick are dropped. Also `CarryLoopOvershoot` in C#. |
| **Random Duration** | Randomize the duration between a min/max range on every start. |
| **Auto Start** | Automatically begin counting on `OnEnable`. |
| **Progress Image** | Reference to a UI `Image`. The timer will automatically update its `fillAmount` (No-Code). |
| **Time Text** / **Time Format** | Optional `TMP_Text` and a .NET composite format: `{0}` = time, `{1}` = progress (0–1), `{2}` = percent. Assign the label explicitly: it is never auto-found, so a label on the same GameObject that [TimeToText](TimeFormatting.md) or your code drives is left alone. Day cycles use **Day Time Format** with `{0}` hours, `{1}` minutes, `{2}` seconds. The label is updated without per-frame string allocation and only when its text changes. An invalid format (for example `{3}` or `{time}`) shows `{0:F1}s` (day cycles: `{0:00}:{1:00}`) instead of throwing; a gated `NeoDiagnostics` warning reports it once. |
| **Save Progress** | Persist timer state. Can save "seconds" (pauses while game is closed) or "real time" (timer continues running while game is closed). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void StartTimer(float newDuration = -1f, float newUpdateInterval = -1f)` | Restarts the timer; negative arguments retain the configured values. |
| `void Play()` | Starts/restarts at `initialProgress`, resetting milestone state and buffered elapsed time. |
| `void Pause(bool paused = true)` / `Resume()` / `TogglePause()` | Freeze/continue the current value without restarting. |
| `void Stop()` | Stop and reset to the configured initial value, including when paused. |
| `void Reset()` | Reset value, milestones, and buffered elapsed time; retain the active/paused state. |
| `void Tick(float deltaTime)` | Advances the timer deterministically using a caller-provided delta while preserving pause, time-scale, update interval, events, milestones, completion, and looping. |
| `void SetTime(float time)` / `AddTime(float seconds)` | Set/adjust current seconds. Finite timers clamp to duration; infinite timers clamp only at zero; day cycles wrap. |
| `void SetProgress(float progress)` | Set progress without changing duration. |
| `void SetDuration(float newDuration, bool keepProgress)` | Change duration, preserving progress when true or clamping seconds when false. The one-argument overload preserves progress. Infinite timers retain elapsed seconds. |
| `bool IsRunning { get; }` / `IsCompleted` | Active state / whether a finite timer is at its terminal value. |
| `float Progress { get; }` | Returns the current progress from 0.0 to 1.0. |
| `float CurrentTime { get; }` | Returns the current time value in seconds. |
| `ReactivePropertyFloat Time` / `float TimeValue` | Reactive/public current seconds; `Time.AddListener(...)` subscribes from C#. |
| `IFrameClock FrameClock { get; set; }` | Frame time source for `Update` and the `pauseOnTimeScaleZero` check. Defaults to `UnityFrameClock.Instance`; `null` restores it. See [Clocks](Clocks.md). |
| `INeoClock UtcClock { get; set; }` | UTC source of the default `GetUtcNow()` used by RealTime persistence; `null` means device UTC. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnTimerStarted` / `OnTimerStopped` | *(none)* | Fired on start/restart or stop/reset. |
| `OnTimerPaused` / `OnTimerResumed` | *(none)* | Fired on pause/resume transitions. |
| `OnTimerCompleted` | *(none)* | Timer reached the end (fired on every cycle if `Looping = true`). |
| `OnProgressChanged` | `float progress` | Fired every `updateInterval` with the completion percentage (0.0 - 1.0). |
| `OnMilestoneReached` | `float progress` | Fired once per threshold per run. `Play`/`Reset` rearms milestones; resizing the threshold array rearms the new configuration. |

Completion handlers can call `Stop()` or `Play()` even when looping: the automatic loop restart respects that choice. Initialize optional UnityEvents before subscribing when adding components from code, for example `timer.OnTimerCompleted ??= new UnityEvent();`.

By default a looping cycle restarts at its start value; with **Carry Loop Overshoot** it starts with the time by which the previous cycle's last tick overshot its end, so the long-run period matches elapsed time. When one long tick spans several cycles, the skipped whole cycles are dropped: `OnTimerCompleted` fires once, the phase is kept, and the new cycle never starts already complete. Elapsed time is always accumulated with a double-precision residual, so day-length (86400 s) and infinite timers do not drift from float rounding of small frame deltas. The optional start pulse (**Enable Start Animation**) is owned by the timer: restarting or disabling it mid-pulse restores the original scale.

Time commands called from a listener supersede the timer's outstanding notification batch. For example, a terminal progress listener calling `SetTime(5f)` on a ten-second timer publishes the new 50% value and suppresses the superseded 100% notification, UI update, and completion. This applies to `SetTime`, `SetProgress`, `SetDuration`, and `AddTime`, as well as restart/reset/stop continuations and day/infinite UI paths. The current UnityEvent invocation finishes its normal listener dispatch; subsequent work from the old timer state is abandoned when it returns. Setters do not start playback. Use the typed commands inside callbacks: direct writes to public serialized fields do not participate in this revision contract.

## Examples

### No-Code Example (Inspector)
Drag a UI `Image` (with Image Type set to `Filled`) into the `Progress Image` field. Set `Count Up = false` (countdown). With `Fill Image Normal = false`, the bar starts full and depletes. Connect pause/resume buttons to `Pause`/`Resume` or use `TogglePause` for one button. `Play` always restarts.

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

Assign `timer.FrameClock = new ManualFrameClock(0.1f);` to keep the built-in `Update` but make every frame's delta deterministic. `Tick` is the supported entry point when Unity's frame clock is not the owner of time—for example in deterministic tests, replay playback, or a server simulation. Pass elapsed seconds in the timer's selected clock domain. `timeScale`, `updateInterval`, active state, and pause policy remain owned by `TimerObject`. Override `Update()` in a derived timer to suppress automatic ticking when supplying an external clock; calling both advances time twice. Non-positive, NaN, and infinite deltas are ignored. Other numeric configuration should be finite.

```csharp
timer.Play();
timer.Tick(0.25f);
Debug.Assert(Mathf.Approximately(timer.CurrentTime, expectedTime));
```

## Persistence and day cycles

Enable **Save Progress**, choose a unique **Save Key**, and choose a save mode. State loads in `Init`/`Awake` and saves on disable; calling `Pause()` itself does not immediately persist it. `Seconds` restores the saved value and active state. `RealTime` restores an active timer from UTC start/end timestamps supplied by `GetUtcNow()` (device UTC by default). Paused finite and infinite RealTime timers restore frozen seconds and remain paused, regardless of the offline interval.

Existing keys (`_t`, `_a`, `_rt`) remain compatible. Legacy RealTime saves without frozen seconds fall back to their timestamp; missing `_a` retains the legacy active-state behavior. Offline timestamp arithmetic uses wall-clock seconds and does not apply the runtime `timeScale` multiplier. A plain TimerObject does not automatically opt into `NeoTrustedTime`; derived reward timers must use the same UTC source for eligibility, saving, and restoring. Changing a derived clock domain does not rewrite old timestamps; existing records keep their stored values.

### Override the persistence clock

All RealTime start/deadline writes and elapsed/remaining-time reads call one additive hook, `protected virtual DateTime GetUtcNow()`. Override it to return UTC from a trusted or application-specific source. The source must be available during base initialization and disable-time saving. Seconds-mode persistence does not consult this hook.

```csharp
using System;
using Neo.Tools;

public sealed class TrustedTimer : TimerObject
{
    protected override DateTime GetUtcNow()
    {
        return NeoTrustedTime.UtcNow;
    }
}
```

Scene/NoCode setup stays the same: add the derived timer, enable **Save Progress**, select **RealTime**, assign a unique key, and wire the existing events. The override changes the clock used by persistence; it does not add automatic reward claims or change frame-delta timing.

For a count-up day cycle, set **Day Length Seconds** and **Duration** to the same value (for example 86400), enable **Looping**, and leave **Infinite Duration** off. `SetTime` and `AddTime` wrap positive or negative offsets into the day. `OnNewDay` fires when a tick crosses midnight; multiple days in a single tick produce one notification. `OnDayTimeChanged` and the `DayTime*` properties expose time-of-day values.

## Deriving from TimerObject

`TimerObject` is designed as a reusable base class. Its Unity lifecycle hooks are `protected virtual`: `Awake`, `OnEnable`, `Update`, `OnDisable`, and `OnValidate`.

- Prefer overriding `Init()` for runtime initialization.
- Call the `base` implementation from lifecycle overrides unless the derived type intentionally replaces that responsibility.
- Override `Update()` only when supplying a different clock. Otherwise inherit it and let `TimerObject` call `Tick`.
- Override `GetUtcNow()` when RealTime persistence must share a custom UTC domain with derived runtime rules.
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
- [Clocks](Clocks.md)
- ← [Tools/Time](README.md)
