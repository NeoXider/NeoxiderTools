# ColorAnimator

**Namespace:** `Neo.Animations`  
**File:** `Assets/Neoxider/Scripts/Animations/ColorAnimator.cs`

## Purpose

Universal animator for colors. Animates between start and end colors using configurable animation types. Exposes current color via `CurrentColor` and `OnColorChanged`.

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **animationType** | AnimationType | Animation type. |
| **startColor**, **endColor** | Color | Start and end colors. |
| **animationSpeed** | float | Speed; <= 0 evaluates to startColor. Inspector range 0–30. |
| **noiseScale**, **use2DNoise**, **noiseOffset** | — | Noise settings. |
| **customCurve** | AnimationCurve | Curve for CustomCurve type. |
| **playOnStart** | bool | Auto-start on Start. |
| **OnColorChanged** | UnityEvent&lt;Color&gt; | Invoked when RGBA distance from the last notified color exceeds 0.001. |
| **OnAnimationStarted / Stopped / Paused** | UnityEvent | Lifecycle events. |

## API

- **CurrentColor** — current animated color (read-only)
- **IsPlaying**, **IsPaused** — state
- **StartColor**, **EndColor**, **AnimationSpeed**, **AnimationType** — writable
- **Play()**, **Stop()**, **Pause()**, **Resume()**, **ResetTime()**, **RandomizeTime()**

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

`animationSpeed <= 0` selects `startColor` without stopping playback. Startup randomizes the clock even
when auto-play is disabled. Stop retains output/time; disabling suspends Update without resetting flags.
See the [shared lifecycle table](README.md#playback-and-pooling-contracts) for reset and pooling behavior.

## See also

- [Vector3Animator](Vector3Animator.md)
- [FloatAnimator](FloatAnimator.md)
- [README](README.md)
