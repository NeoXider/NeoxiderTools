# Vector3Animator

**Namespace:** `Neo.Animations`  
**File:** `Assets/Neoxider/Scripts/Animations/Vector3Animator.cs`

## Purpose

Universal animator for Vector3 values. Animates position, scale, rotation, or any other Vector3 parameter using configurable animation types. The component updates the animated vector each frame and exposes it via `CurrentVector` and `OnVectorChanged`.

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **animationType** | AnimationType | Animation type (PerlinNoise, Pulsing, etc.). |
| **startVector** | Vector3 | Start vector. |
| **endVector** | Vector3 | End vector. |
| **animationSpeed** | float | Speed; <= 0 evaluates to startVector. Inspector range 0–30. |
| **noiseScale** | float | Noise scale for PerlinNoise. |
| **use2DNoise** | bool | Use 2D noise instead of 1D. |
| **noiseOffset** | Vector2 | Additional noise offset. |
| **customCurve** | AnimationCurve | Curve for CustomCurve type. |
| **playOnStart** | bool | Auto-start on Start. |
| **OnVectorChanged** | UnityEvent&lt;Vector3&gt; | Invoked when distance from the last notified vector exceeds 0.001. |
| **OnAnimationStarted** | UnityEvent | Invoked when animation starts. |
| **OnAnimationStopped** | UnityEvent | Invoked when animation stops. |
| **OnAnimationPaused** | UnityEvent | Invoked when animation is paused. |

## API

### Properties (read-only)

- **CurrentVector** — Current animated vector
- **IsPlaying** — Whether animation is playing
- **IsPaused** — Whether animation is paused

### Writable properties

- **StartVector**, **EndVector**, **AnimationSpeed**, **AnimationType**

### Methods

- **Play()** — Start animation
- **Stop()** — Stop animation
- **Pause()** — Pause (only if playing)
- **Resume()** — Resume from pause
- **ResetTime()** — Reset animation time to zero
- **RandomizeTime()** — Set random initial time

`Play()`, `Stop()`, `Pause()`, and `Resume()` are also available as Play Mode-only Inspector buttons for
runtime preview and lifecycle testing.

## Output contract

`Start` initializes `CurrentVector` and the last notified vector from `startVector`, without emitting a
change event. Each playing, unpaused Update evaluates `CurrentVector`; `OnVectorChanged` requires
distance greater than 0.001 from the last notified sample. Poll `CurrentVector` when every sample is
needed. Interpolation is component-wise Vector3 interpolation; a receiver chooses world/local space,
position/scale/Euler angles. This is not quaternion rotation interpolation.

## Example (code)

Inside a component method, with a `Vector3Animator` present:

```csharp
using Neo.Animations;
using UnityEngine;
using UnityEngine.Events;

Vector3Animator animator = GetComponent<Vector3Animator>();
UnityEvent<Vector3> changes = animator.OnVectorChanged ??= new UnityEvent<Vector3>();
UnityAction<Vector3> applyPosition = (Vector3 position) => transform.position = position;
changes.AddListener(applyPosition);
applyPosition(animator.StartVector);
animator.Play();
```

Retain `changes` and `applyPosition`; call `changes.RemoveListener(applyPosition)` when unbinding,
even if the component's public event field has since been replaced.

## Example (No-Code)

Add Vector3Animator, set start/end vectors and animation type, and connect **On Vector Changed** to a
dynamic Vector3 receiver that applies world position, local position, local scale or Euler angles.
Author or apply the receiver's initial value explicitly. Enable **playOnStart**, or wire `Play()` from
another UnityEvent; `Pause()`, `Resume()` and `Stop()` use the same public methods.

`animationSpeed <= 0` selects `startVector` without stopping playback. Startup randomizes the clock
even when auto-play is disabled; resetting before Start is overwritten. Stop retains output/time and
disable suspends Update without resetting flags. See the
[shared lifecycle table](README.md#playback-and-pooling-contracts).

## See also

- [FloatAnimator](FloatAnimator.md)
- [ColorAnimator](ColorAnimator.md)
- [AnimationType](AnimationType.md)
- [Tools/View/LightAnimator](../Tools/View/LightAnimator.md)
- [README](README.md)
