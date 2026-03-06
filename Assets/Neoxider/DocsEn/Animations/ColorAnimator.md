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
| **animationSpeed** | float | Speed (0 = disabled). Range 0–30. |
| **noiseScale**, **use2DNoise**, **noiseOffset** | — | Noise settings. |
| **customCurve** | AnimationCurve | Curve for CustomCurve type. |
| **playOnStart** | bool | Auto-start on Start. |
| **OnColorChanged** | UnityEvent&lt;Color&gt; | Invoked when color changes. |
| **OnAnimationStarted / Stopped / Paused** | UnityEvent | Lifecycle events. |

## API

- **CurrentColor** — current animated color (read-only)
- **IsPlaying**, **IsPaused** — state
- **StartColor**, **EndColor**, **AnimationSpeed**, **AnimationType** — writable
- **Play()**, **Stop()**, **Pause()**, **Resume()**, **ResetTime()**, **RandomizeTime()**

## See also

- [Vector3Animator](Vector3Animator.md)
- [FloatAnimator](FloatAnimator.md)
- [README](README.md)
