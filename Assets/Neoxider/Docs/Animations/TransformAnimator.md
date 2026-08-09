# TransformAnimator

Universal transform animator that combines constant rotation, curve-driven floating (bob), symmetric
scale pulse, continuous Perlin shake and one-shot impulse shakes on a single component.

The math lives in the pure, unit-tested `TransformAnimationEvaluator`; the component only owns Unity
lifecycle, target binding and application of the evaluated local pose.

## Architecture

| Piece | Role |
|-------|------|
| `TransformAnimationSettings` | `[Serializable]` plain-data settings for all channels |
| `TransformAnimationEvaluator` | Static, scene-free evaluation through `Evaluate` and `CyclePhase` |
| `TransformAnimationState` | Evaluated local position, Euler rotation and scale value |
| `TransformAnimator` | MonoBehaviour wrapper: captures the base pose, ticks the clock, applies the result |

## Channels

| Channel | Key fields | Notes |
|---------|------------|-------|
| **Rotate** | `RotateEnabled`, `RotationSpeed` | Constant rotation in degrees per second, per axis |
| **Float (bob)** | `FloatEnabled`, `FloatDirection`, `FloatHeight`, `FloatDuration`, `FloatCurve` | Position offset along a normalized direction |
| **Scale Pulse** | `ScaleEnabled`, `ScaleAmplitude`, `ScaleDuration`, `ScaleCurve` | Curve values 0..1 map symmetrically to `-amplitude..+amplitude` around the base scale |
| **Shake (continuous)** | `ShakeEnabled`, `ShakePositionStrength`, `ShakeRotationStrength`, `ShakeSpeed` | Deterministic Perlin noise; each animator has its own seed |
| **Impulse Shake** | `ImpulseDuration`, `ImpulsePositionStrength`, `ImpulseRotationStrength`, `ImpulseDecayCurve` | One-shot jolt via `Shake(strength)`; a non-positive duration disables it |

All channels compose over the captured base pose.

## Usage

1. Add `TransformAnimator` from **Neoxider/Animations/TransformAnimator**.
2. Enable the required channels and tune their curves.
3. `PlayOnEnable` starts on first startup and every subsequent pool re-enable. Serialized values from the
   earlier `playOnStart` field migrate automatically.
4. `RandomizeStartTime` desynchronizes rows of pickups.
5. Call `Shake()` from gameplay code for hit or pickup feedback.

```csharp
TransformAnimator animator = item.GetComponent<TransformAnimator>();
animator.SetTarget(itemVisual); // optional; null selects animator.transform
animator.Shake(1.5f);
animator.Stop();
```

## Public API

| Member | Description |
|--------|-------------|
| `Target` / `SetTarget(Transform)` | Restores the old target, then captures the new target's base pose; null selects self |
| `Settings` | Channel settings; null is valid and evaluates to the captured base pose |
| `PlayOnEnable` | Auto-play on startup and pool re-enable without duplicate start events |
| `RandomizeStartTime` / `UseUnscaledTime` | Clock configuration |
| `Play()` / `Stop()` / `Pause()` / `Resume()` | Clock control; `Stop` restores the base pose |
| `Shake(float strength)` / `Shake()` | One-shot impulse shake |
| `CaptureBase()` | Re-captures the effective target's local pose |
| `ResetTime()` / `RandomizeTime()` | Clock control |
| `ApplyCurrentState()` | Evaluates and applies immediately |
| `IsPlaying` / `IsPaused` / `Time` | Runtime state |
| `OnAnimationStarted` / `OnAnimationStopped` / `OnAnimationPaused` | UnityEvents |

For runtime preview, the Inspector exposes Play Mode-only buttons for `Play()`, `Stop()`, `Pause()`,
`Resume()`, and the parameterless `Shake()` impulse. They use the same public lifecycle API and are
disabled while editing the scene.

The captured pose is bound to the exact `Transform` that supplied it. `Stop`, disable and target changes
never apply that pose to another transform. Use `Target` or `SetTarget` for runtime target changes.

## See also

- [Module README](README.md)
- [Vector3Animator](Vector3Animator.md)
- [AnimationUtils](AnimationUtils.md)
