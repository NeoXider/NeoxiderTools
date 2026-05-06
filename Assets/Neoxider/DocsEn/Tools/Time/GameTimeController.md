# GameTimeController

**Purpose:** A simple utility to control the game's time speed (`Time.timeScale`). Perfect for pausing the game or creating slow-motion effects via `UnityEvent`.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Reset On Awake** | Automatically resets `Time.timeScale` to 1 when the script is enabled or destroyed (prevents infinite pauses when loading new scenes). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void PauseGame()` | Hard-sets `Time.timeScale = 0`. |
| `void ResumeGame()` | Returns time to normal speed (`Time.timeScale = 1`). |
| `void SetTimeScale(float scale)` | Sets a custom time scale (e.g., `0.5` for slow-motion). |

## Examples

### No-Code Example (Inspector)
On your UI "Pause" button, add `GameTimeController.PauseGame()` to the `OnClick` event. On the "Resume" button, call `GameTimeController.ResumeGame()`.

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
- ← [Tools/Time](../README.md)
