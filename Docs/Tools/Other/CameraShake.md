# CameraShake

**What it is:** a `MonoBehaviour` that shakes a camera (or any object) with DOTween: position and/or rotation,
configurable strength, vibrato, fade-out and `Time.timeScale`-independent playback. Script:
`Scripts/Tools/Other/CameraShake.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Other`.

**How to use:**
1. Add **Neoxider / Tools / CameraShake** to the camera (or to a child pivot of a camera controller).
2. Pick **Shake Type**, **Duration**, **Strength** and the axes.
3. Call `StartShake()` from an explosion/hit UnityEvent, or `StartShake(duration, strength)` from code.

---

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
| **Relative Shake** | Start from the current local pose and settle back to it when enabled; otherwise start from (and return to) the pose captured in `Awake`, for the selected channels. |
| **Ease Type** | DOTween easing applied to each shake channel. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void StartShake()` | Start shaking with inspector settings. |
| `void StartShake(float duration, float strength)` | Start with custom parameters. |
| `void StopShake()` | Stop shaking and restore the affected position/rotation channels to the pose the shake started from (relative) or the `Awake` pose (non-relative). |
| `void ResetTransform()` | Reset position/rotation to the pose captured in `Awake`. |
| `void StopAndReset()` | Stop an active shake, then restore both the original local position and rotation. |
| `bool IsShaking { get; }` | Whether the object is currently shaking. |

Position shake preserves fractional movement; `Fade Out` controls amplitude decay without enabling integer snapping. Unscaled time applies to the owning sequence, including both position and rotation.

Unscaled mode ignores Unity's `Time.timeScale`; DOTween's global `timeScale` and `unscaledTimeScale` still affect playback. The native DOTween driver must be active and receiving frames. This component preserves the project's DOTween clock settings.

`Awake` captures the reset pose. Each shake remembers where it started: with **Relative Shake** that is the current
local pose, so stopping or interrupting a shake on a camera that has moved since `Awake` (a follow camera) returns
it to its pre-shake pose instead of snapping it back to the `Awake` pose. `StopShake()` restores the channels
selected when that shake started, even if `shakeType` changes during playback. `ResetTransform()` restores both
channels to the `Awake` pose without canceling an active tween; use `StopAndReset()` when both actions are required.
A camera controller that writes the same Transform every frame still competes with the shake tween; put this
component on a dedicated child pivot in that case.

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
