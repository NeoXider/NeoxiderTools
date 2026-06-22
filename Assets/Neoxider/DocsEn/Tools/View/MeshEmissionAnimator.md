# MeshEmission

**Purpose:** Component that synchronizes mesh emission with a Light source. Copies intensity and color from a `Light` component in real time, applying additional sync settings.

**Namespace:** `Neo.Tools.View`  
**Script:** `Assets/Neoxider/Scripts/Tools/View/MeshEmission.cs`

## Description

A component that synchronizes mesh emission with a Light source. Copies intensity and color from a `Light` component in real time with configurable sync settings. Ideal for effects where material emission must follow a light source.

## Key Features

- **Synchronization**: Copies intensity and color from a `Light` component
- **Flexibility**: Configurable sync options (intensity, color, multipliers)
- **Delay**: Optional synchronization delay
- **Smoothing**: Animation curve for smooth synchronization
- **Auto-find**: Automatic light source detection
- **Events**: UnityEvents for reacting to emission changes

## Public Fields

### Sync Mode
- `syncWithLight` (`bool`) — Enable synchronization with the light source
- `targetLight` (`Light`) — Light source to synchronize with (can be on another object)

### Sync Settings
- `syncIntensity` (`bool`) — Synchronize intensity
- `syncColor` (`bool`) — Synchronize color
- `intensityMultiplier` (`float`) — Intensity multiplier (1.0 = exactly the light's value)
- `syncDelay` (`float`) — Synchronization delay in seconds
- `syncCurve` (`AnimationCurve`) — Curve for smoothing synchronization

### Control
- `playOnStart` (`bool`) — Automatically start synchronization on Start

### Debug Settings
- `enableDebugging` (`bool`) — Enable debug messages

### Events
- `OnIntensityChanged` (`UnityEvent<float>`) — Fired when emission intensity changes
- `OnColorChanged` (`UnityEvent<Color>`) — Fired when emission color changes
- `OnAnimationStarted` (`UnityEvent`) — Fired when synchronization starts
- `OnAnimationStopped` (`UnityEvent`) — Fired when synchronization stops
- `OnAnimationPaused` (`UnityEvent`) — Fired when synchronization is paused

## Public Properties

### Read-only
- `CurrentIntensity` (`float`) — Current emission intensity
- `CurrentColor` (`Color`) — Current emission color
- `IsPlaying` (`bool`) — Whether synchronization is running
- `IsPaused` (`bool`) — Whether synchronization is paused

### Read-write
- `SyncWithLight` (`bool`) — Whether synchronization with the light source is enabled
- `TargetLight` (`Light`) — Light source for synchronization
- `IntensityMultiplier` (`float`) — Intensity multiplier

## Public Methods

### `Play()`
Start synchronization. Sets `IsPlaying = true` and `IsPaused = false`.

### `Stop()`
Stop synchronization. Sets `IsPlaying = false` and `IsPaused = false`.

### `Pause()`
Pause synchronization. Only works while synchronization is running.

### `Resume()`
Resume from pause. Only works while synchronization is paused.

### `ResetToOriginal()`
Reset to the original emission values.

### `ResetSyncTime()`
Reset the synchronization timer to zero.

### `FindAndAttachLight()`
Find and attach the nearest light source on the same object or its children.

## Usage Examples

### Simple Emission Sync
```csharp
public class EmissionController : MonoBehaviour
{
    private MeshEmissionAnimator animator;

    void Start()
    {
        animator = GetComponent<MeshEmissionAnimator>();
        animator.OnIntensityChanged.AddListener(OnIntensityChanged);
        animator.OnColorChanged.AddListener(OnColorChanged);
    }

    void OnIntensityChanged(float intensity)
    {
        Debug.Log($"Emission intensity changed to: {intensity}");
    }

    void OnColorChanged(Color color)
    {
        Debug.Log($"Emission color changed to: {color}");
    }
}
```

### Controlling Sync from Code
```csharp
public class EmissionManager : MonoBehaviour
{
    public MeshEmissionAnimator[] emissionAnimators;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (var anim in emissionAnimators)
            {
                if (anim.IsPlaying)
                    anim.Pause();
                else
                    anim.Resume();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var anim in emissionAnimators)
            {
                anim.Stop();
                anim.Play();
            }
        }
    }
}
```

### Dynamic Parameter Changes
```csharp
public class DynamicEmissionController : MonoBehaviour
{
    public MeshEmissionAnimator animator;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Sync intensity only
            animator.syncIntensity = true;
            animator.syncColor = false;
            animator.intensityMultiplier = 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Sync color only
            animator.syncIntensity = false;
            animator.syncColor = true;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Sync everything with a multiplier
            animator.syncIntensity = true;
            animator.syncColor = true;
            animator.intensityMultiplier = 2.0f; // Emission twice as bright as the light
        }
    }
}
```

### Auto-finding the Light Source
```csharp
public class AutoLightFinder : MonoBehaviour
{
    public MeshEmissionAnimator animator;

    void Start()
    {
        animator.FindAndAttachLight();

        if (animator.TargetLight != null)
            Debug.Log($"Found light: {animator.TargetLight.gameObject.name}");
        else
            Debug.LogWarning("No light found!");
    }
}
```

## Inspector Setup

1. **Sync Mode**: Enable synchronization and assign the light source
2. **Sync Settings**: Configure what to synchronize and with what parameters
3. **Events**: Wire methods to react to events
4. **Control**: Configure auto-start

## Typical Use Cases

### Glowing Object Near a Lamp
- `syncWithLight`: `true`
- `syncIntensity`: `true`
- `syncColor`: `true`
- `intensityMultiplier`: `0.8` (slightly dimmer than the light)

### Emission as Light Reflection
- `syncWithLight`: `true`
- `syncIntensity`: `true`
- `syncColor`: `false` (keep original color)
- `intensityMultiplier`: `0.5`

### Delayed Light Response
- `syncWithLight`: `true`
- `syncDelay`: `0.5` (0.5-second delay)
- `syncCurve`: `AnimationCurve.EaseInOut(0, 0, 1, 1)`

### Emission Brighter Than the Light
- `syncWithLight`: `true`
- `syncIntensity`: `true`
- `intensityMultiplier`: `2.0` (twice as bright)

## Tips

- Use events instead of polling values every frame
- Use `intensityMultiplier` to tune emission brightness relative to the light
- Use `syncDelay` and `syncCurve` for delayed or smoothed effects
- Call `FindAndAttachLight()` for automatic light detection
- Combine multiple `MeshEmissionAnimator` components for complex effects

## Requirements

- **MeshRenderer**: Must be on the same object
- **Material**: Must support emission (keyword `_EMISSION`)
- **Light**: A light source for synchronization (can be on another object)
