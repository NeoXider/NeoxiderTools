# GameTimeController

**Purpose:** A simple utility to control the game's time speed (`Time.timeScale`). Perfect for pausing the game or creating slow-motion effects via `UnityEvent`.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Reset On Awake** | Automatically resets `Time.timeScale` to 1 in `Awake` and `OnDestroy`. Re-enabling the component does not reset speed. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void PauseGame()` | Hard-sets `Time.timeScale = 0`. |
| `void ResumeGame()` | Returns time to normal speed (`Time.timeScale = 1`). |
| `void SetTimeScale(float scale)` | Sets a custom finite time scale (e.g., `0.5` for slow-motion). Negative values clamp to zero; NaN and infinities leave the current speed unchanged. |

## Examples

### No-Code Example (Inspector)
On your UI "Pause" button, add `GameTimeController.PauseGame()` to the `OnClick` event. On the "Resume" button, call `GameTimeController.ResumeGame()`.

Pause-menu timers should use unscaled time if they need to continue during the pause. Prefer one owner of global game speed: multiple controllers write the same `Time.timeScale`, and destroying one with **Reset On Awake** enabled resets speed to 1.

### Code Example
```csharp
[SerializeField] private GameTimeController _timeController;

public void EnterBulletTime()
{
    // Slow down time by 5x for a cinematic shot
    _timeController.SetTimeScale(0.2f);
}
```

## See Also
- [TimerObject](TimerObject.md)
- ← [Tools/Time](README.md)
