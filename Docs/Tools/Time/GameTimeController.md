# GameTimeController

**What it is:** A MonoBehaviour (`Scripts/Tools/Time/GameTimeController.cs`, namespace `Neo.Tools`) that controls the global game speed (`Time.timeScale`) for pause buttons and slow-motion via `UnityEvent`.

**How to use:**
1. Add **Neoxider > Tools > GameTimeController** to a persistent object.
2. Wire buttons to `PauseGame()`, `ResumeGame()`, `TogglePause()` or `SetTimeScale(float)`.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Reset On Awake** | Automatically resets `Time.timeScale` to 1 in `Awake` and `OnDestroy`. Re-enabling the component does not reset speed. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void PauseGame()` | Hard-sets `Time.timeScale = 0`, remembering the running speed for `TogglePause()`. |
| `void ResumeGame()` | Returns time to normal speed (`Time.timeScale = 1`); `ResumeScale` becomes 1. |
| `void TogglePause()` | Pauses, or resumes to `ResumeScale` (for example a slow-motion scale). One-button pause toggle. |
| `bool IsPaused { get; }` | True while `Time.timeScale` is zero. |
| `float ResumeScale { get; }` | Speed `TogglePause()` resumes to: the last non-zero scale set through this controller (or observed when it paused); 1 after `ResumeGame()`. A pause made elsewhere (another controller or a direct `Time.timeScale = 0`) resumes to this value too. |
| `void SetTimeScale(float scale)` | Sets a custom finite time scale (e.g., `0.5` for slow-motion). A non-zero value becomes `ResumeScale`; `0` pauses like `PauseGame()`. Negative values clamp to zero; NaN and infinities leave the current speed unchanged. |

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
