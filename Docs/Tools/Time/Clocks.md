# Clocks

**What it is:** The canonical time seams of the package, all in `Scripts/Tools/Time/` (namespace `Neo.Tools`, assembly `Neo.Tools.Time`): `IFrameClock` for per-frame pacing (`FrameClock.cs`) and `INeoClock` for wall-clock UTC plus a monotonic counter (`TrustedClock.cs`), with `TrustedClock` / `NeoTrustedTime` as the tamper-resistant implementation.

**How to use:**
1. Frame-paced logic (timers, cooldown bars, animations): accept an `IFrameClock`, default to `UnityFrameClock.Instance`, inject `ManualFrameClock` in tests. A module that does not reference `Neo.Tools.Time` can instead expose `Tick(float deltaTime)` and keep its `Update` as a one-line adapter that passes Unity's delta (as `DialogueLinePlayer` does).
2. Wall-clock logic (offline rewards, daily resets, RealTime saves): accept an `INeoClock`, default to `NeoTrustedTime.AsClock` (guarded) or `new SystemNeoClock()` (raw device time), inject a fake in tests.
3. Never read `Time.deltaTime` or `DateTime.UtcNow` directly inside a rule that needs tests; read the injected clock or take the delta as an argument. Reading Unity time is fine only in the thin `Update` adapter that forwards it.

---

## Which seam

| Need | Seam | Production default | Test double |
|------|------|--------------------|-------------|
| Seconds since last frame, game speed | `IFrameClock` (`DeltaTime`, `UnscaledDeltaTime`, `TimeScale`) | `UnityFrameClock.Instance` | `ManualFrameClock` |
| Current UTC and elapsed real time | `INeoClock` (`UtcNow`, `MonotonicSeconds`) | `NeoTrustedTime.AsClock` or `SystemNeoClock` | Any small class implementing `INeoClock` |
| Tamper-resistant UTC with a persisted high-water mark | `TrustedClock` over an `INeoClock` | `NeoTrustedTime` (shared, persisted through `SaveProvider`) | `new TrustedClock(fakeClock)` and `NeoTrustedTime.SetClockForTests` |

`trustedClock.AsNeoClock()` and `NeoTrustedTime.AsClock` expose guarded time through the plain `INeoClock` interface, so a consumer only depends on one seam and the caller decides whether it is guarded.

## Built-in consumers

| Type | Frame clock | UTC clock |
|------|-------------|-----------|
| [Timer](Timer.md) | `Timer.FrameClock` | — |
| [TimerObject](TimerObject.md) | `TimerObject.FrameClock` (used by `Update` and the `pauseOnTimeScaleZero` check); `Tick(delta)` for a fully external driver | `TimerObject.UtcClock` (RealTime persistence), or override `GetUtcNow()` |
| `Bonus` cooldowns | — | Read `NeoTrustedTime`; new plain cores take an `INeoClock` |
| [DialogueLinePlayer](../Dialogue/DialogueLinePool.md) | `Tick(deltaTime)`; `Update` forwards Unity's (unscaled) delta | — |

## Examples

Deterministic frame pacing:

```csharp
ManualFrameClock frames = new ManualFrameClock(unscaledDeltaTime: 0.1f);
Timer cooldown = new Timer(1f, updateInterval: 0f) { FrameClock = frames };
cooldown.Start(); // each player-loop frame now advances exactly 0.1 s
frames.TimeScale = 0.5f;
frames.SetFrame(0.2f); // UnscaledDeltaTime = 0.2, DeltaTime = 0.2 * TimeScale = 0.1
```

`ManualFrameClock.SetFrame(unscaledDeltaTime)` sets the unscaled delta and derives `DeltaTime` from the current
`TimeScale`, the way Unity relates the two; the constructor uses it too. `DeltaTime`, `UnscaledDeltaTime` and
`TimeScale` also have their own setters for a frame where the two deltas must not follow that rule.

A rule with an injectable UTC clock:

```csharp
public sealed class DailyGate
{
    private readonly INeoClock _clock;

    public DailyGate(INeoClock clock = null)
    {
        _clock = clock ?? NeoTrustedTime.AsClock;
    }

    public bool IsOpen(DateTime lastClaimUtc)
    {
        return (_clock.UtcNow - lastClaimUtc).TotalHours >= 24d;
    }
}
```

Scene setup: a `TimerObject` with **Save Progress** and **RealTime** mode uses device UTC. To share guarded time, assign `timer.UtcClock = NeoTrustedTime.AsClock;` before its GameObject is activated (`Awake` restores the save), or add a derived timer that overrides `GetUtcNow()`; the second option also works for timers placed in a scene.

## See also

- [Tools/Time](README.md)
- [Save](../../Save/README.md)
- [Bonus](../../Bonus/README.md)
