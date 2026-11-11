# ColorAnimator

**What it is:** a MonoBehaviour that animates a color between `startColor` and `endColor` with an
[AnimationType](AnimationType.md) and reports it through `CurrentColor` and `OnColorChanged`. It does not
apply the color anywhere itself. Script: `Scripts/Animations/ColorAnimator.cs`, namespace `Neo.Animations`.

**How to use:**
1. Add the component, set the two colors, `animationType` and `animationSpeed`.
2. Connect **On Color Changed** to a dynamic `Color` receiver (sprite, image, light), or poll `CurrentColor`.
3. Leave **playOnStart** on, or call `Play()`.
4. For a custom clock set `ManualUpdate = true` and call `Tick(deltaTime)` yourself.

---

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **animationType** | AnimationType | Animation type. |
| **startColor**, **endColor** | Color | Start and end colors. |
| **animationSpeed** | float | Speed; <= 0 evaluates to startColor. Inspector range 0–30. |
| **noiseScale**, **use2DNoise**, **noiseOffset** | — | Noise settings. |
| **customCurve** | AnimationCurve | Curve for CustomCurve type. |
| **playOnStart** | bool | Auto-start on Start. |
| **_randomizeStartTime** (`RandomizeStartTime`) | bool | Default on: randomize the clock and noise offset in `Start`. Off: deterministic start at time 0, or at the time set by `ResetTime()` before `Start`. |
| **_useUnscaledTime** (`UseUnscaledTime`) | bool | Advance on `Time.unscaledDeltaTime`. |
| **OnColorChanged** | UnityEvent&lt;Color&gt; | Invoked when RGBA distance from the last notified color exceeds 0.001. |
| **OnAnimationStarted / Stopped / Paused** | UnityEvent | Lifecycle events. |

## API

- **CurrentColor** — current animated color (read-only)
- **IsPlaying**, **IsPaused** — state
- **AnimationTime** — current clock in seconds (read-only)
- **StartColor**, **EndColor**, **AnimationSpeed**, **AnimationType** — writable
- **Play()**, **Stop()**, **Pause()**, **Resume()**, **ResetTime()**, **RandomizeTime()**
- **Tick(float deltaTime)** — advance and sample; ignored while stopped, paused or disabled; bad deltas do not move the clock.
- **ManualUpdate** (`bool`, not serialized) — when true, `Update` does nothing and the owner calls `Tick`.

`Play()`, `Stop()`, `Pause()`, and `Resume()` are also exposed as Play Mode-only Inspector buttons for
runtime preview and lifecycle testing.

## Output and bindings

`Start` sets `CurrentColor` and the last notified color to `startColor` without emitting a change event.
Each playing, unpaused Update evaluates `CurrentColor`; `OnColorChanged` uses RGBA distance greater
than 0.001 from the last notified value. Poll `CurrentColor` when every sample is needed. The component
does not apply the color to a renderer or own a material.

Inside a component method, with a `ColorAnimator` and `SpriteRenderer` present:

```csharp
using Neo.Animations;
using UnityEngine;
using UnityEngine.Events;

ColorAnimator animator = GetComponent<ColorAnimator>();
SpriteRenderer sprite = GetComponent<SpriteRenderer>();
UnityEvent<Color> changes = animator.OnColorChanged ??= new UnityEvent<Color>();
UnityAction<Color> applyColor = (Color color) => sprite.color = color;
changes.AddListener(applyColor);
applyColor(animator.StartColor);
animator.Play();
```

Retain `changes` and `applyColor`; call `changes.RemoveListener(applyColor)` when unbinding. Retaining
the event instance also permits cleanup if another caller replaces the public event field.

For Inspector/NoCode setup, configure the endpoints and animation type, then connect
**On Color Changed** to a dynamic `Color` receiver. Author or apply its initial color explicitly;
startup does not force a notification. Enable **playOnStart**, or connect a UnityEvent to `Play()`;
`Pause()`, `Resume()` and `Stop()` are callable through the same API.

`animationSpeed <= 0` selects `startColor` without stopping playback. With **Randomize Start Time** on,
startup randomizes the clock even when auto-play is disabled. Stop retains output/time; disabling
suspends Update without resetting flags.
See the [shared lifecycle table](README.md#playback-and-pooling-contracts) for reset and pooling behavior.

## See also

- [Vector3Animator](Vector3Animator.md)
- [FloatAnimator](FloatAnimator.md)
- [README](README.md)
