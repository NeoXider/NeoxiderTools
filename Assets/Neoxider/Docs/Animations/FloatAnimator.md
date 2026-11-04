# FloatAnimator

**What it is:** a MonoBehaviour that animates one float between `minValue` and `maxValue` with an
[AnimationType](AnimationType.md) and publishes it through the reactive property `Value`.
Script: `Scripts/Animations/FloatAnimator.cs`, namespace `Neo.Animations`.

**How to use:**
1. Add the component, choose `animationType`, the range and `animationSpeed`.
2. Connect **Value → On Changed** to a float receiver in the Inspector, or `Value.Subscribe(...)` from code.
3. Leave **playOnStart** on, or call `Play()` from code or a UnityEvent.
4. For a custom clock set `ManualUpdate = true` and call `Tick(deltaTime)` yourself.

---

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **animationType** | AnimationType | Animation type. |
| **minValue** | float | Minimum value. |
| **maxValue** | float | Maximum value. |
| **animationSpeed** | float | Speed; <= 0 evaluates to minValue. Inspector range 0–30. |
| **noiseScale**, **use2DNoise**, **noiseOffset** | — | Noise settings for PerlinNoise. |
| **customCurve** | AnimationCurve | Curve for CustomCurve type. |
| **playOnStart** | bool | Auto-start on Start. |
| **_randomizeStartTime** (`RandomizeStartTime`) | bool | Default on: `Start` randomizes the clock and noise offset so several animators do not move in sync. Off: deterministic start at time 0, or at the time set by `ResetTime()` before `Start`. |
| **_useUnscaledTime** (`UseUnscaledTime`) | bool | Advance on `Time.unscaledDeltaTime` (UI shown during pause). |
| **Value** | ReactivePropertyFloat | Reactive value; subscribe via Value.OnChanged. |
| **OnAnimationStarted / Stopped / Paused** | UnityEvent | Lifecycle events. |

## API

- **CurrentValue**, **ValueFloat** — current animated value (read-only)
- **IsPlaying**, **IsPaused** — state
- **AnimationTime** — current clock in seconds (read-only)
- **MinValue**, **MaxValue**, **AnimationSpeed**, **AnimationType** — writable
- **Play()**, **Stop()**, **Pause()**, **Resume()**, **ResetTime()**, **RandomizeTime()**
- **Tick(float deltaTime)** — advance the clock and publish the sample; ignored while stopped, paused or disabled.
  Negative, NaN and infinite deltas sample without advancing.
- **ManualUpdate** (`bool`, not serialized) — when true, `Update` does nothing and the owner calls `Tick`.

A warm `Tick` allocates nothing.

```csharp
// Drive the animator from your own loop (deterministic replays, tests, custom time).
FloatAnimator pulse = GetComponent<FloatAnimator>();
pulse.RandomizeStartTime = false;
pulse.ManualUpdate = true;
pulse.Play();
pulse.Tick(myClock.Delta);
```

`Play()`, `Stop()`, `Pause()`, and `Resume()` are also exposed as Play Mode-only Inspector buttons for
runtime preview and lifecycle testing.

## Value updates and bindings

Each playing, unpaused frame writes the evaluated value to `Value.Value`. There is no animator-level
0.001 threshold: small ranges such as 0..0.0005 remain usable, and `CurrentValue` / `ValueFloat` expose
the value held by the current reactive property. `ReactivePropertyFloat` suppresses notifications when
the value is unchanged.

`Value` remains publicly assignable. If code writes a different value or replaces the reactive property,
the next playing frame writes the animation result to that property. Paused or stopped animators leave
the value alone. A replacement must be a non-null `ReactivePropertyFloat`; listeners belong to the
property on which they were registered.

For C# consumers:

```csharp
using Neo.Animations;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

// Inside a component method; both components must be present.
FloatAnimator animator = GetComponent<FloatAnimator>();
CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
animator.MinValue = 0f;
animator.MaxValue = 1f;
ReactivePropertyFloat output = animator.Value;
UnityAction<float> applyAlpha = (float value) => canvasGroup.alpha = value;
output.AddListener(applyAlpha);
applyAlpha(output.CurrentValue);
animator.Play();
```

Retain `output` and `applyAlpha` for the lifetime of the binding; on unbind call
`output.RemoveListener(applyAlpha)`, including when `animator.Value` has since been replaced. Rebind
explicitly to a replacement property. In the Inspector, expand **Value → On Changed** and connect a
dynamic float receiver, then enable **playOnStart** or wire a UnityEvent to `Play()`.

`animationSpeed <= 0` evaluates to `minValue`; it does not change `IsPlaying`. With **Randomize Start Time**
on (the default), `Start` initializes a random clock even when **playOnStart** is disabled; turn it off to
keep the clock the caller set. `ResetTime()` after startup sets the clock to zero without
immediately changing the output; the next playing, unpaused Update advances time before sampling.
`Stop()` retains the current output and clock. Disabling the component suspends Update without changing
its playback flags; see the [shared lifecycle table](README.md#playback-and-pooling-contracts).

## See also

- [Vector3Animator](Vector3Animator.md)
- [ColorAnimator](ColorAnimator.md)
- [README](README.md)
