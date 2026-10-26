# LightAnimator

**Purpose:** Animate intensity and optional color on a local `Light`, or a local URP `Light2D` when URP
is installed. Namespace: `Neo.Tools.View`; assembly: `Neo.Tools.View`.

## Setup

- Add `Light` or `Light2D` first, configure its baseline values, then add `LightAnimator`.
  Light2D takes priority when both are present. No light disables the animator with a diagnostic error.
- Configure the intensity range, animation type/speed, and optional target color/curve. `playOnStart`
  controls automatic playback. UnityEvents can trigger the same typed controls used from C#.

## Runtime controls

`Play()`, `Stop()`, `Pause()`, and `Resume()` control the animation lifecycle. The same four controls are
available as Inspector buttons in Play Mode and are disabled while editing the scene.

```csharp
animator.MinIntensity = 0.5f;
animator.MaxIntensity = 2f;
animator.Play();
```

`Stop()` leaves the displayed light at its last value. `ResetToOriginal()` restores the values captured
in `Awake` and updates `CurrentIntensity`, `Intensity` and `CurrentColor`; it does not stop playback, so
the next update can animate again. Disabling restores that same baseline and readback state. Re-enabling
continues the prior playing/paused state. `ResetTime()` and `RandomizeTime()` change animation phase.
Initial readbacks are populated in `Start`; subscribe to `Intensity` for subsequent intensity changes
and `OnColorChanged` for animated color changes. URP accessors bind directly to the actual Light2D instance.
Optional Light2D discovery supports both Unity 6 URP's separate `Unity.RenderPipelines.Universal.2D.Runtime`
assembly and the older `Unity.RenderPipelines.Universal.Runtime` layout without adding a required URP reference.
Player builds using managed stripping still require Light2D/reflected-accessor preservation and native validation.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `AnimationSpeed` | Animation Speed. |
| `AnimationType` | Animation Type. |
| `CurrentColor` | Current Color. |
| `CurrentIntensity` | Current Intensity. |
| `Intensity` | Intensity. |
| `IntensityValue` | Intensity Value. |
| `IsPaused` | Is Paused. |
| `IsPlaying` | Is Playing. |
| `MaxIntensity` | Max Intensity. |
| `MinIntensity` | Min Intensity. |
| `OnAnimationPaused` | On Animation Paused. |
| `OnAnimationStarted` | On Animation Started. |
| `OnAnimationStopped` | On Animation Stopped. |
| `OnColorChanged` | On Color Changed. |
| `TargetColor` | Target Color. |
| `animationSpeed` | Animation Speed. |
| `animationType` | Animation Type. |
| `changeColor` | Change Color. |
| `colorBlendSpeed` | Color Blend Speed. |
| `customCurve` | Custom Curve. |
| `enableDebugging` | Enable Debugging. |
| `maxIntensity` | Max Intensity. |
| `minIntensity` | Min Intensity. |
| `noiseOffset` | Noise Offset. |
| `noiseScale` | Noise Scale. |
| `playOnStart` | Play On Start. |
| `targetColor` | Target Color. |
| `use2DNoise` | Use2DNoise. |

## See Also

- [Module Root](../README.md)
