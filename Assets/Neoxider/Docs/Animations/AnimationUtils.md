# AnimationUtils

**What it is:** a static helper class that computes animated values over time; the core shared by every
animator component. Script: `Scripts/Animations/AnimationUtils.cs`, namespace `Neo.Animations`.

**How to use:** call the static methods with your own time (`GetAnimatedFloat`, `GetAnimatedColor`,
`GetAnimatedVector3`) and apply the result, or use the animator components that call them for you.

---

## Main API

- **GetAnimatedFloat**(type, min, max, animationTime, speed, customCurve = null) — animated float
- **GetAnimatedColor**(type, colorA, colorB, animationTime, speed, customCurve = null) — interpolated color
- **GetAnimatedColor**(type, colorA, colorB, animationTime, speed, use2DNoise, randomOffset, noiseOffset, noiseScale, customCurve = null) — noise-aware overload (PerlinNoise honours scale/2D/offsets and the per-instance random offset)
- **GetAnimatedVector3**(type, vectorA, vectorB, animationTime, speed, customCurve = null) — interpolated Vector3
- **GetAnimatedVector3**(type, vectorA, vectorB, animationTime, speed, use2DNoise, randomOffset, noiseOffset, noiseScale, customCurve = null) — noise-aware overload
- **GetTargetValue**(...) — low-level target value by type and parameters
- **GetPerlinNoiseValue**(...), **GetColorBlendFactor**(...) — public lower-level helpers
- **ApplyToLight**(ILightAccessor, ...), **ApplyToMesh**(Material, ...) — apply intensity/color to light or material emission
- **ApplyToRenderer**(Renderer, MaterialPropertyBlock, ...) — the same emission through a caller-owned
  `MaterialPropertyBlock`, so the renderer keeps its shared material (no per-object material copy)
- **EvaluateEmission**(...) — the pure emission math behind both, for custom receivers
- **EmissionColorPropertyId** — cached `_EmissionColor` property id used by the emission helpers
- **SanitizeDeltaTime**(float) — the clock step the animators accept (negative/NaN/infinite become 0)
- **ColorDistanceSquared**(Color, Color), **ChangeThreshold**, **ChangeThresholdSquared** — the change test
  used by the animators' change events (distance greater than 0.001)

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

Avoid `renderer.material` for per-object emission: it silently clones the material for every object.
`ApplyToRenderer` writes through a `MaterialPropertyBlock` instead; allocate the block once and reuse it.
Property blocks keep the shared material but opt the renderer out of the SRP Batcher, so prefer a shared
material when many objects animate in lockstep.

```csharp
private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

private void Update()
{
    float pulse = AnimationUtils.GetAnimatedFloat(AnimationType.SinWave, 0.5f, 2f, Time.time, 1f);
    AnimationUtils.ApplyToRenderer(_renderer, _block, pulse, _baseEmission, false, Color.white, 0f);
}
```

The emission keyword (`_EMISSION`) must already be enabled on the shared material.

## See also

- [Module README](README.md)
- [AnimationType](AnimationType.md)
- [FloatAnimator](FloatAnimator.md)
- [ColorAnimator](ColorAnimator.md)
- [Vector3Animator](Vector3Animator.md)
- [TransformAnimator](TransformAnimator.md) — composable pose evaluator and scene wrapper
