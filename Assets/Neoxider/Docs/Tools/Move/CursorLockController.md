# CursorLockController

**Purpose:** The single owner of the mouse cursor state (visibility and `CursorLockMode`). Automatically handles UI lifecycles (opening/closing menus), restores the previous cursor state using a snapshot stack system, and **drives referenced `PlayerController3DPhysics` instances**: while the cursor is visible, player look (and optionally movement) is suspended; when the cursor locks again, only what this controller suspended is restored. Features presets for common scenarios (Gameplay, UI Page).

## Cursor ownership rule

**Esc owner = `CursorLockController`; the player controller defers automatically.** When a `CursorLockController` is active for a player (same GameObject, assigned in the player's *External Cursor Lock Controller* field, or referencing the player in its *Player Control* list), `PlayerController3DPhysics` skips all of its own cursor paths — lock-on-start, its own Escape toggle, and direct cursor writes (`SetCursorLocked` forwards to the owner). Configure Escape behaviour only here (*Allow Toggle* / *Toggle Key*).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Preset** | Quick setup presets: `Gameplay_Default`, `UI_Page_ShowCursorWhileActive`, `UI_MenuScene_Standalone`. |
| **Manage Cursor** | Master switch (*Cursor Ownership* section). Turn off if your game manages the cursor itself: the component then never reads keys, never applies lifecycle state and never writes `Cursor` automatically. Explicit calls (`ShowCursor`/`HideCursor`/`SetCursorLocked`) still work. |
| **Mode** | What aspect to control: `LockAndHide` (lock + invisible), `OnlyHide` (visibility only), or `OnlyLock`. |
| **Control Mode** | `AutomaticAndManual` (responds to Unity events and code calls), `AutomaticOnly`, or `ManualOnly`. |
| **Lock On Start / Enable / Disable** | The desired cursor state during various Unity lifecycle events. |
| **Lifecycle Snapshot Mode** | Whether to save the cursor state (SaveOnEnable/SaveOnDisable) to restore it later when a menu closes. |
| **Allow Toggle / Toggle Key** | The key used to toggle the cursor (typically `Escape`). This component is the single Escape owner when a player controller is present. |
| **Player Controllers** | *Player Control* section. Explicit `PlayerController3DPhysics` references driven by this controller. Referenced players are auto-bound so they defer their own cursor handling. |
| **Auto Find Player On Scene** | Off by default. Fallback when the list is empty: finds players in the scene once. Prefer explicit references. |
| **Disable Look While Cursor Visible** | On by default. Suspends player look while the cursor is visible; restores it on lock. A player disabled by someone else (pause, cutscene) is never force-enabled. |
| **Disable Movement While Cursor Visible** | Off by default. Also suspends player movement while the cursor is visible. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void ShowCursor()` | Makes the cursor visible and unlocks it (suspends driven players). |
| `void HideCursor()` | Hides the cursor and locks it (restores driven players). |
| `void SetCursorLocked(bool locked)` | Directly sets the requested state (true = locked/hidden). |
| `void ReleaseControl()` | Relinquishes cursor control. Hands authority back to the previous active `CursorLockController` in the stack. |
| `void RegisterPlayer(PlayerController3DPhysics player)` | Adds a player at runtime (e.g. a network-spawned player), binds this controller as its cursor owner and applies the current cursor state to it. |
| `void UnregisterPlayer(PlayerController3DPhysics player)` | Removes a player, restoring anything this controller suspended on it. |
| `bool IsLocked { get; }` | Returns the current system state of `Cursor.lockState`. |
| `bool ManageCursor { get; set; }` | Runtime access to the per-instance master switch. |
| `bool DisableLookWhileCursorVisible { get; set; }` | Runtime access to the look toggle. |
| `bool DisableMovementWhileCursorVisible { get; set; }` | Runtime access to the movement toggle. |
| `static bool GlobalCursorManagement { get; set; }` | Global kill-switch. When `false`, **every** `CursorLockController` instance stops writing cursor state and driving players — for projects that ship their own cursor system but reuse prefabs containing this component. Default `true`; resets on domain reload. |

## Choosing a cursor setup

| Scenario | Setup |
|----------|-------|
| **(a) Standalone player controller only** | No `CursorLockController` in the scene. `PlayerController3DPhysics` keeps its full built-in behaviour: lock on start, its own Escape toggle, cursor writes. |
| **(b) CursorLockController owns cursor + player** | Add `CursorLockController` (e.g. `Gameplay_Default` preset) and reference the player in *Player Controllers* (or put both on the same GameObject). Esc is handled here only; the player defers automatically and its look/movement follows the cursor state. |
| **(c) Your own cursor system** | Turn **Manage Cursor** off on each `CursorLockController` (or set `CursorLockController.GlobalCursorManagement = false` once at startup), and turn **Enable Cursor Control** off on `PlayerController3DPhysics`. Neither component will touch `UnityEngine.Cursor`. |

## Examples

### No-Code Example (Inspector)
On your "Pause Menu" panel GameObject, attach `CursorLockController`. Select the **`UI_Page_ShowCursorWhileActive`** preset. That's it! When the menu becomes active, the cursor will appear. When the menu is disabled, the cursor will automatically lock and hide again, restoring gameplay control.

For a first-person scene: attach `CursorLockController` with the **`Gameplay_Default`** preset to a manager object and drag your player into **Player Controllers**. Escape now shows the cursor and freezes the camera; pressing it again locks the cursor and returns control — no wiring needed.

### Code Example
```csharp
[SerializeField] private CursorLockController _cursorManager;

public void StartMiniGame()
{
    // Force the cursor to show so the player can click UI elements.
    // Look on driven players is suspended automatically.
    _cursorManager.ShowCursor();
}

public void OnPlayerSpawned(PlayerController3DPhysics player)
{
    // Network-spawned players can be attached at runtime.
    _cursorManager.RegisterPlayer(player);
}
```

## See Also
- [PlayerController3DPhysics](PlayerController3DPhysics.md)
- ← [Tools/Move](../README.md)
