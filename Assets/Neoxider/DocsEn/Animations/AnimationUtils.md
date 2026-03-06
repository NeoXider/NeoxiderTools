# AnimationUtils

Static helper class for computing animated values over time. Core of the animation system (`Neo.Animations`, `Scripts/Animations/AnimationUtils.cs`). No instances; call static methods from code or use via animator components.

## Main API

- **GetAnimatedFloat**(type, min, max, animationTime, speed, customCurve = null) — animated float
- **GetAnimatedColor**(type, colorA, colorB, animationTime, speed, customCurve = null) — interpolated color
- **GetAnimatedVector3**(type, vectorA, vectorB, animationTime, speed, customCurve = null) — interpolated Vector3
- **GetTargetValue**(...) — low-level target value by type and parameters
- **GetPerlinNoiseValue**(...), **GetColorBlendFactor**(...) — internal building blocks
- **ApplyToLight**(ILightAccessor, ...), **ApplyToMesh**(Material, ...) — apply intensity/color to light or material emission

When `speed` is 0, methods return the minimum/initial value (no animation).

## Example

```csharp
float value = AnimationUtils.GetAnimatedFloat(
    AnimationType.PerlinNoise,
    0f, 1f,
    Time.time, 2f
);
```

## See also

- [AnimationType](AnimationType.md)
- [FloatAnimator](FloatAnimator.md)
- Russian docs: [AnimationUtils](../../Docs/Animations/AnimationUtils.md)
