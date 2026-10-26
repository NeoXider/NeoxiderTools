# AnimationUtils

Static helper class for computing animated values over time. Core of the animation system (`Neo.Animations`, `Scripts/Animations/AnimationUtils.cs`). No instances; call static methods from code or use via animator components.

## Main API

- **GetAnimatedFloat**(type, min, max, animationTime, speed, customCurve = null) — animated float
- **GetAnimatedColor**(type, colorA, colorB, animationTime, speed, customCurve = null) — interpolated color
- **GetAnimatedColor**(type, colorA, colorB, animationTime, speed, use2DNoise, randomOffset, noiseOffset, noiseScale, customCurve = null) — noise-aware overload (PerlinNoise honours scale/2D/offsets and the per-instance random offset)
- **GetAnimatedVector3**(type, vectorA, vectorB, animationTime, speed, customCurve = null) — interpolated Vector3
- **GetAnimatedVector3**(type, vectorA, vectorB, animationTime, speed, use2DNoise, randomOffset, noiseOffset, noiseScale, customCurve = null) — noise-aware overload
- **GetTargetValue**(...) — low-level target value by type and parameters
- **GetPerlinNoiseValue**(...), **GetColorBlendFactor**(...) — public lower-level helpers
- **ApplyToLight**(ILightAccessor, ...), **ApplyToMesh**(Material, ...) — apply intensity/color to light or material emission

For `GetTargetValue` and the `GetAnimated*` methods, `speed <= 0` returns the minimum/initial value.
`CustomCurve` with a null curve also returns that initial value. An assigned curve is evaluated at
`animationTime * speed`; interpolation clamps its output to the endpoint range.
Curve looping follows the supplied `AnimationCurve` wrap modes; the utility does not reset its time.

The low-level `GetPerlinNoiseValue` and `GetColorBlendFactor` helpers evaluate their own formulas;
they do not apply the minimum-value shortcut. Callers own the supplied time and noise offsets.
The short color/vector overloads use zero noise offsets and scale 1; use the full overloads for explicit
Perlin settings. Repeated analytic/Perlin evaluation with the same inputs does not advance a clock.
`RandomFlicker` instead consumes `UnityEngine.Random` on each sample; time alone does not reproduce it.

## Shared-time example

Inside a component method, supply one time to all evaluations to align their phase. Components do not
automatically share their private playback clocks.

```csharp
using Neo.Animations;
using UnityEngine;

float elapsed = Time.time;
float brightness = AnimationUtils.GetAnimatedFloat(
    AnimationType.SinWave, 0.2f, 1f, elapsed, 2f);
Vector3 scale = AnimationUtils.GetAnimatedVector3(
    AnimationType.SinWave, Vector3.one, Vector3.one * 1.2f, elapsed, 2f);
```

The caller applies the returned values and chooses scaled, unscaled or manually accumulated time.
These APIs use Unity math types but do not require a scene component.

## Light and material ownership

`ApplyToLight` requires a valid caller-supplied `ILightAccessor` and writes its intensity/color.
`ApplyToMesh` requires a valid caller-supplied `Material` and writes `_EmissionColor`. The helper does
not create or dispose a material, enable a shader's emission keyword, or choose shared versus per-object
material ownership. Configure the material/shader and its lifetime at the receiver; see
[Tools/View](../Tools/View/README.md) for existing scene wrappers.

## See also

- [Module README](README.md)
- [AnimationType](AnimationType.md)
- [FloatAnimator](FloatAnimator.md)
- [ColorAnimator](ColorAnimator.md)
- [Vector3Animator](Vector3Animator.md)
- [TransformAnimator](TransformAnimator.md) — composable pose evaluator and scene wrapper
