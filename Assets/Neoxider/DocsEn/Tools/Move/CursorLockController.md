# CursorLockController

**Purpose:** A global or local manager for the mouse cursor state (visibility and `CursorLockMode`). Automatically handles UI lifecycles (opening/closing menus) and restores the previous cursor state using a snapshot stack system. Features presets for common scenarios (Gameplay, UI Page).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Preset** | Quick setup presets: `Gameplay_Default`, `UI_Page_ShowCursorWhileActive`, `UI_MenuScene_Standalone`. |
| **Mode** | What aspect to control: `LockAndHide` (lock + invisible), `OnlyHide` (visibility only), or `OnlyLock`. |
| **Control Mode** | `AutomaticAndManual` (responds to Unity events and code calls), `AutomaticOnly`, or `ManualOnly`. |
| **Lock On Start / Enable / Disable** | The desired cursor state during various Unity lifecycle events. |
| **Lifecycle Snapshot Mode** | Whether to save the cursor state (SaveOnEnable/SaveOnDisable) to restore it later when a menu closes. |
| **Toggle Key** | The key used to manually toggle the cursor (typically `Escape`). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void ShowCursor()` | Makes the cursor visible and unlocks it. |
| `void HideCursor()` | Hides the cursor and locks it to the center of the screen. |
| `void SetCursorLocked(bool locked)` | Directly sets the requested state (true = locked/hidden). |
| `void ReleaseControl()` | Relinquishes cursor control. Hands authority back to the previous active `CursorLockController` in the stack. |
| `bool IsLocked { get; }` | Returns the current system state of `Cursor.lockState`. |

## Examples

### No-Code Example (Inspector)
On your "Pause Menu" panel GameObject, attach `CursorLockController`. Select the **`UI_Page_ShowCursorWhileActive`** preset. That's it! When the menu becomes active, the cursor will appear. When the menu is disabled, the cursor will automatically lock and hide again, restoring gameplay control.

### Code Example
```csharp
[SerializeField] private CursorLockController _cursorManager;

public void StartMiniGame()
{
    // Force the cursor to show so the player can click UI elements
    _cursorManager.ShowCursor();
}
```

## See Also
- [PlayerController3DPhysics](PlayerController3DPhysics.md)
- ← [Tools/Move](../README.md)
