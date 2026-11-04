# LightAnimator

**What it is:** a MonoBehaviour that animates the intensity, and optionally the color, of a local `Light`,
or a local URP `Light2D` when URP is installed. Script: `Scripts/Tools/View/LightAnimator.cs`, namespace
`Neo.Tools.View`, assembly `Neo.Tools.View`.

**How to use:**
1. Add `Light` or `Light2D` first and configure its baseline values, then add `LightAnimator`.
   Light2D takes priority when both are present. No light disables the animator with a diagnostic error.
2. Set the intensity range, animation type/speed, and optionally `changeColor` with a target color.
3. Leave `playOnStart` on, or call `Play()` from code or a UnityEvent.
4. Subscribe to `Intensity` (reactive) and `OnColorChanged` for readbacks.

---

## Runtime controls

`Play()`, `Stop()`, `Pause()`, and `Resume()` control the animation lifecycle. The same four controls are
available as Inspector buttons in Play Mode and are disabled while editing the scene.

```csharp
animator.MinIntensity = 0.5f;
animator.MaxIntensity = 2f;
animator.Play();

// Drive it from your own clock instead of Update.
animator.ManualUpdate = true;
animator.Tick(Time.unscaledDeltaTime);
```

`Stop()` leaves the displayed light at its last value. `ResetToOriginal()` restores the values captured
in `Awake` and updates `CurrentIntensity`, `Intensity` and `CurrentColor`; it does not stop playback, so
the next update can animate again. Disabling restores that same baseline and readback state. Re-enabling
continues the prior playing/paused state. `ResetTime()` and `RandomizeTime()` change animation phase.
Initial readbacks are populated in `Start`; subscribe to `Intensity` for subsequent intensity changes
and `OnColorChanged` for animated color changes.

If the light component is destroyed while the animator lives, the next tick stops the animator (and
`LightComponent` reads null) instead of throwing every frame; the reset on disable is skipped as well.

URP accessors bind directly to the actual Light2D instance. Optional Light2D discovery supports both
Unity 6 URP's separate `Unity.RenderPipelines.Universal.2D.Runtime` assembly and the older
`Unity.RenderPipelines.Universal.Runtime` layout without adding a required URP reference; the reflected
type is resolved once per domain, not per instance. Player builds using managed stripping still require
Light2D/reflected-accessor preservation and native validation.

Each tick reads the applied intensity and color back once and allocates nothing while `enableDebugging`
is off (debugging logs a formatted line every frame).

## Fields

| Field | Description |
|-------|-------------|
| `animationType` | Animation curve family (see [AnimationType](../../Animations/AnimationType.md)). |
| `minIntensity`, `maxIntensity` | Intensity range; `maxIntensity` is kept at or above `minIntensity`. |
| `animationSpeed` | Speed; 0 holds `minIntensity`. |
| `noiseScale`, `use2DNoise`, `noiseOffset` | Perlin noise settings. |
| `changeColor`, `targetColor`, `colorBlendSpeed` | Optional ping-pong blend from the original color to `targetColor`. |
| `customCurve` | Curve for the `CustomCurve` type. |
| `playOnStart` | Start playing in `Start`. |
| `_randomizeStartTime` (`RandomizeStartTime`) | Default on: random clock and noise offset in `Start` so lights do not flicker in sync. Off: start at time 0 or at the time set by `ResetTime()` before `Start`. |
| `_useUnscaledTime` (`UseUnscaledTime`) | Advance on `Time.unscaledDeltaTime`. |
| `enableDebugging` | Logs the evaluated values every frame (debug only). |
| `Intensity` | `ReactivePropertyFloat` readback of the applied intensity. |
| `OnColorChanged`, `OnAnimationStarted`, `OnAnimationStopped`, `OnAnimationPaused` | UnityEvents. |

## Properties and methods

| Member | Description |
|--------|-------------|
| `CurrentIntensity`, `IntensityValue`, `CurrentColor` | Current readbacks. |
| `IsPlaying`, `IsPaused` | Playback state. |
| `MinIntensity`, `MaxIntensity`, `AnimationSpeed`, `AnimationType`, `TargetColor` | Typed setters for the fields. |
| `LightComponent` | The animated `Light` / `Light2D`, or null. |
| `ManualUpdate` | Not serialized. When true, `Update` does nothing and the owner calls `Tick`. |
| `Tick(float deltaTime)` | Advance and apply; ignored while stopped, paused or disabled (the baseline `OnDisable` restored stays); negative/NaN/infinite deltas do not move the clock. |
| `Play()`, `Stop()`, `Pause()`, `Resume()`, `ResetToOriginal()`, `ResetTime()`, `RandomizeTime()` | Lifecycle. |

## See also

- [MeshEmission](MeshEmission.md) - copy a light into a mesh's emission.
- [Animations](../../Animations/README.md)
- [Module root](README.md)
