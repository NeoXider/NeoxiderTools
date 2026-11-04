# AnimationType

**What it is:** the enumeration of animation curves used by FloatAnimator, ColorAnimator, Vector3Animator,
the Tools/View LightAnimator and `AnimationUtils.GetAnimatedFloat` / `GetAnimatedColor` / `GetAnimatedVector3`.
Namespace `Neo.Animations`, file `Scripts/Animations/AnimationType.cs`.

**How to use:** pick a value in an animator's **animationType** field, or pass it as the first argument of
the `AnimationUtils` methods.

---

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

The utilities receive elapsed time from their caller. `speed <= 0` returns the initial/minimum value;
it does not stop a component's playback state. `CustomCurve` evaluates the supplied curve at
`animationTime * speed`, with clamped interpolation between the endpoints. Its wrap modes control
looping; a null curve returns the initial value.

`Pulsing` uses a sine argument of `PI * time * speed`, while `SinWave` uses `2 * PI * time * speed`.
`RandomFlicker` samples Unity's global random generator on each evaluation and cannot be reproduced by
time alone. Perlin evaluation uses the supplied offsets/scale; use the noise-aware overloads when those
parameters must be explicit. See [AnimationUtils](AnimationUtils.md) for sampling and ownership contracts.

## See also

- [Module README](README.md)
- [AnimationUtils](AnimationUtils.md)
- [FloatAnimator](FloatAnimator.md)
- [ColorAnimator](ColorAnimator.md)
- [Vector3Animator](Vector3Animator.md)
