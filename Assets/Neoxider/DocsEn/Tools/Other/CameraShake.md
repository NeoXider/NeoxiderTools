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

## API

| Method / Property | Description |
|-------------------|-------------|
| `void StartShake()` | Start shaking with inspector settings. |
| `void StartShake(float duration, float strength)` | Start with custom parameters. |
| `void StopShake()` | Stop shaking and reset to original position. |
| `void ResetTransform()` | Reset position/rotation to original values. |
| `bool IsShaking { get; }` | Whether the object is currently shaking. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnShakeStart` | *(none)* | Shake has started. |
| `OnShakeComplete` | *(none)* | Shake has finished naturally. |
| `OnShakeStop` | *(none)* | Shake was stopped manually via `StopShake()`. |

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
