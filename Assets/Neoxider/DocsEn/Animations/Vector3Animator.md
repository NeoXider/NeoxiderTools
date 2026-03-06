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
| **animationSpeed** | float | Speed (0 = disabled). Range 0–30. |
| **noiseScale** | float | Noise scale for PerlinNoise. |
| **use2DNoise** | bool | Use 2D noise instead of 1D. |
| **noiseOffset** | Vector2 | Additional noise offset. |
| **customCurve** | AnimationCurve | Curve for CustomCurve type. |
| **playOnStart** | bool | Auto-start on Start. |
| **OnVectorChanged** | UnityEvent&lt;Vector3&gt; | Invoked when vector changes. |
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

## Example (code)

```csharp
var animator = GetComponent<Vector3Animator>();
animator.OnVectorChanged.AddListener(pos => transform.position = pos);
animator.Play();
```

## Example (No-Code)

Add Vector3Animator to a GameObject, set start/end vectors and animation type, enable playOnStart. Subscribe to OnVectorChanged in the Inspector and assign a method that applies the Vector3 to transform.position, localScale, or rotation.

## See also

- [FloatAnimator](FloatAnimator.md)
- [ColorAnimator](ColorAnimator.md)
- [AnimationType](AnimationType.md)
- [Tools/View/LightAnimator](../Tools/View/LightAnimator.md)
- [README](README.md)
