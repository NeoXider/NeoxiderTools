# AnimationType

Enumeration of animation types used by FloatAnimator, ColorAnimator, Vector3Animator and by `AnimationUtils.GetAnimatedFloat` / `GetAnimatedColor` / `GetAnimatedVector3`. Namespace `Neo.Animations`, file `Scripts/Animations/AnimationType.cs`.

## Values

| Value | Description |
|-------|-------------|
| **RandomFlicker** | Random flicker between min and max. |
| **Pulsing** | Smooth sine-based pulsing. |
| **SmoothTransition** | Smooth back-and-forth (PingPong). |
| **PerlinNoise** | Perlin noise; use noiseScale, use2DNoise, noiseOffset. |
| **SinWave** | Sine wave. |
| **Exponential** | Exponential decay. |
| **BounceEase** | Bounce with decay. |
| **ElasticEase** | Elastic effect. |
| **CustomCurve** | Driven by an AnimationCurve (customCurve). |

## Usage

Select in animator components (**animationType** field) or pass as the first argument to `AnimationUtils.GetAnimatedFloat` / `GetAnimatedColor` / `GetAnimatedVector3`.

## See also

- [AnimationUtils](AnimationUtils.md)
- [FloatAnimator](FloatAnimator.md)
- Russian docs: [AnimationType](../../Docs/Animations/AnimationType.md)
