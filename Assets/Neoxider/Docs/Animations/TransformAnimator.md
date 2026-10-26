# TransformAnimator

Universal transform animator that combines constant rotation, curve-driven floating (bob), symmetric
scale pulse, continuous Perlin shake and one-shot impulse shakes on a single component.

The math lives in the scene-free `TransformAnimationEvaluator`; the component owns Unity
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

With `ImpulseDecayCurve = null`, impulse strength fades linearly from full strength at impulse time zero
to zero at `ImpulseDuration`. An assigned curve controls the envelope instead. Null `FloatCurve` and
`ScaleCurve` use the cycle phase directly. These fallbacks apply equally to the evaluator and the component.

## Usage

1. Add `TransformAnimator` from **Neoxider/Animations/TransformAnimator**.
2. Enable the required channels and tune their curves.
3. `PlayOnEnable` starts on first startup and every subsequent pool re-enable. The serialized field
   retains `FormerlySerializedAs("playOnStart")` for earlier assets.
4. `RandomizeStartTime` desynchronizes rows of pickups.
5. Call `Shake()` from gameplay code for hit or pickup feedback.

```csharp
TransformAnimator animator = item.GetComponent<TransformAnimator>();
animator.SetTarget(itemVisual); // optional; null selects animator.transform
animator.Play();
animator.Shake(1.5f);
```

Call `Stop()` later when the animation should end; it restores the captured base and cancels the impulse.

## Public API

| Member | Description |
|--------|-------------|
| `Target` / `SetTarget(Transform)` | Restores the old target, then captures the new target's base pose; null selects self |
| `Settings` | Channel settings; null is valid and evaluates to the captured base pose |
| `PlayOnEnable` | Auto-play on startup and pool re-enable without duplicate start events |
| `RandomizeStartTime` / `UseUnscaledTime` | Clock configuration |
| `Play()` / `Stop()` / `Pause()` / `Resume()` | Clock control; `Stop` restores the base pose |
| `Shake(float strength)` / `Shake()` | Arms an impulse; does not start playback; parameterless strength is 1 |
| `CaptureBase()` | Re-captures the effective target's local pose |
| `ResetTime()` / `RandomizeTime()` | Change main time without sampling; do not reset impulse time; RandomizeTime also changes the seed |
| `ApplyCurrentState()` | Evaluates and applies immediately, even when stopped or paused; does not advance time |
| `IsPlaying` / `IsPaused` / `Time` | Runtime state |
| `OnAnimationStarted` / `OnAnimationStopped` / `OnAnimationPaused` | UnityEvents |

For runtime preview, the Inspector exposes Play Mode-only buttons for `Play()`, `Stop()`, `Pause()`,
`Resume()`, and the parameterless `Shake()` impulse. They use the same public lifecycle API and are
disabled while editing the scene.

The captured pose is bound to the exact `Transform` that supplied it. `Stop`, disable and target changes
never apply that pose to another transform. Use `Target` or `SetTarget` for runtime target changes.

`Pause()` freezes both main and impulse advancement. `Stop()` retains main time, restores the base and
clears the impulse. Disable also restores and clears playback/impulse, without emitting a stopped
event; re-enable captures the current target pose and optionally randomizes time/seed and plays.
`PlayOnEnable` avoids duplicate startup events. See the
[lifecycle comparison](README.md#playback-and-pooling-contracts) before pooling value animators and this
component interchangeably.

For scene/NoCode authoring, configure the optional target and channel settings, enable **Play On Enable**
or wire `Play()` from another UnityEvent, then bind `Shake()` or `Shake(float)` for feedback. Lifecycle
UnityEvents can notify other scene components. This wrapper applies the local pose itself; it does not
require a separate value receiver. Call `CaptureBase()` only when the current pose is the intended new
baseline, rather than recapturing an active animation offset unintentionally.

## Scene-free evaluation

```csharp
TransformAnimationSettings settings = new TransformAnimationSettings
{
    RotateEnabled = false,
    ImpulseDuration = 0.4f,
    ImpulseDecayCurve = null // linear fade from 1 to 0
};
TransformAnimationState state = TransformAnimationEvaluator.Evaluate(
    settings, Vector3.zero, Vector3.zero, Vector3.one,
    time: 0f, randomSeed: 3f, impulseTime: 0.1f, impulseStrength: 1f);
```

The caller supplies elapsed impulse time (`-1` means no active impulse). Evaluation returns data without
changing a transform. For Inspector/UnityEvent use, bind `Shake()` for a full-strength impulse, or
`Shake(float)` for a chosen strength, while the component is playing; the component advances impulse time.

## See also

- [Module README](README.md)
- [Vector3Animator](Vector3Animator.md)
- [AnimationUtils](AnimationUtils.md)
