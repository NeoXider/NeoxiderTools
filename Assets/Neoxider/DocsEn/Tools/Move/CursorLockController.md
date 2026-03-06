# CursorLockController

## Overview
`CursorLockController` manages cursor lock state (`Cursor.lockState`) and visibility (`Cursor.visible`).
It supports:

- Start state
- Optional apply on `OnEnable` / `OnDisable`
- Optional toggle hotkey
- A master enable switch (`Controller Enabled`)
- Multiple controllers coexistence via ownership (the most recent controller wins until it calls `ReleaseControl()`)

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Move/CursorLockController.cs`

## Inspector fields (high level)
- **Mode**:
  - `LockAndHide`: lock + hide when locked, unlock + show when unlocked
  - `OnlyHide`: visibility only
  - `OnlyLock`: lock state only
- **Control Mode**:
  - `AutomaticAndManual`: both lifecycle/hotkey and direct method calls
  - `AutomaticOnly`: only `Start` / `OnEnable` / `OnDisable` / hotkey
  - `ManualOnly`: only direct method calls

## Public API
- `bool IsLocked`
- `bool ControllerEnabled`
- `CursorLockController.ControlMode Mode`
- `bool HasCursorOwnership`
- `void SetCursorLocked(bool locked)`
- `void ToggleCursorState()`
- `void ShowCursor()`
- `void HideCursor()`
- `void ReleaseControl()`
- `void SetControllerEnabled(bool enabled)`
- `void EnableController()`
- `void DisableController()`

## Typical use-case: gameplay + UI page
For FPS/TPS it is often convenient to keep gameplay controllers on the player and put a dedicated
`CursorLockController` on a menu/pause page GameObject:

- page enabled → `Lock On Enable = false` → cursor is shown/unlocked
- page disabled → `Lock On Disable = true` → cursor returns to gameplay state

If the cursor controller is not on the player, assign it to `PlayerController3DPhysics.External Cursor Lock Controller`.

---

## See also
- [`PlayerController3DPhysics`](./PlayerController3DPhysics.md)
- [`Move`](./README.md)
