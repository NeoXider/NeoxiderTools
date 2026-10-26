# CursorLockController

**Purpose:** Owns mouse cursor visibility/lock through a controller stack and **drives referenced `PlayerController3DPhysics` instances**. Each controller can suspend look and optionally movement. Overlapping controllers share ownership of each player gate: only the last release restores the value captured by the first request. Features presets for gameplay and UI panels.

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
| **Disable Look While Cursor Visible** | On by default. Acquires a look suspension; releases it on lock. The last owner restores the captured value, including an initially disabled gate. |
| **Disable Movement While Cursor Visible** | Off by default. Also suspends player movement while the cursor is visible. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void ShowCursor()` | Makes the cursor visible and unlocks it (suspends driven players). |
| `void HideCursor()` | Hides/locks the cursor and releases this controller's player-gate requests. Other owners can keep input suspended. |
| `void SetCursorLocked(bool locked)` | Directly sets the requested state (true = locked/hidden). |
| `void ReleaseControl()` | Relinquishes cursor control. Hands authority back to the previous active `CursorLockController` in the stack. |
| `void RegisterPlayer(PlayerController3DPhysics player)` | Adds a player at runtime (e.g. a network-spawned player), binds this controller as its cursor owner and applies the current cursor state to it. |
| `void UnregisterPlayer(PlayerController3DPhysics player)` | Removes a player and releases only this controller's gate requests. |
| `bool IsLocked { get; }` | Returns the current system state of `Cursor.lockState`. |
| `bool ManageCursor { get; set; }` | Runtime access to the per-instance master switch. |
| `bool DisableLookWhileCursorVisible { get; set; }` | Runtime access to the look toggle. |
| `bool DisableMovementWhileCursorVisible { get; set; }` | Runtime access to the movement toggle. |
| `static bool GlobalCursorManagement { get; set; }` | Global kill-switch. When `false`, **every** `CursorLockController` instance stops writing cursor state and driving players — for projects that ship their own cursor system but reuse prefabs containing this component. Default `true`; resets on domain reload. |

## Choosing a cursor setup

### Overlapping owners in 11.0

Reference the same player from each panel's controller. Enable **Disable Movement While Cursor Visible**
on panels that should block walking. The existing C# calls and Inspector presets need no extra manager:

```csharp
firstPanel.ShowCursor();
secondPanel.ShowCursor();
firstPanel.ReleaseControl();  // secondPanel still blocks the requested player gates
secondPanel.ReleaseControl(); // last release restores each gate's first captured value
```

Look and movement have separate captured values and owners. Repeated requests/releases are idempotent;
unregistering, disabling or destroying one owner does not release another owner's request. The cursor
stack still chooses the cursor state independently: locking one controller releases its requests,
while another outstanding controller can continue blocking that player.

This is snapshot ownership for cursor controllers. An external `SetMovementEnabled` / `SetLookEnabled`
call made while requests are active does not update the captured baseline. The last release restores
that original value. A general base-intent/scoped-input API is not part of this change; coordinate
cutscene/pause setters accordingly. A gate already false before the first request remains false on release.

Internal look restoration also preserves the surviving controller's cursor request. For example, if
panel A blocks only look and panel B blocks only movement, disabling A restores look's captured value
while B keeps movement blocked and its cursor visible/unlocked. This repair is silent: it does not send
B another unlock event, acquire new gate baselines, or replace the player's assigned external owner.
When a top controller closes and exposes a lower controller, the existing activation notification for
that lower controller still applies. With no surviving cursor owner, the player's ordinary standalone
`SetLookEnabled(true)` auto-lock behavior remains available with its default cursor-control settings.

> The rows below name the **legacy** `PlayerController3DPhysics`. With the current [CharacterController module](./CharacterController/README.md) the split is different: `NeoCameraInput` never writes `Cursor` state at all — it only *reads* `Cursor.visible` (`Pause Look When Cursor Visible`) — so a `CursorLockController` is always the owner. The character prefabs ship with one on the root; if your scene already has an owner, remove the prefab's. To gate movement as well, call `NeoCharacterInput.SetMovementEnabled(false)`.

| Scenario | Setup |
|----------|-------|
| **(a) Standalone player controller only** | No `CursorLockController` in the scene. `PlayerController3DPhysics` keeps its full built-in behaviour: lock on start, its own Escape toggle, cursor writes. *(Legacy only — the CMF-based controller has no built-in cursor handling and needs a `CursorLockController`.)* |
| **(b) CursorLockController owns cursor + player** | Add `CursorLockController` (e.g. `Gameplay_Default` preset) and reference the player in *Player Controllers* (or put both on the same GameObject). Esc is handled here only; the player defers automatically and its look/movement follows the cursor state. |
| **(c) Your own cursor system** | Turn **Manage Cursor** off on each `CursorLockController` (or set `CursorLockController.GlobalCursorManagement = false` once at startup), and turn **Enable Cursor Control** off on `PlayerController3DPhysics`. Neither component will touch `UnityEngine.Cursor`. |

## Examples

### No-Code Example (Inspector)
On your "Pause Menu" panel GameObject, attach `CursorLockController` and select **`UI_Page_ShowCursorWhileActive`**.
When the panel activates, its configured cursor state is applied. Disabling it releases its player-gate
requests and follows the cursor snapshot/stack policy; another open panel can keep gameplay input blocked.

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
