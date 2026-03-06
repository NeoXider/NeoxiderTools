# FloatAnimator

**Namespace:** `Neo.Animations`  
**File:** `Assets/Neoxider/Scripts/Animations/FloatAnimator.cs`

## Purpose

Universal animator for float values. Animates any numeric value with configurable animation types. Exposes the value via reactive property `Value` (subscribe with `Value.OnChanged`) and events.

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **animationType** | AnimationType | Animation type. |
| **minValue** | float | Minimum value. |
| **maxValue** | float | Maximum value. |
| **animationSpeed** | float | Speed (0 = disabled). Range 0–30. |
| **noiseScale**, **use2DNoise**, **noiseOffset** | — | Noise settings for PerlinNoise. |
| **customCurve** | AnimationCurve | Curve for CustomCurve type. |
| **playOnStart** | bool | Auto-start on Start. |
| **Value** | ReactivePropertyFloat | Reactive value; subscribe via Value.OnChanged. |
| **OnAnimationStarted / Stopped / Paused** | UnityEvent | Lifecycle events. |

## API

- **CurrentValue**, **ValueFloat** — current animated value (read-only)
- **IsPlaying**, **IsPaused** — state
- **MinValue**, **MaxValue**, **AnimationSpeed**, **AnimationType** — writable
- **Play()**, **Stop()**, **Pause()**, **Resume()**, **ResetTime()**, **RandomizeTime()**

## See also

- [Vector3Animator](Vector3Animator.md)
- [ColorAnimator](ColorAnimator.md)
- [README](README.md)
