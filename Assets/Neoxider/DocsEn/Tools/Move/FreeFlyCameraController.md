# FreeFlyCameraController

**Purpose:** Scene View style free-flight controller for debug cameras, spectator cameras, level previews, and internal tools.

Default controls:

- Right mouse button: enable look and flight
- `W/A/S/D`: forward/left/back/right
- `E/Q`: up/down
- `Left Shift`: fast movement
- `Left Alt`: slow movement
- Mouse wheel: adjust base speed

## Location

- Add Component: `Neoxider/Tools/FreeFlyCameraController`
- Create menu: `Neoxider/Tools/Movement/FreeFlyCameraController`
- Namespace: `Neo.Tools`
- Script: `Assets/Neoxider/Scripts/Tools/Move/FreeFlyCameraController.cs`

## Quick Start

1. Add `FreeFlyCameraController` to a `Camera` or another object that should fly freely.
2. Keep `Require Look Button` enabled for Unity-like behavior: look and movement work while RMB is held.
3. Disable `Require Look Button` if look should always be active.
4. Disable `Move Only While Looking` if keyboard movement should work without RMB.

## Inspector Fields

| Field | Description |
|---|---|
| `Controller Enabled` | Runtime master switch. |
| `Require Look Button` | Requires a held mouse button for look mode. Enabled by default. |
| `Look Mouse Button` | Look-mode mouse button: `0` left, `1` right, `2` middle. |
| `Move Only While Looking` | Ignores keyboard movement until look mode is active. Enabled by default. |
| `Lock Cursor While Looking` | Hides and locks the cursor while looking, then restores the previous state. |
| `Input Backend` | Legacy Input Manager, New Input System, or automatic fallback. |
| `Log Input Fallback Warnings` | Enables one-time warning logs when input backend fallback is used. Off by default to avoid runtime log noise. |
| `Movement Space` | `Local` flies along object axes, `World` ignores object rotation. |
| `Base Speed` | Base movement speed in units per second. |
| `Fast / Slow Multiplier` | Multipliers used by the fast and slow modifier keys. |
| `Allow Mouse Wheel Speed` | Allows mouse wheel changes to `Base Speed`. |
| `Look Sensitivity` | Mouse look sensitivity. |
| `Invert Y` | Inverts vertical look. |
| `Min / Max Pitch` | Vertical angle limits to prevent flipping. |

## API

| Method / Property | Description |
|---|---|
| `SetControllerEnabled(bool)` | Enables or disables the controller. |
| `SetRequireLookButton(bool)` | Toggles the required mouse button gate. |
| `SetMoveOnlyWhileLooking(bool)` | Controls whether movement depends on look mode. |
| `SetBaseSpeed(float)` | Changes base speed with min/max clamping. |
| `SetExternalMoveInput(Vector3?)` | Overrides movement input, useful for UI, replay, or tests. `null` returns to built-in input. |
| `SetExternalLookInput(Vector2?)` | Overrides look input. `null` returns to built-in input. |
| `ClearExternalInput()` | Clears both external inputs. |
| `SetRotationAngles(float yaw, float pitch)` | Sets yaw/pitch and applies rotation. |
| `Warp(Vector3, Quaternion)` | Teleports the object and syncs internal angles. |
| `Tick(float)` | Manual controller step for tests or an external driver. |
| `IsLooking` / `IsFlying` | Current look and movement state. |

## Events

- `On Look Start`
- `On Look Stop`
- `On Fly Start`
- `On Fly Stop`

## Notes

- Use `PlayerController3DPhysics` for player gameplay cameras; `FreeFlyCameraController` is intended for free debug/spectator cameras.
- It can be used with `CursorLockController`, but `Lock Cursor While Looking` already snapshots and restores the cursor state when RMB is released.
- In networked scenes, enable it only on the local debug/spectator camera. It does not implement Mirror synchronization.

## See Also

- [CameraRotationController](./CameraRotationController.md)
- [CursorLockController](./CursorLockController.md)
- [PlayerController3DPhysics](./PlayerController3DPhysics.md)
