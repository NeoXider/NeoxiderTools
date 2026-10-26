# CameraShake

**Purpose:** A camera (or any object) shake component built on DOTween. Supports separate position and/or rotation shake, configurable strength, vibrato, fade-out, and `Time.timeScale`-independent operation.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Shake Type** | What to shake: `Position`, `Rotation`, or `Both`. |
| **Duration** | Shake duration in seconds. |
| **Strength** | Shake amplitude. |
| **Vibrato** | Number of vibrations (1–50). |
| **Randomness** | Direction randomness (0 = linear, 180 = full chaos). |
| **Fade Out** | Smoothly fade out the shake towards the end. |
| **Shake X / Y / Z** | Which axes to shake (position). |
| **Rotate X / Y / Z** | Which axes to shake (rotation). |
| **Use Unscaled Time** | Ignore `Time.timeScale` (for shaking during pause). |
| **Relative Shake** | Start from the current local pose when enabled; otherwise start from the pose captured in `Awake`, for the selected channels. |
| **Ease Type** | DOTween easing applied to each shake channel. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void StartShake()` | Start shaking with inspector settings. |
| `void StartShake(float duration, float strength)` | Start with custom parameters. |
| `void StopShake()` | Stop shaking and restore the original values of the affected position/rotation channels. |
| `void ResetTransform()` | Reset position/rotation to original values. |
| `void StopAndReset()` | Stop an active shake, then restore both the original local position and rotation. |
| `bool IsShaking { get; }` | Whether the object is currently shaking. |

Position shake preserves fractional movement; `Fade Out` controls amplitude decay without enabling integer snapping. Unscaled time applies to the owning sequence, including both position and rotation.

Unscaled mode ignores Unity's `Time.timeScale`; DOTween's global `timeScale` and `unscaledTimeScale` still affect playback. The native DOTween driver must be active and receiving frames. This component preserves the project's DOTween clock settings.

`Awake` captures the reset pose. `StopShake()` restores the channels selected when that shake started, even if `shakeType` changes during playback. `ResetTransform()` restores both channels without canceling an active tween; use `StopAndReset()` when both actions are required. For a moving camera, put this component on a dedicated child pivot so a stop/reset does not replace the parent camera controller's pose.

Disabling or destroying the component stops its owned sequence. Starting while disabled/inactive does nothing. A listener may stop or replace a shake from `OnShakeStart` or `OnShakeStop`; the latest request wins, and canceled sequences do not emit `OnShakeComplete`.

## Inspector testing

In Play Mode, the Inspector exposes **Start Shake**, **Stop Shake**, and **Stop And Reset** buttons. These
buttons use the same public methods shown above; they are disabled outside Play Mode so testing cannot
modify the scene while editing.

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnShakeStart` | *(none)* | Shake has started. |
| `OnShakeComplete` | *(none)* | Shake has finished naturally. |
| `OnShakeStop` | *(none)* | Shake was stopped manually via `StopShake()`. |

Replacing an active shake and disabling/destroying the component also invoke `OnShakeStop`. Natural completion invokes only `OnShakeComplete`.

## Examples

### No-Code Example (Inspector)
Attach `CameraShake` to your camera. In an explosion event, call `CameraShake.StartShake()`. Set `Strength = 0.5`, `Duration = 0.3`. On every hit, the camera will shake.

### Code Example
```csharp
[SerializeField] private CameraShake _cameraShake;

public void OnPlayerHit(float damage)
{
    _cameraShake.StartShake(0.2f, damage * 0.1f);
}
```

## See Also
- ← [Tools/Other](README.md)
